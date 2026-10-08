# Change Guide

## Common Modification Scenarios

### Adding a New Configuration Setting

1. **Add property to settings class** (`Configuration/*.cs`):
   ```csharp
   public sealed class BotSettings
   {
       public int PageLoadTimeout { get; set; }
       public int NewSetting { get; set; }  // Add here
   }
   ```

2. **Update `appsettings.json`** with default value:
   ```json
   {
     "botSettings": {
       "pageLoadTimeout": 90,
       "newSetting": 42
     }
   }
   ```

3. **No code changes needed** — Configuration binding is automatic via `config.Bind(nameof(BotSettings), botSettings)`

4. **Add unit test** in `Configuration/BotSettingsTests.cs`

### Adding a New IMAP Header/Field

1. **Modify `EmailProcessor.GetHouseholdConfirmationUrl()`** or `ExtractConfirmationUrlFromEmail()`
2. **Use `email.Headers["Header-Name"]`** for custom headers
3. **Prefer standard MIME properties** (`email.Date`, `email.From`, `email.Subject`)
4. **Add unit test** for new extraction logic

### Changing Browser Selectors

1. **Locate selector constants** in `NetflixProcessor.ConfirmHousehold()`
2. **Update XPath/CSS selectors** to match new Netflix DOM
3. **Update integration tests** in `HouseholdConfirmationIntegrationTests.cs`:
   - `ConfirmButtonSelector`
   - `LocationDetailsSelector`
3. **Test manually** with real browser

### Adding a New Processor

1. **Create interface** in `Service/Processors/INewProcessor.cs`
2. **Create implementation** in `Service/Processors/NewProcessor.cs`
3. **Register in `Program.CreateIOC()`**:
   ```csharp
   .AddSingleton<INewProcessor, NewProcessor>()
   ```
4. **Inject into `HouseholdConfirmator`** constructor
5. **Add unit tests** and **integration tests**

### Modifying the Polling Loop

**Location:** `HouseholdConfirmator.ConfirmIncomingHouseholdUpdateRequests()`

**Current pattern:**
```csharp
while (true)
{
    string url = emailProcessor.GetHouseholdConfirmationUrl();
    if (!string.IsNullOrEmpty(url))
    {
        netflixProcessor.ConfirmHousehold(url);
    }
    Thread.Sleep(pollInterval);
}
```

**To add cancellation support:**
1. Add `CancellationToken` parameter
2. Check `cancellationToken.ThrowIfCancellationRequested()` in loop
3. Update `IHouseholdConfirmator` interface
4. Update `Program.Main` to pass token

### Changing Logging Operations/Fields

1. **Add to `MyOperation.cs`** or `MyLogInfoKey.cs`**
2. **Use in processors/orchestrator**
3. **Add vocabulary tests** in `Logging/*Tests.cs`

## Safe Refactoring Patterns

### Extract Method
- Keep method private
- Pass dependencies as parameters
- Update tests to cover new method

### Rename Class/Interface
1. Use IDE rename (updates all references)
2. Update namespace if moving files
3. Run tests to verify

### Change Method Signature
1. Update interface first
2. Update implementation
3. Update all call sites
4. Run tests

## Breaking Changes Checklist

Before merging changes that affect:
- [ ] Configuration schema → Document in `configuration.md`
- [ ] Public interfaces → Version bump (MAJOR)
- [ ] Log format → Update `logging.md`
- [ ] Test contracts → Update tests
- [ ] Deployment requirements → Update `build-and-deployment.md`

## Code Review Focus Areas

| Area | What to Check |
|------|---------------|
| Configuration | New settings have defaults, documented |
| IMAP | No plaintext passwords in logs, proper disposal |
| Browser | Selectors are robust, timeouts reasonable |
| Error handling | Exceptions logged with context, not swallowed silently |
| Tests | New behaviour covered, existing tests pass |
| Logging | New operations/fields follow naming conventions |

## Adding a New Test

### Unit Test
```csharp
[Test]
public void Given<Context>_When<Action>_Then<Outcome>()
{
    // Arrange
    // Act
    // Assert
}
```

### Integration Test
```csharp
[TestCase(BrowserFailureStage.NewStage)]
public void Given<Context>_When<Action>_Then<Outcome>(BrowserFailureStage stage)
{
    // Arrange with failure injection
    // Act
    // Assert
}
```

## Updating Documentation

When changing code, update corresponding docs:
- Configuration changes → `configuration.md`
- New dependencies → `dependencies.md`
- New log operations/fields → `logging.md`
- New error types → `error-handling.md`
- New test patterns → `testing.md`
- Architecture changes → `architecture.md`
- Component changes → `components/*.md`
- Flow changes → `flows/*.md`
- Behaviour changes → `behaviour/*.md`