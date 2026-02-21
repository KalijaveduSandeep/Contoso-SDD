# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-02-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

## Summary

Add offline-first document upload and management to ContosoDashboard with secure local file storage,
role-aware access control, project/task integration, sharing and notifications, and auditable activity
tracking. The implementation uses layered changes in existing Blazor pages, service layer, EF Core data
model, and local storage abstraction (`IFileStorageService`) to keep training operation local while
remaining cloud-migration ready. Malware scanning is processed asynchronously via a Queue Storage-backed
background workflow, with Azure Functions Queue Trigger workers handling scan jobs after upload.

## Technical Context

**Language/Version**: C# / .NET 10 (ASP.NET Core Blazor Server)  
**Primary Dependencies**: ASP.NET Core, Entity Framework Core (SQL Server), Bootstrap UI, existing auth/authorization services, Azure Functions isolated worker (scan processor), Azure Queue Storage client  
**Storage**: SQL Server LocalDB for metadata; local filesystem outside web root for file content; Queue Storage for scan job dispatch  
**Testing**: Build validation (`dotnet build`) + manual scenario validation (no dedicated automated test project currently present)  
**Target Platform**: Windows development environment, ASP.NET Core web host  
**Project Type**: Single web application (`ContosoDashboard`)  
**Performance Goals**: Upload <= 30s for <=25 MB files; list/search <= 2s for target dataset; preview <= 3s  
**Constraints**: Offline-first; no mandatory cloud services in baseline runtime (use local emulators such as Azurite/Functions Core Tools for training); security checks at service layer; maintain existing architecture and mock auth model  
**Scale/Scope**: Up to ~500 user-visible documents per list page; broad role-based operations for Employee/TeamLead/ProjectManager/Administrator

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate Assessment

- **I. Training-First Scope (Non-Production)**: PASS — design remains local/offline with no required cloud dependency.
- **II. Secure-by-Default Access Control**: PASS — access rules defined for all roles and document operations.
- **III. Service-Layer Authorization & Data Isolation**: PASS — plan enforces service-level checks for list/search/download/share/delete.
- **IV. Spec-Driven Incremental Delivery**: PASS — implementation derived from prioritized user stories and explicit requirements.
- **V. Verifiable Quality Gates**: PASS — build + manual validation criteria included in quickstart.

No blocking constitution violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── (existing domain models)
│   ├── Document.cs
│   ├── DocumentTag.cs
│   ├── DocumentShare.cs
│   └── DocumentActivity.cs
├── Services/
│   ├── IFileStorageService.cs
│   ├── LocalFileStorageService.cs
│   ├── IDocumentService.cs
│   └── DocumentService.cs
├── Pages/
│   ├── Documents.razor
│   ├── ProjectDetails.razor (integration)
│   ├── Tasks.razor (integration)
│   └── Index.razor (recent documents widget)
├── wwwroot/
│   └── (UI assets)
└── Program.cs
```

**Structure Decision**: Single existing Blazor web app is retained. Feature is added in-place to
`ContosoDashboard` using current layered architecture (Models/Services/Pages/Data) to satisfy
constitution constraints and minimize refactoring.

## Phase 0: Research Output

- [research.md](./research.md) resolves design decisions for storage workflow, scan behavior,
  authorization scope, sharing model, and audit/report strategy.

## Phase 1: Design & Contracts Output

- [data-model.md](./data-model.md) defines entities, relationships, validation, and state model.
- [contracts/document-api.openapi.yaml](./contracts/document-api.openapi.yaml) defines API contracts
  for upload, browse, download, preview, metadata lifecycle, sharing, and reporting.
- [quickstart.md](./quickstart.md) defines executable validation workflow and acceptance checks.

## Background Scan Architecture

1. User uploads file through `DocumentService`.
2. Service validates size/type/authorization and stores file + metadata with `ScanStatus=Pending`.
3. Service enqueues a scan job message to Queue Storage (`document-scan-jobs`).
4. Azure Function (`DocumentScanWorker`) with Queue Trigger dequeues job and performs malware scan.
5. Worker updates document scan status:
   - `Clean`: document becomes downloadable/previewable/search-visible.
   - `Rejected`: document remains blocked, activity logged, and uploader notified.
6. Retry and poison-queue handling are configured for failed scan attempts.

### Queue Message Contract (internal)

- `documentId` (int)
- `filePath` (string)
- `fileType` (string)
- `uploadedByUserId` (int)
- `projectId` (int|null)
- `enqueuedAtUtc` (datetime)

### Security/Access Rule for Pending Scans

- Documents with `ScanStatus=Pending` or `ScanStatus=Rejected` MUST NOT be downloadable or previewable.
- List/search views MAY display pending items to owners with explicit status indicators, but must exclude
  rejected file access actions.

### Post-Design Constitution Re-check

- **I. Training-First Scope (Non-Production)**: PASS — local filesystem + LocalDB; no required cloud.
- **II. Secure-by-Default Access Control**: PASS — contract surfaces protected operations with role/ownership expectations.
- **III. Service-Layer Authorization & Data Isolation**: PASS — data model and contracts support user/project-scoped access enforcement.
- **IV. Spec-Driven Incremental Delivery**: PASS — artifacts map directly to user stories and FR/SC set.
- **V. Verifiable Quality Gates**: PASS — quickstart includes build and scenario verification steps.

No constitution exceptions required.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
