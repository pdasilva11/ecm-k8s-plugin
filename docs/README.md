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

```bash
# Install with sync service enabled
helm install vault-sync ecm-plugin/ecm-plugin \
  -f https://raw.githubusercontent.com/pdasilva11/ecm-k8s-plugin/main/helm/ecm-plugin/values-sync.yaml \
  --namespace vault-services \
  --create-namespace \
  --set app.ecm.sraSiteHostname=your-pra-instance.beyondtrustcloud.com \
  --set app.ecm.sraClientId=your-oauth-client-id \
  --set secrets.sraClientSecret=your-pra-client-secret \
  --set app.vault.baseUrl=http://vault.vault.svc.cluster.local:8200 \
  --set secrets.vaultUsername=your-vault-username \
  --set secrets.vaultPassword=your-vault-password
```

## Available Charts

### ecm-plugin v2.1.0 (Latest) ⭐
**Vault-to-PRA Sync Service** - Intelligent credential synchronization with change detection

- **Version**: 2.1.0
- **App Version**: 2.1.0
- **Chart URL**: [ecm-plugin-2.1.0.tgz](https://pdasilva11.github.io/ecm-k8s-plugin/ecm-plugin-2.1.0.tgz)
- **New in v2.1.0**:
  - ✨ **Change detection** using Vault metadata API
  - 📊 **Version tracking** - only syncs when secrets change
  - 🚀 **~95% reduction** in API calls after initial sync
  - 💾 **Persistent state** across pod restarts
  - 📝 **Enhanced logging** - shows new/changed/unchanged secrets
- **Core Features**:
  - Python-based sync service
  - OAuth2 authentication to PRA
  - Continuous or one-time sync modes
  - Automatic account creation/updates in PRA vault
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

1. **Add the Helm repository**
   ```bash
   helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
   ```

2. **Download the values file**
   ```bash
   curl -O https://raw.githubusercontent.com/pdasilva11/ecm-k8s-plugin/main/helm/ecm-plugin/values-sync.yaml
   ```

3. **Edit the values file** with your credentials

4. **Install the chart**
   ```bash
   helm install vault-sync ecm-plugin/ecm-plugin \
     -f values-sync.yaml \
     --namespace vault-services \
     --create-namespace
   ```

5. **Verify the deployment**
   ```bash
   kubectl get pods -n vault-services -l component=sync
   kubectl logs -n vault-services -l component=sync
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

### Intelligent Change Detection
The sync service now uses Vault's metadata API to detect changes:

```
GET /v1/{mount}/metadata/{path}
```

**Benefits:**
- Only syncs secrets when version changes
- Reduces PRA API calls by ~95% after initial sync
- Faster sync cycles (skips unchanged secrets)
- State persisted in `/tmp/sync_state.json`

**Example Log Output:**
```
Checking 4 secrets for changes...
  → New secret detected: myecm
  → Version changed: test-credential (v1 → v2)
  → No changes: test-credentials (v1)
Sync complete: 2 synced, 1 unchanged, 0 failed
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

To update to the latest version:
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
