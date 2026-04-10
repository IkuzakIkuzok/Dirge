
// (c) 2026 Kazuki Kohzuki

namespace Dirge.Diagnostics;

internal static class DiagnosticDescriptors
{
    internal static string ReadonlyStructNotSupportedId => _readonlyStructNotSupportedId;
    internal static string TypeMustBePartialId => _typeMustBePartialId;
    internal static string DisposeInNonDisposableBaseId => _disposeInNonDisposableBaseId;
    internal static string MissingAccessibleDisposeBoolId => _missingAccessibleDisposeBoolId;
    internal static string DoNotDisposeWhenTargetMustBeBoolFieldId => _doNotDisposeWhenTargetMustBeBoolFieldId;

    private const string _readonlyStructNotSupportedId = "DIRGE001";
    private const string _typeMustBePartialId = "DIRGE002";
    private const string _disposeInNonDisposableBaseId = "DIRGE003";
    private const string _missingAccessibleDisposeBoolId = "DIRGE004";
    private const string _doNotDisposeWhenTargetMustBeBoolFieldId = "DIRGE005";

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

#endif
} // internal static class DiagnosticDescriptors
