# Build and Deployment

## Build Pipeline

### Local Build

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Publish (self-contained, single-file)
dotnet publish NetflixHouseholdConfirmator/NetflixHouseholdConfirmator.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

### Build Outputs

| Command | Output Location | Description |
|---------|-----------------|-------------|
| `dotnet build` | `bin/Debug/net10.0/` | Debug build with PDBs |
| `dotnet build -c Release` | `bin/Release/net10.0/` | Optimised build |
| `dotnet publish` | `publish/` | Self-contained executable |

### Build Configuration

**Target Framework:** `net10.0` (all projects)

**Key csproj settings (executable):**
```xml
<OutputType>Exe</OutputType>
<TargetFramework>net10.0</TargetFramework>
<RootNamespace>NetflixHouseholdConfirmator</RootNamespace>
<ImplicitUsings>disable</ImplicitUsings>
```

**Test projects:**
```xml
<IsPackable>false</IsPackable>
<IsTestProject>true</IsTestProject>
<ExcludeFromCodeCoverage>true</ExcludeFromCodeCoverage>
<Nullable>enable</Nullable>
<LangVersion>latest</LangVersion>
```

## CI/CD Pipeline

### GitHub Actions (`.github/workflows/dotnet.yml`)

```yaml
name: .NET

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### Release Workflow (`.github/workflows/github-release.yml`)

Triggered on version tags (`v*`):
1. Build and test
2. Publish self-contained executables for linux-x64, win-x64, osx-x64
3. Create GitHub Release with artifacts

### NuGet Release (`.github/workflows/nuget-release.yml`)

Not applicable — this is an executable, not a library package.

## Deployment

### Deployment Model

**Single binary deployment** — Self-contained executable with no external .NET runtime dependency.

### Target Environments

| Environment | Runtime Identifier | Notes |
|-------------|-------------------|-------|
| Linux (x64) | `linux-x64` | Primary target; systemd service |
| Windows (x64) | `win-x64` | Windows Service or scheduled task |
| macOS (x64) | `osx-x64` | launchd or manual |
| Linux (ARM64) | `linux-arm64` | Raspberry Pi, etc. |

### Prerequisites

| Requirement | Details |
|-------------|---------|
| Browser | Chrome/Chromium or Firefox installed |
| WebDriver | Matching browser version; in PATH or same directory |
| IMAP Access | Network access to IMAP server (port 993) |
| Netflix Account | Valid credentials for household confirmation |

### Configuration Deployment

1. Copy `appsettings.json` to deployment directory
2. Replace placeholders with real values:
   ```json
   {
     "imapSettings": {
       "server": "mail.example.com",
       "username": "user@example.com",
       "password": "real-password"
     }
   }
   ```
3. Set file permissions: `chmod 600 appsettings.json`

### Linux systemd Service

```ini
# /etc/systemd/system/netflix-household-confirmator.service
[Unit]
Description=Netflix Household Confirmator
After=network.target

[Service]
Type=simple
User=netflix-confirmator
WorkingDirectory=/opt/netflix-household-confirmator
ExecStart=/opt/netflix-household-confirmator/NetflixHouseholdConfirmator
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable netflix-household-confirmator
sudo systemctl start netflix-household-confirmator
```

### Windows Service

Use NSSM or native .NET Windows Service (requires code changes for `IHostedService`).

### Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NetflixHouseholdConfirmator/NetflixHouseholdConfirmator.csproj", "NetflixHouseholdConfirmator/"]
RUN dotnet restore "NetflixHouseholdConfirmator/NetflixHouseholdConfirmator.csproj"
COPY . .
WORKDIR "/src/NetflixHouseholdConfirmator"
RUN dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY appsettings.json .
RUN apt-get update && apt-get install -y chromium chromium-driver
ENTRYPOINT ["./NetflixHouseholdConfirmator"]
```

Build and run:
```bash
docker build -t netflix-household-confirmator .
docker run -d \
  --name netflix-confirmator \
  -v /host/config:/app \
  netflix-household-confirmator
```

## Versioning

**Scheme:** Semantic Versioning (MAJOR.MINOR.PATCH)

**Source:** Git tags (`v1.0.0`, `v1.1.0`, etc.)

**Assembly Version:** Set via GitVersion or manually in csproj:
```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

## Release Process

1. Update version in csproj or use GitVersion
2. Commit changes
3. Create and push tag: `git tag v1.0.0 && git push origin v1.0.0`
4. GitHub Actions builds and creates release
5. Download artifacts from GitHub Release page
6. Deploy to target environments

## Rollback

1. Deploy previous version's executable
2. Keep `appsettings.json` (backward compatible)
3. Restart service

## Health Checks

**Current:** None built-in.

**Recommended:** Add HTTP health endpoint (requires ASP.NET Core) or use process supervision (systemd `Type=notify`, Windows Service recovery).

## Logs in Production

- File logs: `logfile.log` (configured in `appsettings.json`)
- Crash screenshots: Same directory as log file
- systemd journal: `journalctl -u netflix-household-confirmator -f`

## Monitoring

**Recommended:**
- Log aggregation (ELK, Loki, Seq)
- Alert on `Fatal` level logs
- Alert on process restarts (systemd `Restart=` count)
- Monitor IMAP connectivity separately