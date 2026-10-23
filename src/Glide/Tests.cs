using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Glide {
internal static class Tests {
 private static readonly List<string> log=new List<string>();
 private static void Check(bool condition,string name){log.Add((condition?"PASS ":"FAIL ")+name);if(!condition)throw new InvalidOperationException(name);}
 internal static int Run(string output) {
  try {
   Check(Marshal.SizeOf(typeof(Native.Input))==(IntPtr.Size==8?40:28),"Native INPUT layout matches Windows architecture");
   if(IntPtr.Size==8)Check(Marshal.SizeOf(typeof(TouchpadOutput.Pointer))==96&&Marshal.SizeOf(typeof(TouchpadOutput.Contact))==144&&Marshal.SizeOf(typeof(TouchpadOutput.Frame))==152,"Native touchpad structures match the Windows ABI");
   using(StringReader oldSettings=new StringReader("<Settings><GlideMilliseconds>700</GlideMilliseconds><SpeedPercent>103</SpeedPercent></Settings>")) {
    Settings migrated=(Settings)new System.Xml.Serialization.XmlSerializer(typeof(Settings)).Deserialize(oldSettings);
    Check(migrated.NativeTouchpad&&migrated.GlideMilliseconds==700&&migrated.SpeedPercent==103,"Existing settings enable the new native backend and preserve user tuning");
   }
   Check(!ScrollEngine.ShouldPauseFullscreen("Discord",true),"Discord remains eligible even when Windows reports full-screen coverage");
   Check(!ScrollEngine.ShouldPauseFullscreen("explorer",true),"Explorer remains eligible even when Windows reports full-screen coverage");
   Check(ScrollEngine.ShouldPauseFullscreen("javaw",true),"Full-screen game pausing remains enabled");
   Rectangle screen=new Rectangle(0,0,1920,1080);
   Check(!ScrollEngine.FullscreenCandidate(screen,screen,true,0x00C40000,"Chrome_WidgetWin_1"),"Maximized framed apps are not mistaken for full-screen games");
   Check(ScrollEngine.FullscreenCandidate(screen,screen,false,0x80000000,"Game"),"Borderless full-screen games pause");
   Check(ScrollEngine.FullscreenCandidate(screen,screen,true,0x80000000,"Game"),"Maximized borderless games still pause");
   Check(!ScrollEngine.FullscreenCandidate(screen,screen,false,0x80000000,"Progman"),"The desktop is not a full-screen game");
   Check(!ScrollEngine.FullscreenCandidate(new Rectangle(0,0,1920,1040),screen,false,0,"Window"),"A normal working-area window is not full-screen");
   Rectangle leftScreen=new Rectangle(-1920,-100,1920,1080);
   Check(ScrollEngine.FullscreenCandidate(leftScreen,leftScreen,false,0x80000000,"Game"),"Full-screen detection handles monitors at negative coordinates");
   foreach(int sign in new int[]{-1,1})foreach(int duration in new int[]{100,320,700})foreach(int interval in new int[]{4,8,16,37}) {
    Motion motion=new Motion();motion.Add(sign*120,0,duration);int total=0,events=0;
    for(int t=0;t<=duration+interval;t+=interval){int v=motion.Step(t);total+=v;if(v!=0)events++;CheckValue(v*sign>=0,"Motion changed direction unexpectedly");}
    CheckValue(total==sign*120&&!motion.Active&&events>1,"Detent distance/duration not conserved");
   }
   Check(true,"24 combinations preserve a full notch and finish within the duration");
   Random random=new Random(1729);
   for(int iteration=0;iteration<500;iteration++) {
    Motion m=new Motion();double expected=0;int emitted=0;int duration=random.Next(100,701),sign=random.Next(2)*2-1;
    for(int t=0;t<1500;t+=8) {
     if(random.Next(4)==0){double amount=sign*random.Next(30,301);expected+=amount;m.Add(amount,t,duration);}
     emitted+=m.Step(t);
    }
    for(int t=1500;t<2300;t+=8)emitted+=m.Step(t);
    CheckValue(emitted==(int)expected&&!m.Active,"Overlapping impulse distance not conserved");
   }
   Check(true,"500 randomized bursts preserve scroll distance without accumulating a tail");
   Motion reverse=new Motion();reverse.Add(-120,0,320);reverse.Step(40);reverse.Add(120,45,320);
   Check(reverse.Step(53)>0,"Direction reversal responds immediately");
   reverse.Clear();Check(!reverse.Active&&reverse.Step(1000)==0,"Cancel removes queued motion");
   Motion longSleep=new Motion();longSleep.Add(120,0,320);Check(longSleep.Step(100000)==120&&!longSleep.Active,"Long timing gaps remain bounded");
   Motion overload=new Motion();for(int i=0;i<3000;i++)overload.Add(120,0,320);
   int sum=0;for(int i=0;i<100;i++)sum+=overload.Step(1000);
   Check(sum==360000&&!overload.Active,"Burst limit preserves distance and bounds memory");
   Check(ScrollEngine.Excluded("Discord","game.exe, DISCORD.EXE; blender.exe"),"Exclusions match case-insensitive executable names");
   Check(!ScrollEngine.Excluded("discord","mydiscord.exe"),"Exclusions require an exact process match");
   Settings s=new Settings{GlideMilliseconds=-4,SpeedPercent=900,ExcludedApps=null};s.Validate();
   Check(s.GlideMilliseconds==100&&s.SpeedPercent==250&&s.ExcludedApps=="","Malformed setting values are clamped");
   Settings clone=s.Copy();clone.Enabled=false;Check(s.Enabled,"Settings snapshots are independent");
   log.Add("ALL CORE TESTS PASSED");File.WriteAllLines(output,log.ToArray());return 0;
  }catch(Exception ex){log.Add(ex.ToString());File.WriteAllLines(output,log.ToArray());return 1;}
 }
 private static void CheckValue(bool value,string error){if(!value)throw new Exception(error);}
 internal sealed class Sink:Control {
  internal readonly List<int> V=new List<int>(),H=new List<int>();
  internal Sink(){BackColor=Color.FromArgb(35,50,46);}
  protected override void WndProc(ref Message m){
   if(m.Msg==Native.Wheel||m.Msg==Native.HWheel){
    int delta=unchecked((short)((m.WParam.ToInt64()>>16)&0xffff));(m.Msg==Native.Wheel?V:H).Add(delta);return;
   }base.WndProc(ref m);
  }
  internal void Clear(){V.Clear();H.Clear();}
  internal static int Sum(List<int> values){int n=0;foreach(int v in values)n+=v;return n;}
 }
 internal sealed class TestForm:Form { internal Sink Target; protected override void WndProc(ref Message m){if(Target!=null&&(m.Msg==Native.Wheel||m.Msg==Native.HWheel)){int d=unchecked((short)((m.WParam.ToInt64()>>16)&0xffff));(m.Msg==Native.Wheel?Target.V:Target.H).Add(d);return;}base.WndProc(ref m);} }
 internal static int Integration(string output) {
  Native.Point oldPoint;Native.GetCursorPos(out oldPoint);IntPtr oldFocus=Native.GetForegroundWindow();int result=1;
  Settings cfg=new Settings();ScrollEngine engine=null;
  using(TestForm form=new TestForm{Text="Glide input checks",Size=new Size(440,300),StartPosition=FormStartPosition.CenterScreen}) {
   Sink sink=new Sink{Dock=DockStyle.Fill};form.Controls.Add(sink);form.Target=sink;
   Label text=new Label{Text="Checking smooth scrolling in this test window.\nThis closes automatically.",AutoSize=false,Size=new Size(400,60),Location=new Point(12,12),ForeColor=Color.White,BackColor=sink.BackColor};sink.Controls.Add(text);
   System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=100};int stage=0,deadline=0,sentBefore=0;
   Rectangle oldBounds=Rectangle.Empty;
   Action target=delegate{Point p=sink.PointToScreen(new Point(200,160)); uint pid; uint fgThread=Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid); uint selfThread=Native.GetCurrentThreadId(); bool attached=Native.AttachThreadInput(selfThread,fgThread,true); try{form.Show();form.BringToFront();Native.SetForegroundWindow(form.Handle);sink.Focus();}finally{if(attached)Native.AttachThreadInput(selfThread,fgThread,false);} Native.SetCursorPos(p.X,p.Y);};
   Action<int> later=delegate(int ms){deadline=Environment.TickCount+ms;};
   timer.Tick+=delegate {

    if(Environment.TickCount-deadline<0)return;
    try {
     switch(stage++) {
      case 0:target();engine=new ScrollEngine(cfg,true);later(350);break;
      case 1:
       log.Add("Preflight: " + engine.Status + "; error=" + engine.Error + "; visible=" + form.Visible + "; foreground=" + Native.GetForegroundWindow() + "; test=" + form.Handle); Check(engine.Status=="Smooth scrolling is on","Input hook and target permission preflight start");
       sink.Clear();Check(Native.SendWheel(-120,false,Native.TestTag),"Send a test detent");later(550);break;
      case 2:
       log.Add("Output: decision="+engine.LastDecision+"; status="+engine.Status+"; error="+engine.Error+"; intercepted="+engine.Intercepted+"; emitted="+engine.Emitted+"; count="+sink.V.Count+"; sum="+Sink.Sum(sink.V)+"; packets="+String.Join(",",sink.V)); Check(Sink.Sum(sink.V)==-120&&sink.V.Count>1,"Real hook splits one detent into fine deltas with exact distance");
       Check(engine.Intercepted==1&&engine.Emitted==sink.V.Count,"Injected output is never captured recursively");
       cfg.SpeedPercent=200;engine.Update(cfg);sink.Clear();later(180);break;
      case 3:Native.SendWheel(-120,false,Native.TestTag);later(550);break;
      case 4:
       log.Add("Speed: decision="+engine.LastDecision+"; status="+engine.Status+"; fg="+Native.GetForegroundWindow()+"; target="+form.Handle+"; intercepted="+engine.Intercepted+"; emitted="+engine.Emitted+"; packets="+String.Join(",",sink.V)); Check(Sink.Sum(sink.V)==-240,"2x speed changes the emitted distance correctly");
       cfg.SpeedPercent=100;engine.Update(cfg);sink.Clear();later(180);break;
      case 5:Native.SendWheel(120,true,Native.TestTag);later(550);break;
      case 6:
       log.Add("Horizontal: decision="+engine.LastDecision+"; status="+engine.Status+"; intercepted="+engine.Intercepted+"; emitted="+engine.Emitted+"; packets="+String.Join(",",sink.H)); Check(Sink.Sum(sink.H)==120&&sink.H.Count>1,"Horizontal wheel smoothing works");
       sink.Clear();Native.SendWheel(-1,false,Native.TestTag);later(180);break;
      case 7:
       Check(sink.V.Count==1&&sink.V[0]==-1,"Precision wheel deltas pass through unchanged");
       sink.Clear();Native.SendWheel(-120,false,0x12345678);later(180);break;
      case 8:
       Check(sink.V.Count==1&&sink.V[0]==-120,"Other injected inputs pass through unchanged");
       cfg.Enabled=false;engine.Update(cfg);sink.Clear();later(180);break;
      case 9:Native.SendWheel(-120,false,Native.TestTag);later(180);break;
      case 10:
       Check(sink.V.Count==1&&sink.V[0]==-120,"Pause restores the original wheel event");
       cfg.Enabled=true;cfg.ExcludedApps=Process.GetCurrentProcess().ProcessName+".exe";engine.Update(cfg);sink.Clear();later(180);break;
      case 11:Native.SendWheel(-120,false,Native.TestTag);later(180);break;
      case 12:
       Check(sink.V.Count==1&&sink.V[0]==-120,"Excluded apps retain normal scrolling");
       cfg.ExcludedApps="";engine.Update(cfg);oldBounds=form.Bounds;form.FormBorderStyle=FormBorderStyle.None;form.Bounds=Screen.FromControl(form).Bounds;target();sink.Clear();later(250);break;
      case 13:
       Check(ScrollEngine.IsFullscreen(form.Handle)&&engine.Status=="Paused - full-screen app","Full-screen detection pauses smoothing");
       Native.SendWheel(-120,false,Native.TestTag);later(180);break;
      case 14:
       Check(sink.V.Count==1&&sink.V[0]==-120,"Full-screen apps receive their original wheel input");
       form.FormBorderStyle=FormBorderStyle.Sizable;form.Bounds=oldBounds;target();sink.Clear();later(250);break;
      case 15:Native.SendWheel(-120,false,Native.TestTag);later(45);break;
      case 16:
       bool moved=Native.SetCursorPos(form.Left-12,form.Top-12);Native.Point at;Native.GetCursorPos(out at);log.Add("Leave: moved="+moved+"; cursor="+at.X+","+at.Y+"; hit="+Native.WindowFromPoint(at)+"; sink="+sink.Handle+"; bounds="+form.Bounds);later(100);break;
      case 17:
       Check(!engine.Gliding,"Moving off the target cancels remaining motion");sentBefore=sink.V.Count;later(200);break;
      case 18:
       log.Add("Cancel: before="+sentBefore+"; after="+engine.Emitted+"; status="+engine.Status); Check(sink.V.Count==sentBefore,"Cancelled motion does not reach the previous target");
       target();sink.Clear();later(250);break;
      case 19:Native.SendWheel(-120,false,0);later(550);break;
      case 20:Check(Sink.Sum(sink.V)==-120&&sink.V.Count>1,"Untagged mouse-software wheel events are smoothed");form.WindowState=FormWindowState.Maximized;later(250);break;
      case 21:Check(!ScrollEngine.IsFullscreen(form.Handle)&&engine.Status=="Smooth scrolling is on","Ordinary maximized windows remain eligible");engine.Dispose();engine=null;log.Add("ALL WINDOWS INPUT TESTS PASSED");result=0;form.Close();break;
     }
    }catch(Exception ex){log.Add("FAIL "+ex);form.Close();}
   };
   form.Shown+=delegate{timer.Start();};
   try{Application.Run(form);}finally{timer.Dispose();if(engine!=null)engine.Dispose();Native.SetCursorPos(oldPoint.X,oldPoint.Y);Native.SetForegroundWindow(oldFocus);File.WriteAllLines(output,log.ToArray());}
  }return result;
 }
}
}