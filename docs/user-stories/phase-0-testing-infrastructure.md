# Phase 0: Testing Infrastructure

> **Critical:** Story 0.0 must be completed before any other work.

## Story 0.0: Replace FluentAssertions with Shouldly

**Title:** Migrate from FluentAssertions to Shouldly

**As a** maintainer,
**I want** to replace FluentAssertions with an open-source alternative
**So that** the project remains under permissive open-source licensing

**Background:** FluentAssertions v8+ switched to a commercial license (January 2025). All existing projects use FluentAssertions 8.8.0.

**Acceptance Criteria:**
- [ ] Replace FluentAssertions package with Shouldly in all test projects
- [ ] PopfileNet.Common.Tests - compiles and tests pass
- [ ] PopfileNet.Database.Tests - compiles and tests pass
- [ ] PopfileNet.Imap.Tests - compiles and tests pass
- [ ] PopfileNet.Classifier.Tests - compiles and tests pass
- [ ] PopfileNet.Ui.Tests - compiles and tests pass
- [ ] Run dotnet build to verify all projects compile
- [ ] Run dotnet test to verify all tests pass

---

## Story 0.1: Backend API Integration Tests Project

**Title:** Create Backend Integration Test Project

**As a** developer,
**I want** an integration test project for the Backend API
**So that** I can verify API endpoints work correctly

**Acceptance Criteria:**
- [ ] Create PopfileNet.Backend.IntegrationTests project
- [ ] Add Microsoft.AspNetCore.Mvc.Testing package
- [ ] Add Shouldly package
- [ ] Add Testcontainers.PostgreSQL package
- [ ] Configure test database with Testcontainers
- [ ] Project compiles and runs tests

---

## Story 0.2: Settings API Integration Tests

**Title:** Test Settings Endpoints

**As a** developer,
**I want** integration tests for settings API endpoints
**So that** I can verify settings CRUD operations work

**Acceptance Criteria:**
- [ ] Test GET /settings returns current settings
- [ ] Test POST /settings saves settings
- [ ] Test POST /settings/test-connection works
- [ ] Test bucket CRUD endpoints
- [ ] All tests use Shouldly for assertions

---

## Story 0.3: Classifier API Integration Tests

**Title:** Test Classifier Endpoints

**As a** developer,
**I want** integration tests for classifier endpoints
**So that** I can verify training and prediction work

**Acceptance Criteria:**
- [ ] Test GET /classifier/status
- [ ] Test POST /classifier/train
- [ ] Test POST /classifier/predict
- [ ] All tests use Shouldly for assertions

---

## Story 0.4: Mails API Integration Tests

**Title:** Test Mails Endpoints

**As a** developer,
**I want** integration tests for mail API endpoints
**So that** I can verify mail sync and retrieval work

**Acceptance Criteria:**
- [ ] Test GET /mails returns paged results
- [ ] Test GET /mails/{id} returns email detail
- [ ] Test GET /folders returns folders
- [ ] Test POST /jobs/sync triggers sync
- [ ] All tests use Shouldly for assertions

---

## Story 0.5: UI Page Integration Tests

**Title:** Add Tests for UI Pages

**As a** developer,
**I want** comprehensive UI component tests
**So that** I can verify user interactions work correctly

**Acceptance Criteria:**
- [ ] Add tests for Settings page (save, test connection)
- [ ] Add tests for Sync page (sync trigger, pagination)
- [ ] Add tests for Classify page (train, predict)
- [ ] Add tests for Home page navigation
- [ ] Add tests for Mails page (view, pagination)
- [ ] All tests use Shouldly for assertions

---

## Story 0.6: Database Integration Tests

**Title:** Add Database Integration Tests

**As a** developer,
**I want** integration tests with real PostgreSQL
**So that** I can verify database operations work with actual database

**Acceptance Criteria:**
- [ ] Create PopfileNet.Database.IntegrationTests project
- [ ] Configure Testcontainers PostgreSQL
- [ ] Test EmailRepository bulk insert
- [ ] Test EmailRepository deduplication
- [ ] Test FolderRepository operations
- [ ] All tests use Shouldly for assertions

---

## Story 0.7: IMAP Service Integration Tests

**Title:** Add IMAP Service Tests with Mock Server

**As a** developer,
**I want** tests for IMAP service with mock server
**So that** I can verify IMAP operations without real server

**Acceptance Criteria:**
- [ ] Add WireMock or similar for IMAP mocking
- [ ] Test connection and authentication
- [ ] Test folder listing
- [ ] Test email fetching
- [ ] All tests use Shouldly for assertions
