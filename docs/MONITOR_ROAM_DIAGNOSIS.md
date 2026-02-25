# Monitor Roam Diagnosis

## Observed Runtime Symptoms (Release Build)

- App launches correctly bottom-right.
- On focus loss, window transitions to monitor-sized mode and relocates to top-left.
- Environment area becomes a black rectangle.
- Pet rendering becomes partially or fully invisible outside that region.
- Input/focus behavior becomes inconsistent until alt-tab recovery.

## Root Cause Assessment

The failure pattern indicates compositor incompatibility with the current combination of:

- borderless + always-on-top + transparent window,
- monitor-sized unfocused layout,
- GL compatibility renderer.

Window size/position changes succeed, but render composition and input behavior are unstable in that mode.

## Implemented Stabilization

- Added `experimental_monitor_roam` setting (default `false`).
- Monitor roam now only activates when this experimental flag is explicitly enabled.
- Default unfocused behavior is a compact bottom-right companion window (stable fallback).
- Focused mode remains unchanged.

## Next Investigation Path (Optional)

If monitor roam is revisited, test these combinations separately:

1. monitor-sized + transparent + borderless
2. monitor-sized + non-transparent + borderless
3. compact + transparent + borderless

and compare behavior in both editor and exported release.
