# Testing Model

## Test Architecture

[NetflixHouseholdConfirmator.UnitTests.csproj](../NetflixHouseholdConfirmator.UnitTests/NetflixHouseholdConfirmator.UnitTests.csproj) and [NetflixHouseholdConfirmator.IntegrationTests.csproj](../NetflixHouseholdConfirmator.IntegrationTests/NetflixHouseholdConfirmator.IntegrationTests.csproj) reference the executable project. NUnit supplies tests, Moq substitutes external boundaries, and Coverlet can collect coverage. The integration suite composes the real orchestrator, processors, MIME messages, and shipped configuration while substituting IMAP, browser, and logging infrastructure. No live IMAP server, browser, WebDriver, or Netflix page is required.

## Behaviour Coverage

| Test area | File | Verified behaviour |
|---|---|---|
| Orchestration | [HouseholdConfirmatorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/HouseholdConfirmatorTests.cs) | Login, start logging, repeated URL forwarding, null suppression, exception propagation, and logout cleanup. |
| IMAP and email selection | [EmailProcessorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/Processors/EmailProcessorTests.cs) | Connection/authentication/disconnection, read-only inbox access, newest-first selection, age cutoff, subject matching, timestamp duplicate suppression, line-break removal, URL extraction, and invalid-date failure. |
| Browser confirmation | [NetflixProcessorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/Processors/NetflixProcessorTests.cs) | Navigation, selector waiting, already-confirmed detection, click/wait behaviour, swallowed browser exceptions, and logging states. |
| Settings | [BotSettingsTests.cs](../NetflixHouseholdConfirmator.UnitTests/Configuration/BotSettingsTests.cs), [DebugSettingsTests.cs](../NetflixHouseholdConfirmator.UnitTests/Configuration/DebugSettingsTests.cs) | Property retention, headless inverse logic, and screenshot enablement. |
| Logging vocabulary | [MyOperationTests.cs](../NetflixHouseholdConfirmator.UnitTests/Logging/MyOperationTests.cs), [MyLogInfoKeyTests.cs](../NetflixHouseholdConfirmator.UnitTests/Logging/MyLogInfoKeyTests.cs) | Operation and key name contracts. |
| Integrated mailbox-to-browser workflow | [HouseholdConfirmationIntegrationTests.cs](../NetflixHouseholdConfirmator.IntegrationTests/Service/HouseholdConfirmationIntegrationTests.cs) | MIME selection, URL forwarding, duplicate suppression, browser states and failures, IMAP lifecycle failures, polling failures, and cleanup. |
| Shipped configuration | [ConfigurationIntegrationTests.cs](../NetflixHouseholdConfirmator.IntegrationTests/Configuration/ConfigurationIntegrationTests.cs) | Output copying and binding of every application-owned setting from the shipped JSON file. |

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

## Commands

Run all tests with:

```bash
dotnet test
```

Run only integration tests with:

```bash
dotnet test NetflixHouseholdConfirmator.IntegrationTests/NetflixHouseholdConfirmator.IntegrationTests.csproj
```

Collect Cobertura output with:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory NetflixHouseholdConfirmator.UnitTests/TestResults
```

When changing a production contract, update the narrowest relevant test file first, then run the complete suite because orchestration, configuration, and logging contracts cross project boundaries.
