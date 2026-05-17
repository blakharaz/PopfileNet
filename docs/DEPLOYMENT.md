# Deployment Guide

Deploy PopfileNet locally or on a server running Ubuntu 26.04.

## Local Deployment

### Prerequisites

- [Docker](https://www.docker.com/) (or Podman)
- .NET SDK 10.0 for building images

### Quick Start with Docker Compose

PopfileNet uses Aspire to generate `docker-compose.yaml` at release time. For local development, you can use the generated compose file or create your own:

```yaml
version: "3"
services:
  postgres:
    image: docker.io/library/postgres:17-alpine
    environment:
      POSTGRES_PASSWORD: "${POSTGRES_PASSWORD:-changeme}"
    volumes:
      - pgdata:/var/lib/postgresql/data

  backend:
    image: ghcr.io/blakharaz/popfilenet-backend:v0.3
    ports:
      - "8000:80"
    environment:
      DB_HOST: postgres
      DB_DATABASE: popfilenet
      DB_USERNAME: postgres
      DB_PASSWORD: "${POSTGRES_PASSWORD:-changeme}"
      DevMode: "true"

  ui:
    image: ghcr.io/blakharaz/popfilenet-ui:v0.3
    ports:
      - "8001:80"
    environment:
      BACKEND_URL: http://backend:80/

volumes:
  pgdata:
```

### Environment Variables

Copy `.env.example` to `.env` and fill in your values:

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `POSTGRES_PASSWORD` | PostgreSQL password | `changeme` | Yes |
| `IMAP_SERVER` | IMAP server address | — | Yes |
| `IMAP_PORT` | IMAP port | `993` | No |
| `IMAP_USERNAME` | Email account username | — | Yes |
| `IMAP_PASSWORD` | Email account password (or app-specific) | — | Yes |
| `IMAP_USE_SSL` | Enable SSL/TLS | `true` | No |
| `IMAP_MAX_PARALLEL_CONNECTIONS` | Max parallel IMAP connections | `4` | No |
| `SYNC_INTERVAL` | Sync interval (Timespan format) | `00:05:00` | No |
| `DB_HOST` | PostgreSQL host (`postgres` in Docker Compose) | `localhost` | No |
| `DB_DATABASE` | Database name | `popfilenet` | No |
| `DB_USERNAME` | Database username | `postgres` | No |

### Start the Services

```bash
docker compose up -d
```

Access:
- Backend API at http://localhost:8000
- UI at http://localhost:8001
- PostgreSQL data persists in a named volume (`pgdata`)

## Server Deployment

PopfileNet is designed to run on Ubuntu 26.04 servers via Docker Compose.

### Prerequisites

- Ubuntu 26.04 (LTS)
- [Docker](https://www.docker.com/) and [docker compose plugin](https://docs.docker.com/compose/install/) installed
- Firewall configured to expose only necessary ports

### Generate Deployment Files

PopfileNet uses Aspire + Nuke to generate `docker-compose.yaml` from the application host:

```bash
dotnet run --project PopfileNet.AppHost --target GenerateRelease
```

This produces `compose/docker-compose.yaml` and `.env`. For production, edit `.env`:

1. Set a strong `POSTGRES_PASSWORD`
2. Disable DevMode (remove or set to `"false"`)
3. Update connection strings if needed

### Deploy

Copy the compose files and `.env` to the server:

```bash
scp -r compose/. .env user@server:/opt/popfilenet/
ssh user@server 'cd /opt/popfilenet && docker compose up -d'
```

## Configuration Reference

PopfileNet reads configuration from `appsettings.json` (and environment variables that override it). All settings are defined in the root JSON object:

### IMAP Settings (`ImapSettings`)

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `Server` | IMAP server address | — | Yes |
| `Port` | IMAP port | `993` | No |
| `Username` | Email account username | — | Yes |
| `Password` | Password or app-specific token | — | Yes |
| `UseSsl` | Enable SSL/TLS connection | `true` | No |
| `MaxParallelConnections` | Max parallel IMAP connections (1–20) | `4` | No |

### Sync Settings

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `SyncInterval` | How often to sync emails (Timespan format, e.g., `00:05:00`) | `00:05:00` | No |

### Bucket Configuration (`Buckets`)

Define classification categories. Each bucket has an `Id`, `Name`, and optional `Description`:

```json
"Buckets": [
  { "id": "work", "name": "Work", "description": "Work-related emails" },
  { "id": "personal", "name": "Personal", "description": "Personal emails" }
]
```

### Folder Mappings (`FolderMappings`)

Map IMAP folders to buckets so the classifier knows which bucket each folder belongs to:

```json
"FolderMappings": [
  { "folderName": "Inbox", "bucketId": null },
  { "folderName": "Work", "bucketId": "work" }
]
```

Folders without a mapping are treated as training data.

### Connection Strings (`ConnectionStrings:popfilenet`)

PostgreSQL connection string for EF Core migrations and queries:

```json
"ConnectionStrings": {
  "popfilenet": "Host=localhost;Database=popfilenet;Username=postgres;Password=<password>"
}
```

In Docker Compose, use `Host=postgres` (the service name).

### DevMode (`DevMode`)

Enable developer mode to access evaluation pages and debugging tools. **Should be disabled in production.**

```json
"DevMode": false
```

When enabled:
- Classifier evaluation page is accessible
- Debug endpoints are available
- Skips certain validation checks useful during development

## Container Registry (GHCR)

PopfileNet publishes container images to GitHub Container Registry (ghcr.io).

### Image Names

| Service | Image |
|---------|-------|
| Backend | `ghcr.io/blakharaz/popfilenet-backend:v0.3` |
| UI | `ghcr.io/blakharaz/popfilenet-ui:v0.3` |
| PostgreSQL | `docker.io/library/postgres:17-alpine` (official image) |

### Tagging Strategy

Images are tagged with semantic versioning tags: full version (`v0.3.0`), major.minor (`v0.3`), and major (`v0`). A short SHA hash is also pushed for exact references.

### CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on pushes to `main` and version tags:

1. Build and compile
2. Run unit, integration, and functional tests
3. SonarQube analysis (if token is configured)
4. Publish Docker images with GHA cache
5. Generate deployment compose files via Nuke target
6. Create a GitHub release with compose files attached

## Troubleshooting

### Database Migration Errors

If migrations fail on startup, check that:
- The PostgreSQL container is running and accessible
- The connection string points to the correct host (use `postgres` in Docker Compose)
- The database user has permission to create tables

The app automatically applies pending migrations on first start. On subsequent starts, it verifies the schema exists before migrating — if you see a legacy format error, delete the existing database and restart.

### IMAP Connection Issues

Verify your settings:
- Use `IMAP_USE_SSL=true` for most modern providers (Gmail, Outlook)
- For Gmail, use an [app-specific password](https://support.google.com/accounts/answer/185833), not your account password
- Check that the IMAP server allows external connections

### Port Conflicts

If ports 8000 or 8001 are already in use on your host, modify `docker-compose.yaml` to map to different host ports:

```yaml
ports:
  - "9000:80"   # Host port 9000 -> Container port 80
```
