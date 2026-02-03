# Helm Chart Quick Start

Get the ECM Plugin deployed in minutes!

## 1. Prerequisites Check

```bash
# Verify kubectl is configured
kubectl cluster-info

# Verify Helm is installed
helm version

# Verify Vault is accessible (update URL as needed)
curl http://vault.vault.svc.cluster.local:8200/v1/sys/health
```

## 2. Install (Development)

```bash
cd helm

helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f ecm-plugin/values-development.yaml
```

## 3. Install (Production)

```bash
cd helm

# Create a secure values file
cat > prod-secrets.yaml <<EOF
secrets:
  vaultUsername: "your-vault-username"
  vaultPassword: "your-vault-password"

app:
  vault:
    baseUrl: "https://vault.yourcompany.com:8200"

ingress:
  hosts:
    - host: vault-api.yourcompany.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: vault-api-tls
      hosts:
        - vault-api.yourcompany.com
EOF

# Install with production values
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f ecm-plugin/values-production.yaml \
  -f prod-secrets.yaml

# IMPORTANT: Don't commit prod-secrets.yaml to Git!
echo "prod-secrets.yaml" >> .gitignore
```

## 4. Verify Deployment

```bash
# Check release
helm status ecm-plugin -n vault-services

# Check pods
kubectl get pods -n vault-services

# Check all resources
kubectl get all -n vault-services
```

## 5. Test the Service

```bash
# Port forward
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80

# In another terminal, test health endpoint
curl http://localhost:8080/api/credentials/health
```

## 6. View Logs

```bash
kubectl logs -n vault-services -l app=vault-credential-service --tail=50 -f
```

## 7. Upgrade

```bash
# Upgrade with new values
helm upgrade ecm-plugin ./ecm-plugin \
  -n vault-services \
  -f ecm-plugin/values-production.yaml \
  -f prod-secrets.yaml

# Or update just the image tag
helm upgrade ecm-plugin ./ecm-plugin \
  -n vault-services \
  --reuse-values \
  --set image.tag=v1.1.0
```

## 8. Uninstall

```bash
helm uninstall ecm-plugin -n vault-services
```

## Common Commands

```bash
# List releases
helm list -n vault-services

# Get values
helm get values ecm-plugin -n vault-services

# Get manifest
helm get manifest ecm-plugin -n vault-services

# View history
helm history ecm-plugin -n vault-services

# Rollback
helm rollback ecm-plugin -n vault-services

# Dry run (test without installing)
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  -f ecm-plugin/values-production.yaml \
  --dry-run --debug
```

## Key Configuration Options

### Scale Replicas
```bash
--set replicaCount=5
```

### Change Image Tag
```bash
--set image.tag=v1.2.0
```

### Disable Autoscaling
```bash
--set autoscaling.enabled=false
```

### Change Resource Limits
```bash
--set resources.limits.cpu=2000m \
--set resources.limits.memory=2Gi
```

### Disable Ingress
```bash
--set ingress.enabled=false
```

## Troubleshooting Quick Reference

### Pod Not Starting
```bash
kubectl describe pod -n vault-services <pod-name>
kubectl logs -n vault-services <pod-name>
```

### ImagePullBackOff
```bash
# Check image name and tag
kubectl get deployment -n vault-services vault-credential-service -o yaml | grep image:
```

### Health Check Failing
```bash
# Check probe configuration
kubectl describe deployment -n vault-services vault-credential-service

# Test health endpoint
kubectl port-forward -n vault-services <pod-name> 8080:8080
curl http://localhost:8080/api/credentials/health
```

### Can't Connect to Vault
```bash
# Test from pod
kubectl exec -n vault-services <pod-name> -- curl -v http://vault.vault.svc.cluster.local:8200/v1/sys/health

# Check vault configuration
kubectl get deployment -n vault-services vault-credential-service -o yaml | grep -A 5 VaultConfig
```

## Need More Help?

- Full installation guide: `INSTALLATION.md`
- Chart documentation: `ecm-plugin/README.md`
- Kubernetes deployment guide: `../K8S_DEPLOYMENT_GUIDE.md`
- GitHub Issues: https://github.com/pdasilva11/ecm-k8s-plugin/issues
