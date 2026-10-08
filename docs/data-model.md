# Data Model

## Overview

The application has a minimal domain model — no persistent entities, no database, no ORM. Data flows as primitive values and DTOs through the pipeline.

## Domain Concepts

| Concept | Representation | Persistence |
|---------|----------------|-------------|
| IMAP Settings | `ImapSettings` (POCO) | `appsettings.json` |
| Bot Settings | `BotSettings` (POCO) | `appsettings.json` |
| Debug Settings | `DebugSettings` (POCO) | `appsettings.json` |
| Logger Settings | `NuciLoggerSettings` (POCO) | `appsettings.json` |
| Confirmation URL | `string` | In-memory only |
| Email Timestamp | `DateTime` | In-memory only |
| IMAP Connection | `ImapClient` | Process lifetime |
| WebDriver Session | `IWebDriver` | Process lifetime |

## Settings Classes (Configuration Model)

### BotSettings
```csharp
public sealed class BotSettings
{
    public int PageLoadTimeout { get; set; }
}
```
- **Source:** `appsettings.json` → `botSettings` section
- **Used by:** `WebDriverInitialiser`, `NetflixProcessor` (indirectly)

### DebugSettings
```csharp
public sealed class DebugSettings
{
    public string CrashScreenshotFileName { get; set; }
    public bool IsDebugMode { get; set; }
    public bool IsHeadless => !IsDebugMode;
    public bool IsCrashScreenshotEnabled => !string.IsNullOrWhiteSpace(CrashScreenshotFileName);
}
```
- **Source:** `appsettings.json` → `debugSettings` section
- **Computed properties:** `IsHeadless`, `IsCrashScreenshotEnabled`
- **Used by:** `Program` (WebDriver, screenshot), `EmailProcessor` (indirectly)

### ImapSettings
```csharp
public sealed class ImapSettings
{
    public string Server { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int MaxEmailAge { get; set; }
}
```
- **Source:** `appsettings.json` → `imapSettings` section
- **Used by:** `EmailProcessor` (connection, search)

### NuciLoggerSettings
```csharp
// From NuciLog.Configuration
public class NuciLoggerSettings
{
    public string MinimumLevel { get; set; }
    public string LogFilePath { get; set; }
    public bool IsFileOutputEnabled { get; set; }
}
```
- **Source:** `appsettings.json` → `nuciLoggerSettings` section
- **Used by:** `NuciLogger` (internal)

## Runtime Data Flow

### Email Processing
```
IMAP Message (MimeMessage)
    ├── Subject (string) → Filter
    ├── Date (DateTimeOffset) → Duplicate check
    ├── HtmlBody (string) → URL extraction
    ├── TextBody (string) → Fallback URL extraction
    └── Headers (HeaderList) → Not used (fixed)
```

### Confirmation URL
```
Regex Match → string (URL or raw body)
    → NetflixProcessor.ConfirmHousehold(string)
    → IWebProcessor.GoToUrl(string)
```

### Duplicate Suppression State
```
EmailProcessor.lastConfirmationEmailDateTime : DateTime
    Initial: DateTime.Now
    Updated: When newer qualifying email found
    Scope: Process lifetime
    Persistence: None
```

## External Data Models

### IMAP Message (MailKit/MimeKit)
- `MimeMessage` — Full message with headers, body parts
- `MessageSummary` — Lightweight metadata (UID, envelope)
- `UniqueId` — IMAP UID (not currently tracked)

### Selenium/WebDriver
- `IWebDriver` — Browser session
- `IWebElement` — Page elements
- `By` / `Select` — Locator strategies

## No Domain Entities

The application does not define:
- User/Account entities
- Confirmation request entities
- Audit/log entities
- Configuration entities (beyond settings POCOs)

## Serialisation

| Data | Format | Direction |
|------|--------|-----------|
| Settings | JSON | File → Object (startup) |
| Logs | NuciLog format | Object → File/Console |
| Screenshots | PNG | WebDriver → File (on crash) |

## Validation

**Current:** None — Invalid config causes runtime failures.

**Recommended:** Add validation attributes or FluentValidation:
```csharp
public sealed class ImapSettings
{
    [Required] public string Server { get; set; }
    [Range(1, 65535)] public int Port { get; set; }
    [Required] public string Username { get; set; }
    [Required] public string Password { get; set; }
    [Range(1, int.MaxValue)] public int MaxEmailAge { get; set; }
}
```

## Versioning

No data model versioning — settings are flat POCOs bound directly from JSON.