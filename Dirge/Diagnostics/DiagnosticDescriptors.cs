
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Diagnostics;

internal static class DiagnosticDescriptors
{
    internal static string ReadonlyStructNotSupportedId => _readonlyStructNotSupportedId;
    internal static string TypeMustBePartialId => _typeMustBePartialId;
    internal static string DisposeInNonDisposableBaseId => _disposeInNonDisposableBaseId;
    internal static string MissingAccessibleDisposeBoolId => _missingAccessibleDisposeBoolId;
    internal static string DoNotDisposeWhenTargetMustBeBoolFieldId => _doNotDisposeWhenTargetMustBeBoolFieldId;
    internal static string StaticClassNotSupportedId => _staticClassNotSupportedId;
    internal static string DoNotDisposeWhenNameShouldBeNameofId => _doNotDisposeWhenNameShouldBeNameofId;

    private const string _readonlyStructNotSupportedId = "DIRGE001";
    private const string _typeMustBePartialId = "DIRGE002";
    private const string _disposeInNonDisposableBaseId = "DIRGE003";
    private const string _missingAccessibleDisposeBoolId = "DIRGE004";
    private const string _doNotDisposeWhenTargetMustBeBoolFieldId = "DIRGE005";
    private const string _staticClassNotSupportedId = "DIRGE006";
    private const string _doNotDisposeWhenNameShouldBeNameofId = "DIRGE101";

#if !DIAGNOSTIC_ID_ONLY

    private static readonly DiagnosticDescriptor _readonlyStructNotSupported = new(
        id: _readonlyStructNotSupportedId,
        title: "Invalid use of AutoDispose on readonly struct",
        messageFormat: "The [AutoDispose] attribute cannot be applied to the readonly struct '{0}'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _typeMustBePartial = new(
        id: _typeMustBePartialId,
        title: "Type must be partial to use AutoDispose",
        messageFormat: "The type '{0}' must be declared as 'partial' to use the [AutoDispose] attribute",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _disposeInNonDisposableBase = new(
        id: _disposeInNonDisposableBaseId,
        title: "Dispose method in non-IDisposable base class",
        messageFormat: "The base class '{0}' has a 'void Dispose()' method but does not implement IDisposable",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _missingAccessibleDisposeBool = new(
        id: _missingAccessibleDisposeBoolId,
        title: "Missing overridable Dispose(bool) in IDisposable base class",
        messageFormat: "The base class '{0}' implements IDisposable but does not provide an overridable 'void Dispose(bool)' method required by '{1}'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _doNotDisposeWhenTargetMustBeBoolField = new(
        id: _doNotDisposeWhenTargetMustBeBoolFieldId,
        title: "DoNotDisposeWhen flag must be a bool field",
        messageFormat: "The flag '{0}' specified in [DoNotDisposeWhen] must be a field of type 'bool'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _staticClassNotSupported = new(
        id: _staticClassNotSupportedId,
        title: "Static class is not supported",
        messageFormat: "The [AutoDispose] attribute cannot be applied to the static class '{0}'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _doNotDisposeWhenNameShouldBeNameof = new(
        id: _doNotDisposeWhenNameShouldBeNameofId,
        title: "DoNotDisposeWhen name argument should be nameof(...)",
        messageFormat: "The name argument in [DoNotDisposeWhen] should be a nameof(...) expression for better refactorability",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static DiagnosticInfo ReadonlyStructNotSupported(INamedTypeSymbol typeSymbol)
    {
        var readonlyTokenLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(typeDeclaration => typeDeclaration.Modifiers)
            .FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
            .GetLocation();

        var location = readonlyTokenLocation ?? typeSymbol.Locations.FirstOrDefault();
        return new(_readonlyStructNotSupportedId, location, new([typeSymbol.Name]));
    } // internal static DiagnosticInfo ReadonlyStructNotSupported (INamedTypeSymbol)

    internal static DiagnosticInfo TypeMustBePartial(TypeDeclarationSyntax typeDeclaration)
    {
        var location = typeDeclaration.Identifier.GetLocation();
        return new(_typeMustBePartialId, location, new([typeDeclaration.Identifier.Text]));
    } // internal static DiagnosticInfo TypeMustBePartial (TypeDeclarationSyntax)

    internal static DiagnosticInfo DisposeInNonDisposableBase(INamedTypeSymbol typeSymbol)
    {
        var baseType = typeSymbol.BaseType;
        var baseTypeName = baseType?.ToDisplayString() ?? "<unknown>";

        var baseListLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault()
            ?.BaseList?.GetLocation();

        var location = baseListLocation ?? typeSymbol.Locations.FirstOrDefault();
        return new(_disposeInNonDisposableBaseId, location, new([baseTypeName]));
    } // internal static DiagnosticInfo DisposeInNonDisposableBase (INamedTypeSymbol)

    internal static DiagnosticInfo MissingAccessibleDisposeBool(INamedTypeSymbol typeSymbol)
    {
        var baseType = typeSymbol.BaseType;
        var baseTypeName = baseType?.ToDisplayString() ?? "<unknown>";
        var targetTypeName = typeSymbol.ToDisplayString();

        var baseListLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault()
            ?.BaseList?.GetLocation();

        var location = baseListLocation ?? typeSymbol.Locations.FirstOrDefault();
        return new(_missingAccessibleDisposeBoolId, location, new([baseTypeName, targetTypeName]));
    } // internal static DiagnosticInfo MissingAccessibleDisposeBool (INamedTypeSymbol)

    internal static DiagnosticInfo DoNotDisposeWhenTargetMustBeBoolField(AttributeData attribute)
    {
        var targetName = attribute.ConstructorArguments.FirstOrDefault().Value as string ?? "<unknown>";

        // get location of the first argument of the attribute
        var firstArgument = attribute.ApplicationSyntaxReference
            ?.GetSyntax()
            .DescendantNodes()
            .OfType<AttributeArgumentSyntax>()
            .FirstOrDefault();
        var argumentLocation = firstArgument?.Expression.GetLocation();
        var location = argumentLocation ?? attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        return new(_doNotDisposeWhenTargetMustBeBoolFieldId, location, new([targetName]));
    } // internal static DiagnosticInfo DoNotDisposeWhenTargetMustBeBoolField (AttributeData)

    internal static DiagnosticInfo StaticClassNotSupported(INamedTypeSymbol typeSymbol)
    {
        var staticTokenLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(typeDeclaration => typeDeclaration.Modifiers)
            .FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))
            .GetLocation();
        var location = staticTokenLocation ?? typeSymbol.Locations.FirstOrDefault();
        return new(_staticClassNotSupportedId, location, new([typeSymbol.Name]));
    } // internal static DiagnosticInfo StaticClassNotSupported (INamedTypeSymbol)

    internal static DiagnosticInfo DoNotDisposeWhenNameShouldBeNameof(ExpressionSyntax syntax)
    {
        var location = syntax.GetLocation();
        return new(_doNotDisposeWhenNameShouldBeNameofId, location, Array.Empty<string>());
    } // internal static DiagnosticInfo DoNotDisposeWhenNameShouldBeNameof (ExpressionSyntax)

    internal static Diagnostic GetDiagnostic(DiagnosticInfo diagnosticInfo)
    {
        var diagnosticDescriptor = GetDescriptor(diagnosticInfo.Id);
        return Diagnostic.Create(diagnosticDescriptor, diagnosticInfo.Location, diagnosticInfo.Arguments);
    }

    private static DiagnosticDescriptor GetDescriptor(string id)
        => id switch
        {
            _readonlyStructNotSupportedId => _readonlyStructNotSupported,
            _typeMustBePartialId => _typeMustBePartial,
            _disposeInNonDisposableBaseId => _disposeInNonDisposableBase,
            _missingAccessibleDisposeBoolId => _missingAccessibleDisposeBool,
            _doNotDisposeWhenTargetMustBeBoolFieldId => _doNotDisposeWhenTargetMustBeBoolField,
            _staticClassNotSupportedId => _staticClassNotSupported,
            _doNotDisposeWhenNameShouldBeNameofId => _doNotDisposeWhenNameShouldBeNameof,
            _ => throw new ArgumentException($"Unknown diagnostic ID: {id}")
        };

#endif
} // internal static class DiagnosticDescriptors
