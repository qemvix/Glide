using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
namespace Glide {
internal static class Theme {
 internal static readonly Color Bg=Color.FromArgb(17,21,25),Card=Color.FromArgb(27,33,38),
  Text=Color.FromArgb(239,244,242),Muted=Color.FromArgb(157,173,177),Accent=Color.FromArgb(137,232,194),Line=Color.FromArgb(49,61,67);
 internal static Font Font(float size,FontStyle style){return new Font("Segoe UI",size,style,GraphicsUnit.Point);}
 internal static GraphicsPath Round(RectangleF r,float radius) {
  float d=radius*2;GraphicsPath p=new GraphicsPath();
  p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
  p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
 }
}
internal sealed class Switch:CheckBox {
 internal Switch(){Appearance=Appearance.Button;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Size=new Size(50,28);Cursor=Cursors.Hand;Text="";DoubleBuffered=true;AccessibleName="Smooth scrolling";}
 protected override void OnPaint(PaintEventArgs e) {
  e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  using(GraphicsPath p=Theme.Round(new RectangleF(0,1,48,26),13))
  using(Brush b=new SolidBrush(Checked?Theme.Accent:Theme.Line))e.Graphics.FillPath(b,p);
  using(Brush b=new SolidBrush(Checked?Theme.Bg:Theme.Text))e.Graphics.FillEllipse(b,Checked?26:4,5,18,18);
  if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(0,0,49,27),Theme.Text,Theme.Bg);
 }
}
internal sealed class Slider:Control {
 internal int Minimum=0,Maximum=100;
 private int value;
 internal int Value {get{return value;}set{int v=Math.Max(Minimum,Math.Min(Maximum,value));if(this.value==v)return;this.value=v;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
 internal event EventHandler ValueChanged;
 internal Slider(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);Size=new Size(340,30);Cursor=Cursors.Hand;TabStop=true;}
 protected override void OnPaint(PaintEventArgs e) {
  e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float fraction=(Value-Minimum)/(float)(Maximum-Minimum);
  int x=12+(int)(fraction*(Width-24)),y=Height/2;
  using(Pen p=new Pen(Theme.Line,4)){p.StartCap=p.EndCap=LineCap.Round;e.Graphics.DrawLine(p,12,y,Width-12,y);}
  using(Pen p=new Pen(Theme.Accent,4)){p.StartCap=p.EndCap=LineCap.Round;e.Graphics.DrawLine(p,12,y,x,y);}
  using(Brush b=new SolidBrush(Theme.Accent))e.Graphics.FillEllipse(b,x-7,y-7,14,14);
  if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle,Theme.Muted,BackColor);
 }
 private void Pick(int x){Value=Minimum+(int)Math.Round(Math.Max(0,Math.Min(1,(x-12)/(double)(Width-24)))*(Maximum-Minimum));}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;Pick(e.X);}}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)Pick(e.X);}
 protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
 protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||base.IsInputKey(key);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left)Value-=5;if(e.KeyCode==Keys.Right)Value+=5;base.OnKeyDown(e);}
}
internal sealed class Card:Panel {
 internal Card(){DoubleBuffered=true;BackColor=Theme.Bg;}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath p=Theme.Round(new RectangleF(0,0,Width-1,Height-1),14))using(Brush b=new SolidBrush(Theme.Card))e.Graphics.FillPath(b,p);base.OnPaint(e);}
}
internal sealed class Preview:Control {
 private readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
 private readonly Stopwatch clock=Stopwatch.StartNew();private readonly Motion motion=new Motion();
 private double offset;internal int Duration=320,Speed=100;
 internal Preview() {
  SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Theme.Card;Cursor=Cursors.Hand;TabStop=true;
  timer.Interval=8;timer.Tick+=delegate{offset-=motion.Step(clock.Elapsed.TotalMilliseconds)*0.6;offset=Math.Max(0,Math.Min(1180,offset));Invalidate();if(!motion.Active)timer.Stop();};
  AccessibleName="Scrolling preview. Scroll here to try the glide.";
 }
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);}
 protected override void OnMouseWheel(MouseEventArgs e){motion.Add(e.Delta*Speed/100.0,clock.Elapsed.TotalMilliseconds,Duration);timer.Start();base.OnMouseWheel(e);}
 protected override void OnMouseDown(MouseEventArgs e){Focus();base.OnMouseDown(e);}
 protected override void OnPaint(PaintEventArgs e) {
  e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  for(int i=0;i<22;i++){
   float y=16+i*63-(float)offset;if(y < -60 || y>Height)continue;
   using(Brush b=new SolidBrush(i%3==0?Color.FromArgb(51,78,71):Color.FromArgb(42,52,59)))
   using(GraphicsPath p=Theme.Round(new RectangleF(14,y,30,30),7))e.Graphics.FillPath(b,p);
   using(Pen p=new Pen(i%3==0?Theme.Accent:Theme.Muted,2))e.Graphics.DrawLines(p,new PointF[]{new PointF(23,y+11),new PointF(23,y+20),new PointF(35,y+20)});
   TextRenderer.DrawText(e.Graphics,new string[]{"The good stuff","Keep it moving","A little more flow","In your own rhythm","Easy does it"}[i%5],Font,new Point(56,(int)y-1),Theme.Text);
   TextRenderer.DrawText(e.Graphics,"Folder  /  "+(i+1).ToString("00"),Font,new Point(56,(int)y+19),Theme.Muted);
  }
  int thumbY=10+(int)(offset/1180*(Height-55));
  using(Pen p=new Pen(Theme.Line,3)){p.StartCap=p.EndCap=LineCap.Round;e.Graphics.DrawLine(p,Width-9,thumbY,Width-9,thumbY+35);}
 }
 protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
internal sealed class MainForm:Form {
 private readonly bool renderOnly;private readonly Settings settings;private ScrollEngine engine;
 private NotifyIcon tray;private Icon appIcon;private Switch enabled;
 private Label statusLabel,glideValue,speedValue,saveLabel;private Slider glide,speed;
 private CheckBox fullscreen,startup;private TextBox exclusions;private Preview preview;
 private readonly System.Windows.Forms.Timer saveTimer=new System.Windows.Forms.Timer(),statusTimer=new System.Windows.Forms.Timer();
 private bool exiting,loading=true;private ToolStripMenuItem toggleItem;private bool hotkey;
 internal MainForm(Settings initial,bool render) {
  settings=initial;renderOnly=render;
  Text="Glide";ClientSize=new Size(720,690);MinimumSize=MaximumSize=Size;
  FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;
  StartPosition=FormStartPosition.CenterScreen;BackColor=Theme.Bg;ForeColor=Theme.Text;
  Font=Theme.Font(9,FontStyle.Regular);AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);
  appIcon=LoadIcon();Icon=appIcon;
  Build();loading=false;
  saveTimer.Interval=450;saveTimer.Tick+=delegate{saveTimer.Stop();Save();};
  statusTimer.Interval=350;statusTimer.Tick+=delegate{UpdateStatus();};
  Shown+=delegate {
   int dark=1;try{Native.DwmSetWindowAttribute(Handle,20,ref dark,4);}catch{}
   if(Height>Screen.FromControl(this).WorkingArea.Height){MaximumSize=Size.Empty;MinimumSize=Size.Empty;AutoScroll=true;AutoScrollMinSize=new Size(ClientSize.Width,ClientSize.Height);Height=Screen.FromControl(this).WorkingArea.Height;Top=Screen.FromControl(this).WorkingArea.Top;} if(!renderOnly)StartEngine();
  };
 }
 private Icon LoadIcon() {
  using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Glide.ico")) {
   if(stream!=null)return new Icon(stream);
  }return (Icon)SystemIcons.Application.Clone();
 }
 private Label Label(Control parent,string text,int x,int y,int w,int h,float size,Color color,FontStyle style) {
  Label l=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=Theme.Font(size,style),ForeColor=color,BackColor=Color.Transparent};
  parent.Controls.Add(l);return l;
 }
 private Button Button(Control parent,string text,int x,int y,int w,int h,bool primary) {
  Button b=new Button{Text=text,Location=new Point(x,y),Size=new Size(w,h),FlatStyle=FlatStyle.Flat,
   BackColor=primary?Theme.Accent:Theme.Card,ForeColor=primary?Theme.Bg:Theme.Text,Cursor=Cursors.Hand};
  b.FlatAppearance.BorderSize=primary?0:1;b.FlatAppearance.BorderColor=Theme.Line;parent.Controls.Add(b);return b;
 }
 private CheckBox Check(Control parent,string text,int x,int y,int w,bool value) {
  CheckBox c=new CheckBox{Text=text,Location=new Point(x,y),Size=new Size(w,27),Checked=value,FlatStyle=FlatStyle.Flat,ForeColor=Theme.Text,BackColor=Theme.Card};
  parent.Controls.Add(c);return c;
 }
 private void Build() {
  Label(this,"~",29,20,42,60,32,Theme.Accent,FontStyle.Bold);
  Label(this,"Glide",78,25,200,48,29,Theme.Text,FontStyle.Bold);
  Label(this,"A softer scroll. A calmer desktop.",30,82,540,27,11,Theme.Muted,FontStyle.Regular);
  enabled=new Switch{Location=new Point(637,39),Checked=settings.Enabled};Controls.Add(enabled);
  enabled.CheckedChanged+=delegate{settings.Enabled=enabled.Checked;Changed();};

  Card tuning=new Card{Location=new Point(28,124),Size=new Size(418,244)};Controls.Add(tuning);
  Label(tuning,"FIND YOUR FLOW",20,17,270,24,9,Theme.Accent,FontStyle.Bold);
  Label(tuning,"Glide",20,58,190,28,12,Theme.Text,FontStyle.Bold);
  glideValue=Label(tuning,settings.GlideMilliseconds+" ms",297,60,100,24,10,Theme.Muted,FontStyle.Regular);glideValue.TextAlign=ContentAlignment.TopRight;
  glide=new Slider{Location=new Point(12,88),Size=new Size(393,30),Minimum=100,Maximum=700,Value=settings.GlideMilliseconds,BackColor=Theme.Card,AccessibleName="Glide duration in milliseconds"};tuning.Controls.Add(glide);
  Label(tuning,"Quick & crisp",20,120,180,20,8,Theme.Muted,FontStyle.Regular);
  Label end=Label(tuning,"Long & floaty",260,120,137,20,8,Theme.Muted,FontStyle.Regular);end.TextAlign=ContentAlignment.TopRight;
  Label(tuning,"Scroll speed",20,157,210,27,12,Theme.Text,FontStyle.Bold);
  speedValue=Label(tuning,(settings.SpeedPercent/100.0).ToString("0.00")+"x",292,160,105,24,10,Theme.Muted,FontStyle.Regular);speedValue.TextAlign=ContentAlignment.TopRight;
  speed=new Slider{Location=new Point(12,191),Size=new Size(393,30),Minimum=25,Maximum=250,Value=settings.SpeedPercent,BackColor=Theme.Card,AccessibleName="Scroll speed percent"};tuning.Controls.Add(speed);
  Card demo=new Card{Location=new Point(460,124),Size=new Size(232,244)};Controls.Add(demo);
  Label(demo,"GIVE IT A SPIN",17,17,200,22,9,Theme.Accent,FontStyle.Bold);
  Label(demo,"Scroll over the list below",17,42,200,23,8,Theme.Muted,FontStyle.Regular);
  preview=new Preview{Location=new Point(7,74),Size=new Size(218,155),Font=Theme.Font(8,FontStyle.Regular),Duration=settings.GlideMilliseconds,Speed=settings.SpeedPercent};demo.Controls.Add(preview);
  glide.ValueChanged+=delegate{settings.GlideMilliseconds=glide.Value;glideValue.Text=glide.Value+" ms";preview.Duration=glide.Value;Changed();};
  speed.ValueChanged+=delegate{settings.SpeedPercent=speed.Value;speedValue.Text=(speed.Value/100.0).ToString("0.00")+"x";preview.Speed=speed.Value;Changed();};

  Card behavior=new Card{Location=new Point(28,382),Size=new Size(664,178)};Controls.Add(behavior);
  Label(behavior,"MADE FOR YOUR DESKTOP",20,16,440,25,9,Theme.Accent,FontStyle.Bold);
  fullscreen=Check(behavior,"Pause full-screen apps (except Explorer / Discord)",20,48,355,settings.PauseFullscreen);
  startup=Check(behavior,"Start with Windows",404,48,230,IsStartup());
  fullscreen.CheckedChanged+=delegate{settings.PauseFullscreen=fullscreen.Checked;Changed();};
  startup.CheckedChanged+=delegate{if(!loading)SetStartup(startup.Checked);};
  Label(behavior,"Skip these apps",20,88,200,22,9,Theme.Text,FontStyle.Regular);
  Label(behavior,"Separate names with commas, e.g. game.exe, blender.exe",20,112,590,20,8,Theme.Muted,FontStyle.Regular);
  exclusions=new TextBox{Location=new Point(20,138),Size=new Size(621,24),Text=settings.ExcludedApps,BackColor=Theme.Bg,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle,AccessibleName="Apps to exclude"};
  behavior.Controls.Add(exclusions);exclusions.TextChanged+=delegate{settings.ExcludedApps=exclusions.Text;Changed();};
  Label(this,"Native touchpad gestures power the glide on supported Windows builds.\nSome app views may still snap to rows.",30,575,660,40,9,Theme.Muted,FontStyle.Regular);
  statusLabel=Label(this,"Ready for smoother scrolling",30,632,390,22,9,Theme.Accent,FontStyle.Regular);
  saveLabel=Label(this,"Ctrl + Alt + S to pause",30,654,390,20,8,Theme.Muted,FontStyle.Regular);
  Button reset=Button(this,"Reset",445,630,82,35,false);reset.Click+=delegate{Reset();};
  Button hide=Button(this,"Keep in tray",541,630,149,35,true);hide.Click+=delegate{Hide();};
 }
 private void StartEngine() {
  try {engine=new ScrollEngine(settings,false);Diagnostics.Start(engine);}
  catch(Exception ex){statusLabel.Text="Could not start scrolling";MessageBox.Show(this,ex.Message,"Glide",MessageBoxButtons.OK,MessageBoxIcon.Error);}
  ContextMenuStrip menu=new ContextMenuStrip{BackColor=Theme.Card,ForeColor=Theme.Text};
  menu.Items.Add("Open Glide",null,delegate{ShowSettings();});
  toggleItem=new ToolStripMenuItem("Pause scrolling",null,delegate{enabled.Checked=!enabled.Checked;});menu.Items.Add(toggleItem);
  menu.Items.Add("Input diagnostics",null,delegate{Diagnostics.Show(engine);});menu.Items.Add(new ToolStripSeparator());
  menu.Items.Add("Quit Glide",null,delegate{exiting=true;Close();});
  tray=new NotifyIcon{Icon=appIcon,Text="Glide - smooth scrolling",ContextMenuStrip=menu,Visible=true};
  tray.DoubleClick+=delegate{ShowSettings();};
  hotkey=Native.RegisterHotKey(Handle,1,0x4003,(uint)Keys.S);
  if(!hotkey)saveLabel.Text="Shortcut in use - pause from the tray menu";
  statusTimer.Start();UpdateStatus();
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"--tray")>=0)Hide();
 }
 private void ShowSettings(){Show();WindowState=FormWindowState.Normal;Activate();}
 private void UpdateStatus() {
  if(engine!=null)statusLabel.Text=engine.Status;
  if(toggleItem!=null)toggleItem.Text=settings.Enabled?"Pause scrolling":"Resume scrolling";
  if(tray!=null)tray.Text=settings.Enabled?"Glide - smooth scrolling":"Glide - paused";
  if(engine!=null&&engine.Failed)statusLabel.ForeColor=Color.FromArgb(255,182,135);
 }
 private void Changed() {
  if(loading)return;if(engine!=null)engine.Update(settings);
  saveTimer.Stop();saveTimer.Start();UpdateStatus();
 }
 private void Save() {
  if(renderOnly)return;
  try{SettingsStore.Save(settings);}
  catch(Exception){saveLabel.Text="Could not save settings; changes work for this session.";}
 }
 private void Reset() {
  glide.Value=320;speed.Value=100;fullscreen.Checked=true;exclusions.Text="";enabled.Checked=true;
 }
 private static bool IsStartup() {
  try {using(RegistryKey key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
   return key!=null&&key.GetValue("GlideScroll")!=null;
  }catch{return false;}
 }
 private void SetStartup(bool value) {
  if(renderOnly)return;
  try {
   using(RegistryKey key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) {
    if(value)key.SetValue("GlideScroll","\""+Application.ExecutablePath+"\" --tray");else key.DeleteValue("GlideScroll",false);
   }
  }catch(Exception ex){loading=true;startup.Checked=IsStartup();loading=false;MessageBox.Show(this,ex.Message,"Could not change startup",MessageBoxButtons.OK,MessageBoxIcon.Information);}
 }
 protected override void WndProc(ref System.Windows.Forms.Message m) {
  if(m.Msg==0x0312&&m.WParam.ToInt32()==1)enabled.Checked=!enabled.Checked;
  if((uint)m.Msg==Program.ShowMessage)ShowSettings();base.WndProc(ref m);
 }
 protected override void OnFormClosing(FormClosingEventArgs e) {
  if(!exiting&&!renderOnly&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();return;}
  Save();if(hotkey)Native.UnregisterHotKey(Handle,1);
  statusTimer.Stop();saveTimer.Stop();if(tray!=null){tray.Visible=false;tray.Dispose();}
  if(engine!=null)engine.Dispose();base.OnFormClosing(e);
 }
 protected override void Dispose(bool disposing) {
  if(disposing){statusTimer.Dispose();saveTimer.Dispose();if(appIcon!=null)appIcon.Dispose();}
  base.Dispose(disposing);
 }
}
internal static class Program {
 internal static readonly uint ShowMessage=Native.RegisterWindowMessage("GlideScroll.Show.1");
 [STAThread] private static int Main(string[] args) {
  Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try {
   if(args.Length>1&&args[0]=="--status")return Diagnostics.Fetch(args[1]);
   if(args.Length>0&&args[0]=="--self-test")return Tests.Run(args.Length>1?args[1]:"self-test.txt");
   if(args.Length>0&&args[0]=="--integration-test")return Tests.Integration(args.Length>1?args[1]:"integration-test.txt");
   if(args.Length>1&&args[0]=="--render-ui") {
    using(MainForm view=new MainForm(new Settings(),true)) {
     view.Show();Application.DoEvents();using(Bitmap bitmap=new Bitmap(view.Width,view.Height)) {
      view.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);
     }view.Close();
    }return 0;
   }
   bool created;
   using(Mutex mutex=new Mutex(true,@"Local\GlideScroll.SingleInstance.1",out created)) {
    if(!created){Native.PostMessage(new IntPtr(0xffff),ShowMessage,IntPtr.Zero,IntPtr.Zero);return 0;}
    Application.Run(new MainForm(SettingsStore.Load(),false));
   }return 0;
  }catch(Exception ex){MessageBox.Show(ex.Message,"Glide",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
}
}