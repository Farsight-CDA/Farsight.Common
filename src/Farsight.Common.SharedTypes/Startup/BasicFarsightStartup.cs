using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common.Startup;

public sealed class BasicFarsightStartup(IServiceProvider provider, ILogger<FarsightStartup> logger, IHostApplicationLifetime lifetime)
    : FarsightStartup(provider, logger, lifetime)
{
    public override Task StartingAsync(CancellationToken cancellationToken)
        => SetupServicesAsync(cancellationToken);
    public override Task StartAsync(CancellationToken cancellationToken)
        => InitializeServicesAsync(cancellationToken);
    public override Task StartedAsync(CancellationToken cancellationToken)
        => RunServicesAsync(_lifetime.ApplicationStopping);
}
