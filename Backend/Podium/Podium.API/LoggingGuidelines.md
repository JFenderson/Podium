# Podium Application Logging Guidelines

This document provides guidelines and best practices for logging in the Podium application.

## Table of Contents
1. [Log Levels](#log-levels)
2. [Structured Logging](#structured-logging)
3. [PII Handling](#pii-handling)
4. [Correlation IDs](#correlation-ids)
5. [Logging Examples by Scenario](#logging-examples-by-scenario)
6. [Event IDs Reference](#event-ids-reference)
7. [Performance Considerations](#performance-considerations)
8. [Common Mistakes to Avoid](#common-mistakes-to-avoid)

---

## 1. Log Levels

Use appropriate log levels based on the severity and nature of the message.

### Log Level Guidelines

| Level | When to Use | Examples |
|-------|-------------|----------|
| **Trace** | Very detailed diagnostic information, typically only enabled in development | Variable values, loop iterations |
| **Debug** | Detailed information useful during development and troubleshooting | Method entry/exit, query parameters |
| **Information** | General informational messages about application flow | User login, API requests, successful operations |
| **Warning** | Potentially harmful situations that don't prevent operation | Slow queries, deprecated API usage, retries |
| **Error** | Error events that don't prevent the application from running | Failed operations, caught exceptions, validation errors |
| **Critical** | Critical failures that require immediate attention | Database unavailable, security breaches, data corruption |

### Examples

```csharp
// Trace: Very detailed, typically disabled in production
_logger.LogTrace("Entering ProcessVideoUpload with fileSize={FileSize}", fileSize);

// Debug: Development and troubleshooting
_logger.LogDebug("Query parameters: {Parameters}", queryParams);

// Information: Normal application flow
_logger.LogInformation(LoggingConstants.Authentication.LoginSuccess,
    "User {UserId} logged in successfully from {IpAddress}", userId, ipAddress);

// Warning: Something unexpected but not critical
_logger.LogWarning(LoggingConstants.Database.SlowQuery,
    "Query {QueryName} took {DurationMs}ms, exceeding threshold", queryName, duration);

// Error: Operation failed but application continues
_logger.LogError(LoggingConstants.VideoManagement.UploadFailed,
    "Video upload failed for user {UserId}: {Error}", userId, ex.Message);

// Critical: Immediate attention required
_logger.LogCritical(LoggingConstants.Database.ConnectionError,
    "Database connection failed - application cannot function");
```

---

## 2. Structured Logging

Always use structured logging with named properties instead of string interpolation.

### ✅ DO THIS (Structured Logging)

```csharp
_logger.LogInformation(
    "User {UserId} uploaded video {VideoId} with size {FileSizeBytes} bytes",
    userId, 
    videoId, 
    fileSizeBytes);
```

### ❌ DON'T DO THIS (String Interpolation)

```csharp
_logger.LogInformation($"User {userId} uploaded video {videoId} with size {fileSizeBytes} bytes");
```

### Why Structured Logging?

1. **Queryable**: Properties can be searched and filtered in Application Insights
2. **Type-safe**: Values maintain their types (numbers stay numbers)
3. **Performance**: No string concatenation overhead
4. **Consistency**: Easier to build dashboards and alerts

### Structured Logging in Queries

```kql
// With structured logging, you can query like this:
traces
| where customDimensions.UserId == "user123"
| where customDimensions.FileSizeBytes > 1000000
```

---

## 3. PII Handling

**NEVER** log Personally Identifiable Information (PII) or sensitive data.

### What is PII?

- Full names (first + last)
- Email addresses
- Phone numbers
- Social Security Numbers
- Credit card numbers
- Passwords
- API keys / Secrets
- JWT tokens
- IP addresses (in some jurisdictions)

### Logging User Information Safely

```csharp
// ✅ DO: Log user ID (pseudonymous identifier)
_logger.LogInformation("User {UserId} completed action", user.Id);

// ❌ DON'T: Log email or full name
_logger.LogInformation("User {Email} completed action", user.Email); // WRONG!

// ✅ DO: Log partially masked email for support
var maskedEmail = MaskEmail(user.Email); // "j***@example.com"
_logger.LogInformation("User {UserId} ({MaskedEmail}) completed action", 
    user.Id, maskedEmail);
```

### Logging Errors Without Exposing Secrets

```csharp
try
{
    await _azureStorageService.UploadAsync(connectionString, data);
}
catch (Exception ex)
{
    // ❌ DON'T: Exception message might contain connection string
    _logger.LogError("Upload failed: {Error}", ex.ToString()); // WRONG!
    
    // ✅ DO: Log sanitized error
    _logger.LogError("Upload failed to Azure Storage: {ErrorType}", ex.GetType().Name);
}
```

### The SensitiveDataFilter

The application automatically filters sensitive data patterns:
- Passwords (password=, pwd=)
- JWT tokens (Bearer ...)
- API keys (apikey=, api_key=)
- Credit cards (digit patterns)
- SSNs (###-##-####)

However, **still avoid logging this data intentionally**.

---

## 4. Correlation IDs

Use correlation IDs to trace requests across distributed systems.

### Automatic Correlation

The `RequestLoggingMiddleware` automatically:
1. Extracts or generates a correlation ID
2. Adds it to response headers
3. Includes it in all logs for that request

### Using Correlation IDs

```csharp
// Correlation ID is automatically available in LogContext
_logger.LogInformation("Processing request"); // Includes CorrelationId property

// Access correlation ID if needed
var correlationId = HttpContext.TraceIdentifier;
```

### Tracing a Request

In Application Insights, trace all events for a request:

```kql
let correlationId = "YOUR_CORRELATION_ID";
union requests, dependencies, exceptions, traces
| where operation_Id == correlationId or 
        customDimensions.CorrelationId == correlationId
| order by timestamp asc
```

### Manual Correlation for Background Jobs

```csharp
public async Task ProcessVideoAsync(int videoId, string correlationId)
{
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        _logger.LogInformation("Starting video processing for {VideoId}", videoId);
        // All logs in this scope will have the CorrelationId property
    }
}
```

---

## 5. Logging Examples by Scenario

### Authentication

```csharp
// Login attempt
_logger.LogInformation(LoggingConstants.Authentication.LoginAttempt,
    "Login attempt for user {Email} from {IpAddress}", 
    email,  // Consider masking: MaskEmail(email)
    ipAddress);

// Login success
_logger.LogInformation(LoggingConstants.Authentication.LoginSuccess,
    "User {UserId} logged in successfully", userId);

// Login failure
_logger.LogWarning(LoggingConstants.Authentication.LoginFailure,
    "Login failed for user {Email}: {Reason}", 
    MaskEmail(email), 
    "Invalid password");

// Track in Application Insights
_telemetryService.TrackAuthenticationAttempt(userId, success: true, reason: "");
```

### Video Upload

```csharp
// Upload start
_logger.LogInformation(LoggingConstants.VideoManagement.UploadStarted,
    "Video upload started for user {UserId}, filename {FileName}, size {FileSizeBytes} bytes",
    userId, fileName, fileSizeBytes);

var stopwatch = Stopwatch.StartNew();

try
{
    await _storageService.UploadAsync(file);
    
    stopwatch.Stop();
    
    // Upload success
    _logger.LogInformation(LoggingConstants.VideoManagement.UploadCompleted,
        "Video upload completed for user {UserId} in {DurationMs}ms", 
        userId, 
        stopwatch.ElapsedMilliseconds);
    
    // Track telemetry
    _telemetryService.TrackVideoUpload(
        userId, 
        fileSizeBytes, 
        stopwatch.Elapsed, 
        success: true);
}
catch (Exception ex)
{
    stopwatch.Stop();
    
    // Upload failure
    _logger.LogError(LoggingConstants.VideoManagement.UploadFailed,
        ex,
        "Video upload failed for user {UserId} after {DurationMs}ms", 
        userId, 
        stopwatch.ElapsedMilliseconds);
    
    _telemetryService.TrackVideoUpload(
        userId, 
        fileSizeBytes, 
        stopwatch.Elapsed, 
        success: false);
    
    throw;
}
```

### Scholarship Offers

```csharp
// Offer created
_logger.LogInformation(LoggingConstants.ScholarshipOffers.OfferCreated,
    "Scholarship offer {OfferId} created for student {StudentId} by band staff {BandStaffId}, amount ${Amount}",
    offerId, studentId, bandStaffId, amount);

// Offer accepted
_logger.LogInformation(LoggingConstants.ScholarshipOffers.OfferAccepted,
    "Scholarship offer {OfferId} accepted by student {StudentId}", 
    offerId, studentId);

// Offer rejected
_logger.LogInformation(LoggingConstants.ScholarshipOffers.OfferRejected,
    "Scholarship offer {OfferId} rejected by student {StudentId}, reason: {Reason}", 
    offerId, studentId, reason);

// Track conversion metric
_metricsService.TrackScholarshipOfferConversion(wasAccepted);
```

### Database Operations

```csharp
// Slow query warning
_logger.LogWarning(LoggingConstants.Database.SlowQuery,
    "Query {QueryName} took {DurationMs}ms, exceeding threshold of 500ms",
    "GetStudentsByInstrument", 
    durationMs);

// Track telemetry
_telemetryService.TrackDatabaseQuery("GetStudentsByInstrument", duration);

// Query failure
_logger.LogError(LoggingConstants.Database.QueryFailed,
    ex,
    "Query {QueryName} failed: {ErrorType}",
    "GetStudentsByInstrument",
    ex.GetType().Name);
```

### API Requests (Handled by Middleware)

The `RequestLoggingMiddleware` and `PerformanceTrackingMiddleware` automatically log:
- Request start
- Request completion with duration and status
- Slow requests (>2000ms)

No manual logging needed for basic request/response.

### Exception Handling

```csharp
try
{
    await _service.ProcessAsync(data);
}
catch (ValidationException vex)
{
    // Validation errors are expected - use Warning
    _logger.LogWarning("Validation failed for {Entity}: {Errors}", 
        entityName, 
        string.Join(", ", vex.Errors));
}
catch (NotFoundException nfex)
{
    // Not found - use Warning
    _logger.LogWarning("Entity not found: {EntityType} with ID {EntityId}",
        nfex.EntityType,
        nfex.EntityId);
}
catch (Exception ex)
{
    // Unexpected errors - use Error or Critical
    _logger.LogError(ex, 
        "Unexpected error processing {Entity} with ID {EntityId}",
        entityName, 
        entityId);
    
    // Track in Application Insights with context
    _telemetryService.TrackException(ex, new Dictionary<string, string>
    {
        { "EntityType", entityName },
        { "EntityId", entityId.ToString() },
        { "Operation", "Process" }
    });
    
    throw;
}
```

---

## 6. Event IDs Reference

Event IDs help categorize and filter logs. Use constants from `LoggingConstants`.

### Available Event ID Ranges

| Range | Category | Example Events |
|-------|----------|----------------|
| 1000-1099 | Authentication | Login, Registration, Token Refresh |
| 2000-2099 | Video Management | Upload, Transcoding, Delete |
| 3000-3099 | Scholarship Offers | Create, Accept, Reject |
| 4000-4099 | Database | Query, Migration, Connection |
| 5000-5099 | External APIs | SendGrid, Azure Storage, AWS S3 |
| 6000-6099 | Background Jobs | Hangfire jobs, Scheduled tasks |
| 7000-7099 | Performance | Slow requests, High memory |

### Using Event IDs

```csharp
using Podium.Core.Constants;

_logger.LogInformation(LoggingConstants.Authentication.LoginSuccess,
    "User logged in successfully");

_logger.LogError(LoggingConstants.VideoManagement.TranscodingFailed,
    "Video transcoding failed");
```

### Querying by Event ID

```kql
traces
| where customDimensions.EventId >= 1000 and customDimensions.EventId < 1100
| where customDimensions.EventId == 1001  // LoginSuccess
```

---

## 7. Performance Considerations

### Avoid Expensive Operations in Log Messages

```csharp
// ❌ DON'T: Serialize large objects for logging
_logger.LogDebug("User data: {UserData}", JsonSerializer.Serialize(userData)); // WRONG!

// ✅ DO: Log only necessary properties
_logger.LogDebug("User {UserId} with role {Role}", userData.Id, userData.Role);

// ❌ DON'T: Call methods that fetch data
_logger.LogDebug("Related records: {Records}", GetAllRelatedRecords()); // WRONG!

// ✅ DO: Use guard clause
if (_logger.IsEnabled(LogLevel.Debug))
{
    var records = GetAllRelatedRecords();
    _logger.LogDebug("Related records: {RecordCount}", records.Count);
}
```

### Log Level Performance

In production:
- `Information` and above: Minimal performance impact
- `Debug`: Moderate impact, use selectively
- `Trace`: Significant impact, disable in production

### Conditional Logging

```csharp
// For expensive operations, check if level is enabled first
if (_logger.IsEnabled(LogLevel.Debug))
{
    var detailedInfo = BuildDetailedDiagnosticInfo(); // Expensive
    _logger.LogDebug("Detailed info: {Info}", detailedInfo);
}
```

---

## 8. Common Mistakes to Avoid

### Mistake #1: Using String Interpolation

```csharp
// ❌ WRONG
_logger.LogInformation($"User {userId} performed action");

// ✅ CORRECT
_logger.LogInformation("User {UserId} performed action", userId);
```

### Mistake #2: Logging Sensitive Data

```csharp
// ❌ WRONG
_logger.LogInformation("Login for {Email} with password {Password}", email, password);

// ✅ CORRECT
_logger.LogInformation("Login attempt for user {UserId}", userId);
```

### Mistake #3: Not Including Context

```csharp
// ❌ WRONG - Too vague
_logger.LogError("Operation failed");

// ✅ CORRECT - Includes context
_logger.LogError("Video upload operation failed for user {UserId}, file {FileName}", 
    userId, fileName);
```

### Mistake #4: Logging Inside Loops

```csharp
// ❌ WRONG - Creates too many log entries
foreach (var item in items)
{
    _logger.LogInformation("Processing item {ItemId}", item.Id);
}

// ✅ CORRECT - Log summary
_logger.LogInformation("Processing {ItemCount} items", items.Count);
// Log only errors or significant events inside loop
```

### Mistake #5: Not Using Event IDs

```csharp
// ❌ WRONG - No event ID
_logger.LogInformation("User logged in");

// ✅ CORRECT - With event ID
_logger.LogInformation(LoggingConstants.Authentication.LoginSuccess,
    "User {UserId} logged in", userId);
```

### Mistake #6: Logging Exception ToString()

```csharp
// ❌ WRONG - May include sensitive data
_logger.LogError("Error occurred: {Error}", ex.ToString());

// ✅ CORRECT - Use proper exception logging
_logger.LogError(ex, "Error occurred during {Operation}", operationName);
```

### Mistake #7: Not Using Appropriate Log Levels

```csharp
// ❌ WRONG - Everything as Error
_logger.LogError("User {UserId} not found", userId); // Should be Warning

// ✅ CORRECT - Appropriate levels
_logger.LogWarning("User {UserId} not found", userId);
_logger.LogError(ex, "Database connection failed");
_logger.LogCritical("Application cannot start - configuration missing");
```

---

## Best Practices Summary

### DO:
- ✅ Use structured logging with named properties
- ✅ Use appropriate log levels
- ✅ Include relevant context (user ID, operation, etc.)
- ✅ Use Event IDs from `LoggingConstants`
- ✅ Log exceptions with context
- ✅ Use correlation IDs for tracing
- ✅ Log business-significant events
- ✅ Consider performance for high-frequency logs

### DON'T:
- ❌ Use string interpolation
- ❌ Log sensitive data (passwords, tokens, PII)
- ❌ Log inside tight loops
- ❌ Call expensive methods in log statements
- ❌ Use wrong log levels
- ❌ Log generic messages without context
- ❌ Ignore exception details

---

## Quick Reference

```csharp
using Microsoft.Extensions.Logging;
using Podium.Core.Constants;
using Podium.Core.Interfaces;

public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly ITelemetryService _telemetryService;
    
    public MyService(ILogger<MyService> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }
    
    public async Task PerformOperationAsync(string userId, DataDto data)
    {
        _logger.LogInformation(
            "Starting operation for user {UserId}", 
            userId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Do work
            await ProcessAsync(data);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Operation completed successfully in {DurationMs}ms", 
                stopwatch.ElapsedMilliseconds);
            
            _telemetryService.TrackApiEndpoint(
                "PerformOperation",
                stopwatch.Elapsed,
                statusCode: 200);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Operation failed for user {UserId} after {DurationMs}ms",
                userId,
                stopwatch.ElapsedMilliseconds);
            
            _telemetryService.TrackException(ex, new Dictionary<string, string>
            {
                { "UserId", userId },
                { "Operation", "PerformOperation" }
            });
            
            throw;
        }
    }
}
```

---

*Last Updated: January 2026*
