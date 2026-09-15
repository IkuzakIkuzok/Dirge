
// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Dirge.Utils;
using System.Collections.Generic;

namespace Dirge.Generators;

internal record DisposableFieldInfo(string Name, bool IsRefStruct, string? FlagName, bool FlagCondition, bool IncludeAsync = false,
    DisposeCallKind SyncCall = DisposeCallKind.None, DisposeCallKind AsyncCall = DisposeCallKind.None,
    bool IsValueType = false, bool IsNullableValueType = false)
{
    internal static Result<DisposableFieldInfo>? Create(IFieldSymbol field, INamedTypeSymbol targetType, INamedTypeSymbol disposableSymbol, Compilation compilation, INamedTypeSymbol? asyncDisposableSymbol = null)
    {
        if (field.IsStatic || (asyncDisposableSymbol is not null && field.IsImplicitlyDeclared)) return null;

        var fieldType = field.Type;
        if (asyncDisposableSymbol is not null && fieldType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            fieldType = nullable.TypeArguments[0];
        var isAsyncDisposable = asyncDisposableSymbol is not null && SupportsInterface(fieldType, asyncDisposableSymbol);
        var isDisposable = asyncDisposableSymbol is null
            ? IsDisposable(field, targetType, disposableSymbol, compilation)
            : SupportsInterface(fieldType, disposableSymbol);
        if (!isDisposable && !isAsyncDisposable) return null;

        var attributes = field.GetAttributes();
        if (attributes.Any(a => a.AttributeClass.FullName == TypesGenerator.DoNotDisposeAttributeName)) return null;

        var isRefStruct = field.Type.IsRefLikeType;
        var syncCall = asyncDisposableSymbol is null ? DisposeCallKind.None
            : GetCallKind(fieldType, disposableSymbol, isDisposable, compilation, targetType);
        var asyncCall = asyncDisposableSymbol is null ? DisposeCallKind.None
            : GetCallKind(fieldType, asyncDisposableSymbol, isAsyncDisposable, compilation, targetType);
        var info = new DisposableFieldInfo(field.Name, isRefStruct, null, false, asyncDisposableSymbol is not null,
            syncCall, asyncCall, fieldType.IsValueType, !SymbolEqualityComparer.Default.Equals(field.Type, fieldType));

        var conditionalAttribute = attributes.FirstOrDefault(a => a.AttributeClass.FullName == TypesGenerator.DoNotDisposeWhenAttributeName);
        if (conditionalAttribute is null)
            return info;

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

        return info with { FlagName = name, FlagCondition = condition };
    } // internal static Result<DisposableFieldInfo>? Create (IFieldSymbol, INamedTypeSymbol, INamedTypeSymbol, Compilation)

    private static DisposeCallKind GetCallKind(ITypeSymbol type, INamedTypeSymbol contract, bool supported, Compilation compilation, INamedTypeSymbol target)
    {
        if (!supported)
            return type is ITypeParameterSymbol || (!type.IsValueType && !type.IsSealed)
                ? DisposeCallKind.Runtime : DisposeCallKind.None;

        var member = contract.GetMembers().OfType<IMethodSymbol>().Single();
        if (type is ITypeParameterSymbol parameter)
        {
            // Constraints can expose the contract directly, but a class constraint
            // may hide it or implement it explicitly.
            var pending = new Stack<ITypeSymbol>(parameter.ConstraintTypes);
            var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            while (pending.Count > 0)
            {
                var constraint = pending.Pop();
                if (!visited.Add(constraint)) continue;
                if (constraint.TypeKind == TypeKind.Class)
                {
                    if (!SupportsInterface(constraint, contract)
                        || GetCallKind(constraint, contract, true, compilation, target) != DisposeCallKind.Direct)
                        return DisposeCallKind.Interface;
                    continue;
                }
                if (constraint is ITypeParameterSymbol nested)
                    foreach (var item in nested.ConstraintTypes) pending.Push(item);
                else if (constraint.GetMembers(member.Name).Any(m => !SymbolEqualityComparer.Default.Equals(m, member)))
                    return DisposeCallKind.Interface;
            }
            return DisposeCallKind.Direct;
        }

        var implementation = type.FindImplementationForInterfaceMember(member);
        if (type.TypeKind == TypeKind.Interface)
        {
            var interfaces = new[] { type }.Concat(type.AllInterfaces);
            return interfaces.SelectMany(t => t.GetMembers(member.Name))
                .All(m => SymbolEqualityComparer.Default.Equals(m, member))
                ? DisposeCallKind.Direct : DisposeCallKind.Interface;
        }

        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            var members = current.GetMembers(member.Name);
            if (members.Length == 0) continue;
            return members.Any(m => SymbolEqualityComparer.Default.Equals(m, implementation)
                && m is IMethodSymbol { IsStatic: false } && compilation.IsSymbolAccessibleWithin(m, target))
                ? DisposeCallKind.Direct : DisposeCallKind.Interface;
        }

        // The declared interface is sufficient even if its implementation is generated later.
        return DisposeCallKind.Interface;
    }

    private static bool SupportsInterface(ITypeSymbol type, INamedTypeSymbol interfaceSymbol)
    {
        if (type.ImplementsInterface(interfaceSymbol)) return true;
        if (type is not ITypeParameterSymbol) return false;

        var pending = new Stack<ITypeSymbol>();
        var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        pending.Push(type);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current)) continue;
            if (current.ImplementsInterface(interfaceSymbol)) return true;
            if (current is ITypeParameterSymbol parameter)
                foreach (var constraint in parameter.ConstraintTypes)
                    pending.Push(constraint);
        }
        return false;
    }

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
