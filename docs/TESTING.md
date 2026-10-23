# Testing

## Core regression checks

~~~powershell
.\scripts\test.ps1
~~~

These checks do not install a hook or move the pointer. They cover:

- Native INPUT and touchpad ABI layouts.
- Migration of existing settings while preserving tuning.
- Discord and Explorer eligibility under the full-screen rule.
- Full-screen game, maximized-window, shell, and multi-monitor cases.
- Motion distance, duration, direction reversal, cancellation, and bounded bursts.
- 500 randomized overlapping motion sequences.
- Exact executable-name exclusions and settings validation.

The GitHub Actions workflow runs the build and these checks on Windows. Results are saved under bin/test-results/.

## Native receiver and full pipeline

Quit the running Glide app, then use a supported interactive Windows 11 desktop:

~~~powershell
.\scripts\test.ps1 -NativeInput
~~~

Two checks run:

1. A synthetic two-finger gesture is delivered directly to a touchpad-capable receiver.
2. A test wheel event passes through ScrollEngine and becomes a native touchpad gesture.

The receiver checks the pointer type, movement in physical device coordinates, absence of generated clicks, and completion of the gesture. The test restores the previous pointer position and foreground window.

Do not interact with the mouse during a native check. Pointer movement or a focus change can intentionally cancel the gesture. Native checks are not run in unattended CI.

## Optional legacy wheel harness

The main executable also includes a fallback-specific harness:

~~~powershell
.\bin\Glide.exe --integration-test .\bin\test-results\legacy-wheel.txt
~~~

Run it only after quitting another Glide instance. It activates a temporary receiver, tests small wheel events, exclusions, full-screen pausing, and cancellation, then restores the previous pointer/window.

This harness selects the wheel fallback intentionally. Passing it does not verify native touchpad delivery or smooth animation inside another app.

## Visual checks

For changes to the input pipeline, use a real notched mouse and check:

- Discord's message list and navigation lists.
- File Explorer folder contents and navigation pane.
- Short taps, sustained scrolling, and immediate direction changes.
- Ctrl+wheel and other modifier shortcuts.
- Pause/resume, exclusions, and full-screen game bypass.
- Moving to another pane or switching windows during a glide.
- Quitting Glide and returning to ordinary wheel behaviour.

A native receiver proves input delivery; it does not prove how every application animates the content.

## Diagnostic commands

~~~powershell
.\bin\Glide.exe --status .\bin\status.txt
.\bin\Glide.exe --render-ui .\bin\settings.png
~~~

The status command reads the running instance's local diagnostic pipe. The UI render command creates a preview without starting the global input engine.
