# HashiCorp Vault to BeyondTrust PRA Sync Service - Helm Repository

This directory hosts the Helm chart repository for the Vault-to-PRA sync service via GitHub Pages.

## Using This Helm Repository

### Add the Repository

```bash
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
helm repo update
```

### Search for Charts

```bash
helm search repo ecm-plugin
```

### Install the Sync Service

**Install using --set flags:**
```bash
# Install with sync service enabled
helm install vault-sync ecm-plugin/ecm-plugin \
  --version 2.1.0 \
  --namespace vault-services \
  --create-namespace \
  --set syncService.enabled=true \
  --set app.ecm.sraSiteHostname="your-pra-instance.beyondtrustcloud.com" \
  --set app.ecm.sraClientId="your-oauth-client-id" \
  --set app.ecm.accountGroup="Default" \
  --set secrets.sraClientSecret="your-pra-client-secret" \
  --set app.vault.baseUrl="http://vault.vault.svc.cluster.local:8200" \
  --set secrets.vaultUsername="your-vault-username" \
  --set secrets.vaultPassword="your-vault-password" \
  --set app.vault.secretsEngine="secret"
```

**Or install using values file:**
```bash
helm install vault-sync ecm-plugin/ecm-plugin \
  --version 2.1.0 \
  -f https://raw.githubusercontent.com/pdasilva11/ecm-k8s-plugin/main/helm/ecm-plugin/values-sync.yaml \
  --namespace vault-services \
  --create-namespace
```

## Available Charts

### ecm-plugin v2.1.0 (Latest) ⭐
**Vault-to-PRA Sync Service** - Smart diff-based credential synchronization

- **Version**: 2.1.0
- **App Version**: 2.1.0
- **Chart URL**: [ecm-plugin-2.1.0.tgz](https://pdasilva11.github.io/ecm-k8s-plugin/ecm-plugin-2.1.0.tgz)
- **Sync Behavior**:
  - 🔄 **Diff-based sync** - scans both vaults and creates only missing accounts
  - 📊 **Efficient** - no duplicate creation, no unnecessary updates
  - 🚀 **Smart** - PRA vault is source of truth for existing accounts
  - 💾 **Persistent state** across pod restarts
  - 📝 **Clear logging** - shows what exists vs what's being created
- **Core Features**:
  - Python-based sync service
  - OAuth2 authentication to PRA
  - Continuous or one-time sync modes
  - Automatic account creation for missing credentials
  - Kubernetes-native deployment
  - 5-minute sync interval (configurable)

### ecm-plugin v2.0.0
Initial sync service release

- **Version**: 2.0.0
- **Chart URL**: [ecm-plugin-2.0.0.tgz](https://pdasilva11.github.io/ecm-k8s-plugin/ecm-plugin-2.0.0.tgz)

### ecm-plugin v1.1.0
Legacy REST API version (deprecated)

- **Version**: 1.1.0
- **Chart URL**: [ecm-plugin-1.1.0.tgz](https://pdasilva11.github.io/ecm-k8s-plugin/ecm-plugin-1.1.0.tgz)

## Repository Index

The repository index is maintained at: [index.yaml](https://pdasilva11.github.io/ecm-k8s-plugin/index.yaml)

## Quick Start Example

### Option 1: Using --set flags (Recommended)

1. **Add the Helm repository**
   ```bash
   helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
   helm repo update
   ```

2. **Install the chart with your credentials**
   ```bash
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

3. **Verify the deployment**
   ```bash
   kubectl get pods -n vault-services -l component=sync
   kubectl logs -n vault-services -l component=sync --tail=50
   ```

### Option 2: Using values file

1. **Add the Helm repository**
   ```bash
   helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
   helm repo update
   ```

2. **Download and edit the values file**
   ```bash
   curl -O https://raw.githubusercontent.com/pdasilva11/ecm-k8s-plugin/main/helm/ecm-plugin/values-sync.yaml
   # Edit values-sync.yaml with your credentials
   ```

3. **Install the chart**
   ```bash
   helm install vault-sync ecm-plugin/ecm-plugin \
     --version 2.1.0 \
     -f values-sync.yaml \
     --namespace vault-services \
     --create-namespace
   ```

4. **Verify the deployment**
   ```bash
   kubectl get pods -n vault-services -l component=sync
   kubectl logs -n vault-services -l component=sync --tail=50
   ```

## Configuration

### Required Settings

| Parameter | Description |
|-----------|-------------|
| `app.ecm.sraSiteHostname` | PRA instance hostname |
| `app.ecm.sraClientId` | OAuth2 client ID |
| `secrets.sraClientSecret` | OAuth2 client secret |
| `app.vault.baseUrl` | Vault API URL |
| `secrets.vaultUsername` | Vault username |
| `secrets.vaultPassword` | Vault password |

### Sync Service Settings

| Parameter | Description | Default |
|-----------|-------------|---------|
| `syncService.enabled` | Enable sync service | `false` |
| `syncService.mode` | Sync mode: `continuous` or `once` | `continuous` |
| `syncService.intervalSeconds` | Sync interval (seconds) | `300` (5 minutes) |
| `syncService.image.repository` | Docker image repository | `pdasilva1/vault-pra-sync` |
| `syncService.image.tag` | Docker image tag | `latest` |

## What's New in v2.1.0

### Smart Diff-Based Sync
The sync service uses an intelligent diff approach:

**How it works:**
1. Scans PRA vault for existing accounts
2. Scans HashiCorp Vault for secrets
3. Compares and finds accounts missing in PRA
4. Creates only the missing accounts

**Benefits:**
- No duplicate account creation
- Only syncs what's actually missing
- PRA vault remains source of truth for existing accounts
- Clear visibility into what exists vs what's being created
- Efficient - minimal API calls

**Example Log Output:**
```
Scanning PRA vault for existing accounts...
Found 3 accounts in PRA vault: ['myecm', 'test-credential', 'test-credentials']
Scanning HashiCorp Vault for secrets...
Found 4 secrets in Vault: ['myecm', 'new-db-account', 'test-credential', 'test-credentials']
Found 1 accounts missing in PRA: ['new-db-account']
Creating missing account in PRA: new-db-account
✓ Successfully created PRA vault account: new-db-account
Sync complete: 1 created, 0 failed
```

## Documentation

For detailed documentation, see:
- [Main Repository](https://github.com/pdasilva11/ecm-k8s-plugin)
- [README](https://github.com/pdasilva11/ecm-k8s-plugin#readme)
- [Quick Start Guide](https://github.com/pdasilva11/ecm-k8s-plugin/blob/main/helm/QUICKSTART.md)
- [Sync Service Documentation](https://github.com/pdasilva11/ecm-k8s-plugin/tree/main/sync-service)

## Monitoring

Check sync status:
```bash
# View logs
kubectl logs -n vault-services -l component=sync --tail=50

# Follow logs
kubectl logs -n vault-services -l component=sync -f
```

## Troubleshooting

Common issues and solutions are documented in the [main README](https://github.com/pdasilva11/ecm-k8s-plugin#troubleshooting).

## Chart Updates

**Upgrade using --set flags:**
```bash
# Update repository and upgrade to latest version
helm repo update

# Upgrade with reuse of existing values
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
  --set app.ecm.accountGroup="Default" \
  --set secrets.sraClientSecret="your-pra-client-secret" \
  --set app.vault.baseUrl="http://vault.vault.svc.cluster.local:8200" \
  --set secrets.vaultUsername="your-vault-username" \
  --set secrets.vaultPassword="your-vault-password"
```

**Or upgrade using values file:**
```bash
helm repo update
helm upgrade vault-sync ecm-plugin/ecm-plugin \
  -f values-sync.yaml \
  --namespace vault-services
```

---

**Repository URL**: https://pdasilva11.github.io/ecm-k8s-plugin/
**Source Code**: https://github.com/pdasilva11/ecm-k8s-plugin
**Docker Hub**: https://hub.docker.com/r/pdasilva1/vault-pra-sync
