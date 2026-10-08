# State and Persistence

## Overview

The application maintains minimal in-memory state only. No persistent storage, databases, or distributed state management.

## In-Memory State

### EmailProcessor State

| Field | Type | Initial Value | Updated | Scope |
|-------|------|---------------|---------|-------|
| `imapClient` | `ImapClient` | `new()` | `LogIn()`/`LogOut()` | Process |
| `lastConfirmationEmailDateTime` | `DateTime` | `DateTime.Now` | `GetHouseholdConfirmationUrl()` | Process |

### Program State

| Field | Type | Initial Value | Updated | Scope |
|-------|------|---------------|---------|-------|
| `webDriver` | `IWebDriver` | `null` | `Main()` startup | Process |
| `serviceProvider` | `IServiceProvider` | `null` | `Main()` startup | Process |
| `logger` | `ILogger` | `null` | `Main()` startup | Process |
| Settings POCOs | Various | `new()` | `LoadConfiguration()` | Process |

### HouseholdConfirmator State

**None** — Stateless orchestrator (delegates to processors)

### NetflixProcessor State

**None** — Stateless (delegates to `IWebProcessor`)

## Persistence

### Configuration File
- **File:** `appsettings.json`
- **Format:** JSON
- **Read:** Once at startup
- **Write:** Never (manual edit only)
- **Schema:** See [Configuration](configuration.md)

### Log File
- **File:** `logfile.log` (configurable)
- **Format:** NuciLog text format
- **Write:** Continuous during operation
- **Rotation:** None (grows indefinitely)
- **Retention:** Manual

### Crash Screenshot
- **File:** `crash.png` (configurable)
- **Format:** PNG
- **Write:** On fatal exception (if enabled)
- **Overwrites:** Yes (single file)

## No Persistent Stores

| Store Type | Used? | Notes |
|------------|-------|-------|
| Database | No | — |
| File-based state | No | Only config/logs/screenshots |
| Cache | No | In-memory only |
| Message queue | No | Direct polling |
| Distributed lock | No | Single instance |
| Session store | No | Browser session in memory |

## State Transitions

### Startup
```
Process Start
    → LoadConfiguration() → Settings populated
    → WebDriverInitialiser → webDriver created
    → CreateIOC() → serviceProvider created
    → EmailProcessor.LogIn() → imapClient connected
    → Polling loop begins
```

### Steady State
```
Polling Loop (infinite)
    → GetHouseholdConfirmationUrl()
        → Search IMAP
        → If new email: update lastConfirmationEmailDateTime
    → If URL: ConfirmHousehold()
        → Browser navigation/interaction
    → (No sleep - tight loop)
```

### Shutdown
```
Exception or Ctrl+C
    → Catch in Main()
    → Log Fatal
    → SaveCrashScreenshot() (if enabled)
    → finally:
        → webDriver.Quit()
        → EmailProcessor.LogOut() (via orchestrator finally)
        → Log ShutDown
    → Process Exit
```

## Duplicate Suppression

**Mechanism:** Timestamp comparison in `EmailProcessor`

```csharp
private DateTime lastConfirmationEmailDateTime = DateTime.Now;

DateTime emailDateTime = email.Date.DateTime;
if (emailDateTime > lastConfirmationEmailDateTime)
{
    lastConfirmationEmailDateTime = emailDateTime;
    return ExtractConfirmationUrlFromEmail(email);
}
```

**Limitations:**
- Resets on restart (`DateTime.Now`)
- No UID tracking — relies on server timestamp
- Clock skew between app and mail server not handled
- Single instance only

## Caches

**None** — No caching layer. Each polling iteration searches IMAP fresh.

## Migrations

**Not applicable** — No schema, no persistent data model.

## Backup/Recovery

| Artifact | Backup Needed? | Recovery |
|----------|----------------|----------|
| `appsettings.json` | Yes | Manual restore |
| `logfile.log` | Optional | Rotate/archive |
| `crash.png` | No | Overwritten on next crash |
| In-memory state | No | Lost on restart (by design) |

## Scaling Implications

**Single-instance only** — State is not shareable:
- `lastConfirmationEmailDateTime` not synchronised
- `ImapClient` not shareable
- `IWebDriver` not shareable
- No leader election

To scale: Run multiple instances with disjoint email filters (not supported).