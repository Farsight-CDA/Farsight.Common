using Microsoft.Extensions.Hosting;

namespace Farsight.Common;

public static class FarsightCommonRegistry
{
    private static readonly List<Action<IHostApplicationBuilder>> _registrationActions = new();

    public static void Register(Action<IHostApplicationBuilder> action)
    {
        lock (_registrationActions)
        {
            _registrationActions.Add(action);
        }
    }

    public static void Apply(IHostApplicationBuilder builder)
    {
        Action<IHostApplicationBuilder>[] actions;
        lock (_registrationActions)
        {
            actions = _registrationActions.ToArray();
        }

        foreach (var action in actions)
        {
            action(builder);
        }
    }
}
