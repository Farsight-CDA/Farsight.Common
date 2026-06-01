using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farsight.Common;

/// <summary>
/// Base type for generated transient application services.
/// </summary>
/// <param name="provider">The current service provider.</param>
/// <param name="logger">The logger for this transient.</param>
/// <param name="lifetime">The host application lifetime controller.</param>
public abstract class Transient(IServiceProvider provider, ILogger logger, IHostApplicationLifetime lifetime)
{
    /// <summary>
    /// The current service provider for resolving framework services.
    /// </summary>
    protected readonly IServiceProvider _provider = provider;

    /// <summary>
    /// The logger associated with this transient.
    /// </summary>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// The application lifetime used to coordinate shutdown.
    /// </summary>
    protected readonly IHostApplicationLifetime _lifetime = lifetime;
}
