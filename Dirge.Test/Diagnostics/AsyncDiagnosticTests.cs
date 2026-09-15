// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.Diagnostics;

[DiagnosticTest]
public sealed partial class AsyncDiagnosticTests
{
    // lang=C#
    [TestSource]
    private static readonly string _duplicateAutoDispose = """
        using Dirge;

        [AutoDispose]
        [{|CS0579:AutoDispose|}(IncludeAsync = true)]
        public partial class Owner { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _duplicateAutoDisposeAcrossPartialDeclarations = """
        using Dirge;

        [AutoDispose]
        public partial class Owner { }

        [{|CS0579:AutoDispose|}(IncludeAsync = true)]
        public partial class Owner { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _includeAsyncOnStruct = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        public partial struct {|DIRGE007:Owner|} { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _includeAsyncOnRefStruct = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        public ref partial struct {|DIRGE007:Owner|} { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _declaredDisposeMethod = """
        using Dirge;

        [AutoDispose(IncludeAsync = true)]
        public partial class {|DIRGE007:Invalid|}
        {
            public void Dispose() { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _syncOnlyDisposableBase = """
        using Dirge;

        class Parent : System.IDisposable
        {
            public void Dispose() { }
        }

        [AutoDispose(IncludeAsync = true)]
        partial class {|DIRGE007:Invalid|} : Parent { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _asyncOnlyDisposableBase = """
        using Dirge;

        class Parent : System.IAsyncDisposable
        {
            public System.Threading.Tasks.ValueTask DisposeAsync() => default;
        }

        [AutoDispose(IncludeAsync = true)]
        partial class {|DIRGE007:Invalid|} : Parent { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _inaccessibleDisposeAsyncCore = """
        using Dirge;

        class Parent : System.IDisposable, System.IAsyncDisposable
        {
            public void Dispose() { }

            public System.Threading.Tasks.ValueTask DisposeAsync() => default;

            protected virtual void Dispose(bool disposing) { }

            private System.Threading.Tasks.ValueTask DisposeAsyncCore() => default;
        }

        [AutoDispose(IncludeAsync = true)]
        partial class {|DIRGE007:Invalid|} : Parent { }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _nonVirtualDisposeAsyncCore = """
        using Dirge;

        class Parent : System.IDisposable, System.IAsyncDisposable
        {
            public void Dispose() { }

            public System.Threading.Tasks.ValueTask DisposeAsync() => default;

            protected virtual void Dispose(bool disposing) { }

            protected System.Threading.Tasks.ValueTask DisposeAsyncCore() => default;
        }

        [AutoDispose(IncludeAsync = true)]
        partial class {|DIRGE007:Invalid|} : Parent { }
        """;
} // public sealed partial class AsyncDiagnosticTests
