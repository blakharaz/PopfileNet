Feature: Health Check

Scenario: Backend API is running
    Given the backend API is available
    When I request the health endpoint
    Then I should receive a successful response
