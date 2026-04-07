
// (c) 2026 Kazuki Kohzuki

using Dirge.Test.IntegrationTest.Types;

namespace Dirge.Test.IntegrationTest;

[AutoDispose]
[TestClass(
    Name = "Conditional dispose based on field values",
    DisposedFields = [nameof(_obj2), nameof(_obj3)],
    NotDisposedFields = [nameof(_obj1), nameof(_obj4)]
)]
public partial class ConditionalDispose
{
    private readonly bool _trueFlag = true;
    private readonly bool _falseFlag = false;

    [DoNotDisposeWhen(nameof(_trueFlag), true)]
    private readonly DisposableClass _obj1 = new();

    [DoNotDisposeWhen(nameof(_falseFlag), true)]
    private readonly DisposableClass _obj2 = new();

    [DoNotDisposeWhen(nameof(_trueFlag), false)]
    private readonly DisposableClass _obj3 = new();

    [DoNotDisposeWhen(nameof(_falseFlag), false)]
    private readonly DisposableClass _obj4 = new();
}
