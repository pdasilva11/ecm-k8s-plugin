# Helm Chart Quick Start

Get the Vault-to-PRA Sync Service deployed in minutes!

## 1. Prerequisites

```bash
# Verify kubectl is configured
kubectl cluster-info

# Verify Helm is installed
helm version
```

## 2. Add Helm Repository

```bash
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
helm repo update
```

## 3. Install Sync Service

```bash
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

## 4. Verify Deployment

```bash
# Check pods — only the sync pod should be running
kubectl get pods -n vault-services

# Check deployments
kubectl get deploy -n vault-services
```

Expected output:
```
NAME                            READY   UP-TO-DATE   AVAILABLE
vault-credential-service        0/0     0            0           # Disabled
vault-credential-service-sync   1/1     1            1           # Running
```

## 5. View Logs

```bash
# Follow sync logs
kubectl logs -n vault-services -l component=sync -f

# View last 50 lines
kubectl logs -n vault-services -l component=sync --tail=50
```

## 6. Upgrade

```bash
helm repo update ecm-plugin

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

### Restart sync pod (after new Docker image build)
```bash
kubectl rollout restart deployment vault-credential-service-sync -n vault-services
```

## 7. Uninstall

```bash
helm uninstall ecm-plugin -n vault-services
```

## Common Commands

```bash
# List releases
helm list -n vault-services

# Get deployed values
helm get values ecm-plugin -n vault-services

# View history
helm history ecm-plugin -n vault-services

# Rollback
helm rollback ecm-plugin -n vault-services

# Dry run (test without installing)
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --version 2.2.0 \
  --namespace vault-services \
  --dry-run --debug
```

## Required `--set` Flags

| Flag | Description |
|------|-------------|
| `replicaCount=0` | Disable main ECM plugin |
| `autoscaling.enabled=false` | Prevent HPA from overriding replicaCount |
| `syncService.enabled=true` | Enable the sync service |
| `app.ecm.sraSiteHostname` | PRA instance hostname |
| `app.ecm.sraClientId` | PRA OAuth2 client ID |
| `app.ecm.accountGroup` | PRA account group name or ID |
| `secrets.sraClientSecret` | PRA OAuth2 client secret |
| `app.vault.baseUrl` | HashiCorp Vault API endpoint |
| `secrets.vaultUsername` | Vault username |
| `secrets.vaultPassword` | Vault password |
| `app.vault.secretsEngine` | KV v2 secrets engine path |

## Troubleshooting

### Sync pod not starting
```bash
kubectl describe pod -n vault-services -l component=sync
kubectl logs -n vault-services -l component=sync
```

### Account group not found
Check the PRA account group name matches exactly (case-insensitive). The sync logs will show available groups.

### Passwords not updating in PRA
- Ensure the account is not checked out in PRA (returns 400)
- The sync service detects changes via Vault secret version numbers

## Need More Help?

- Full installation guide: [INSTALLATION.md](INSTALLATION.md)
- Chart documentation: [ecm-plugin/README.md](ecm-plugin/README.md)
- GitHub Issues: https://github.com/pdasilva11/ecm-k8s-plugin/issues
