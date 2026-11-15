# Integration Models

## Overview

Integration models are the adapters that connect the application to external systems: IMAP (email) and Selenium/WebDriver (browser automation).

## IMAP Integration (EmailProcessor)

### Library
- **MailKit 4.18.1** — IMAP client
- **MimeKit** — MIME message parsing (transitive via MailKit)

### Connection Model

```csharp
private ImapClient imapClient = new();

// LogIn()
if (imapClient.IsConnected)
{
    imapClient.Disconnect(true);
}

imapClient.Connect(imapSettings.Server, imapSettings.Port, true); // SSL
imapClient.Authenticate(imapSettings.Username, imapSettings.Password);

// LogOut()
imapClient.Disconnect(true);
imapClient.Dispose();
```

- One `ImapClient` instance is created at construction and reused for the process lifetime
- `LogIn()` disconnects a still-connected client before reconnecting, so it can be re-entered by the recovery path

### Message Retrieval Model

```csharp
IMailFolder inbox = imapClient.Inbox;
inbox.Open(FolderAccess.ReadOnly);

for (int emailIndex = inbox.Count - 1; emailIndex >= 0; emailIndex -= 1)
{
    MimeMessage email = inbox.GetMessage(emailIndex);

    if ((DateTime.Now - email.Date).TotalSeconds > imapSettings.MaxEmailAge)
    {
        break;
    }

    emails.Add(email);
}
```

- Opens the mailbox read-only on every polling iteration, refreshing the message count
- Reads newest to oldest by index and stops at the first message older than `MaxEmailAge`
- No server-side search or summary fetch is issued

### Session Recovery Model

```csharp
int attempt = 1;

while (true)
{
    try
    {
        return ReadRecentEmails();
    }
    catch (Exception exception) when (
        ImapFailurePolicy.IsRecoverable(exception) &&
        attempt < MaximumRetrievalAttempts)
    {
        attempt += 1;
        LogIn();
    }
}
```

- Up to three retrieval attempts
- Recovery is limited to the exception types classified by `ImapFailurePolicy`
- Retries are immediate; there is no delay or backoff

### Message Filtering Model

```csharp
private static string HouseholdUpdateEmailSubject
    => "How to update your Netflix Household";

if (email.Subject.Contains(HouseholdUpdateEmailSubject))
{
    DateTimeOffset emailDateTime = email.Date; // Offset-bearing MIME Date header

    if (emailDateTime > lastConfirmationEmailDateTime)
    {
        lastConfirmationEmailDateTime = emailDateTime;
        return ExtractConfirmationUrlFromEmail(email);
    }
}
```

- Duplicate suppression compares offset-bearing `DateTimeOffset` values, so a non-UTC `Date` header cannot shift the recorded instant into the future

### URL Extraction Model

```csharp
private static string ConfirmationUrlPattern
    => ".*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*";

private static string ExtractConfirmationUrlFromEmail(MimeMessage email)
    => Regex.Replace(
        email.HtmlBody.Replace(Environment.NewLine, string.Empty),
        ConfirmationUrlPattern,
        "$1");
```

- Only the HTML body is read; a message without an HTML body raises `NullReferenceException`

### Error Model

| Exception | Source | Meaning | Recoverable |
|-----------|--------|---------|-------------|
| `ImapProtocolException` | MailKit | Protocol-level error, including an unexpected server disconnect | Yes |
| `ProtocolException` | MailKit | Base protocol failure, including an unexpected server disconnect | Yes |
| `ServiceNotConnectedException` | MailKit | Operation attempted on a severed session | Yes |
| `ServiceNotAuthenticatedException` | MailKit | Session no longer authenticated | Yes |
| `IOException` | System | I/O failure, including a connection timeout | Yes |
| `SocketException` | System | Network failure | Yes |
| `TimeoutException` | System | Operation timeout | Yes |
| `ImapCommandException` | MailKit | The server rejected a command; not treated as recoverable | No |
| `SslHandshakeException` | MailKit | TLS negotiation failure; not treated as recoverable | No |
| `AuthenticationException` | System | Invalid credentials; not treated as recoverable | No |

Recoverable failures trigger an immediate reconnect and retrieval retry, up to three attempts.

### Configuration Mapping

| ImapSettings Property | MailKit Usage |
|----------------------|---------------|
| `Server` | `Connect(server, ...)` |
| `Port` | `Connect(..., port, ...)` |
| `Username` | `Authenticate(username, ...)` |
| `Password` | `Authenticate(..., password)` |
| `MaxEmailAge` | Message-age cutoff applied while reading the newest-first message list |

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
2. **Immediate recovery attempts** — A recoverable connection loss triggers up to three immediate reconnect-and-retry attempts before propagating
3. **No IDLE support** — Polling only, with no delay between iterations
4. **In-memory duplicate tracking** — Resets on restart

### Browser
1. **No persistent profile** — Fresh browser each run
2. **No login handling** — Assumes authenticated session
3. **Brittle selectors** — Netflix `data-uia` attributes
4. **Swallowed exceptions** — Failures hidden from orchestrator
5. **No success verification** — Assumes click completes flow