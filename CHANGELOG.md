# Changelog

## 1.3.0

- Add readable horizontal signal interference through Glitch Volume and runtime overrides; defaults to zero and animates with unscaled time.
- Allow renderer subclasses to supply fixed CRT screen properties. Fixed values override volume and transition contributions, including when the volume is disabled or absent.
- Preserve default renderer behavior, existing physical-property APIs and transition timings from 1.2.0.
- Add EditMode compatibility and effect-state tests. No project-specific constants, assets or dependencies are included.
- Validated package tests on Unity 2022.3.62f3 / URP 14.0.12. Unity 6 Render Graph execution has not been exercised for this release.
