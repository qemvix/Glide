using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Glide {
internal sealed class TouchpadOutput : IDisposable {
 [StructLayout(LayoutKind.Sequential)] internal struct Creation {
  internal uint Type,Count,Feedback; internal IntPtr Monitor; internal uint Width,Height,Options;
 }
 [StructLayout(LayoutKind.Sequential)] internal struct Pointer {
  internal uint Type,Id,Frame,Flags;
  internal IntPtr Source,Target;
  internal Native.Point Pixel,Himetric,RawPixel,RawHimetric;
  internal uint Time,History;
  internal int Data;
  internal uint Keys;
  internal ulong Performance;
  internal uint ButtonChange;
 }
 [StructLayout(LayoutKind.Sequential)] internal struct Contact {
  internal Pointer Pointer;
  internal uint Flags,Mask;
  internal Native.Rect Area,RawArea;
  internal uint Orientation,Pressure;
 }
 [StructLayout(LayoutKind.Sequential)] internal struct Frame {
  internal uint Type;
  internal Contact Contact;
 }
 [StructLayout(LayoutKind.Sequential)] internal struct Preferences {
  internal uint Version,MaxContacts,Legacy,SystemFlags,UserFlags;
  internal int Sensitivity;
  internal uint Speed,Feedback,Force,RightWidth,RightHeight;
 }
 [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr CreateSyntheticPointerDevice2(ref Creation options);
 [DllImport("user32.dll")] private static extern void DestroySyntheticPointerDevice(IntPtr device);
 [DllImport("user32.dll",SetLastError=true)] private static extern bool InjectSyntheticPointerInput(IntPtr device,[In] Frame[] frames,uint count);
 [DllImport("user32.dll",SetLastError=true)] private static extern bool InjectTouchpadAction(IntPtr device,uint action);
 [DllImport("user32.dll",EntryPoint="SystemParametersInfoW",SetLastError=true)] private static extern bool ReadPreferences(uint action,uint size,ref Preferences preferences,uint flags);
 [DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterTouchpadCapableWindow(IntPtr window,bool enabled);
 [DllImport("user32.dll",SetLastError=true)] internal static extern bool GetPointerTouchpadInfo(uint pointer,out Contact contact);

 private IntPtr device;
 private readonly Frame[] frame=new Frame[2];
 private int x=5000,y=3000;
 private uint timestamp;
 private readonly Stopwatch clock=Stopwatch.StartNew();
 private bool reverse;
 internal bool Active { get; private set; }
 internal string Error { get; private set; }
 internal long Frames { get; private set; }
 internal static TouchpadOutput Create(out string reason) {
  TouchpadOutput output=new TouchpadOutput();
  try {
   if(Marshal.SizeOf(typeof(Pointer))!=96||Marshal.SizeOf(typeof(Frame))!=152)
    throw new NotSupportedException("The native touchpad backend needs a 64-bit process.");
   Creation create=new Creation{Type=5,Count=2,Feedback=3,Width=10000,Height=6000,Options=3};
   output.device=CreateSyntheticPointerDevice2(ref create);
   if(output.device==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"Windows rejected the virtual touchpad.");
   Preferences preferences=new Preferences{Version=1};
   if(ReadPreferences(0xAE,(uint)Marshal.SizeOf(typeof(Preferences)),ref preferences,0))
    output.reverse=(preferences.UserFlags&512)!=0;
   for(int i=0;i<2;i++) {
    output.frame[i].Type=5;
    output.frame[i].Contact.Pointer.Type=5;
    output.frame[i].Contact.Pointer.Id=(uint)i;
   }
   reason=null;return output;
  }catch(Exception ex){reason=ex.Message;output.Dispose();return null;}
 }
 private bool Send(uint flags) {
  timestamp=Math.Max(timestamp+1,1+(uint)clock.ElapsedMilliseconds);
  for(int i=0;i<2;i++) {
   Pointer p=frame[i].Contact.Pointer;
   p.Flags=flags;
   p.Himetric.X=x+(i==0?-1500:1500);
   p.Himetric.Y=y;
   p.Time=timestamp;
   frame[i].Contact.Pointer=p;
  }
  bool accepted=InjectSyntheticPointerInput(device,frame,2);
  if(!accepted) { Error="Native touchpad input rejected: Win32 "+Marshal.GetLastWin32Error();return false; }
  Frames++;return true;
 }
 internal bool Move(int horizontal,int vertical) {
  if(horizontal==0&&vertical==0)return true;if(device==IntPtr.Zero){Error="The native touchpad is unavailable.";return false;}
  if(!Active) {
   x=5000;y=3000;
   if(!Send(0x4006))return false;
   Active=true;
  }
  // HIMETRIC values describe physical finger movement, not wheel notches.
  // Cancel native inertia whenever we lift, since our easing supplies its own tail.
  int dx=horizontal*4*(reverse?1:-1),dy=vertical*4*(reverse?-1:1);
  while(dx!=0||dy!=0) {
   int sx=Math.Max(-900,Math.Min(900,dx)),sy=Math.Max(-900,Math.Min(900,dy));
   if(x+sx<2000||x+sx>8000||y+sy<600||y+sy>5400) {
    Stop();x=5000;y=3000;
    if(!Send(0x4006))return false;
    Active=true;
   }
   x+=sx;y+=sy;
   if(!Send(0x4006))return false;
   dx-=sx;dy-=sy;
  }
  return true;
 }
 internal void Stop() {
  if(device==IntPtr.Zero||!Active)return;
  // Contacts leave detection range without ever becoming mouse clicks.
  bool lifted=Send(0x4000);Active=false;if(!lifted){DestroySyntheticPointerDevice(device);device=IntPtr.Zero;return;}
  InjectTouchpadAction(device,9);
  InjectTouchpadAction(device,10);
 }
 public void Dispose() {
  if(device==IntPtr.Zero)return;
  try { Stop(); } finally {DestroySyntheticPointerDevice(device);device=IntPtr.Zero;}
 }
}
}