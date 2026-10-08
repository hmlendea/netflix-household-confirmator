# Troubleshooting

## Startup Failures

### "No WebDriver found" / "Unable to locate WebDriver"

**Cause:** Selenium cannot find a compatible WebDriver executable.

**Solutions:**
1. Install ChromeDriver matching your Chrome version:
   ```bash
   # Linux
   sudo apt-get install chromium-chromedriver
   # Or download from https://chromedriver.chromium.org/
   ```
2. Install GeckoDriver for Firefox:
   ```bash
   sudo apt-get install firefox-geckodriver
   ```
3. Ensure WebDriver is in PATH or same directory as executable
4. Check `debugSettings.isDebugMode` — headless mode may require additional setup

### IMAP Connection Failed

**Symptoms:** `SslHandshakeException`, `SocketException`, timeout

**Checks:**
1. Verify `imapSettings.server` and `port` (usually 993 for SSL)
2. Test connectivity: `openssl s_client -connect mail.example.com:993`
3. Check firewall allows outbound port 993
4. Verify server supports TLS 1.2+
5. Try `imapSettings.port: 143` with `Connect(..., false)` for non-SSL (code change required)

### IMAP Authentication Failed

**Symptoms:** `AuthenticationException`, "Invalid credentials"

**Solutions:**
1. Use **app-specific password** (not main account password)
   - Gmail: Google Account → Security → App passwords
   - Outlook: Microsoft Account → Security → App passwords
2. Verify `imapSettings.username` is full email address
3. Check IMAP is enabled in email provider settings
4. Some providers require "less secure apps" enabled (deprecated)

### Configuration Not Loaded

**Symptoms:** Default values used (0, null, false)

**Checks:**
1. `appsettings.json` exists in working directory
2. JSON is valid (no trailing commas, correct quotes)
3. Section names match class names exactly (`botSettings`, `imapSettings`, etc.)
4. File permissions allow read access

## Runtime Failures

### "Element not found" / "Timeout waiting for element"

**Cause:** Netflix page structure changed; selectors outdated.

**Debug Steps:**
1. Set `debugSettings.isDebugMode: true` (visible browser)
2. Run and observe where it fails
3. Inspect page in browser DevTools
4. Update selectors in `NetflixProcessor.cs`:
   - `ConfirmButtonSelector`
   - `LocationDetailsSelector`
5. Rebuild and test

### Confirmation URL Not Extracted

**Cause:** Email format changed; regex no longer matches.

**Debug Steps:**
1. Check logs for `ListenForConfirmationRequests` — email subject logged?
2. Verify email subject contains "How to update your Netflix Household"
3. Check email body for `UPDATE_HOUSEHOLD_REQUESTED_OTP_CTA` pattern
4. Update `ConfirmationUrlPattern` regex in `EmailProcessor.cs`

### Duplicate Emails Processed

**Cause:** `lastConfirmationEmailDateTime` resets on restart; in-memory only.

**Workaround:** Process restarts lose duplicate suppression. Acceptable for single-instance deployment.

**Fix (if needed):** Persist `lastConfirmationEmailDateTime` to file/database.

### Crash Screenshot Not Saved

**Checks:**
1. `debugSettings.crashScreenshotFileName` not empty/null
2. `debugSettings.IsCrashScreenshotEnabled` returns `true`
3. Log file directory is writable
4. WebDriver supports `ITakesScreenshot` (most do)

## Log Analysis

### Enable Debug Logging

```json
{
  "nuciLoggerSettings": {
    "minimumLevel": "Debug",
    "isFileOutputEnabled": true
  }
}
```

### Key Log Patterns

| Pattern | Meaning |
|---------|---------|
| `EmailLogIn` → `Success` | IMAP connected and authenticated |
| `ListenForConfirmationRequests` → `Success` | Polling iteration completed |
| `HouseholdConfirmation` → `Success` | Browser confirmed household |
| `EmailLogOut` → `Success` | Clean shutdown |
| `Fatal` | Unhandled exception — check stack trace |

### Log File Location

Default: `logfile.log` in working directory (configurable via `nuciLoggerSettings.logFilePath`)

## Browser Issues

### Headless Mode Failures

**Common causes:**
- Missing display server (Linux headless)
- GPU acceleration issues
- Window size too small

**Fixes:**
```csharp
// In WebDriverInitialiser or custom setup
options.AddArgument("--headless=new");
options.AddArgument("--no-sandbox");
options.AddArgument("--disable-dev-shm-usage");
options.AddArgument("--window-size=1920,1080");
```

### WebDriver Version Mismatch

**Symptom:** `SessionNotCreatedException` with version mismatch message

**Fix:** Update WebDriver to match browser version exactly.

## Network Issues

### IMAP Timeout During Polling

**Symptom:** `ImapCommandException` or timeout in `GetHouseholdConfirmationUrl`

**Mitigation:**
- Increase `imapSettings.maxEmailAge` to reduce search scope
- Check network stability to IMAP server
- Consider adding retry logic (not currently implemented)

### Netflix Login Required

**Symptom:** Browser redirects to Netflix login page

**Cause:** Session expired or new browser profile

**Fix:**
1. Run with `isDebugMode: true` once to complete login manually
2. Cookies persist in WebDriver profile (if using persistent profile)
3. Or implement login automation in `NetflixProcessor`

## Getting Help

1. Check logs at `Debug` level
2. Enable `isDebugMode: true` to watch browser
3. Capture crash screenshot (ensure enabled)
4. Search existing issues in repository
5. Create new issue with:
   - Log excerpt (redact passwords)
   - `appsettings.json` (redacted)
   - Browser/OS/.NET versions
   - Steps to reproduce