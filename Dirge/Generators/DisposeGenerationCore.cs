
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

internal static class DisposeGenerationCore
{
    internal static void GenerateSimpleDispose(CodeBuilder builder, DisposableFieldInfo[] fields)
    {
        builder.AppendLine("""
            public void Dispose()
            {
                if (this.__generated_disposed) return;

                try
                {
            """);

        builder.Indent(2);
        GenerateDisposeCalls(builder, fields);
        builder.Unindent(2);

        builder.AppendLine("""
                }
                finally
                {
                    this.__generated_disposed = true;
                }
            }
            """);
    } // internal static void GenerateSimpleDispose (CodeBuilder builder, DisposableFieldInfo[] fields)

    internal static void GenerateRoot(CodeBuilder builder, bool overrideDispose, bool isSealed, DisposableFieldInfo[] fields, string className, string? releaseUnmanagedResources)
    {
        if (overrideDispose)
            builder.Append("override ");

        builder.AppendLine($$"""
            public void Dispose()
            {
                Dispose(true);
            """);

        if (isSealed && releaseUnmanagedResources is null)
        {
            builder.AppendLine('}');
        }
        else
        {
            builder.AppendLine("""
                global::System.GC.SuppressFinalize(this);
            }
            """);
        }

        var mod = isSealed ? "private" : "protected virtual";
        builder.AppendLine($$"""

            {{mod}} void Dispose(bool disposing)
            {
                if (this.__generated_disposed) return;

                try
                {
            """);
        builder.Indent();

        if (fields.Length > 0)
        {
            builder.Indent();
            builder.AppendLine("if (disposing)");
            builder.AppendLine('{');
            GenerateDisposeCalls(builder.Indented, fields);
            builder.AppendLine('}');
            builder.Unindent();
        }

        if (!string.IsNullOrWhiteSpace(releaseUnmanagedResources))
        {
            builder.AppendLine();
            builder.Append("    ");
            builder.Append(releaseUnmanagedResources!);
            builder.AppendLine("();");
        }

        builder.Unindent();
        builder.AppendLine("""
                }
                finally
                {
                    this.__generated_disposed = true;
                }
            }
            """);

        if (string.IsNullOrWhiteSpace(releaseUnmanagedResources)) return;

        builder.AppendLine($$"""

            ~{{className}}()
            {
                Dispose(false);
            }
            """);
    } // internal static void GenerateRoot (CodeBuilder, bool, DisposableFieldInfo[], string, string?)
    
    internal static void GenerateOverrideDisposeBool(CodeBuilder builder, string accessModifier, DisposableFieldInfo[] fields, string className, string? releaseUnmanagedResources)
    {
        builder.Append("override ");
        builder.Append(accessModifier);
        builder.AppendLine("""
             void Dispose(bool disposing)
            {
                if (this.__generated_disposed) return;

                try
                {
            """);
        builder.Indent();

        if (fields.Length > 0)
        {
            builder.Indent();
            builder.AppendLine("if (disposing)");
            builder.AppendLine('{');
            GenerateDisposeCalls(builder.Indented, fields);
            builder.AppendLine('}');
            builder.Unindent();
        }

        if (!string.IsNullOrWhiteSpace(releaseUnmanagedResources))
        {
            builder.AppendLine();
            builder.Append("    ");
            builder.Append(releaseUnmanagedResources!);
            builder.AppendLine("();");
        }

        builder.Unindent();
        builder.AppendLine("""
                }
                finally
                {
                    this.__generated_disposed = true;
                    base.Dispose(disposing);
                }
            }
            """);

        if (string.IsNullOrWhiteSpace(releaseUnmanagedResources)) return;

        builder.AppendLine($$"""

            ~{{className}}()
            {
                Dispose(false);
            }
            """);
    } // internal static void GenerateOverrideDisposeBool (CodeBuilder, string, DisposableFieldInfo[], string, string?)

    #region dispose calls

    private static void GenerateDisposeCalls(CodeBuilder builder, DisposableFieldInfo[] fields)
    {
        var fieldsGroup = fields.GroupBy(f => f.FlagName);

        var alwaysDisposeFields = fieldsGroup.Where(g => g.Key is null).SelectMany(g => g);
        foreach (var f in alwaysDisposeFields)
        {
            builder.Append(f.GetDisposeCall());
            builder.AppendLine(';');
        }

        foreach (var group in fieldsGroup.Where(g => g.Key is not null))
        {
            builder.AppendLine();
            GenerateConditionalDisposeCalls(builder, group);
        }
    } // private static void GenerateDisposeCalls (CodeBuilder, DisposableFieldInfo[])

    private static void GenerateConditionalDisposeCalls(CodeBuilder builder, IGrouping<string?, DisposableFieldInfo> group)
    {
        var disposeWhenTrue = group.Where(f => !f.FlagCondition).ToArray();
        var disposeWhenFalse = group.Where(f => f.FlagCondition).ToArray();

        if (disposeWhenTrue.Length == 0 || disposeWhenFalse.Length == 0)
        {
            var condition = disposeWhenTrue.Length > 0 ? group.Key : $"!{group.Key}";
            builder.AppendLine($"if ({condition})");
            builder.AppendLine('{');

            using (builder.BeginIndent())
            {
                foreach (var f in disposeWhenTrue.Length > 0 ? disposeWhenTrue : disposeWhenFalse)
                {
                    builder.Append(f.GetDisposeCall());
                    builder.AppendLine(';');
                }
            }
                

            builder.AppendLine('}');

            return;
        }

        builder.AppendLine($"if ({group.Key})");
        builder.AppendLine('{');

        using (builder.BeginIndent())
        {
            foreach (var f in disposeWhenTrue)
            {
                builder.Append(f.GetDisposeCall());
                builder.AppendLine(';');
            }
        }
        

        builder.AppendLine("""
            }
            else
            {
            """);

        using (builder.BeginIndent())
        {
            foreach (var f in disposeWhenFalse)
            {
                builder.Append(f.GetDisposeCall());
                builder.AppendLine(';');
            }
        }
        
        builder.AppendLine('}');
    } // private static void GenerateConditionalDisposeCalls (CodeBuilder, IGrouping<string?, DisposableFieldInfo> group)

    #endregion dispose calls
} // internal static class DisposeGenerationCore
