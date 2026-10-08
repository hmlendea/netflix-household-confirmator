# Repository Overview

## Purpose

Netflix Household Confirmator monitors a configured IMAP inbox for Netflix household update messages and uses Selenium-backed browser automation to confirm each detected request. It is a self-hosted .NET 10 console application with no inbound API, database, or distributed coordination.

## Scope

The repository contains:
- A single .NET console executable (`NetflixHouseholdConfirmator`)
- Unit tests (`NetflixHouseholdConfirmator.UnitTests`)
- Integration tests (`NetflixHouseholdConfirmator.IntegrationTests`)
- Configuration, logging, and documentation files

The application does **not**:
- Persist domain state in a database
- Expose an inbound API
- Implement a scheduler, queue, or cancellation mechanism
- Coordinate multiple running instances

## Entry Points

| Entry Point | Type | Description |
|-------------|------|-------------|
| `Program.Main` | Console entry | Composition root, configuration, WebDriver initialisation, DI setup, process lifecycle |
| `IHouseholdConfirmator.ConfirmIncomingHouseholdUpdateRequests` | Service method | Orchestration loop: login → poll → confirm → logout |
| `IEmailProcessor.GetHouseholdConfirmationUrl` | Processor method | IMAP message retrieval, filtering, URL extraction |
| `INetflixProcessor.ConfirmHousehold` | Processor method | Browser navigation and confirmation interaction |

## Repository Structure

```
NetflixHouseholdConfirmator/           # Main executable project
├── Program.cs                         # Composition root
├── appsettings.json                   # Configuration template
├── Configuration/                     # Typed settings
│   ├── BotSettings.cs
│   ├── DebugSettings.cs
│   └── ImapSettings.cs
├── Logging/                           # Logging vocabulary
│   ├── MyLogInfoKey.cs
│   └── MyOperation.cs
├── Service/                           # Core logic
│   ├── HouseholdConfirmator.cs        # Orchestrator
│   ├── IHouseholdConfirmator.cs       # Orchestrator interface
│   └── Processors/
│       ├── EmailProcessor.cs          # IMAP adapter
│       ├── IEmailProcessor.cs         # IMAP adapter interface
│       ├── NetflixProcessor.cs        # Browser automation adapter
│       ├── INetflixProcessor.cs       # Browser automation adapter interface
│       └── INetflixProcessor.cs
NetflixHouseholdConfirmator.UnitTests/ # Unit tests (NUnit + Moq)
NetflixHouseholdConfirmator.IntegrationTests/ # Integration tests (mock-based composition)
docs/                                  # Detailed documentation
├── INDEX.md                           # Master index
├── architecture.md                    # Detailed architecture
├── configuration.md                   # Configuration schema
├── dependencies.md                    # External dependencies
├── logging.md                         # Logging framework
├── error-handling.md                  # Error taxonomy
├── testing.md                         # Test strategy
├── build-and-deployment.md            # Build pipeline
├── change-guide.md                    # Modification guide
├── quick-start.md                     # Getting started
├── troubleshooting.md                 # Common issues
├── ambiguities-and-open-questions.md  # Known gaps
├── api-reference/                     # API reference (per controller)
├── behaviour/                         # User-facing behaviours
├── components/                        # Per-component deep dives
└── flows/                             # End-to-end execution flows
```

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10.0 |
| DI | Microsoft.Extensions.DependencyInjection |
| Configuration | Microsoft.Extensions.Configuration (JSON) |
| IMAP | MailKit 4.16.0 |
| Browser automation | NuciWeb (Selenium wrapper) |
| Logging | NuciLog |
| Testing | NUnit, Moq, Microsoft.NET.Test.Sdk, Coverlet |
| Build | `dotnet build` / `dotnet test` |

## Key Design Decisions

1. **Single-process polling** — No queue, scheduler, or cancellation; simplest implementation for a dedicated appliance
2. **In-memory duplicate suppression** — `lastConfirmationEmailDateTime` resets on restart; acceptable for single-instance deployment
3. **Interface-based processors** — `IEmailProcessor` and `INetflixProcessor` enable unit testing with mocks
4. **Exception propagation** — Failures bubble up to the host; host logs fatal and optionally captures screenshot
5. **No retry logic** — Transient failures (including IMAP timeouts) propagate immediately; no built-in reconnection