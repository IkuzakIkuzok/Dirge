
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.IntegrationTest.Types;

internal ref struct NotDisposableRefStruct : IDisposedStateTestType
{
    public bool IsDisposed { get; private set; } = false;

    internal NotDisposableRefStruct(bool isDisposed)
    {
        this.IsDisposed = isDisposed;
    } // ctor (bool)

    // Externally inaccessible Dispose method to make this type non-disposable.
    private void Dispose()
    {
        this.IsDisposed = true;
    } // private void Dispose ()
} // internal ref struct NotDisposableRefStruct : IDisposedStateTestType
