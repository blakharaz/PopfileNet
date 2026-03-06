# Phase 1: Core Experience

## Story 1.1: Folder Mapping Management

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

---

## Story 1.2: Email Detail View

**Title:** View Full Email Content

**As a** user,
**I want** to click on an email and see its full content
**So that** I can verify the classification is correct

### Acceptance Criteria

- [ ] Clicking email opens detail view/modal
- [ ] Shows: Subject, From, To, Date, Folder
- [ ] Shows: Plain text body
- [ ] Shows: HTML body (rendered if HTML, raw if plain)
- [ ] Shows: Email headers
- [ ] Close button returns to list

---

## Story 1.3: Email Counts Before Sync

**Title:** Show Email Counts Per Folder

**As a** user,
**I want** to see how many emails are in each IMAP folder before syncing
**So that** I can decide which folders to sync

### Acceptance Criteria

- [ ] Sync page displays folder name with email count
- [ ] Count updates when folders are refreshed
- [ ] Shows count of new (unsynced) emails vs total
