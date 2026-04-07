
// (c) 2026 Kazuki Kohzuki

using Dirge.Test.IntegrationTest.Types;

namespace Dirge.Test.IntegrationTest;

[AutoDispose]
[TestClass(
    Name = "Sealed class (most simple case)",
    DisposedFields = [nameof(_disposedField)],
    NotDisposedFields = [nameof(_notDisposedField)]
)]
public sealed partial class SealedClass
{
    private readonly DisposableClass _disposedField = new();

    [DoNotDispose]
    private readonly DisposableClass _notDisposedField = new();
} // public sealed partial class SealedClass
