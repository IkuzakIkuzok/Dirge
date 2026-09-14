
// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Dirge.Utils;

namespace Dirge.Generators;

internal record DisposableFieldInfo(string Name, bool IsRefStruct, string? FlagName, bool FlagCondition)
{
    internal static Result<DisposableFieldInfo>? Create(IFieldSymbol field, INamedTypeSymbol targetType, INamedTypeSymbol disposableSymbol, Compilation compilation)
    {
        if (field.IsStatic) return null;

        if (!IsDisposable(field, targetType, disposableSymbol, compilation)) return null;

        var attributes = field.GetAttributes();
        if (attributes.Any(a => a.AttributeClass.FullName == TypesGenerator.DoNotDisposeAttributeName)) return null;

        var isRefStruct = field.Type.IsRefLikeType;

        var conditionalAttribute = attributes.FirstOrDefault(a => a.AttributeClass.FullName == TypesGenerator.DoNotDisposeWhenAttributeName);
        if (conditionalAttribute is null)
            return new DisposableFieldInfo(field.Name, isRefStruct, null, false);

        if (conditionalAttribute.ConstructorArguments.Length != 2) return null;

        var nameArg = conditionalAttribute.ConstructorArguments[0];
        if (nameArg.Value is not string name) return null;

        // 'name' must be a field of the parent type and must be a boolean
        var flagField = targetType.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault();
        if (flagField?.Type.SpecialType != SpecialType.System_Boolean)
            return DiagnosticDescriptors.DoNotDisposeWhenTargetMustBeBoolField(conditionalAttribute);
        if (!flagField.IsStatic) name = $"this.{name}";

        var nameSyntax = (conditionalAttribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)
            ?.ArgumentList?.Arguments[0].Expression;
        if (nameSyntax.IsKind(SyntaxKind.StringLiteralExpression))
            return DiagnosticDescriptors.DoNotDisposeWhenNameShouldBeNameof(nameSyntax);

        var condArg = conditionalAttribute.ConstructorArguments[1];
        if (condArg.Value is not bool condition) return null;

        return new DisposableFieldInfo(field.Name, isRefStruct, name, condition);
    } // internal static Result<DisposableFieldInfo>? Create (IFieldSymbol, INamedTypeSymbol, INamedTypeSymbol, Compilation)

    private static bool IsDisposable(IFieldSymbol field, INamedTypeSymbol targetType, INamedTypeSymbol disposableSymbol, Compilation compilation)
    {
        var fieldType = field.Type;
        if (fieldType.ImplementsInterface(disposableSymbol)) return true;

        if (!fieldType.IsRefLikeType) return false;

        var disposeMethod = fieldType.GetMembers("Dispose")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m =>
                !m.IsStatic &&
                m.Parameters.Length == 0 &&
                m.ReturnsVoid);

        if (disposeMethod is null) return false;

        return compilation.IsSymbolAccessibleWithin(disposeMethod, targetType);
    } // private static bool IsDisposable (IFieldSymbol, INamedTypeSymbol, INamedTypeSymbol, Compilation)
} // internal record DisposableFieldInfo (string, string?, bool)
