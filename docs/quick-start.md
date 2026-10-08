# Quick Start

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| .NET SDK | 10.0 | `dotnet --version` |
| Browser | Chrome/Chromium or Firefox | Must match WebDriver version |
| WebDriver | Matching browser | In PATH or app directory |
| IMAP Account | — | With app-specific password recommended |

## Clone and Build

```bash
git clone <repository-url>
cd netflix-household-confirmator
dotnet restore
dotnet build
```

## Configure

1. Copy template:
   ```bash
   cp NetflixHouseholdConfirmator/appsettings.json NetflixHouseholdConfirmator/appsettings.local.json
   ```

2. Edit `appsettings.local.json` with your values:
   ```json
   {
     "botSettings": {
       "pageLoadTimeout": 90
     },
     "imapSettings": {
       "server": "mail.example.com",
       "port": 993,
       "username": "your-email@example.com",
       "password": "your-app-specific-password",
       "maxEmailAge": 1800
     },
     "debugSettings": {
       "crashScreenshotFileName": "crash.png",
       "isDebugMode": false
     },
     "nuciLoggerSettings": {
       "minimumLevel": "Debug",
       "logFilePath": "logfile.log",
       "isFileOutputEnabled": true
     }
   }
   ```

3. Rename or copy to `appsettings.json` in the working directory:
   ```bash
   cp NetflixHouseholdConfirmator/appsettings.local.json NetflixHouseholdConfirmator/appsettings.json
   ```

## Run

```bash
cd NetflixHouseholdConfirmator
dotnet run
```

Or run published executable:
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
./publish/NetflixHouseholdConfirmator
```

## Verify Operation

Watch the console output:
```
[Info] [StartUp] The service has started.
[Info] [EmailLogIn] [Started] Connecting to the IMAP server. {Server=mail.example.com, Port=993}
[Info] [EmailLogIn] [InProgress] Authenticating on the IMAP server. {Server=mail.example.com, Port=993, Username=your-email@example.com}
[Info] [EmailLogIn] [Success] Logged into the IMAP server. {Server=mail.example.com, Port=993, Username=your-email@example.com}
[Info] [ListenForConfirmationRequests] [Started] Listening for incoming household update requests.
[Info] [ListenForConfirmationRequests] [Success] Listening for incoming household update requests.
```

When a Netflix household update email arrives:
```
[Info] [HouseholdConfirmation] [Started] Confirming household update.
[Info] [HouseholdConfirmation] [Success] Household update confirmed.
```

## Stop

Press `Ctrl+C` to stop gracefully.

## Common Issues

| Issue | Solution |
|-------|----------|
| "No WebDriver found" | Install ChromeDriver/GeckoDriver matching browser version; ensure in PATH |
| IMAP authentication failed | Use app-specific password; verify server/port; check firewall |
| "Element not found" | Netflix UI changed; update selectors in `NetflixProcessor.cs` |
| Crash screenshot not saved | Check `debugSettings.crashScreenshotFileName` not empty; verify write permissions |

## Development Mode

Set `isDebugMode: true` in `debugSettings` to run with visible browser (non-headless) for debugging.

## Logs

- Console: Real-time structured logs
- File: `logfile.log` (configurable)
- Crash screenshots: Same directory as log file