
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.IntegrationTest.Types;

internal sealed class DisposableClass : IDisposable, IDisposedStateTestType
{
    public bool IsDisposed { get; private set; } = false;

    public void Dispose()
    {
        this.IsDisposed = true;
    } // public Dispose ()
} // internal sealed class DisposableClass : IDisposable, IDisposedStateTestType
