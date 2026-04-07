
// (c) 2026 Kazuki Kohzuki

#pragma warning disable CS0282

using Dirge.Test.IntegrationTest.Types;

namespace Dirge.Test.IntegrationTest;

[AutoDispose]
[TestClass(
    Name = "Ref struct, pattern-based dispose",
    DisposedFields = [nameof(_disposedField)],
    NotDisposedFields = [nameof(_notDisposedField)]
)]
public ref partial struct RefStruct
{
    // DisposableRefStruct does not implement IDisposable, but it has a pattern-based dispose method, so it should be disposed by the generator.
    private DisposableRefStruct _disposedField;

    // NotDisposableRefStruct does not implement IDisposable and does not have a pattern-based dispose method, so it should not be disposed by the generator.
    private NotDisposableRefStruct _notDisposedField;

    public RefStruct()
    {
        this._disposedField = new();
        this._notDisposedField = new();
    } // ctor ()
} // public ref struct RefStruct
