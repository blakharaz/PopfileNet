# Phase 2: Classification Improvements

## Story 2.1: Model Persistence

**Title:** Save and Load Trained Model

**As a** user,
**I want** the trained classifier model to persist between application restarts
**So that** I don't have to retrain every time I start the app

### Acceptance Criteria

- [ ] Model is saved to disk after training
- [ ] Model is loaded on application startup if exists
- [ ] Classifier status shows "Loaded from disk" vs "Not trained"
- [ ] Handles missing/corrupted model file gracefully
- [ ] Model file location configurable

---

## Story 2.2: Emails Grouped by Bucket

**Title:** View Training Data by Bucket

**As a** user,
**I want** to see emails grouped by their bucket/category
**So that** I can verify training data and understand classifier behavior

### Acceptance Criteria

- [ ] Classifier page shows sections for each bucket
- [ ] Each section lists emails assigned to that bucket
- [ ] Shows count per bucket
- [ ] Clicking email shows detail view
- [ ] Works with both mapped folders and manual assignments

---

## Story 2.3: Auto-Classification During Sync

**Title:** Automatically Classify New Emails

**As a** user,
**I want** new emails to be classified automatically during sync
**So that** I don't have to manually classify each email

### Acceptance Criteria

- [ ] Sync process runs classifier on new emails
- [ ] Classification results stored in database
- [ ] Shows classification results in sync summary
- [ ] If model not trained, emails sync without classification
- [ ] Configurable: enable/disable auto-classify
