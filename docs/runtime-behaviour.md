# Runtime Behaviour

## System Boundary

Netflix Household Confirmator is a single-process .NET console application. [Program.cs](../NetflixHouseholdConfirmator/Program.cs) composes the process, [HouseholdConfirmator.cs](../NetflixHouseholdConfirmator/Service/HouseholdConfirmator.cs) orchestrates the work, [EmailProcessor.cs](../NetflixHouseholdConfirmator/Service/Processors/EmailProcessor.cs) reads IMAP, and [NetflixProcessor.cs](../NetflixHouseholdConfirmator/Service/Processors/NetflixProcessor.cs) drives the browser.

The process has no inbound API, database, durable checkpoint, queue, scheduler, cancellation token, or distributed lock. Its external inputs are startup configuration and mailbox contents. Its external effects are IMAP access, browser navigation, structured logs, and optional screenshots.

## Startup And Composition

1. `LoadConfiguration` creates `BotSettings`, `DebugSettings`, `ImapSettings`, and `NuciLoggerSettings`, then binds matching sections from [appsettings.json](../NetflixHouseholdConfirmator/appsettings.json).
2. `WebDriverInitialiser.InitialiseAvailableWebDriver` creates one process-owned WebDriver. `DebugSettings.IsDebugMode` selects visible versus headless execution; `BotSettings.PageLoadTimeout` supplies the page-load timeout.
3. `CreateIOC` registers settings, processors, logger, WebDriver, and interfaces as singletons.
4. The host resolves `ILogger` and `IHouseholdConfirmator`, writes the startup event, and invokes `ConfirmIncomingHouseholdUpdateRequests`.
5. The host catches aggregate or general failures, optionally saves a screenshot, quits the WebDriver, and writes shutdown.

The browser is initialised before the host `try` block. A failure during configuration, browser initialisation, or dependency composition therefore does not receive the normal runtime catch/finally handling.

## Polling Lifecycle

`HouseholdConfirmator` calls `IEmailProcessor.LogIn`, writes the listener-start event, and enters `while (true)`. Each iteration calls `GetHouseholdConfirmationUrl`. A non-null result is forwarded to `INetflixProcessor.ConfirmHousehold`; null means no browser work occurs. There is no delay between iterations.

The listener's `try` begins after login and start logging. Exceptions from polling or confirmation are logged, rethrown, and followed by `IEmailProcessor.LogOut` in `finally`. Login failure and start-log failure occur before that `try`, so they do not trigger logout. If polling fails and logout also fails, the logout exception becomes the propagated exception, as shown by [HouseholdConfirmatorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/HouseholdConfirmatorTests.cs).

## Email Selection Algorithm

`EmailProcessor.RetrieveRecentEmails` opens the IMAP inbox read-only and reads messages from `Count - 1` down to zero. It stops at the first message whose `Date` is older than `ImapSettings.MaxEmailAge` seconds relative to `DateTime.Now`; it does not inspect older indexes after that point.

`GetHouseholdConfirmationUrl` then examines the retrieved messages in newest-first order:

- The subject must contain the exact, case-sensitive text `How to update your Netflix Household`.
- The `DateReceived` header is parsed with invariant culture using `DateTime.Parse`.
- The received timestamp must be later than the processor's private `lastConfirmationEmailDateTime`.
- The timestamp is recorded before URL extraction, so an extraction result that is empty or malformed still advances the duplicate-suppression timestamp.
- The first matching message that satisfies these conditions wins; older matching messages are not considered in that call.
- No qualifying message returns `null`.

`lastConfirmationEmailDateTime` starts at `DateTime.Now` when the singleton processor is constructed. Consequently, messages received before process startup are normally ignored, and duplicate suppression lasts only for the current process lifetime.

A missing or malformed `DateReceived` header raises a parsing exception. Retrieval and parsing exceptions are not caught by `EmailProcessor`; they propagate to the listener and host.

## URL Extraction

The HTML body has `Environment.NewLine` removed, then is processed with the regular expression `.*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*` and replacement `$1`.

This extracts a contiguous HTTPS value ending at the marker, provided the surrounding text matches the expression. If the pattern does not match, `Regex.Replace` returns the unchanged HTML body. The caller treats every non-null string as actionable, including an empty or whitespace string and the unchanged body fallback. This is a current implementation contract, not validation of a usable Netflix URL.

## Browser Confirmation

`NetflixProcessor` performs these operations in order:

1. Navigate to the supplied string.
2. Wait for either the confirmation button XPath `//button[@data-uia='set-primary-location-action']` or location-details XPath `//div[@data-uia='location-details']` to become visible.
3. Query location-details visibility.
4. If location details are not visible, click the confirmation button and wait 5,000 milliseconds.
5. If location details are visible, skip clicking and finish.

Any exception from navigation, waiting, visibility, clicking, or the fixed wait is logged as a failure and swallowed. The method then writes the success event regardless of whether the browser interaction succeeded. A success log therefore means only that the method reached its final log statement; it does not prove Netflix accepted the request.

## Resource Ownership

- `Program` owns WebDriver creation and calls `Quit` in its final shutdown path.
- `EmailProcessor` owns the injected `IImapClient` session and disconnects and disposes it in `LogOut` after successful disconnect.
- `HouseholdConfirmator` owns the login/poll/logout ordering but not the mailbox or browser implementations.
- `NetflixProcessor` owns Netflix-specific selectors and browser interaction policy, but not WebDriver creation.

## Change Impact

Changes to the email subject, `DateReceived` representation, URL marker, or HTML layout affect [EmailProcessor.cs](../NetflixHouseholdConfirmator/Service/Processors/EmailProcessor.cs) and [EmailProcessorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/Processors/EmailProcessorTests.cs). Changes to Netflix selectors or confirmation states affect [NetflixProcessor.cs](../NetflixHouseholdConfirmator/Service/Processors/NetflixProcessor.cs) and [NetflixProcessorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/Processors/NetflixProcessorTests.cs). Changes to lifecycle ordering affect the interfaces, [HouseholdConfirmator.cs](../NetflixHouseholdConfirmator/Service/HouseholdConfirmator.cs), and [HouseholdConfirmatorTests.cs](../NetflixHouseholdConfirmator.UnitTests/Service/HouseholdConfirmatorTests.cs).
