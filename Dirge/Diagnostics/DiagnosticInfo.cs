
// (c) 2026 Kazuki Kohzuki

using Dirge.Utils;

namespace Dirge.Diagnostics;

internal record DiagnosticInfo(string Id, Location? Location, EquatableArray<string> Arguments)
{
    public static implicit operator Diagnostic(DiagnosticInfo diagnosticInfo)
        => DiagnosticDescriptors.GetDiagnostic(diagnosticInfo);
} // internal record DiagnosticInfo (string, Location?, EquatableArray<string>)