// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

// Keeps synchronous call syntax separate from field discovery and validation.
internal static class DisposeCallGenerator
{
    private static string GetDisposeCall(DisposableFieldInfo field)
        => field.IsRefStruct ? $"this.{field.Name}.Dispose()" : $"this.{field.Name}?.Dispose()";

    internal static void Generate(CodeBuilder builder, DisposableFieldInfo[] fields)
    {
        var fieldsGroup = fields.GroupBy(f => f.FlagName);

        var alwaysDisposeFields = fieldsGroup.Where(g => g.Key is null).SelectMany(g => g);
        foreach (var f in alwaysDisposeFields)
        {
            builder.Append(GetDisposeCall(f));
            builder.AppendLine(';');
        }

        foreach (var group in fieldsGroup.Where(g => g.Key is not null))
        {
            builder.AppendLine();
            GenerateConditionalDisposeCalls(builder, group);
        }
    } // internal static void Generate (CodeBuilder, DisposableFieldInfo[])

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
                    builder.Append(GetDisposeCall(f));
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
                builder.Append(GetDisposeCall(f));
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
                builder.Append(GetDisposeCall(f));
                builder.AppendLine(';');
            }
        }

        builder.AppendLine('}');
    } // private static void GenerateConditionalDisposeCalls (CodeBuilder, IGrouping<string?, DisposableFieldInfo> group)
} // internal static class DisposeCallGenerator
