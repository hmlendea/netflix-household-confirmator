# Presentation Layer (Console Host)

## Overview

The presentation layer is the console application entry point — `Program.cs`. It handles process lifecycle, configuration loading, dependency injection setup, and top-level error handling.

## Responsibilities

1. **Configuration Loading** — Read and bind `appsettings.json`
2. **WebDriver Initialisation** — Create browser instance via `WebDriverInitialiser`
3. **Dependency Injection** — Register all services and settings
4. **Service Execution** — Run orchestrator
5. **Error Handling** — Catch fatal errors, log, capture screenshot
6. **Cleanup** — Quit WebDriver, log shutdown

## Program.cs Structure

### Static Fields
```csharp
static BotSettings botSettings;
static DebugSettings debugSettings;
static ImapSettings imapSettings;
static NuciLoggerSettings loggerSettings;

static IWebDriver webDriver;
static ILogger logger;
static IServiceProvider serviceProvider;
```

### Main Method
```csharp
static void Main(string[] args)
{
    LoadConfiguration();
    webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode, botSettings.PageLoadTimeout);

    serviceProvider = CreateIOC();
    logger = serviceProvider.GetService<ILogger>();
    IHouseholdConfirmator service = serviceProvider.GetService<IHouseholdConfirmator>();

    logger.Info(Operation.StartUp, "The service has started.");

    try
    {
        service.ConfirmIncomingHouseholdUpdateRequests();
    }
    catch (AggregateException ex)
    {
        LogInnerExceptions(ex);
        SaveCrashScreenshot();
    }
    catch (Exception ex)
    {
        logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
        SaveCrashScreenshot();
    }
    finally
    {
        webDriver?.Quit();
        logger.Info(Operation.ShutDown, "The service has stopped.");
    }
}
```

## Configuration Loading

```csharp
static IConfiguration LoadConfiguration()
{
    botSettings = new BotSettings();
    debugSettings = new DebugSettings();
    imapSettings = new ImapSettings();
    loggerSettings = new NuciLoggerSettings();

    IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", true, true)
        .Build();

    config.Bind(nameof(BotSettings), botSettings);
    config.Bind(nameof(DebugSettings), debugSettings);
    config.Bind(nameof(ImapSettings), imapSettings);
    config.Bind(nameof(NuciLoggerSettings), loggerSettings);

    return config;
}
```

- Creates default settings instances
- Loads `appsettings.json` (optional, reload on change)
- Binds each section to corresponding POCO
- **Note:** Reload on change enabled but POCOs not re-bound

## WebDriver Initialisation

```csharp
webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(debugSettings.IsDebugMode, botSettings.PageLoadTimeout);
```

- Delegates to `NuciWeb.Automation.Selenium.WebDriverInitialiser`
- Tries available WebDrivers (Chrome, Firefox, Edge)
- `IsDebugMode` → visible browser; `false` → headless
- `PageLoadTimeout` → navigation timeout

## Dependency Injection Container

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

**Registrations:**
| Service | Lifetime | Implementation |
|---------|----------|----------------|
| `BotSettings` | Singleton | Instance |
| `DebugSettings` | Singleton | Instance |
| `ImapSettings` | Singleton | Instance |
| `NuciLoggerSettings` | Singleton | Instance |
| `IEmailProcessor` | Singleton | `EmailProcessor` |
| `ILogger` | Singleton | `NuciLogger` |
| `IWebDriver` | Singleton | Factory (pre-created) |
| `IWebProcessor` | Singleton | `SeleniumWebProcessor` |
| `INetflixProcessor` | Singleton | `NetflixProcessor` |
| `IHouseholdConfirmator` | Singleton | `HouseholdConfirmator` |

## Error Handling

### AggregateException Unwrapping
```csharp
static void LogInnerExceptions(AggregateException exception)
{
    foreach (Exception innerException in exception.InnerExceptions)
    {
        if (innerException is not AggregateException innerAggregateException)
        {
            logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
        }
        else
        {
            LogInnerExceptions(innerAggregateException);
        }
    }
}
```

- Recursively unwraps nested `AggregateException`
- Logs each leaf exception as `Fatal`

### Crash Screenshot
```csharp
static void SaveCrashScreenshot()
{
    if (!debugSettings.IsCrashScreenshotEnabled)
    {
        return;
    }

    string directory = Path.GetDirectoryName(loggerSettings.LogFilePath);
    string filePath = Path.Combine(directory, debugSettings.CrashScreenshotFileName);

    ((ITakesScreenshot)webDriver)
        .GetScreenshot()
        .SaveAsFile(filePath);
}
```

- Only if `CrashScreenshotFileName` not empty
- Saves to same directory as log file
- Uses Selenium `ITakesScreenshot`

## Lifecycle

```
Main()
├── LoadConfiguration()
├── WebDriverInitialiser.InitialiseAvailableWebDriver()
├── CreateIOC()
├── logger.Info(StartUp)
├── try
│   └── service.ConfirmIncomingHouseholdUpdateRequests()  ← Runs until exception
├── catch AggregateException
│   └── LogInnerExceptions() + SaveCrashScreenshot()
├── catch Exception
│   └── logger.Fatal() + SaveCrashScreenshot()
└── finally
    ├── webDriver?.Quit()
    └── logger.Info(ShutDown)
```

## Logging

| Event | Operation | Status |
|-------|-----------|--------|
| Service start | `Operation.StartUp` | — |
| Fatal error (aggregate) | `Operation.Unknown` | `Failure` |
| Fatal error (single) | `Operation.Unknown` | `Failure` |
| Service stop | `Operation.ShutDown` | — |

## Testing

**No unit tests** for `Program.cs` — it's the composition root.

**Integration tests** exercise the full container via `HouseholdConfirmationIntegrationTests`.

## Known Issues

1. **No graceful shutdown** — `CancellationToken` not used; `Ctrl+C` kills process
2. **Configuration not re-bound** — `reloadOnChange: true` but POCOs static
3. **WebDriver created before DI** — Could be registered as factory in DI
4. **Static fields** — Makes testing difficult; consider instance-based
5. **No health check endpoint** — Cannot probe liveness externally