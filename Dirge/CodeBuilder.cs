
// (c) 2026 Kazuki Kohzuki

namespace Dirge;

internal class CodeBuilder
{
    private bool _needIndent = true;
    private readonly StringBuilder _builder;

    internal int IndentLevel { get; private set; } = 0;

    internal CodeBuilder Indented => new(this._builder) { IndentLevel = this.IndentLevel + 1 };

    internal CodeBuilder() : this(new()) { }

    internal CodeBuilder(StringBuilder builder)
    {
        this._builder = builder;
    } // ctor (StringBuilder)

    override public string ToString()
        => _builder.ToString();

    internal CodeBuilder AppendLine(char c)
    {
        AppendIndentIfNeeded();
        this._builder.Append(c);
        return AppendLine();
    } // AppendLine (char)

    internal CodeBuilder AppendLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return AppendLine();

        var span = line.AsSpan();

        while (true)
        {
            var idx_cr = span.IndexOf('\r');
            var idx_lf = span.IndexOf('\n');

            if (idx_cr < 0 && idx_lf < 0)
            {
                if (span.IsEmpty)
                    AppendLine();
                else
                    AppendSingleLine(span);
                break;
            }

            var idx = (int)Math.Min((uint)idx_cr, (uint)idx_lf);
            var l = span[..idx];
            if (l.IsEmpty)
                AppendLine();
            else
                AppendSingleLine(l);

            span = span[(idx + 1)..];
            if (idx_cr >= 0 && idx_lf >= 0 && idx_cr == idx_lf - 1)
                span = span[1..];
        }

        return this;
    } // AppendLine (string)

    unsafe private void AppendSingleLine(ReadOnlySpan<char> line)
    {
        AppendIndentIfNeeded();
        
        fixed (char *p = line)
            this._builder.Append(p, line.Length);
        this._builder.AppendLine();

        this._needIndent = true;
    } // unsafe private void AppendSingleLine (ReadOnlySpan<char>)

    internal CodeBuilder AppendLine()
    {
        this._builder.AppendLine();
        this._needIndent = true;
        return this;
    } // AppendLine ()

    internal CodeBuilder Append(string text)
    {
        AppendIndentIfNeeded();
        this._builder.Append(text);
        this._needIndent = false;
        return this;
    } // Append (string)

    private void AppendIndentIfNeeded()
    {
        if (!this._needIndent) return;
        this._builder.Append(new string(' ', this.IndentLevel * 4));
        this._needIndent = false;
    } // private void AppendIndentIfNeeded ()

    internal void Indent(int level = 1)
    {
        this.IndentLevel += level;
    } // internal void Indent ([int])

    internal void Unindent(int level = 1)
    {
        this.IndentLevel = Math.Max(0, this.IndentLevel - level);
    } // internal void Unindent ([int])

    internal IndentScope BeginIndent() => new(this);

    internal readonly struct IndentScope : IDisposable
    {
        private readonly CodeBuilder _builder;

        public IndentScope(CodeBuilder builder)
        {
            _builder = builder;
            _builder.Indent();
        } // public IndentScope (CodeBuilder)

        public void Dispose()
        {
            _builder.Unindent();
        } // public void Dispose ()
    } // internal readonly struct IndentScope : IDisposable
} // internal class CodeBuilder
