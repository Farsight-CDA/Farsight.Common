using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common;

public abstract class Singleton(IServiceProvider provider, ILogger logger, IHostApplicationLifetime lifetime) : IHostedService
{
    protected readonly IServiceProvider _provider = provider;
    protected readonly ILogger _logger = logger;
    protected readonly IHostApplicationLifetime _lifetime = lifetime;

    protected virtual Task InitializeAsync() => Task.CompletedTask;
    protected virtual Task RunAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
    protected virtual Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() => RunAsync(_lifetime.ApplicationStopping));
        return InitializeAsync();
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => StopAsync(cancellationToken);
}
