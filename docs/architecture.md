# Detailed Architecture

This document elaborates on the root [ARCHITECTURE.md](../ARCHITECTURE.md) with implementation-grounded detail. It covers component internals, data flows, contracts, and operational semantics not fully described in the high-level overview.

## Component Internals

### Program (Composition Root)

**Location:** `NetflixHouseholdConfirmator/Program.cs`

**Responsibilities:**
- Loads and binds `BotSettings`, `DebugSettings`, `ImapSettings`, `NuciLoggerSettings` from `appsettings.json`
- Initialises the WebDriver before DI composition (`WebDriverInitialiser.InitialiseAvailableWebDriver`)
- Registers all services and settings as singletons in `CreateIOC()`
- Resolves `ILogger` and `IHouseholdConfirmator`, writes startup event
- Invokes `ConfirmIncomingHouseholdUpdateRequests()` in a try/catch/finally block
- Handles `AggregateException` by logging inner exceptions individually
- Saves crash screenshot on failure when `DebugSettings.IsCrashScreenshotEnabled`
- Quits WebDriver and writes shutdown event in `finally`

**Key Implementation Details:**
- Configuration is bound once at startup; no reload mechanism
- WebDriver creation precedes the try block — failures during initialisation bypass normal error handling
- Settings objects are mutable POCOs registered as singletons
- `NuciLogger` is the concrete `ILogger` implementation
- `SeleniumWebProcessor` wraps the process-owned `IWebDriver`

**Dependencies:**
- `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`
- `NuciLog`, `NuciLog.Configuration`, `NuciLog.Core`
- `NuciWeb.Automation`, `NuciWeb.Automation.Selenium`
- `OpenQA.Selenium`

### HouseholdConfirmator (Orchestrator)

**Location:** `NetflixHouseholdConfirmator/Service/HouseholdConfirmator.cs`

**Interface:** `IHouseholdConfirmator` (`NetflixHouseholdConfirmator/Service/IHouseholdConfirmator.cs`)

**Responsibilities:**
- Calls `IEmailProcessor.LogIn()`
- Logs `ListenForConfirmationRequests` started event
- Enters unbounded `while (true)` polling loop
- Calls `IEmailProcessor.GetHouseholdConfirmationUrl()` each iteration
- Forwards non-null URLs to `INetflixProcessor.ConfirmHousehold(url)`
- Catches exceptions from polling/confirmation, logs as `ListenForConfirmationRequests` failure, rethrows
- Calls `IEmailProcessor.LogOut()` in `finally` block

**Error Handling Semantics:**
- Login failure: logged, rethrown; polling never starts; logout not called
- Start-logging failure: logged, rethrown; polling never starts; logout not called
- Polling failure: logged (twice — once in inner catch, once in outer catch), rethrown; logout attempted
- Confirmation failure: logged (twice), rethrown; logout attempted
- Logout failure during cleanup: logged, but **replaces** the original exception as the propagated exception

**Dependencies:** `IEmailProcessor`, `INetflixProcessor`, `ILogger`

### EmailProcessor (IMAP Adapter)

**Location:** `NetflixHouseholdConfirmator/Service/Processors/EmailProcessor.cs`

**Interface:** `IEmailProcessor` (`NetflixHouseholdConfirmator/Service/Processors/IEmailProcessor.cs`)

**Responsibilities:**
- `LogIn()`: Connects to IMAP server over SSL/TLS, authenticates with username/password
- `LogOut()`: Disconnects and disposes the `IImapClient`
- `GetHouseholdConfirmationUrl()`: Retrieves recent emails, filters by subject, extracts URL from newest qualifying message
- `RetrieveRecentEmails()`: Opens inbox read-only, iterates from newest to oldest, stops at first message older than `MaxEmailAge` seconds
- `ExtractConfirmationUrlFromEmail()`: Removes newlines from HTML body, applies regex to extract URL ending with `UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA`

**State:**
- `lastConfirmationEmailDateTime` (private, initialised to `DateTime.Now` at construction) — tracks the most recently processed matching email timestamp for duplicate suppression within the process lifetime

**Email Selection Algorithm:**
1. Retrieve messages from inbox (newest first) until `MaxEmailAge` cutoff
2. Filter messages whose subject contains exact text "How to update your Netflix Household" (case-sensitive)
3. Parse `email.Date.DateTime` (previously used `email.Headers["DateReceived"]` which could be null)
4. Compare against `lastConfirmationEmailDateTime` — must be strictly greater
5. Update `lastConfirmationEmailDateTime` **before** URL extraction
6. Return extracted URL (or unchanged HTML body if regex doesn't match)
7. If no qualifying message, return `null`

**URL Extraction Regex:**
- Pattern: `.*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*`
- Replacement: `$1`
- If no match, returns the entire HTML body (with newlines removed)

**Error Propagation:**
- IMAP connection/authentication failures: logged, rethrown
- Message retrieval failures: propagate to caller
- Date parsing failures (`FormatException`): propagate to caller
- No internal retry or recovery

**Dependencies:** `ImapSettings`, `ILogger`, `MailKit.Net.Imap.ImapClient`

### NetflixProcessor (Browser Automation Adapter)

**Location:** `NetflixHouseholdConfirmator/Service/Processors/NetflixProcessor.cs`

**Interface:** `INetflixProcessor` (`NetflixHouseholdConfirmator/Service/Processors/INetflixProcessor.cs`)

**Responsibilities:**
- `ConfirmHousehold(string confirmationUrl)`: Navigates to URL, waits for selectors, conditionally clicks confirmation button

**Confirmation Algorithm:**
1. Navigate to supplied URL string
2. Wait for either selector to become visible:
   - Confirm button: `//button[@data-uia='set-primary-location-action']`
   - Location details: `//div[@data-uia='location-details']`
3. Check if location details are visible
4. If **not** visible: click confirm button, wait 5000ms
5. If visible: skip click (already confirmed)
6. Log success regardless of whether browser interaction succeeded

**Error Handling:**
- **All** exceptions from navigation, waiting, visibility checks, clicking, or fixed wait are caught
- Logged as `HouseholdConfirmation` failure with exception details
- **Swallowed** — method continues to log success
- Success log means only "method reached final log statement", not "Netflix accepted the request"

**Selectors (Hardcoded):**
- Confirm button: XPath `//button[@data-uia='set-primary-location-action']`
- Location details: XPath `//div[@data-uia='location-details']`

**Dependencies:** `IWebProcessor`, `ILogger`

## Data Flow Summary

```
appsettings.json
    ↓ (bind once at startup)
BotSettings, DebugSettings, ImapSettings, NuciLoggerSettings
    ↓ (DI registration)
Program.CreateIOC() → ServiceProvider
    ↓ (resolve)
IHouseholdConfirmator (HouseholdConfirmator)
    ↓ (LogIn)
IEmailProcessor (EmailProcessor) → IMAP mailbox
    ↓ (while true: GetHouseholdConfirmationUrl)
RetrieveRecentEmails() → MimeMessage[]
    ↓ (filter: subject contains "How to update your Netflix Household")
    ↓ (filter: email.Date.DateTime > lastConfirmationEmailDateTime)
    ↓ (extract: regex on HTML body)
URL string or null
    ↓ (if non-null)
INetflixProcessor (NetflixProcessor) → IWebProcessor (SeleniumWebProcessor) → WebDriver → Netflix
    ↓ (navigate, wait, click if needed)
    ↓ (log success regardless)
loop
```

## Contracts and Invariants

### EmailProcessor Contracts

| Contract | Current Behaviour |
|----------|-------------------|
| Subject matching | Exact case-sensitive substring "How to update your Netflix Household" |
| Date source | `email.Date.DateTime` (standard MIME Date header) |
| Duplicate suppression | In-memory, per-process, timestamp-based (`lastConfirmationEmailDateTime`) |
| URL extraction | Regex capture group; fallback = entire HTML body (newlines removed) |
| Null return | No qualifying message in current batch |
| Non-null return | Always forwarded to NetflixProcessor, even if empty/whitespace/invalid |

### NetflixProcessor Contracts

| Contract | Current Behaviour |
|----------|-------------------|
| Input validation | None — any non-null string passed to `GoToUrl` |
| Selector waiting | Waits for **either** confirm button **or** location details |
| Click condition | Clicks only if location details **not** visible |
| Post-click wait | Fixed 5000ms |
| Exception handling | All swallowed; success logged regardless |
| Idempotency | Not guaranteed — same URL can be processed multiple times across restarts |

### HouseholdConfirmator Contracts

| Contract | Current Behaviour |
|----------|-------------------|
| Polling interval | None — tight loop, no delay |
| Stop mechanism | None — runs until process termination or exception |
| Login ordering | Once before loop; failure aborts entire process |
| Logout ordering | Always in `finally` after loop exits (exception or not) |
| Exception propagation | Polling/confirmation exceptions rethrown; logout failure replaces original |

## Extension Points

### Email Processor
Replace `IEmailProcessor` implementation to change:
- IMAP protocol (e.g., Graph API, POP3)
- Message selection logic
- URL extraction logic
- Duplicate suppression strategy

### Netflix Processor
Replace `INetflixProcessor` implementation to change:
- Browser automation framework
- Page interaction logic
- Selector strategies
- Confirmation detection logic

### Household Confirmator
Replace `IHouseholdConfirmator` implementation to change:
- Polling interval/backoff
- Stop/cancellation mechanism
- Batch processing
- Coordination between instances

## Dependency Direction

```
Program (composition root)
    ↓ owns
WebDriver, Settings, Logger
    ↓ registers as singletons
IHouseholdConfirmator (HouseholdConfirmator)
    ↓ depends on interfaces
IEmailProcessor, INetflixProcessor, ILogger
    ↓ concrete implementations
EmailProcessor → MailKit IImapClient
NetflixProcessor → IWebProcessor → SeleniumWebProcessor → IWebDriver
```

**Rule:** Concrete registrations only in `Program.CreateIOC()`. Services consume interfaces and typed settings.