// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

internal static class AsyncDisposeGenerationCore
{
    internal static void Generate(CodeBuilder builder, DisposableTypeInfo source)
    {
        var generation = source.AsyncGenerationInfo!;
        builder.AppendLine($$"""

            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
            [global::System.Runtime.CompilerServices.CompilerGenerated]
            private bool {{source.AsyncDisposedFieldName}} = false;
            """);

        if (!generation.OverrideCore)
        {
            var modifier = generation.OverrideDisposeAsync ? "override " : "";
            builder.AppendLine($$"""

                public {{modifier}}async global::System.Threading.Tasks.ValueTask DisposeAsync()
                {
                    if (this.{{source.DisposedFieldName}}) return;

                    try
                    {
                        await DisposeAsyncCore().ConfigureAwait(false);
                    }
                    finally
                    {
                        Dispose(false);
                        global::System.GC.SuppressFinalize(this);
                    }
                }
                """);
        }

        var modifiers = generation.OverrideCore ? $"{generation.CoreAccessibility} override"
            : source.IsSealed ? "private" : "protected virtual";
        var isAsync = source.Fields.Any(f => f.AsyncCall != DisposeCallKind.None) || generation.OverrideCore;
        var asyncModifier = isAsync ? "async " : "";
        var earlyReturn = isAsync ? "return;" : "return default;";
        builder.AppendLine($$"""

            {{modifiers}} {{asyncModifier}}global::System.Threading.Tasks.ValueTask DisposeAsyncCore()
            {
                if (this.{{source.DisposedFieldName}} || this.{{source.AsyncDisposedFieldName}}) {{earlyReturn}}

                try
                {
            """);
        builder.Indent(2);
        DisposeCallGenerator.Generate(builder, source.Fields, asynchronously: true);
        if (!isAsync) builder.AppendLine("return default;");
        builder.Unindent(2);
        builder.AppendLine($$"""
                }
                finally
                {
                    this.{{source.AsyncDisposedFieldName}} = true;
            """);
        if (generation.OverrideCore)
        {
            builder.Indent(2);
            builder.AppendLine("await base.DisposeAsyncCore().ConfigureAwait(false);");
            builder.Unindent(2);
        }
        builder.AppendLine("""
                }
            }
            """);
    }
}
