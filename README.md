# HashiCorp Vault to BeyondTrust PRA Sync Service

Intelligent credential synchronization service that syncs secrets from HashiCorp Vault to BeyondTrust PRA's internal vault with change detection.

**Latest Version: v2.1.0** - Now with intelligent change detection using Vault metadata API!

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

- **🔄 Intelligent Change Detection**: Uses Vault metadata API to detect secret version changes
- **📊 Version Tracking**: Only syncs when secrets are modified, reducing API calls by ~95%
- **⚡ Automated Sync**: Continuously syncs credentials every 5 minutes (configurable)
- **🔐 OAuth2 Authentication**: Secure authentication to BeyondTrust PRA
- **📝 Enhanced Logging**: Clear visibility into new, changed, and unchanged secrets
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

   # Install v2.1.0 using --set flags
   helm install vault-sync ecm-plugin/ecm-plugin \
     --version 2.1.0 \
     --namespace vault-services \
     --create-namespace \
     --set syncService.enabled=true \
     --set app.ecm.sraSiteHostname="your-pra-instance.beyondtrustcloud.com" \
     --set app.ecm.sraClientId="your-oauth-client-id" \
     --set secrets.sraClientSecret="your-pra-client-secret" \
     --set app.vault.baseUrl="http://vault.vault.svc.cluster.local:8200" \
     --set secrets.vaultUsername="your-vault-username" \
     --set secrets.vaultPassword="your-vault-password" \
     --set app.vault.secretsEngine="secret"
   ```

   Or install using values file:
   ```bash
   helm install vault-sync ecm-plugin/ecm-plugin \
     --version 2.1.0 \
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

### Environment Variables

The sync service accepts the following configuration:

| Variable | Description | Default |
|----------|-------------|---------|
| `PRA_HOSTNAME` | PRA instance hostname | - |
| `PRA_CLIENT_ID` | OAuth2 client ID | - |
| `PRA_CLIENT_SECRET` | OAuth2 client secret | - |
| `VAULT_URL` | Vault API endpoint | `http://vault.vault.svc.cluster.local:8200` |
| `VAULT_USERNAME` | Vault userpass username | `root` |
| `VAULT_PASSWORD` | Vault userpass password | - |
| `VAULT_SECRETS_ENGINE` | KV secrets engine path | `secret` |
| `SYNC_MODE` | Sync mode: `continuous` or `once` | `continuous` |
| `SYNC_INTERVAL_SECONDS` | Sync interval in continuous mode | `300` |
| `SYNC_STATE_FILE` | Path to state file for change tracking | `/tmp/sync_state.json` |

### Secret Format

Secrets in HashiCorp Vault must contain `username` and `password` fields:

```bash
# Example: Create a secret in Vault
vault kv put secret/myapp-db username=dbuser password=secretpass123
```

This will create a vault account in PRA named `myapp-db` with the specified credentials.

## Change Detection

**New in v2.1.0**: The sync service intelligently detects changes using Vault's metadata API.

### How It Works

1. **Metadata Check**: Uses `GET /v1/{mount}/metadata/{path}` to retrieve secret version
2. **Version Comparison**: Compares current version with last synced version
3. **Conditional Sync**: Only syncs if version has changed
4. **State Persistence**: Saves sync state to `/tmp/sync_state.json`

### Benefits

- **Reduced API Calls**: ~95% reduction after initial sync
- **Faster Syncs**: Skips unchanged secrets
- **Better Visibility**: Logs show new/changed/unchanged status
- **Persistent State**: Survives pod restarts

### Example Log Output

```
2026-02-04 01:25:10 - INFO - Checking 4 secrets for changes...
2026-02-04 01:25:10 - INFO -   → New secret detected: myecm
2026-02-04 01:25:10 - INFO -   → Version changed: test-credential (v1 → v2)
2026-02-04 01:25:11 - DEBUG -   → No changes: test-credentials (v3)
2026-02-04 01:25:11 - INFO - Sync complete: 2 synced, 1 unchanged, 0 failed
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
# Upgrade to latest version
helm upgrade vault-sync ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --reuse-values \
  --set syncService.image.tag=latest

# Or upgrade with new configuration
helm upgrade vault-sync ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --set syncService.enabled=true \
  --set app.ecm.sraSiteHostname="your-pra-instance.beyondtrustcloud.com" \
  --set app.ecm.sraClientId="your-oauth-client-id" \
  --set secrets.sraClientSecret="your-pra-client-secret" \
  --set app.vault.baseUrl="http://vault.vault.svc.cluster.local:8200" \
  --set secrets.vaultUsername="your-vault-username" \
  --set secrets.vaultPassword="your-vault-password"
```

**Or upgrade using values file:**
```bash
helm upgrade vault-sync ecm-plugin/ecm-plugin \
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
