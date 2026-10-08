# Configuration

## Configuration Schema

The application reads `appsettings.json` from its working directory during startup. Configuration is bound once at startup; changes require a process restart.

### Configuration File Location

| Scope | Path |
|-------|------|
| Application installation | `NetflixHouseholdConfirmator/appsettings.json` (copied to output directory) |

### Settings Sections

#### botSettings

| Key | Type | Default | Required | Description |
|-----|------|---------|----------|-------------|
| `pageLoadTimeout` | integer | 90 | Yes | Maximum browser page-load interval, in seconds |

**Class:** `NetflixHouseholdConfirmator.Configuration.BotSettings`

#### imapSettings

| Key | Type | Default | Required | Description |
|-----|------|---------|----------|-------------|
| `server` | string | — | Yes | IMAP server hostname |
| `port` | integer | 993 | Yes | SSL/TLS IMAP server port |
| `username` | string | — | Yes | IMAP account username |
| `password` | string | — | Yes | IMAP account password |
| `maxEmailAge` | integer | 1800 | Yes | Maximum eligible message age, in seconds |

**Class:** `NetflixHouseholdConfirmator.Configuration.ImapSettings`

#### debugSettings

| Key | Type | Default | Required | Description |
|-----|------|---------|----------|-------------|
| `crashScreenshotFileName` | string | `crash.png` | No | Crash screenshot filename; empty value deactivates capture |
| `isDebugMode` | boolean | `false` | No | Uses visible browser when `true`, headless when `false` |

**Class:** `NetflixHouseholdConfirmator.Configuration.DebugSettings`

**Computed Properties:**
- `IsHeadless` → `!IsDebugMode`
- `IsCrashScreenshotEnabled` → `!string.IsNullOrWhiteSpace(CrashScreenshotFileName)`

#### nuciLoggerSettings

| Key | Type | Default | Required | Description |
|-----|------|---------|----------|-------------|
| `minimumLevel` | string | `Debug` | Yes | Minimum recorded log level |
| `logFilePath` | string | `logfile.log` | When file logging or screenshots active | Destination for file logs and base directory for crash screenshots |
| `isFileOutputEnabled` | boolean | `true` | No | Activates file logging |

**Class:** `NuciLog.Configuration.NuciLoggerSettings` (from NuciLog package)

## Configuration Binding

**Location:** `NetflixHouseholdConfirmator/Program.cs` → `LoadConfiguration()`

```csharp
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true, true)
    .Build();

config.Bind(nameof(BotSettings), botSettings);
config.Bind(nameof(DebugSettings), debugSettings);
config.Bind(nameof(ImapSettings), imapSettings);
config.Bind(nameof(NuciLoggerSettings), loggerSettings);
```

- File is optional (`optional: true`) — missing file produces default/empty settings
- Reload on change is enabled (`reloadOnChange: true`) but **settings objects are not re-bound** after initial load
- Section names match class names exactly (case-sensitive)

## Reload Behaviour

**Current implementation:** Configuration values are bound during startup only. The `reloadOnChange: true` parameter enables file watching but the bound POCO instances are not updated. Restart the application after modifying `appsettings.json`.

## Secret Management

The IMAP password is read directly from `appsettings.json`. Security recommendations:

1. **Restrict file access** — Set filesystem permissions so only the application user can read the file
2. **Use dedicated application password** — When the email provider supports app-specific passwords, use one instead of the primary account password
3. **Never commit genuine credentials** — The repository `appsettings.json` contains placeholders (`[[IMAP_SERVER]]`, `[[IMAP_USERNAME]]`, `[[IMAP_PASSWORD]]`)
4. **Consider external secret stores** — For production deployments, consider using environment variables, Azure Key Vault, HashiCorp Vault, or similar (would require code changes to `LoadConfiguration`)

## Logging of Configuration

The application logs the following configuration values during operation:
- IMAP server, port, and username (via `MyLogInfoKey.Server`, `Port`, `Username`)
- **IMAP password is NOT logged** (excluded from `LogInfo` in `EmailProcessor.LogIn`)

File logs and crash screenshots can still contain sensitive operational context and require filesystem protection and retention management.

## Example Configuration

```json
{
  "botSettings": {
    "pageLoadTimeout": 90
  },
  "imapSettings": {
    "server": "mail.example.com",
    "port": 993,
    "username": "user@example.com",
    "password": "app-specific-password",
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

## Configuration Validation

**Current implementation:** No explicit validation. Missing or invalid values result in:
- Default values for value types (0 for int, false for bool)
- Null for reference types (string)
- Runtime failures when the values are used (e.g., null server → IMAP connection failure)

**Recommended improvement:** Add validation in `LoadConfiguration` or use data annotations with a validation library.

## Environment Variable Override

**Current implementation:** Not supported. The configuration builder only reads from `appsettings.json`. To add environment variable support:

```csharp
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", true, true)
    .AddEnvironmentVariables()
    .Build();
```

This would allow overriding settings via environment variables (e.g., `imapSettings__password`).