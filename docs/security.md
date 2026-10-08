# Security

## Overview

This document describes the security model, threat analysis, and mitigations for the Netflix Household Confirmator. Complements the root `SECURITY.md` policy document.

## Threat Model

### Assets

| Asset | Sensitivity | Protection |
|-------|-------------|------------|
| IMAP Password | High | Config file permissions, app-specific password |
| Netflix Session | High | Browser profile isolation, no credential storage |
| Confirmation URLs | Medium | One-time tokens, short-lived |
| Log Files | Medium | File permissions, no passwords logged |
| Crash Screenshots | Medium | File permissions, may contain PII |

### Attack Surface

| Entry Point | Threat | Mitigation |
|-------------|--------|------------|
| `appsettings.json` | Credential theft | File permissions (600), app-specific passwords |
| IMAP Connection | MITM, credential intercept | TLS 1.2+, certificate validation |
| Browser Automation | Session hijacking | Local WebDriver, no remote debugging |
| Log Files | Information disclosure | No passwords, restricted access |
| Crash Screenshots | PII exposure | Restricted directory, optional |

## Security Controls

### Credential Management

**IMAP Password:**
- Stored in `appsettings.json` (plaintext)
- **Never logged** (excluded from `LogInfo`)
- **Recommendation:** Use app-specific password
- **Recommendation:** Restrict file permissions: `chmod 600 appsettings.json`

**Netflix Credentials:**
- **Not stored** by application
- Relies on browser session cookies
- No programmatic login

### Network Security

**IMAP:**
- TLS enforced (`Connect(..., true)`)
- Certificate validation (MailKit default)
- Port 993 (IMAPS)

**Netflix:**
- HTTPS only (browser enforced)
- No custom certificate handling

**WebDriver:**
- Local process only (no remote WebDriver)
- No debugging port exposed

### Logging Security

**Structured Logging (NuciLog):**
- `MyLogInfoKey.Password` defined but **never used**
- `EmailProcessor.LogIn` explicitly omits password
- Confirmation URLs logged only at Debug level (if at all)

**Log File Protection:**
- Set directory permissions: `chmod 700 logs/`
- Rotate logs regularly
- Monitor for anomalous access

### Crash Screenshot Security

**Trigger:** Fatal exception + `debugSettings.IsCrashScreenshotEnabled`

**Content Risk:** May contain:
- Netflix page with account info
- Email content in browser
- URLs with tokens

**Mitigations:**
- Optional (disabled by default)
- Same directory as logs (protect together)
- Clean up old screenshots

## Data Flow Security

```
appsettings.json (600)
    → Program.LoadConfiguration()
    → ImapSettings (memory)
    → EmailProcessor.LogIn()
        → MailKit.Authenticate() (TLS)
    → NetflixProcessor.ConfirmHousehold()
        → Browser (HTTPS, existing session)
```

**No data leaves the system except:**
- IMAP traffic (encrypted)
- HTTPS to Netflix (encrypted)
- Log files (local)
- Crash screenshots (local)

## Vulnerability Assessment

### Current Vulnerabilities

| Vulnerability | Severity | Status |
|---------------|----------|--------|
| Plaintext password in config | Medium | Mitigated by file perms + app passwords |
| No config encryption | Medium | Acceptable for single-user appliance |
| Log file grows indefinitely | Low | Operational concern |
| Crash screenshots may contain PII | Low | Optional, directory protection |
| No dependency scanning | Medium | Add to CI |
| No SAST | Medium | Add to CI |

### Dependency Security

**Current:** No automated vulnerability scanning.

**Recommended:** Add to CI:
```yaml
- name: Security scan
  run: |
    dotnet list package --vulnerable --include-transitive
    # Or use GitHub Dependabot, Snyk, etc.
```

## Compliance

**Not applicable** — Personal appliance, no regulatory scope.

## Incident Response

### Credential Compromise

1. Revoke app-specific password immediately
2. Generate new app-specific password
3. Update `appsettings.json`
4. Restart service
5. Review logs for anomalous access

### Log Exposure

1. Rotate log files
2. Review access logs
3. Ensure file permissions correct

### Crash Screenshot Exposure

1. Delete screenshot files
2. Disable crash screenshots if not needed
3. Review directory permissions

## Secure Deployment Checklist

- [ ] `appsettings.json` permissions: `600` (owner read/write only)
- [ ] Log directory permissions: `700` (owner only)
- [ ] IMAP password is app-specific (not primary account password)
- [ ] Crash screenshots disabled or directory protected
- [ ] System user for service has minimal privileges
- [ ] Firewall: only outbound 993 (IMAP) and 443 (Netflix)
- [ ] No inbound ports open
- [ ] Dependency scanning in CI
- [ ] Regular `dotnet list package --vulnerable` checks

## Security Testing

**Manual:**
- Verify password not in logs: `grep -i password logfile.log`
- Verify file permissions: `ls -la appsettings.json logfile.log`
- Test IMAP TLS: `openssl s_client -connect server:993`

**Automated (Recommended):**
- Secret scanning in CI (GitHub Secret Scanning, TruffleHog)
- Dependency vulnerability scanning
- SAST (CodeQL, SonarQube)