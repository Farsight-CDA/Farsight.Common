using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common.Startup;

public abstract class FarsightStartup(IServiceProvider provider, ILogger logger, IHostApplicationLifetime lifetime) : IHostedLifecycleService
{
    protected readonly IServiceProvider _provider = provider;
    protected readonly ILogger _logger = logger;
    protected readonly IHostApplicationLifetime _lifetime = lifetime;

    private readonly ISingleton[] _singletons = [.. provider.GetServices<Singleton>().Select(x => (ISingleton) x)];

    protected async Task SetupServicesAsync(CancellationToken cancellationToken)
        => await Task.WhenAll(_singletons.Select(x => x.SetupAsync(cancellationToken)));

    protected async Task InitializeServicesAsync(CancellationToken cancellationToken)
    {
        foreach(var singleton in _singletons)
        {
            await singleton.InitializeAsync(cancellationToken);
        }
    }

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

    public virtual Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public virtual Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
