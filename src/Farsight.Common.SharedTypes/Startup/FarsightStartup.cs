using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common.Startup;

/// <summary>
/// Coordinates singleton setup, initialization, and run phases with host lifecycle events.
/// </summary>
/// <param name="provider">The root service provider.</param>
/// <param name="logger">The startup logger.</param>
/// <param name="lifetime">The host application lifetime controller.</param>
public abstract class FarsightStartup(IServiceProvider provider, ILogger logger, IHostApplicationLifetime lifetime) : IHostedLifecycleService
{
    /// <summary>
    /// The root service provider for resolving framework services.
    /// </summary>
    protected readonly IServiceProvider _provider = provider;

    /// <summary>
    /// The logger associated with startup orchestration.
    /// </summary>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// The application lifetime used to stop the host on fatal errors.
    /// </summary>
    protected readonly IHostApplicationLifetime _lifetime = lifetime;

    private readonly ISingleton[] _singletons = [.. provider.GetServices<Singleton>().Select(x => (ISingleton) x)];

    /// <summary>
    /// Runs setup for all discovered singletons in parallel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when setup finishes.</returns>
    protected async Task SetupServicesAsync(CancellationToken cancellationToken)
        => await Task.WhenAll(_singletons.Select(x => x.SetupAsync(cancellationToken)));

    /// <summary>
    /// Runs initialization for all discovered singletons sequentially.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    protected async Task InitializeServicesAsync(CancellationToken cancellationToken)
    {
        foreach(var singleton in _singletons)
        {
            await singleton.InitializeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Starts long-running singleton execution tasks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task representing startup of singleton runtime execution.</returns>
    protected async Task RunServicesAsync(CancellationToken cancellationToken)
        => _ = Task.WhenAll(_singletons.Select(x => Task.Run(async () =>
        {
            try
            {
                await x.RunAsync(cancellationToken);
            }
            catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "Singleton RunAsync failed, stopping application...");
                _lifetime.StopApplication();
            }
        })));

    /// <summary>
    /// Called before the host starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A completed task by default.</returns>
    public virtual Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called when the host starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A completed task by default.</returns>
    public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called after the host has started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A completed task by default.</returns>
    public virtual Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called before the host begins stopping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A completed task by default.</returns>
    public virtual Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called when the host is stopping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A completed task by default.</returns>
    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Called after the host has stopped.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A completed task by default.</returns>
    public virtual Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
