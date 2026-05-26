
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;

namespace Dirge.Diagnostics;

internal class Result<T> : IEquatable<Result<T>> where T : IEquatable<T>
{
    private readonly T? _value;
    private readonly EquatableArray<DiagnosticInfo>? _diagnostic;

    internal bool IsSuccess => this._diagnostic is null;

    internal T? Value => this._value;
    internal DiagnosticInfo[]? Diagnostic => this._diagnostic;

    private Result(T? value, DiagnosticInfo? diagnostic)
    {
        this._value = value;
        this._diagnostic = diagnostic is null ? null : new([diagnostic]);
    } // ctor (T?, DiagnosticInfo?)

    private Result(DiagnosticInfo[] diagnostics)
    {
        this._value = default;
        this._diagnostic = diagnostics;
    } // ctor (DiagnosticInfo[])

    internal static Result<T> Success(T value) => new(value, null);

    internal static Result<T> Failure(DiagnosticInfo diagnostic) => new(default, diagnostic);

    internal static Result<T> Failure(DiagnosticInfo[] diagnostics) => new(diagnostics);

    public bool Equals(Result<T> other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (this.IsSuccess && other.IsSuccess)
            return this.Value!.Equals(other.Value!);
        else if (!this.IsSuccess && !other.IsSuccess)
            return this.Diagnostic!.SequenceEqual(other.Diagnostic!);

        return false;
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(DiagnosticInfo diagnostic) => Failure(diagnostic);

    public static implicit operator Result<T>(DiagnosticInfo[] diagnostics) => Failure(diagnostics);
} // internal class Result<T> : IEquatable<Result<T>> where T : IEquatable<T>
