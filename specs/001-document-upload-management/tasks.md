# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests were not explicitly requested in the feature specification; tasks focus on implementation and manual validation via quickstart.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize feature scaffolding and baseline configuration for document management.

- [X] T001 Add document feature package/service registrations in ContosoDashboard/Program.cs
- [X] T002 Create base documents page shell and route in ContosoDashboard/Pages/Documents.razor
- [X] T003 [P] Add document navigation entry in ContosoDashboard/Shared/NavMenu.razor
- [X] T004 [P] Add document UI styles and status badges in ContosoDashboard/wwwroot/css/site.css
- [X] T005 Create queue scanning configuration section in ContosoDashboard/appsettings.json
- [X] T006 [P] Create local development queue scanning notes in specs/001-document-upload-management/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement core domain, storage abstraction, async scan pipeline contracts, and authorization boundaries required by all stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 Create Document domain model in ContosoDashboard/Models/Document.cs
- [X] T008 [P] Create DocumentTag model in ContosoDashboard/Models/DocumentTag.cs
- [X] T009 [P] Create DocumentShare model in ContosoDashboard/Models/DocumentShare.cs
- [X] T010 [P] Create DocumentActivity model in ContosoDashboard/Models/DocumentActivity.cs
- [X] T011 Configure EF DbSets, relations, and indexes for document entities in ContosoDashboard/Data/ApplicationDbContext.cs
- [X] T012 Add file storage abstraction interface in ContosoDashboard/Services/IFileStorageService.cs
- [X] T013 Implement local file storage service in ContosoDashboard/Services/LocalFileStorageService.cs
- [X] T014 Define document service interface and DTO contracts in ContosoDashboard/Services/IDocumentService.cs
- [X] T015 Implement baseline DocumentService with service-layer authorization helpers in ContosoDashboard/Services/DocumentService.cs
- [X] T016 Define scan queue publisher abstraction in ContosoDashboard/Services/IScanQueueService.cs
- [X] T017 Implement queue publisher service for scan jobs in ContosoDashboard/Services/ScanQueueService.cs
- [X] T018 Register document and queue services in DI container in ContosoDashboard/Program.cs
- [X] T019 Add scan status and activity enums/shared constants in ContosoDashboard/Models/DocumentEnums.cs
- [X] T020 Add migration-ready API endpoint shell for document operations in ContosoDashboard/Controllers/DocumentsController.cs

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Upload and organize documents (Priority: P1) 🎯 MVP

**Goal**: Enable secure document upload with metadata and asynchronous scan initiation.

**Independent Test**: Upload valid and invalid files and verify metadata capture, validation messages, and pending scan state in My Documents.

- [X] T021 [US1] Implement upload input model and server-side validation rules in ContosoDashboard/Models/DocumentUploadRequest.cs
- [X] T022 [US1] Implement upload workflow (`validate -> authorize -> path -> save -> metadata -> queue`) in ContosoDashboard/Services/DocumentService.cs
- [X] T023 [US1] Implement upload API endpoint returning accepted/pending scan response in ContosoDashboard/Controllers/DocumentsController.cs
- [X] T024 [US1] Build upload form/modal UI with metadata fields in ContosoDashboard/Pages/Documents.razor
- [X] T025 [US1] Add upload progress indicator and completion/error feedback in ContosoDashboard/Pages/Documents.razor
- [X] T026 [US1] Add My Documents list with metadata columns and scan status in ContosoDashboard/Pages/Documents.razor
- [X] T027 [US1] Add document upload and validation activity logging in ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: User Story 1 is independently functional and provides MVP value.

---

## Phase 4: User Story 2 - Discover and access permitted documents (Priority: P2)

**Goal**: Provide scoped browse/search/sort/filter and controlled preview/download.

**Independent Test**: Verify list and search results honor access rules and preview/download blocks pending/rejected scans.

- [X] T028 [US2] Implement access-scoped document query methods in ContosoDashboard/Services/DocumentService.cs
- [X] T029 [US2] Implement list endpoint with filtering/sorting/query parameters in ContosoDashboard/Controllers/DocumentsController.cs
- [X] T030 [US2] Implement project documents endpoint with project membership enforcement in ContosoDashboard/Controllers/DocumentsController.cs
- [X] T031 [US2] Implement preview and download endpoints with scan-status gating in ContosoDashboard/Controllers/DocumentsController.cs
- [X] T032 [US2] Add browse/search/filter/sort UI interactions in ContosoDashboard/Pages/Documents.razor
- [X] T033 [US2] Add project documents integration section in ContosoDashboard/Pages/ProjectDetails.razor
- [X] T034 [US2] Add document preview panel/modal behavior for supported types in ContosoDashboard/Pages/Documents.razor
- [X] T035 [US2] Add download and preview activity logging in ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: User Stories 1 and 2 are independently testable and value-complete.

---

## Phase 5: User Story 3 - Manage sharing and lifecycle (Priority: P3)

**Goal**: Enable metadata edits, replacement, deletion, sharing, notifications, and audit trails.

**Independent Test**: Confirm owner/manager actions work per role and recipients receive shared visibility and notifications.

- [X] T036 [US3] Implement metadata update and tag persistence methods in ContosoDashboard/Services/DocumentService.cs
- [X] T037 [US3] Implement file replace workflow with re-queue for scan in ContosoDashboard/Services/DocumentService.cs
- [X] T038 [US3] Implement permanent delete workflow with confirmation guard in ContosoDashboard/Services/DocumentService.cs
- [X] T039 [US3] Implement share grant creation and shared-with-me retrieval in ContosoDashboard/Services/DocumentService.cs
- [X] T040 [US3] Implement metadata, replace, delete, share endpoints in ContosoDashboard/Controllers/DocumentsController.cs
- [X] T041 [US3] Add metadata edit UI in ContosoDashboard/Pages/Documents.razor
- [X] T042 [US3] Add replace and delete UI actions in ContosoDashboard/Pages/Documents.razor
- [X] T043 [US3] Add share dialog and shared-with-me section in ContosoDashboard/Pages/Documents.razor
- [X] T044 [US3] Integrate share and lifecycle notifications using ContosoDashboard/Services/NotificationService.cs
- [X] T045 [US3] Add lifecycle and share audit event persistence in ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: User Stories 1–3 are independently functional and enforce lifecycle controls.

---

## Phase 6: User Story 4 - Use documents in existing workflows (Priority: P4)

**Goal**: Integrate documents with task/project/dashboard/reporting experiences.

**Independent Test**: Validate task-context upload linkage, dashboard recent docs/counts, project notifications, and admin report outputs.

- [ ] T046 [US4] Implement task-context upload method with auto project association in ContosoDashboard/Services/DocumentService.cs
- [ ] T047 [US4] Implement task document upload endpoint in ContosoDashboard/Controllers/DocumentsController.cs
- [ ] T048 [US4] Add task-level document attach/upload UI in ContosoDashboard/Pages/Tasks.razor
- [ ] T049 [US4] Add recent documents dashboard query methods in ContosoDashboard/Services/DashboardService.cs
- [ ] T050 [US4] Add recent documents widget and document count cards in ContosoDashboard/Pages/Index.razor
- [ ] T051 [US4] Implement admin reporting aggregations in ContosoDashboard/Services/DocumentService.cs
- [ ] T052 [US4] Implement reporting endpoint for type/uploader/access summaries in ContosoDashboard/Controllers/DocumentsController.cs
- [ ] T053 [US4] Trigger project-level notifications for new project documents in ContosoDashboard/Services/DocumentService.cs

**Checkpoint**: All user stories are integrated and independently verifiable.

---

## Phase 7: Async Scan Worker & Cross-Cutting Polish

**Purpose**: Finalize background scanning worker, operational hardening, and acceptance validation.

- [ ] T054 Create queue-triggered scan worker project scaffold in DocumentScanWorker/DocumentScanWorker.csproj
- [ ] T055 Implement queue trigger function processing scan jobs in DocumentScanWorker/Functions/DocumentScanWorker.cs
- [ ] T056 [P] Implement scan engine abstraction and local scanner adapter in DocumentScanWorker/Services/IScanEngine.cs
- [ ] T057 [P] Implement scan result callback/update client to app database in DocumentScanWorker/Services/DocumentStatusUpdater.cs
- [ ] T058 Configure worker retry and poison queue handling in DocumentScanWorker/host.json
- [ ] T059 Add worker local settings for queue connection in DocumentScanWorker/local.settings.json
- [ ] T060 [P] Add queue message contract type shared with app in ContosoDashboard/Models/DocumentScanJob.cs
- [ ] T061 Add explicit pending/rejected access messaging in ContosoDashboard/Pages/Documents.razor
- [ ] T062 Align API contract examples with implemented behavior in specs/001-document-upload-management/contracts/document-api.openapi.yaml
- [ ] T063 Run quickstart scenario walkthrough and record outcomes in specs/001-document-upload-management/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — starts immediately.
- **Foundational (Phase 2)**: Depends on Setup completion — blocks all user story implementation.
- **User Stories (Phase 3+)**: Depend on Foundational completion; execute in priority order for MVP delivery.
- **Polish (Phase 7)**: Depends on all user story phases being functionally complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; no dependency on other user stories.
- **US2 (P2)**: Starts after US1 core upload/list primitives exist (T021–T027).
- **US3 (P3)**: Starts after US1/US2 document identity and access scope methods exist.
- **US4 (P4)**: Starts after US1–US3 document services and endpoints are available.

### Within Each User Story

- Service logic before controller endpoints.
- Controller endpoints before full UI wiring.
- UI integration before scenario validation.

### Parallel Opportunities

- Phase 1 tasks marked [P] can run in parallel.
- In Phase 2, model creation tasks (T008–T010) can run in parallel.
- In Phase 7, scan engine and status updater tasks (T056–T057) can run in parallel.

---

## Parallel Example: User Story 3

```bash
# Parallel model-independent lifecycle work:
Task: "Implement metadata update and tag persistence methods in ContosoDashboard/Services/DocumentService.cs"
Task: "Integrate share and lifecycle notifications using ContosoDashboard/Services/NotificationService.cs"

# Then converge on endpoint/UI:
Task: "Implement metadata, replace, delete, share endpoints in ContosoDashboard/Controllers/DocumentsController.cs"
Task: "Add metadata edit UI in ContosoDashboard/Pages/Documents.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Validate upload, metadata capture, and pending scan behavior.
4. Demo MVP before proceeding.

### Incremental Delivery

1. Foundation complete.
2. Deliver US1 (upload core).
3. Deliver US2 (discover/access).
4. Deliver US3 (lifecycle/sharing).
5. Deliver US4 (integration/reporting).
6. Finalize async scan worker and polish.

### Parallel Team Strategy

1. Team A: UI and page integration tasks.
2. Team B: service and API endpoint tasks.
3. Team C: async scan worker and queue processing tasks.
4. Merge at phase checkpoints and validate via quickstart.

---

## Notes

- All tasks follow strict checklist format: `- [ ] T### [P?] [US?] Description with file path`.
- Story labels are applied only to user story phases.
- Tasks are specific and executable without additional context.
- Quickstart remains the acceptance execution reference for this feature.
