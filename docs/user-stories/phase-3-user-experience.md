# Phase 3: User Experience

## Story 3.1: Dashboard Statistics

**Title:** View Classification Statistics

**As a** user,
**I want** to see statistics about my emails and classifications
**So that** I can understand how the classifier is performing

### Acceptance Criteria

- [ ] Dashboard shows total emails in database
- [ ] Shows emails per bucket (pie/bar chart)
- [ ] Shows classifier status (trained/not trained)
- [ ] Shows last sync time
- [ ] Shows email count trend over time (optional)

---

## Story 3.2: Sync Status Per Folder

**Title:** Track Sync Status by Folder

**As a** user,
**I want** to see when each folder was last synced and how many emails were added
**So that** I can monitor the sync process

### Acceptance Criteria

- [ ] Each folder shows last sync timestamp
- [ ] Shows number of emails synced in last sync
- [ ] Shows sync status: idle, syncing, error
- [ ] Shows error message if sync failed

---

## Story 3.3: Move Emails to IMAP Folder

**Title:** Move Classified Emails to Target Folder

**As a** user,
**I want** the system to move emails to their classified IMAP folder
**So that** my email client reflects the classifications

### Acceptance Criteria

- [ ] After classification, option to move to bucket's mapped folder
- [ ] Moves email in IMAP using MOVE command
- [ ] Updates local database after move
- [ ] Shows confirmation of move action
- [ ] Handles move failures gracefully
- [ ] Configurable: enable/disable auto-move
