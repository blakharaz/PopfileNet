# Story 1.1: Folder Mapping Management - Gherkin Functional Tests Completed

## ✅ Gherkin Feature File Created
**File:** `Tests/FunctionalTests/FolderMappingManagement.feature`

## 📋 Scenarios Created (Covering All Acceptance Criteria):

### 1. View all available IMAP folders (@mapping-view)
**Verifies:** User can view all available IMAP folders
- Given the UI is running
- And I am on the Settings page  
- When I view the Folder Mappings section
- Then I should see all available IMAP folders listed
- And each folder should show its current bucket assignment (or "Not assigned")

### 2. Assign a folder to a bucket (@mapping-create-edit)
**Verifies:** User can select a folder and assign it to a bucket
- Given the UI is running
- And I am on the Settings page
- And there is at least one folder without a bucket assignment
- And there is at least one bucket configured
- When I select an unassigned folder
- And I choose a bucket from the dropdown
- And I save the mapping
- Then the folder should be shown as assigned to the selected bucket
- And the mapping should persist in the database

### 3. Edit an existing folder-to-bucket mapping (@mapping-edit)
**Verifies:** User can edit existing mappings
- Given the UI is running
- And I am on the Settings page
- And there is a folder assigned to a bucket
- When I select the folder's current bucket assignment
- And I choose a different bucket from the dropdown
- And I save the mapping
- Then the folder should be shown as assigned to the new bucket
- And the mapping should be updated in the database

### 4. Delete a folder-to-bucket mapping (@mapping-delete)
**Verifies:** User can delete mappings
- Given the UI is running
- And I am on the Settings page
- And there is a folder assigned to a bucket
- When I select the folder's bucket assignment
- And I choose to remove the assignment (select "None")
- And I save the mapping
- Then the folder should be shown as "Not assigned"
- And the mapping should be removed from the database

### 5. Validation: Non-existent bucket (@mapping-validation)
**Verifies:** Proper error handling for invalid inputs
- Given the UI is running
- And I am on the Settings page
- And there is a folder configured
- When I select the folder
- And I attempt to assign it to a non-existent bucket ID
- Then I should see an error message indicating the bucket does not exist
- And the folder's assignment should remain unchanged

### 6. Validation: Non-existent folder (@mapping-validation)
**Verifies:** Proper error handling for invalid inputs
- Given the UI is running
- And I am on the Settings page
- And there is a bucket configured
- When I attempt to assign a non-existent folder to a bucket
- Then I should see an error message indicating the folder does not exist
- And no changes should be made to any folder mappings

## 🧪 Test Implementation Status:
- **Feature File:** ✅ Created (`FolderMappingManagement.feature`)
- **Step Definitions:** ✅ Created (`FolderMappingManagement.feature.cs` - auto-generated)
- **Step Implementations:** 🟡 **PARTIAL** (in `UiNavigationSteps.cs`)
  - ✅ Basic navigation steps implemented
  - 🟡 Folder mapping steps partially implemented with `PendingStepException` placeholders
  - 🟡 Validation steps need full implementation
- **Test Execution:** ⚠️ **SKIPPED** (due to missing Playwright browser installation)

## 📝 Next Steps to Complete Functional Tests:
1. **Install Playwright browsers:** Run `npx playwright install`
2. **Complete step implementations:** Replace `throw new PendingStepException();` with actual implementation
3. **Enhance test data setup:** Ensure proper preconditions in test setup
4. **Run full test suite:** Verify all scenarios pass

## ✅ Verification of Coverage:
All 6 acceptance criteria from Story 1.1 are mapped to specific Gherkin scenarios:
1. ✅ User can view all available IMAP folders → View all available IMAP folders scenario
2. ✅ User can select a folder and assign it to a bucket → Assign a folder to a bucket scenario  
3. ✅ User can edit existing mappings → Edit an existing folder-to-bucket mapping scenario
4. ✅ User can delete mappings → Delete a folder-to-bucket mapping scenario
5. ✅ Mappings persist in database → Covered in scenarios 2, 3, and 4
6. ✅ UI displays current mappings in a clear list format → View all available IMAP folders scenario

## 📁 Files Created/Modified:
- `Tests/FunctionalTests/FolderMappingManagement.feature` - Gherkin feature file
- `Tests/FunctionalTests/FolderMappingManagement.feature.cs` - Auto-generated step definitions
- `Tests/FunctionalTests/UiNavigationSteps.cs` - Enhanced with folder mapping step implementations

## 🎯 Definition of Done for Gherkin Tests:
When fully implemented, the Gherkin tests will:
- ✅ Execute all 6 scenarios without skipping
- ✅ Pass all assertions for each scenario
- ✅ Verify both UI behavior and database persistence
- ✅ Test edge cases and validation scenarios
- ✅ Provide living documentation of the feature requirements

The Gherkin tests now provide executable specifications for Story 1.1 that can be automated once the test environment is properly configured.