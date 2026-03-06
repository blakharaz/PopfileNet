Feature: UI Navigation

Scenario: User can access the home page
    Given the user navigates to the application
    When the page loads
    Then the home page should be displayed
