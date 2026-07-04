# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

RpFlo — ERP procurement request approval workflow. .NET 10 backend (Clean Architecture, DDD, railway-oriented programming) with React 19 + TypeScript frontend. MSSQL database with temporal tables for change tracking. Runs via Docker Compose on `localhost:3000` (frontend) and `localhost:5000` (API).

## Commands

```bash
# Run everything (frontend + API + MSSQL)
docker compose up --build

# Backend build
dotnet build

# All tests (integration tests need Docker for TestContainers)
dotnet test

# Single test project
dotnet test tests/RpFlo.Domain.Tests
dotnet test tests/RpFlo.Application.Tests
dotnet test tests/RpFlo.Integration.Tests

# Single test
dotnet test --filter "FullyQualifiedName~MethodName"

# Frontend dev (from frontend/)
npm run dev          # Vite dev server with proxy to localhost:5000
npm run build        # tsc + vite build
npm run lint         # oxlint
npm run test         # vitest (ErrorBoundary smoke tests)
```

## Architecture

Clean Architecture with four layers. Dependencies point inward: `Api → Application ← Infrastructure → Domain`.

- **Domain** (`src/RpFlo.Domain/`) — Zero dependencies. Entities with encapsulated behavior, `Result<T>` monad for railway-oriented error flow, `Money` value object, domain events. `ProcurementRequest` is the aggregate root and state machine — all workflow transitions enforced here via methods returning `Result<T>`.
- **Application** (`src/RpFlo.Application/`) — `ProcurementService` orchestrates domain operations. FluentValidation validators. DTOs for API contracts. Repository interfaces (`IProcurementRepository`, `IUserRepository`, etc.) and `IUnitOfWork`.
- **Infrastructure** (`src/RpFlo.Infrastructure/`) — EF Core code-first with MSSQL. SQL Server temporal tables on mutable entities for automatic change history. Repository implementations. `SeedData` creates demo users (Alice/Bob/Carol/Dave/Eve) on startup. Migrations in `Migrations/`.
- **Api** (`src/RpFlo.Api/`) — ASP.NET Core controllers with Swashbuckle Swagger UI at `/swagger` in Development mode. `ErrorHandlingMiddleware` catches `ValidationException` → 400. Controllers map `Result<T>` error codes to HTTP status (`NotFound.*` → 404, `Unauthorized.*` → 403, `Validation.*`/`Domain.*` → 400).

## Key Patterns

**Result<T> railway** — Domain methods return `Result<T>` not exceptions. `Bind()` chains operations, `Match()` unwraps. Error codes are prefixed: `Validation.`, `NotFound.`, `Unauthorized.`, `Domain.`, `Conflict.`. Controllers pattern-match on prefix to pick HTTP status.

**AuditableEntity** — Abstract base class extending `Entity` with `LastModifiedBy` (Guid?). Entities that mutate (ProcurementRequest, LineItem, Comment, Notification) extend this. Immutable entities (User, AuditEntry) extend `Entity` directly.

**Temporal tables** — MSSQL temporal tables enabled on auditable entity tables (ProcurementRequests, LineItems, Comments, Notifications). History tables auto-managed by SQL Server. Persistence-layer only — not exposed in application/UI.

**State machine in entity** — `ProcurementRequest` enforces: `Draft → Submitted → ManagerApproved → FinanceApproved → PurchaseOrderIssued`. Rejections branch to `ManagerRejected`/`FinanceRejected`, which can `ReviseToDraft`. Invalid transitions return domain errors.

**Simulated auth** — `X-User-Id` header, no real auth. Frontend sets it via `setCurrentUser()` in `api/client.ts`. User-switcher dropdown in layout. Procurement API reads are server-scoped by the caller: requesters can only view their own requests, managers can view non-draft workflow requests plus their own, finance can view finance-stage requests plus their own, and admins can view all. Keep list/detail/export/metrics endpoints on the scoped service methods.

**EF Core quirk** — New child entities (AuditEntry, Comment) added through domain methods need explicit `DbContext.Add()` in `UpdateAsync` because EF Core doesn't auto-detect new entities with Guid keys in DDD-style encapsulated collections.

**EF Core temporal + owned types** — Owned types sharing same table (e.g., Money in LineItems) require explicit temporal period column alignment via `HasPeriodStart("PeriodStart").HasColumnName("PeriodStart")` on both owner and owned type.

## Frontend

React 19 + Vite + Tailwind CSS v4 + shadcn/ui (base-ui). TanStack Query for data fetching. React Router for routing.

- `frontend/src/api/` — Axios client with `X-User-Id` header injection
- `frontend/src/pages/` — Dashboard, RequestList, RequestDetail, CreateRequest, Approvals
- `frontend/src/hooks/useAuth.tsx` — User context and role-based access
- Vite proxies `/api` to `http://localhost:5000` in dev
- Import alias: `@/*` → `./src/*`
- Linter: oxlint (not eslint)
- Procurement list endpoints (`/api/procurement`, `/api/procurement/my`, `/api/procurement/pending`) return paged results with server-side filtering and pagination.

## Style Preferences (from Preferences.md)

- Favor immutability and LINQ over loops
- Functional/railway-oriented approach without overdoing it
- Strongly typed states to eliminate illegal states at compile time
- Domain-driven error codes
- UI should be rich but empathetic — not overbuilt
- Fetch once, filter client-side when cost is low

## Living Documentation

- **`docs/decisions.md`** — Architecture Decision Log. When making a change that involves an architectural choice, trade-off, or deviation from existing patterns, add a new ADR entry at the top of this file. Use the existing format (ADR number, date, status, context, decision, trade-offs). Update the status of existing entries if a decision is superseded or reversed.
- **This file (`CLAUDE.md`)** — Keep this file accurate. When adding new projects, layers, patterns, commands, or conventions, update the relevant section here. If a section becomes stale due to a code change, fix it in the same commit.
