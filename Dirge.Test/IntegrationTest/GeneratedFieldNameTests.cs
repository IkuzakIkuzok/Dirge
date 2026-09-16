// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.IntegrationTest;

public sealed partial class GeneratedFieldNameTests
{
    [Fact]
    public void GeneratedFieldAvoidsTheTypeName()
    {
        var owner = new __generated_disposed();
        owner.Dispose();
        owner.Dispose();
        Assert.Equal(1, owner.ReleaseCalls);
    }

    [Fact]
    public void SyncDisposalPreservesCollidingMembersAndRunsOnce()
    {
        var resource = new Resource();
        var owner = new SyncOwner(resource);
        owner.Dispose();
        owner.Dispose();
        Assert.Equal(1, resource.SyncCalls);
        Assert.True(owner.__generated_disposed_1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsyncDisposalWithInheritedCorePreservesCollidingMembersAndRunsOnce(bool asynchronously)
    {
        var resource = new Resource();
        var owner = new AsyncChild(resource);
        if (asynchronously)
        {
            await owner.DisposeAsync();
            await owner.DisposeAsync();
            owner.Dispose();
        }
        else
        {
            owner.Dispose();
            owner.Dispose();
            await owner.DisposeAsync();
        }
        Assert.Equal(asynchronously ? 0 : 1, resource.SyncCalls);
        Assert.Equal(asynchronously ? 1 : 0, resource.AsyncCalls);
        Assert.Equal(1, owner.ReleaseCalls);
        Assert.True(owner.__generated_disposed);
        Assert.True(owner.__generated_asyncDisposed_1);
    }

    [AutoDispose]
    private sealed partial class SyncOwner(Resource resource)
    {
        private readonly Resource __generated_disposed = resource;
    }

    private partial class SyncOwner
    {
        public bool __generated_disposed_1 { get; } = true;
        private void __generated_disposed_2() { }
        private class __generated_disposed_3 { }
    }

    [AutoDispose(IncludeAsync = true, ReleaseUnmanagedResources = nameof(Release))]
    private partial class AsyncParent<__generated_disposed>
    {
        public int ReleaseCalls { get; private set; }
        private void Release() => this.ReleaseCalls++;
        private void __generated_asyncDisposed() { }
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class AsyncChild(Resource resource) : AsyncParent<int>
    {
        private readonly Resource __generated_asyncDisposed = resource;
        public bool __generated_disposed { get; } = true;
        public bool __generated_asyncDisposed_1 { get; } = true;
    }

    [AutoDispose(ReleaseUnmanagedResources = nameof(Release))]
    private sealed partial class __generated_disposed
    {
        public int ReleaseCalls { get; private set; }
        private void Release() => this.ReleaseCalls++;
    }

    private sealed class Resource : IDisposable, IAsyncDisposable
    {
        public int SyncCalls { get; private set; }
        public int AsyncCalls { get; private set; }
        public void Dispose() => this.SyncCalls++;
        public ValueTask DisposeAsync()
        {
            this.AsyncCalls++;
            return default;
        }
    }
}
