// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

// Keeps call syntax separate from field discovery and validation.
internal static class DisposeCallGenerator
{
    private static string GetDisposeCall(DisposableFieldInfo field)
        => field.IsRefStruct ? $"this.{field.Name}.Dispose()" : $"this.{field.Name}?.Dispose()";

    private static void GenerateCall(CodeBuilder builder, DisposableFieldInfo field, bool asynchronously)
    {
        if (!field.IncludeAsync)
        {
            builder.Append(GetDisposeCall(field));
            builder.AppendLine(';');
            return;
        }

        var value = $"this.@{field.Name}";
        if (!asynchronously)
        {
            GenerateSyncCall(builder, field, value);
            return;
        }

        if (field.AsyncCall == DisposeCallKind.None)
        {
            GenerateSyncCall(builder, field, value);
        }
        else if (field.AsyncCall == DisposeCallKind.Runtime)
        {
            var name = $"__generated_async_{field.Name}";
            builder.AppendLine($$"""
                if ({{value}} is global::System.IAsyncDisposable {{name}})
                {
                    await {{name}}.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                """);
            using (builder.BeginIndent()) GenerateSyncCall(builder, field, value);
            builder.AppendLine('}');
        }
        else
        {
            var needsNullCheck = !field.IsValueType || field.IsNullableValueType;
            if (needsNullCheck)
            {
                builder.AppendLine(field.IsNullableValueType ? $"if ({value}.HasValue)" : $"if ({value} is not null)");
                builder.AppendLine('{');
                builder.Indent();
            }
            var receiver = field.IsNullableValueType ? $"{value}.Value" : value;
            if (field.AsyncCall == DisposeCallKind.Interface)
                receiver = $"((global::System.IAsyncDisposable){receiver})";
            builder.AppendLine($"await {receiver}.DisposeAsync().ConfigureAwait(false);");
            if (needsNullCheck)
            {
                builder.Unindent();
                builder.AppendLine('}');
            }
        }
    }

    private static void GenerateSyncCall(CodeBuilder builder, DisposableFieldInfo field, string value)
    {
        if (field.SyncCall == DisposeCallKind.None) return;
        if (field.SyncCall == DisposeCallKind.Runtime)
        {
            var name = $"__generated_sync_{field.Name}";
            builder.AppendLine($$"""
                if ({{value}} is global::System.IDisposable {{name}})
                {
                    {{name}}.Dispose();
                }
                """);
            return;
        }
        if (field.SyncCall == DisposeCallKind.Direct)
        {
            var access = field.IsValueType && !field.IsNullableValueType ? "." : "?.";
            builder.AppendLine($"{value}{access}Dispose();");
            return;
        }

        if (field.IsNullableValueType)
        {
            builder.AppendLine($$"""
                if ({{value}}.HasValue)
                {
                    ((global::System.IDisposable){{value}}.Value).Dispose();
                }
                """);
        }
        else
        {
            var access = field.IsValueType ? "." : "?.";
            builder.AppendLine($"((global::System.IDisposable){value}){access}Dispose();");
        }
    }

    internal static void Generate(CodeBuilder builder, DisposableFieldInfo[] fields, bool asynchronously = false)
    {
        var fieldsGroup = fields.GroupBy(f => f.FlagName);

        var alwaysDisposeFields = fieldsGroup.Where(g => g.Key is null).SelectMany(g => g);
        foreach (var f in alwaysDisposeFields)
        {
            GenerateCall(builder, f, asynchronously);
        }

        foreach (var group in fieldsGroup.Where(g => g.Key is not null))
        {
            builder.AppendLine();
            GenerateConditionalDisposeCalls(builder, group, asynchronously);
        }
    } // internal static void Generate (CodeBuilder, DisposableFieldInfo[])

    private static void GenerateConditionalDisposeCalls(CodeBuilder builder, IGrouping<string?, DisposableFieldInfo> group, bool asynchronously)
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
                    GenerateCall(builder, f, asynchronously);
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
                GenerateCall(builder, f, asynchronously);
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
                GenerateCall(builder, f, asynchronously);
            }
        }

        builder.AppendLine('}');
    } // private static void GenerateConditionalDisposeCalls (CodeBuilder, IGrouping<string?, DisposableFieldInfo> group, bool asynchronously)
} // internal static class DisposeCallGenerator
