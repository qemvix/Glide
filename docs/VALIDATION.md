# v1.0.0 validation

The first public release preserves the native touchpad build confirmed working on 23 October 2026.

## Preserved executable

~~~text
SHA-256
B21558EF9A2B8E7D5F5796EC450F5615A5F192A6898C5F4C81F68193359EA0D8
~~~

The published Glide.exe is copied from that confirmed build. Packaging and documentation changes do not rebuild or replace the running app.

A fresh local compilation may have different bytes because of compiler-generated metadata and timestamps. Check the release checksum against the downloaded release asset.

## Completed checks

| Check | Result |
| --- | --- |
| Core regression suite | Passed |
| Native touchpad ABI layout | 96-byte pointer, 144-byte contact, 152-byte frame |
| Direct native receiver | 32 accepted frames; 32 received touchpad pointer messages |
| Native pointer type | 5: PT_TOUCHPAD |
| Direct physical travel | 480 HIMETRIC |
| Generated mouse clicks | 0 |
| Wheel-to-touchpad pipeline | One seeded wheel notch; 30 output movement frames |
| End-to-end receiver | 48 touchpad pointer messages; 480 HIMETRIC travel |
| Gesture completion | No remaining glide and no output error |
| Settings window | Rendered and visually checked |
| Discord | Smooth glide visually confirmed by the tester |
| File Explorer | Smooth glide visually confirmed by the tester |

Core checks include 500 randomized overlapping motion sequences, settings migration, frame ABI checks, full-screen rules, exclusions, direction reversal, and cancellation.

The complete legacy wheel harness was not rerun after adopting the native backend. It is not used as evidence of the native result.

## Scope

The visual checks were performed on the original supported Windows 11 x64 test PC. They do not certify every mouse, app view, Windows build, elevated process, or compatibility fallback. Full-screen game bypass is geometry-based; windowed games should use exclusions.
