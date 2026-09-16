// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

internal static class DisposeGenerationCore
{
    internal static void Generate(CodeBuilder builder, DisposableTypeInfo source)
    {
        var generation = source.GenerationInfo;
        switch (generation.Strategy)
        {
            case DisposeGenerationStrategy.GenerateSimple:
                GenerateSimpleDispose(builder, source.Fields, source.DisposedFieldName);
                break;
            case DisposeGenerationStrategy.GenerateRoot:
            case DisposeGenerationStrategy.OverrideDispose:
                GenerateRoot(builder, generation.Strategy == DisposeGenerationStrategy.OverrideDispose,
                    source.IsSealed, source.Fields, source.ReleaseUnmanagedResources, source.DisposedFieldName);
                break;
            case DisposeGenerationStrategy.OverrideDisposeBool:
                GenerateDisposeBool(builder, $"override {generation.AccessModifier}", source.Fields,
                    source.ReleaseUnmanagedResources, source.DisposedFieldName, callBase: true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(generation.Strategy));
        }

        GenerateFinalizer(builder, source.Name, source.ReleaseUnmanagedResources);
    } // internal static void Generate (CodeBuilder, DisposableTypeInfo)

    private static void GenerateSimpleDispose(CodeBuilder builder, DisposableFieldInfo[] fields, string disposedFieldName)
    {
        builder.AppendLine($$"""
            public void Dispose()
            {
                if (this.{{disposedFieldName}}) return;

                try
                {
            """);

        builder.Indent(2);
        DisposeCallGenerator.Generate(builder, fields);
        builder.Unindent(2);

        builder.AppendLine($$"""
                }
                finally
                {
                    this.{{disposedFieldName}} = true;
                }
            }
            """);
    } // private static void GenerateSimpleDispose (CodeBuilder builder, DisposableFieldInfo[] fields)

    private static void GenerateRoot(CodeBuilder builder, bool overrideDispose, bool isSealed, DisposableFieldInfo[] fields, string? releaseUnmanagedResources, string disposedFieldName)
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
        builder.AppendLine();
        GenerateDisposeBool(builder, mod, fields, releaseUnmanagedResources, disposedFieldName, callBase: false);
    } // private static void GenerateRoot (CodeBuilder, bool, bool, DisposableFieldInfo[], string?)

    private static void GenerateDisposeBool(CodeBuilder builder, string modifiers, DisposableFieldInfo[] fields, string? releaseUnmanagedResources, string disposedFieldName, bool callBase)
    {
        builder.AppendLine($$"""
            {{modifiers}} void Dispose(bool disposing)
            {
                if (this.{{disposedFieldName}}) return;

                try
                {
            """);
        builder.Indent();

        if (fields.Length > 0)
        {
            builder.Indent();
            builder.AppendLine("if (disposing)");
            builder.AppendLine('{');
            DisposeCallGenerator.Generate(builder.Indented, fields);
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
        builder.AppendLine($$"""
                }
                finally
                {
                    this.{{disposedFieldName}} = true;
            """);
        if (callBase)
        {
            builder.Indent(2);
            builder.AppendLine("base.Dispose(disposing);");
            builder.Unindent(2);
        }
        builder.AppendLine("""
                }
            }
            """);
    } // private static void GenerateDisposeBool (CodeBuilder, string, DisposableFieldInfo[], string?, bool)

    private static void GenerateFinalizer(CodeBuilder builder, string className, string? releaseUnmanagedResources)
    {
        if (string.IsNullOrWhiteSpace(releaseUnmanagedResources)) return;

        builder.AppendLine($$"""

            ~{{className}}()
            {
                Dispose(false);
            }
            """);
    } // private static void GenerateFinalizer (CodeBuilder, string, string?)
} // internal static class DisposeGenerationCore
