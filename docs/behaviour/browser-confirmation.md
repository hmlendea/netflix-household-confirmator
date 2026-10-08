# Browser Confirmation Behaviour

## Overview

The browser confirmation behaviour covers Netflix page navigation, element detection, and household confirmation interaction using Selenium-backed automation.

## Entry Point

`INetflixProcessor.ConfirmHousehold(string confirmationUrl)` — Called by orchestrator when a confirmation URL is found.

## Confirmation Flow

### 1. Navigate to Confirmation URL
```csharp
webProcessor.GoToUrl(confirmationUrl);
```

- Uses `IWebProcessor.GoToUrl()` (NuciWeb abstraction over Selenium)
- Waits for page load (timeout from `BotSettings.PageLoadTimeout`)
- No explicit wait for specific element — relies on subsequent waits

### 2. Wait for Key Elements
```csharp
string confirmButtonSelector = Select.ByXPath(@"//button[@data-uia='set-primary-location-action']");
string locationDetailsSelector = Select.ByXPath(@"//div[@data-uia='location-details']");

webProcessor.WaitForAnyElementToBeVisible(confirmButtonSelector, locationDetailsSelector);
```

- Waits for **either** element to become visible
- `WaitForAnyElementToBeVisible` polls until one matches
- Timeout governed by `WebProcessor` configuration

### 3. Branch Based on Page State

#### Case A: Location Details Visible (Already Confirmed)
```csharp
if (webProcessor.IsElementVisible(locationDetailsSelector))
{
    // Do nothing — household already confirmed
}
```

- Checks if location details section is visible
- If yes, confirmation already completed (idempotent)
- No click, no wait — logs success and returns

#### Case B: Confirm Button Visible (Action Required)
```csharp
if (!webProcessor.IsElementVisible(locationDetailsSelector))
{
    webProcessor.Click(confirmButtonSelector);
    webProcessor.Wait(5000);
}
```

- Clicks "Set as Primary Location" button
- Waits 5 seconds for confirmation to process
- No verification of success — assumes click completes flow

## Selectors

| Selector | XPath | Purpose |
|----------|-------|---------|
| `confirmButtonSelector` | `//button[@data-uia='set-primary-location-action']` | Primary action button |
| `locationDetailsSelector` | `//div[@data-uia='location-details']` | Confirmed state indicator |

**Source:** Netflix household update confirmation page DOM attributes (`data-uia`)

## Error Handling

### Exception Catching
```csharp
try
{
    // ... confirmation logic
}
catch (Exception exception)
{
    logger.Error(
        MyOperation.HouseholdConfirmation,
        OperationStatus.Failure,
        "An error has occurred while confirming the household.",
        exception);
}
```

- Catches **all** exceptions
- Logs with full exception details
- **Does not re-throw** — swallows exception
- Continues to log `Success` status

### Logging Behaviour
```csharp
logger.Info(MyOperation.HouseholdConfirmation, OperationStatus.Started, "Starting...");
try { ... }
catch { logger.Error(..., OperationStatus.Failure, ...); }
logger.Info(MyOperation.HouseholdConfirmation, OperationStatus.Success, "The household was successfully confirmed.");
```

**Critical Issue:** `Success` is logged **even after caught exception** — misleading log output.

## Return Value

**None** (`void`) — No indication of success/failure to caller.

## Integration Test Coverage

**Integration Tests** (`HouseholdConfirmationIntegrationTests`):

| Test | Scenario |
|------|----------|
| `GivenANewConfirmationEmail_WhenListening_ThenTheBrowserHandlesTheCurrentPageState` | Both page states (confirmed/unconfirmed) |
| `GivenSuccessiveConfirmationEmails_WhenListening_ThenEveryNewRequestIsOpened` | Multiple URLs |
| `GivenTheSameConfirmationEmailTwice_WhenListening_ThenItIsOpenedOnce` | Duplicate suppression (email level) |
| `GivenAnEmptyInbox_WhenListening_ThenTheBrowserIsNotOpened` | No email → no browser |
| `GivenOnlyNonQualifyingEmails_WhenListening_ThenTheBrowserIsNotOpened` | Non-matching emails |
| `GivenMatchingHtmlWithoutAUrl_WhenListening_ThenTheRawBodyIsOpened` | Invalid URL → navigates to body text |
| `GivenBrowserNavigationFails_WhenListening_ThenTheNextRequestIsStillOpened` | Navigation failure → continues |
| `GivenABrowserOperationFails_WhenListening_ThenTheFailureIsSwallowedAndTheMailboxIsClosed` | Failure at each stage (parameterised) |

**BrowserFailureStage Enum:**
- `ElementWaiting` — `WaitForAnyElementToBeVisible` throws
- `VisibilityDetection` — `IsElementVisible` throws
- `ButtonClicking` — `Click` throws
- `PostConfirmationWaiting` — `Wait` throws

All failure stages verified to:
- Swallow exception (not propagate)
- Close mailbox (`LogOut` called)
- Continue polling (next iteration)

## Configuration Impact

| Setting | Effect |
|---------|--------|
| `botSettings.PageLoadTimeout` | `GoToUrl` timeout |
| `debugSettings.IsDebugMode` | Headless vs visible browser |
| `debugSettings.CrashScreenshotFileName` | Screenshot on fatal error |

## Known Issues

### 1. Misleading Success Log
**Problem:** `OperationStatus.Success` logged even when exception caught.
**Impact:** Logs show success but confirmation may have failed.
**Fix:** Move success log inside `try` block, or re-throw after logging.

### 2. No Success Verification
**Problem:** No check that confirmation actually succeeded after click.
**Risk:** Click may fail silently (element stale, network error, page change).
**Fix:** Wait for success indicator (toast, URL change, element state).

### 3. Hardcoded Wait
**Problem:** `webProcessor.Wait(5000)` fixed 5-second wait.
**Risk:** Too short (confirmation pending) or too long (wasted time).
**Fix:** Wait for specific success condition.

### 4. Brittle Selectors
**Problem:** XPath depends on Netflix `data-uia` attributes.
**Risk:** Netflix UI changes break automation.
**Mitigation:** More robust selectors, fallback strategies.

### 5. Swallowed Exceptions
**Problem:** All exceptions caught and not re-thrown.
**Impact:** Orchestrator continues polling; no alert on repeated failures.
**Fix:** Re-throw or track failure count.

### 6. No Login Handling
**Problem:** Assumes browser session already authenticated with Netflix.
**Risk:** New browser profile → redirects to login → confirmation fails.
**Fix:** Persistent browser profile or login automation.

## Testing Behaviour

### Unit Tests
- `NetflixProcessorTests.cs` — Mock `IWebProcessor`, verify calls

### Integration Tests
- Real `NetflixProcessor` with mocked `IWebProcessor`
- Parameterised failure injection via `BrowserFailureStage`
- Verify call sequences and error swallowing

## Manual Testing

1. Set `debugSettings.isDebugMode: true`
2. Run application
3. Trigger household update email
4. Observe browser interaction
5. Verify confirmation completes

## Future Improvements

1. **Fix success logging** — Log success only on actual success
2. **Add verification** — Wait for confirmation success indicator
3. **Configurable waits** — Replace hardcoded 5s with condition-based wait
4. **Selector resilience** — Multiple fallback selectors
5. **Failure propagation** — Option to re-throw or track failures
6. **Login automation** — Handle Netflix authentication
7. **Screenshot on failure** — Capture page state on confirmation error