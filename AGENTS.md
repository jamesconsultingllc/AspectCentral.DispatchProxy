# Agent Instructions

> **Universal and library-specific directives for all AI agents working in this repository.**
> This is a single-project NuGet library — all rules live in this one file (no `src/` subtree).

## Variables

| Variable | Description | Example (Windows) | Example (macOS/Linux) |
|----------|-------------|--------------------|-----------------------|
| `${REPOS_ROOT}` | Root directory where tools/repos are cloned | `E:\tools` | `~/tools` |

> **Setup**: Set the `REPOS_ROOT` environment variable on your machine, or mentally substitute the correct path when reading these instructions.

---

## Vertical Slice Implementation

**Implement features as vertical slices — UI to datastore — not horizontal layers.**

When building a feature, complete the full stack for that feature before starting the next:

```
UI Component → API Client / Hook → API Endpoint → Service Layer → Data Access → Database Schema
```

### Rules

1. **One feature at a time** — Finish the entire vertical before moving on
2. **Start from the outside in** — Define the user-facing contract (UI/API shape) first, then work inward
3. **Tests at every layer** — Each slice includes tests for UI, API, service, and data access
4. **Commit per slice** — Each vertical slice should be a single, deployable commit
5. **No partial layers** — Never build "all the API endpoints" then "all the UI" — that's horizontal

### Workflow

```
1. Define the user story / acceptance criteria
2. Write BDD feature file (.feature) for the slice
3. Build UI component (with mock data / stub API)
4. Build API endpoint + service layer
5. Build data access + schema migration
6. Wire everything together
7. Run full vertical test suite (unit + integration + E2E)
8. Commit
```

---

## Workflow: Plan Before Coding

**STOP and PLAN before writing any code.**

1. **Understand the task** — Read the task/issue thoroughly
2. **Check ADO work items** — If using Azure DevOps, read the work item description and acceptance criteria
3. **Review existing code** — Understand the current implementation and patterns
4. **Plan the approach** — Outline what files need changes and why
5. **Only then implement** — One vertical slice at a time

---

## Session Management

### Maintain a `session.md` File

Every project should have a `session.md` (or `docs/session.md`) tracking:
- **Last completed task** — Work item ID, title, commit hash
- **Current task** — What's in progress
- **Next tasks** — What's queued up
- **Blockers** — Any issues preventing progress
- **Notes** — Important decisions or context

### When Starting Work

1. **Read `session.md`** to understand where we left off
2. **Read the ADO work item** (if applicable) for full context
3. **Update status** as you progress

### When Finishing a Task

1. **Update `session.md`** — Mark task complete with commit hash
2. **Prompt for next work** — Always end with a clear prompt:
   > *Task 474 is complete. The next task is **Task 475 - [title]**. Ready to proceed?*
3. **Never silently finish** — The user should always know what comes next

---

## Development Methodology: BDD/TDD First

**NO CODE WITHOUT TESTS FIRST.** This is non-negotiable.

### Implementation Order

```
Write failing test → Write minimum code to pass → Refactor
```

1. **BDD**: Write Gherkin `.feature` files defining expected behavior BEFORE implementation
2. **TDD**: Write unit tests that fail BEFORE writing production code
3. **Red-Green-Refactor**: Fail first, pass minimally, then clean up
4. **No exceptions**: Even "simple" changes get tests first

### Test Coverage

- **90% minimum** code coverage for all new code
- Unit tests for ALL business logic
- Integration tests for ALL API endpoints
- Security tests for EVERY endpoint
- Accessibility tests for EVERY UI component
- E2E tests for critical user flows (at least the happy path per vertical slice)

---

## Security Requirements (OWASP)

All code must follow **OWASP WSTG v4.2** and address **OWASP Top 10:2025**.

**References:**
- OWASP WSTG v4.2: https://owasp.org/www-project-web-security-testing-guide/v42/
- OWASP Top 10:2025: https://owasp.org/Top10/2025/
- **OWASP Cheat Sheet Series** (local): `${REPOS_ROOT}/owasp-cheatsheets/cheatsheets/`
- **OWASP Cheat Sheet Series** (web): https://cheatsheetseries.owasp.org/

### OWASP Cheat Sheets (Consult Before Implementation)

**Local copies are available at `${REPOS_ROOT}/owasp-cheatsheets/cheatsheets/`** — use these for faster, offline access.

| Feature Area | Local Cheat Sheet File |
|--------------|------------------------|
| **Authentication** | `Authentication_Cheat_Sheet.md`, `Password_Storage_Cheat_Sheet.md`, `Session_Management_Cheat_Sheet.md`, `Multifactor_Authentication_Cheat_Sheet.md` |
| **Authorization** | `Authorization_Cheat_Sheet.md`, `Access_Control_Cheat_Sheet.md`, `Insecure_Direct_Object_Reference_Prevention_Cheat_Sheet.md` |
| **Input Validation** | `Input_Validation_Cheat_Sheet.md`, `Injection_Prevention_Cheat_Sheet.md` |
| **SQL/Database** | `SQL_Injection_Prevention_Cheat_Sheet.md`, `Query_Parameterization_Cheat_Sheet.md`, `Database_Security_Cheat_Sheet.md` |
| **XSS Prevention** | `Cross_Site_Scripting_Prevention_Cheat_Sheet.md`, `DOM_based_XSS_Prevention_Cheat_Sheet.md`, `DOM_Clobbering_Prevention_Cheat_Sheet.md` |
| **CSRF Protection** | `Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.md` |
| **API Security** | `REST_Security_Cheat_Sheet.md`, `GraphQL_Cheat_Sheet.md`, `Web_Service_Security_Cheat_Sheet.md` |
| **Cryptography** | `Cryptographic_Storage_Cheat_Sheet.md`, `Key_Management_Cheat_Sheet.md`, `Transport_Layer_Security_Cheat_Sheet.md` |
| **File Handling** | `File_Upload_Cheat_Sheet.md` |
| **Error Handling** | `Error_Handling_Cheat_Sheet.md` |
| **Logging** | `Logging_Cheat_Sheet.md`, `Logging_Vocabulary_Cheat_Sheet.md` |
| **HTTP Security** | `HTTP_Headers_Cheat_Sheet.md`, `HTTP_Strict_Transport_Security_Cheat_Sheet.md`, `Content_Security_Policy_Cheat_Sheet.md` |
| **Multi-Tenancy** | `Multi_Tenant_Security_Cheat_Sheet.md` |
| **Secrets** | `Secrets_Management_Cheat_Sheet.md` |
| **Microservices** | `Microservices_Security_Cheat_Sheet.md`, `Docker_Security_Cheat_Sheet.md`, `Kubernetes_Security_Cheat_Sheet.md` |
| **AI/LLM** | `AI_Agent_Security_Cheat_Sheet.md`, `LLM_Prompt_Injection_Prevention_Cheat_Sheet.md`, `Secure_AI_Model_Ops_Cheat_Sheet.md` |
| **CI/CD** | `CI_CD_Security_Cheat_Sheet.md`, `Software_Supply_Chain_Security_Cheat_Sheet.md` |
| **Cloud/IaC** | `Secure_Cloud_Architecture_Cheat_Sheet.md`, `Infrastructure_as_Code_Security_Cheat_Sheet.md`, `Serverless_FaaS_Security_Cheat_Sheet.md` |
| **Threat Modeling** | `Threat_Modeling_Cheat_Sheet.md`, `Attack_Surface_Analysis_Cheat_Sheet.md`, `Abuse_Case_Cheat_Sheet.md` |

### OWASP Top 10:2025 Compliance

| Rank | Vulnerability | Key Mitigations |
|------|---------------|-----------------|
| **A01** | Broken Access Control | Deny by default, verify ownership, log failures |
| **A02** | Security Misconfiguration | Security headers, remove unused features |
| **A03** | Software Supply Chain Failures | Verify packages, use lockfiles, audit deps |
| **A04** | Cryptographic Failures | AES-256, Argon2id, TLS 1.2+, no hardcoded secrets |
| **A05** | Injection | Parameterized queries, input validation |
| **A06** | Insecure Design | Threat modeling, secure design patterns |
| **A07** | Authentication Failures | MFA, rate limiting, secure sessions |
| **A08** | Software/Data Integrity | Verify signatures, validate serialized data |
| **A09** | Logging & Alerting Failures | Log security events, protect logs |
| **A10** | Mishandling Exceptions | No stack traces leaked, fail securely |

### Security Checklist (Pre-Merge)

- [ ] All endpoints verify resource ownership (no IDOR)
- [ ] Security headers configured, no debug info in prod
- [ ] Dependencies audited, lockfiles used
- [ ] Strong encryption, no hardcoded secrets
- [ ] Parameterized queries, no XSS
- [ ] Threat model reviewed for new features
- [ ] Auth has rate limiting, secure session config
- [ ] Serialized data validated, signatures verified
- [ ] Security events logged (auth failures, access denied)
- [ ] Exceptions handled securely, no stack traces leaked

---

## Observability: Telemetry, Metrics & Logging

**Every application must be observable.** If you can't measure it, you can't fix it.

### Guiding Principles

1. **Instrument from day one** — Don't bolt on observability after launch
2. **Use OpenTelemetry (OTel)** — Prefer the vendor-neutral standard for traces, metrics, and logs
3. **Correlate everything** — Every log, metric, and trace must share a correlation/trace ID
4. **Structured over unstructured** — Always emit structured (key-value) logs, never free-text strings
5. **Low-cardinality metrics** — Labels must be bounded. Never use user IDs, request IDs, or URLs as metric labels

### Log Levels

| Level | When to use |
|-------|-------------|
| `Trace` | Ultra-verbose diagnostics — off in production |
| `Debug` | Developer-useful detail (cache hit/miss) — off in production by default |
| `Information` | Normal operations worth recording (request completed, job ran) |
| `Warning` | Unexpected but recoverable (retry, fallback, deprecated usage) |
| `Error` | Failure affecting the current operation but not the process |
| `Critical/Fatal` | Process-level failure, crash, unrecoverable state |

### What to Log

- Request start/end with duration
- Authentication and authorization outcomes (success and failure)
- External dependency calls (database, HTTP, queue) with duration and status
- Background job start/complete/fail
- Configuration changes at startup
- Feature flag evaluations

### What NEVER to Log

- Passwords, tokens, API keys, secrets
- Full credit card or SSN numbers
- PII unless explicitly consented and pseudonymized
- Request/response bodies in production (unless redacted)
- Health-check noise at `Information` level

### Distributed Tracing Rules

- Propagate trace context (`traceparent` / W3C Trace Context) across all service boundaries
- Create spans for every meaningful unit of work
- Add span attributes for domain-relevant data
- Record errors on spans — set status to `Error` and attach exception details
- Name spans clearly — `POST /api/orders`, `sql SELECT orders`

### Metrics Rules

- Use OTel instruments: Counter, Histogram, UpDownCounter, Gauge
- Name with dots following OTel semantic conventions
- Keep labels low-cardinality
- Capture **RED** metrics for services: Rate, Errors, Duration
- Capture **USE** metrics for resources: Utilization, Saturation, Errors

### Standard Metrics (Every Service)

| Metric | Type | Labels | Purpose |
|--------|------|--------|---------|
| `http.server.request.duration` | Histogram | `method`, `route`, `status_code` | Request latency |
| `http.server.active_requests` | UpDownCounter | `method`, `route` | Concurrency |
| `app.errors.total` | Counter | `type`, `operation` | Error rate |
| `db.client.operation.duration` | Histogram | `operation`, `collection` | DB latency |
| `app.queue.depth` | Gauge | `queue_name` | Queue backlog |
| `app.cache.hit_ratio` | Gauge | `cache_name` | Cache effectiveness |

### Health Checks

- Expose `/health` and `/ready` endpoints
- `/health` — Is the process alive?
- `/ready` — Can it serve traffic?
- Do not log health-check requests at `Information` level

### Alerting

| Severity | Meaning | Response |
|----------|---------|----------|
| **P1 / Critical** | Service down, data loss risk | Immediate page |
| **P2 / High** | Degraded for many users | Respond within 30 min |
| **P3 / Medium** | Degraded for some users | Respond within business hours |
| **P4 / Low** | Cosmetic or informational | Next sprint |

### Observability Checklist (Pre-Merge)

- [ ] Structured logging with correlation IDs on all new code paths
- [ ] No sensitive data in logs, traces, or metric labels
- [ ] Distributed tracing spans for external calls
- [ ] RED metrics for new endpoints/operations
- [ ] Health-check endpoints implemented and tested
- [ ] Log levels used correctly
- [ ] Alerts defined for critical failure paths with runbook links

---

## Code Documentation

- All public methods/classes: purpose, parameters, return values, exceptions
- Complex logic: inline comments for non-obvious algorithms
- Public APIs: request/response examples
- Configuration: all environment variables documented

---

## GitFlow Branching

**Always create feature branches from `develop`, never from `main`.**

| Branch Type | Create From | Merge To | Pattern |
|-------------|-------------|----------|---------|
| `feature/*` | `develop` | `develop` | `feature/descriptive-name` |
| `bugfix/*` | `develop` | `develop` | `bugfix/descriptive-name` |
| `release/*` | `develop` | `main` + `develop` | `release/x.y.z` |
| `hotfix/*` | `main` | `main` + `develop` | `hotfix/x.y.z` |

---

## Azure DevOps Integration

### Before Starting a Task

1. **Assign the work item** to the user
2. **Move to In Progress**
3. **Read the work item description** and acceptance criteria

### Task Workflow

1. Read the work item before starting implementation
2. Reference work item IDs in commits and PRs
3. Update work item status as you progress
4. Link commits/PRs to work items

---

## Core Principles (Priority Order)

1. **Vertical Slices** — Implement features UI-to-datastore, not layer-by-layer
2. **BDD/TDD** — Tests first, always
3. **Security First** — Designed into every feature from the start
4. **Accessibility** — WCAG 2.1 AA minimum, semantic HTML
5. **Localization** — All user-facing text localizable
6. **Mobile Responsiveness** — Mobile-first CSS approach
7. **Documentation** — Document all public APIs
8. **Observability** — Structured logging, metrics, telemetry
9. **SOLID Principles** — Clean architecture, dependency inversion
10. **DRY** — Extract reusable components, services, and utilities

---

# Class Library Agent Instructions (C# / .NET)

> **This is a reusable NuGet package, NOT an application.** Libraries expose hooks — they never configure infrastructure.

---

## Golden Rule

**A library must be usable with zero observability configured and still function correctly.**

- No Serilog / sink configuration
- No OTel exporter setup
- No `IHost` / `IHostBuilder` usage
- No HTTP status code awareness
- No middleware registration
- No `appsettings.json` reading (use `IOptions<T>` pattern)

The consuming application owns all configuration. The library provides the building blocks.

---

## Required Packages

### Core

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.Logging.Abstractions` | `ILogger<T>` interface (no concrete sinks) |
| `Microsoft.Extensions.Options` | `IOptions<T>` for configuration |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `IServiceCollection` extension methods |
| `System.Diagnostics.DiagnosticSource` | `ActivitySource` + `Meter` (built into .NET) |
| `MemoryPack` | High-performance binary serialization (if DTOs are shared) |
| `FluentValidation` | Validation rules (NOT `FluentValidation.AspNetCore`) |

### Testing

| Package | Purpose |
|---------|---------|
| `xUnit` | Unit test framework |
| `FluentAssertions` | Readable assertion syntax |
| `NSubstitute` | Mocking framework |
| `Bogus` | Realistic test data generation |
| `Reqnroll` + `Reqnroll.xUnit` | BDD / Gherkin `.feature` file runner |
| `Verify.Xunit` | Snapshot testing for complex outputs |
| `NetArchTest.Rules` | Enforce architecture rules |
| `Microsoft.Extensions.Logging.Testing` | `FakeLogger<T>` for asserting log output |
| `Microsoft.Extensions.Options` | `Options.Create<T>()` for test configuration |

### Code Quality

| Package | Purpose |
|---------|---------|
| `Microsoft.CodeAnalysis.PublicApiAnalyzers` | Track public API surface changes |
| `MinVer` or `GitVersion` | SemVer from git tags |
| `Microsoft.SourceLink.GitHub` | Source link for debugging NuGet consumers |

---

## Project Structure

```
src/
├── MyLib/
│   ├── MyLib.csproj
│   ├── Features/
│   │   ├── Parsing/
│   │   │   ├── DocumentParser.cs
│   │   │   ├── ParsingOptions.cs         # IOptions<T> config
│   │   │   └── ParsingErrors.cs          # Error code constants
│   │   └── Validation/
│   │       ├── RuleEngine.cs
│   │       └── RuleEngineErrors.cs
│   ├── Telemetry/
│   │   ├── LibraryActivitySources.cs     # Named ActivitySource constants
│   │   └── LibraryMeters.cs              # Named Meter constants
│   ├── DependencyInjection/
│   │   └── ServiceCollectionExtensions.cs # AddMyLib() registration
│   └── PublicAPI.Shipped.txt             # API surface tracking
│   └── PublicAPI.Unshipped.txt
tests/
├── MyLib.UnitTests/
│   └── Features/
│       ├── Parsing/
│       │   ├── DocumentParserTests.cs
│       │   └── Parsing.feature
│       └── Validation/
│           └── RuleEngineTests.cs
├── MyLib.ArchTests/
│   └── ArchitectureTests.cs
└── MyLib.IntegrationTests/
    └── ...
```

> **Note for this repo:** the actual layout is flat — `AspectCentral.DispatchProxy/` (library) and
> `AspectCentral.DispatchProxy.Tests/` (xUnit tests) live at the repo root, not under `src/` and `tests/`.
> Treat the structure above as the *target* pattern for new feature folders inside the library.

---

## Multi-Targeting

Support the two most recent .NET LTS/current versions:

```xml
<PropertyGroup>
  <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### Conditional Compilation

When using APIs only available in newer targets:

```csharp
#if NET10_0_OR_GREATER
    // Use new .NET 10 API
    return span.TrySplit(separator, out var left, out var right);
#else
    // Fallback for .NET 9
    var index = span.IndexOf(separator);
    if (index < 0) return false;
    left = span[..index];
    right = span[(index + 1)..];
    return true;
#endif
```

---

## NuGet Packaging

### Project File Metadata

```xml
<PropertyGroup>
  <PackageId>MyCompany.MyLib</PackageId>
  <Authors>My Company</Authors>
  <Description>Brief description of what the library does.</Description>
  <PackageTags>relevant;tags;here</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <RepositoryUrl>https://github.com/mycompany/mylib</RepositoryUrl>

  <!-- Source Link + deterministic builds -->
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>

<ItemGroup>
  <None Include="../../README.md" Pack="true" PackagePath="/" />
</ItemGroup>
```

### SemVer Rules

| Change | Version Bump | Examples |
|--------|-------------|----------|
| Bug fix, perf improvement | Patch (`1.0.x`) | Fix null check, optimize hot path |
| New public API (backward compatible) | Minor (`1.x.0`) | Add new method, new overload |
| Breaking change to public API | Major (`x.0.0`) | Remove method, change signature, rename type |

### API Surface Management

Use `PublicApiAnalyzers` to track breaking changes:

```
# PublicAPI.Shipped.txt — committed public API surface
MyLib.DocumentParser.Parse(string) -> MyLib.ParseResult
MyLib.ParseResult.IsSuccess.get -> bool
MyLib.ParseResult.Errors.get -> System.Collections.Generic.IReadOnlyList<string>
```

**Never** remove entries from `PublicAPI.Shipped.txt` without a major version bump.

---

## Logging (Library Pattern)

Libraries accept `ILogger<T>` via DI and use `[LoggerMessage]` source generator. They **never** configure sinks.

```csharp
/// <summary>
/// Source-generated log messages for the parsing feature.
/// Zero-allocation, compile-time checked, unique EventIds.
/// </summary>
public static partial class ParsingLogs
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Document {DocumentId} parsed in {ElapsedMs}ms — {PageCount} pages")]
    public static partial void DocumentParsed(
        ILogger logger, string documentId, double elapsedMs, int pageCount);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Document {DocumentId} parse failed: {Reason}")]
    public static partial void DocumentParseFailed(
        ILogger logger, string documentId, string reason);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug,
        Message = "Parsing page {PageNumber}/{TotalPages} of document {DocumentId}")]
    public static partial void ParsingPage(
        ILogger logger, int pageNumber, int totalPages, string documentId);
}

// Usage — consumer provides the ILogger, library just uses it
public class DocumentParser
{
    private readonly ILogger<DocumentParser> _logger;

    public DocumentParser(ILogger<DocumentParser> logger)
    {
        _logger = logger;
    }

    public ParseResult Parse(string documentId, Stream content)
    {
        var sw = Stopwatch.StartNew();
        // ... parsing logic ...
        ParsingLogs.DocumentParsed(_logger, documentId, sw.Elapsed.TotalMilliseconds, pageCount);
        return result;
    }
}
```

### EventId Ranges

Assign non-overlapping EventId ranges per feature to avoid collisions:

| Feature | EventId Range |
|---------|--------------|
| Parsing | 2001–2099 |
| Validation | 2100–2199 |
| Caching | 2200–2299 |

---

## Telemetry (Library Pattern)

Libraries **expose** named `ActivitySource` and `Meter` instances. They **never** configure exporters.

### Expose Named Sources

```csharp
/// <summary>
/// Named ActivitySource and Meter for the library.
/// Consumers register these with their OTel pipeline:
///   .WithTracing(b => b.AddSource(LibraryActivitySources.Parsing))
///   .WithMetrics(b => b.AddMeter(LibraryMeters.Parsing))
/// </summary>
public static class LibraryActivitySources
{
    /// <summary>Tracing source for document parsing operations.</summary>
    public const string Parsing = "MyCompany.MyLib.Parsing";

    /// <summary>Tracing source for validation operations.</summary>
    public const string Validation = "MyCompany.MyLib.Validation";

    /// <summary>All sources — for easy bulk registration by consumers.</summary>
    public static readonly IReadOnlyList<string> All = [Parsing, Validation];
}

public static class LibraryMeters
{
    /// <summary>Meter for parsing metrics.</summary>
    public const string Parsing = "MyCompany.MyLib.Parsing";

    /// <summary>All meters — for easy bulk registration by consumers.</summary>
    public static readonly IReadOnlyList<string> All = [Parsing];
}
```

### Usage in Library Code

```csharp
public class DocumentParser
{
    private static readonly ActivitySource s_activity = new(LibraryActivitySources.Parsing);
    private static readonly Meter s_meter = new(LibraryMeters.Parsing);
    private static readonly Counter<long> s_docsParsed = s_meter.CreateCounter<long>(
        "mylib.documents.parsed", "documents", "Total documents parsed");
    private static readonly Histogram<double> s_parseDuration = s_meter.CreateHistogram<double>(
        "mylib.documents.parse_duration", "ms", "Document parse duration");

    public ParseResult Parse(string documentId, Stream content)
    {
        using var activity = s_activity.StartActivity("ParseDocument");
        activity?.SetTag("document.id", documentId);

        var sw = Stopwatch.StartNew();
        try
        {
            // ... parsing logic ...
            s_docsParsed.Add(1);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
        finally
        {
            s_parseDuration.Record(sw.Elapsed.TotalMilliseconds);
            activity?.SetTag("document.page_count", pageCount);
        }
    }
}
```

### Document Registration for Consumers

In your library's README and DI extension:

```csharp
/// <summary>
/// Registers MyLib services. Call in the consuming application's DI setup.
/// </summary>
/// <example>
/// services.AddMyLib(options => { options.MaxPageSize = 100; });
///
/// // Register telemetry sources:
/// services.AddOpenTelemetry()
///     .WithTracing(b => b.AddSource(LibraryActivitySources.All.ToArray()))
///     .WithMetrics(b => b.AddMeter(LibraryMeters.All.ToArray()));
/// </example>
public static IServiceCollection AddMyLib(
    this IServiceCollection services,
    Action<MyLibOptions>? configure = null)
{
    if (configure is not null)
        services.Configure(configure);

    services.AddSingleton<DocumentParser>();
    services.AddSingleton<RuleEngine>();
    return services;
}
```

---

## Error Codes (Library Pattern)

Libraries define domain error codes as **constants**. They never map to HTTP status codes — that's the consuming application's job.

### Pattern: Error Code Constants

```csharp
/// <summary>
/// Error codes for the parsing feature.
/// Consuming applications map these to HTTP status codes and localized messages.
/// </summary>
public static class ParsingErrors
{
    public const string DocumentTooLarge = "PARSING_DOCUMENT_TOO_LARGE";
    public const string UnsupportedFormat = "PARSING_UNSUPPORTED_FORMAT";
    public const string CorruptedContent = "PARSING_CORRUPTED_CONTENT";
    public const string PageLimitExceeded = "PARSING_PAGE_LIMIT_EXCEEDED";
}

public static class ValidationErrors
{
    public const string RuleNotFound = "VALIDATION_RULE_NOT_FOUND";
    public const string InvalidExpression = "VALIDATION_INVALID_EXPRESSION";
}
```

### Pattern: Typed Exceptions (No HTTP)

```csharp
/// <summary>
/// Base exception for library errors. Contains a domain error code.
/// The consuming application's exception middleware maps these to HTTP responses.
/// </summary>
public class MyLibException : Exception
{
    /// <summary>Domain error code (e.g., PARSING_DOCUMENT_TOO_LARGE).</summary>
    public string Code { get; }

    /// <summary>Optional structured details for the error.</summary>
    public object? Details { get; }

    public MyLibException(string code, string message, object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }
}

/// <summary>Thrown when a document cannot be parsed.</summary>
public class ParsingException : MyLibException
{
    public ParsingException(string code, string message, object? details = null)
        : base(code, message, details) { }
}

// Usage in library code:
throw new ParsingException(
    ParsingErrors.DocumentTooLarge,
    $"Document exceeds maximum size of {maxBytes} bytes",
    new { ActualSize = content.Length, MaxSize = maxBytes });
```

### FluentValidation with Error Codes

```csharp
/// <summary>
/// Validation rules for parsing options.
/// Every rule has a .WithErrorCode() — no exceptions.
/// </summary>
public class ParsingOptionsValidator : AbstractValidator<ParsingOptions>
{
    public ParsingOptionsValidator()
    {
        RuleFor(x => x.MaxPageSize)
            .GreaterThan(0)
            .WithErrorCode(ParsingErrors.PageLimitExceeded)
            .WithMessage("MaxPageSize must be greater than 0");

        RuleFor(x => x.SupportedFormats)
            .NotEmpty()
            .WithErrorCode(ParsingErrors.UnsupportedFormat)
            .WithMessage("At least one supported format is required");
    }
}
```

---

## Configuration (IOptions Pattern)

Libraries accept configuration via `IOptions<T>`, never by reading files directly:

```csharp
/// <summary>
/// Configuration options for the library.
/// Consumers bind this from appsettings.json, environment variables, etc.
/// </summary>
public class MyLibOptions
{
    /// <summary>Maximum number of pages to parse per document.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Supported document formats.</summary>
    public List<string> SupportedFormats { get; set; } = ["pdf", "docx", "xlsx"];

    /// <summary>Enable detailed parsing telemetry (debug-level spans).</summary>
    public bool DetailedTelemetry { get; set; } = false;
}
```

---

## MemoryPack (Library DTOs)

If the library exposes DTOs shared between client and server:

```csharp
/// <summary>
/// Version-tolerant DTO for cross-service use.
/// Use [MemoryPackOrder] for safe schema evolution.
/// </summary>
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class ParseResult
{
    [MemoryPackOrder(0)] public bool IsSuccess { get; set; }
    [MemoryPackOrder(1)] public int PageCount { get; set; }
    [MemoryPackOrder(2)] public IReadOnlyList<string> Errors { get; set; } = [];
    [MemoryPackOrder(3)] public byte[]? OutputData { get; set; }
}
```

---

## Architecture Tests

```csharp
/// <summary>
/// Library must not depend on ASP.NET Core, hosting, or HTTP abstractions.
/// </summary>
[Fact]
public void Library_ShouldNot_DependOn_AspNetCore()
{
    Types.InAssembly(typeof(DocumentParser).Assembly)
        .ShouldNot()
        .HaveDependencyOnAny(
            "Microsoft.AspNetCore",
            "Microsoft.Extensions.Hosting",
            "System.Net.Http")
        .GetResult()
        .IsSuccessful.Should().BeTrue();
}

/// <summary>
/// All public ActivitySource and Meter names must follow the naming convention.
/// </summary>
[Fact]
public void TelemetrySources_ShouldFollow_NamingConvention()
{
    foreach (var source in LibraryActivitySources.All)
        source.Should().StartWith("MyCompany.MyLib.");

    foreach (var meter in LibraryMeters.All)
        meter.Should().StartWith("MyCompany.MyLib.");
}

/// <summary>
/// All exception classes must inherit from MyLibException.
/// </summary>
[Fact]
public void Exceptions_ShouldInherit_MyLibException()
{
    Types.InAssembly(typeof(DocumentParser).Assembly)
        .That().Inherit(typeof(Exception))
        .And().DoNotHaveNameMatching("MyLibException")
        .Should().Inherit(typeof(MyLibException))
        .GetResult()
        .IsSuccessful.Should().BeTrue();
}
```

---

## Documentation

### XML Docs Required for ALL Public APIs

```csharp
/// <summary>
/// Parses a document from the provided stream.
/// </summary>
/// <param name="documentId">Unique identifier for correlation and telemetry.</param>
/// <param name="content">Document content stream. Caller is responsible for disposal.</param>
/// <returns>Parse result indicating success/failure with page count and errors.</returns>
/// <exception cref="ParsingException">
/// Thrown with <see cref="ParsingErrors.DocumentTooLarge"/> when content exceeds max size.
/// Thrown with <see cref="ParsingErrors.UnsupportedFormat"/> when format is not recognized.
/// </exception>
/// <example>
/// <code>
/// var parser = serviceProvider.GetRequiredService&lt;DocumentParser&gt;();
/// using var stream = File.OpenRead("document.pdf");
/// var result = parser.Parse("doc-123", stream);
/// if (result.IsSuccess)
///     Console.WriteLine($"Parsed {result.PageCount} pages");
/// </code>
/// </example>
public ParseResult Parse(string documentId, Stream content) { ... }
```

Use `<inheritdoc/>` for interface implementations:

```csharp
/// <inheritdoc/>
public ParseResult Parse(string documentId, Stream content) { ... }
```

---

## What Does NOT Apply to Libraries

These sections from the shared rules above are **application-level concerns** — skip them for libraries:

- HTTP middleware, route guards, rate limiting
- Authentication / authorization / tenant isolation
- Health check endpoints (`/health`, `/ready`)
- Accessibility (a11y) / i18n / mobile responsiveness
- Frontend concerns (React, MSAL, UI components)
- `appsettings.json` / host configuration
- Database context / migrations (unless the library IS a data access library)

---

## Library Checklist (Pre-Merge)

- [ ] Multi-targets `net9.0;net10.0` (or current two supported versions)
- [ ] `PublicApiAnalyzers` tracks API surface — no unintentional breaking changes
- [ ] SemVer applied correctly (patch/minor/major per rules above)
- [ ] NuGet metadata complete (PackageId, Authors, Description, README, SourceLink)
- [ ] `ILogger<T>` via DI + `[LoggerMessage]` source generator — no sink configuration
- [ ] Named `ActivitySource` + `Meter` exposed as public constants with `.All` collection
- [ ] Error codes as constants — no HTTP status code awareness in library code
- [ ] All exceptions inherit from library base exception (`MyLibException`)
- [ ] FluentValidation rules have `.WithErrorCode()` on every rule
- [ ] `IOptions<T>` for configuration — library never reads files
- [ ] `AddMyLib()` extension method for DI registration
- [ ] Architecture tests verify no ASP.NET Core / hosting dependency
- [ ] XML docs on ALL public types, methods, properties
- [ ] BDD `.feature` files written before implementation
- [ ] 90%+ test coverage on new code (enforced in CI)
- [ ] `TreatWarningsAsErrors` enabled
- [ ] Deterministic builds enabled for CI
- [ ] README includes usage examples, telemetry registration, and error code reference

---

# Repo-Specific Context

## Commands

Build, test, and pack target multiple TFMs (`net5.0;netstandard2.1` for the library; `net5.0;netcoreapp3.1` for tests).

```sh
# Restore + build the whole solution
dotnet restore
dotnet build AspectCentral.DispatchProxy.sln --configuration Debug --no-restore

# Run all tests (xUnit) across all target frameworks
dotnet test AspectCentral.DispatchProxy.Tests/AspectCentral.DispatchProxy.Tests.csproj

# Run tests for a single framework
dotnet test AspectCentral.DispatchProxy.Tests/AspectCentral.DispatchProxy.Tests.csproj --framework net5.0

# Run a single test class or method (xUnit filter)
dotnet test --filter "FullyQualifiedName~BaseAspectTests"
dotnet test --filter "FullyQualifiedName=AspectCentral.DispatchProxy.Tests.BaseAspectTests.SomeTest"

# Pack the NuGet package (matches azure-pipelines.yml)
dotnet pack AspectCentral.DispatchProxy/AspectCentral.DispatchProxy.csproj --configuration Release
```

CI (`azure-pipelines.yml`) additionally runs SonarCloud analysis, signs the `.nupkg` with NuGetKeyVaultSignTool, and publishes artifacts. The version is composed from `VersionMajor.VersionMinor.VersionBuild` in the `.csproj`, suffixed with `-local` outside CI and `-$(BUILD_BUILDNUMBER)-preview` for Debug CI builds.

## Architecture

This library implements AOP for DI-registered services using `System.Reflection.DispatchProxy`. It plugs into `Microsoft.Extensions.DependencyInjection` and depends on the abstractions in the `AspectCentral.Abstractions` NuGet package (which defines `IAspectRegistrationBuilder`, `AspectRegistrationBuilder`, `AspectConfiguration`, `IAspectConfigurationProvider`, `AspectContext`, `MethodTypeOptions`, etc.). The two built-in aspects shipped here (`Logging`, `Profiling`) are reference implementations of the same pattern.

Three layers cooperate at runtime:

1. **Registration** — `ServiceCollectionExtensions.AddAspectSupport` returns a `DispatchProxyAspectRegistrationBuilder`. The builder is the entry point users call (e.g. `.AddAspectViaFactory<LoggingAspectFactory>()` or the `AddLogging`/`AddProfiling` extensions in `Logging/` and `Profiling/`) to record which aspects wrap which service. Registration also reflectively scans loaded assemblies for every concrete `IAspectFactory` and `TryAddSingleton`s them so they can be resolved later.

2. **Service-collection rewrite** — `ConfigureAspects` walks the `IServiceCollection` and, for every interface→implementation pair that has an `AspectConfiguration`, replaces the descriptor with a factory delegate that calls `InvokeCreateFactory` → `CreateFactory<TService>`. The implementation type is also re-registered concretely with the original lifetime so the proxy can resolve the real instance. `DispatchProxyAspectRegistrationBuilder.InvokeCreateFactory` is the analogous path used when aspects are added via the builder after the fact; it additionally supports `ImplementationFactory`-based descriptors.

3. **Proxy invocation** — `CreateFactory` composes the aspect chain by repeatedly resolving each `IAspectFactory` (in `AspectConfiguration.GetAspects()` order) and calling `factory.Create(innerInstance, implementationType)`. Each factory wraps the previous instance in a `DispatchProxy` derived from `BaseAspect<T>`. When a method on the proxied interface is called, `BaseAspect<T>.Invoke` builds an `AspectContext`, asks `AspectConfigurationProvider.ShouldIntercept(...)` whether to run the aspect, and then dispatches on `MethodTypeOptions`:
   - **Sync** → `Process` invokes the target and `PostInvoke` runs inline.
   - **Async action (`Task`)** → `ProcessAction` chains `PostInvoke` via `Task.ContinueWith`.
   - **Async function (`Task<T>`)** → reflection over `ProcessFunctionAsync<TK>` makes a generic method matching the result type, awaits the task, then runs `PostInvoke` in a `finally`.

   Subclasses (`LoggingAspect<T>`, `ProfilingAspect<T>`) override `PreInvoke`/`PostInvoke`. `BaseAspect<T>` caches `MethodInfo[]` per object type via `JamesConsulting.Constants.TypeMethods` and resolves the concrete implementation method (handling generic method definitions) so attribute/method lookups in `ShouldIntercept` work against the implementation type, not the interface.

### Adding a new aspect

To add an aspect, mirror `Logging/` or `Profiling/`: a `BaseAspect<T>` subclass overriding `PreInvoke`/`PostInvoke` (and a static `Create` that wires `Instance`, `ObjectType`, `Logger`, `AspectConfigurationProvider`, `FactoryType`), a `BaseAspectFactory` subclass whose `Create<T>` calls into it, and an `IAspectRegistrationBuilder` extension method. The factory will be auto-discovered by `RegisterAspectFactories` as long as it implements `IAspectFactory` and is non-abstract.
