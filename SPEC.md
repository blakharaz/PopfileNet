# PopfileNet Web UI Specification

## Overview

Blazor Web UI for PopfileNet using Microsoft Fluent UI Blazor. Provides configuration, mail sync, and classification capabilities via REST API.

## Architecture

- **PopfileNet.Ui**: Blazor Server app with Fluent UI components
- **PopfileNet.Backend**: REST API serving the UI
- Communication: HTTP client from UI to Backend REST API

## UI Pages

### 1. Settings Page (`/settings`)

**Purpose**: Configure IMAP connection and folder-to-bucket mappings

**Form Fields**:
- IMAP Server (text input, required)
- IMAP Port (number input, default 993)
- Username (text input, required)
- Password (password input, required)
- Use SSL (toggle, default true)
- Test Connection button

**Folder Mappings**:
- List of available IMAP folders
- Each folder can be mapped to a bucket (category)
- Add/Edit/Delete bucket mappings
- Buckets: Name, Description

**Actions**:
- Save Settings (POST to `/settings`)
- Test Connection (POST to `/settings/test-connection`)

### 2. Mail Sync Page (`/sync`)

**Purpose**: Load emails from IMAP into local database

**UI Elements**:
- List of available IMAP folders with email counts
- "Sync All Folders" button
- "Sync Selected" button
- Progress indicator during sync
- Last sync status per folder
- Total emails in database counter

**Actions**:
- Get Folders: GET `/mails/folders`
- Sync Folder: POST `/mails/sync/{folderName}`
- Get Stats: GET `/mails/stats`

### 3. Classifier Page (`/classify`)

**Purpose**: Train and run the email classifier

**Training Section**:
- Display emails grouped by bucket from database
- Train Model button
- Training status (trained/not trained)
- Training data count

**Prediction Section**:
- Select email from database
- Run Classification button
- Display prediction result (bucket name + confidence)
- Show all bucket probabilities

## API Endpoints

### Settings
- `GET /settings` - Get current configuration
- `POST /settings` - Save configuration
- `POST /settings/test-connection` - Test IMAP connection
- `GET /settings/buckets` - Get all buckets
- `POST /settings/buckets` - Create bucket
- `PUT /settings/buckets/{id}` - Update bucket
- `DELETE /settings/buckets/{id}` - Delete bucket

### Mail Operations
- `GET /mails/folders` - Get IMAP folders
- `GET /mails/stats` - Get database statistics
- `POST /mails/sync/{folderName}` - Sync folder to database
- `GET /mails/{folderName}` - Get emails from database by folder

### Classification
- `POST /classifier/train` - Train classifier
- `POST /classifier/predict` - Predict bucket for email
- `GET /classifier/status` - Get training status

## Configuration Storage

Settings stored in `appsettings.json` in Backend project:
```json
{
  "ImapSettings": {
    "Server": "imap.example.com",
    "Port": 993,
    "Username": "user@example.com",
    "Password": "secret",
    "UseSsl": true
  },
  "Buckets": [
    { "Id": "guid", "Name": "Work", "Description": "Work emails" },
    { "Id": "guid", "Name": "Personal", "Description": "Personal emails" }
  ],
  "FolderMappings": [
    { "FolderName": "INBOX", "BucketId": "guid" }
  ],
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

## UI Components

### Shared Components
- `MainLayout.razor` - App shell with navigation
- `NavMenu.razor` - Side navigation
- `PageTitle.razor` - Page header component
- `LoadingSpinner.razor` - Loading indicator
- `StatusMessage.razor` - Success/Error messages

### Pages
- `Settings.razor` - Configuration page
- `Sync.razor` - Mail sync page
- `Classify.razor` - Classifier page
- `Index.razor` - Dashboard/home

## Acceptance Criteria

1. **Settings Page**:
   - All IMAP settings can be configured and persisted
   - Connection test provides clear success/failure feedback
   - Folder-to-bucket mappings can be created and edited

2. **Mail Sync Page**:
   - Shows available IMAP folders
   - Successfully syncs emails to database
   - Shows sync progress and results

3. **Classifier Page**:
   - Can train classifier with emails from database
   - Can classify individual emails
   - Shows prediction confidence

4. **General**:
   - App builds without errors
   - All API endpoints respond correctly
   - Fluent UI theme is properly applied
