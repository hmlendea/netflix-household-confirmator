# API Reference

## Overview

This section documents the internal interfaces and public APIs of the Netflix Household Confirmator. The application has no external HTTP/gRPC API — all interfaces are for internal composition and testing.

## Interface Index

| Interface | Namespace | Purpose | Implementation |
|-----------|-----------|---------|----------------|
| `IHouseholdConfirmator` | `NetflixHouseholdConfirmator.Service` | Orchestrator contract | `HouseholdConfirmator` |
| `IEmailProcessor` | `NetflixHouseholdConfirmator.Service.Processors` | IMAP adapter contract | `EmailProcessor` |
| `INetflixProcessor` | `NetflixHouseholdConfirmator.Service.Processors` | Browser automation contract | `NetflixProcessor` |

## IHouseholdConfirmator

```csharp
namespace NetflixHouseholdConfirmator.Service;

public interface IHouseholdConfirmator
{
    void ConfirmIncomingHouseholdUpdateRequests();
}
```

**Description:** Main orchestration entry point. Runs the polling loop until process termination.

**Behaviour:**
1. Calls `IEmailProcessor.LogIn()`
2. Calls `IWebProcessor.LogIn()` (via `NetflixProcessor`)
3. Enters infinite loop:
   - Calls `IEmailProcessor.GetHouseholdConfirmationUrl()`
   - If non-null/non-empty, calls `INetflixProcessor.ConfirmHousehold(url)`
   - Sleeps 75 seconds
4. On any exception: propagates to caller
5. Finally: calls `IEmailProcessor.LogOut()`

**Exceptions:** Propagates all exceptions from dependencies.

## IEmailProcessor

```csharp
namespace NetflixHouseholdConfirmator.Service.Processors;

public interface IEmailProcessor
{
    void LogIn();
    void LogOut();
    string GetHouseholdConfirmationUrl();
}
```

**Description:** IMAP mailbox operations for retrieving Netflix household update emails.

### LogIn()

**Description:** Connects to IMAP server and authenticates.

**Steps:**
1. `ImapClient.Connect(server, port, true)` — SSL/TLS
2. `ImapClient.Authenticate(username, password)`

**Logging:** `MyOperation.EmailLogIn` with `Server`, `Port`, `Username` (not Password)

**Exceptions:** `ImapProtocolException`, `AuthenticationException`, `SslHandshakeException`, `SocketException`

### LogOut()

**Description:** Disconnects and disposes IMAP client.

**Steps:**
1. `ImapClient.Disconnect(true)`
2. `ImapClient.Dispose()`

**Logging:** `MyOperation.EmailLogOut` with `Server`, `Port`, `Username`

**Exceptions:** `ImapProtocolException`, `ObjectDisposedException`

### GetHouseholdConfirmationUrl()

**Description:** Retrieves recent emails, filters for Netflix household update subject, extracts confirmation URL.

**Algorithm:**
1. `ImapClient.Inbox.Open(FolderAccess.ReadOnly)`
2. Read messages newest-first by index, stopping at the first message older than `MaxEmailAge`
3. For each message (newest first):
   - Fetch full `MimeMessage`
   - If `Subject.Contains("How to update your Netflix Household")`:
     - Read the offset-bearing `email.Date` value
     - If `emailDateTime > lastConfirmationEmailDateTime`:
       - Update `lastConfirmationEmailDateTime`
       - Return `ExtractConfirmationUrlFromEmail(email)`
4. Return `null` if no qualifying email

**Duplicate Suppression:** In-memory `lastConfirmationEmailDateTime` (`DateTimeOffset`, resets on restart)

**Recovery:** Recoverable retrieval failures reconnect through `LogIn()` and retry, up to three attempts.

**Logging:** `MyOperation.ListenForConfirmationRequests`; a failed attempt logs through `MyOperation.EmailLogIn`

**Exceptions:** `ImapProtocolException`, `IOException`, `SocketException`, `TimeoutException`, `ServiceNotConnectedException`, `ServiceNotAuthenticatedException` (recoverable, retried), `AuthenticationException` (propagated), `NullReferenceException` (message without an HTML body)

## INetflixProcessor

```csharp
namespace NetflixHouseholdConfirmator.Service.Processors;

public interface INetflixProcessor
{
    void ConfirmHousehold(string confirmationUrl);
}
```

**Description:** Browser automation for confirming Netflix household update.

### ConfirmHousehold(string confirmationUrl)

**Description:** Navigates to confirmation URL and completes household confirmation flow.

**Steps:**
1. `webProcessor.GoToUrl(confirmationUrl)`
2. `webProcessor.WaitForAnyElementToBeVisible(ConfirmButtonSelector, LocationDetailsSelector)`
3. If `LocationDetailsSelector` visible:
   - Already confirmed — do nothing
4. Else:
   - `webProcessor.Click(ConfirmButtonSelector)`
   - `webProcessor.Wait(5000)` — wait for confirmation to process

**Selectors:**
- `ConfirmButtonSelector` = `//*[@data-uia='update-household-confirm-cta' or @data-uia='set-primary-location-action']`
- `LocationDetailsSelector` = `//div[@data-uia='location-details']`

**Logging:** `MyOperation.HouseholdConfirmation`

**Exceptions:** `WebDriverException`, `NoSuchElementException`, `TimeoutException`, `StaleElementReferenceException`

## Settings Classes

### BotSettings

```csharp
namespace NetflixHouseholdConfirmator.Configuration;

public sealed class BotSettings
{
    public int PageLoadTimeout { get; set; }
}
```

**Configuration Section:** `botSettings`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PageLoadTimeout` | int | 90 | Browser page load timeout (seconds) |

### DebugSettings

```csharp
namespace NetflixHouseholdConfirmator.Configuration;

public sealed class DebugSettings
{
    public string CrashScreenshotFileName { get; set; }
    public bool IsDebugMode { get; set; }
    public bool IsHeadless => !IsDebugMode;
    public bool IsCrashScreenshotEnabled => !string.IsNullOrWhiteSpace(CrashScreenshotFileName);
}
```

**Configuration Section:** `debugSettings`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CrashScreenshotFileName` | string | `""` | Screenshot filename on fatal error; empty disables |
| `IsDebugMode` | bool | `false` | Visible browser when `true`, headless when `false` |
| `IsHeadless` | bool | computed | `!IsDebugMode` |
| `IsCrashScreenshotEnabled` | bool | computed | `CrashScreenshotFileName` not empty |

### ImapSettings

```csharp
namespace NetflixHouseholdConfirmator.Configuration;

public sealed class ImapSettings
{
    public string Server { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int MaxEmailAge { get; set; }
}
```

**Configuration Section:** `imapSettings`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Server` | string | — | IMAP server hostname |
| `Port` | int | 993 | IMAP server port (SSL) |
| `Username` | string | — | IMAP username (email) |
| `Password` | string | — | IMAP password (app-specific) |
| `MaxEmailAge` | int | 1800 | Max email age to search (seconds) |

## Logging Vocabulary

### MyOperation

```csharp
namespace NetflixHouseholdConfirmator.Logging;

public sealed class MyOperation : Operation
{
    public static Operation EmailLogIn => new MyOperation(nameof(EmailLogIn));
    public static Operation EmailLogOut => new MyOperation(nameof(EmailLogOut));
    public static Operation HouseholdConfirmation => new MyOperation(nameof(HouseholdConfirmation));
    public static Operation ListenForConfirmationRequests => new MyOperation(nameof(ListenForConfirmationRequests));
}
```

### MyLogInfoKey

```csharp
namespace NetflixHouseholdConfirmator.Logging;

public sealed class MyLogInfoKey : LogInfoKey
{
    public static LogInfoKey Server => new MyLogInfoKey(nameof(Server));
    public static LogInfoKey Port => new MyLogInfoKey(nameof(Port));
    public static LogInfoKey Username => new MyLogInfoKey(nameof(Username));
    public static LogInfoKey Password => new MyLogInfoKey(nameof(Password)); // Never used
    public static LogInfoKey MaxAge => new MyLogInfoKey(nameof(MaxAge));
}
```

## NuciLog Integration

The application uses `NuciLogger` implementing `ILogger` from `NuciLog.Core`.

**Registration:**
```csharp
.AddSingleton<ILogger, NuciLogger>()
```

**Usage:**
```csharp
logger.Info(operation, status, message, logInfos);
logger.Error(operation, status, message, exception, logInfos);
logger.Fatal(operation, status, exception);
```

## NuciWeb Integration

**Interfaces:**
- `IWebProcessor` — Browser automation abstraction
- `IWebDriver` — Selenium WebDriver

**Implementation:** `SeleniumWebProcessor` (from `NuciWeb.Automation.Selenium`)

**Registration:**
```csharp
.AddSingleton<IWebDriver>(s => webDriver)
.AddSingleton<IWebProcessor, SeleniumWebProcessor>()
```