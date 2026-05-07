# AspectCentral.DispatchProxy

[![Build Status](https://dev.azure.com/jamesconsulting/Aspect%20Central/_apis/build/status/jamesconsultingllc.AspectCentral.DispatchProxy?branchName=master)](https://dev.azure.com/jamesconsulting/Aspect%20Central/_build/latest?definitionId=24&branchName=master)
![Azure DevOps tests (branch)](https://img.shields.io/azure-devops/tests/jamesconsulting/Aspect%20Central/24/master)
![Azure DevOps coverage (branch)](https://img.shields.io/azure-devops/coverage/jamesconsulting/Aspect%20Central/24/master)

> Lightweight, dependency-injection-native **Aspect-Oriented Programming (AOP)** for .NET, built on
> [`System.Reflection.DispatchProxy`](https://learn.microsoft.com/dotnet/api/system.reflection.dispatchproxy).

`AspectCentral.DispatchProxy` lets you wrap any **interface-based** service registered in
`Microsoft.Extensions.DependencyInjection` with one or more cross-cutting concerns — logging,
profiling, caching, validation, retry, authorization — **without modifying the underlying class**.
Method calls go through a transparent runtime proxy that invokes your `PreInvoke` / `PostInvoke`
hooks around the original implementation.

The library is small (one assembly), allocation-conscious (uses the `[LoggerMessage]` source
generator), and ships first-class observability via the platform-standard
[`ActivitySource`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource) and
[`Meter`](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics.meter) — so traces and
metrics light up automatically when a consumer wires up OpenTelemetry.

---

## Table of Contents

- [Why AspectCentral?](#why-aspectcentral)
- [Installation](#installation)
- [Concepts](#concepts)
- [Quick Start](#quick-start)
- [Built-in Aspects](#built-in-aspects)
    - [Logging Aspect](#logging-aspect)
    - [Profiling Aspect](#profiling-aspect)
- [Writing a Custom Aspect](#writing-a-custom-aspect)
- [Method Filtering](#method-filtering)
- [Async Method Support](#async-method-support)
- [Observability](#observability)
- [How It Works (Internals)](#how-it-works-internals)
- [Target Frameworks & Compatibility](#target-frameworks--compatibility)
- [Building, Testing, Packing](#building-testing-packing)
- [Versioning](#versioning)
- [Contributing](#contributing)
- [License](#license)

---

## Why AspectCentral?

Most .NET AOP libraries fall into one of two camps:

1. **IL-rewriting / weaving** (PostSharp, Fody) — powerful but requires a build-time tool, can be
   hard to debug, and changes your assemblies.
2. **Castle DynamicProxy / Reflection.Emit** — runtime emit, large surface, dependency on
   `Castle.Core`.

`AspectCentral.DispatchProxy` takes a **third path**: it leans on the BCL's built-in
`System.Reflection.DispatchProxy`, which is purpose-built for transparent interface proxying.
That gives you:

- **No build-time weaving.** Your assemblies are untouched on disk.
- **No third-party proxy runtime.** Only BCL primitives and `Microsoft.Extensions.*`.
- **DI-native.** You compose aspects fluently against `IServiceCollection`.
- **Fully transparent.** Consumers resolve `IMyService` and get a proxy — no API change.
- **Async-aware.** Sync, `Task`, and `Task<T>` methods are all dispatched correctly so
  `PostInvoke` runs after the awaited result, not at the synchronous return of the proxy.
- **Observable by default.** Every intercepted call emits an `Activity` and records duration /
  invocation metrics. Hook them up to OpenTelemetry with one line.

---

## Installation

```bash
dotnet add package AspectCentral.DispatchProxy
```

This package depends on the `AspectCentral.Abstractions` package, which contributes
`IAspectRegistrationBuilder`, `AspectConfiguration`, `AspectContext`, `MethodTypeOptions`, and
related primitives. It is pulled in transitively — you do not need to install it explicitly.

---

## Concepts

| Concept                           | Type                                                                                                                           | Role                                                                                                                                                                         |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Aspect**                        | `BaseAspect<T>`                                                                                                                | The proxy itself. Holds the wrapped instance and exposes `PreInvoke` / `PostInvoke` hooks around every intercepted call.                                                     |
| **Aspect Factory**                | `IAspectFactory` / `BaseAspectFactory`                                                                                         | Knows how to construct an aspect of a given type around a given service instance. Registered as a singleton in DI and resolved per-request when the proxy chain is composed. |
| **Registration Builder**          | `IAspectRegistrationBuilder` (from `AspectCentral.Abstractions`), implemented here by `DispatchProxyAspectRegistrationBuilder` | Fluent API for declaring "wrap service X with aspects A, B, C." Builds an `AspectConfiguration`.                                                                             |
| **Aspect Configuration**          | `AspectConfiguration`                                                                                                          | The data model: which factories apply to which `(serviceType, implementationType)` pair, in what order, and which methods to intercept.                                      |
| **Aspect Configuration Provider** | `IAspectConfigurationProvider`                                                                                                 | Read-only view of the configuration consulted at proxy construction time and on every call (`ShouldIntercept(...)`).                                                         |
| **Aspect Context**                | `AspectContext`                                                                                                                | Per-call state: the target `MethodInfo`, arguments, return value, async kind, and a human-readable `InvocationString` for logs.                                              |

The proxy chain is composed inside out: the innermost call is the real service; each registered
aspect wraps the previous proxy in the order they were added.

---

## Quick Start

### 1. Register your service and add aspect support

```csharp
using AspectCentral.DispatchProxy;
using AspectCentral.DispatchProxy.Logging;
using AspectCentral.DispatchProxy.Profiling;

var services = new ServiceCollection();

services.AddLogging();                                 // ILogger<T> is required by aspects
services.AddSingleton<IGreeter, Greeter>();            // your real service

services.AddAspectSupport()                            // returns IAspectRegistrationBuilder
        .AddLoggingAspect()                            // every interface registered above gets logging
        .AddProfilingAspect();                         // ...and profiling, in that order
```

`AddAspectSupport()`:

1. Registers an in-memory `IAspectConfigurationProvider`.
2. Reflectively discovers every concrete `IAspectFactory` in loaded assemblies and registers each
   as a singleton in DI.
3. Returns a `DispatchProxyAspectRegistrationBuilder` you can chain against.

### 2. Resolve the service — get a proxy transparently

```csharp
var provider = services.BuildServiceProvider();
var greeter  = provider.GetRequiredService<IGreeter>(); // this is a DispatchProxy
greeter.Greet("Rudy");
```

You did not change `IGreeter` or `Greeter`. You did not call `Create` yourself. The DI container
hands you a proxy that calls `LoggingAspect → ProfilingAspect → Greeter.Greet`.

### 3. Apply aspects to specific services only

```csharp
services.AddAspectSupport()
        .AddLoggingAspect(typeof(IGreeter).GetMethod(nameof(IGreeter.Greet))!) // only this method
        .AddAspectViaFactory<MyAuditAspectFactory>();                          // for everything
```

`AddAspectViaFactory<TFactory>(int? sortOrder = null, params MethodInfo[] methodsToIntercept)`
gives you full control over both ordering and method filtering. Lower `sortOrder` runs first
(closest to the caller).

---

## Built-in Aspects

### Logging Aspect

`LoggingAspect<T>` writes `{Method}(args) Start`, `Return value : {value}` (when the method has a
return value), and `{Method}(args) End` at `Information` level using the source-generated
`AspectLogs` log methods (event IDs **2001 / 2002 / 2003**).

Register it in one of two ways:

```csharp
services.AddAspectSupport().AddLoggingAspect();                      // all DI-registered interfaces
services.AddAspectSupport().AddLoggingAspect(targetMethod);          // only the listed methods
services.AddAspectSupport().AddAspectViaFactory<LoggingAspectFactory>(sortOrder: 100);
```

The logger category is the implementation type's `FullName` (e.g. `MyApp.Services.Greeter`) so you
can filter by category in any logging provider.

### Profiling Aspect

`ProfilingAspect<T>` starts a `Stopwatch` in `PreInvoke` and writes `Runtime hh:mm:ss.ff` in
`PostInvoke` (event IDs **3001 / 3002**). It works correctly for `Task` and `Task<T>` methods —
the stopwatch stops after the awaited completion, not after the proxy synchronously hands back
the `Task`.

```csharp
services.AddAspectSupport().AddProfilingAspect();
services.AddAspectSupport().AddProfilingAspect(method1, method2);
services.AddAspectSupport().AddAspectViaFactory<ProfilingAspectFactory>(sortOrder: 200);
```

### Order matters

When you stack `AddLoggingAspect().AddProfilingAspect()` the proxy chain at runtime is
`Caller → Logging → Profiling → real service`. The profiling timer therefore measures **only the
real method**, not the logging overhead. Reverse the calls and the logging output will be
included in the timing.

---

## Writing a Custom Aspect

A custom aspect is **two small types**: an aspect (extends `BaseAspect<T>`) and a factory
(extends `BaseAspectFactory`). The factory is auto-discovered and registered when the consumer
calls `AddAspectSupport()` — you don't need a registration extension method, but providing one
keeps the call site fluent.

```csharp
using System.Reflection;
using AspectCentral.Abstractions;
using AspectCentral.Abstractions.Configuration;
using AspectCentral.DispatchProxy;
using Microsoft.Extensions.Logging;

public sealed class AuditAspect<T> : BaseAspect<T> where T : class?
{
    public static readonly Type Type = typeof(AuditAspect<>);

    public static T Create(
        T instance,
        Type implementationType,
        ILoggerFactory loggerFactory,
        IAspectConfigurationProvider provider,
        Type factoryType)
    {
        var proxy = (AuditAspect<T>)(object)Create<T, AuditAspect<T>>();
        proxy.Instance                     = instance;
        proxy.ObjectType                   = implementationType;
        proxy.Logger                       = loggerFactory.CreateLogger(implementationType.FullName ?? implementationType.Name);
        proxy.AspectConfigurationProvider  = provider;
        proxy.FactoryType                  = factoryType;
        return (T)(object)proxy;
    }

    public override void PreInvoke(AspectContext ctx)
        => Logger.LogInformation("[AUDIT] {User} -> {Call}", Environment.UserName, ctx.InvocationString);

    public override void PostInvoke(AspectContext ctx)
        => Logger.LogInformation("[AUDIT] {Call} completed", ctx.InvocationString);
}

public sealed class AuditAspectFactory(ILoggerFactory lf, IAspectConfigurationProvider p)
    : BaseAspectFactory(lf, p)
{
    public static readonly Type AuditAspectFactoryType = typeof(AuditAspectFactory);

    // Note: override methods cannot restate the generic constraint (CS0460);
    // the `where T : class?` from BaseAspectFactory.Create<T> is inherited.
    public override T Create<T>(T instance, Type implementationType)
        => AuditAspect<T>.Create(instance, implementationType, LoggerFactory, AspectConfigurationProvider, AuditAspectFactoryType);
}

// optional: a fluent extension for nicer call sites
public static class AuditRegistrationExtensions
{
    public static IAspectRegistrationBuilder AddAuditAspect(
        this IAspectRegistrationBuilder builder,
        params MethodInfo[] methodsToIntercept)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddAspect(AuditAspectFactory.AuditAspectFactoryType, methodsToIntercept: methodsToIntercept);
        return builder;
    }
}
```

You can short-circuit the underlying call by setting `aspectContext.InvokeMethod = false` and
populating `aspectContext.ReturnValue` yourself in `PreInvoke` — useful for caching aspects.

---

## Method Filtering

There are two filtering mechanisms, applied in this order:

1. **Per-aspect method list** — supplied at registration time via the `params MethodInfo[]
   methodsToIntercept` overload. An empty array means *every method on the interface*.
2. **`IAspectConfigurationProvider.ShouldIntercept(...)`** — consulted on every call. The default
   in-memory provider checks the per-aspect method list, so if you supply an explicit list, only
   methods in that list will trigger `PreInvoke` / `PostInvoke`. Methods that fail the check are
   still dispatched to the underlying service — they are just not wrapped.

```csharp
var greet = typeof(IGreeter).GetMethod(nameof(IGreeter.Greet))!;
services.AddAspectSupport().AddLoggingAspect(greet); // only Greet is logged
```

---

## Async Method Support

`BaseAspect<T>.Invoke` inspects the target method's return type and dispatches accordingly:

| Return type                      | Dispatch path                                           | When `PostInvoke` runs                                                 |
|----------------------------------|---------------------------------------------------------|------------------------------------------------------------------------|
| Synchronous (e.g. `int`, `void`) | `Process`                                               | Inline, after the call returns.                                        |
| `Task` (async action)            | `ProcessAction`                                         | After the returned `Task` is awaited inside the proxy.                 |
| `Task<TResult>` (async function) | `ProcessFunctionAsync<TResult>` (resolved reflectively) | After the awaited result is captured into `aspectContext.ReturnValue`. |

This means a profiling aspect on an `async Task<T>` method correctly measures the **end-to-end
async duration**, and a logging aspect logs the awaited result, not the unfinished `Task`.

`ValueTask` / `ValueTask<T>` are not currently a first-class case — they fall through to the
synchronous path.

---

## Observability

The library exposes a single `ActivitySource` and a single `Meter`. Both are also exposed as
`public const string` so consumers can register them without referencing internal fields.

### Activity Source

`AspectCentral.DispatchProxy.Aspects` — every intercepted call opens an `Activity` named
`{InterfaceName}.{MethodName}` with the following tags:

| Tag                     | Description                                         |
|-------------------------|-----------------------------------------------------|
| `code.namespace`        | Namespace of the interface declaring the method.    |
| `code.function`         | Method name.                                        |
| `aspect.factory`        | Full type name of the factory that built the proxy. |
| `aspect.target_type`    | Full type name of the underlying implementation.    |
| `aspect.interface_type` | Full type name of the proxied interface.            |

When the underlying call throws, the activity status is set to `Error` and an `exception` event
is recorded with `exception.type`, `exception.message`, and `exception.stacktrace`.

### Meter

`AspectCentral.DispatchProxy.Aspects` exposes:

| Instrument                   | Type                | Unit   | Tags                                   | Purpose                       |
|------------------------------|---------------------|--------|----------------------------------------|-------------------------------|
| `aspect.invocations`         | `Counter<long>`     | (none) | `aspect`, `status` (`success`/`error`) | Total intercepted calls.      |
| `aspect.invocation_duration` | `Histogram<double>` | `ms`   | `aspect`                               | Per-call wall-clock duration. |

### Wiring up OpenTelemetry

```csharp
using AspectCentral.DispatchProxy.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource(LibraryActivitySources.All.ToArray()))
    .WithMetrics(m => m.AddMeter(LibraryMeters.All.ToArray()));
```

`LibraryActivitySources.All` and `LibraryMeters.All` are `IReadOnlyList<string>` collections so
you can splat them into any OTel pipeline regardless of how many sources the library grows in the
future.

---

## How It Works (Internals)

The pipeline runs in three phases:

### Phase 1 — Registration

`services.AddAspectSupport()` does the following:

1. Calls `GetOrAddInMemoryProvider`, which:
    - Returns the existing `InMemoryAspectConfigurationProvider` if one was previously registered as
      an `ImplementationInstance` singleton.
    - Throws `InvalidOperationException` if an `IAspectConfigurationProvider` is already registered
      via type or factory (the existing instance cannot be observed up-front, so silent reuse would
      risk a provider mismatch at runtime).
    - Otherwise registers a new instance via
      `AddSingleton<IAspectConfigurationProvider>(provider)`.
2. Scans every loaded assembly via `AppDomain.CurrentDomain.GetAssemblies()` and finds every
   concrete `IAspectFactory`. Each is `TryAddSingleton`-registered against itself.
3. Returns a `DispatchProxyAspectRegistrationBuilder` for fluent chaining.

When you call `.AddLoggingAspect()` (or `.AddAspectViaFactory<T>()`), the builder records an
`AspectConfiguration` entry: `(serviceType → implementationType → [factoryType, sortOrder, methods])`.

### Phase 2 — Service-collection rewrite

When the service provider is built, `ConfigureAspects` (called from
`AddAspectSupport(IAspectConfigurationProvider)`) walks the `IServiceCollection`. For every
descriptor whose `ServiceType` is an interface and whose `ImplementationType` has an
`AspectConfiguration`, it:

1. Re-registers the implementation type as itself (concrete) with the original `Lifetime` so the
   real instance can still be resolved.
2. Replaces the original descriptor with a factory delegate that calls
   `InvokeCreateFactory(...)` → `CreateFactory<TService>(...)`.

`CreateFactory<TService>` composes the proxy chain by iterating the configured aspects in order
and wrapping each successive instance via `IAspectFactory.Create`.

### Phase 3 — Invocation

When a method is called on the outermost proxy, `BaseAspect<T>.Invoke`:

1. Builds an `AspectContext` (resolves the **implementation** `MethodInfo`, builds an
   `InvocationString`, sets `MethodType` to sync / async-action / async-function).
2. Starts an `Activity` and tags it.
3. Asks `IAspectConfigurationProvider.ShouldIntercept(...)`.
    - If yes → `PreInvoke` → dispatch on `MethodTypeOptions` → `PostInvoke` (inline for sync; via
      `ContinueWith` / async continuation for `Task`/`Task<T>`).
    - If no → dispatch directly without hooks.
4. Records `aspect.invocations` and `aspect.invocation_duration`.
5. Re-throws on exception with the activity marked `Error`.

The `MethodInfo` cache (`JamesConsulting.Constants.TypeMethods`) is populated lazily per
`ObjectType` to avoid repeated `Type.GetMethods()` calls on the hot path.

---

## Target Frameworks & Compatibility

The current package multi-targets:

- `net9.0`
- `net10.0`
- `netstandard2.1`

`netstandard2.1` covers .NET Core 3.x, Mono, and Xamarin / Unity workloads where applicable. The
modern TFMs unlock source-generated logging and `[ExcludeFromCodeCoverage]` enhancements.

---

## Building, Testing, Packing

The repository is a standard `dotnet` solution with a single library project and a single test
project (xUnit).

```bash
# Restore + build
dotnet restore
dotnet build AspectCentral.DispatchProxy.sln --configuration Debug --no-restore

# Run all tests across every TFM the test project targets
dotnet test AspectCentral.DispatchProxy.Tests/AspectCentral.DispatchProxy.Tests.csproj

# Run tests for a single TFM
dotnet test AspectCentral.DispatchProxy.Tests/AspectCentral.DispatchProxy.Tests.csproj --framework net10.0

# Run a single class or method (xUnit filter syntax)
dotnet test --filter "FullyQualifiedName~BaseAspectTests"

# Pack the NuGet (matches azure-pipelines.yml)
dotnet pack AspectCentral.DispatchProxy/AspectCentral.DispatchProxy.csproj --configuration Release
```

CI (`azure-pipelines.yml`) additionally runs SonarCloud analysis, signs the `.nupkg` with
`dotnet sign` against Azure Artifact Signing (workload-identity federation, no client secrets),
and publishes artifacts. Local builds get a `-local` suffix; Debug CI
builds get `-$(BUILD_BUILDNUMBER)-preview`.

---

## Versioning

The package follows [Semantic Versioning](https://semver.org/):

| Change                                                            | Bump                |
|-------------------------------------------------------------------|---------------------|
| Bug fix, perf improvement, internal refactor                      | **Patch** (`x.y.Z`) |
| New public API (backward-compatible)                              | **Minor** (`x.Y.0`) |
| Removal, signature change, or constraint tightening on public API | **Major** (`X.0.0`) |

Constraint changes on public generics (e.g. `where T : class?` → `where T : class`) are
treated as breaking and require a major bump.

---

## Contributing

1. Fork and create a feature branch from `develop` (`feature/short-name`).
2. Add a failing test under `AspectCentral.DispatchProxy.Tests` first — this repository follows
   TDD and BDD per [`AGENTS.md`](AGENTS.md).
3. Make the smallest change that turns the test green.
4. Run `dotnet test` across every TFM. Architecture tests under `ArchitectureTests.cs` enforce
   that the library does **not** depend on ASP.NET Core, hosting, or HTTP abstractions.
5. Open a PR back into `develop`.

For larger contributions please open an issue first to discuss the design.

---

## License

Released under the [MIT License](LICENSE). © James Consulting LLC.
