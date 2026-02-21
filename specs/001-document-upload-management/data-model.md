# Data Model: Document Upload and Management

## Entity: Document
- **Purpose**: Canonical metadata record for uploaded file content.
- **Primary Key**: `DocumentId` (int)
- **Fields**:
  - `DocumentId` (int, required, identity)
  - `Title` (string, required, 1..200)
  - `Description` (string, optional, max 2000)
  - `Category` (string, required, enum-text from predefined set)
  - `FileName` (string, required, max 255)
  - `FilePath` (string, required, max 500)
  - `FileType` (string, required, max 255)
  - `FileSizeBytes` (long, required, >0 and <= 26_214_400)
  - `UploadedByUserId` (int, required, FK -> User)
  - `ProjectId` (int, optional, FK -> Project)
  - `TaskId` (int, optional, FK -> TaskItem)
  - `UploadedAtUtc` (datetime, required)
  - `UpdatedAtUtc` (datetime, required)
  - `ScanStatus` (string, required: Pending|Clean|Rejected)
  - `ScanRequestedAtUtc` (datetime, required)
  - `ScanCompletedAtUtc` (datetime, optional)
  - `ScanFailureReason` (string, optional, max 500)
  - `IsDeleted` (bool, required, default false)
  - `DeletedAtUtc` (datetime, optional)
  - `DeletedByUserId` (int, optional, FK -> User)
- **Indexes**:
  - `(UploadedByUserId, UploadedAtUtc desc)`
  - `(ProjectId, UploadedAtUtc desc)`
  - `(Category, UploadedAtUtc desc)`
  - full-text-supporting columns: `Title`, `Description`

## Entity: DocumentTag
- **Purpose**: Searchable user-defined tags.
- **Primary Key**: `DocumentTagId` (int)
- **Fields**:
  - `DocumentTagId` (int, required)
  - `DocumentId` (int, required, FK -> Document)
  - `TagValue` (string, required, 1..64)
  - `CreatedAtUtc` (datetime, required)
- **Constraints**:
  - Unique `(DocumentId, TagValue)`
  - Normalize tag casing for comparisons.

## Entity: DocumentShare
- **Purpose**: Explicit grant for shared access.
- **Primary Key**: `DocumentShareId` (int)
- **Fields**:
  - `DocumentShareId` (int, required)
  - `DocumentId` (int, required, FK -> Document)
  - `SharedWithUserId` (int, optional, FK -> User)
  - `SharedWithTeamKey` (string, optional, max 100)
  - `SharedByUserId` (int, required, FK -> User)
  - `SharedAtUtc` (datetime, required)
- **Constraints**:
  - At least one recipient target (`SharedWithUserId` or `SharedWithTeamKey`) required.
  - Unique `(DocumentId, SharedWithUserId)` where `SharedWithUserId` is not null.

## Entity: DocumentActivity
- **Purpose**: Immutable audit trail for compliance/reporting.
- **Primary Key**: `DocumentActivityId` (int)
- **Fields**:
  - `DocumentActivityId` (int, required)
  - `DocumentId` (int, required, FK -> Document)
  - `ActorUserId` (int, required, FK -> User)
  - `ActivityType` (string, required: Upload|Download|Preview|MetadataEdit|Replace|Delete|Share)
  - `OccurredAtUtc` (datetime, required)
  - `TargetUserId` (int, optional, FK -> User)  # for share events
  - `MetadataJson` (string, optional, max 4000)

## Relationships
- `User (1) -> (many) Document` via `UploadedByUserId`
- `Project (1) -> (many) Document` via optional `ProjectId`
- `TaskItem (1) -> (many) Document` via optional `TaskId`
- `Document (1) -> (many) DocumentTag`
- `Document (1) -> (many) DocumentShare`
- `Document (1) -> (many) DocumentActivity`

## Validation Rules
- Allowed upload extensions: `.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .jpg, .jpeg, .png`
- Max file size: `25 MB`
- Category allowed values:
  - `Project Documents`
  - `Team Resources`
  - `Personal Files`
  - `Reports`
  - `Presentations`
  - `Other`
- Project association requires uploader authorization on target project.
- Replace-file operation preserves `DocumentId` and updates file attributes/path.

## State Transitions

### Document Lifecycle
- `PendingScan` -> `Clean` (background queue-triggered scan succeeds)
- `PendingScan` -> `Rejected` (background scan fails or scanner unavailable)
- `Clean` -> `Clean` (metadata edits)
- `Clean` -> `PendingScan` (file replaced and re-queued for scan)
- `Clean` -> `Deleted` (authorized delete + confirmation)

### Access Visibility
- `VisibleToOwner`
- `VisibleToProjectMembers` (if project-associated and authorized)
- `VisibleToShareRecipients` (if explicit share exists)
- `VisibleToAdministrator` (audit/compliance override)
- `NotVisible` (deleted or unauthorized)
