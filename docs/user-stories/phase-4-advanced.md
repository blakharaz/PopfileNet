# Phase 4: Advanced Features

## Story 4.1: OAuth2 Authentication

**Title:** Authenticate with OAuth2

**As a** user,
**I want** to authenticate using OAuth2 (Google, Microsoft)
**So that** I don't need to use an app password

### Acceptance Criteria

- [ ] Support Google OAuth2 authentication
- [ ] Support Microsoft/Outlook OAuth2 authentication
- [ ] Store OAuth tokens securely
- [ ] Handle token refresh automatically
- [ ] Fallback to password if OAuth unavailable

---

## Story 4.2: Scheduled Automatic Sync

**Title:** Configure Automatic Sync Schedule

**As a** user,
**I want** to configure automatic email sync on a schedule
**So that** my classifications stay up to date without manual action

### Acceptance Criteria

- [ ] Settings page has sync interval option
- [ ] Options: manual, every 15min, hourly, daily
- [ ] Background service respects configured interval
- [ ] Shows next scheduled sync time
- [ ] Can manually trigger immediate sync

---

## Story 4.3: Multi-Account Support

**Title:** Manage Multiple Email Accounts

**As a** user,
**I want** to configure and manage multiple email accounts
**So that** I can classify emails from all my accounts in one place

### Acceptance Criteria

- [ ] Can add multiple IMAP accounts
- [ ] Each account has separate settings
- [ ] Can sync all accounts or select specific ones
- [ ] Accounts page lists all configured accounts
- [ ] Can delete accounts
