using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common;

/// <summary>
/// Base type for long-lived application services managed by <c>FarsightStartup</c>.
/// </summary>
/// <param name="provider">The root service provider.</param>
/// <param name="logger">The logger for this singleton.</param>
/// <param name="lifetime">The host application lifetime controller.</param>
public abstract class Singleton(IServiceProvider provider, ILogger logger, IHostApplicationLifetime lifetime) : ISingleton
{
    /// <summary>
    /// The root service provider for resolving framework services.
    /// </summary>
    protected readonly IServiceProvider _provider = provider;

    /// <summary>
    /// The logger associated with this singleton.
    /// </summary>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// The application lifetime used to coordinate shutdown.
    /// </summary>
    protected readonly IHostApplicationLifetime _lifetime = lifetime;

    /// <summary>
    /// Performs pre-initialization setup work.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when setup finishes.</returns>
    protected virtual Task SetupAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Performs initialization work that runs after setup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    protected virtual Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Performs long-running execution work.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>A task that represents the singleton runtime loop.</returns>
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
