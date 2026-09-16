// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.IntegrationTest;

public sealed partial class AsyncDisposeTests
{
    [Fact]
    public async Task StaticallyKnownCallsHandleValuesNullsAndGenericConstraints()
    {
        var calls = new List<string>();
        await new DirectCallsOwner(calls).DisposeAsync();
        await new GenericAsyncOwner<AsyncValue>(new(calls)).DisposeAsync();
        await new GenericAsyncOwner<IAsyncDisposable?>(null).DisposeAsync();
        await new GenericAsyncOwner<AsyncResource>(new(calls, "generic-explicit")).DisposeAsync();
        await new ClassConstrainedAsyncOwner<PublicAsyncResource>(new(calls)).DisposeAsync();
        Assert.Equal(["public.async", "value.async", "value.async", "generic-explicit", "public.async"], calls);
    }

    [Fact]
    public async Task DirectValueCallsUpdateTheStoredResource()
    {
        var target = new MutableValueOwner();
        await target.DisposeAsync();
        Assert.Equal(1, target.Calls);
    }

    [Fact]
    public async Task OnlyConfirmedDisposableFieldsAreSelected()
    {
        var calls = new List<string>();
        await new SyncAnnotatedFieldsOwner(calls).DisposeAsync();
        Assert.Equal(["sync", "confirmed-async"], calls);
    }

    [Fact]
    public async Task SyncOnlyResourcesCanBeDisposedThroughEitherEntryPoint()
    {
        var calls = new List<string>();
        var target = new SyncOnlyAsyncOwner(calls);
        await target.DisposeAsync();
        target.Dispose();
        Assert.Equal(["sync"], calls);
    }

    [Fact]
    public async Task HiddenMethodsDoNotReplaceInterfaceImplementations()
    {
        var calls = new List<string>();
        await new HiddenMethodOwner(calls).DisposeAsync();
        Assert.Equal(["public.async"], calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IncludeAsyncUsesTheSelectedDisposalPathOnce(bool asynchronously)
    {
        var calls = new List<string>();
        var target = new MixedAsyncOwner(calls);
        if (asynchronously)
        {
            await target.DisposeAsync();
            target.Dispose();
            await target.DisposeAsync();
            Assert.Equal(["async", "sync", "dual-sync-field.async", "dual-async-field.async", "value.async", "unmanaged"], calls);
        }
        else
        {
            target.Dispose();
            await target.DisposeAsync();
            target.Dispose();
            Assert.Equal(["sync", "dual-sync-field.sync", "dual-async-field.sync", "unmanaged"], calls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConditionalAndExcludedResourcesRespectFlags(bool leaveOpen)
    {
        var calls = new List<string>();
        await using (var target = new ConditionalAsyncOwner(calls, leaveOpen)) { }
        Assert.Equal(leaveOpen ? ["when-true"] : new[] { "when-false" }, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneratedInheritanceDisposesDerivedThenBase(bool throws)
    {
        var calls = new List<string>();
        var target = new DerivedAsyncOwner(calls, throws);
        if (throws)
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await target.DisposeAsync());
        else
            await target.DisposeAsync();

        target.Dispose();
        await target.DisposeAsync();
        Assert.Equal(["derived", "base", "derived.unmanaged", "base.unmanaged"], calls);
    }

    [Fact]
    public async Task DisposalWaitsForTheResourceBeforeReleasingUnmanagedState()
    {
        var calls = new List<string>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = new AwaitingAsyncOwner(calls, completion.Task);
        var disposal = target.DisposeAsync();
        Assert.False(disposal.IsCompleted);
        Assert.Equal(["started"], calls);
        completion.SetResult();
        await disposal;
        Assert.Equal(["started", "completed", "unmanaged"], calls);
    }

    [Fact]
    public async Task AbstractEntryPointsAreImplemented()
    {
        var calls = new List<string>();
        AbstractAsyncOwner target = new ConcreteAsyncOwner(calls);
        await target.DisposeAsync();
        target.Dispose();
        Assert.Equal(["abstract-child"], calls);
    }

    [Fact]
    public async Task EmptyOwnerStillImplementsBothInterfaces()
    {
        var target = new EmptyAsyncOwner();
        await ((IAsyncDisposable)target).DisposeAsync();
        ((IDisposable)target).Dispose();
    }

    [Fact]
    public async Task EmptyChildPreservesRepeatedCallsToBaseHooks()
    {
        var target = new EmptyCountingChild();
        target.Dispose();
        target.Dispose();
        await target.DisposeAsync();
        await target.DisposeAsync();

        Assert.Equal(4, target.SyncCoreCalls);
        Assert.Equal(2, target.AsyncCoreCalls);
    }

    [Fact]
    public async Task EmptyChildStillImplementsAbstractEntryPoints()
    {
        AbstractAsyncOwner target = new EmptyAbstractChild();
        target.Dispose();
        await target.DisposeAsync();
    }

    [Fact]
    public async Task HandwrittenBaseUsesBothVirtualCleanupHooks()
    {
        var calls = new List<string>();
        IAsyncDisposable target = new ManualBaseChild(calls);
        await target.DisposeAsync();
        ((IDisposable)target).Dispose();
        Assert.Equal(["child", "manual.async", "manual.unmanaged"], calls);
    }

    private sealed class AsyncResource(List<string> calls, string name, bool throws = false) : IAsyncDisposable
    {
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await Task.Yield();
            calls.Add(name);
            if (throws) throw new InvalidOperationException();
        }
    }

    private sealed class SyncResource(List<string> calls) : IDisposable
    {
        void IDisposable.Dispose() => calls.Add("sync");
    }

    private sealed class DualResource(List<string> calls, string name) : IDisposable, IAsyncDisposable
    {
        void IDisposable.Dispose() => calls.Add(name + ".sync");
        ValueTask IAsyncDisposable.DisposeAsync()
        {
            calls.Add(name + ".async");
            return default;
        }
    }

    private readonly struct AsyncValue(List<string> calls) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            calls.Add("value.async");
            return default;
        }
    }

    private class PublicAsyncResource(List<string> calls) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            calls.Add("public.async");
            return default;
        }
    }

    private sealed class HiddenAsyncResource(List<string> calls) : PublicAsyncResource(calls)
    {
        public new ValueTask DisposeAsync() => throw new InvalidOperationException("Hidden method must not be called");
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class HiddenMethodOwner(List<string> calls)
    {
        private readonly HiddenAsyncResource _resource = new(calls);
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class DirectCallsOwner(List<string> calls)
    {
        private readonly PublicAsyncResource _resource = new(calls);
        private readonly AsyncValue _value = new(calls);
        private readonly PublicAsyncResource? _null = null;
        private readonly AsyncValue? _nullable = null;
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class GenericAsyncOwner<T>(T resource) where T : IAsyncDisposable?
    {
        private readonly T _resource = resource;
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class ClassConstrainedAsyncOwner<T>(T resource) where T : PublicAsyncResource
    {
        private readonly T _resource = resource;
    }

    private struct MutableAsyncValue : IAsyncDisposable
    {
        internal int Calls;
        public ValueTask DisposeAsync()
        {
            Calls++;
            return default;
        }
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class MutableValueOwner
    {
        private MutableAsyncValue _resource = new();
        internal int Calls => _resource.Calls;
    }

    [AutoDispose]
    private sealed partial class EmptySyncField;

    [AutoDispose]
    private sealed partial class PopulatedSyncField(List<string> calls)
    {
        private readonly IDisposable _resource = new SyncResource(calls);
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class SyncAnnotatedFieldsOwner(List<string> calls)
    {
        private readonly EmptySyncField _empty = new();
        private readonly PopulatedSyncField _populated = new(calls);
        private readonly IDisposable _confirmed = new PopulatedSyncField(calls);
        private readonly object _object = new SyncResource(calls);
        private readonly ConfirmedAsyncField _confirmedAsync = new(calls);
        private readonly UnconfirmedAsyncField _unconfirmedAsync = new(calls);
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class ConfirmedAsyncField(List<string> calls) : IDisposable, IAsyncDisposable
    {
        private readonly IAsyncDisposable _resource = new AsyncResource(calls, "confirmed-async");
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class UnconfirmedAsyncField(List<string> calls)
    {
        private readonly IAsyncDisposable _resource = new AsyncResource(calls, "unconfirmed-async");
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class SyncOnlyAsyncOwner(List<string> calls)
    {
        private readonly SyncResource _resource = new(calls);
    }

    [AutoDispose(IncludeAsync = true, ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
    private sealed partial class MixedAsyncOwner(List<string> calls)
    {
        private readonly AsyncResource _async = new(calls, "async");
        private readonly SyncResource _sync = new(calls);
        private readonly IDisposable _dualSync = new DualResource(calls, "dual-sync-field");
        private readonly IAsyncDisposable _dualAsync = new DualResource(calls, "dual-async-field");
        private readonly AsyncValue? _value = new AsyncValue(calls);
        private readonly IAsyncDisposable? _null = null;
        private void ReleaseUnmanagedResources() => calls.Add("unmanaged");
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class ConditionalAsyncOwner(List<string> calls, bool leaveOpen)
    {
        private readonly bool _leaveOpen = leaveOpen;
        [DoNotDisposeWhen(nameof(_leaveOpen), true)]
        private readonly IAsyncDisposable _whenFalse = new AsyncResource(calls, "when-false");
        [DoNotDisposeWhen(nameof(_leaveOpen), false)]
        private readonly IAsyncDisposable _whenTrue = new AsyncResource(calls, "when-true");
        [DoNotDispose]
        private readonly IAsyncDisposable _excluded = new AsyncResource(calls, "excluded");
        private static readonly IAsyncDisposable _static = new AsyncResource([], "static");
    }

    [AutoDispose(IncludeAsync = true, ReleaseUnmanagedResources = nameof(ReleaseBase))]
    private partial class BaseAsyncOwner(List<string> calls)
    {
        private readonly IAsyncDisposable _baseResource = new AsyncResource(calls, "base");
        private void ReleaseBase() => calls.Add("base.unmanaged");
    }

    [AutoDispose(IncludeAsync = true, ReleaseUnmanagedResources = nameof(ReleaseDerived))]
    private sealed partial class DerivedAsyncOwner(List<string> calls, bool throws) : BaseAsyncOwner(calls)
    {
        private readonly List<string> _calls = calls;
        private readonly IAsyncDisposable _derivedResource = new AsyncResource(calls, "derived", throws);
        private void ReleaseDerived() => this._calls.Add("derived.unmanaged");
    }

    private sealed class AwaitingResource(List<string> calls, Task completion) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            calls.Add("started");
            await completion.ConfigureAwait(false);
            calls.Add("completed");
        }
    }

    [AutoDispose(IncludeAsync = true, ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
    private sealed partial class AwaitingAsyncOwner(List<string> calls, Task completion)
    {
        private readonly IAsyncDisposable _resource = new AwaitingResource(calls, completion);
        private void ReleaseUnmanagedResources() => calls.Add("unmanaged");
    }

    private abstract class AbstractAsyncOwner : IDisposable, IAsyncDisposable
    {
        public abstract void Dispose();
        public abstract ValueTask DisposeAsync();
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class ConcreteAsyncOwner(List<string> calls) : AbstractAsyncOwner
    {
        private readonly IAsyncDisposable _resource = new AsyncResource(calls, "abstract-child");
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class EmptyAsyncOwner;

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class EmptyAbstractChild : AbstractAsyncOwner;

    private class CountingDisposalBase : IDisposable, IAsyncDisposable
    {
        internal int SyncCoreCalls { get; private set; }
        internal int AsyncCoreCalls { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) => this.SyncCoreCalls++;

        protected virtual ValueTask DisposeAsyncCore()
        {
            this.AsyncCoreCalls++;
            return default;
        }
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class EmptyCountingChild : CountingDisposalBase;

    private class ManualAsyncBase(List<string> calls) : IDisposable, IAsyncDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            if (!_disposed) calls.Add("manual.async");
            return default;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            calls.Add("manual.unmanaged");
            _disposed = true;
        }
    }

    [AutoDispose(IncludeAsync = true)]
    private sealed partial class ManualBaseChild(List<string> calls) : ManualAsyncBase(calls)
    {
        private readonly IAsyncDisposable _resource = new AsyncResource(calls, "child");
    }
}
