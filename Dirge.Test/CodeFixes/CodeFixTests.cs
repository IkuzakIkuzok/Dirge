
// (c) 2026 Kazuki Kohzuki

using Dirge.CodeFixes;

namespace Dirge.Test.CodeFixes;

[CodeFixTest]
public sealed partial class CodeFixTests
{
    // lang=C#
    [CodeFixSource<AddPartialModifierCodeFixProvider>]
    private const string _nonPartialClass = """
        using Dirge;
        using System.IO;

        [AutoDispose]
        -public class {|DIRGE002:NonPartialClass|}
        +public partial class NonPartialClass
        {
            private readonly Stream _stream;

            public NonPartialClass(Stream stream)
            {
                this._stream = stream;
            }
        }
        """;

    // lang=C#
    [CodeFixSource<AddPartialModifierCodeFixProvider>]
    private const string _nonPartialAncestors = """
        using Dirge;
        using System.IO;
        
        -public class {|DIRGE002:NonPartialClass|}
        +public partial class NonPartialClass
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
    [CodeFixSource<RemoveStaticCodeFixProvider>]
    private const string _staticClass = """
        using Dirge;
        using System.IO;

        [AutoDispose]
        -public {|DIRGE006:static|} partial class StaticClass
        +public partial class StaticClass
        {
            private static readonly Stream _stream = null!;
        }
        """;

    // lang=C#
    [CodeFixSource<UseNameofCodeFixProvider>]
    private const string _literalFieldName = """
        using Dirge;
        using System.IO;
        
        [AutoDispose]
        public partial class MyClass
        {
            private readonly bool _flag = true;

        -    [DoNotDisposeWhen({|DIRGE101:"_flag"|}, true)]
        +    [DoNotDisposeWhen(nameof(_flag), true)]
            private readonly Stream _stream1 = null!;
        }
        """;
} // public sealed partial class CodeFixTests
