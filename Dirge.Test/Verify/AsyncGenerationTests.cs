// (c) 2026 Kazuki Kohzuki

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dirge.Test.Verify;

[VerifyTest(LanguageVersion.CSharp14)]
public sealed partial class AsyncGenerationTests
{
    // lang=C#
    [TestSource]
    private static readonly string _staticallyKnownDisposeCalls = """
        using Dirge;
        using System;
        using System.Threading.Tasks;

        [AutoDispose(IncludeAsync = true)]
        partial class Owner
        {
            private readonly SyncOnly _sync = null;
            private readonly Dual _dual = null;
            private readonly AsyncValue _value;
            private readonly AsyncValue? _nullableValue;
            private readonly ExplicitAsync _explicit = null;
        }

        sealed class SyncOnly : IDisposable
        {
            public void Dispose() { }
        }

        sealed class Dual : IDisposable, IAsyncDisposable
        {
            public void Dispose() { }
            public ValueTask DisposeAsync() => default;
        }

        struct AsyncValue : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => default;
        }

        sealed class ExplicitAsync : IAsyncDisposable
        {
            ValueTask IAsyncDisposable.DisposeAsync() => default;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _asyncOwnerWithSyncOnlyResource = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        sealed partial class Owner
        {
            private readonly SyncOnly _resource = null;
        }

        sealed class SyncOnly : System.IDisposable
        {
            public void Dispose() { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _includeAsyncWithUnmanagedResources = """
        using Dirge;

        [AutoDispose(IncludeAsync = true, ReleaseUnmanagedResources = nameof(ReleaseResources))]
        public sealed partial class Owner
        {
            private readonly System.IDisposable _sync = null;
            private readonly System.IAsyncDisposable _async = null;

            private void ReleaseResources() { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _includeAsyncAcrossPartialDeclarations = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        public sealed partial class Owner
        {
            private readonly System.IDisposable _sync = null;
        }

        public sealed partial class Owner
        {
            private readonly System.IAsyncDisposable _async = null;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _includeAsyncDisabled = """
        using Dirge;

        [AutoDispose(IncludeAsync = false)]
        public sealed partial class Owner
        {
            private readonly System.IDisposable _sync = null;
            private readonly System.IAsyncDisposable _async = null;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _sealedAsyncDispose = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        public sealed partial class Owner<T> where T : System.IAsyncDisposable
        {
            private readonly T _resource;

            public Owner(T resource) => _resource = resource;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _notSealedAsyncDispose = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        public partial class Owner<T> where T : System.IAsyncDisposable
        {
            private readonly T _resource;

            public Owner(T resource) => _resource = resource;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _legacyIgnoresAsyncFields = """
        using Dirge;

        [AutoDispose]
        public sealed partial class Legacy
        {
            private readonly System.IDisposable _sync = null;

            private readonly System.IAsyncDisposable _async = null;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _overrideDisposeAsyncCore = """
        using Dirge;

        class Parent : System.IDisposable, System.IAsyncDisposable
        {
            public void Dispose() { }

            public System.Threading.Tasks.ValueTask DisposeAsync() => default;

            protected virtual void Dispose(bool disposing) { }

            protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() => default;
        }

        [AutoDispose(IncludeAsync = true)]
        partial class Child : Parent
        {
            private readonly System.IAsyncDisposable _resource = null;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _asyncChildWithoutResources = """
        using Dirge;

        class Parent : System.IDisposable, System.IAsyncDisposable
        {
            public void Dispose() { }

            public System.Threading.Tasks.ValueTask DisposeAsync() => default;

            protected virtual void Dispose(bool disposing) { }

            protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() => default;
        }

        [AutoDispose(IncludeAsync = true)]
        partial class Child : Parent { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _asyncChildWithoutResourcesWithGeneratedBase = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        partial class Parent
        {
            private readonly System.IAsyncDisposable _resource = null;
        }

        [AutoDispose(IncludeAsync = true)]
        partial class Child : Parent { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _ignoresUnconfirmedAsyncFieldTypes = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        partial class Child { }

        [AutoDispose(IncludeAsync = true)]
        partial class Parent
        {
            private readonly Child _child = new();
            private readonly object _object = new();
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _declaredAsyncFieldTypes = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        partial class Child : System.IDisposable, System.IAsyncDisposable { }

        [AutoDispose(IncludeAsync = true)]
        partial class Parent
        {
            private readonly Child _child = new();
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _asyncIgnoresAutoProperties = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        partial class PropertyOwner
        {
            public System.IAsyncDisposable Resource { get; set; }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _conditionalDisposeAsync = """
        using Dirge;
        using System.IO;

        namespace Test;

        [AutoDispose(IncludeAsync = true)]
        internal partial class MyClass
        {
            private readonly bool _leaveOpen1;
            private static readonly bool _leaveOpen2;

            private readonly Stream _stream;

            [DoNotDisposeWhen(nameof(_leaveOpen1), true)]
            private readonly Stream _stream1;

            [DoNotDisposeWhen(nameof(_leaveOpen1), true)]
            private readonly Stream _stream2;

            [DoNotDisposeWhen(nameof(_leaveOpen1), false)]
            private readonly Stream _stream3;

            [DoNotDisposeWhen(nameof(_leaveOpen2), false)]
            private readonly Stream _stream4;

            [DoNotDispose]
            private readonly Stream _stream5;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _disposedFieldNameCollisions = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        partial class Owner<__generated_disposed>
        {
            private System.IAsyncDisposable __generated_asyncDisposed;
        }

        partial class Owner<__generated_disposed>
        {
            private bool __generated_disposed_1 { get; set; }
            private void __generated_asyncDisposed_1() { }
            private class __generated_asyncDisposed_2 { }
        }
        """;

    private static readonly string[] _ignoreFiles = [
        "ExtensionMethods.g.cs",
        "Microsoft.CodeAnalysis.EmbeddedAttribute.cs",
    ];

    private static partial bool IgnoreRule(GeneratedSourceResult result)
    {
        if (result.HintName.EndsWith("Attribute.g.cs", StringComparison.OrdinalIgnoreCase)) return true;
        if (_ignoreFiles.Contains(result.HintName)) return true;

        return false;
    } // private static bool IgnoreRule (GeneratedSourceResult)
} // public sealed partial class AsyncGenerationTests
