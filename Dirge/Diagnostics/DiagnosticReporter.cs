
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Diagnostics;

internal static class DiagnosticReporter
{
    internal static void ReadonlyStructNotSupported(SourceProductionContext context, INamedTypeSymbol typeSymbol)
    {
        var readonlyTokenLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(typeDeclaration => typeDeclaration.Modifiers)
            .FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword))
            .GetLocation();
        var location = readonlyTokenLocation ?? typeSymbol.Locations.FirstOrDefault();

        var diagnostic = DiagnosticDescriptors.ReadonlyStructNotSupported(typeSymbol.Name, location);
        context.ReportDiagnostic(diagnostic);
    } // internal static void ReadonlyStructNotSupported (SourceProductionContext, INamedTypeSymbol)

    internal static void TypeMustBePartial(SourceProductionContext context, TypeDeclarationSyntax typeDeclaration)
    {
        var location = typeDeclaration.Identifier.GetLocation();

        var diagnostic = DiagnosticDescriptors.TypeMustBePartial(typeDeclaration.Identifier.Text, location);
        context.ReportDiagnostic(diagnostic);
    } // internal static void TypeMustBePartial (SourceProductionContext, INamedTypeSymbol)

    internal static void DisposeInNonDisposableBase(SourceProductionContext context, INamedTypeSymbol typeSymbol)
    {
        var baseType = typeSymbol.BaseType;
        var baseTypeName = baseType?.ToDisplayString() ?? "<unknown>";

        var baseListLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault()
            ?.BaseList?.GetLocation();

        var location = baseListLocation ?? typeSymbol.Locations.FirstOrDefault();
        var diagnostic = DiagnosticDescriptors.DisposeInNonDisposableBase(baseTypeName, location);
        context.ReportDiagnostic(diagnostic);
    } // internal static void DisposeInNonDisposableBase (SourceProductionContext, INamedTypeSymbol)

    internal static void MissingAccessibleDisposeBool(SourceProductionContext context, INamedTypeSymbol typeSymbol)
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
        var diagnostic = DiagnosticDescriptors.MissingAccessibleDisposeBool(baseTypeName, targetTypeName, location);
        context.ReportDiagnostic(diagnostic);
    } // internal static void MissingAccessibleDisposeBool (SourceProductionContext, INamedTypeSymbol)

    internal static void DoNotDisposeWhenTargetMustBeBoolField(SourceProductionContext context, AttributeData attribute)
    {
        // get location of the first argument of the attribute
        var firstArgument = attribute.ApplicationSyntaxReference
            ?.GetSyntax()
            .DescendantNodes()
            .OfType<AttributeArgumentSyntax>()
            .FirstOrDefault();
        var argumentLocation = firstArgument?.Expression.GetLocation();
        var location = argumentLocation ?? attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        var targetName = attribute.ConstructorArguments.FirstOrDefault().Value as string ?? "<unknown>";

        var diagnostic = DiagnosticDescriptors.DoNotDisposeWhenTargetMustBeBoolField(targetName, location);
        context.ReportDiagnostic(diagnostic);
    } // internal static void DoNotDisposeWhenTargetMustBeBoolField (SourceProductionContext, AttributeData)

    internal static void StaticClassNotSupported(SourceProductionContext context, INamedTypeSymbol typeSymbol)
    {
        var staticTokenLocation = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(typeDeclaration => typeDeclaration.Modifiers)
            .FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))
            .GetLocation();
        var location = staticTokenLocation ?? typeSymbol.Locations.FirstOrDefault();

        var diagnostic = DiagnosticDescriptors.StaticClassNotSupported(typeSymbol.Name, staticTokenLocation);
        context.ReportDiagnostic(diagnostic);
    } // internal static void StaticClassNotSupported (SourceProductionContext, INamedTypeSymbol)

    internal static void DoNotDisposeWhenNameShouldBeNameof(SourceProductionContext context, ExpressionSyntax syntax)
    {
        var location = syntax.GetLocation();
        var diagnostic = DiagnosticDescriptors.DoNotDisposeWhenNameShouldBeNameof(location);
        context.ReportDiagnostic(diagnostic);
    } // internal static void DoNotDisposeWhenNameShouldBeNameof (SourceProductionContext, ExpressionSyntax)
} // internal static class DiagnosticReporter
