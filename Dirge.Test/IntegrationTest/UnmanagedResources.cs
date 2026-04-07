
// (c) 2026 Kazuki Kohzuki

using Dirge.Test.IntegrationTest.Types;

namespace Dirge.Test.IntegrationTest;

[AutoDispose(ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
[TestClass(
    Name = "Release unmanaged resources",
    DisposedFields = [nameof(_managed), nameof(_unmanaged)]
)]
public sealed partial class UnmanagedResources
{
    private readonly DisposableClass _managed = new();
    private readonly UnmanagedResource _unmanaged = new();

    private void ReleaseUnmanagedResources()
    {
        this._unmanaged.ReleaseUnmanagedResources();
    } // private void ReleaseUnmanagedResources ()
} // public sealed partial class UnmanagedResources
