using Microsoft.Extensions.Hosting;

namespace Farsight.Common;

/// <summary>
/// Stores source-generated registration actions and applies them to a host builder.
/// </summary>
public static class FarsightCommonRegistry
{
    private static readonly List<Action<IHostApplicationBuilder>> _optionRegistrationActions = [];
    private static readonly List<Action<IHostApplicationBuilder>> _serviceRegistrationActions = [];

    /// <summary>
    /// Registers service-related generated actions.
    /// </summary>
    /// <param name="action">The registration action to store.</param>
    public static void Register(Action<IHostApplicationBuilder> action)
        => RegisterServices(action);

    /// <summary>
    /// Registers options-related generated actions.
    /// </summary>
    /// <param name="action">The registration action to store.</param>
    public static void RegisterOptions(Action<IHostApplicationBuilder> action)
    {
        lock(_optionRegistrationActions)
        {
            _optionRegistrationActions.Add(action);
        }
    }

    /// <summary>
    /// Registers service-related generated actions.
    /// </summary>
    /// <param name="action">The registration action to store.</param>
    public static void RegisterServices(Action<IHostApplicationBuilder> action)
    {
        lock(_serviceRegistrationActions)
        {
            _serviceRegistrationActions.Add(action);
        }
    }

    /// <summary>
    /// Applies only options registrations to the host builder.
    /// </summary>
    /// <param name="builder">The host builder receiving registrations.</param>
    public static void ApplyOptions(IHostApplicationBuilder builder)
        => Apply(_optionRegistrationActions, builder);

    /// <summary>
    /// Applies only service registrations to the host builder.
    /// </summary>
    /// <param name="builder">The host builder receiving registrations.</param>
    public static void ApplyServices(IHostApplicationBuilder builder)
        => Apply(_serviceRegistrationActions, builder);

    /// <summary>
    /// Applies options and service registrations to the host builder.
    /// </summary>
    /// <param name="builder">The host builder receiving registrations.</param>
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
