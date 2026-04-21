[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/netflix-household-confirmator)](https://github.com/hmlendea/netflix-household-confirmator/releases/latest)
[![Build Status](https://github.com/hmlendea/netflix-household-confirmator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/netflix-household-confirmator/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# Netflix Household Confirmator

Automatically confirms Netflix household update emails by:

1. connecting to an IMAP inbox,
2. scanning recent messages for the Netflix household confirmation email,
3. extracting the confirmation URL from the email body,
4. opening that URL in a browser automation session,
5. clicking the confirmation button when needed.

The application runs continuously until stopped.

## How It Works

At startup, the application:

1. loads configuration from `appsettings.json`,
2. starts an available Selenium-compatible web driver,
3. logs into the configured IMAP account,
4. enters a loop that checks recent inbox messages,
5. confirms new Netflix household requests as they arrive.

The browser runs headless by default. Setting `debugSettings.isDebugMode` to `true` disables headless mode so you can watch the automation interact with the page.

## Requirements

- .NET SDK/runtime targeting `net10.0`
- Access to an IMAP mailbox that receives the Netflix confirmation emails
- A supported browser plus a compatible Selenium driver available on the machine
- Network access to both the IMAP server and Netflix

## Configuration

Edit `appsettings.json` before running the application.

### Example

```json
{
	"botSettings": {
		"pageLoadTimeout": 90
	},
	"imapSettings": {
		"server": "imap.example.com",
		"port": 993,
		"username": "user@example.com",
		"password": "your-password",
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

### Settings Reference

| Section | Key | Description |
| --- | --- | --- |
| `botSettings` | `pageLoadTimeout` | Page load timeout used by browser automation, in seconds. |
| `imapSettings` | `server` | IMAP server hostname. |
| `imapSettings` | `port` | IMAP server port. `993` is typical for IMAPS. |
| `imapSettings` | `username` | IMAP login username. |
| `imapSettings` | `password` | IMAP login password. |
| `imapSettings` | `maxEmailAge` | Maximum age, in seconds, for emails considered during polling. Older emails are ignored. |
| `debugSettings` | `crashScreenshotFileName` | File name used for a crash screenshot when browser automation fails. Leave empty to disable screenshots. |
| `debugSettings` | `isDebugMode` | Enables visible browser mode when `true`. Headless mode is used when `false`. |
| `nuciLoggerSettings` | `minimumLevel` | Minimum log level. |
| `nuciLoggerSettings` | `logFilePath` | Path to the log file. |
| `nuciLoggerSettings` | `isFileOutputEnabled` | Enables or disables file logging. |

The service will keep polling the inbox until you stop it.

## Logging And Debugging

- Application logs are written through `NuciLog`.
- If browser automation crashes and `crashScreenshotFileName` is configured, a screenshot is saved next to the log file.
- Set `debugSettings.isDebugMode` to `true` to run the browser in visible mode for troubleshooting.

## Operational Notes

- The application inspects the inbox of the configured IMAP account.
- It only considers relatively recent emails, based on `imapSettings.maxEmailAge`.
- It looks specifically for emails with the subject containing `How to update your Netflix Household`.
- It is intended to process new confirmation emails that arrive after the service starts.

## Development

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

### Publish

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from: `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

## Contributing

Contributions are welcome.

Please:

- keep changes cross-platform
- preserve public APIs unless the change is intentionally breaking
- keep pull requests focused and consistent with existing style
- update documentation when behaviour changes
- add or update tests for new behaviour

## License

Licensed under the GNU General Public License v3.0 or later.
See [LICENSE](./LICENSE) for details.