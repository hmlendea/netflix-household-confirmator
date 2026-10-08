# Concurrency and Scheduling

## Overview

The application is single-threaded with no concurrency primitives. The polling loop runs synchronously on the main thread.

## Threading Model

### Main Thread Only

```
Program.Main (Thread 1)
    ├── LoadConfiguration()
    ├── WebDriverInitialiser.InitialiseAvailableWebDriver()
    ├── CreateIOC()
    └── service.ConfirmIncomingHouseholdUpdateRequests()
        └── while (true)
            ├── emailProcessor.GetHouseholdConfirmationUrl()
            └── netflixProcessor.ConfirmHousehold()
```

**No background threads, no thread pool, no async/await.**

### Thread Safety

| Component | Thread-Safe? | Notes |
|-----------|-------------|-------|
| `EmailProcessor` | No | Single-threaded access |
| `NetflixProcessor` | No | Single-threaded access |
| `HouseholdConfirmator` | No | Single-threaded access |
| `ImapClient` | No | Single-threaded access |
| `IWebDriver` | No | Single-threaded access |
| Settings POCOs | Yes | Immutable after startup |
| `ILogger` | Yes | NuciLogger thread-safe |

## Async/Await

**Not used** — All methods are synchronous (`void` return, blocking calls).

| Method | Async? | Blocking Calls |
|--------|--------|----------------|
| `Program.Main` | No | All |
| `LoadConfiguration` | No | File I/O |
| `CreateIOC` | No | Container build |
| `ConfirmIncomingHouseholdUpdateRequests` | No | IMAP, browser |
| `EmailProcessor.LogIn` | No | `ImapClient.Connect`, `Authenticate` |
| `EmailProcessor.GetHouseholdConfirmationUrl` | No | `Inbox.Search`, `Inbox.Fetch`, `Inbox.GetMessage` |
| `NetflixProcessor.ConfirmHousehold` | No | `GoToUrl`, `Click`, `Wait` |

## Scheduling

### Polling Loop

```csharp
while (true)
{
    string confirmationUrl = emailProcessor.GetHouseholdConfirmationUrl();
    if (confirmationUrl is not null)
    {
        netflixProcessor.ConfirmHousehold(confirmationUrl);
    }
    // NO Thread.Sleep — tight loop!
}
```

**Critical Issue:** No sleep between iterations → 100% CPU usage, IMAP hammering.

**Expected:** `Thread.Sleep(75000)` (75 seconds) between iterations.

### No Timers

- No `System.Threading.Timer`
- No `System.Threading.ScheduledTask`
- No cron-like scheduling

### No Cancellation

- No `CancellationToken`
- No `CancellationTokenSource`
- `Ctrl+C` kills process (no graceful shutdown)

## Locks and Synchronization

**None used.** No `lock`, `Monitor`, `Mutex`, `Semaphore`, `ConcurrentDictionary`, etc.

## Parallel Processing

**None.** No `Parallel.For`, `Task.WhenAll`, `ThreadPool.QueueUserWorkItem`, etc.

## Process Model

### Single Process
- One executable instance
- One browser instance
- One IMAP connection
- No inter-process communication

### No Multi-Instance Coordination
- No file locks
- No distributed locks
- No leader election
- Multiple instances would process same emails

## Resource Contention

| Resource | Contention Risk | Mitigation |
|----------|----------------|------------|
| IMAP connection | None (single) | Single connection |
| Browser session | None (single) | Single WebDriver |
| Log file | None (single) | Single writer |
| Config file | None (read-only) | Read at startup only |

## Performance Characteristics

### CPU
- **Tight loop:** 100% CPU (bug — no sleep)
- **With sleep:** ~0% CPU (waiting)

### Memory
- **Stable:** No memory leaks detected
- **WebDriver:** Browser process memory (significant)
- **IMAP:** Message objects (transient)

### I/O
- **IMAP:** Network I/O per iteration
- **Browser:** Local IPC to WebDriver
- **Logs:** File I/O per log entry

## Concurrency Recommendations

1. **Add polling delay:** `Thread.Sleep(TimeSpan.FromSeconds(75))`
2. **Add cancellation:** `CancellationToken` for graceful shutdown
3. **Consider async:** `async Task Main` with `await` for I/O
4. **Add health check:** Background thread for liveness probe
5. **Consider `IHostedService`:** Proper lifecycle management

## Testing Concurrency

**Not applicable** — No concurrency to test.

**Integration tests** verify sequential behaviour only.