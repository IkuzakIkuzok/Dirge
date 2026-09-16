// (c) 2026 Kazuki Kohzuki

#pragma warning disable CA1816

namespace Dirge.Test.IntegrationTest;

public sealed partial class DisposeLifecycleTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DisposePreservesOrderAndDoesNotRetryAfterFailure(bool inherited, bool throws)
    {
        var calls = new List<string>();
        var resource = new RecordingResource(calls, throws);
        IDisposable target = inherited
            ? new InheritedLifecycle(resource, calls)
            : new RootLifecycle(resource, calls);

        try
        {
            if (throws)
                Assert.Throws<InvalidOperationException>(target.Dispose);
            else
                target.Dispose();

            target.Dispose();

            var expected = new List<string> { "managed" };
            if (!throws) expected.Add("unmanaged");
            if (inherited) expected.Add("base");
            Assert.Equal(expected, calls);
        }
        finally
        {
            GC.SuppressFinalize(target);
        }
    }

    [Fact]
    public void DisposeFalseSkipsManagedResourcesAndRunsBaseOnce()
    {
        var calls = new List<string>();
        var target = new InheritedLifecycle(new RecordingResource(calls, false), calls);
        try
        {
            target.DisposeWithoutManagedResources();
            target.Dispose();

            Assert.Equal(["unmanaged", "base"], calls);
        }
        finally
        {
            GC.SuppressFinalize(target);
        }
    }

    [Fact]
    public void SimpleDisposeDoesNotRetryAfterFailure()
    {
        var calls = new List<string>();
        var target = new SimpleLifecycle(new RecordingResource(calls, true));

        Assert.Throws<InvalidOperationException>(target.Dispose);
        target.Dispose();

        Assert.Equal(["managed"], calls);
    }

    private sealed class RecordingResource(List<string> calls, bool throws) : IDisposable
    {
        public void Dispose()
        {
            calls.Add("managed");
            if (throws) throw new InvalidOperationException();
        }
    }

    [AutoDispose]
    private sealed partial class SimpleLifecycle(RecordingResource resource)
    {
        private readonly RecordingResource _resource = resource;
    }

    [AutoDispose(ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
    private sealed partial class RootLifecycle(RecordingResource resource, List<string> calls)
    {
        private readonly RecordingResource _resource = resource;

        private void ReleaseUnmanagedResources() => calls.Add("unmanaged");
    }

    private class LifecycleBase(List<string> calls) : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) => calls.Add("base");
    }

    [AutoDispose(ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
    private sealed partial class InheritedLifecycle(RecordingResource resource, List<string> calls) : LifecycleBase(calls)
    {
        private readonly RecordingResource _resource = resource;
        private readonly List<string> _calls = calls;

        private void ReleaseUnmanagedResources() => this._calls.Add("unmanaged");

        internal void DisposeWithoutManagedResources() => Dispose(false);
    }
}
