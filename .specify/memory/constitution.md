<!--
  Sync Impact Report
  ==================
  Version change: N/A (initial) → 1.0.0
  Modified principles: N/A (first ratification)
  Added sections:
    - Core Principles (5 principles)
    - Technology Constraints
    - Development Workflow
    - Governance
  Removed sections: N/A
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ No update needed (generic Constitution Check section)
    - .specify/templates/spec-template.md ✅ No update needed (generic template)
    - .specify/templates/tasks-template.md ✅ No update needed (generic template)
    - .specify/templates/checklist-template.md ✅ No update needed (generic template)
  Follow-up TODOs: None
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Design

Every feature and architectural decision MUST prioritize educational
clarity over production optimization. The codebase serves as a learning
tool for Spec-Driven Development (SDD) and secure coding practices.

- All features MUST be self-contained and runnable without external
  accounts, subscriptions, or cloud services.
- Mock implementations (e.g., authentication) MUST be clearly documented
  as training-only with explicit production migration guidance.
- Code MUST demonstrate good practices in a simplified form, with known
  limitations documented in README.md.
- Complexity MUST be justified by educational value; avoid patterns that
  obscure the learning objectives.

### II. Offline-First Architecture

The application MUST operate fully offline with no external service
dependencies. All infrastructure uses local resources with abstraction
layers enabling future cloud migration.

- Database MUST use SQLite (embedded, zero-install) for local
  development and training.
- File storage, if implemented, MUST use the local filesystem.
- Authentication MUST use cookie-based mock login with no external
  identity provider required.
- All infrastructure dependencies MUST be behind interface abstractions
  (e.g., `IFileStorageService`) to enable implementation swapping via
  dependency injection without business logic changes.

### III. Security by Design

Security patterns MUST be implemented at every layer, even in a training
context, to teach defense-in-depth principles.

- All protected pages MUST use `[Authorize]` attributes.
- Service-layer methods MUST validate user authorization before
  returning or modifying data (IDOR prevention).
- Role-based access control (RBAC) MUST enforce hierarchical
  permissions: Employee < TeamLead < ProjectManager < Administrator.
- Security headers (CSP, X-Frame-Options, X-Content-Type-Options,
  X-XSS-Protection, Referrer-Policy) MUST be applied via middleware.
- Input validation MUST occur at system boundaries.

### IV. Spec-Driven Development

All feature work MUST follow the SpecKit workflow pipeline. No
implementation begins without a specification.

- Features MUST progress through: specify → clarify → plan → tasks →
  implement.
- The `/speckit.analyze` command MUST be used to validate consistency
  across spec.md, plan.md, and tasks.md before implementation begins.
- User stories MUST be priority-ordered (P1, P2, P3) and independently
  testable.
- Tasks MUST be organized by user story with explicit dependency
  ordering and parallelism markers.

### V. Simplicity and YAGNI

The codebase MUST remain minimal and focused. Only implement what is
directly required by the current feature specification.

- No speculative abstractions or premature optimization.
- Three similar lines of code are preferred over a premature helper
  function.
- Error handling and validation MUST target realistic scenarios only;
  do not guard against impossible states.
- Each new dependency (NuGet package, service, abstraction layer) MUST
  be justified by a concrete current requirement.

## Technology Constraints

- **Framework**: ASP.NET Core 8.0 with Blazor Server.
- **Language**: C# with nullable reference types enabled.
- **Database**: SQLite via `Microsoft.EntityFrameworkCore.Sqlite`.
  EF Core `EnsureCreated()` for development; migrations for production.
- **UI**: Bootstrap 5.3 with Bootstrap Icons. No additional CSS
  frameworks or JavaScript libraries unless justified.
- **Authentication**: Cookie-based mock authentication for training.
  Azure AD / Microsoft Entra ID configuration preserved for migration
  reference.
- **Architecture**: N-tier with Models, Services, Data, and Pages
  layers. Interface-based dependency injection for all services.
- **Target Platform**: Windows, macOS, Linux (cross-platform via
  .NET 8.0). No platform-specific dependencies.

## Development Workflow

- All feature branches MUST follow the naming convention
  `###-feature-name` (e.g., `001-document-upload`).
- Feature specifications MUST be stored under
  `specs/###-feature-name/` with spec.md, plan.md, and tasks.md.
- Code reviews MUST verify compliance with this constitution's
  principles before merge.
- The application MUST build with zero errors before any commit.
  Warnings SHOULD be addressed but do not block commits.
- Seed data MUST use fixed `DateTime` values (not `DateTime.UtcNow`)
  for SQLite compatibility.
- Sensitive files (`.db`, `.env`, credentials) MUST be listed in
  `.gitignore` and never committed.

## Governance

This constitution is the authoritative governance document for the
ContosoDashboard project. It supersedes all other development practices
and guidelines when conflicts arise.

- **Amendments**: Any change to this constitution MUST be documented
  with a version bump, rationale, and sync impact report. Changes to
  principles require MAJOR version increments; new sections require
  MINOR; clarifications require PATCH.
- **Compliance**: All pull requests and code reviews MUST verify
  adherence to the core principles. The plan-template.md Constitution
  Check section MUST reference the active principles.
- **Review Cadence**: The constitution SHOULD be reviewed when new
  features introduce architectural changes or when training objectives
  evolve.
- **Runtime Guidance**: Use README.md for runtime development guidance
  and onboarding instructions.

**Version**: 1.0.0 | **Ratified**: 2026-02-02 | **Last Amended**: 2026-02-02
