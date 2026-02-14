using System.Collections.Immutable;

namespace Farsight.Common;

internal record struct SingletonModel(
    string FullName,
    string Name,
    string Namespace,
    ImmutableArray<InjectedFieldModel> InjectedFields,
    ImmutableArray<DiagnosticInfo> Diagnostics,
    bool IsValid
);
