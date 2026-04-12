
## Release 1.1.0

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|------
DIRGE001 | Design   | Error    | Invalid use of AutoDispose on readonly struct
DIRGE002 | Design   | Error    | Type must be partial to use AutoDispose
DIRGE003 | Design   | Error    | Dispose method in non-IDisposable base class
DIRGE004 | Design   | Error    | Missing overridable Dispose(bool) in IDisposable base class
DIRGE005 | Design   | Error    | DoNotDisposeWhen flag must be a bool field

## Release 1.2.0

### New Rules

Rule ID  | Category | Severity | Notes
---------|----------|----------|------
DIRGE101 | Design   | Warning  | DoNotDisposeWhen name argument should be nameof(...)
