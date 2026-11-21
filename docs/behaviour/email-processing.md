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
- `LogIn()` disconnects a still-connected client before reconnecting, so recovery can re-enter it

### Search Criteria
```csharp
IMailFolder inbox = imapClient.Inbox;
inbox.Open(FolderAccess.ReadOnly);
```

- Opens `INBOX` read-only on every polling iteration
- Re-selects the mailbox, so the message count is refreshed each iteration
- No server-side search query is issued

### Fetch Strategy
```csharp
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

- Reads messages from newest to oldest by index
- Fetches the full `MimeMessage` for every inspected message
- Stops at the first message older than `MaxEmailAge`, so older indexes are never fetched
- Does not scan further indexes after the stop point

### Session Recovery

`RetrieveRecentEmails()` retries a failed retrieval up to three attempts. When the failure is recoverable — an I/O, socket, or timeout error, a `ProtocolException`, or a `ServiceNotConnectedException`/`ServiceNotAuthenticatedException` — the processor logs the failure, calls `LogIn()` again, and re-reads the mailbox. A non-recoverable failure, or a failure on the final attempt, propagates to the caller.

- Connection loss no longer terminates the process on the first failure
- Retries are immediate; no delay or exponential backoff is applied
- A failed attempt re-fetches from the newest index, so no partial result is reused

## Filtering

### Subject Match
```csharp
private static string HouseholdUpdateEmailSubject
    => "How to update your Netflix Household";

if (email.Subject.Contains(HouseholdUpdateEmailSubject))
```

- Case-sensitive substring match
- Matches the Netflix email subject line
- No regex — simple string containment

### Duplicate Suppression
```csharp
private DateTimeOffset lastConfirmationEmailDateTime = DateTimeOffset.Now;

DateTimeOffset emailDateTime = email.Date;
if (emailDateTime > lastConfirmationEmailDateTime)
{
    lastConfirmationEmailDateTime = emailDateTime;
    return ExtractConfirmationUrlFromEmail(email);
}
```

- Tracks the newest processed message instant as a `DateTimeOffset`
- Uses the standard MIME `Date` header (`email.Date`), which carries its own offset
- The offset-bearing comparison prevents a non-UTC `Date` header from shifting the value into the future and suppressing later messages
- The timestamp is recorded before extraction, so a non-matching extraction still advances suppression
- In-memory only — resets on process restart
- **Fixed:** Previously used `email.Headers["DateReceived"]`, which could be null; a later revision used `email.Date.DateTime`, which discarded the offset

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
public static string Extract(string htmlBody)
{
    string normalisedBody = htmlBody.Replace(Environment.NewLine, string.Empty);
    string extractedUrl = Regex.Replace(
        normalisedBody,
        ConfirmationUrlPattern,
        ConfirmationUrlReplacement);

    if (string.Equals(extractedUrl, normalisedBody, StringComparison.Ordinal))
    {
        return normalisedBody;
    }

    return WebUtility.HtmlDecode(extractedUrl);
}
```

- Lives in `ConfirmationUrlExtractor`; `EmailProcessor.ExtractConfirmationUrlFromEmail()` delegates to it
- Processes the HTML body with `Environment.NewLine` removed
- HTML-decodes the matched URL, since the email's `href` value is HTML-escaped (`&amp;`)
- Returns the matched URL, or the unchanged HTML body when the pattern does not match
- No validation that the result is a usable Netflix URL

### HTML Escaping

- Netflix serialises the `href` query separators as `&amp;`, which is the correct HTML source form
- Without decoding, Selenium receives `&amp;g`, `&amp;lnktrk`, `&amp;operation`, `&amp;lkid`, so the `operation=update` parameter is lost and the confirmation page never renders its action button
- Decoding yields `https://www.netflix.com/account/update-primary-location?nftoken=…&g=…&lnktrk=EVO&operation=update&lkid=UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA`

## Return Values

| Condition | Return Value |
|-----------|--------------|
| Qualifying email found, URL extracted | Confirmation URL string |
| Qualifying email found, no URL match | Unchanged HTML body |
| No qualifying email | `null` |
| Non-recoverable or final-attempt failure | Propagates to caller |

## Error Scenarios

| Scenario | Exception | Handling |
|----------|-----------|----------|
| IMAP connection lost | `ImapProtocolException` | Logged, session re-established, retrieval retried |
| Connection timed out | `IOException` | Logged, session re-established, retrieval retried |
| Socket failure | `SocketException` | Logged, session re-established, retrieval retried |
| Stale or unauthenticated session | `ServiceNotConnectedException`, `ServiceNotAuthenticatedException` | Logged, session re-established, retrieval retried |
| Retrieval fails on the final attempt | Recoverable exception | Propagates; host logs fatal |
| Authentication rejected during retry | `AuthenticationException` | Propagates; host logs fatal |
| Subject does not match | None | Message skipped |
| HTML body has no URL marker | None | Unchanged body returned |

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

**Unit Tests:** `ConfirmationUrlExtractorTests` covers pattern matching, HTML-decoding, `operation=update` survival, newline removal, and the unchanged-body case; `EmailProcessorTests` covers construction

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
3. **Single folder** — Only reads `INBOX`
4. **Full-body fetch** — Retrieves the whole message for every inspected index
5. **Brittle subject match** — Exact string; Netflix could change the subject
6. **Brittle URL regex** — Depends on a specific token pattern in the URL
7. **Immediate retries** — Recovery retries have no delay or backoff, so a persistently failing server is contacted three times in quick succession

## Revision Note

The extraction path HTML-decodes the captured URL. Earlier revisions passed the escaped `&amp;` form to the browser, which produced a confirmation request without a valid `operation` parameter and made `set-primary-location-action` unavailable.