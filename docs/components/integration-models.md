# Integration Models

## Overview

Integration models are the adapters that connect the application to external systems: IMAP (email) and Selenium/WebDriver (browser automation).

## IMAP Integration (EmailProcessor)

### Library
- **MailKit 4.18.1** — IMAP client
- **MimeKit** — MIME message parsing (transitive via MailKit)

### Connection Model

```csharp
private readonly ImapClient imapClient = new();

// LogIn()
imapClient.Connect(imapSettings.Server, imapSettings.Port, true); // SSL
imapClient.Authenticate(imapSettings.Username, imapSettings.Password);

// LogOut()
imapClient.Disconnect(true);
imapClient.Dispose();
```

### Message Retrieval Model

```csharp
// Search
var query = SearchQuery.DeliveredAfter(
    DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(imapSettings.MaxEmailAge)));
var uids = imapClient.Inbox.Search(query);

// Fetch summaries
var summaries = imapClient.Inbox.Fetch(uids, 
    MessageSummaryItems.UniqueId | 
    MessageSummaryItems.Envelope | 
    MessageSummaryItems.BodyStructure);

// Fetch full message (per candidate)
var email = imapClient.Inbox.GetMessage(uid);
```

### Message Filtering Model

```csharp
const string HouseholdUpdateEmailSubject = "How to update your Netflix Household";

if (email.Subject.Contains(HouseholdUpdateEmailSubject))
{
    DateTime emailDateTime = email.Date.DateTime; // Standard MIME Date header
    if (emailDateTime > lastConfirmationEmailDateTime)
    {
        lastConfirmationEmailDateTime = emailDateTime;
        return ExtractConfirmationUrlFromEmail(email);
    }
}
```

### URL Extraction Model

```csharp
private static string ConfirmationUrlPattern
    => ".*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*";

private string ExtractConfirmationUrlFromEmail(MimeMessage email)
{
    string body = email.HtmlBody ?? email.TextBody ?? string.Empty;
    return Regex.Replace(body, ConfirmationUrlPattern, "$1");
}
```

### Error Model

| Exception | Source | Meaning |
|-----------|--------|---------|
| `ImapProtocolException` | MailKit | Protocol-level error |
| `ImapCommandException` | MailKit | Command failed |
| `SslHandshakeException` | MailKit/BouncyCastle | TLS failure |
| `AuthenticationException` | MailKit | Invalid credentials |
| `SocketException` | System | Network failure |
| `ObjectDisposedException` | MailKit | Client disposed |

### Configuration Mapping

| ImapSettings Property | MailKit Usage |
|----------------------|---------------|
| `Server` | `Connect(server, ...)` |
| `Port` | `Connect(..., port, ...)` |
| `Username` | `Authenticate(username, ...)` |
| `Password` | `Authenticate(..., password)` |
| `MaxEmailAge` | `DeliveredAfter(Now - MaxEmailAge)` |

## Browser Automation Integration (NetflixProcessor)

### Library Stack
- **NuciWeb 4.0.0** — Abstraction layer
- **NuciWeb.Automation 1.0.0** — Automation interfaces
- **NuciWeb.Automation.Selenium 1.0.2** — Selenium implementation
- **Selenium.WebDriver** — Browser driver (transitive)

### Abstraction Layer

```
INetflixProcessor (app interface)
    └── NetflixProcessor (implementation)
        └── IWebProcessor (NuciWeb abstraction)
            └── SeleniumWebProcessor (NuciWeb.Selenium)
                └── IWebDriver (Selenium)
                    └── ChromeDriver / GeckoDriver / EdgeDriver
```

### WebDriver Initialisation

```csharp
// Program.cs
webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(
    debugSettings.IsDebugMode, 
    botSettings.PageLoadTimeout);
```

- Tries Chrome, Firefox, Edge in order
- `IsDebugMode` → visible; `false` → headless
- `PageLoadTimeout` → navigation timeout

### WebProcessor Operations Used

| Operation | NuciWeb Method | Purpose |
|-----------|----------------|---------|
| Navigate | `GoToUrl(url)` | Load confirmation page |
| Wait for elements | `WaitForAnyElementToBeVisible(sel1, sel2)` | Detect page state |
| Check visibility | `IsElementVisible(selector)` | Branch logic |
| Click | `Click(selector)` | Confirm household |
| Wait | `Wait(ms)` | Post-click delay |

### Selector Model

```csharp
string confirmButtonSelector = Select.ByXPath(@"//button[@data-uia='set-primary-location-action']");
string locationDetailsSelector = Select.ByXPath(@"//div[@data-uia='location-details']");
```

- Uses NuciWeb `Select.ByXPath()` factory
- Targets Netflix `data-uia` attributes
- Two-element strategy: action button vs confirmed state

### Error Model

| Exception | Source | Meaning |
|-----------|--------|---------|
| `WebDriverException` | Selenium | General WebDriver error |
| `NoSuchElementException` | Selenium | Element not found |
| `TimeoutException` | Selenium | Wait timeout |
| `StaleElementReferenceException` | Selenium | Element detached from DOM |
| `InvalidOperationException` | NuciWeb | Abstraction-level error |

### Configuration Mapping

| Setting | Usage |
|---------|-------|
| `botSettings.PageLoadTimeout` | `GoToUrl` timeout |
| `debugSettings.IsDebugMode` | Headless vs visible |
| `debugSettings.CrashScreenshotFileName` | Screenshot on fatal |

## Integration Boundaries

### EmailProcessor Boundary

```
Application                    External
─────────────────────────────────────────────
IEmailProcessor interface  →  MailKit.ImapClient
ImapSettings (config)      →  IMAP Server (TCP 993)
MimeMessage (domain)       ←  MimeKit parsing
```

### NetflixProcessor Boundary

```
Application                    External
─────────────────────────────────────────────
INetflixProcessor          →  IWebProcessor (NuciWeb)
IWebProcessor              →  SeleniumWebProcessor
IWebDriver                 →  ChromeDriver/FirefoxDriver
Browser                    →  Netflix Web UI (HTTPS)
```

## Testing Integration Models

### EmailProcessor Testing

**Unit Tests:** Construction only (minimal)

**Integration Tests:** Mocked via `IEmailProcessor` in `HouseholdConfirmationIntegrationTests`
- `SetupSequence` for `GetHouseholdConfirmationUrl()` return values
- Verifies `LogIn()`/`LogOut()` called correctly

### NetflixProcessor Testing

**Unit Tests:** `NetflixProcessorTests.cs` with mocked `IWebProcessor`
- Verifies `GoToUrl`, `WaitForAnyElementToBeVisible`, `Click`, `Wait` calls

**Integration Tests:** Real `NetflixProcessor` with mocked `IWebProcessor`
- Parameterised failure injection via `BrowserFailureStage`
- Verifies error swallowing and mailbox cleanup

## Known Integration Issues

### IMAP
1. **No connection pooling** — Single `ImapClient` for entire process
2. **No reconnection** — Connection loss crashes process
3. **No IDLE support** — Polling only (75s interval, but currently tight loop)
4. **In-memory duplicate tracking** — Resets on restart

### Browser
1. **No persistent profile** — Fresh browser each run
2. **No login handling** — Assumes authenticated session
3. **Brittle selectors** — Netflix `data-uia` attributes
4. **Swallowed exceptions** — Failures hidden from orchestrator
5. **No success verification** — Assumes click completes flow