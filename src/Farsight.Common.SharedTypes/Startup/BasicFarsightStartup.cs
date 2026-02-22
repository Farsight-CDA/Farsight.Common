using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common.Startup;

/// <summary>
/// Default startup implementation that maps host lifecycle events to singleton phases.
/// </summary>
/// <param name="provider">The root service provider.</param>
/// <param name="logger">The startup logger.</param>
/// <param name="lifetime">The host application lifetime controller.</param>
public sealed class BasicFarsightStartup(IServiceProvider provider, ILogger<FarsightStartup> logger, IHostApplicationLifetime lifetime)
    : FarsightStartup(provider, logger, lifetime)
{
    /// <summary>
    /// Executes singleton setup during host starting.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when setup finishes.</returns>
    public override Task StartingAsync(CancellationToken cancellationToken)
        => SetupServicesAsync(cancellationToken);

    /// <summary>
    /// Executes singleton initialization during host start.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    public override Task StartAsync(CancellationToken cancellationToken)
        => InitializeServicesAsync(cancellationToken);

    /// <summary>
    /// Starts singleton runtime execution after host startup completes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when runtime tasks are launched.</returns>
    public override Task StartedAsync(CancellationToken cancellationToken)
        => RunServicesAsync(_lifetime.ApplicationStopping);
}
