# Repository Structure

## Source Tree Layout

```
netflix-household-confirmator/
├── ARCHITECTURE.md                    # High-level architecture overview
├── LICENSE                            # License terms
├── NetflixHouseholdConfirmator.slnx   # Solution file
├── PRIVACY.md                         # Privacy and personal data handling
├── README.md                          # User-facing documentation
├── release.sh                         # Release script
├── SECURITY.md                        # Security policy
├── docs/                              # Detailed implementation documentation
│   ├── INDEX.md                       # Master index and navigation
│   ├── architecture.md                # Detailed architecture
│   ├── repository-overview.md         # This file
│   ├── repository-structure.md        # Source tree layout
│   ├── configuration.md               # Configuration schema
│   ├── dependencies.md                # External dependencies
│   ├── logging.md                     # Logging framework
│   ├── error-handling.md              # Error taxonomy
│   ├── testing.md                     # Test strategy
│   ├── build-and-deployment.md        # Build pipeline
│   ├── change-guide.md                # Modification guide
│   ├── quick-start.md                 # Getting started
│   ├── troubleshooting.md             # Common issues
│   ├── ambiguities-and-open-questions.md # Known gaps
│   ├── api-reference/                 # API reference (internal interfaces)
│   │   └── INDEX.md
│   ├── behaviour/                     # User-facing behaviours
│   │   ├── email-processing.md
│   │   └── browser-confirmation.md
│   ├── components/                    # Per-component deep dives
│   │   ├── presentation.md
│   │   ├── host-and-composition.md
│   │   ├── application-services.md
│   │   └── integration-models.md
│   └── flows/                         # End-to-end execution flows
│       ├── startup-and-rendering.md
│       └── household-confirmation.md
├── NetflixHouseholdConfirmator/       # Main executable project
│   ├── NetflixHouseholdConfirmator.csproj
│   ├── Program.cs                     # Composition root
│   ├── appsettings.json               # Configuration template
│   ├── Configuration/                 # Typed settings (POCOs)
│   │   ├── BotSettings.cs
│   │   ├── DebugSettings.cs
│   │   └── ImapSettings.cs
│   ├── Logging/                       # Logging vocabulary
│   │   ├── MyLogInfoKey.cs
│   │   └── MyOperation.cs
│   └── Service/                       # Core business logic
│       ├── HouseholdConfirmator.cs    # Orchestrator
│       ├── IHouseholdConfirmator.cs   # Orchestrator interface
│       └── Processors/                # Integration adapters
│           ├── EmailProcessor.cs      # IMAP adapter
│           ├── IEmailProcessor.cs     # IMAP adapter interface
│           ├── NetflixProcessor.cs    # Browser automation adapter
│           ├── INetflixProcessor.cs   # Browser automation adapter interface
│           └── INetflixProcessor.cs
├── NetflixHouseholdConfirmator.UnitTests/  # Unit tests
│   ├── NetflixHouseholdConfirmator.UnitTests.csproj
│   ├── Configuration/
│   │   ├── BotSettingsTests.cs
│   │   └── DebugSettingsTests.cs
│   ├── Logging/
│   │   ├── MyLogInfoKeyTests.cs
│   │   └── MyOperationTests.cs
│   └── Service/
│       ├── HouseholdConfirmatorTests.cs
│       └── Processors/
│           ├── EmailProcessorTests.cs
│           └── NetflixProcessorTests.cs
└── NetflixHouseholdConfirmator.IntegrationTests/  # Integration tests
    ├── NetflixHouseholdConfirmator.IntegrationTests.csproj
    ├── Configuration/
    │   └── ConfigurationIntegrationTests.cs
    └── Service/
        ├── BrowserFailureStage.cs
        └── HouseholdConfirmationIntegrationTests.cs
```

## Module Organisation

### NetflixHouseholdConfirmator (Executable)

**Purpose:** Console application entry point and composition root.

**Key Files:**
- `Program.cs` — Loads config, initialises WebDriver, registers DI, runs orchestration
- `appsettings.json` — Configuration template with placeholders

**Subdirectories:**
- `Configuration/` — Typed settings classes bound from JSON
- `Logging/` — Application-specific log operation names and info keys
- `Service/` — Core orchestration and processor implementations

### NetflixHouseholdConfirmator/Configuration

**Purpose:** Strongly-typed configuration sections.

| File | Section | Properties |
|------|---------|------------|
| `BotSettings.cs` | `botSettings` | `PageLoadTimeout` (int, seconds) |
| `DebugSettings.cs` | `debugSettings` | `CrashScreenshotFileName` (string), `IsDebugMode` (bool), computed `IsHeadless`, `IsCrashScreenshotEnabled` |
| `ImapSettings.cs` | `imapSettings` | `Server` (string), `Port` (int), `Username` (string), `Password` (string), `MaxEmailAge` (int, seconds) |

### NetflixHouseholdConfirmator/Logging

**Purpose:** Structured logging vocabulary extending NuciLog.

| File | Purpose |
|------|---------|
| `MyOperation.cs` | Operation categories: `EmailLogIn`, `EmailLogOut`, `HouseholdConfirmation`, `ListenForConfirmationRequests` |
| `MyLogInfoKey.cs` | Structured log fields: `Server`, `Port`, `Username`, `Password`, `MaxAge` |

### NetflixHouseholdConfirmator/Service

**Purpose:** Core orchestration and integration adapters.

| File | Responsibility |
|------|----------------|
| `IHouseholdConfirmator.cs` | Orchestrator interface |
| `HouseholdConfirmator.cs` | Orchestrator implementation — login, poll, confirm, logout |
| `Processors/IEmailProcessor.cs` | IMAP adapter interface |
| `Processors/EmailProcessor.cs` | IMAP adapter — MailKit implementation |
| `Processors/INetflixProcessor.cs` | Browser automation adapter interface |
| `Processors/NetflixProcessor.cs` | Browser automation adapter — NuciWeb/Selenium implementation |

### NetflixHouseholdConfirmator.UnitTests

**Purpose:** Unit tests with mocked external boundaries.

**Organisation mirrors production:**
- `Configuration/` — Settings property tests
- `Logging/` — Vocabulary contract tests
- `Service/` — Orchestrator and processor behaviour tests

**Test Framework:** NUnit + Moq

### NetflixHouseholdConfirmator.IntegrationTests

**Purpose:** Integration tests composing real orchestration with mocked infrastructure.

**Key Files:**
- `Service/HouseholdConfirmationIntegrationTests.cs` — End-to-end mailbox-to-browser workflow tests
- `Service/BrowserFailureStage.cs` — Enum for parameterised browser failure injection
- `Configuration/ConfigurationIntegrationTests.cs` — Configuration binding verification

## Project Files

| File | Purpose |
|------|---------|
| `NetflixHouseholdConfirmator.csproj` | Executable project: target framework, package references |
| `NetflixHouseholdConfirmator.UnitTests.csproj` | Unit test project: NUnit, Moq, Coverlet |
| `NetflixHouseholdConfirmator.IntegrationTests.csproj` | Integration test project: same dependencies plus integration test setup |
| `NetflixHouseholdConfirmator.slnx` | Solution file referencing all three projects |

## Build Output

```
NetflixHouseholdConfirmator/bin/Debug/net10.0/
├── NetflixHouseholdConfirmator.dll
├── NetflixHouseholdConfirmator.exe (Windows)
├── NetflixHouseholdConfirmator (Linux/macOS)
├── appsettings.json (copied)
├── *.deps.json
├── *.runtimeconfig.json
└── NuciLog, MailKit, NuciWeb, Selenium assemblies
```

## Documentation Structure

```
docs/
├── INDEX.md                           # Master index
├── architecture.md                    # Detailed architecture
├── repository-overview.md             # Repository purpose and scope
├── repository-structure.md            # This file
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
├── api-reference/
│   └── INDEX.md                       # Internal interface reference
├── behaviour/
│   ├── email-processing.md            # IMAP message handling
│   └── browser-confirmation.md        # Netflix page interaction
├── components/
│   ├── presentation.md                # Console host
│   ├── host-and-composition.md        # DI and WebDriver lifecycle
│   ├── application-services.md        # Orchestration and processors
│   └── integration-models.md          # IMAP and browser adapters
└── flows/
    ├── startup-and-rendering.md       # Process startup
    └── household-confirmation.md      # End-to-end confirmation flow
```

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Projects | PascalCase | `NetflixHouseholdConfirmator` |
| Namespaces | PascalCase, matching folder | `NetflixHouseholdConfirmator.Service.Processors` |
| Classes | PascalCase | `EmailProcessor` |
| Interfaces | PascalCase with `I` prefix | `IEmailProcessor` |
| Methods | PascalCase | `GetHouseholdConfirmationUrl` |
| Properties | PascalCase | `PageLoadTimeout` |
| Private fields | camelCase | `lastConfirmationEmailDateTime` |
| Constants | PascalCase | `ConfirmationUrlPattern` |
| Test classes | PascalCase with `Tests` suffix | `EmailProcessorTests` |
| Test methods | `Given...When...Then...` | `GivenANewConfirmationEmail_WhenListening_ThenTheBrowserHandlesTheCurrentPageState` |
| Documentation files | kebab-case | `repository-structure.md` |
| Documentation directories | kebab-case, plural | `components/`, `flows/` |