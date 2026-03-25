# Folder Mapping Management Feature
Feature: Manage Folder-to-Bucket Mappings
  As a user
  I want to create, edit, and delete folder-to-bucket mappings
  So that I can control how emails from different folders are classified

  @mapping-view
  Scenario: View all available IMAP folders
    Given the UI is running
    And I am on the Settings page
    When I view the Folder Mappings section
    Then I should see all available IMAP folders listed
    And each folder should show its current bucket assignment (or "Not assigned")

  @mapping-create-edit
  Scenario: Assign a folder to a bucket
    Given the UI is running
    And I am on the Settings page
    And there is at least one folder without a bucket assignment
    And there is at least one bucket configured
    When I select an unassigned folder
    And I choose a bucket from the dropdown
    And I save the mapping
    Then the folder should be shown as assigned to the selected bucket
    And the mapping should persist in the database

  @mapping-create-edit
  Scenario: Change a folder's bucket assignment
    Given the UI is running
    And I am on the Settings page
    And there is a folder assigned to Bucket 1
    And there is a different Bucket 2 configured
    When I select the folder
    And I choose Bucket 2 from the dropdown
    And I save the mapping
    Then the folder should be shown as assigned to Bucket 2
    And the mapping should be updated in the database

  @mapping-create-edit
  Scenario: Unassign a folder from its bucket
    Given the UI is running
    And I am on the Settings page
    And there is a folder assigned to a bucket
    When I select the folder
    And I choose to remove the assignment (select "None")
    And I save the mapping
    Then the folder should be shown as "Not assigned"
    And the mapping should be removed from the database

  @mapping-edit
  Scenario: Edit an existing folder-to-bucket mapping
    Given the UI is running
    And I am on the Settings page
    And there is a folder assigned to a bucket
    When I select the folder's current bucket assignment
    And I choose a different bucket from the dropdown
    And I save the mapping
    Then the folder should be shown as assigned to the new bucket
    And the mapping should be updated in the database

  @mapping-delete
  Scenario: Delete a folder-to-bucket mapping
    Given the UI is running
    And I am on the Settings page
    And there is a folder assigned to a bucket
    When I select the folder's bucket assignment
    And I choose to remove the assignment (select "None")
    And I save the mapping
    Then the folder should be shown as "Not assigned"
    And the mapping should be removed from the database

  @mapping-validation
  Scenario: Attempt to assign a folder to a non-existent bucket
    Given the UI is running
    And I am on the Settings page
    And there is a folder configured
    When I select the folder
    And I attempt to assign it to a non-existent bucket ID
    Then I should see an error message indicating the bucket does not exist
    And the folder's assignment should remain unchanged

  @mapping-validation
  Scenario: Attempt to assign a non-existent folder to a bucket
    Given the UI is running
    And I am on the Settings page
    And there is a bucket configured
    When I attempt to assign a non-existent folder to a bucket
    Then I should see an error message indicating the folder does not exist
    And no changes should be made to any folder mappings

  @mapping-validation
  Scenario: Attempt to assign with empty folder name
    Given the UI is running
    And I am on the Settings page
    When I attempt to assign an empty folder name to a bucket
    Then I should see an error message indicating the folder name is required
    And no changes should be made to any folder mappings

  @mapping-validation
  Scenario: Attempt to assign with whitespace-only folder name
    Given the UI is running
    And I am on the Settings page
    When I attempt to assign a whitespace-only folder name to a bucket
    Then I should see an error message indicating the folder name is required
    And no changes should be made to any folder mappings

  @mapping-persistence
  Scenario: Mappings persist across application restarts
    Given the UI is running
    And I am on the Settings page
    And there is a folder assigned to a bucket
    When I save the mapping
    And I restart the application
    And I navigate back to the Settings page
    Then the folder should still be shown as assigned to the same bucket

  @mapping-workflow
  Scenario: Complete folder mapping workflow
    Given the UI is running
    And I am on the Settings page
    And there are no existing folder mappings
    And there are at least two buckets configured (Bucket 1 and Bucket 2)
    When I assign Folder A to Bucket 1
    And I assign Folder B to Bucket 2
    Then Folder A should be shown as assigned to Bucket 1
    And Folder B should be shown as assigned to Bucket 2
    When I change Folder A's assignment to Bucket 2
    Then Folder A should be shown as assigned to Bucket 2
    And Folder B should still be shown as assigned to Bucket 2
    When I remove Folder B's assignment
    Then Folder B should be shown as "Not assigned"
    And Folder A should still be shown as assigned to Bucket 2

  @mapping-ui
  Scenario: UI displays folder mappings in clear table format
    Given the UI is running
    And I am on the Settings page
    When I view the Folder Mappings section
    Then I should see a table with columns: Folder Name, Assigned Bucket, Actions
    And each row should display the folder name, bucket assignment (or "(None)"), and action buttons
    And unassigned folders should show "(None)" in the Assigned Bucket column

  @mapping-ui-feedback
  Scenario: UI shows success feedback after saving mapping
    Given the UI is running
    And I am on the Settings page
    And there is a folder without a bucket assignment
    And there is a bucket configured
    When I select the folder
    And I choose a bucket from the dropdown
    And I save the mapping
    Then I should see a success message indicating the mapping was saved

  @mapping-ui-feedback
  Scenario: UI shows error feedback for invalid input
    Given the UI is running
    And I am on the Settings page
    When I attempt to assign a folder to a non-existent bucket
    Then I should see an error message indicating the bucket does not exist

  @mapping-ui-loading
  Scenario: UI shows loading state during API calls
    Given the UI is running
    And I am on the Settings page
    When I perform an API operation (get mappings, save mapping, etc.)
    Then I should see a loading indicator during the operation

  @mapping-multi-bucket
  Scenario: Multiple folders can be assigned to the same bucket
    Given the UI is running
    And I am on the Settings page
    And there are at least two folders without bucket assignments
    And there is at least one bucket configured
    When I assign Folder 1 to the bucket
    And I assign Folder 2 to the same bucket
    Then both folders should be shown as assigned to the same bucket