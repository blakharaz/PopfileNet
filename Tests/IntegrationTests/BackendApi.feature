Feature: Backend API
    As a client application
    I want to verify the backend API is working
    So that I can integrate with it

Scenario: Root endpoint is accessible
    Given the API is running
    When I request the root endpoint "/"
    Then I should receive a successful response

Scenario: Accounts endpoint returns OK
    Given the API is running
    When I request the accounts endpoint "/accounts"
    Then I should receive an OK response
