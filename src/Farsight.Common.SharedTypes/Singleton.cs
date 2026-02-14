using Microsoft.Extensions.Logging;

namespace Farsight.Common;

public abstract class Singleton(IServiceProvider provider, ILogger logger)
{
    protected readonly IServiceProvider _provider = provider;
    protected readonly ILogger _logger = logger;
}
