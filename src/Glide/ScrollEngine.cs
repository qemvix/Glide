using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace Glide {
internal sealed class ScrollEngine:IDisposable {
 private readonly Thread thread; private Thread observer; private readonly AutoResetEvent refreshSignal=new AutoResetEvent(false); private readonly ManualResetEvent ready=new ManualResetEvent(false);
 private readonly Motion vertical=new Motion(),horizontal=new Motion();
 private TouchpadOutput touchpad;private bool endGesture;private volatile string outputMode="Starting";private readonly bool gestureTesting; private readonly Native.HookProc callback; private readonly bool testing;
 private volatile Settings settings; private volatile bool stopping,failed;
 private volatile string status="Starting...",error; private volatile string lastInput="No wheel input yet",lastRoute="Waiting for your mouse wheel"; private long seen; private volatile string lastCancellation="None"; private readonly string[] history=new string[24];private int historyPosition;
 private uint threadId,ownIntegrity; private IntPtr hook,target,foreground; private bool timing;
 private volatile Snapshot snapshot;
 private readonly Dictionary<uint,ProcessInfo> processes=new Dictionary<uint,ProcessInfo>();
 private readonly uint ownPid=(uint)Process.GetCurrentProcess().Id;
 private double lastFrame; private readonly Stopwatch clock=Stopwatch.StartNew();
 private readonly Native.Input[] inputBuffer=new Native.Input[1];
 internal string Status {get{return status;}}
 internal string Error {get{return error;}}
 internal bool Failed {get{return failed;}}
 internal volatile bool Gliding; internal long Intercepted,Emitted; internal volatile string LastDecision;
 private sealed class ProcessInfo {internal string Name;internal uint Pid,Integrity;internal double Checked;internal bool Valid;}
 private sealed class Snapshot {internal IntPtr Root,Foreground;internal bool Allowed;internal string App,FrontApp;internal bool HoverFullscreen,FrontFullscreen;internal uint Routing;}
 internal ScrollEngine(Settings initial,bool testMode,bool testGestures=false) {
  gestureTesting=testGestures;
  settings=initial.Copy();testing=testMode;callback=Hook;
  thread=new Thread(Run){IsBackground=true,Name="Glide mouse input"};thread.SetApartmentState(ApartmentState.STA);thread.Start();
  if(!ready.WaitOne(5000)){Dispose();throw new TimeoutException("The mouse input service did not start.");}
  if(error!=null){Dispose();throw new InvalidOperationException(error);}
 }
 internal void Update(Settings value) {
  Settings copy=value.Copy();copy.Validate();settings=copy;
  Native.PostThreadMessage(threadId,0x8001,UIntPtr.Zero,IntPtr.Zero);
 }
 private double Now {get{return clock.Elapsed.TotalMilliseconds;}}
 private void Run() {
  try {
   threadId=Native.GetCurrentThreadId();Native.Message message;
   Native.PeekMessage(out message,IntPtr.Zero,0,0,0);
   ProcessInfo self=ReadProcess(ownPid);
   if(!self.Valid)throw new InvalidOperationException("Windows did not allow Glide to check its input permissions.");
   ownIntegrity=self.Integrity;
   string touchpadReason=null;if(settings.NativeTouchpad&&(!testing||gestureTesting))touchpad=TouchpadOutput.Create(out touchpadReason);
   outputMode=touchpad!=null?"Native two-finger touchpad gestures":"Wheel fallback"+(touchpadReason==null?"":" - "+touchpadReason);Refresh();
   hook=Native.SetWindowsHookEx(14,callback,Native.GetModuleHandle(null),0);
   if(hook==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"Windows could not start the mouse input service.");
   observer=new Thread(Observe){IsBackground=true,Name="Glide app checks"}; observer.Start();ready.Set();
   while(!stopping) {
    while(Native.PeekMessage(out message,IntPtr.Zero,0,0,1)) {
     if(message.Id==0x8001)Cancel("Settings changed");
     Native.TranslateMessage(ref message);Native.DispatchMessage(ref message);
    }
    if(endGesture){endGesture=false;if(touchpad!=null)touchpad.Stop();}
    double now=Now;
    if(vertical.Active||horizontal.Active) {
     if(!ValidTarget())Cancel(LastDecision??"Target invalid");
     else if(now-lastFrame>=7) {
      lastFrame=now;
      int dy=vertical.Step(now),dx=horizontal.Step(now);bool sent;
      if(touchpad!=null){sent=touchpad.Move(dx,dy);if(sent&&(dx!=0||dy!=0))Interlocked.Increment(ref Emitted);}
      else sent=Emit(dy,false)&&Emit(dx,true);
      if(!sent) {
       failed=true;error=(touchpad==null?"":touchpad.Error+" ")+"Windows blocked scrolling output. Smoothing is paused; normal wheel input is restored. Restart Glide to retry.";
       status="Paused - output blocked";Cancel();
      }
     }
    }
    bool active=vertical.Active||horizontal.Active;Gliding=active;SetTiming(active);if(!active&&touchpad!=null)touchpad.Stop();
    uint delay=active?8u:uint.MaxValue;
    if(Native.MsgWaitForMultipleObjectsEx(0,IntPtr.Zero,delay,0x04FF,4)==uint.MaxValue)
     throw new Win32Exception(Marshal.GetLastWin32Error(),"The mouse input message loop stopped.");
   }
  } catch(Exception ex){error=ex.Message;failed=true;status="Paused - service stopped";}
  finally {if(hook!=IntPtr.Zero){Native.UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}if(touchpad!=null)touchpad.Dispose();SetTiming(false);ready.Set();}
 }
 private void SetTiming(bool active) {
  if(active==timing)return;
  if(active)timing=Native.timeBeginPeriod(1)==0;else{Native.timeEndPeriod(1);timing=false;}
 }
 private void Record(string value){history[(historyPosition++)%history.Length]=clock.Elapsed.TotalSeconds.ToString("0.00")+"s "+value;}
 private void Cancel(string reason="Target changed"){if(Gliding){lastCancellation=reason;Record("Cancel: "+reason);}vertical.Clear();horizontal.Clear();target=IntPtr.Zero;Gliding=false;if(touchpad!=null){endGesture=true;Native.PostThreadMessage(threadId,0x8002,UIntPtr.Zero,IntPtr.Zero);}}
 private IntPtr Hook(int code,IntPtr message,IntPtr pointer) {
  if(code<0)return Native.CallNextHookEx(hook,code,message,pointer);
  try {
   int kind=(int)message.ToInt64();
   if(kind==0x0201||kind==0x0204||kind==0x0207||kind==0x020B)Cancel("Mouse clicked"); if(kind==0x0200&&target!=IntPtr.Zero){Native.MouseHook move=(Native.MouseHook)Marshal.PtrToStructure(pointer,typeof(Native.MouseHook));if(Native.WindowFromPoint(move.Point)!=target){LastDecision="Pointer move: "+move.Point.X+","+move.Point.Y+"; hit="+Native.WindowFromPoint(move.Point)+"; target="+target;Cancel(LastDecision);}}
   if(kind==Native.Wheel||kind==Native.HWheel) {
    Native.MouseHook data=(Native.MouseHook)Marshal.PtrToStructure(pointer,typeof(Native.MouseHook));
    uint tag=unchecked((uint)data.Extra.ToUInt64());
    if(tag==Native.SelfTag){if(!ValidTarget()){Cancel(LastDecision??"Queued output target invalid");return new IntPtr(1);}return Native.CallNextHookEx(hook,code,message,pointer);}
    if(touchpad!=null&&(data.Flags&1)!=0&&!(testing&&tag==Native.TestTag))return Native.CallNextHookEx(hook,code,message,pointer);
    int delta=unchecked((short)(data.MouseData>>16));Settings cfg=settings;
    // No process queries, file access or cross-process messaging in this callback.
    bool physical=(data.Flags&1)==0||tag==0||(testing&&tag==Native.TestTag);
    Snapshot snap=snapshot;Interlocked.Increment(ref seen);lastInput=(snap==null?"Unknown app":snap.App)+" | delta "+delta+" | "+((data.Flags&1)==0?"hardware":"mouse software");IntPtr child=Native.WindowFromPoint(data.Point);
    IntPtr root=Native.GetAncestor(child,2),focus=Native.GetForegroundWindow();
    if(!physical||delta==0||delta%120!=0||!cfg.Enabled||failed||Native.Modified()
     ||Native.Down(1)||Native.Down(2)||Native.Down(4)
     ||snap==null||!snap.Allowed||root!=snap.Root||focus!=snap.Foreground) {
      lastRoute=BypassReason(physical,delta,cfg,snap,root,focus); LastDecision="Bypass: physical="+physical+"; delta="+delta+"; enabled="+cfg.Enabled+"; failed="+failed+"; modified="+Native.Modified()+"; buttons="+(Native.Down(1)||Native.Down(2)||Native.Down(4))+"; allowed="+(snap!=null&&snap.Allowed)+"; root="+root+"/"+(snap==null?IntPtr.Zero:snap.Root)+"; foreground="+focus+"/"+(snap==null?IntPtr.Zero:snap.Foreground)+"; tag="+tag; Record(lastInput+" | "+lastRoute);Cancel(lastRoute);return Native.CallNextHookEx(hook,code,message,pointer);
    }
    if(child!=target||focus!=foreground)Cancel();
    target=child;foreground=focus;
    double now=Now;
    Motion motion=kind==Native.HWheel?horizontal:vertical;
    motion.Add(delta*cfg.SpeedPercent/100.0,now,cfg.GlideMilliseconds);Gliding=true;
    lastRoute=touchpad!=null?"Native touchpad gesture":"Smoothing wheel input";Record(lastInput+" | smoothed");Interlocked.Increment(ref Intercepted);Native.PostThreadMessage(threadId,0x8002,UIntPtr.Zero,IntPtr.Zero);return new IntPtr(1);
   }
  } catch {Cancel();}
  return Native.CallNextHookEx(hook,code,message,pointer);
 }
 private bool ValidTarget() {
  if(target==IntPtr.Zero||failed||!settings.Enabled||Native.Modified()
   ||Native.Down(1)||Native.Down(2)||Native.Down(4)||Native.GetForegroundWindow()!=foreground){LastDecision="Invalid state: target="+target+"; enabled="+settings.Enabled+"; modified="+Native.Modified()+"; buttons="+(Native.Down(1)||Native.Down(2)||Native.Down(4))+"; fg="+Native.GetForegroundWindow()+"/"+foreground;return false;}
  Native.Point point;if(!Native.GetCursorPos(out point)||Native.WindowFromPoint(point)!=target){LastDecision="Pointer changed: "+point.X+","+point.Y+"; hit="+Native.WindowFromPoint(point)+"; target="+target;return false;}
  Snapshot snap=snapshot;return snap!=null&&snap.Allowed&&snap.Root==Native.GetAncestor(target,2);
 }
 private bool Emit(int delta,bool sideways) {
  if(delta==0)return true;Native.Input input=new Native.Input();
  input.Mouse.Data=unchecked((uint)delta);input.Mouse.Flags=sideways?0x1000u:0x0800u;
  input.Mouse.Extra=new UIntPtr(Native.SelfTag);inputBuffer[0]=input;
  bool sent=Native.SendInput(1,inputBuffer,Marshal.SizeOf(typeof(Native.Input)))==1;
  if(sent)Interlocked.Increment(ref Emitted);return sent;
 }
 private void Observe() { while(!stopping) { try { Refresh(); } catch { snapshot=null;status="Normal scrolling - target unavailable"; } refreshSignal.WaitOne(70); } }
 private void Refresh() {
  Settings cfg=settings;Native.Point point;Native.GetCursorPos(out point);
  IntPtr root=Native.GetAncestor(Native.WindowFromPoint(point),2),focus=Native.GetForegroundWindow();string reason=null;ProcessInfo hover=GetProcess(root),front=GetProcess(focus);uint routing; if(!Native.SystemParametersInfo(0x201C,0,out routing,0))routing=0;bool hoverFull=IsFullscreen(root),frontFull=IsFullscreen(focus);
  if(failed)reason="Paused - service stopped";
  else if(!cfg.Enabled)reason="Paused";
  else if(cfg.PauseFullscreen&&ShouldPauseFullscreen(routing==2?hover.Name:front.Name,routing==2?hoverFull:frontFull))reason="Paused - full-screen app";
  else {
   if(!hover.Valid||!front.Valid||hover.Integrity>ownIntegrity||front.Integrity>ownIntegrity)reason="Normal scrolling - protected app";
   else if(Excluded(hover.Name,cfg.ExcludedApps)||Excluded(front.Name,cfg.ExcludedApps))reason="Normal scrolling - excluded app";
   else if(hover.Pid==ownPid&&!testing)reason="Ready - try another app";
  }
  snapshot=new Snapshot{Root=root,Foreground=focus,Allowed=reason==null&&root!=IntPtr.Zero,App=hover.Name??"Unknown app",FrontApp=front.Name??"Unknown",HoverFullscreen=hoverFull,FrontFullscreen=frontFull,Routing=routing};
  status=reason??"Smooth scrolling is on";
 }
 internal static bool Excluded(string process,string list) {
  foreach(string item in (list??"").Split(new char[]{',',';','\r','\n'},StringSplitOptions.RemoveEmptyEntries)) {
   string name=item.Trim();if(name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))name=name.Substring(0,name.Length-4);
   if(String.Equals(process,name,StringComparison.OrdinalIgnoreCase))return true;
  }return false;
 }
 internal static bool ShouldPauseFullscreen(string app,bool fullscreen) {
  if(!fullscreen)return false;
  string name=(app??"").ToLowerInvariant();
  return name!="explorer"&&name!="discord"&&name!="discordcanary"&&name!="discordptb";
 }
 internal static bool IsFullscreen(IntPtr window) {
  if(window==IntPtr.Zero)return false;
  StringBuilder className=new StringBuilder(256);Native.GetClassName(window,className,className.Capacity);
  uint style=unchecked((uint)Native.GetWindowLong(window,-16));
  Native.Rect rect;
  if(Native.DwmGetWindowAttribute(window,9,out rect,Marshal.SizeOf(typeof(Native.Rect)))!=0&&!Native.GetWindowRect(window,out rect))return false;
  System.Drawing.Rectangle bounds=Screen.FromHandle(window).Bounds;
  return FullscreenCandidate(System.Drawing.Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom),bounds,Native.IsZoomed(window),style,className.ToString());
 }
 internal static bool FullscreenCandidate(System.Drawing.Rectangle window,System.Drawing.Rectangle monitor,bool maximized,uint style,string className) {
  if(className=="Progman"||className=="WorkerW"||className=="Shell_TrayWnd"||className=="Shell_SecondaryTrayWnd")return false;
  // Ordinary maximized windows remain eligible, including with hidden taskbars.
  // A borderless full-screen game still qualifies even when marked maximized.
  if(maximized&&(style&0x00C40000)!=0)return false;
  return window.Left<=monitor.Left+1&&window.Top<=monitor.Top+1&&window.Right>=monitor.Right-1&&window.Bottom>=monitor.Bottom-1;
 }
 private string BypassReason(bool physical,int delta,Settings cfg,Snapshot snap,IntPtr root,IntPtr focus) {
  if(!cfg.Enabled)return "Paused by the switch or shortcut";
  if(failed)return "Input service stopped: "+error;
  if(!physical)return "Another input tool's tagged events are passed through";
  if(delta==0||delta%120!=0)return "Precision scrolling passed through";
  if(Native.Modified())return "Modifier shortcut passed through";
  if(Native.Down(1)||Native.Down(2)||Native.Down(4))return "Mouse button held";
  if(snap==null||!snap.Allowed)return status;
  if(root!=snap.Root||focus!=snap.Foreground)return "New target - checking app permissions";
  return "Normal wheel input";
 }
 internal string DiagnosticsText() {
  Snapshot snap=snapshot;
  return "Glide 2.0 input diagnostics\r\n"+"Output mode: "+outputMode+"\r\n"+
   "Current status: "+status+"\r\n"+
   "App under pointer: "+(snap==null?"Unknown":snap.App)+"\r\n"+
   "Foreground app: "+(snap==null?"Unknown":snap.FrontApp)+"\r\n"+"Full-screen hover / foreground: "+(snap==null?"Unknown":snap.HoverFullscreen+" / "+snap.FrontFullscreen)+"\r\n"+"Windows wheel routing: "+(snap==null?"Unknown":snap.Routing.ToString())+"\r\n"+"Last wheel: "+lastInput+"\r\n"+
   "Last decision: "+lastRoute+"\r\n"+
   "Wheel events seen: "+Interlocked.Read(ref seen)+"\r\n"+
   "Wheel events smoothed: "+Interlocked.Read(ref Intercepted)+"\r\n"+
   "Output movement frames submitted: "+Interlocked.Read(ref Emitted)+"\r\n"+
   "Gliding: "+Gliding+"\r\n"+
   "Last cancellation: "+lastCancellation+"\r\n"+"Last error: "+(error??"None")+"\r\nRecent decisions:\r\n"+String.Join("\r\n",history);
 }
 private ProcessInfo GetProcess(IntPtr window) {
  uint pid;Native.GetWindowThreadProcessId(window,out pid);if(pid==0)return new ProcessInfo();ProcessInfo info;
  if(!processes.TryGetValue(pid,out info)||Now-info.Checked>3000) {
   if(processes.Count>128)processes.Clear();info=ReadProcess(pid);info.Checked=Now;processes[pid]=info;
  }return info;
 }
 private static ProcessInfo ReadProcess(uint pid) {
  ProcessInfo result=new ProcessInfo{Pid=pid};IntPtr process=Native.OpenProcess(0x1000,false,pid),token=IntPtr.Zero,memory=IntPtr.Zero;
  if(process==IntPtr.Zero)return result;
  try {
   StringBuilder text=new StringBuilder(2048);int count=text.Capacity;
   if(!Native.QueryFullProcessImageName(process,0,text,ref count))return result;
   result.Name=Path.GetFileNameWithoutExtension(text.ToString());
   if(!Native.OpenProcessToken(process,8,out token))return result;
   int size;Native.GetTokenInformation(token,25,IntPtr.Zero,0,out size);if(size<=0)return result;
   memory=Marshal.AllocHGlobal(size);if(!Native.GetTokenInformation(token,25,memory,size,out size))return result;
   IntPtr sid=Marshal.ReadIntPtr(memory);byte authorities=Marshal.ReadByte(Native.GetSidSubAuthorityCount(sid));if(authorities==0)return result;
   result.Integrity=unchecked((uint)Marshal.ReadInt32(Native.GetSidSubAuthority(sid,(uint)authorities-1)));result.Valid=true;
  } finally {
   if(memory!=IntPtr.Zero)Marshal.FreeHGlobal(memory);if(token!=IntPtr.Zero)Native.CloseHandle(token);Native.CloseHandle(process);
  }return result;
 }
 public void Dispose() {
  stopping=true;refreshSignal.Set();if(threadId!=0)Native.PostThreadMessage(threadId,0x8001,UIntPtr.Zero,IntPtr.Zero);
  if(thread.IsAlive&&!thread.Join(2000))return;if(observer!=null&&observer.IsAlive&&!observer.Join(2000))return;ready.Dispose();refreshSignal.Dispose();
 }
}}