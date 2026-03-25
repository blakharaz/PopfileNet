# Implementation Plan for Story 1.1: Folder Mapping Management

## Story Description
**Title:** Manage Folder-to-Bucket Mappings

**As a** user,
**I want** to create, edit, and delete folder-to-bucket mappings
**So that** I can control how emails from different folders are classified

### Acceptance Criteria
- [ ] User can view all available IMAP folders
- [ ] User can select a folder and assign it to a bucket
- [ ] User can edit existing mappings
- [ ] User can delete mappings
- [ ] Mappings persist in database
- [ ] UI displays current mappings in a clear list format

## Current State Analysis
- FolderMappingDto exists in both backend (`/PopfileNet.Backend/Models/FolderMappingDto.cs`) and UI
- Folder mappings are already included in settings retrieval (`SettingsService.cs` line 30)
- Database relationship exists: Bucket has many MailFolders (FK BucketId in MailFolder)
- `/folders` endpoint exists to list folders
- No specific API endpoints for creating/editing/deleting folder mappings

## Implementation Approach
Implement the feature in four incremental PRs, each adding a specific capability with corresponding tests:

### Increment 1: Add Folder Mapping Retrieval Endpoint
**Goal:** Allow viewing current folder-to-bucket mappings

**Backend Changes:**
- Add `GET /settings/folder-mappings` endpoint in `SettingsGroupExtensions.cs`
- Add `GetFolderMappingsAsync` method to `ISettingsService` and `SettingsService`
- Return list of all folder mappings with folder names and assigned bucket IDs

**Tests:**
- Unit tests for `SettingsService.GetFolderMappingsAsync()`
- Integration tests for `GET /settings/folder-mappings` endpoint
- Verify response includes all folders with correct bucket assignments

**UI Changes:**
- Update Settings page to call the new endpoint on load
- Display current folder mappings in a clear list/table format
- Show folder name and assigned bucket (or "Not assigned" if none)

### Increment 2: Add Folder Mapping Creation/Update Endpoint
**Goal:** Allow assigning/changing a folder's bucket

**Backend Changes:**
- Add `POST /settings/folder-mappings` endpoint in `SettingsGroupExtensions.cs`
- Add `SetFolderMappingAsync` method to `ISettingsService` and `SettingsService`
- Accept folder name and bucket ID, update the folder's bucket assignment in database
- Handle both new assignments and changes to existing assignments

**Tests:**
- Unit tests for `SettingsService.SetFolderMappingAsync(string folderName, string? bucketId)`
- Integration tests for `POST /settings/folder-mappings` endpoint
- Test assigning a folder to a bucket
- Test changing a folder's bucket assignment
- Test assigning a folder to no bucket (null/empty bucket ID)
- Verify database persistence

**UI Changes:**
- Add folder selection UI (dropdown of available IMAP folders)
- Add bucket assignment UI (dropdown of available buckets + "None" option)
- Add "Assign" or "Save" button for each folder
- Show success/error feedback after assignment
- Automatically refresh the folder mappings list after successful assignment

### Increment 3: Add Folder Mapping Deletion Endpoint
**Goal:** Allow removing a folder's bucket assignment

**Backend Changes:**
- Add `DELETE /settings/folder-mappings/{folderName}` endpoint in `SettingsGroupExtensions.cs`
- Add `RemoveFolderMappingAsync` method to `ISettingsService` and `SettingsService`
- Set the folder's bucket assignment to NULL in database

**Tests:**
- Unit tests for `SettingsService.RemoveFolderMappingAsync(string folderName)`
- Integration tests for `DELETE /settings/folder-mappings/{folderName}` endpoint
- Test removing a folder's bucket assignment
- Verify the folder shows as "Not assigned" afterward
- Verify database persistence (BucketId set to NULL)

**UI Changes:**
- Add "Remove" or "Clear Assignment" button for each folder mapping
- Show success feedback after removal
- Automatically refresh the folder mappings list after successful removal
- Update UI to show folder as "Not assigned" after removal

### Increment 4: Integration and Polish
**Goal:** Ensure complete, working functionality

**Quality Assurance:**
- Run all tests to verify nothing is broken
- Test edge cases:
  - Assigning same bucket to multiple folders (should be allowed)
  - Attempting to assign non-existent folder (should return appropriate error)
  - Attempting to assign non-existent bucket (should return appropriate error)
  - Concurrent modifications
- Verify UI updates correctly after all operations
- Ensure error handling is appropriate (invalid folder names, etc.)
- Test with empty database state
- Test with pre-existing mappings

**Manual Verification:**
- Complete workflow test:
  1. Start with no mappings
  2. Assign Folder A to Bucket 1
  3. Assign Folder B to Bucket 2
  4. Change Folder A to Bucket 2
  5. Remove Folder B's assignment
  6. Verify final state matches expectations
- Verify mappings persist across application restarts

## Technical Details

### API Endpoints:
1. `GET /settings/folder-mappings` - Returns list of all folder mappings
   - Response: Array of `FolderMappingDto` objects
   - Example: `[{"name": "Inbox", "bucketId": "bucket1"}, {"name": "Archive", "bucketId": null}]`

2. `POST /settings/folder-mappings` - Creates/updates a folder mapping
   - Request: `FolderMappingDto` object
   - Response: Created/updated `FolderMappingDto` object
   - Status: 200 OK for update, 201 Created for new mapping

3. `DELETE /settings/folder-mappings/{folderName}` - Deletes a folder mapping
   - Response: 204 No Content on success
   - Status: 404 Not Found if folder doesn't exist

### Service Methods:
1. `Task<IReadOnlyList<FolderMappingDto>> GetFolderMappingsAsync(CancellationToken ct = default)`
2. `Task SetFolderMappingAsync(string folderName, string? bucketId, CancellationToken ct = default)`
3. `Task RemoveFolderMappingAsync(string folderName, CancellationToken ct = default)`

### Database Operations:
- Setting a mapping: Update `MailFolder.BucketId` to the bucket's ID
- Removing a mapping: Set `MailFolder.BucketId` to `NULL`
- The existing foreign key relationship (`MailFolder.BucketId` → `Bucket.Id`) handles persistence
- Folder name lookup: Find `MailFolder` by `Name` field

### Error Handling:
- Return 400 Bad Request for invalid input (null/empty folder name)
- Return 404 Not Found if folder doesn't exist in database
- Return 400 Bad Request if bucket ID doesn't correspond to an existing bucket
- Return 500 Internal Server Error for unexpected database errors

### UI Implementation Notes:
- Fetch folders from `/folders` endpoint to populate folder dropdown
- Fetch buckets from `/settings/buckets` endpoint to populate bucket dropdown
- Use reactive updates to refresh folder mappings list after changes
- Show loading states during API calls
- Display validation errors to user
- Consider using a table or list layout for displaying mappings:
  ```
  | Folder Name | Assigned Bucket | Actions |
  |-------------|-----------------|---------|
  | Inbox       | Personal        | [Change] [Remove] |
  | Work        | Work            | [Change] [Remove] |
  | Archive     | Not assigned    | [Assign] [ - ]    |
  ```

## Dependencies
- No new external dependencies required
- Uses existing:
  - `PopfileNet.Backend.Models.FolderMappingDto`
  - `PopfileNet.Database.MailFolder` entity
  - `PopfileNet.Database.Bucket` entity
  - Existing settings infrastructure
  - Existing IMAP folder discovery (`ImapService.GetAllPersonalFoldersAsync()`)

## Definition of Done
- [ ] All acceptance criteria met
- [ ] Backend API endpoints implemented and tested
- [ ] Service methods implemented and tested
- [ ] Database persistence verified
- [ ] UI updated to support full folder mapping management
- [ ] All unit and integration tests passing
- [ ] Manual verification of complete workflow successful
- [ ] Code follows existing project conventions and standards