# Frequently Asked Questions

## General

### What does this application do?

Monitors an IMAP inbox for Netflix "Update Your Household" emails and automatically confirms them using browser automation.

### Is this an official Netflix tool?

No. This is an independent, open-source utility.

### What platforms are supported?

Linux, Windows, macOS (anywhere .NET 10 and a supported browser run).

## Configuration

### Where is the configuration file?

`NetflixHouseholdConfirmator/appsettings.json` (copied to output directory on build).

### Do I need to restart after changing config?

Yes. Configuration is loaded once at startup.

### What is an app-specific password?

A generated password for a specific application, separate from your main account password. Required by Gmail, Outlook, and others for IMAP access.

### Can I use environment variables instead of JSON?

Not currently. Only `appsettings.json` is supported.

## IMAP / Email

### Which email providers work?

Any IMAP provider (Gmail, Outlook, Yahoo, custom). Must support IMAP over TLS (port 993).

### Why am I getting authentication errors?

1. Use app-specific password (not main password)
2. Enable IMAP in provider settings
3. Check username is full email address
4. Verify server/port (usually 993 for SSL)

### Can I monitor multiple email accounts?

Not currently. Single IMAP connection only.

### How does duplicate detection work?

Tracks the timestamp of the last processed confirmation email (`email.Date.DateTime`). Resets on restart.

## Browser / Automation

### Which browsers are supported?

Chrome/Chromium, Firefox, Edge (auto-detected in that order).

### Do I need to install WebDriver separately?

Yes. ChromeDriver, GeckoDriver, or EdgeDriver must be in PATH or app directory.

### Why does it run headless by default?

For server/container deployment. Set `debugSettings.isDebugMode: true` for visible browser.

### Netflix login — how does it work?

The application does **not** handle login. You must have an existing browser session with Netflix logged in, or run once in debug mode to establish the session.

### What if Netflix changes their page?

Selectors in `NetflixProcessor.cs` will need updating. Check `data-uia` attributes in browser DevTools.

## Operation

### How often does it check for emails?

**Currently:** Continuously (bug — no delay). **Intended:** Every 75 seconds.

### Does it keep running forever?

Yes, until stopped (Ctrl+C, service stop, crash).

### What happens on crash?

- Logs fatal error
- Captures screenshot (if enabled)
- Exits (external supervisor should restart)

### Can I run multiple instances?

Not recommended — they'll process the same emails. No coordination mechanism.

## Troubleshooting

### "No WebDriver found"

Install ChromeDriver/GeckoDriver matching your browser version. Ensure in PATH.

### IMAP connection timeout

Check firewall, server/port, TLS support. Test: `openssl s_client -connect server:993`

### "Element not found" / timeout

Netflix UI changed. Update selectors in `NetflixProcessor.cs`. Run with `isDebugMode: true` to inspect.

### Confirmation not working

1. Check logs for `HouseholdConfirmation` entries
2. Run in debug mode to watch browser
3. Verify selectors match current Netflix page
4. Check if already confirmed (location details visible)

### Logs show success but household not confirmed

Known issue: success logged even after caught exception. Check for `Failure` logs before `Success`.

## Development

### How do I run tests?

```bash
dotnet test
```

### How do I build a release?

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### How do I add a new configuration setting?

1. Add property to settings class in `Configuration/`
2. Add to `appsettings.json`
3. No code changes needed (auto-bound)

### Where are the logs?

`logfile.log` in working directory (configurable via `nuciLoggerSettings.logFilePath`).

## Deployment

### Can I run this in Docker?

Yes. See `build-and-deployment.md` for Dockerfile example.

### How do I run as a systemd service?

See `build-and-deployment.md` for systemd unit file example.

### Does it need a database?

No. No persistent storage.

### What are the resource requirements?

Minimal: ~50MB RAM + browser memory (~200-500MB). CPU: low (with polling fix).

## Security

### Is my password safe?

- Stored in `appsettings.json` (protect with file permissions)
- Never logged
- Use app-specific password
- No network transmission except IMAP (TLS)

### Does it send data anywhere?

No. Only communicates with your IMAP server and Netflix (via browser).

## Known Issues

### Tight polling loop (100% CPU)

**Fixed in code:** Add `Thread.Sleep(75000)` in `HouseholdConfirmator` loop.

### Misleading success logs

**Known:** `HouseholdConfirmation` logs `Success` even after caught exception.

### No graceful shutdown

**Known:** `Ctrl+C` kills process; no in-flight completion.

### No retry logic

**Known:** Transient failures crash process.

### In-memory duplicate tracking

**Known:** Resets on restart; no UID persistence.

## Contributing

### How do I report a bug?

Open a GitHub issue with:
- Log excerpt (redacted)
- Configuration (redacted)
- Steps to reproduce
- Environment details

### How do I submit a fix?

Fork, create branch, make changes, run tests, submit PR.

### Code style?

Follow `.editorconfig` and C# conventions in `csharp.coding.instructions.md`.