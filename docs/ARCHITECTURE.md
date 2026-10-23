# Architecture

Glide is a Windows Forms tray application written in C# and built against .NET Framework. The public v1.0.0 release preserves the confirmed working native touchpad engine.

## Input pipeline

~~~text
Physical wheel
    |
Dedicated low-level mouse hook
    |
Eligibility, modifiers, exclusions, full-screen rule
    |
Finite eased motion pulses
    |
Native gesture-only touchpad
    |
Windows dispatches scrolling to the target app
~~~

The fallback backend submits smaller wheel events when native touchpad creation is unavailable. It has different compatibility characteristics and is identified in diagnostics.

## Thread responsibilities

- **UI thread:** settings, tray menu, preview, shortcut, and diagnostic window.
- **Input thread:** the low-level hook, message pump, motion state, frame scheduling, and gesture lifecycle.
- **App-check worker:** process integrity, cached process metadata, window coverage, and Windows wheel-routing state.
- **Diagnostic worker:** a local named-pipe server restricted to the current user.

The hook callback performs no file I/O or process enumeration. It wakes the input thread explicitly after adding a pulse. App-check results are published as snapshots.

## Native touchpad backend

TouchpadOutput creates a two-contact synthetic PT_TOUCHPAD device using CreateSyntheticPointerDevice2, with physical dimensions and the gesture-only option. This mode cannot turn the injected contacts into mouse clicks.

Eased wheel movement becomes relative contact movement in HIMETRIC units. Frames are sent through InjectSyntheticPointerInput. Contact release and inertia-stop/end actions finish a gesture. Device destruction releases resources on exit or failure.

The backend reads the Windows touchpad direction setting; it does not write global touchpad settings. Native struct layouts are checked by the regression suite.

## Cancellation and failure handling

Remaining motion is cancelled when the target changes, a mouse button is pressed, a modifier is held, settings change, or smoothing is paused. Gesture cleanup is queued to the input loop rather than performed inside the low-level callback.

The target process and foreground process must satisfy the integrity check. An inaccessible or higher-integrity target keeps normal scrolling. Output failure pauses smoothing instead of continuing to swallow input.

Full-screen decisions follow Windows' wheel-routing setting. Ordinary framed maximized windows and the desktop are excluded from full-screen-game detection. Discord and Explorer explicitly remain eligible.

## Motion model

The Motion class maintains bounded, overlapping ease-out pulses. Opposite-direction input clears the previous direction's remaining motion. Integer carry preserves the accumulated wheel distance in the fallback backend. Cancelling deliberately discards remaining movement.

The native touchpad backend converts those motion units to physical finger displacement. Its final visual distance depends on Windows touchpad processing and the destination app, rather than the fallback wheel-line setting.

## Local diagnostics

Diagnostics contain process names and input decisions, not window titles or application content. The named pipe is restricted to the current user. The --status command writes a snapshot to the requested file; it does not send data over the network.

## Windows references

- [CreateSyntheticPointerDevice2](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createsyntheticpointerdevice2)
- [Precision Touchpad Programming Guide](https://learn.microsoft.com/en-us/windows/win32/input-precisiontouchpad/precision-touchpad-guide)
- [InjectSyntheticPointerInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-injectsyntheticpointerinput)
- [LowLevelMouseProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelmouseproc)
