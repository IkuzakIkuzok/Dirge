
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;

namespace Dirge.Generators;

internal record DisposableFieldInfo(string Name, bool IsRefStruct, string? FlagName, bool FlagCondition)
{
    internal string GetDisposeCall()
        => this.IsRefStruct ? $"this.{this.Name}.Dispose()" : $"this.{this.Name}?.Dispose()";

    internal static DisposableFieldInfo? Create(IFieldSymbol field, INamedTypeSymbol targetType, INamedTypeSymbol disposableSymbol, SourceProductionContext context, Compilation compilation)
    {
        if (field.IsStatic) return null;

        if (!IsDisposable(field, targetType, disposableSymbol, compilation)) return null;

        var attributes = field.GetAttributes();
        if (attributes.Any(a => a.AttributeClass.FullName == TypesGenerator.DoNotDisposeAttributeName)) return null;

        var isRefStruct = field.Type.IsRefLikeType;

        var conditionalAttribute = attributes.FirstOrDefault(a => a.AttributeClass.FullName == TypesGenerator.DoNotDisposeWhenAttributeName);
        if (conditionalAttribute is null)
            return new(field.Name, isRefStruct, null, false);

        if (conditionalAttribute.ConstructorArguments.Length != 2) return null;

        var nameArg = conditionalAttribute.ConstructorArguments[0];
        if (nameArg.Value is not string name) return null;

        // 'name' must be a field of the parent type and must be a boolean
        var flagField = targetType.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault();
        if (flagField?.Type.SpecialType != SpecialType.System_Boolean)
        {
            DiagnosticReporter.DoNotDisposeWhenTargetMustBeBoolField(context, conditionalAttribute);
            return null;
        }
        if (!flagField.IsStatic) name = $"this.{name}";

        var condArg = conditionalAttribute.ConstructorArguments[1];
        if (condArg.Value is not bool condition) return null;

        return new(field.Name, isRefStruct, name, condition);
    } // internal static DisposableFieldInfo? Create (IFieldSymbol, INamedTypeSymbol, INamedTypeSymbol, SourceProductionContext, Compilation)

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
