# HashiCorp Vault to BeyondTrust PRA Sync Service

Intelligent credential synchronization service that syncs secrets from HashiCorp Vault to BeyondTrust PRA's internal vault with change detection.

**Latest Version: v2.2.0** - Smart diff-based sync with proper account group reassignment!

## Overview

This service provides seamless integration between HashiCorp Vault and BeyondTrust Privileged Remote Access (PRA) by automatically synchronizing credentials. When secrets are created or updated in Vault, they become available in PRA's vault for privileged session management.

### Architecture

```
┌──────────────────────┐         ┌───────────────────────┐
│  HashiCorp Vault     │         │  BeyondTrust PRA      │
│  (KV v2 Engine)      │         │  (Cloud Vault)        │
│                      │         │                       │
│  • myecm             │         │  • myecm ✓            │
│  • test-credential   │  ──────>│  • test-credential ✓  │
│  • test-credentials  │  sync   │  • test-credentials ✓ │
└──────────────────────┘         └───────────────────────┘
           │                                  │
           └──────────┬───────────────────────┘
                      │
              ┌───────▼────────┐
              │  Sync Service  │
              │  (Kubernetes)  │
              └────────────────┘
```

## Features

- **🔄 Smart Diff-Based Sync**: Scans both vaults and syncs only missing accounts
- **📊 Efficient Scanning**: Compares PRA and Vault every 5 minutes, creates only what's missing
- **⚡ Automated Sync**: Continuously monitors and syncs credentials every 5 minutes (configurable)
- **🔐 OAuth2 Authentication**: Secure authentication to BeyondTrust PRA
- **📝 Clear Logging**: Shows exactly what exists in each vault and what's being created
- **💾 Persistent State**: Maintains sync state across pod restarts
- **☸️ Kubernetes Native**: Deployed via Helm chart with best practices
- **🚀 CI/CD Ready**: Automated Docker image builds via GitHub Actions
- **✅ Production Ready**: Includes health checks, logging, and error handling

## Quick Start

### Prerequisites

- Kubernetes cluster (1.19+)
- Helm 3.x
- HashiCorp Vault instance (KV v2 secrets engine)
- BeyondTrust PRA instance with OAuth2 credentials

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/pdasilva11/ecm-k8s-plugin.git
   cd ecm-k8s-plugin
   ```

2. **Configure credentials**

   Edit `helm/ecm-plugin/values-sync.yaml` with your configuration:
   ```yaml
   app:
     ecm:
       sraSiteHostname: "your-pra-instance.beyondtrustcloud.com"
       sraClientId: "your-oauth-client-id"
     vault:
       baseUrl: "http://vault.vault.svc.cluster.local:8200"
       secretsEngine: "secret"

   secrets:
     vaultUsername: "your-vault-username"
     vaultPassword: "your-vault-password"
     sraClientSecret: "your-pra-client-secret"
   ```

3. **Deploy with Helm** (from GitHub Pages)
   ```bash
   # Add Helm repository
   helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
   helm repo update

   # Install v2.2.0 using --set flags
   helm install ecm-plugin ecm-plugin/ecm-plugin \
     --version 2.2.0 \
     --namespace vault-services \
     --create-namespace \
     --set replicaCount=0 \
     --set autoscaling.enabled=false \
     --set syncService.enabled=true \
     --set app.ecm.sraSiteHostname="your-pra-instance.beyondtrustcloud.com" \
     --set app.ecm.sraClientId="your-oauth-client-id" \
     --set app.ecm.accountGroup="your-account-group-name" \
     --set secrets.sraClientSecret="your-pra-client-secret" \
     --set app.vault.baseUrl="http://vault.vault.svc.cluster.local:8200" \
     --set secrets.vaultUsername="your-vault-username" \
     --set secrets.vaultPassword="your-vault-password" \
     --set app.vault.secretsEngine="secret"
   ```

   Or install using values file:
   ```bash
   helm install ecm-plugin ecm-plugin/ecm-plugin \
     --version 2.2.0 \
     -f values-sync.yaml \
     --namespace vault-services \
     --create-namespace
   ```

4. **Verify deployment**
   ```bash
   # Check pod status
   kubectl get pods -n vault-services -l component=sync

   # View logs
   kubectl logs -n vault-services -l component=sync --tail=50
   ```

## Configuration

### Helm `--set` Flags

| Flag | Description | Default |
|------|-------------|---------|
| `replicaCount` | Main ECM plugin replicas (set to `0` for sync-only) | `1` |
| `autoscaling.enabled` | Enable HPA for main plugin (set to `false` for sync-only) | `true` |
| `syncService.enabled` | Enable the sync service deployment | `false` |
| `app.ecm.sraSiteHostname` | PRA instance hostname | - |
| `app.ecm.sraClientId` | PRA OAuth2 client ID | - |
| `app.ecm.accountGroup` | PRA account group name or ID | `Default` |
| `secrets.sraClientSecret` | PRA OAuth2 client secret | - |
| `app.vault.baseUrl` | HashiCorp Vault API endpoint | `http://vault.vault.svc.cluster.local:8200` |
| `secrets.vaultUsername` | Vault userpass username | - |
| `secrets.vaultPassword` | Vault userpass password | - |
| `app.vault.secretsEngine` | KV v2 secrets engine path | `secret` |

### Environment Variables

The sync service accepts the following configuration (set automatically via Helm flags above):

| Variable | Description | Default |
|----------|-------------|---------|
| `PRA_HOSTNAME` | PRA instance hostname | - |
| `PRA_CLIENT_ID` | OAuth2 client ID | - |
| `PRA_CLIENT_SECRET` | OAuth2 client secret | - |
| `PRA_ACCOUNT_GROUP` | PRA account group name or ID | `Default` |
| `VAULT_URL` | Vault API endpoint | `http://vault.vault.svc.cluster.local:8200` |
| `VAULT_USERNAME` | Vault userpass username | `root` |
| `VAULT_PASSWORD` | Vault userpass password | - |
| `VAULT_SECRETS_ENGINE` | KV secrets engine path | `secret` |
| `SYNC_MODE` | Sync mode: `continuous` or `once` | `continuous` |
| `SYNC_INTERVAL_SECONDS` | Sync interval in continuous mode | `300` |
| `SYNC_STATE_FILE` | Path to state file for change tracking | `/tmp/sync_state.json` |

### Secret Format

The sync service automatically detects the secret type and creates the appropriate account in PRA:

**Username/Password Secrets** — If the secret contains both `username` and `password` fields, it creates a `username_password` account:

```bash
# Creates a username_password account in PRA
vault kv put secret/myapp-db username=dbuser password=secretpass123
```

**Opaque Token Secrets** — If the secret does NOT contain both `username` and `password`, it creates an `opaque_token` account. The service looks for a token value in the following fields (in order): `token`, `api_key`, `secret`, `key`, `access_token`, `api_token`. If none are found, it uses the first non-empty string value.

```bash
# Creates an opaque_token account in PRA
vault kv put secret/my-api-token token="ghp_1234567890abcdef"

# Also works with other field names
vault kv put secret/aws-key api_key="AKIAIOSFODNN7EXAMPLE"
vault kv put secret/service-secret secret="my-secret-value"
```

### Account Groups

The sync service can automatically assign created accounts to a specific PRA account group:

**Configuration:**
```bash
# Use account group by name (recommended)
export PRA_ACCOUNT_GROUP="Production Servers"

# Or use account group by ID
export PRA_ACCOUNT_GROUP="5"
```

**How it works:**
1. On first sync, the service queries PRA for available account groups: `GET /vault/account-group`
2. Finds the group matching the configured name (case-insensitive) or ID
3. When creating or updating an account, it assigns the account to the group via: `PATCH /vault/account/{id}` with `account_group_id`

**Example log output:**
```
Looking up account group by name: Production Servers
Found account group 'Production Servers' with ID: 5
✓ Successfully created PRA vault account: myapp-db (ID: 123)
Binding account 'myapp-db' (ID: 123) to group ID: 5
✓ Successfully bound account 'myapp-db' to group 5
```

If the configured group is not found, the service will log available groups and fail to create accounts.

## Sync Behavior

The sync service uses a **diff-based approach** to keep PRA vault in sync with HashiCorp Vault.

### How It Works

Every 5 minutes (configurable), the service:

1. **Scans PRA Vault**: Gets list of all existing accounts in PRA
2. **Scans HashiCorp Vault**: Gets list of all secrets in Vault
3. **Compares**: Finds secrets that exist in Vault but NOT in PRA
4. **Syncs Missing**: Creates only the accounts that are missing in PRA

### Benefits

- **No Duplicates**: Never creates accounts that already exist
- **Efficient**: Only creates what's missing, no unnecessary updates
- **Clear Visibility**: Logs show exactly what exists and what's being created
- **PRA as Source of Truth**: Existing PRA accounts are never modified or deleted
- **Simple Logic**: Easy to understand and troubleshoot

### Example Log Output

```
2026-02-04 16:30:00 - INFO - Scanning PRA vault for existing accounts...
2026-02-04 16:30:01 - INFO - Found 3 accounts in PRA vault: ['myecm', 'test-credential', 'test-credentials']
2026-02-04 16:30:01 - INFO - Scanning HashiCorp Vault for secrets...
2026-02-04 16:30:02 - INFO - Found 4 secrets in Vault: ['myecm', 'new-db-account', 'test-credential', 'test-credentials']
2026-02-04 16:30:02 - INFO - Found 1 accounts missing in PRA: ['new-db-account']
2026-02-04 16:30:02 - INFO - Creating missing account in PRA: new-db-account
2026-02-04 16:30:03 - INFO - ✓ Successfully created PRA vault account: new-db-account
2026-02-04 16:30:03 - INFO - Sync complete: 1 created, 0 failed
```

If all accounts are already synced:
```
2026-02-04 16:35:00 - INFO - Found 4 accounts in PRA vault: ['myecm', 'new-db-account', 'test-credential', 'test-credentials']
2026-02-04 16:35:01 - INFO - Found 4 secrets in Vault: ['myecm', 'new-db-account', 'test-credential', 'test-credentials']
2026-02-04 16:35:01 - INFO - ✓ All Vault secrets are already present in PRA - nothing to sync
```

## Docker Image

The sync service is automatically built and published to Docker Hub:

- **Repository**: `pdasilva1/vault-pra-sync`
- **Tags**: `latest`, `main`, `sha-<commit>`
- **Trigger**: Every push to `sync-service/` directory

### Manual Build

```bash
cd sync-service
docker build -t pdasilva1/vault-pra-sync:latest .
docker push pdasilva1/vault-pra-sync:latest
```

## Helm Chart

The Helm chart includes:

- **Deployment**: Sync service with configurable resources
- **Secrets**: Secure credential management
- **ServiceAccount**: RBAC for Kubernetes API access
- **Security**: Non-root container, read-only filesystem options

### Chart Structure

```
helm/ecm-plugin/
├── Chart.yaml                 # Chart metadata
├── values.yaml                # Default values
├── values-sync.yaml           # Sync-specific values
└── templates/
    ├── sync-deployment.yaml   # Sync service deployment
    ├── secrets.yaml           # Credentials secret
    └── serviceaccount.yaml    # RBAC service account
```

### Upgrading

**Upgrade using --set flags:**
```bash
helm upgrade ecm-plugin ecm-plugin/ecm-plugin \
  --version 2.2.0 \
  --namespace vault-services \
  --set replicaCount=0 \
  --set autoscaling.enabled=false \
  --set syncService.enabled=true \
  --set app.ecm.sraSiteHostname="your-pra-instance.beyondtrustcloud.com" \
  --set app.ecm.sraClientId="your-oauth-client-id" \
  --set app.ecm.accountGroup="your-account-group-name" \
  --set secrets.sraClientSecret="your-pra-client-secret" \
  --set app.vault.baseUrl="http://vault.vault.svc.cluster.local:8200" \
  --set secrets.vaultUsername="your-vault-username" \
  --set secrets.vaultPassword="your-vault-password" \
  --set app.vault.secretsEngine="secret"
```

**Or upgrade using values file:**
```bash
helm upgrade ecm-plugin ecm-plugin/ecm-plugin \
  --version 2.2.0 \
  -f values-sync.yaml \
  --namespace vault-services
```

## Monitoring

### Check Sync Status

```bash
# View recent sync operations
kubectl logs -n vault-services -l component=sync --tail=100

# Follow logs in real-time
kubectl logs -n vault-services -l component=sync -f
```

### Successful Sync Output

```
2026-02-03 21:08:56 - INFO - Starting Vault → PRA sync
2026-02-03 21:08:56 - INFO - Successfully authenticated to Vault
2026-02-03 21:08:56 - INFO - Successfully authenticated to PRA
2026-02-03 21:08:56 - INFO - Found 3 secrets in Vault: ['myecm', 'test-credential', 'test-credentials']
2026-02-03 21:08:56 - INFO - Successfully created PRA vault account: myecm
2026-02-03 21:08:57 - INFO - Successfully created PRA vault account: test-credential
2026-02-03 21:08:57 - INFO - Successfully created PRA vault account: test-credentials
2026-02-03 21:08:57 - INFO - Sync complete: 3 success, 0 failed
```

## Troubleshooting

### Common Issues

**Issue**: DNS resolution fails for PRA hostname
```
ERROR - Failed to authenticate to PRA: Name or service not known
```
**Solution**: Verify the PRA hostname is correct and accessible from the pod

**Issue**: OAuth authentication fails
```
ERROR - Failed to authenticate to PRA: 401 Unauthorized
```
**Solution**: Verify PRA_CLIENT_ID and PRA_CLIENT_SECRET are correct

**Issue**: Vault authentication fails
```
ERROR - Failed to authenticate to Vault: 403 permission denied
```
**Solution**: Verify VAULT_USERNAME and VAULT_PASSWORD, and that userpass auth is enabled

**Issue**: Secrets missing username or password
```
WARNING - Skipping mysecret: missing username or password
```
**Solution**: Ensure secrets contain both `username` and `password` fields

### Manual Testing

Test the sync service outside of Kubernetes:

```bash
cd sync-service
export PRA_HOSTNAME="your-pra-instance.beyondtrustcloud.com"
export PRA_CLIENT_ID="your-client-id"
export PRA_CLIENT_SECRET="your-client-secret"
export VAULT_URL="http://your-vault:8200"
export VAULT_USERNAME="root"
export VAULT_PASSWORD="your-password"
export SYNC_MODE="once"

python3 vault_pra_sync.py
```

## API Integration

The sync service uses BeyondTrust PRA Configuration API v1:

- **Endpoint**: `POST /api/config/v1/vault/account`
- **Authentication**: OAuth2 Bearer token
- **Schema**: `VaultUsernamePasswordAccount`

### API Payload

```json
{
  "type": "username_password",
  "name": "account-name",
  "username": "username",
  "password": "password",
  "description": "Synced from HashiCorp Vault",
  "account_group_id": 1
}
```

## GitHub Actions

The repository includes automated CI/CD:

### Workflow: Sync Service Build

- **Trigger**: Push to `sync-service/` directory or manual dispatch
- **Actions**:
  1. Checkout code
  2. Set up Docker Buildx
  3. Login to Docker Hub
  4. Extract metadata (tags, labels)
  5. Build and push image
  6. Cache layers for faster builds

**Secrets Required**:
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password or PAT

## Development

### Project Structure

```
.
├── sync-service/           # Python sync service
│   ├── vault_pra_sync.py  # Main service code
│   ├── Dockerfile         # Container image
│   └── README.md          # Service documentation
├── helm/                  # Helm charts
│   └── ecm-plugin/
│       ├── Chart.yaml
│       ├── values.yaml
│       ├── values-sync.yaml
│       └── templates/
├── docs/                  # GitHub Pages documentation
│   ├── index.yaml        # Helm repository index
│   └── README.md
├── .github/
│   └── workflows/
│       └── sync-service-build.yml
└── README.md
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is provided as-is for integration between HashiCorp Vault and BeyondTrust PRA.

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Review logs using `kubectl logs`
- Check PRA API documentation

## Links

- **Docker Hub**: https://hub.docker.com/r/pdasilva1/vault-pra-sync
- **GitHub Repository**: https://github.com/pdasilva11/ecm-k8s-plugin
- **Helm Repository**: https://pdasilva11.github.io/ecm-k8s-plugin/
