# Phase 0 Research: Document Upload and Management

## Decision 1: File storage implementation and path strategy
- **Decision**: Use local filesystem storage outside web root (`AppData/uploads`) with GUID-based path pattern `{userId}/{projectScope}/{guid}.{ext}` generated before metadata persistence.
- **Rationale**: Satisfies offline/training requirement, prevents direct static access, avoids duplicate-path collisions, and supports future blob-name reuse.
- **Alternatives considered**:
  - Store files under `wwwroot` (rejected: weakens authorization boundary)
  - Store file bytes directly in SQL (rejected: larger DB footprint and reduced file streaming efficiency)

## Decision 2: Upload transaction order
- **Decision**: Upload workflow order is `validate -> authorize -> generate unique path -> save file -> save DB metadata -> notify`.
- **Rationale**: Prevents orphaned records and duplicate key risk while keeping failure handling deterministic.
- **Alternatives considered**:
  - Save DB first then write file (rejected: can create orphan metadata on I/O failure)

## Decision 3: Malware scanning behavior for offline training
- **Decision**: Accept upload into `Pending` state, enqueue scan jobs to Queue Storage, and process malware scans asynchronously using an Azure Functions Queue Trigger worker.
- **Rationale**: Decouples upload latency from scan duration, supports throughput under multi-file upload, and provides a migration-ready background processing pattern while preserving security gates.
- **Alternatives considered**:
  - Synchronous in-request scanning (rejected: increases upload response time and failure coupling)
  - Best-effort scan with unrestricted access before results (rejected: security exposure window)

## Decision 3a: Training/offline compatibility for queue worker
- **Decision**: Run Queue Storage and Azure Functions worker in local development using emulators/tooling (e.g., Azurite + Functions Core Tools), with same queue contract used for production.
- **Rationale**: Keeps constitution compliance for offline-first training while preserving real async architecture.
- **Alternatives considered**:
  - Cloud-only queue/function dependency (rejected: violates training-first baseline runtime)

## Decision 4: Authorization scope for operations
- **Decision**: Enforce service-layer checks for all document operations using role + ownership + project membership + share grants.
- **Rationale**: Matches constitution principles II/III and existing defense-in-depth architecture.
- **Alternatives considered**:
  - UI/page-level checks only (rejected: bypass risk)
  - Role-only checks without ownership/project scope (rejected: over-permissive access)

## Decision 5: Search and listing model
- **Decision**: Build access-scoped query first, then apply filters/sort/search predicates to authorized subset.
- **Rationale**: Prevents leakage from global search and supports required list/search performance target.
- **Alternatives considered**:
  - Global query then post-filter in memory (rejected: data leakage and performance risk)

## Decision 6: Share model granularity
- **Decision**: Implement explicit `DocumentShare` grants to user recipients (with optional team-scope extension field), and expose “Shared with Me” via share records.
- **Rationale**: Provides auditable, deterministic permissions and direct mapping to notification triggers.
- **Alternatives considered**:
  - Implicit department-only sharing (rejected: too broad)
  - Project-only inheritance without explicit share records (rejected: insufficient for ad-hoc sharing)

## Decision 7: Audit and reporting implementation shape
- **Decision**: Persist immutable `DocumentActivity` events and derive admin reports from aggregate queries over activity + document metadata.
- **Rationale**: Supports compliance reporting requirements without introducing external analytics systems.
- **Alternatives considered**:
  - Log-only approach without database records (rejected: weak queryability for in-app reporting)
  - External telemetry dependency (rejected: conflicts with offline-first constraint)
