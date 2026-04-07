
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.IntegrationTest.Types;

internal ref struct DisposableRefStruct : IDisposedStateTestType
{
    public bool IsDisposed { get; private set; } = false;

    internal DisposableRefStruct(bool isDisposed)
    {
        this.IsDisposed = isDisposed;
    } // ctor (bool)

    public void Dispose()
    {
        this.IsDisposed = true;
    } // public Dispose ()
} // internal ref struct DisposableRefStruct : IDisposedStateTestType
