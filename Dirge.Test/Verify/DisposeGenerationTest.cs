
// (c) 2026 Kazuki Kohzuki

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dirge.Test.Verify;

[VerifyTest(LanguageVersion.CSharp14)]
public sealed partial class DisposeGenerationTest
{
    // lang=C#
    [TestSource]
    private static readonly string _simpleDispose = """
        using Dirge;
        using System.IO;

        namespace Test;

        [AutoDispose]
        internal sealed partial class MyClass
        {
            private readonly Stream _stream;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _notSealed = """
        using Dirge;
        using System.IO;

        namespace Test;

        [AutoDispose]
        internal partial class MyClass
        {
            private readonly Stream _stream;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _globalNotSealed = """
        using Dirge;
        using System.IO;

        [AutoDispose]
        internal partial class MyClass
        {
            private readonly Stream _stream;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _nestedType = """
        using Dirge;
        using System.IO;
        
        namespace Test;
        
        internal partial class ParentClass
        {
            internal partial abstract struct GenericStruct<T>
            {
                internal partial static class StaticClass
                {
                    [AutoDispose]
                    internal sealed partial class MyClass
                    {
                        private readonly Stream _stream;
                    }
                }
            }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _refStruct = """
        using Dirge;
        
        namespace Test;

        [global::Dirge.AutoDispose]
        internal ref partial struct MyStruct
        {
            private readonly DisposableStruct _disposable;
            private readonly NotDisposableStruct _notDisposable;

            internal MyStruct(DisposableStruct disposableStruct, NotDisposableStruct notDisposableStruct)
            {
                this._disposable = disposableStruct;
                this._notDisposable = notDisposableStruct;
            }
        }
        
        internal ref struct DisposableStruct
        {
            // Accessible Dispose method
            internal void Dispose() { }
        }

        internal ref struct NotDisposableStruct
        {
            // Not accessible Dispose method
            private void Dispose() { }

            // Accessible but not a valid Dispose method for pattern matching
            internal void Dispose(int value) { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _conditionalDispose = """
        using Dirge;
        using System.IO;

        namespace Test;

        [AutoDispose]
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
    private static readonly string _releaseUnmanagedResources = """
        using Dirge;
        using System.IO;
        
        namespace Test;
        
        [AutoDispose(ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
        internal sealed partial class MyClass
        {
            private readonly Stream _stream;

            internal void ReleaseUnmanagedResources()
            {
                // Custom logic to release unmanaged resources
            }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _unmanagedResourceOnly = """
        using Dirge;

        namespace Test;
        
        [AutoDispose(ReleaseUnmanagedResources = nameof(ReleaseUnmanagedResources))]
        internal sealed partial class MyClass
        {
            internal void ReleaseUnmanagedResources()
            {
                // Custom logic to release unmanaged resources
            }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _overrideDispose = """
        using Dirge;
        using System;
        using System.IO;

        namespace Test;

        internal abstract class BaseClass : IDisposable
        {
            abstract public void Dispose();
        }

        [AutoDispose]
        internal sealed partial class MyClass : BaseClass
        {
            private readonly Stream _stream = null!;
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _overrideDisposeBool = """
        using Dirge;
        using System;
        using System.IO;

        namespace Test;

        internal class BaseClass : IDisposable
        {
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // Base class dispose logic
            }
        }

        [AutoDispose]
        internal sealed partial class MyClass : BaseClass
        {
            private readonly Stream _stream = null!;
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
} // public sealed partial class DisposeGenerationTest
