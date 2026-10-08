# Logging

## Logging Framework

The application uses **NuciLog** (v1.2.1) with **NuciLog.Core** (v3.1.0) for structured logging. NuciLog provides:
- Operation-based logging with status tracking (Started, InProgress, Success, Failure)
- Structured `LogInfo` key-value pairs
- Multiple output sinks (console, file)
- Correlation via operation names

## Log Levels

| Level | Usage |
|-------|-------|
| `Debug` | Detailed diagnostic information (default minimum) |
| `Info` | General operational events (login, logout, confirmation) |
| `Warning` | Not currently used |
| `Error` | Failures with exception details |
| `Fatal` | Unhandled exceptions in `Program.Main` |

## Operations

Defined in `NetflixHouseholdConfirmator.Logging.MyOperation`:

| Operation | Description | Status Transitions |
|-----------|-------------|-------------------|
| `EmailLogIn` | IMAP connection and authentication | Started → InProgress → Success / Failure |
| `EmailLogOut` | IMAP disconnection and disposal | Started → Success / Failure |
| `HouseholdConfirmation` | Browser-based household confirmation | Started → Success / Failure |
| `ListenForConfirmationRequests` | Polling loop iteration | Started → Success / Failure |

## Structured Log Fields

Defined in `NetflixHouseholdConfirmator.Logging.MyLogInfoKey`:

| Key | Type | Source | Logged In |
|-----|------|--------|-----------|
| `Server` | string | `ImapSettings.Server` | `EmailLogIn`, `EmailLogOut` |
| `Port` | int | `ImapSettings.Port` | `EmailLogIn`, `EmailLogOut` |
| `Username` | string | `ImapSettings.Username` | `EmailLogIn`, `EmailLogOut` |
| `Password` | string | `ImapSettings.Password` | **Never logged** (excluded intentionally) |
| `MaxAge` | int | `ImapSettings.MaxEmailAge` | Not currently used |

## Log Output Format

### Console Output (NuciLog default)

```
[2024-01-15 10:30:45.123] [Info] [EmailLogIn] [Started] Connecting to the IMAP server. {Server=mail.example.com, Port=993}
[2024-01-15 10:30:45.456] [Info] [EmailLogIn] [InProgress] Authenticating on the IMAP server. {Server=mail.example.com, Port=993, Username=user@example.com}
[2024-01-15 10:30:45.789] [Info] [EmailLogIn] [Success] Logged into the IMAP server. {Server=mail.example.com, Port=993, Username=user@example.com}
```

### File Output (when enabled)

Same format as console, written to `logfile.log` (configurable via `nuciLoggerSettings.logFilePath`).

## Configuration

Controlled via `nuciLoggerSettings` section in `appsettings.json`:

```json
{
  "nuciLoggerSettings": {
    "minimumLevel": "Debug",
    "logFilePath": "logfile.log",
    "isFileOutputEnabled": true
  }
}
```

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `minimumLevel` | string | `Debug` | Minimum level to record |
| `logFilePath` | string | `logfile.log` | File path for file logging and crash screenshot base directory |
| `isFileOutputEnabled` | boolean | `true` | Enable file logging |

## Correlation

Each operation invocation creates a logical scope. The operation name (`MyOperation.*`) serves as the correlation identifier. Multiple log entries with the same operation name belong to the same logical operation.

Example correlation flow:
```
EmailLogIn:Started → EmailLogIn:InProgress → EmailLogIn:Success
ListenForConfirmationRequests:Started → ListenForConfirmationRequests:Success
HouseholdConfirmation:Started → HouseholdConfirmation:Success
EmailLogOut:Started → EmailLogOut:Success
```

## Exception Logging

Exceptions are logged with full stack trace via `logger.Error()`:

```csharp
logger.Error(
    MyOperation.EmailLogIn,
    OperationStatus.Failure,
    "Failed to connect to the IMAP server.",
    exception,  // Full exception with stack trace
    logInfos);
```

Unhandled exceptions in `Program.Main` are caught and logged as `Fatal`:

```csharp
catch (Exception exception)
{
    logger.Fatal(exception, "Fatal error occurred.");
    SaveCrashScreenshot(webDriver, debugSettings);
    throw;
}
```

## Crash Screenshots

When `debugSettings.IsCrashScreenshotEnabled` is `true` (non-empty `CrashScreenshotFileName`), a screenshot is captured on fatal error:

```csharp
private static void SaveCrashScreenshot(IWebDriver webDriver, DebugSettings debugSettings)
{
    if (webDriver is ITakesScreenshot screenshotDriver)
    {
        Screenshot screenshot = screenshotDriver.GetScreenshot();
        string filePath = Path.Combine(
            Path.GetDirectoryName(loggerSettings.LogFilePath) ?? ".",
            debugSettings.CrashScreenshotFileName);
        screenshot.SaveAsFile(filePath);
    }
}
```

Screenshot saved to same directory as log file, using `CrashScreenshotFileName` from config.

## Sensitive Data Handling

**IMAP password is never logged:**
- `MyLogInfoKey.Password` exists but is never added to `LogInfo` collections
- `EmailProcessor.LogIn` explicitly omits password from logInfos
- `EmailProcessor.LogOut` does not include password

**Other sensitive data:**
- Confirmation URLs contain one-time tokens — logged only at `Debug` level if at all
- Crash screenshots may contain visible page content — protect log directory

## Log Retention

**Current implementation:** No automatic rotation or retention. The log file grows indefinitely.

**Recommended:** Implement log rotation (size-based or time-based) or use external log management (e.g., logrotate, ELK, Seq).

## Testing Logging

Unit tests verify logging vocabulary contracts:
- `MyLogInfoKeyTests.cs` — Key names match property names
- `MyOperationTests.cs` — Operation names match property names

Integration tests do not assert on log output.

## Extending Logging

To add a new operation:
1. Add static property to `MyOperation.cs`
2. Use in processor/orchestrator via `logger.Info/Error(...)`

To add a new structured field:
1. Add static property to `MyLogInfoKey.cs`
2. Include in `LogInfo` collections where relevant

## Performance Considerations

- `LogInfo` collections created per log call — minimal allocation
- File I/O is synchronous — consider async sink for high-volume scenarios
- Structured logging adds overhead vs. plain text — acceptable for this workload