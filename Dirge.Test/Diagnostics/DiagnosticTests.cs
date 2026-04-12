
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Test.Diagnostics;

[DiagnosticTest]
public sealed partial class DiagnosticTests
{
    // lang=C#
    [TestSource]
    private static readonly string _readonlyStruct = """
        using Dirge;
        using System.IO;

        namespace Test;

        [AutoDispose]
        public {|DIRGE001:readonly|} partial struct ReadonlyStruct
        {
            private readonly Stream _stream;

            public ReadonlyStruct(Stream stream)
            {
                this._stream = stream;
            }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _nonPartialClass = """
        using Dirge;
        using System.IO;

        [AutoDispose]
        public class {|DIRGE002:NonPartialClass|}
        {
            private readonly Stream _stream;

            public NonPartialClass(Stream stream)
            {
                this._stream = stream;
            }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _nonPartialAncestors = """
        using Dirge;
        using System.IO;
        
        public class {|DIRGE002:NonPartialClass|}
        {
            [AutoDispose]
            public partial class PartialClass
            {
                private readonly Stream _stream;
        
                public PartialClass(Stream stream)
                {
                    this._stream = stream;
                }
            }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _disposeInNonDisposableBase = """
        using Dirge;
        using System.IO;
        
        [AutoDispose]
        public partial class MyClass {|DIRGE003:: BaseClass|}
        {
            private readonly Stream _stream;
        
            public MyClass(Stream stream)
            {
                this._stream = stream;
            }
        }

        // Base class with a Dispose method but does not implement IDisposable
        public class BaseClass
        {
            public void Dispose() { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _missingAccessibleDisposeBool = """
        using Dirge;
        using System;
        using System.IO;
        
        [AutoDispose]
        public partial class MyClass {|DIRGE004:: BaseClass|}
        {
            private readonly Stream _stream;
        
            public MyClass(Stream stream)
            {
                this._stream = stream;
            }
        }
        
        // Base class implements IDisposable but does not provide an accessible Dispose(bool) method
        public class BaseClass : IDisposable
        {
            public void Dispose() { }

            private void Dispose(bool disposing) { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _missingOverridableDisposeBool = """
        using Dirge;
        using System;
        using System.IO;
        
        [AutoDispose]
        public partial class MyClass {|DIRGE004:: BaseClass|}
        {
            private readonly Stream _stream;
        
            public MyClass(Stream stream)
            {
                this._stream = stream;
            }
        }
        
        // Base class provides a Dispose(bool) method but it is not virtual, so it cannot be overridden by the generated code
        public class BaseClass : IDisposable
        {
            public void Dispose() { }

            protected void Dispose(bool disposing) { }
        }
        """;

    // lang=C#
    [TestSource]
    private static readonly string _invalidConditionalFlags = """
        using Dirge;
        using System.IO;
        
        [AutoDispose]
        public partial class MyClass
        {
            private readonly bool _boolFlag = true;
            private readonly int _nonBool = 0;
            
            internal static bool PropFlag { get; } = true;

            [DoNotDisposeWhen(nameof(_boolFlag), true)]
            private readonly Stream _stream1 = null!;

            [DoNotDisposeWhen({|DIRGE005:nameof(_nonBool)|}, true)] // Non-bool field
            private readonly Stream _stream2 = null!;

            [DoNotDisposeWhen({|DIRGE005:nameof(PropFlag)|}, true)] // Property
            private readonly Stream _stream3 = null!;

            [DoNotDisposeWhen({|DIRGE005:"_nonExistentFlag"|}, true)] // Non-existent field
            private readonly Stream _stream4 = null!;
        }
        """;
} // public sealed partial class DiagnosticTests
