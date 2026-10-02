# Isthifa.AspNetCore.ExceptionHandling

[![Build](https://github.com/Isthifa/dotnetexceptionhandler/actions/workflows/ci.yml/badge.svg)](https://github.com/Isthifa/dotnetexceptionhandler/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Isthifa.AspNetCore.ExceptionHandling.svg)](https://www.nuget.org/packages/Isthifa.AspNetCore.ExceptionHandling)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Safe, consistent exception handling for ASP.NET Core APIs. The middleware turns known exceptions, malformed requests, validation failures, and unhandled errors into one predictable JSON contract while preserving logging and distributed trace correlation.

It has no hard dependency on .NET Aspire, FluentValidation, or a database provider. The package supports .NET 9 today and multi-targets .NET 9/.NET 10 when built with the .NET 10 SDK.

## Features

- Maps common API exceptions to the appropriate HTTP status code.
- Returns UTC timestamps, error codes, field violations, and OpenTelemetry-compatible trace IDs.
- Prevents stack traces, connection strings, inner exceptions, and unknown exception messages from reaching clients.
- Detects FluentValidation errors without requiring FluentValidation as a dependency.
- Formats ASP.NET Core controller invalid-model-state responses with the same contract.
- Works cleanly with Aspire service defaults, logging, health checks, and OpenTelemetry.

## Install

```bash
dotnet add package Isthifa.AspNetCore.ExceptionHandling
```

For local development, clone the repository and run:

```bash
dotnet test
dotnet pack -c Release
```

## Configure

Register the services, then add middleware **early in the pipeline, before endpoints are mapped**:

```csharp
using Isthifa.AspNetCore.ExceptionHandling.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGenericExceptionHandling(options =>
{
    options.Enabled = true;
    options.IncludeExceptionDetails = false; // default
    options.EnableTraceId = true;            // default
});

var app = builder.Build();
app.UseGenericExceptionHandling();

app.MapGet("/customers/{id}", (int id) =>
{
    throw new ResourceNotFoundException($"Customer {id} was not found");
});
app.Run();
```

### Options

| Option | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Enables or bypasses the middleware. |
| `IncludeExceptionDetails` | `false` | Reserved for safe, known exception details; stack traces and inner exceptions are never exposed. |
| `EnableTraceId` | `true` | Adds the current activity ID or request trace ID to the response. |

Set `Enabled` to `false` to leave the request pipeline untouched. Regardless of configuration, this package never serializes stack traces, connection strings, inner exceptions, or unknown exception messages.

## Exceptions

Use the exceptions from `Isthifa.AspNetCore.ExceptionHandling.Exceptions`:

| Exception | Status |
| --- | ---: |
| `BadRequestException` | 400 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ResourceNotFoundException` | 404 |
| `ConflictException` | 409 |
| `ValidationException` | 400 |

`ApiException` accepts any status, including 422:

```csharp
throw new ApiException(422, "The submitted state cannot be processed", "STATE_INVALID");
```

`ValidationException` optionally accepts `FieldViolation` values for field-specific messages. FluentValidation is detected at runtime by type name, so its package remains optional. ASP.NET Core invalid-model-state responses are also formatted with the same contract when MVC controllers are used.

## Response contract

All handled errors use `application/problem+json`, the standardized media type for HTTP API problem documents. The body keeps an intentionally compact, stable application schema:

```json
{
  "timestamp": "2026-10-02T17:00:00+00:00",
  "status": 404,
  "error": "Not Found",
  "message": "Customer 42 was not found",
  "errorCode": null,
  "traceId": "00-...",
  "violations": []
}
```

Timestamps are UTC. When tracing is enabled, `traceId` prefers `Activity.Current.Id` (the W3C/OpenTelemetry trace identifier), then falls back to `HttpContext.TraceIdentifier`. Unrecognized errors produce HTTP 500 with only `An unexpected error occurred`; full details are structured-logged through `ILogger`.

## .NET Aspire

No Aspire dependency is required. The middleware works with Aspire service defaults because it simply participates in the normal ASP.NET Core pipeline and uses the existing `Activity` and `ILogger` infrastructure:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddGenericExceptionHandling();

var app = builder.Build();
app.UseGenericExceptionHandling();
app.MapDefaultEndpoints();

app.MapGet("/customers/{id}", (int id) =>
{
    throw new ResourceNotFoundException($"Customer {id} was not found");
});

app.Run();
```

This leaves Aspire health checks, service discovery, structured logging, and OpenTelemetry setup intact.

## Logging and security

Unexpected exceptions are recorded with structured `ILogger` logging. Clients receive only HTTP 500 and `An unexpected error occurred`. Known `ApiException` messages are treated as client-safe by the calling application; do not place secrets or internal diagnostics in those messages.

## License

Released under the [MIT License](LICENSE).
