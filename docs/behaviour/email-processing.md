# Email Processing Behaviour

## Overview

The email processing behaviour covers IMAP message retrieval, filtering for Netflix household update emails, and confirmation URL extraction.

## Entry Point

`IEmailProcessor.GetHouseholdConfirmationUrl()` — Called by orchestrator each polling iteration.

## Message Retrieval

### Connection
- Established once via `LogIn()` at startup
- Single `ImapClient` instance reused
- SSL/TLS on port 993 (configurable)

### Search Criteria
```csharp
var query = SearchQuery.DeliveredAfter(
    DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(imapSettings.MaxEmailAge)));
```

- `MaxEmailAge` default: 1800 seconds (30 minutes)
- Searches `INBOX` folder only
- Returns messages delivered within the time window

### Fetch Strategy
```csharp
var summaries = imapClient.Inbox.Fetch(
    uids,
    MessageSummaryItems.UniqueId |
    MessageSummaryItems.Envelope |
    MessageSummaryItems.BodyStructure);
```

- Fetches minimal metadata first (UID, envelope, body structure)
- Full message fetched only for qualifying candidates

## Filtering

### Subject Match
```csharp
const string HouseholdUpdateEmailSubject = "How to update your Netflix Household";
if (email.Subject.Contains(HouseholdUpdateEmailSubject))
```

- Case-sensitive substring match
- Matches exact Netflix email subject line
- No regex — simple string containment

### Duplicate Suppression
```csharp
private DateTime lastConfirmationEmailDateTime = DateTime.Now;

DateTime emailDateTime = email.Date.DateTime;
if (emailDateTime > lastConfirmationEmailDateTime)
{
    lastConfirmationEmailDateTime = emailDateTime;
    return ExtractConfirmationUrlFromEmail(email);
}
```

- Tracks newest processed email timestamp
- In-memory only — resets on process restart
- Uses standard MIME `Date` header (`email.Date.DateTime`)
- **Fixed:** Previously used `email.Headers["DateReceived"]` which could be null

## URL Extraction

### Pattern
```csharp
private static string ConfirmationUrlPattern
    => ".*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*";

private static string ConfirmationUrlReplacement => "$1";
```

- Regex captures URL containing `UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA`
- Greedy match from start of string
- Replacement extracts capture group 1 (the URL)

### Extraction Method
```csharp
private string ExtractConfirmationUrlFromEmail(MimeMessage email)
{
    string body = email.HtmlBody ?? email.TextBody ?? string.Empty;
    return Regex.Replace(body, ConfirmationUrlPattern, ConfirmationUrlReplacement);
}
```

- Prefers HTML body, falls back to plain text
- Returns full matched URL or original body if no match
- No validation that result is a valid URL

## Return Values

| Condition | Return Value |
|-----------|--------------|
| Qualifying email found, URL extracted | Confirmation URL string |
| Qualifying email found, no URL match | Entire HTML body (newlines removed) |
| No qualifying email | `null` |

## Email Selection Algorithm (Detailed)

`EmailProcessor.RetrieveRecentEmails` opens the IMAP inbox read-only and reads messages from `Count - 1` down to zero. It stops at the first message whose `Date` is older than `ImapSettings.MaxEmailAge` seconds relative to `DateTime.Now`; it does not inspect older indexes after that point.

`GetHouseholdConfirmationUrl` then examines the retrieved messages in newest-first order:

1. The subject must contain the exact, case-sensitive text `How to update your Netflix Household`.
2. The standard MIME `Date` header is used via `email.Date.DateTime` (previously used `email.Headers["DateReceived"]` which could be null).
3. The received timestamp must be later than the processor's private `lastConfirmationEmailDateTime`.
4. The timestamp is recorded **before** URL extraction, so an extraction result that is empty or malformed still advances the duplicate-suppression timestamp.
5. The first matching message that satisfies these conditions wins; older matching messages are not considered in that call.
6. No qualifying message returns `null`.

`lastConfirmationEmailDateTime` starts at `DateTime.Now` when the singleton processor is constructed. Consequently, messages received before process startup are normally ignored, and duplicate suppression lasts only for the current process lifetime.

A missing or malformed `DateReceived` header raises a parsing exception. Retrieval and parsing exceptions are not caught by `EmailProcessor`; they propagate to the listener and host.

## URL Extraction Details

The HTML body has `Environment.NewLine` removed, then is processed with the regular expression `.*(https://[^ ]*UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA).*` and replacement `$1`.

This extracts a contiguous HTTPS value ending at the marker, provided the surrounding text matches the expression. If the pattern does not match, `Regex.Replace` returns the unchanged HTML body. The caller treats every non-null string as actionable, including an empty or whitespace string and the unchanged body fallback. This is a current implementation contract, not validation of a usable Netflix URL.
| Qualifying email found, no URL match | Email body (HTML or text) |
| No qualifying email | `null` |
| Exception during processing | Propagates to caller |

## Error Scenarios

| Scenario | Exception | Handling |
|----------|-----------|----------|
| IMAP connection lost | `ImapProtocolException` | Propagates; host logs fatal |
| Authentication expired | `AuthenticationException` | Propagates; host logs fatal |
| Folder not found | `ImapProtocolException` | Propagates |
| Search syntax error | `ImapProtocolException` | Propagates |
| Message fetch failure | `ImapProtocolException` | Propagates |
| Date parsing (old code) | `ArgumentNullException` | **Fixed** — now uses `email.Date.DateTime` |
| Regex timeout | `RegexMatchTimeoutException` | Propagates |

## Logging

**Operation:** `MyOperation.ListenForConfirmationRequests`

**Status Transitions:**
- `Started` → `Success` (normal completion)
- `Started` → `Failure` (exception)

**Structured Fields:** None currently (could add `EmailCount`, `QualifyingCount`)

## Configuration Impact

| Setting | Effect |
|---------|--------|
| `imapSettings.MaxEmailAge` | Search window; larger = more messages to scan |
| `imapSettings.Server/Port` | Connection target |
| `imapSettings.Username/Password` | Authentication |

## Testing Behaviour

**Unit Tests:** Minimal (construction only)

**Integration Tests:** Via mocked `IEmailProcessor` in `HouseholdConfirmationIntegrationTests`:
- `GivenANewConfirmationEmail_WhenListening_ThenTheBrowserHandlesTheCurrentPageState`
- `GivenSuccessiveConfirmationEmails_WhenListening_ThenEveryNewRequestIsOpened`
- `GivenTheSameConfirmationEmailTwice_WhenListening_ThenItIsOpenedOnce`
- `GivenAnEmptyInbox_WhenListening_ThenTheBrowserIsNotOpened`
- `GivenOnlyNonQualifyingEmails_WhenListening_ThenTheBrowserIsNotOpened`
- `GivenAStaleNewestEmail_WhenListening_ThenOlderEmailsAreNotInspected`
- `GivenMatchingHtmlWithoutAUrl_WhenListening_ThenTheRawBodyIsOpened`
- `GivenAMatchingPlainTextEmail_WhenListening_ThenPollingFailsAndTheMailboxIsClosed`
- `GivenAnInvalidReceivedDate_WhenListening_ThenPollingFailsAndTheMailboxIsClosed`
- `GivenInboxRetrievalFails_WhenListening_ThenTheFailurePropagatesAndTheMailboxIsClosed`

## Known Limitations

1. **In-memory duplicate suppression** — Resets on restart
2. **No UID tracking** — Relies on timestamp comparison only
3. **Single folder** — Only searches `INBOX`
4. **No pagination** — Fetches all matching UIDs at once
5. **Brittle subject match** — Exact string; Netflix could change subject
6. **Brittle URL regex** — Depends on specific token pattern in URL
7. **No retry** — Transient IMAP failures crash process