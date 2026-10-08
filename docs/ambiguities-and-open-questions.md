# Ambiguities and Open Questions

## Unresolved Design Decisions

### 1. Duplicate Suppression Persistence
**Current:** In-memory `lastConfirmationEmailDateTime` resets on restart.
**Question:** Should processed email IDs be persisted to survive restarts?
**Options:**
- File-based storage (JSON/SQLite)
- IMAP flags (SEEN/FLAGGED) — requires IMAP UID tracking
- Accept current behaviour (single-instance appliance)

### 2. Retry and Backoff Strategy
**Current:** No retry logic; all failures propagate to host.
**Question:** Should transient failures (network, IMAP timeout) be retried?
**Options:**
- Polly policies with exponential backoff
- Simple retry count with fixed delay
- Leave to external orchestrator (systemd, Kubernetes)

### 3. Graceful Shutdown
**Current:** No `CancellationToken`; `Thread.Sleep` in loop; `Ctrl+C` kills process.
**Question:** Implement graceful shutdown with in-flight request completion?
**Impact:** Requires `IHostedService` or manual `CancellationToken` plumbing.

### 4. Multi-Instance Coordination
**Current:** No coordination; multiple instances would process same emails.
**Question:** Support multiple instances for HA?
**Options:**
- IMAP UID tracking with distributed lock (Redis, database)
- Leader election
- Single-instance only (document as limitation)

### 5. Health and Readiness Endpoints
**Current:** No HTTP endpoints.
**Question:** Add minimal HTTP server for health checks?
**Impact:** Adds ASP.NET Core dependency; changes deployment model.

### 6. Configuration Reload
**Current:** `reloadOnChange: true` but POCOs not re-bound.
**Question:** Implement live configuration reload?
**Options:**
- `IOptionsMonitor` with `AddOptions`
- SIGHUP handler
- Restart-only (current)

### 7. Structured Log Output Format
**Current:** NuciLog default format (console/file).
**Question:** Support JSON output for log aggregation?
**Options:**
- NuciLog JSON sink (if available)
- Custom sink
- External log shipper (Filebeat, Fluent Bit)

## Known Technical Debt

### 1. EmailProcessor Date Parsing (FIXED)
**Was:** `DateTime.Parse(email.Headers["DateReceived"], CultureInfo.InvariantCulture)` — threw `ArgumentNullException` when header missing.
**Fixed:** `email.Date.DateTime` — uses standard MIME Date header.
**Status:** ✅ Resolved in current codebase.

### 2. Hardcoded Polling Interval
**Location:** `HouseholdConfirmator.ConfirmIncomingHouseholdUpdateRequests()`
**Issue:** `Thread.Sleep(75000)` hardcoded (75 seconds).
**Should be:** Configurable via `BotSettings` or `ImapSettings`.

### 3. No Email UID Tracking
**Issue:** Duplicate suppression uses `DateTime` comparison only.
**Risk:** Clock skew, emails with same timestamp, restart loses state.
**Better:** Track IMAP UIDs of processed messages.

### 4. NetflixProcessor Selectors Brittle
**Issue:** XPath selectors depend on specific Netflix DOM structure.
**Risk:** Netflix UI changes break automation silently.
**Mitigation:** More robust selectors, fallback strategies, visual validation.

### 5. Single Browser Profile
**Issue:** New WebDriver instance each run; no persistent cookies.
**Impact:** May require manual login each restart.
**Fix:** Configure persistent browser profile directory.

### 6. No Input Validation on Configuration
**Issue:** Missing/invalid config values cause runtime failures.
**Fix:** Add validation in `LoadConfiguration` or use `System.ComponentModel.DataAnnotations`.

## Open Questions for Future Work

### 1. Metrics and Observability
- Should we export Prometheus metrics?
- Add OpenTelemetry traces?
- Custom metrics: emails processed, confirmations successful, failures by type?

### 2. Notification on Failure
- Email/webhook alert on fatal error?
- Integration with PagerDuty, Opsgenie, Slack?

### 3. Multi-Account Support
- Monitor multiple Netflix accounts/email addresses?
- Single process with multiple IMAP connections?
- Separate process per account?

### 4. Webhook/Event Output
- Emit event on successful confirmation?
- Integrate with home automation (Home Assistant, etc.)?

### 5. Container-First Deployment
- Optimise Docker image size?
- Distroless base image?
- ARM64 native build?

### 6. Configuration via Environment Variables
- Currently only JSON file.
- Add `AddEnvironmentVariables()` for 12-factor compliance?

### 7. Test Coverage Targets
- No enforced coverage thresholds.
- Add coverlet thresholds in CI?

## Decisions Deferred

| Topic | Deferred Until | Reason |
|-------|----------------|--------|
| Persistent duplicate suppression | Multi-instance requirement | YAGNI for single-instance |
| Retry policies | Observed production failure patterns | Premature optimisation |
| Health endpoints | Container/orchestration adoption | Not needed for systemd |
| Live config reload | Demonstrated need | Restart acceptable |
| JSON logging | Log aggregation adoption | Current format parseable |
| Multi-account | User request | Single account currently |

## Tracking

Review this document quarterly or when:
- New failure patterns emerge in production
- Deployment model changes (container, Kubernetes)
- User requests new capabilities
- Dependencies updated with breaking changes