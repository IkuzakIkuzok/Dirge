
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;
using System.Buffers;

namespace Dirge.TestGenerator.CodeFixes;

internal readonly ref struct DiffSources
{
    internal readonly string Before { get; }

    internal readonly string After { get; }

    internal DiffSources(string input)
    {
        var length = input.Length;
        var maxLength = length << 1;
        char[]? pooled = null;

        try
        {
            var buffer = maxLength <= 0x2000 ? stackalloc char[maxLength] : (pooled = ArrayPool<char>.Shared.Rent(maxLength));
            var before = new SpanBuilder<char>(buffer[..length]);
            var after = new SpanBuilder<char>(buffer[length..(length << 1)]);

            ParseLines(input, ref before, ref after);

            this.Before = before.AsSpan().ToString();
            this.After = after.AsSpan().ToString();
        }
        finally
        {
            if (pooled is not null)
                ArrayPool<char>.Shared.Return(pooled);
        }
    } // ctor (string)

    private void ParseLines(ReadOnlySpan<char> input, ref SpanBuilder<char> before, ref SpanBuilder<char> after)
    {
        while (!input.IsEmpty)
        {
            var sep = input.IndexOfAny('\r', '\n');
            if (sep == -1)
            {
                AppendLine(input, ref before, ref after);
                break;
            
            }
            AppendLine(input[..sep], ref before, ref after);
            input = input[(sep + 1)..];
            if (!input.IsEmpty && input[0] == '\n')
            {
                input = input[1..];
            }
        }
    } // private void ParseLines (ReadOnlySpan<char>, ref SpanBuilder<char>, ref SpanBuilder<char>)

    private static void AppendLine(ReadOnlySpan<char> line, ref SpanBuilder<char> before, ref SpanBuilder<char> after)
    {
        if (line.IsEmpty)
        {
            before.AppendLine();
            after.AppendLine();
            return;
        }

        if (line[0] == '-')
        {
            before.AppendLine(line[1..]);
        }
        else if (line[0] == '+')
        {
            after.AppendLine(line[1..]);
        }
        else
        {
            before.AppendLine(line);
            after.AppendLine(line);
        }
    } // private void AppendLine (ReadOnlySpan<char>, ref SpanBuilder<char>, ref SpanBuilder<char>)
} // internal readonly ref struct DiffSources
