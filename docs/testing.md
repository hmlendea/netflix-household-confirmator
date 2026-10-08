# Testing

## Test Strategy

| Layer | Framework | Scope | Execution |
|-------|-----------|-------|-----------|
| Unit | NUnit 5 + Moq | Individual classes with mocked dependencies | `dotnet test` |
| Integration | NUnit 5 + Moq | Orchestrator with real implementation, mocked infrastructure | `dotnet test` |
| Manual | — | Full end-to-end with real IMAP and browser | Local only |

## Test Organisation

### Unit Tests (`NetflixHouseholdConfirmator.UnitTests`)

**Structure mirrors production code:**

```
NetflixHouseholdConfirmator.UnitTests/
├── Configuration/
│   ├── BotSettingsTests.cs
│   └── DebugSettingsTests.cs
├── Logging/
│   ├── MyLogInfoKeyTests.cs
│   └── MyOperationTests.cs
└── Service/
    ├── HouseholdConfirmatorTests.cs
    └── Processors/
        ├── EmailProcessorTests.cs
        └── NetflixProcessorTests.cs
```

**Principles:**
- Test public behaviour, not implementation details
- Mock all external boundaries (IMAP, WebDriver, Logger)
- Use `SetupSequence` for multi-call scenarios
- Verify interactions with `Verify`
- Name tests: `Given<Context>_When<Action>_Then<Outcome>`

### Integration Tests (`NetflixHouseholdConfirmator.IntegrationTests`)

**Structure:**

```
NetflixHouseholdConfirmator.IntegrationTests/
├── Configuration/
│   └── ConfigurationIntegrationTests.cs
└── Service/
    ├── BrowserFailureStage.cs
    └── HouseholdConfirmationIntegrationTests.cs
```

**Principles:**
- Compose real `HouseholdConfirmator` with real `NetflixProcessor`
- Mock `IEmailProcessor` and `IWebProcessor`
- Test end-to-end orchestration flows
- Parameterise browser failure scenarios via `BrowserFailureStage`

## Test Naming Convention

```
Given<Precondition>_When<Action>_Then<ExpectedResult>
```

Examples:
- `GivenAConfirmationUrl_WhenListeningForRequests_ThenTheHouseholdIsConfirmed`
- `GivenNoConfirmationUrl_WhenListeningForRequests_ThenNoHouseholdIsConfirmed`
- `GivenANewConfirmationEmail_WhenListening_ThenTheBrowserHandlesTheCurrentPageState`

## Key Test Scenarios

### HouseholdConfirmator (Orchestrator)

| Scenario | Unit Test | Integration Test |
|----------|-----------|------------------|
| Single confirmation URL processed | ✅ | ✅ |
| Multiple URLs processed sequentially | ✅ | ✅ |
| Null/empty URL skipped | ✅ | ✅ |
| Duplicate URL suppressed | ✅ | ✅ |
| Email processor LogIn called once | ✅ | — |
| Started state logged | ✅ | — |
| Browser failure swallowed, mailbox closed | — | ✅ |
| Navigation failure, next request still processed | — | ✅ |

### EmailProcessor (IMAP Adapter)

| Scenario | Unit Test | Integration Test |
|----------|-----------|------------------|
| Construction with settings/logger | ✅ | — |
| LogIn connects and authenticates | — | — |
| LogOut disconnects and disposes | — | — |
| GetHouseholdConfirmationUrl filters by subject | — | — |
| GetHouseholdConfirmationUrl extracts URL via regex | — | — |
| Duplicate suppression by Date | — | — |

**Note:** EmailProcessor unit tests are minimal (construction only). Integration tests cover behaviour via mocked `IEmailProcessor`.

### NetflixProcessor (Browser Adapter)

| Scenario | Unit Test | Integration Test |
|----------|-----------|------------------|
| ConfirmHousehold navigates to URL | ✅ | — |
| ConfirmHousehold waits for elements | ✅ | — |
| ConfirmHousehold clicks button when needed | ✅ | — |
| ConfirmHousehold handles visible location details | ✅ | — |

### Configuration

| Scenario | Unit Test | Integration Test |
|----------|-----------|------------------|
| BotSettings binds PageLoadTimeout | ✅ | — |
| DebugSettings computes IsHeadless | ✅ | — |
| DebugSettings computes IsCrashScreenshotEnabled | ✅ | — |
| Configuration binds all sections | — | ✅ |

### Logging Vocabulary

| Scenario | Unit Test |
|----------|-----------|
| MyLogInfoKey names match property names | ✅ |
| MyOperation names match property names | ✅ |

## BrowserFailureStage Enum

Used in integration tests to inject failures at specific points:

```csharp
public enum BrowserFailureStage
{
    ElementWaiting,      // WaitForAnyElementToBeVisible throws
    VisibilityDetection, // IsElementVisible throws
    ButtonClicking,      // Click throws
    PostConfirmationWaiting // Wait throws
}
```

Each stage tested to verify:
- Failure is swallowed (does not propagate)
- Mailbox is closed (LogOut called)
- No crash screenshot (handled gracefully)

## Important Assertions

The tests intentionally capture current edge behaviour:

- A non-null empty or whitespace URL is forwarded to `NetflixProcessor`.
- A subject containing the expected phrase matches; an upper-case variant does not.
- A matching email without the URL marker returns its unchanged HTML body.
- A malformed `DateReceived` header raises `FormatException`.
- Login failure prevents polling and does not call logout.
- Polling failure logs and rethrows, then attempts logout.
- Logout failure can replace an earlier polling exception.
- Browser failures are swallowed and still produce the success log.

These assertions are compatibility evidence. They should be revisited before changing the corresponding implementation, even when the behaviour appears undesirable.

## Verification Gaps

The test suites do not exercise:

- Real MailKit TLS negotiation, authentication, folder state, or MIME variations.
- Real browser/WebDriver startup, Netflix markup, navigation timing, or selector compatibility.
- Process-level startup failures before the host `try` block.
- WebDriver quit failures or screenshot path failures.
- Cancellation, operating-system signals, restart recovery, or multiple-instance coordination.
- Time-zone and culture edge cases beyond the invariant-format examples.
- A missing `DateReceived` header separately from an invalid value.
- Polling load, absence of backoff, or operational log file retention.

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test NetflixHouseholdConfirmator.UnitTests/NetflixHouseholdConfirmator.UnitTests.csproj

# Integration tests only
dotnet test NetflixHouseholdConfirmator.IntegrationTests/NetflixHouseholdConfirmator.IntegrationTests.csproj

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific test
dotnet test --filter "FullyQualifiedName~HouseholdConfirmatorTests.GivenAConfirmationUrl"
```

## Coverage

**Current:** Coverlet configured in both test projects. Run with `--collect:"XPlat Code Coverage"` to generate `coverage.cobertura.xml`.

**Targets:** No explicit coverage thresholds enforced.

## Test Data

**Standard test values** (from `common.test-values.instructions.md`):
- Names: `John Doe`, `Jane Smith`
- Emails: `john.doe@example.com`, `jane.smith@example.com`
- URLs: `https://example.com/path`
- Dates: `2024-01-15`, `2024-06-20`
- Amounts: `123.45` (USD)

**Application-specific test constants:**
- `ConfirmationUrl` = `https://test.url.com/UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA`
- `HouseholdUpdateEmailSubject` = `How to update your Netflix Household`
- `MaximumEmailAgeSeconds` = `64`
- `PollingStoppedMessage` = `Polling stopped.`

## Mocking Strategy

| Dependency | Mocked In | Approach |
|------------|-----------|----------|
| `IEmailProcessor` | Unit (Orchestrator), Integration | `Mock<IEmailProcessor>` with `SetupSequence` |
| `INetflixProcessor` | Unit (Orchestrator) | `Mock<INetflixProcessor>` |
| `IWebProcessor` | Integration | `Mock<IWebProcessor>` |
| `ILogger` | All | `Mock<ILogger>` with `Verify` for log calls |
| `ImapSettings` | Unit (EmailProcessor) | Real instance with test values |
| `IWebDriver` | Not mocked (created by WebDriverInitialiser) | — |

## Continuous Integration

**GitHub Actions** (`.github/workflows/dotnet.yml`):
- Restore
- Build
- Test (all projects)
- Publish (on release tags)

## Known Test Gaps

1. **No EmailProcessor behaviour tests** — Only construction tested
2. **No NetflixProcessor unit tests** — Only integration tests cover it
3. **No IMAP integration tests** — Requires real IMAP server
4. **No browser integration tests** — Requires real WebDriver/browser
5. **No configuration validation tests** — Missing required value scenarios
6. **No logging output tests** — Only vocabulary contract tests
7. **No performance/load tests** — Not applicable for this workload

## Adding New Tests

1. **Unit test:** Add to corresponding `*Tests.cs` in `UnitTests/`
2. **Integration test:** Add to `HouseholdConfirmationIntegrationTests.cs` or new file in `IntegrationTests/Service/`
3. **Follow naming convention:** `Given...When...Then...`
4. **Mock external boundaries** — Never use real IMAP/WebDriver in tests
5. **Verify interactions** — Use `Verify` for method calls, `Assert` for return values