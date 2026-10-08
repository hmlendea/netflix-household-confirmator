# Application Services

## Overview

Application services contain the core business logic: orchestration (`HouseholdConfirmator`) and integration adapters (`EmailProcessor`, `NetflixProcessor`).

## Service Map

| Service | Interface | Responsibility |
|---------|-----------|----------------|
| `HouseholdConfirmator` | `IHouseholdConfirmator` | Orchestration: login, poll, confirm, logout |
| `EmailProcessor` | `IEmailProcessor` | IMAP: connect, search, filter, extract URL |
| `NetflixProcessor` | `INetflixProcessor` | Browser: navigate, detect state, confirm |

## HouseholdConfirmator (Orchestrator)

### Interface
```csharp
public interface IHouseholdConfirmator
{
    void ConfirmIncomingHouseholdUpdateRequests();
}
```

### Implementation
```csharp
public class HouseholdConfirmator(
    IEmailProcessor emailProcessor,
    INetflixProcessor netflixProcessor,
    ILogger logger) : IHouseholdConfirmator
{
    public void ConfirmIncomingHouseholdUpdateRequests()
    {
        try
        {
            emailProcessor.LogIn();

            logger.Info(
                MyOperation.ListenForConfirmationRequests,
                OperationStatus.Started,
                "Listening for incoming household update requests.");

            try
            {
                while(true)
                {
                    string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();

                    if (confirmationUrl is not null)
                    {
                        netflixProcessor.ConfirmHousehold(confirmationUrl);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.Error(
                    MyOperation.ListenForConfirmationRequests,
                    OperationStatus.Failure,
                    exception);
                throw;
            }
            finally
            {
                try
                {
                    emailProcessor.LogOut();
                }
                catch (Exception ex)
                {
                    logger.Error(
                        MyOperation.EmailLogOut,
                        OperationStatus.Failure,
                        "Failed to disconnect from the IMAP server during cleanup.",
                        ex);
                }
            }
        }
        catch (Exception exception)
        {
            logger.Error(
                MyOperation.ListenForConfirmationRequests,
                OperationStatus.Failure,
                exception);
            throw;
        }
    }
}
```

### Flow

```
ConfirmIncomingHouseholdUpdateRequests()
├── emailProcessor.LogIn()
├── Log: ListenForConfirmationRequests Started
├── while (true)
│   ├── confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl()
│   ├── if (confirmationUrl != null)
│   │   └── netflixProcessor.ConfirmHousehold(confirmationUrl)
│   └── (no sleep - tight loop!)
├── catch Exception
│   ├── Log: ListenForConfirmationRequests Failure
│   └── throw
└── finally
    └── emailProcessor.LogOut() (with own try/catch)
```

### Critical Issues

1. **No polling delay** — `while(true)` with no `Thread.Sleep` → 100% CPU, IMAP hammering
2. **Exception handling duplication** — Inner and outer try/catch both log same operation
3. **No cancellation support** — Cannot stop gracefully
4. **LogOut in finally** — Good, but nested try/catch swallows LogOut errors

### Dependencies

| Dependency | Used For |
|------------|----------|
| `IEmailProcessor` | IMAP operations (LogIn, GetHouseholdConfirmationUrl, LogOut) |
| `INetflixProcessor` | Browser confirmation (ConfirmHousehold) |
| `ILogger` | Structured logging |

### Logging

| Operation | Status | Context |
|-----------|--------|---------|
| `ListenForConfirmationRequests` | `Started` | Loop entry |
| `ListenForConfirmationRequests` | `Failure` | Any exception in loop |
| `EmailLogOut` | `Failure` | LogOut exception during cleanup |

## EmailProcessor (IMAP Adapter)

### Interface
```csharp
public interface IEmailProcessor
{
    void LogIn();
    void LogOut();
    string GetHouseholdConfirmationUrl();
}
```

### Key Implementation Details

**State:**
```csharp
private readonly ImapClient imapClient = new();
private DateTime lastConfirmationEmailDateTime = DateTime.Now;
```

**LogIn():**
- Connect SSL/TLS: `imapClient.Connect(server, port, true)`
- Authenticate: `imapClient.Authenticate(username, password)`
- Logs: `EmailLogIn` with Server, Port, Username

**LogOut():**
- Disconnect: `imapClient.Disconnect(true)`
- Dispose: `imapClient.Dispose()`
- Logs: `EmailLogOut` with Server, Port, Username

**GetHouseholdConfirmationUrl():**
1. Search inbox: `DeliveredAfter(Now - MaxEmailAge)`
2. Fetch summaries (UID, Envelope, BodyStructure)
3. For each message (newest first):
   - Fetch full MimeMessage
   - If subject contains "How to update your Netflix Household":
     - Get `email.Date.DateTime` (standard MIME Date header)
     - If newer than `lastConfirmationEmailDateTime`:
       - Update timestamp
       - Extract URL via regex
       - Return URL
4. Return null

**Duplicate Suppression:**
- In-memory `lastConfirmationEmailDateTime`
- Resets on process restart
- Uses standard `email.Date.DateTime` (fixed from `email.Headers["DateReceived"]`)

### Dependencies

| Dependency | Used For |
|------------|----------|
| `ImapSettings` | Server, port, credentials, max age |
| `ILogger` | Structured logging |

### Logging

| Operation | Status | Context |
|-----------|--------|---------|
| `EmailLogIn` | `Started` | Connecting |
| `EmailLogIn` | `InProgress` | Authenticating |
| `EmailLogIn` | `Success` | Logged in |
| `EmailLogIn` | `Failure` | Connect/auth error |
| `EmailLogOut` | `Started` | Disconnecting |
| `EmailLogOut` | `Success` | Logged out |
| `EmailLogOut` | `Failure` | Disconnect error |
| `ListenForConfirmationRequests` | `Started`/`Success`/`Failure` | Delegated from orchestrator |

## NetflixProcessor (Browser Adapter)

### Interface
```csharp
public interface INetflixProcessor
{
    void ConfirmHousehold(string confirmationUrl);
}
```

### Implementation
```csharp
public void ConfirmHousehold(string confirmationUrl)
{
    logger.Info(MyOperation.HouseholdConfirmation, OperationStatus.Started, "Starting...");

    try
    {
        webProcessor.GoToUrl(confirmationUrl);

        string confirmButtonSelector = Select.ByXPath(@"//button[@data-uia='set-primary-location-action']");
        string locationDetailsSelector = Select.ByXPath(@"//div[@data-uia='location-details']");

        webProcessor.WaitForAnyElementToBeVisible(confirmButtonSelector, locationDetailsSelector);

        if (!webProcessor.IsElementVisible(locationDetailsSelector))
        {
            webProcessor.Click(confirmButtonSelector);
            webProcessor.Wait(5000);
        }
    }
    catch (Exception exception)
    {
        logger.Error(MyOperation.HouseholdConfirmation, OperationStatus.Failure, "Error...", exception);
    }

    logger.Info(MyOperation.HouseholdConfirmation, OperationStatus.Success, "Successfully confirmed.");
}
```

### Flow

```
ConfirmHousehold(url)
├── Log: HouseholdConfirmation Started
├── GoToUrl(url)
├── WaitForAnyElement(confirmButton, locationDetails)
├── if (locationDetails NOT visible)
│   ├── Click(confirmButton)
│   └── Wait(5000)
├── catch Exception
│   └── Log: HouseholdConfirmation Failure (swallowed!)
└── Log: HouseholdConfirmation Success (ALWAYS - even after error!)
```

### Critical Issues

1. **Success logged after caught exception** — Misleading logs
2. **No success verification** — Assumes click works
3. **Hardcoded 5s wait** — Not condition-based
4. **Swallowed exceptions** — Orchestrator never knows confirmation failed
5. **Brittle selectors** — Netflix `data-uia` attributes

### Dependencies

| Dependency | Used For |
|------------|----------|
| `IWebProcessor` | Browser automation (NuciWeb) |
| `ILogger` | Structured logging |

### Logging

| Operation | Status | Context |
|-----------|--------|---------|
| `HouseholdConfirmation` | `Started` | Method entry |
| `HouseholdConfirmation` | `Failure` | Any exception (but continues) |
| `HouseholdConfirmation` | `Success` | Always logged (even after failure) |

## Service Interactions

```
HouseholdConfirmator
    ├── EmailProcessor.LogIn()
    ├── loop:
    │   ├── EmailProcessor.GetHouseholdConfirmationUrl()
    │   │   ├── Search inbox
    │   │   ├── Filter by subject
    │   │   ├── Check duplicate (timestamp)
    │   │   └── Extract URL (regex)
    │   └── NetflixProcessor.ConfirmHousehold(url)
    │       ├── GoToUrl
    │       ├── WaitForElements
    │       ├── Click/Wait (conditional)
    │       └── Log (misleading)
    └── EmailProcessor.LogOut()
```

## Testing

### Unit Tests

| Service | Test File | Coverage |
|---------|-----------|----------|
| `HouseholdConfirmator` | `HouseholdConfirmatorTests.cs` | Orchestration logic with mocks |
| `EmailProcessor` | `EmailProcessorTests.cs` | Construction only |
| `NetflixProcessor` | `NetflixProcessorTests.cs` | Browser interactions with mocks |

### Integration Tests

| Test File | Coverage |
|-----------|----------|
| `HouseholdConfirmationIntegrationTests.cs` | Full orchestration with real `NetflixProcessor`, mocked `IEmailProcessor`/`IWebProcessor` |

## Known Issues Summary

| Service | Issue | Severity |
|---------|-------|----------|
| `HouseholdConfirmator` | No polling delay (tight loop) | High |
| `HouseholdConfirmator` | No cancellation support | Medium |
| `HouseholdConfirmator` | Duplicate exception logging | Low |
| `EmailProcessor` | In-memory duplicate suppression | Medium |
| `EmailProcessor` | No IMAP retry/reconnect | Medium |
| `NetflixProcessor` | Success logged after failure | High |
| `NetflixProcessor` | No confirmation verification | High |
| `NetflixProcessor` | Swallowed exceptions | High |
| `NetflixProcessor` | Hardcoded waits/selectors | Medium |