using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common;

public abstract class Singleton(IServiceProvider provider, ILogger logger, IHostApplicationLifetime lifetime) : ISingleton
{
    protected readonly IServiceProvider _provider = provider;
    protected readonly ILogger _logger = logger;
    protected readonly IHostApplicationLifetime _lifetime = lifetime;

    protected virtual Task SetupAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    Task ISingleton.SetupAsync(CancellationToken cancellationToken) => SetupAsync(cancellationToken);
    Task ISingleton.InitializeAsync(CancellationToken cancellationToken) => InitializeAsync(cancellationToken);
    Task ISingleton.RunAsync(CancellationToken cancellationToken) => RunAsync(cancellationToken);
}

internal interface ISingleton
{
    public Task SetupAsync(CancellationToken cancellationToken);
    public Task InitializeAsync(CancellationToken cancellationToken);
    public Task RunAsync(CancellationToken cancellationToken);
}
