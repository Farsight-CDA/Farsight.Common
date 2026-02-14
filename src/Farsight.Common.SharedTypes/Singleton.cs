using Microsoft.Extensions.Logging;

namespace Farsight.Common;

public abstract class Singleton
{
    protected readonly IServiceProvider _provider;
    protected readonly ILogger _logger;

    protected Singleton(IServiceProvider provider, ILogger logger)
    {
        _provider = provider;
        _logger = logger;
    }
}
