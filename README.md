# RpFlo — ERP Procurement Request Workflow

A self-contained procurement request approval workflow application demonstrating Clean Architecture, domain-driven design, and railway-oriented programming in .NET 10 with a React 19 frontend.

## Quick Start

```bash
git clone <repo-url>
cd rpflo
docker compose up --build
```

Open **http://localhost:3000** in your browser. No accounts or passwords needed — select a user from the dropdown in the top-right corner.

## Demo Users (Seeded)

| Name  | Role      | Department  | What they can do                          |
|-------|-----------|-------------|-------------------------------------------|
| Alice | Requester | Engineering | Create, submit, and revise requests       |
| Bob   | Requester | Marketing   | Create, submit, and revise requests       |
| Carol | Manager   | Operations  | Approve or reject submitted requests      |
| Dave  | Finance   | Finance     | Approve/reject and issue purchase orders  |
| Eve   | Admin     | Operations  | Full access to all operations             |

Switch users via the dropdown at any time — the UI adapts to show role-appropriate actions.

## Workflow

```
Draft → Submitted → Manager Approved → Finance Approved → Purchase Order Issued
                ↓                   ↓
         Manager Rejected    Finance Rejected
                ↓                   ↓
            Revise to Draft ←───────┘
```

- **Requester** creates a draft with line items, then submits.
- **Manager** reviews and approves or rejects (with reason).
- **Finance** reviews and approves or rejects (with reason).
- **Finance** issues the purchase order (auto-generates PO number).
- On rejection, the **Requester** can revise and resubmit.

## Architecture

### Clean Architecture (4 layers)

```
Domain ← Application ← Infrastructure
                      ← Api
```

- **Domain** — Entities, value objects, enums, domain events. Zero external dependencies.
- **Application** — DTOs, service orchestration, validators, repository interfaces.
- **Infrastructure** — EF Core, PostgreSQL, repository implementations, seed data.
- **Api** — Controllers, middleware, DI composition root.

### Key Design Decisions

**Railway-oriented programming** — All domain operations return `Result<T>` (success or typed error). Errors flow through the pipeline without exceptions. The controller maps error types to HTTP status codes (NotFound→404, Unauthorized→403, Validation→400).

**Strongly typed state machine** — `ProcurementRequest` enforces valid state transitions in the domain. Invalid transitions (e.g., approving a draft) return domain errors, not exceptions. The entity is the single source of truth for workflow rules.

**Value objects** — `Money` is immutable with currency, rounding, and arithmetic. Prevents primitive obsession and ensures monetary calculations are consistent.

**Domain events** — State transitions raise events (e.g., `ProcurementSubmitted`). Currently used for audit trail; the pattern supports future event handlers without modifying the domain.

**Simulated auth** — `X-User-Id` header with a role-switcher dropdown. Demonstrates access control patterns without the complexity of a real auth system. The API validates user roles on every protected operation.

### Tech Stack

| Layer    | Technology |
|----------|------------|
| Backend  | .NET 10, ASP.NET Core, EF Core (code-first) |
| Database | PostgreSQL 16 |
| Validation | FluentValidation |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS v4, Shadcn/ui (base-ui) |
| Data fetching | TanStack Query |
| Testing  | xUnit, FluentAssertions, FsCheck (property-based), TestContainers |
| Deploy   | Docker Compose |

## Data Model

```
User (id, name, email, role, department)
  │
  ├── ProcurementRequest (id, title, description, department, urgency, status, po_number, requester_id)
  │     ├── LineItem (id, name, quantity, unit_price, procurement_request_id)
  │     ├── AuditEntry (id, user_id, action, from_status, to_status, comment, procurement_request_id)
  │     └── Comment (id, user_id, text, procurement_request_id)
  │
  └── Notification (id, user_id, title, message, is_read, reference_id)
```

All entities inherit from `Entity` base class with `Id`, `CreatedAt`, `UpdatedAt`.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/users` | List all users |
| GET | `/api/procurement` | List all requests |
| GET | `/api/procurement/:id` | Get request detail |
| POST | `/api/procurement` | Create draft request |
| PUT | `/api/procurement/:id` | Update draft request |
| POST | `/api/procurement/:id/submit` | Submit for review |
| POST | `/api/procurement/:id/approve/manager` | Manager approval |
| POST | `/api/procurement/:id/reject/manager` | Manager rejection |
| POST | `/api/procurement/:id/approve/finance` | Finance approval |
| POST | `/api/procurement/:id/reject/finance` | Finance rejection |
| POST | `/api/procurement/:id/issue-po` | Issue purchase order |
| POST | `/api/procurement/:id/revise` | Revise rejected request |
| POST | `/api/procurement/:id/comments` | Add comment |
| POST | `/api/procurement/:id/line-items` | Add line items |
| DELETE | `/api/procurement/:id/line-items/:lineItemId` | Remove line item |
| GET | `/api/procurement/metrics` | Dashboard metrics |
| GET | `/api/notifications` | User notifications |
| GET | `/api/notifications/unread-count` | Unread count |
| POST | `/api/notifications/:id/read` | Mark notification read |
| POST | `/api/notifications/read-all` | Mark all read |
| GET | `/api/export/csv` | Export requests as CSV |

All mutating endpoints require `X-User-Id` header.

## Testing

```bash
# All tests (requires Docker for integration tests)
dotnet test

# Domain tests only (fast, no dependencies)
dotnet test tests/RpFlo.Domain.Tests

# Application tests only
dotnet test tests/RpFlo.Application.Tests

# Integration tests (spins up real PostgreSQL via TestContainers)
dotnet test tests/RpFlo.Integration.Tests
```

### Test Coverage

| Suite | Tests | What's covered |
|-------|-------|----------------|
| Domain | 39 | State machine transitions, authorization, value objects, property-based (FsCheck) |
| Application | 11 | FluentValidation rules for all request types |
| Integration | 10 | Full API workflows through real HTTP + PostgreSQL |
| **Total** | **60** | |

**Property-based tests** (FsCheck) verify invariants like Money commutativity, non-negative totals, and state machine properties across randomized inputs.

**Integration tests** use TestContainers to spin up a real PostgreSQL instance per test class, exercise the full HTTP pipeline through `WebApplicationFactory`, and verify multi-step workflows (create → submit → approve → issue PO).

## Features

- **Dashboard** — Metrics cards, status breakdown, department summary
- **Request list** — Filtered views (All, My Requests, Drafts, Pending, Completed, Rejected)
- **Detail view** — Role-appropriate action buttons, line items table, audit trail timeline, comments
- **Notifications** — In-app notifications for workflow events with unread badge
- **CSV export** — Download all procurement data
- **Audit trail** — Complete history of state transitions with timestamps and actors

## Tradeoffs & Assumptions

1. **No real authentication** — Simulated via header + dropdown. In production, this would use JWT/OAuth with proper middleware. The role-based access control logic in the domain is production-ready; only the auth mechanism is simplified.

2. **Single aggregate** — `ProcurementRequest` is the sole aggregate root. For a real ERP, you'd split into bounded contexts (purchasing, inventory, budgeting). The current scope is intentionally focused.

3. **No file attachments** — Line items are data-only. A real procurement system would support document uploads (quotes, invoices). Omitted to keep scope manageable.

4. **Client-side filtering** — The request list fetches all requests and filters in the browser. Acceptable for demo scale; production would need server-side pagination and filtering.

5. **Money is USD-only by default** — The `Money` value object supports currency but no exchange rates. Multi-currency would need a rate service.

6. **EF Core entity tracking** — New child entities (AuditEntry, Comment) added through domain operations are explicitly tracked in `UpdateAsync` to handle EF Core's Guid key detection behavior with DDD-style encapsulated collections.

## Project Structure

```
├── src/
│   ├── RpFlo.Domain/        # Entities, value objects, enums, events
│   ├── RpFlo.Application/   # DTOs, services, validators, interfaces
│   ├── RpFlo.Infrastructure/ # EF Core, repositories, seed data
│   └── RpFlo.Api/           # Controllers, middleware, Program.cs
├── tests/
│   ├── RpFlo.Domain.Tests/
│   ├── RpFlo.Application.Tests/
│   └── RpFlo.Integration.Tests/
├── frontend/                          # React + TypeScript + Vite
├── docker-compose.yml
└── RpFlo.sln
```
