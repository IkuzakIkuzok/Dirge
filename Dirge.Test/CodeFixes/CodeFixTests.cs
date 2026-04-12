
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
} // public sealed partial class CodeFixTests
