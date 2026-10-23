using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace Glide {
internal static class Diagnostics {
 private static string PipeName { get { return "GlideScroll.Status."+Process.GetCurrentProcess().SessionId; } }
 internal static void Start(ScrollEngine engine) {
  Thread worker=new Thread(delegate() {
   while(true) {
    try {
     PipeSecurity security=new PipeSecurity();
     security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,PipeAccessRights.FullControl,AccessControlType.Allow));
     using(NamedPipeServerStream pipe=new NamedPipeServerStream(PipeName,PipeDirection.Out,1,PipeTransmissionMode.Byte,PipeOptions.None,4096,4096,security)) {
      pipe.WaitForConnection();
      using(StreamWriter writer=new StreamWriter(pipe))writer.Write(engine.DiagnosticsText());
     }
    } catch { Thread.Sleep(300); }
   }
  }) {IsBackground=true,Name="Glide local diagnostics"};
  worker.Start();
 }
 internal static int Fetch(string destination) {
  try {
   using(NamedPipeClientStream pipe=new NamedPipeClientStream(".",PipeName,PipeDirection.In)) {
    pipe.Connect(1500);
    using(StreamReader reader=new StreamReader(pipe))File.WriteAllText(destination,reader.ReadToEnd());
   }
   return 0;
  }catch(Exception ex){File.WriteAllText(destination,"Could not read Glide diagnostics: "+ex.Message);return 1;}
 }
 internal static void Show(ScrollEngine engine) {
  if(engine==null)return;
  using(Form form=new Form{Text="Glide - input diagnostics",ClientSize=new Size(640,332),StartPosition=FormStartPosition.CenterScreen,BackColor=Theme.Bg,ForeColor=Theme.Text,MinimizeBox=false,MaximizeBox=false,FormBorderStyle=FormBorderStyle.FixedDialog}) {
   TextBox text=new TextBox{Location=new Point(16,16),Size=new Size(608,255),Multiline=true,ReadOnly=true,WordWrap=false,ScrollBars=ScrollBars.Both,BackColor=Theme.Card,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Consolas",10)};
   Button copy=new Button{Text="Copy diagnostics",Location=new Point(16,286),Size=new Size(150,30),FlatStyle=FlatStyle.Flat,BackColor=Theme.Accent,ForeColor=Theme.Bg};
   Button close=new Button{Text="Close",Location=new Point(524,286),Size=new Size(100,30),DialogResult=DialogResult.OK};
   Label note=new Label{Text="Scroll in another app, then check the last decision here.",Location=new Point(180,294),Size=new Size(332,24),ForeColor=Theme.Muted,Font=Theme.Font(8,FontStyle.Regular)};
   form.Controls.Add(text);form.Controls.Add(copy);form.Controls.Add(close);form.Controls.Add(note);form.AcceptButton=close;
   copy.Click+=delegate{try{Clipboard.SetText(engine.DiagnosticsText());}catch{}};
   using(System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=300}) {
    timer.Tick+=delegate{string value=engine.DiagnosticsText();if(text.Text!=value)text.Text=value;};
    text.Text=engine.DiagnosticsText();timer.Start();form.ShowDialog();timer.Stop();
   }
  }
 }
}
}