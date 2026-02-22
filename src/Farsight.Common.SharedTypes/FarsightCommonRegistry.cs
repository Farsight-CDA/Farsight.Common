using Microsoft.Extensions.Hosting;

namespace Farsight.Common;

public static class FarsightCommonRegistry
{
    private static readonly List<Action<IHostApplicationBuilder>> _optionRegistrationActions = [];
    private static readonly List<Action<IHostApplicationBuilder>> _serviceRegistrationActions = [];

    public static void Register(Action<IHostApplicationBuilder> action)
        => RegisterServices(action);

    public static void RegisterOptions(Action<IHostApplicationBuilder> action)
    {
        lock(_optionRegistrationActions)
        {
            _optionRegistrationActions.Add(action);
        }
    }

    public static void RegisterServices(Action<IHostApplicationBuilder> action)
    {
        lock(_serviceRegistrationActions)
        {
            _serviceRegistrationActions.Add(action);
        }
    }

    public static void ApplyOptions(IHostApplicationBuilder builder)
        => Apply(_optionRegistrationActions, builder);

    public static void ApplyServices(IHostApplicationBuilder builder)
        => Apply(_serviceRegistrationActions, builder);

    public static void Apply(IHostApplicationBuilder builder)
    {
        ApplyOptions(builder);
        ApplyServices(builder);
    }

    private static void Apply(List<Action<IHostApplicationBuilder>> registrationActions, IHostApplicationBuilder builder)
    {
        Action<IHostApplicationBuilder>[] actions;
        lock(registrationActions)
        {
            actions = [.. registrationActions];
        }

        foreach(var action in actions)
        {
            action(builder);
        }
    }
}
