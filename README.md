# TmsApi — Training Management System API

TmsApi is a REST API for managing course offerings, student enrollments, instructor workflows, and academic records. It provides role-aware access for students, instructors, and administrators, with enrollment decisions, grading, reporting, and asynchronous transcript generation. This repository contains the .NET backend; no frontend application is included.

## Key Features

- **Course management:** browse and paginate courses; create, update, and delete courses with unique codes. Course updates are checked against instructor ownership, and deletion is blocked while non-archived enrollments exist.
- **Enrollment workflow:** submit enrollment requests, prevent duplicate or over-capacity enrollments, and let instructors or administrators approve or reject requests.
- **Role-aware access:** student, instructor, and administrator roles; instructors are scoped to assigned courses for protected operations.
- **Authentication:** ASP.NET Core Identity-backed registration and sign-in, short-lived JWT access tokens, refresh-token rotation, and failed-login lockout.
- **Grades and reporting:** record course grades and query student, enrollment, and course summaries.
- **Transcripts:** queue transcript requests, check processing status, download generated plain-text transcripts, and receive SignalR notifications.
- **API safeguards:** request validation, structured Problem Details errors, rate limiting, health endpoints, and API version/deprecation handling.

## Architecture

The solution is a layered .NET application organized as a modular monolith:

```text
HTTP client ── REST / SignalR ──> ASP.NET Core API
                                      │
                       Application services / MediatR
                                      │
                       Domain model and contracts
                                      │
                 Infrastructure: EF Core, PostgreSQL,
                    repositories, cache, background work
```

The application layer uses services and repositories for core operations, and MediatR request/handler flows for versioned enrollment commands and queries. Some endpoints also query the EF Core context directly. Transcript work is processed by a hosted worker through a bounded in-process channel; its status and generated file contents are held in memory, so they do not survive an API restart.

The API exposes REST endpoints and a SignalR hub at `/hubs/tms`. CORS is configured for a separate client (the development default is `http://localhost:4200`); the client itself is not part of this repository.

## Technology Stack

- **Runtime/API:** .NET 10, ASP.NET Core
- **Persistence:** Entity Framework Core, PostgreSQL via Npgsql, EF Core migrations
- **Identity and security:** ASP.NET Core Identity, JWT bearer authentication, role and resource-based authorization
- **Application patterns:** dependency injection, service/repository abstractions, MediatR, FluentValidation
- **Realtime/background work:** SignalR, hosted services, `System.Threading.Channels`
- **Resilience and caching:** Polly resilience pipeline; .NET HybridCache course-service implementation
- **API diagnostics:** development-only OpenAPI/Scalar UI, health checks, JSON console logging, OpenTelemetry with OTLP export
- **Tests:** xUnit, ASP.NET Core integration-test hosting, EF Core InMemory, NSubstitute

## Screenshots

Screenshots are placeholders for the separately prepared client UI. Replace each placeholder with the real GitHub-hosted image path when ready; no image paths are assumed here.

### Course management

<!-- Replace this placeholder with the actual GitHub screenshot path. -->

[Screenshot placeholder]

### Enrollment workflow

<!-- Replace this placeholder with the actual GitHub screenshot path. -->

[Screenshot placeholder]

### Student transcript

<!-- Replace this placeholder with the actual GitHub screenshot path. -->

[Screenshot placeholder]

## Core Workflows

1. **Course setup:** administrators and instructors manage courses; instructors can only edit courses assigned to them, while administrators can manage all courses.
2. **Enrollment:** students submit requests against a course code. The application checks that the course exists, has capacity, and the student is not already enrolled; instructors can approve or reject requests for their courses.
3. **Academic records:** instructors and administrators post scores for enrolled students. Grade changes are broadcast through SignalR.
4. **Transcript generation:** an authorized caller requests a transcript, optionally supplying an `Idempotency-Key`. The API returns an accepted response and status URL while a background worker builds the downloadable transcript and notifies the student's SignalR group when it is ready.

## API Overview

Selected endpoint areas:

| Area           | Routes                                                            | Purpose                                                                        |
| -------------- | ----------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| Authentication | `POST /api/auth/register`, `/api/auth/login`, `/api/auth/refresh` | Account registration and token lifecycle                                       |
| Courses        | `GET /api/v1.0/courses`; `/api/courses`                           | Paginated versioned listing and unversioned course-management endpoints        |
| Enrollments    | `/api/v2.0/enrollments`                                           | Student enrollment requests, schedules, instructor/admin listing and decisions |
| Transcripts    | `/api/v2.0/transcripts`                                           | Request, check status, and download transcripts                                |
| Grades         | `/api/grades`                                                     | Instructor/admin grade entry                                                   |
| Reporting      | `/api/reporting`                                                  | Student and enrollment summaries                                               |
| Realtime       | `/hubs/tms`                                                       | Enrollment, grade, and transcript-ready notifications                          |

API versions are selected in the URL. V1 responses receive deprecation and successor-version headers. OpenAPI documents and the Scalar reference are mapped in Development only.

## Data Model

Students and courses are related many-to-many through `Enrollment`, which stores status, enrollment time, archive state, and optional grade. A unique database index on `(StudentId, CourseId)` prevents duplicate enrollment records; restricted deletes preserve enrollment history. Student soft deletion is implemented with an EF Core query filter. ASP.NET Core Identity users and roles share the application database.

Development startup applies available migrations and inserts sample student/course data when needed. Transcript statuses, idempotency-key mappings, and generated transcript bytes are currently in-memory rather than persisted.

## Project Structure

```text
TmsApi.Api/             HTTP controllers, middleware, auth, configuration, SignalR hub
TmsApi.Application/     Use cases, DTOs, validation, service/repository contracts
TmsApi.Domain/          Student, course, enrollment, and identity domain entities
TmsApi.Infrastructure/  EF Core context/migrations, repositories, cache, workers, integrations
TmsApi.Tests/           Unit and API integration tests
docs/                   API versioning policy and development evidence
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- A running PostgreSQL instance

### Configure and run

The API requires `ConnectionStrings:TmsDatabase` and `Jwt:Key`. Keep these values in .NET user-secrets or environment variables, not in committed settings. The development settings provide the JWT issuer, audience, token lifetime, and allowed client origin.

From the repository root, configure the required settings and start the API:

```powershell
dotnet user-secrets set "ConnectionStrings:TmsDatabase" "Host=localhost;Port=5432;Database=tms;Username=postgres;Password=<your-password>" --project TmsApi.Api
dotnet user-secrets set "Jwt:Key" "<a-random-secret-at-least-32-bytes-long>" --project TmsApi.Api
dotnet restore TmsApi.sln
dotnet run --project TmsApi.Api
```

The launch profiles use `http://localhost:5196` and `https://localhost:7220` (with HTTP also available on port `5196`). The API applies EF Core migrations during startup. In Development, OpenAPI and Scalar are enabled. PostgreSQL must be reachable using the configured connection string before startup.

### Run tests

```powershell
dotnet test TmsApi.sln
```

The test project includes handler unit tests and API tests hosted with an in-memory EF Core database.

## Engineering Highlights

- **Clear project boundaries:** domain, application, infrastructure, and HTTP concerns are separated into solution projects; API endpoints use application abstractions or MediatR handlers where implemented.
- **Business constraints at multiple levels:** enrollment checks are represented in application logic and reinforced by a unique database index; course ownership is checked through resource-based authorization.
- **Consistent request handling:** MediatR validation/logging pipeline behaviors, FluentValidation, and centralized Problem Details exception handling make failures explicit.
- **Resilient and bounded work:** transcript generation is queued on a bounded channel with a hosted worker; a configured Polly pipeline protects the certificate HTTP client, which currently targets a local API fixture rather than a configured third-party provider.
- **Operational hooks:** health endpoints distinguish liveness from PostgreSQL readiness; ASP.NET Core, HTTP-client, and runtime telemetry are configured for OTLP export.

## Current Scope

This is an API-focused project with development/demo fixtures and sample data. In particular, transcript state and content are process-local, and the certificate client is exercised against an in-process fixture endpoint. Durable job storage and a production certificate-provider integration are not included.
