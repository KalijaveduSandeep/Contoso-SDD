# Quickstart: Document Upload and Management

## Prerequisites
- .NET 10 SDK installed
- SQL Server LocalDB available
- Repository root: `Contoso-SDD`
- Queue scanning stack available locally (Azure Functions host + Azurite queue emulator)

## Run
1. `cd ContosoDashboard`
2. Start Azurite queue emulator (for local scan queue): `azurite --queue --location .azurite --silent`
3. `dotnet run --launch-profile http`
4. Open `http://localhost:5000`
5. Login as one of seeded users from the login page.

## Local Queue Notes
- App uses `ScanQueue:ConnectionString=UseDevelopmentStorage=true` and queue `document-scan-jobs`.
- If Azurite is not running, uploads fail fast with queue/scanning unavailable message.
- For worker simulation, poll queue messages and update `Document.ScanStatus` from `Pending` to `Clean`/`Rejected`.

## Validation Scenarios

### 1) Upload success path (P1)
1. Navigate to Documents page.
2. Upload a supported file (`.pdf` or `.png`) <= 25 MB with required title and category.
3. Verify accepted message and new row in My Documents with `Pending` scan status.
4. Wait for queue-triggered scan job completion and refresh list.

Expected:
- Upload is accepted and queued for scan.
- Document appears in list with category, file size, upload timestamp, uploader, and scan status.
- Status transitions from `Pending` to `Clean` after worker processing.

### 2) Validation failure path (P1)
1. Try unsupported extension or file > 25 MB.
2. Submit upload.

Expected:
- Upload blocked.
- Clear validation message displayed.
- No new document record.

### 2a) Queue/scanner unavailable path (P1)
1. Stop queue worker and/or queue emulator.
2. Submit valid upload.

Expected:
- Request fails with queue/scanning unavailable error.
- No document becomes downloadable/previewable.

### 3) Authorization boundaries (P2/P3)
1. Upload a document as User A.
2. Attempt direct access as unauthorized User B.
3. Attempt access as Administrator.

Expected:
- Unauthorized user cannot view/download/preview/edit/delete.
- Administrator can access for audit/compliance.

### 4) Project integration (P2/P4)
1. Upload document associated to a project where caller is a member.
2. Open project details and verify document visibility.
3. Upload from task context and verify task project auto-association.

Expected:
- Project/team visibility aligns with membership and role.
- Task upload is linked to task's project.

### 5) Sharing workflow (P3)
1. Share a document with one user.
2. Login as recipient.
3. Open Shared with Me and notifications.

Expected:
- Recipient sees in-app notification.
- Shared document appears in Shared with Me.

### 6) Dashboard/reporting integration (P4)
1. Upload several documents.
2. Open dashboard and verify Recent Documents widget and count card update.
3. As admin, open report endpoint/page and verify aggregate metrics.

Expected:
- Dashboard reflects latest five uploaded documents for user.
- Reporting includes file type, uploader activity, and access pattern aggregates.

## Quality Gate
- Build passes: `dotnet build ..\Contoso-SDD.sln`
- Manual scenarios above pass without unauthorized data exposure.
- Async scan pipeline processes queued uploads and enforces pending/rejected access blocks.
