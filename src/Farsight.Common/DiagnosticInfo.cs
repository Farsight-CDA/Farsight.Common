using Microsoft.CodeAnalysis;

namespace Farsight.Common;

internal record struct DiagnosticInfo(
    string Id,
    string Title,
    string MessageFormat,
    string Category,
    DiagnosticSeverity Severity,
    Location? Location,
    string[] Args);
