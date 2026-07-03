# Architecture Decision Log

Record of key architectural and trade-off decisions for RpFlo. Newest entries first.

---

## ADR-001: Clean Architecture with Railway-Oriented Programming

**Date:** 2026-07-03
**Status:** Accepted

**Context:** Needed a backend architecture for a procurement approval workflow with complex state transitions and business rules.

**Decision:** Clean Architecture (Api → Application ← Infrastructure → Domain) with `Result<T>` monad for error flow instead of exceptions.

**Trade-offs:**
- (+) Domain logic is fully testable without infrastructure dependencies
- (+) Error codes (`Validation.*`, `NotFound.*`, etc.) give structured, predictable error handling across layers
- (+) State machine lives in the aggregate root — impossible to reach illegal states
- (-) More boilerplate than a simpler layered approach
- (-) `Result<T>` chaining has a learning curve vs. try/catch

---

## ADR-002: Simulated Auth via X-User-Id Header

**Date:** 2026-07-03
**Status:** Accepted

**Context:** Need multi-user workflow testing (requester, manager, finance) without building a full auth system during early development.

**Decision:** Simulate authentication with `X-User-Id` header. Frontend user-switcher dropdown sets the current user. No tokens, no sessions.

**Trade-offs:**
- (+) Fast to develop, easy to demo multi-role workflows
- (+) No auth infrastructure to maintain during prototyping
- (-) Not production-safe — must be replaced before deployment
- (-) No real authorization middleware; role checks happen in domain/application layer

---

## ADR-003: EF Core Code-First with MSSQL + Temporal Tables

**Date:** 2026-07-03
**Status:** Accepted (supersedes original PostgreSQL decision)

**Context:** Needed a relational store for procurement requests with audit trails, comments, and approval chains. Additionally, want automatic row-level change history without application code changes.

**Decision:** EF Core code-first migrations with MSSQL. SQL Server temporal tables enabled on mutable entities (ProcurementRequests, LineItems, Comments, Notifications) for automatic change tracking at the persistence layer. Immutable entities (Users, AuditEntries) remain non-temporal. TestContainers with MSSQL for integration tests. `AuditableEntity` base class adds `LastModifiedBy` to tracked entities.

**Trade-offs:**
- (+) Migrations tracked in source control
- (+) TestContainers give real DB behavior in tests
- (+) Temporal tables provide complete row-level history with zero application code — SQL Server manages history tables automatically
- (+) `AuditableEntity` tracks who made the last change at the domain level
- (-) DDD encapsulated collections require manual `DbContext.Add()` for new child entities (EF Core doesn't auto-detect them with Guid keys)
- (-) MSSQL container is heavier than PostgreSQL (~1.5GB vs ~200MB image)
- (-) Temporal tables add storage overhead for history tables
- (-) Owned types (Money value object) require explicit temporal column alignment in EF Core config

---

## ADR-004: React 19 + Vite + TanStack Query Frontend

**Date:** 2026-07-03
**Status:** Accepted

**Context:** Needed a responsive frontend for procurement workflows — dashboards, forms, approval queues.

**Decision:** React 19 with Vite, Tailwind CSS v4, shadcn/ui, TanStack Query for server state, React Router for navigation. Oxlint over ESLint.

**Trade-offs:**
- (+) Vite gives fast HMR and build times
- (+) TanStack Query handles caching, refetching, optimistic updates
- (+) Oxlint is faster than ESLint for linting
- (-) shadcn/ui components need manual updates (not a versioned package)
- (-) Tailwind v4 is newer — fewer community examples

---

## ADR-005: State Machine in Domain Entity

**Date:** 2026-07-03
**Status:** Accepted

**Context:** Procurement requests follow a strict approval workflow: Draft → Submitted → ManagerApproved → FinanceApproved → PurchaseOrderIssued, with rejection branches.

**Decision:** Encode the state machine directly in `ProcurementRequest` entity. Transition methods return `Result<T>` — invalid transitions produce domain errors, not exceptions.

**Trade-offs:**
- (+) Impossible to bypass workflow rules — entity is the single source of truth
- (+) Each transition can enforce its own preconditions (e.g., only the assigned manager can approve)
- (-) Entity grows as workflow complexity increases
- (-) Adding new states requires touching the entity and potentially all transition methods
