using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Glide {
internal static class Native {
internal const int Wheel=0x020A,HWheel=0x020E;
internal const uint SelfTag=0x474C4944,TestTag=0x474C5453;
internal delegate IntPtr HookProc(int code,IntPtr message,IntPtr data);
[StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X,Y; }
[StructLayout(LayoutKind.Sequential)] internal struct Rect { internal int Left,Top,Right,Bottom; }
[StructLayout(LayoutKind.Sequential)] internal struct MouseHook { internal Point Point; internal uint MouseData,Flags,Time; internal UIntPtr Extra; }
[StructLayout(LayoutKind.Sequential)] internal struct MouseInput { internal int X,Y; internal uint Data,Flags,Time; internal UIntPtr Extra; }
// MOUSEINPUT is the largest member of INPUT's union on both x86 and x64.
[StructLayout(LayoutKind.Sequential)] internal struct Input { internal uint Type; internal MouseInput Mouse; }
[StructLayout(LayoutKind.Sequential)] internal struct Message { internal IntPtr Window; internal uint Id; internal UIntPtr WParam; internal IntPtr LParam; internal uint Time; internal Point Point; internal uint Private; }
[DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int id,HookProc callback,IntPtr module,uint thread);
[DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
[DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
[DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
[DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
[DllImport("user32.dll",SetLastError=true)] internal static extern bool PostThreadMessage(uint thread,uint message,UIntPtr wParam,IntPtr lParam);
[DllImport("user32.dll")] internal static extern bool PeekMessage(out Message message,IntPtr window,uint min,uint max,uint remove);
[DllImport("user32.dll")] internal static extern bool TranslateMessage(ref Message message);
[DllImport("user32.dll")] internal static extern IntPtr DispatchMessage(ref Message message);
[DllImport("user32.dll",SetLastError=true)] internal static extern uint MsgWaitForMultipleObjectsEx(uint count,IntPtr handles,uint milliseconds,uint mask,uint flags);
[DllImport("user32.dll",SetLastError=true)] internal static extern uint SendInput(uint count,Input[] inputs,int size);
[DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
[DllImport("user32.dll")] internal static extern IntPtr WindowFromPoint(Point point);
[DllImport("user32.dll")] internal static extern IntPtr GetAncestor(IntPtr window,uint flags);
[DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
[DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window,out Rect rect);
[DllImport("user32.dll")] internal static extern bool IsZoomed(IntPtr window);
[DllImport("user32.dll",SetLastError=true)] internal static extern bool SystemParametersInfo(uint action,uint param,out uint value,uint flags);
[DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetClassName(IntPtr window,StringBuilder name,int size);
[DllImport("user32.dll",EntryPoint="GetWindowLongW")] internal static extern int GetWindowLong(IntPtr window,int index);
[DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(IntPtr window,int attribute,out Rect rect,int size);
[DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
[DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
[DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window,int id);
[DllImport("winmm.dll")] internal static extern uint timeBeginPeriod(uint value);
[DllImport("winmm.dll")] internal static extern uint timeEndPeriod(uint value);
[DllImport("kernel32.dll",SetLastError=true)] internal static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
[DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
[DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder name,ref int size);
[DllImport("advapi32.dll",SetLastError=true)] internal static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
[DllImport("advapi32.dll",SetLastError=true)] internal static extern bool GetTokenInformation(IntPtr token,int kind,IntPtr data,int length,out int needed);
[DllImport("advapi32.dll")] internal static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
[DllImport("advapi32.dll")] internal static extern IntPtr GetSidSubAuthority(IntPtr sid,uint index);
[DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string text);
[DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
[DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
[DllImport("user32.dll")] internal static extern bool AttachThreadInput(uint from,uint to,bool attach);
[DllImport("user32.dll")] internal static extern bool SetCursorPos(int x,int y);
[DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
[DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
internal static bool Down(int key) { return (GetAsyncKeyState(key)&0x8000)!=0; }
internal static bool Modified() { return Down(0x10)||Down(0x11)||Down(0x12)||Down(0x5B)||Down(0x5C); }
internal static bool SendWheel(int delta,bool horizontal,uint tag) {
 Input input=new Input(); input.Mouse.Data=unchecked((uint)delta);
 input.Mouse.Flags=horizontal?0x1000u:0x0800u; input.Mouse.Extra=new UIntPtr(tag);
 return SendInput(1,new Input[]{input},Marshal.SizeOf(typeof(Input)))==1;
}
}}