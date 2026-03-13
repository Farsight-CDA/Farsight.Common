# Farsight.Common

`Farsight.Common` is a `.NET 10` library that combines:

- Shared runtime types for Farsight-style hosted applications.
- A Roslyn incremental source generator that auto-registers services and configuration.

## What It Does

- Discovers classes annotated with `[ConfigOption]` and registers them with options binding + validation.
- Discovers classes inheriting `Singleton` (and `FarsightStartup`) and auto-registers them in DI.
- Generates constructor injection for `[Inject]` private readonly fields in `Singleton`/`FarsightStartup` classes.
- Applies generated registrations when `AddApplication<TStartup>()` or `AddApplicationOptions()` is called.

## Installation

```bash
dotnet add package Farsight.Common
```

The package targets `net10.0` and includes its source generator automatically.

## 1) Register It On Your App Builder

To activate generated registrations, call `AddApplication<TStartup>()` on your host builder:

```csharp
using Farsight.Common;
using Farsight.Common.Startup;

var builder = Host.CreateApplicationBuilder(args);
builder.AddApplication<BasicFarsightStartup>();

await builder.Build().RunAsync();
```

What this does:

- Adds your startup type (`TStartup`) as a hosted lifecycle service.
- Applies all source-generated options and service registrations collected in `FarsightCommonRegistry`.

If you need configuration binding during design-time only (for example EF tooling), use:

```csharp
builder.AddApplicationOptions();
```

This applies only `[ConfigOption]` registrations and skips singleton/service registrations.

Without this call, discovered `Singleton` and `[ConfigOption]` types will not be applied to DI/options.

## 2) What Startup Is For

`FarsightStartup` is the orchestrator for your application lifecycle. It coordinates discovered `Singleton` services in three phases:

- `SetupAsync`: parallel setup work before normal startup.
- `InitializeAsync`: ordered initialization work.
- `RunAsync`: long-running execution work.

`BasicFarsightStartup` is the default implementation that maps host lifecycle events to those phases:

- `StartingAsync` -> `SetupServicesAsync`
- `StartAsync` -> `InitializeServicesAsync`
- `StartedAsync` -> `RunServicesAsync`

If a singleton throws during `RunAsync` (except expected cancellation), startup logs a critical error and stops the host.

## 3) Capabilities

### Singleton Capabilities

Create a service by inheriting `Singleton` and declaring the class as `partial`:

```csharp
using Farsight.Common;

public sealed partial class Worker : Singleton
{
    [Inject]
    private readonly MyFeatureOptions _options;

    protected override Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Endpoint: {Endpoint}", _options.Endpoint);
        return Task.CompletedTask;
    }
}
```

When discovered by the generator:

- The class is registered as a singleton in DI.
- It is also registered under the `Singleton` base type so startup can discover and run it.
- A constructor is generated that provides framework dependencies (`IServiceProvider`, `ILogger<T>`, `IHostApplicationLifetime`) and assigns `[Inject]` fields.

### Config Capabilities

Define options by annotating a class with `[ConfigOption]`:

```csharp
using Farsight.Common;
using System.ComponentModel.DataAnnotations;

[ConfigOption(SectionName = "MyFeature")]
public sealed class MyFeatureOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
```

When discovered by the generator:

- `AddOptionsWithValidateOnStart<T>()` is registered.
- The type is bound from configuration root or `SectionName`.
- Section-bound options use strict binding by default, so unknown or misspelled keys fail startup instead of being ignored.
- `ValidateDataAnnotations()` is enabled.
- A singleton for the concrete options object is registered so it can be injected directly.

If you bind from the configuration root and still want strict binding, opt in explicitly:

```csharp
[ConfigOption(ErrorOnUnknownConfiguration = true)]
public sealed class RootOptions
{
    public string Endpoint { get; set; } = string.Empty;
}
```

You can also forward selected `BinderOptions` flags from the attribute:

```csharp
[ConfigOption(SectionName = "MyFeature", BindNonPublicProperties = true)]
public sealed class MyFeatureOptions
{
    internal string Endpoint { get; set; } = string.Empty;
}
```

You can also attach a FluentValidation validator without registering the validator in DI:

```csharp
using Farsight.Common;
using FluentValidation;

[ConfigOption<MyFeatureOptionsValidator>(SectionName = "MyFeature")]
public sealed class MyFeatureOptions
{
    public string Endpoint { get; set; } = string.Empty;
}

public sealed class MyFeatureOptionsValidator : AbstractValidator<MyFeatureOptions>
{
    public MyFeatureOptionsValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty();
    }
}
```

When the generic form is used:

- `ValidateDataAnnotations()` still runs.
- The generated options validation step creates `new TValidator()` on demand.
- The FluentValidation validator itself is not registered in DI.
- `TValidator` is constrained to `FluentValidation.IValidator` and `new()`.

## Generator Rules

- `FC001`: classes inheriting `Singleton` or `FarsightStartup` must be `partial`.
- `FC002`: fields marked with `[Inject]` must be `private readonly`.
