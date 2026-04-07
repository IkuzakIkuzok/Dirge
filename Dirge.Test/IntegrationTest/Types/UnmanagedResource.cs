
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.IntegrationTest.Types;

internal sealed class UnmanagedResource : IDisposedStateTestType
{
    public bool IsDisposed { get; private set; } = false;

    internal void ReleaseUnmanagedResources()
    {
        this.IsDisposed = true;
    } // internal void ReleaseUnmanagedResources ()
} // internal sealed class UnmanagedResource : IDisposedStateTestType
