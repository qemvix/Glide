using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Glide {
internal sealed class PadReceiver:Form {
 internal int Contacts,DownClicks,MinY=int.MaxValue,MaxY=int.MinValue;
 internal uint PointerType;
 internal string LastError;
 protected override void OnPaint(PaintEventArgs e) {
  e.Graphics.Clear(Color.FromArgb(25,35,38));
  using(Font f=new Font("Segoe UI",12))e.Graphics.DrawString("Checking native two-finger touchpad input.\nThis temporary window closes automatically.",f,Brushes.White,20,25);
  base.OnPaint(e);
 }
 protected override void WndProc(ref Message m) {
  if(m.Msg==0x201)DownClicks++;
  if(m.Msg==0x245||m.Msg==0x246||m.Msg==0x247) {
   TouchpadOutput.Contact c;
   if(TouchpadOutput.GetPointerTouchpadInfo((uint)(m.WParam.ToInt64()&0xffff),out c)) {
    Contacts++;PointerType=c.Pointer.Type;
    MinY=Math.Min(MinY,c.Pointer.Himetric.Y);MaxY=Math.Max(MaxY,c.Pointer.Himetric.Y);
   }else LastError="Receive: "+Marshal.GetLastWin32Error();
   return;
  }
  base.WndProc(ref m);
 }
}
internal static class TouchpadTest {
 [STAThread] private static int Main(string[] args) {
  Native.SetProcessDPIAware();Application.EnableVisualStyles();
  string destination=args[0],report="";int result=1;
  Native.Point oldPoint;Native.GetCursorPos(out oldPoint);IntPtr oldFocus=Native.GetForegroundWindow();
  TouchpadOutput output=null;ScrollEngine engine=null;bool engineMode=args.Length>1;
  using(PadReceiver form=new PadReceiver{Text="Glide native touchpad check",ClientSize=new Size(500,240),StartPosition=FormStartPosition.CenterScreen}) {
   System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=10};
   int phase=0,steps=0,deadline=0;
   form.Shown+=delegate{timer.Start();};
   timer.Tick+=delegate {
    if(Environment.TickCount-deadline<0)return;
    try {
     if(phase==0) {
      string reason;output=TouchpadOutput.Create(out reason);
      if(output==null)throw new Exception(reason);
      report+="Pointer bytes: "+Marshal.SizeOf(typeof(TouchpadOutput.Pointer))+"; frame bytes: "+Marshal.SizeOf(typeof(TouchpadOutput.Frame))+"\r\n";
      if(!TouchpadOutput.RegisterTouchpadCapableWindow(form.Handle,true))throw new Exception("Touchpad registration: "+Marshal.GetLastWin32Error());
      uint pid;uint fg=Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid),self=Native.GetCurrentThreadId();
      bool attached=Native.AttachThreadInput(self,fg,true);
      try{form.Show();form.BringToFront();Native.SetForegroundWindow(form.Handle);}finally{if(attached)Native.AttachThreadInput(self,fg,false);}
      Point at=form.PointToScreen(new Point(250,160));Native.SetCursorPos(at.X,at.Y);if(engineMode)engine=new ScrollEngine(new Settings(),true,true);
      Native.timeBeginPeriod(1);phase=1;deadline=Environment.TickCount+200;
     }else if(phase==1) {
      if(Native.GetForegroundWindow()!=form.Handle)throw new Exception("Test interrupted: another app became active.");
      if(engineMode){if(steps==0){if(!Native.SendWheel(-120,false,Native.TestTag))throw new Exception("Test wheel injection failed.");}steps=29;}else if(!output.Move(0,-4))throw new Exception(output.Error);
      if(++steps>=30){output.Stop();phase=2;deadline=Environment.TickCount+(engineMode?700:150);}
     }else {
      if(engineMode){report+=engine.DiagnosticsText()+"\r\n";if(engine.Intercepted<1||engine.Failed||engine.Gliding)throw new Exception("The wheel-to-touchpad engine did not finish the gesture safely.");}else report+="Native touchpad frames accepted: "+output.Frames+"\r\n";
      report+="Native touchpad pointer messages received: "+form.Contacts+"\r\n";
      report+="Received pointer type: "+form.PointerType+" (5 is touchpad)\r\n";
      report+="Received physical vertical travel: "+(form.MaxY-form.MinY)+" HIMETRIC\r\n";
      report+="Mouse clicks generated: "+form.DownClicks+"\r\n";
      if(form.Contacts<2||form.PointerType!=5||form.MaxY<=form.MinY||form.DownClicks!=0)throw new Exception("The native gesture was not received as a two-finger touchpad movement. "+form.LastError);
      report+="NATIVE TOUCHPAD CHECK PASSED\r\n";result=0;form.Close();
     }
    }catch(Exception ex){report+="FAILED: "+ex.Message+"\r\n";form.Close();}
   };
   try {Application.Run(form);}
   finally {timer.Dispose();if(engine!=null)engine.Dispose();if(output!=null)output.Dispose();Native.timeEndPeriod(1);Native.SetCursorPos(oldPoint.X,oldPoint.Y);Native.SetForegroundWindow(oldFocus);File.WriteAllText(destination,report);}
  }
  return result;
 }
}
}