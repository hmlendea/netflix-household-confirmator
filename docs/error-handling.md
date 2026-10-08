# Error Handling

## Error Taxonomy

| Category | Examples | Handling Strategy |
|----------|----------|-------------------|
| **Configuration Errors** | Missing `appsettings.json`, invalid JSON, missing required values | Fail fast at startup; logged as `Fatal` |
| **IMAP Connectivity** | DNS failure, connection refused, TLS handshake failure, authentication failure | Propagate as exception; logged in `EmailLogIn` operation |
| **IMAP Protocol** | Folder not found, search syntax error, message fetch failure | Propagate as exception; logged in `ListenForConfirmationRequests` |
| **Email Parsing** | Missing headers, malformed MIME, regex match failure | Propagate as exception; logged in `ListenForConfirmationRequests` |
| **Browser Initialisation** | No WebDriver found, browser launch failure, timeout | Fail fast at startup; logged as `Fatal` |
| **Browser Navigation** | Page load timeout, element not found, stale element, JavaScript error | Propagate as exception; logged in `HouseholdConfirmation` |
| **Netflix Page Changes** | Selector mismatch, flow change, CAPTCHA, login required | Propagate as exception; logged in `HouseholdConfirmation` |
| **Unhandled Exceptions** | Any exception reaching `Program.Main` | Logged as `Fatal`; crash screenshot captured if enabled |

## Handling Patterns

### 1. Fail Fast at Startup

```csharp
// Program.Main
webDriver = WebDriverInitialiser.InitialiseAvailableWebDriver(...);
// Throws if no WebDriver available
```

### 2. Operation-Scoped Try/Catch with Structured Logging

```csharp
// EmailProcessor.LogIn
try
{
    imapClient.Connect(...);
}
catch (Exception exception)
{
    logger.Error(
        MyOperation.EmailLogIn,
        OperationStatus.Failure,
        "Failed to connect to the IMAP server.",
        exception,
        logInfos);
    throw;  // Re-throw to propagate
}
```

### 3. Orchestrator-Level Try/Catch/Finally

```csharp
// HouseholdConfirmator.ConfirmIncomingHouseholdUpdateRequests
try
{
    emailProcessor.LogIn();
    webProcessor.LogIn();

    while (true)
    {
        string url = emailProcessor.GetHouseholdConfirmationUrl();
        if (!string.IsNullOrEmpty(url))
        {
            netflixProcessor.ConfirmHousehold(url);
        }
        Thread.Sleep(pollInterval);
    }
}
catch (Exception)
{
    throw;  // Propagate to host
}
finally
{
    emailProcessor.LogOut();  // Always attempt cleanup
}
```

### 4. Host-Level AggregateException Unwrapping

```csharp
// Program.Main
catch (AggregateException ex)
{
    LogInnerExceptions(ex);  // Recursively log each inner exception
    SaveCrashScreenshot();
}
catch (Exception ex)
{
    logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
    SaveCrashScreenshot();
}
```

## Recovery Behaviour

| Failure Point | Recovery Action |
|---------------|-----------------|
| IMAP connection failure | Process exits; no automatic retry |
| IMAP authentication failure | Process exits; no automatic retry |
| Email fetch failure | Loop iteration fails; next iteration retries |
| URL extraction failure | Email skipped; next email processed |
| Browser login failure | Process exits; no automatic retry |
| Confirmation click failure | Process exits; no automatic retry |
| WebDriver crash | Process exits; no automatic retry |

**No built-in retry logic** — All failures propagate to the host. External orchestration (systemd, Windows Service, container restart policy) must handle restarts.

## Exception Types

### Common Exceptions Thrown

| Source | Exception Types |
|--------|-----------------|
| `MailKit` | `ImapProtocolException`, `ImapCommandException`, `SslHandshakeException`, `AuthenticationException` |
| `Selenium` | `WebDriverException`, `NoSuchElementException`, `TimeoutException`, `StaleElementReferenceException` |
| `Configuration` | `JsonException`, `ConfigurationBinderException` |
| `Application` | `ArgumentNullException`, `InvalidOperationException`, `RegexMatchTimeoutException` |

### Custom Exceptions

**None defined** — Application uses framework exceptions exclusively.

## Error Logging Format

All errors include:
- Operation name (`MyOperation.*`)
- Status (`Failure`)
- Human-readable message
- Full exception with stack trace
- Structured `LogInfo` context (server, port, username — never password)

Example:
```
[Error] [EmailLogIn] [Failure] Failed to connect to the IMAP server.
MailKit.Security.SslHandshakeException: SSL handshake failed
   at MailKit.Net.Imap.ImapClient.Connect(...)
{Server=mail.example.com, Port=993}
```

## Crash Screenshots

On fatal error, if `debugSettings.IsCrashScreenshotEnabled`:
1. Capture screenshot via `ITakesScreenshot`
2. Save to `logfile.log` directory with `CrashScreenshotFileName`
3. Log file path in `Fatal` log entry

## Testing Error Handling

### Unit Tests
- Mock external dependencies to throw specific exceptions
- Verify exceptions propagate correctly
- Verify logging calls with correct operation/status/message

### Integration Tests
- `BrowserFailureStage` enum parameterises failure injection:
  - `BeforeLogin`
  - `AfterLoginBeforeConfirmation`
  - `DuringConfirmation`
- Verify orchestrator propagates failures
- Verify cleanup (`LogOut`) called in `finally`

## Known Gaps

1. **No retry/backoff** — Transient network failures cause process exit
2. **No circuit breaker** — Repeated failures hammer external services
3. **No dead letter queue** — Failed emails are lost (no persistence)
4. **No health endpoint** — Cannot probe liveness/readiness externally
5. **No graceful shutdown** — `CancellationToken` not plumbed through loop

## Recommended Improvements

1. Add `Polly` retry policies for IMAP and browser operations
2. Implement `IHostedService` / `CancellationToken` for graceful shutdown
3. Add health check endpoint (if HTTP server added)
4. Persist processed email IDs to survive restarts
5. Define custom exception hierarchy for clearer catch handling