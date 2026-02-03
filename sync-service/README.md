# Vault-PRA Sync Service

This service synchronizes credentials from HashiCorp Vault to BeyondTrust PRA's internal vault.

## Overview

The sync service:
- Authenticates to HashiCorp Vault using userpass authentication
- Authenticates to PRA using OAuth2 client credentials
- Lists all secrets from Vault's KV v2 secrets engine
- Creates or updates corresponding accounts in PRA's vault

## Configuration

The service is configured via environment variables:

### PRA Configuration
- `PRA_HOSTNAME`: PRA instance hostname (e.g., `pauldasilvapra.beyondtrustcloud.com`)
- `PRA_CLIENT_ID`: OAuth client ID
- `PRA_CLIENT_SECRET`: OAuth client secret

### Vault Configuration
- `VAULT_URL`: Vault API endpoint (e.g., `http://vault.vault.svc.cluster.local:8200`)
- `VAULT_USERNAME`: Vault username for userpass authentication
- `VAULT_PASSWORD`: Vault password
- `VAULT_SECRETS_ENGINE`: KV secrets engine path (default: `secret`)

### Sync Configuration
- `SYNC_MODE`: `continuous` or `once` (default: `continuous`)
- `SYNC_INTERVAL_SECONDS`: Interval between sync cycles in continuous mode (default: `300`)

## Docker Image

The service is available as a Docker image:
```
pdasilva1/vault-pra-sync:latest
```

Built automatically via GitHub Actions on every push to `sync-service/`.

## Deployment

Deploy to Kubernetes using the Helm chart with sync service enabled:

```bash
helm upgrade --install ecm-sync ./helm/ecm-plugin \
  -f ./helm/ecm-plugin/values-sync.yaml \
  --namespace vault-services
```

## API Integration

Uses BeyondTrust PRA Configuration API:
- Endpoint: `POST /api/config/vault/account`
- Schema: `VaultUsernamePasswordAccount`
- Authentication: OAuth2 Bearer token

## Architecture

```
┌──────────────────┐         ┌──────────────────┐
│  HashiCorp Vault │         │  BeyondTrust PRA │
│                  │         │                  │
│  KV v2 Secrets   │         │  Internal Vault  │
└────────┬─────────┘         └────────▲─────────┘
         │                            │
         │  List/Get Secrets          │  Create/Update Accounts
         │                            │
         └──────────┬─────────────────┘
                    │
              ┌─────▼──────┐
              │ Sync Service│
              │ (Python)    │
              └─────────────┘
```

## Logging

The service logs to stdout with structured logging:
- INFO: Sync operations, authentication status
- WARNING: Missing credentials, skipped secrets
- ERROR: API failures, authentication errors
