# Host and Composition

## Overview

This document details the dependency injection container setup, service registration, and WebDriver lifecycle management in the Netflix Household Confirmator.

## Composition Root

**Location:** `Program.CreateIOC()`

```csharp
static IServiceProvider CreateIOC()
{
    return new ServiceCollection()
        .AddSingleton(botSettings)
        .AddSingleton(debugSettings)
        .AddSingleton(imapSettings)
        .AddSingleton(loggerSettings)
        .AddSingleton<IEmailProcessor, EmailProcessor>()
        .AddSingleton<ILogger, NuciLogger>()
        .AddSingleton<IWebDriver>(s => webDriver)
        .AddSingleton<IWebProcessor, SeleniumWebProcessor>()
        .AddSingleton<INetflixProcessor, NetflixProcessor>()
        .AddSingleton<IHouseholdConfirmator, HouseholdConfirmator>()
        .BuildServiceProvider();
}
```

## Service Registrations

### Settings (Singleton Instances)

| Service | Source | Notes |
|---------|--------|-------|
| `BotSettings` | `botSettings` field | Bound from `appsettings.json` |
| `DebugSettings` | `debugSettings` field | Bound from `appsettings.json` |
| `ImapSettings` | `imapSettings` field | Bound from `appsettings.json` |
| `NuciLoggerSettings` | `loggerSettings` field | Bound from `appsettings.json` |

- Registered as **instances** (not types)
- Created in `LoadConfiguration()` before container build
- Immutable after startup (no reload)

### Core Services (Singleton Types)

| Interface | Implementation | Dependencies |
|-----------|----------------|--------------|
| `IEmailProcessor` | `EmailProcessor` | `ImapSettings`, `ILogger` |
| `ILogger` | `NuciLogger` | `NuciLoggerSettings` (via NuciLog) |
| `IWebProcessor` | `SeleniumWebProcessor` | `IWebDriver` |
| `INetflixProcessor` | `NetflixProcessor` | `IWebProcessor`, `ILogger` |
| `IHouseholdConfirmator` | `HouseholdConfirmator` | `IEmailProcessor`, `INetflixProcessor`, `ILogger` |

### WebDriver (Singleton Factory)

```csharp
.AddSingleton<IWebDriver>(s => webDriver)
```

- `webDriver` created **before** container via `WebDriverInitialiser`
- Registered as factory returning pre-created instance
- Shared across all consumers

## Dependency Graph

```
Program.Main
    ├── LoadConfiguration() → Settings instances
    ├── WebDriverInitialiser.InitialiseAvailableWebDriver() → IWebDriver
    └── CreateIOC() → IServiceProvider
        ├── Settings (instances)
        ├── ILogger → NuciLogger
        ├── IWebDriver → factory (pre-created)
        ├── IWebProcessor → SeleniumWebProcessor
        │   └── IWebDriver
        ├── IEmailProcessor → EmailProcessor
        │   ├── ImapSettings
        │   └── ILogger
        ├── INetflixProcessor → NetflixProcessor
        │   ├── IWebProcessor
        │   └── ILogger
        └── IHouseholdConfirmator → HouseholdConfirmator
            ├── IEmailProcessor
            ├── INetflixProcessor
            └── ILogger
```

## Lifetime Management

### Singleton Justification

| Service | Reason |
|---------|--------|
| Settings | Immutable configuration; single source of truth |
| `ILogger` | NuciLogger designed as singleton; shared correlation |
| `IWebDriver` | Single browser instance; expensive to create |
| `IWebProcessor` | Stateless wrapper over WebDriver |
| `IEmailProcessor` | Maintains IMAP connection state (`ImapClient`) |
| `INetflixProcessor` | Stateless; could be transient but singleton fine |
| `IHouseholdConfirmator` | Maintains polling loop state (`lastConfirmationEmailDateTime` in EmailProcessor) |

### No Scoped/Transient Services

- No HTTP requests → no request scope
- No multi-tenancy → no per-operation scope
- Simple console application → singleton sufficient

## WebDriver Lifecycle

### Creation
```csharp
// Program.Main, before DI
webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(
    debugSettings.IsDebugMode, 
    botSettings.PageLoadTimeout);
```

- Tries Chrome, Firefox, Edge in order
- `IsDebugMode=true` → visible browser
- `IsDebugMode=false` → headless
- `PageLoadTimeout` → navigation timeout

### Registration
```csharp
.AddSingleton<IWebDriver>(s => webDriver)
```

- Factory captures `webDriver` variable
- Same instance returned to all consumers

### Disposal
```csharp
// Program.Main, finally block
webDriver?.Quit();
```

- Explicit `Quit()` in `finally` ensures cleanup
- Not managed by DI container (created outside)
- `Dispose()` not called on `IWebDriver` (Selenium uses `Quit()`)

## NuciLog Integration

### Logger Registration
```csharp
.AddSingleton<ILogger, NuciLogger>()
```

- `NuciLogger` implements `NuciLog.Core.ILogger`
- Configured via `NuciLoggerSettings` (bound from config)
- NuciLog internally uses `NuciLoggerSettings` for sinks/levels

### Logger Usage in Services

Services receive `ILogger` via constructor injection:
```csharp
public sealed class EmailProcessor(ImapSettings imapSettings, ILogger logger)
public sealed class NetflixProcessor(IWebProcessor webProcessor, ILogger logger)
public sealed class HouseholdConfirmator(IEmailProcessor emailProcessor, INetflixProcessor netflixProcessor, ILogger logger)
```

## Configuration Binding Order

1. `LoadConfiguration()` creates settings instances
2. `CreateIOC()` registers settings instances
3. Services receive settings via constructor injection
4. Services use settings during operation

## Testing Composition

### Unit Tests
- Manual mock composition in `[SetUp]`
- No DI container used
- Direct instantiation with mocks

### Integration Tests
- Real `HouseholdConfirmator` with real `NetflixProcessor`
- Mocked `IEmailProcessor` and `IWebProcessor`
- Manual composition in `[SetUp]`

## Extending Composition

### Adding a New Service

1. Define interface (e.g., `INewService`)
2. Create implementation (e.g., `NewService`)
3. Add registration in `CreateIOC()`:
   ```csharp
   .AddSingleton<INewService, NewService>()
   ```
4. Inject into consumers via constructor

### Adding a New Setting

1. Create settings class in `Configuration/`
2. Add field in `Program.cs`
3. Instantiate in `LoadConfiguration()`
4. Bind in `LoadConfiguration()`
5. Register instance in `CreateIOC()`
6. Inject into services

## Known Issues

1. **WebDriver outside DI** — Created before container; not managed by DI
2. **Static settings fields** — Hard to test; consider `IOptions<T>` pattern
3. **No configuration reload** — `IOptionsMonitor` not used
4. **No `IHost`/`IHostedService`** — Console app pattern; no graceful shutdown
5. **NuciLoggerSettings not injected** — NuciLog reads config internally