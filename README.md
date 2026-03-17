# Farsight.Common

[![NuGet](https://img.shields.io/nuget/v/Farsight.Common.svg)](https://www.nuget.org/packages/Farsight.Common/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A .NET 10 library providing shared runtime types and a Roslyn incremental source generator for Farsight-style hosted applications. The generator discovers annotated types and auto-registers services and configuration bindings at compile time, eliminating runtime reflection and manual DI registration. Fully compatible with AOT compilation.

---

## Features

- **Compile-time service registration** — Annotated singletons and options are registered via source generation
- **Zero runtime reflection** — All discovery happens at build time
- **AOT compatible** — No reflection-based code paths; safe for native AOT publishing
- **Strict configuration binding** — Unknown configuration keys fail at startup
- **Validation support** — Data annotations and FluentValidation integration

---

## 🚀 Quick Start

### 1. Install the Package

```bash
dotnet add package Farsight.Common
```

### 2. Define Your Configuration

```csharp
using Farsight.Common;
using System.ComponentModel.DataAnnotations;

[ConfigOption(SectionName = "MyApp")]
public class AppOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
```

### 3. Create a Service

```csharp
using Farsight.Common;

public sealed partial class Worker : Singleton
{
    [Inject]
    private readonly AppOptions _options;

    protected override Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running on: {Endpoint}", _options.Endpoint);
        return Task.CompletedTask;
    }
}
```

### 4. Wire Up Your Application

```csharp
using Farsight.Common;
using Farsight.Common.Startup;

var builder = Host.CreateApplicationBuilder(args);
builder.AddApplication<BasicFarsightStartup>();

await builder.Build().RunAsync();
```

---

## 📋 What Gets Generated

| Attribute | What It Does |
|-----------|-------------|
| `[ConfigOption]` | Binds config section → options class with validation |
| `[ConfigOption<TValidator>]` | Adds FluentValidation support |
| `Singleton` (base class) | Registers as singleton + generates constructor injection |
| `[Inject]` | Auto-wires dependencies into private fields |

---

## 🏗️ Application Lifecycle

`BasicFarsightStartup` orchestrates your `Singleton` services through three phases:

1. **Setup** — Parallel preparation work
2. **Initialize** — Ordered initialization
3. **Run** — Long-running execution

```csharp
public sealed partial class MyService : Singleton
{
    protected override Task SetupAsync(CancellationToken ct) 
        => Task.CompletedTask;
    
    protected override Task InitializeAsync(CancellationToken ct) 
        => Task.CompletedTask;
    
    protected override Task RunAsync(CancellationToken ct) 
        => Task.CompletedTask;
}
```

---

## 🔧 Advanced Configuration

### FluentValidation Support

```csharp
[ConfigOption<MyOptionsValidator>(SectionName = "Features")]
public class MyOptions 
{ 
    public string Value { get; set; } = string.Empty;
}

public class MyOptionsValidator : AbstractValidator<MyOptions>
{
    public MyOptionsValidator() => RuleFor(x => x.Value).NotEmpty();
}
```


### Design-Time Configuration

For EF tooling or design-time scenarios:

```csharp
builder.AddApplicationOptions(); // Only config, no services
```

---

## 📦 Requirements

- .NET 10.0+
- The source generator is included automatically

---

## 📝 License

MIT License — see [LICENSE](LICENSE) for details.
