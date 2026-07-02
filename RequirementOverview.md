## Submission Requirements
Please submit:
1. A public GitHub repository link.
2. A working application that can be run locally using Docker.
3. Clear setup instructions in README.md.
4. A short design note explaining your architecture, tradeoffs and assumptions.
5. Any credentials or seed data required for review.

The application should be self-contained. A reviewer should be able to clone the repository and run it without depending on paid external services.

Expected local run experience:
git clone <your-public-repo-url>
cd <repo>
docker compose up --build

If you use external APIs, provide mocks, stubs or documented local alternatives

## What We Are Looking For
We are not looking for a large system. We are looking for judgment.
We will evaluate:
- Product thinking and user flow.
- Backend/API design.
- Frontend usability and state handling.
- Data modeling and persistence.
- Error handling and edge cases.
- Security and access-control awareness.
- Testing approach.
- Docker/self-contained deployment.
- Code readability and maintainability.
- Documentation and assumptions.
- How responsibly you use AI-assisted development, if used.

# ERP Workflow Module

## Context

Company has business teams that often need software to reduce manual coordination between Sales, Accounts, Operations and Management.

Design and build a small ERP-style workflow module for an internal business process.

The exact process is up to you, but it should involve at least:
- A request created by one role or department.
- Review or approval by another role.
- A final business outcome, such as an invoice, purchase request, payment request,
procurement approval, HR approval, delivery approval or similar.
- Status tracking and auditability.

## Example Direction

One possible flow:
Sales submits a billing request. Accounts reviews it. If approved, an invoice is
created and becomes visible in a dashboard.

You may choose this flow or design a different ERP/business workflow if you think another one demonstrates your judgment better.
 
## Minimum Expectations
Your solution should include:
- At least 2-3 user roles.
- A clear workflow with statuses.
- Create, view, approve/reject and update actions where appropriate.
- Basic dashboard/list views.
- Audit trail or activity history.
- Sensible data model.
- Basic validation and error states.
- Seed data for review.

## Space For Creativity
You may add:
- Role-based access control.
- Comments or attachments.
- Notifications or simulated notifications.
- Reporting/summary metrics.
- ERPNext/Frappe-style integration mock.
- Accounting/invoice object model.
- Approval rules.
- Exportable reports.
- AI-assisted draft generation or workflow explanation.

## Technical Freedom
Use any stack you prefer.
Examples:
- Backend: Python, Go, Node.js/TypeScript, Java, .NET, PHP or others.
- Frontend: React, Next.js, Vue, Angular, Svelte or server-rendered UI.
- Database: PostgreSQL, MySQL, SQLite, MongoDB or similar.
- Deployment: Docker Compose is preferred.
Choose tools that let you demonstrate product and engineering judgment within the time limit.

## Documentation Expectations
Your README.md should include:
- What you built.
- Which option you selected.
- How to run it.
- Demo credentials, if any.
- Key user flows.
- Architecture overview.
- Data model overview.
- Known limitations.
- What you would improve with more time.
- AI tools used, if any, and how you reviewed the output

## Review Rubric

| Area                               | Weight | What We Look For                                                             |
| ---------------------------------- | :----: | ---------------------------------------------------------------------------- |
| Product/user flow                  |   20%  | Clear workflow, useful screens, sensible states, and real user empathy.      |
| Backend/API/data model             |   20%  | Clean domain model, APIs/service boundaries, validation, and persistence.    |
| Frontend implementation            |   15%  | Usable UI, state handling, loading/error/empty states, and responsiveness.   |
| Ownership and engineering judgment |   15%  | Good tradeoffs, readable code, maintainable structure, and scoped delivery.  |
| Reliability/security/testing       |   15%  | Error handling, access-control thinking, tests, or testable design.          |
| Docker/deployment/documentation    |   10%  | Reviewer can run it easily and understand design decisions.                  |
| AI-native development reflection   |   5%   | Responsible AI usage, review process, and clear ownership of generated work. |

## Notes
- Do not spend time building authentication perfectly unless it is central to your design. Simple mocked users/roles are fine.
- Do not use paid services or require private credentials.
- Do not overbuild. A thoughtful, complete small module is better than a large unfinished system.
- We value clarity, ownership and judgement more than visual polish alone.