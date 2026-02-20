<!--
Sync Impact Report
Version change: template-placeholder → 1.0.0
Modified principles:
- [PRINCIPLE_1_NAME] → I. Training-First Scope (Non-Production)
- [PRINCIPLE_2_NAME] → II. Secure-by-Default Access Control
- [PRINCIPLE_3_NAME] → III. Service-Layer Authorization & Data Isolation
- [PRINCIPLE_4_NAME] → IV. Spec-Driven Incremental Delivery
- [PRINCIPLE_5_NAME] → V. Verifiable Quality Gates
Added sections:
- Technical & Operational Constraints
- Development Workflow & Review
Removed sections:
- None
Templates requiring updates:
- ✅ reviewed (no changes required): .specify/templates/plan-template.md
- ✅ reviewed (no changes required): .specify/templates/spec-template.md
- ✅ reviewed (no changes required): .specify/templates/tasks-template.md
- ✅ reviewed (directory not present): .specify/templates/commands/*.md
- ✅ reviewed (no changes required): .github/agents/*.md
- ✅ reviewed (no changes required): .github/prompts/*.md
- ✅ reviewed (no changes required): README.md
Follow-up TODOs:
- TODO(RATIFICATION_DATE): Original ratification date was not found in repository history and must be confirmed by project owners.
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope (Non-Production)
The repository MUST remain suitable for offline, training-only use. Features MUST NOT require cloud
resources, paid services, or external identity providers for baseline operation unless a feature spec
explicitly requests that dependency and documents a local fallback. Production-hardening work MAY be
described, but MUST be clearly labeled as guidance rather than default runtime behavior.

Rationale: The project exists for Spec-Driven Development training and must stay runnable in constrained
learning environments.

### II. Secure-by-Default Access Control
All user-accessible pages, endpoints, and operations that read or mutate user-scoped data MUST enforce
authentication and authorization. Access rules MUST be explicit (role, ownership, or membership based)
and MUST fail closed when identity is missing or invalid.

Rationale: Security behavior is a core learning outcome and must be visible and consistent across layers.

### III. Service-Layer Authorization & Data Isolation
Authorization checks MUST be enforced in service/business logic, not only at routing or UI level. Data
queries and updates MUST be scoped to the requesting user unless elevated access is explicitly allowed.
Changes that affect cross-user access MUST include verification steps proving IDOR-style access is
prevented.

Rationale: Defense in depth prevents bypasses and reflects how secure enterprise applications are built.

### IV. Spec-Driven Incremental Delivery
Material changes MUST originate from Spec Kit artifacts (`spec.md`, `plan.md`, `tasks.md`) and be
delivered in independently testable user-story increments (P1 first). Work MUST map to explicit
requirements and success criteria before implementation begins.

Rationale: Predictable delivery and traceability are required for effective training and review.

### V. Verifiable Quality Gates
Before merge, code changes MUST build on the active target framework and include evidence of validation
(tests or explicit manual verification steps). If runtime behavior, setup commands, or developer
workflow changes, corresponding documentation and scripts MUST be updated in the same change set.

Rationale: Reproducibility and clear evidence reduce regressions and improve training quality.

## Technical & Operational Constraints

- Primary application stack MUST remain ASP.NET Core + Blazor Server with EF Core unless a spec
	explicitly authorizes stack expansion.
- Seeded training data and mock authentication flows MUST remain available to support local onboarding.
- Security-sensitive defaults (cookie settings, authorization policies, and protective headers) MUST NOT
	be weakened without an approved spec and documented risk rationale.
- Repository changes SHOULD minimize unrelated refactoring; scope MUST stay aligned to approved spec
	boundaries.

## Development Workflow & Review

- `/speckit.specify` MUST produce a complete feature spec with prioritized user stories and measurable
	outcomes.
- `/speckit.plan` MUST pass constitution checks before and after design artifacts are generated.
- `/speckit.tasks` MUST organize implementation by user story with clear dependencies and independent
	test criteria.
- `/speckit.implement` MUST execute tasks in order, marking completion states and preserving traceability
	between tasks and changed files.
- Reviews MUST confirm constitution compliance, build success, and documented validation evidence before
	approval.

## Governance

This constitution is authoritative for repository workflow and quality expectations. In case of conflict,
this document supersedes informal practices.

Amendment procedure:
1. Propose amendments through a dedicated constitution update change.
2. Document rationale, impacted principles/sections, and required template or workflow updates.
3. Obtain maintainer approval before merging.
4. Record a Sync Impact Report at the top of this file.

Versioning policy (Semantic Versioning):
- MAJOR: Backward-incompatible governance changes or principle removals/redefinitions.
- MINOR: New principle or materially expanded governance requirement.
- PATCH: Clarifications, wording improvements, and non-semantic refinements.

Compliance review expectations:
- Every implementation plan MUST include a constitution check.
- Every pull request MUST attest compliance with applicable MUST statements.
- Deviations MUST be explicitly justified in plan/task artifacts and approved by maintainers.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): Confirm original adoption date with project maintainers. | **Last Amended**: 2026-02-20
