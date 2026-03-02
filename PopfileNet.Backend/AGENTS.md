# Backend API Guidelines

## Project Overview
PopfileNet.Backend provides REST APIs for the email classification system.

## Coding Standards
Follow @docs/coding-standards.md for all code changes.

## Code Organization
- **One class/record per file** - each type should be in its own file with matching filename
- DTOs go in `Models/` folder
- Endpoint groups go in `Groups/` folder

## API Conventions

### Response Format
- Use `ApiResponse<T>` for single object responses
- Use `PagedApiResponse<T>` for collection responses
- Check `IsSuccess` to determine if the response contains data or an error

### Paging
- List endpoints must support paging with defaults: `page = 1`, `pageSize = 20`
- Maximum `pageSize` should be capped at 100
- `PagedApiResponse<T>` includes: `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious`, `HasNext`, `IsSuccess`, `Error`

### Error Handling
- Return `400 Bad Request` for invalid input
- Return `404 Not Found` for missing resources
- Return `500 Internal Server Error` for unexpected errors

## Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/categories` | GET | Get all classification categories (buckets) |
| `/accounts` | GET | List all mail accounts (paged) |
| `/jobs/sync` | POST | Start syncing emails to database |
| `/folders` | GET | List all folders in DB (paged) |
| `/mails` | GET | List all mails in DB (paged) |
| `/mails/{id}` | GET | Get a specific mail by ID |

## Dependencies
- PopfileNet.Database for EF Core DbContext
- PopfileNet.Common for domain models
- PopfileNet.Backend.Services for business logic
