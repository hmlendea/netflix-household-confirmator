[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/netflix-household-confirmator)](https://github.com/hmlendea/netflix-household-confirmator/releases/latest)
[![Build Status](https://github.com/hmlendea/netflix-household-confirmator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/netflix-household-confirmator/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/hmlendea/netflix-household-confirmator)](https://github.com/hmlendea/netflix-household-confirmator/blob/master/LICENSE)

# Netflix Household Confirmator

Netflix Household Confirmator is a .NET console application that monitors an IMAP inbox and automatically confirms Netflix household update requests through browser automation.

## 📑 Table of Contents

- [Table of Contents](#table-of-contents)
- [Capabilities](#capabilities)
- [Usage](#usage)
- [Known Limitations](#known-limitations)
- [System Requirements](#system-requirements)
- [Installation](#installation)
  - [Manual Installation](#manual-installation)
- [Configuration](#configuration)
  - [Configuration Files](#configuration-files)
  - [Settings](#settings)
  - [Reload Behaviour](#reload-behaviour)
  - [Secret Management](#secret-management)
- [Compatibility](#compatibility)
- [Integrations](#integrations)
- [Authentication and Authorisation](#authentication-and-authorisation)
- [Privacy and Data](#privacy-and-data)
  - [Data Locations](#data-locations)
- [Development](#development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Coverage](#coverage)
  - [Continuous Integration](#continuous-integration)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [Project Structure](#project-structure)
  - [Projects and Packages](#projects-and-packages)
  - [Directories](#directories)
- [Contributing](#contributing)
- [Project Engagement](#project-engagement)
- [License](#license)

## ✨ Capabilities

- Monitors an IMAP inbox continuously for recent Netflix household update messages
- Extracts confirmation URLs from matching email content
- Confirms household requests through headless browser automation
- Supports visible browser execution for diagnostics
- Prevents repeated processing of the identical email during a process session
- Records structured logs and optional crash screenshots

## 🚀 Usage

Complete the required values in [appsettings.json](NetflixHouseholdConfirmator/appsettings.json), then start the executable from its extracted directory:

```bash
./NetflixHouseholdConfirmator
```

On Windows, execute `NetflixHouseholdConfirmator.exe`. The application continues polling until the process is terminated.

## ⚠️ Known Limitations

- Only messages received after the application starts are eligible for processing
- Email detection depends upon the subject containing `How to update your Netflix Household`
- Confirmation depends upon the current Netflix page structure and configured browser selectors

## 🖥️ System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Linux | An `arm`, `arm64`, or `x64` host supported by a published release | N/A |
| macOS | An `arm64` or `x64` host supported by a published release | N/A |
| Windows | An `arm64` or `x64` host supported by a published release | N/A |
| Browser automation | A Selenium-compatible browser and corresponding WebDriver | N/A |
| Email service | An SSL/TLS IMAP mailbox that receives Netflix household update messages | N/A |
| Network | Access to the configured IMAP server and Netflix | N/A |

## 📦 Installation

[![Obtain it from GitHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/github.png)](https://github.com/hmlendea/netflix-household-confirmator/releases)

### Manual Installation

1. Download the archive for your operating system and architecture from the [latest release](https://github.com/hmlendea/netflix-household-confirmator/releases/latest).
2. Extract the archive to the desired directory.
3. Ensure that a Selenium-compatible browser and corresponding WebDriver are available.
4. Populate the required placeholders in [appsettings.json](NetflixHouseholdConfirmator/appsettings.json).
5. Launch the executable as described in [Usage](#usage).

## ⚙️ Configuration

The application reads [appsettings.json](NetflixHouseholdConfirmator/appsettings.json) from its working directory during startup. Replace every IMAP placeholder before execution and retain the configuration file beside the executable.

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| [appsettings.json](NetflixHouseholdConfirmator/appsettings.json) | Application installation | Configures browser automation, IMAP access, diagnostics, and logging |

### Settings

The subsequent settings are recognised:
| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `botSettings` | `pageLoadTimeout` | `integer` | `90` | Yes | Maximum browser page-load interval, in seconds |
| `imapSettings` | `server` | `string` | — | Yes | IMAP server hostname |
| `imapSettings` | `port` | `integer` | `993` | Yes | SSL/TLS IMAP server port |
| `imapSettings` | `username` | `string` | — | Yes | IMAP account username |
| `imapSettings` | `password` | `string` | — | Yes | IMAP account password |
| `imapSettings` | `maxEmailAge` | `integer` | `1800` | Yes | Maximum eligible message age, in seconds |
| `debugSettings` | `crashScreenshotFileName` | `string` | `crash.png` | No | Crash screenshot filename; an empty value deactivates capture |
| `debugSettings` | `isDebugMode` | `boolean` | `false` | No | Uses a visible browser when `true` and headless execution when `false` |
| `nuciLoggerSettings` | `minimumLevel` | `string` | `Debug` | Yes | Minimum recorded log level |
| `nuciLoggerSettings` | `logFilePath` | `string` | `logfile.log` | When file logging or screenshots are active | Destination for file logs and the base directory for crash screenshots |
| `nuciLoggerSettings` | `isFileOutputEnabled` | `boolean` | `true` | No | Activates file logging |

### Reload Behaviour

Configuration values are bound during startup. Restart the application after modifying [appsettings.json](NetflixHouseholdConfirmator/appsettings.json).

### Secret Management

The application reads the IMAP password directly from [appsettings.json](NetflixHouseholdConfirmator/appsettings.json). Restrict access to this file, use a dedicated application password when the email provider supports one, and never commit genuine credentials.

## 🧩 Compatibility

| Component | Supported Versions | Notes |
|-----------|--------------------|-------|
| Published Linux executables | `arm`, `arm64`, `x64` | Available in release `v1.1.0` |
| Published macOS executables | `arm64`, `x64` | Available in release `v1.1.0` |
| Published Windows executables | `arm64`, `x64` | Available in release `v1.1.0` |
| Source compilation | .NET 10.0 | The project targets `net10.0` |

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| IMAP | SSL/TLS endpoint on the configured host and port | Retrieves household update messages | Yes |
| Netflix | Household update email and confirmation page | Confirms the household update request | Yes |
| Selenium-compatible browser | Browser and WebDriver recognised by NuciWeb Automation | Navigates and interacts with the confirmation page | Yes |

## 🔐 Authentication and Authorisation

The application authenticates to the configured IMAP server with the username and password from [appsettings.json](NetflixHouseholdConfirmator/appsettings.json). It does not request Netflix account credentials; browser automation accesses the confirmation URL extracted from the email.

## 🛡️ Privacy and Data

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| IMAP username and password | Authenticates to the configured mailbox | [appsettings.json](NetflixHouseholdConfirmator/appsettings.json) and process memory | Until the configuration is modified and the process exits | No |
| Recent email content and confirmation URLs | Identifies and confirms household update requests | Process memory only | Current process session | No |
| IMAP server, port, and username | Records connection diagnostics | Configured logger outputs | No application-level retention policy | No |
| Crash screenshot | Captures browser state after an automation failure | Directory containing the configured log file | Until manually deleted | Yes |

The logger records the IMAP server, port, and username, but does not record the IMAP password. Set `nuciLoggerSettings.isFileOutputEnabled` to `false` to deactivate persistent file logging, and set `debugSettings.crashScreenshotFileName` to an empty value to deactivate screenshots.

### Data Locations

| Platform or Scope | Location | Contents |
|-------------------|----------|----------|
| Application directory | [appsettings.json](NetflixHouseholdConfirmator/appsettings.json) | Runtime configuration and IMAP credentials |
| Configured logging destination | `nuciLoggerSettings.logFilePath` | Application logs when file output is active |
| Configured logging directory | `debugSettings.crashScreenshotFileName` | Optional crash screenshot |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- An IMAP mailbox and a Selenium-compatible browser with a corresponding WebDriver for local execution

### Setup

```bash
git clone https://github.com/hmlendea/netflix-household-confirmator.git
cd netflix-household-confirmator
dotnet restore
```

Populate the required placeholders in [appsettings.json](NetflixHouseholdConfirmator/appsettings.json) before local execution.

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project NetflixHouseholdConfirmator
```

### Test

The NUnit suite uses Moq to isolate IMAP and browser automation dependencies, so it requires no live mailbox or browser:

```bash
dotnet test
```

### Coverage

Generate a Cobertura report with Coverlet:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory NetflixHouseholdConfirmator.UnitTests/TestResults
```

### Continuous Integration

The [.NET workflow](.github/workflows/dotnet.yml) restores dependencies, compiles the project, and invokes the test target for pushes and pull requests targeting `master`:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.1.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| `coverlet.collector` | `6.0.4` | Development | Collects cross-platform code coverage |
| `MailKit` | `4.16.0` | Runtime | Retrieves and parses IMAP messages |
| `MailKit.Net` | `2.0.0` | Runtime | Provides IMAP network support |
| `Microsoft.Extensions.Configuration` | `10.0.5` | Runtime | Provides configuration abstractions |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.5` | Runtime | Binds configuration sections to typed settings |
| `Microsoft.Extensions.Configuration.Json` | `10.0.5` | Runtime | Loads JSON configuration |
| `Microsoft.Extensions.DependencyInjection` | `10.0.5` | Runtime | Constructs application services |
| `Microsoft.NET.Test.Sdk` | `18.0.1` | Development | Hosts and discovers .NET tests |
| `Moq` | `4.20.72` | Development | Creates isolated dependency substitutes |
| `NuciLog` | `1.1.2` | Runtime | Records structured application logs |
| `NuciLog.Core` | `2.6.0` | Runtime | Provides logging contracts and primitives |
| `NuciWeb` | `4.0.0` | Runtime | Provides web interaction abstractions |
| `NuciWeb.Automation` | `1.0.0` | Runtime | Defines browser automation contracts |
| `NuciWeb.Automation.Selenium` | `1.0.1` | Runtime | Implements browser automation through Selenium |
| `NUnit` | `4.4.0` | Development | Provides the unit-testing framework |
| `NUnit.Analyzers` | `4.10.0` | Development | Analyses NUnit test correctness |
| `NUnit3TestAdapter` | `6.0.0` | Development | Integrates NUnit with the .NET test host |

## 🗂️ Project Structure

The solution separates the executable application from its unit tests.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| [NetflixHouseholdConfirmator](NetflixHouseholdConfirmator/NetflixHouseholdConfirmator.csproj) | Console application | Monitors IMAP messages and confirms Netflix household requests |
| [NetflixHouseholdConfirmator.UnitTests](NetflixHouseholdConfirmator.UnitTests/NetflixHouseholdConfirmator.UnitTests.csproj) | NUnit test project | Verifies configuration, logging, orchestration, email processing, and browser automation logic |

### Directories

| Directory | Purpose |
|-----------|---------|
| [NetflixHouseholdConfirmator](NetflixHouseholdConfirmator) | Application source and runtime configuration |
| [NetflixHouseholdConfirmator.UnitTests](NetflixHouseholdConfirmator.UnitTests) | Unit tests and coverage configuration |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Add tests for additional or modified functionality

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/netflix-household-confirmator/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0`.
See [LICENSE](./LICENSE) for further information.