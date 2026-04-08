
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Generators;

internal static class DiagnosticDescriptors
{
    #region definitions

    private static readonly DiagnosticDescriptor _readonlyStructNotSupported = new(
        id: "DIRGE001",
        title: "Invalid use of AutoDispose on readonly struct",
        messageFormat: "The [AutoDispose] attribute cannot be applied to the readonly struct '{0}'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _typeMustBePartial = new(
        id: "DIRGE002",
        title: "Type must be partial to use AutoDispose",
        messageFormat: "The type '{0}' must be declared as 'partial' to use the [AutoDispose] attribute",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _disposeInNonDisposableBase = new(
        id: "DIRGE003",
        title: "Dispose method in non-IDisposable base class",
        messageFormat: "The base class '{0}' has a 'void Dispose()' method but does not implement IDisposable",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _missingAccessibleDisposeBool = new(
        id: "DIRGE004",
        title: "Missing overridable Dispose(bool) in IDisposable base class",
        messageFormat: "The base class '{0}' implements IDisposable but does not provide an overridable 'void Dispose(bool)' method required by '{1}'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor _doNotDisposeWhenTargetMustBeBoolField = new(
        id: "DIRGE005",
        title: "DoNotDisposeWhen flag must be a bool field",
        messageFormat: "The flag '{0}' specified in [DoNotDisposeWhen] must be a field of type 'bool'",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    #endregion

    internal static Diagnostic ReadonlyStructNotSupported(string typeName, Location? location)
        => Diagnostic.Create(_readonlyStructNotSupported, location, typeName);

    internal static Diagnostic TypeMustBePartial(string typeName, Location? location)
        => Diagnostic.Create(_typeMustBePartial, location, typeName);
    
    internal static Diagnostic DisposeInNonDisposableBase(string baseTypeName, Location? location)
        => Diagnostic.Create(_disposeInNonDisposableBase, location, baseTypeName);

    internal static Diagnostic MissingAccessibleDisposeBool(string baseTypeName, string targetTypeName, Location? location)
        => Diagnostic.Create(_missingAccessibleDisposeBool, location, baseTypeName, targetTypeName);

    internal static Diagnostic DoNotDisposeWhenTargetMustBeBoolField(string fieldName, Location? location)
        => Diagnostic.Create(_doNotDisposeWhenTargetMustBeBoolField, location, fieldName);
} // internal static class DiagnosticDescriptors
