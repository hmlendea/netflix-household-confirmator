# Integrations

## Overview

The application integrates with three external systems: IMAP email server, Netflix web UI, and Selenium WebDriver.

## Integration Map

| Integration | Protocol | Library | Direction |
|-------------|----------|---------|-----------|
| IMAP Server | IMAP over TLS (993) | MailKit | Outbound |
| Netflix Web UI | HTTPS + Browser Automation | NuciWeb/Selenium | Outbound |
| WebDriver | Local Process (ChromeDriver/GeckoDriver) | Selenium | Outbound |

## IMAP Integration

### Purpose
Retrieve Netflix household update confirmation emails.

### Configuration
```json
{
  "imapSettings": {
    "server": "mail.example.com",
    "port": 993,
    "username": "user@example.com",
    "password": "app-specific-password",
    "maxEmailAge": 1800
  }
}
```

### Operations

| Operation | IMAP Commands | Frequency |
|-----------|---------------|-----------|
| Connect | `CAPABILITY`, `STARTTLS`/`SSL`, `LOGIN` | Once at startup, and again after a recoverable session failure |
| Mailbox selection | `EXAMINE INBOX` | Per polling iteration |
| Fetch Full | `FETCH <index> (BODY.PEEK[])` | Per inspected message, newest to oldest |
| Disconnect | `LOGOUT` | Once at shutdown |

### Message Processing

1. **Select** — Open `INBOX` read-only, refreshing the message count
2. **Read** — Fetch messages newest to oldest until one is older than `MaxEmailAge`
3. **Filter** — Subject contains "How to update your Netflix Household"
4. **Deduplicate** — Compare `email.Date` with `lastConfirmationEmailDateTime` as offset-bearing values
5. **Extract** — Regex match for the `UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA` URL in the HTML body

### Error Handling

| Error | Detection | Recovery |
|-------|-----------|----------|
| Connection lost | `ProtocolException`, `IOException`, `SocketException`, `TimeoutException` | Session re-established and the retrieval retried, up to three attempts |
| Session invalid | `ServiceNotConnectedException`, `ServiceNotAuthenticatedException` | Session re-established and the retrieval retried, up to three attempts |
| Auth failed | `AuthenticationException` | Process exits |
| Retrieval failed on the final attempt | Recoverable exception | Process exits |

### Security

- TLS 1.2+ required (MailKit default)
- Password in memory only (from config)
- Password never logged
- App-specific password recommended

### Limitations

- No IDLE/push — polling only
- Recovery is limited to three immediate attempts per iteration, with no backoff
- Single folder (INBOX)
- In-memory deduplication

## Netflix Web UI Integration

### Purpose
Confirm household update via Netflix website.

### Flow

1. Navigate to confirmation URL (contains one-time token)
2. Wait for page load
3. Detect page state:
   - Location details visible → already confirmed
   - Confirm button visible → click and wait
4. No success verification

### Selectors

| Element | Selector | Attribute |
|---------|----------|-----------|
| Confirm Button | `//button[@data-uia='set-primary-location-action']` | `data-uia` |
| Location Details | `//div[@data-uia='location-details']` | `data-uia` |

### Dependencies

- Netflix account already logged in (browser session)
- Confirmation URL valid (one-time token)
- Page structure matches selectors

### Error Handling

| Error | Detection | Recovery |
|-------|-----------|----------|
| Navigation timeout | `TimeoutException` | Swallowed, logs success |
| Element not found | `NoSuchElementException` | Swallowed, logs success |
| Click intercepted | `ElementClickInterceptedException` | Swallowed, logs success |
| Stale element | `StaleElementReferenceException` | Swallowed, logs success |

### Security

- No credentials sent to Netflix (uses existing session)
- Confirmation URL contains OTP token (one-time)
- Browser runs locally (no remote WebDriver)

### Limitations

- Brittle selectors (Netflix UI changes break automation)
- No login automation (requires pre-authenticated session)
- No success verification
- Swallowed exceptions hide failures

## WebDriver Integration

### Purpose
Browser automation engine for Netflix interaction.

### Supported Browsers
- Chrome/Chromium (ChromeDriver)
- Firefox (GeckoDriver)
- Edge (EdgeDriver)

### Initialisation

```csharp
WebDriverInitialiser.InitialiseAvailableWebDriver(
    isDebugMode: debugSettings.IsDebugMode,
    pageLoadTimeout: botSettings.PageLoadTimeout);
```

- Tries browsers in order: Chrome → Firefox → Edge
- `isDebugMode=true` → visible window
- `isDebugMode=false` → headless
- `pageLoadTimeout` → navigation timeout

### Lifecycle

| Phase | Action |
|-------|--------|
| Startup | `WebDriverInitialiser` creates driver |
| Operation | `SeleniumWebProcessor` wraps driver |
| Shutdown | `driver.Quit()` in `finally` block |

### Configuration

| Setting | Source | Effect |
|---------|--------|--------|
| Headless | `debugSettings.IsDebugMode` | Visible vs headless |
| Page Load Timeout | `botSettings.PageLoadTimeout` | Navigation timeout |
| Crash Screenshot | `debugSettings.CrashScreenshotFileName` | Screenshot on fatal |

### Error Handling

| Error | Cause | Handling |
|-------|-------|----------|
| No driver found | Missing ChromeDriver/GeckoDriver | Startup crash |
| Version mismatch | Browser ≠ Driver version | Startup crash |
| Session crash | Browser process died | Unhandled → process crash |

## Integration Health

### No Health Endpoints

The application exposes no health/readiness endpoints. Monitoring must be external:
- Process supervision (systemd, Docker)
- Log analysis (Fatal = unhealthy)
- IMAP connectivity (separate check)
- Browser process (separate check)

### Recommended Monitoring

| Check | Method |
|-------|--------|
| Process running | systemd `systemctl status` / Docker `docker ps` |
| Recent log activity | Tail `logfile.log` for recent `Success` entries |
| IMAP connectivity | `openssl s_client -connect server:993` |
| Browser process | `pgrep -f chromedriver` |
| Disk space | Log file growth, screenshot directory |

## Future Integration Points

| Integration | Use Case | Effort |
|-------------|----------|--------|
| Webhook/Event emission | Notify on confirmation | Low (HTTP POST) |
| Metrics export | Prometheus/OpenTelemetry | Medium |
| Multiple IMAP accounts | Multi-account monitoring | Medium |
| Persistent browser profile | Survive restarts | Medium |
| Remote WebDriver | Containerised browser | High |