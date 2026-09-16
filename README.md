
# Dirge

[![Test](https://github.com/IkuzakIkuzok/Dirge/actions/workflows/Test.yml/badge.svg)](https://github.com/IkuzakIkuzok/Dirge/actions/workflows/Test.yml)
[![Version](https://img.shields.io/nuget/v/Dirge?styles=flat)](https://www.nuget.org/packages/Dirge/#versions-body-tab)
[![Download](https://img.shields.io/nuget/dt/Dirge?styles=flat)](https://www.nuget.org/packages/Dirge/#versions-body-tab)
[![MIT License](http://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://github.com/IkuzakIkuzok/Dirge/blob/main/LICENSE)

Disposable Implementation Roslyn Generator Extension
## Installation

You can install the EnumSerializer from [NuGet](https://www.nuget.org/packages/Dirge/).

## Usage

Mark a class with the `[AutoDispose]` attribute and implement the `IDisposable` interface.
The generator will automatically generate the implementation of the `Dispose` method for you.

Generated disposal-state fields keep the names `__generated_disposed` and
`__generated_asyncDisposed` when available. If a name conflicts with a member in
any partial declaration, the type name, or a type parameter, the generator appends
`_1`, `_2`, etc. until it finds an available name. Inherited members do not cause
renaming, preserving the existing behavior of these private fields.

```C#
using Dirge;

namespace Test;

[AutoDispose]
internal partial class TestClass
{
    private readonly Stream _stream = new MemoryStream();
}
```

The generated code will look like this:
```C#
namespace Test;

partial class TestClass : IDisposable
{
    private bool __generated_disposed = false;

    public void Dispose()
    {
        Dispose(true);
        global::System.GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.__generated_disposed) return;

        try
        {
            if (disposing)
            {
                this._stream?.Dispose();
            }
        }
        finally
        {
            this.__generated_disposed = true;
        }
    }
}
```

Note that this example is simplified for demonstration purposes.

To suppress the auto-generated Dispose call for a specific field, you can use the `[DoNotDispose]` attribute:
```C#
using Dirge;

namespace Test;

[AutoDispose]
internal partial class TestClass
{
    private readonly Stream _stream1 = new MemoryStream();

    [DoNotDispose]
    private readonly Stream _stream2 = new MemoryStream(); // This field will not be disposed by the generated Dispose method.
}
```

Ref-struct is also supported, but `IDisposable` will not be implemented regardless of the language version.

### Conditional disposal

Conditional disposal, which allows you to specify conditions under which a field should be disposed, is also supported.
You can use the `[DoNotDisposeWhen]` attribute with a boolean field and a value to compare against:
```C#
using Dirge;

namespace Test;

[AutoDispose]
internal partial class TestClass
{
    private readonly bool _leaveOpen;

    [DoNotDisposeWhen(nameof(_leaveOpen), true)]
    private readonly Stream _stream;

    internal TestClass(Stream stream, bool leaveOpen)
    {
        this._stream = stream;
        this._leaveOpen = leaveOpen;
    }
}
```

This will prevent the generator from disposing the `_stream` field when the `_leaveOpen` field is `true`:
```C#
namespace Test;

partial class TestClass : IDisposable
{
    private bool __generated_disposed = false;

    public void Dispose()
    {
        Dispose(true);
        global::System.GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.__generated_disposed) return;

        try
        {
            if (disposing)
            {
                if (!this._leaveOpen)
                {
                    this._stream?.Dispose();
                }
            }
        }
        finally
        {
            this.__generated_disposed = true;
        }
    }
}
```

### Unmanaged resources

To safely release unmanaged resources, this generator also supports the implementation of a finalizer.
You can specify a method to release unmanaged resources through the `ReleaseUnmanagedResources` option:
```C#
using Dirge;
using System.IO;
        
namespace Test;
        
[AutoDispose(ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
internal sealed partial class TestClass
{
    private readonly Stream _stream;

    internal void ReleaseUnmanagedResources()
    {
        // Custom logic to release unmanaged resources
    }
}
```

This will generate a finalizer that calls the specified method to release unmanaged resources:
```C#
namespace Test;

sealed partial class TestClass : IDisposable
{
    private bool __generated_disposed = false;

    public void Dispose()
    {
        Dispose(true);
        global::System.GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (this.__generated_disposed) return;

        try
        {
            if (disposing)
            {
                this._stream?.Dispose();
            }

            ReleaseUnmanagedResources();
        }
        finally
        {
            this.__generated_disposed = true;
        }
    }

    ~TestClass()
    {
        Dispose(false);
    }
}
```

### Asynchronous disposal

Use `[AutoDispose(IncludeAsync = true)]` on a partial class to generate both
`IDisposable` and `IAsyncDisposable`. `IncludeAsync` defaults to `false`, so existing
`[AutoDispose]` usage and explicit `IncludeAsync = false` retain synchronous-only
generation and ignore fields that implement only `IAsyncDisposable`.

Specify all options, including `ReleaseUnmanagedResources`, on this single attribute.
`AllowMultiple = false` prevents duplicate attributes, including duplicates across
partial declarations; the compiler reports `CS0579`.

```C#
using Dirge;
using System.IO;

[AutoDispose(IncludeAsync = true)]
internal sealed partial class AsyncOwner
{
    private readonly Stream _stream = new MemoryStream();
}

// In an async method:
// await using var owner = new AsyncOwner();
```

`DisposeAsync()` awaits each resource with `ConfigureAwait(false)`, preferring
`IAsyncDisposable` and falling back to `IDisposable`. When a resource's type or generic
constraints guarantee the interface, generated code calls it directly with any needed
null check. Interface conversions are retained for explicit implementations or hidden
methods; runtime interface checks are used when the static type cannot determine the
disposal path. `Dispose()` calls only
`IDisposable`; it does not block on asynchronous cleanup. Resources that support only
asynchronous disposal therefore require `DisposeAsync()` / `await using`.

Field selection uses the input compilation: the field's declared type (including
generic constraints and nullable value types) must implement `IDisposable` or
`IAsyncDisposable`. Attributes on the resource type are not used to predict interfaces
that might be generated later. Declare the interface on that type or use an
interface-typed field to include such a resource. An `object`-typed field is ignored.
Runtime checks only select an additional disposal interface on an already selected
field, without casting through `object`. The existing synchronous ref-struct
pattern-based exception is unchanged.

`[DoNotDispose]`, `[DoNotDisposeWhen]`, and `ReleaseUnmanagedResources` work with both
paths. Static fields and null resources are skipped. Non-sealed classes expose
`protected virtual ValueTask DisposeAsyncCore()`. After asynchronous cleanup,
`Dispose(false)` releases unmanaged resources without disposing managed resources
again, and finalization is suppressed. This follows the
[Microsoft async dispose pattern](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync).

Sequential calls to either disposal method do not dispose fields again. If a resource
throws, later fields in that method are skipped; generated base cleanup and unmanaged
cleanup still run through `finally` blocks, and failed cleanup is not retried.
Concurrent disposal is not synchronized.

`IncludeAsync = true` requires a framework that provides `IAsyncDisposable` and
`ValueTask`. It supports classes, including generic and nested classes, but reports
`DIRGE007` for structs or ref structs. Synchronous struct support is unchanged when
`IncludeAsync` is omitted or `false`. Disposal methods on the target class must be
generated. Derived classes can inherit another `[AutoDispose(IncludeAsync = true)]`
class, or a class implementing
both interfaces with accessible, non-abstract, overridable `Dispose(bool)` and
`DisposeAsyncCore()` methods. A base class with both public disposal entry points
abstract is also supported. Other disposable base configurations produce `DIRGE007`
instead of silently hiding cleanup methods. Handwritten base classes must follow the
same disposal pattern and govern their own exception and repeat-call behavior.
When a derived class adds neither disposable fields nor `ReleaseUnmanagedResources`,
and the base provides both implementations, no code is generated for the derived
class. It inherits the base behavior unchanged. Root classes and classes implementing
abstract disposal entry points still receive implementations even without resources.

## Constraints

To generate the `Dispose` method, the class (or struct) must meet the following constraints:
- It must be a non-static class.
- It must be a partial class or struct.
- It must not be a readonly struct.

For conditional disposal, the field specified in the `nameof` expression must be a boolean field.
Properties and methods are not supported for the current version.
