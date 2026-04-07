
// (c) 2026 Kazuki Kohzuki

namespace Dirge.SourceGeneratorUtils;

[Generator(LanguageNames.CSharp)]
internal sealed class IsExternalInit : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("IsExternalInit.g.cs", Source));
    }

    // lang=C#
    private const string Source = """
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
""";
} // internal sealed class IsExternalInit : IIncrementalGenerator
