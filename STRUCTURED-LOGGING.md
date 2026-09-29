# STRUCTURED-LOGGING

## Current State

- Standard ASP.NET Core logging (`ILogger<T>`).
- Log level configured in `appsettings.json`:
  - Default: Information
  - Microsoft.AspNetCore: Warning
- No structured sinks (Seq, Elasticsearch, OpenTelemetry) yet.
- No correlation / request-id middleware yet.
- Seed / migration errors are logged with `LogError`.

## Recommended Next Steps (when implementing)

1. Add a correlation id middleware that pushes `X-Correlation-ID` into the logging scope.
2. Switch to structured logging with Serilog or the built-in OpenTelemetry logging.
3. Enrich every log with:
   - UserId (when authenticated)
   - Request path + method
   - CorrelationId
4. For authorization decisions, log at Information (or Debug) with the evaluated model (RBAC/ABAC/…), subject, resource, and outcome.

## Example Future Pattern

```csharp
_logger.LogInformation(
    "Authorization decision: Model={Model} Subject={UserId} Resource={Resource} Action={Action} Outcome={Outcome}",
    "RBAC", userId, resourceId, action, "Allow");
```

## Decision

Until a dedicated logging task is started, keep the default ASP.NET Core logger. Do not introduce heavy logging frameworks without an explicit request.
