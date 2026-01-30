# Kubernetes Deployment Guide

## Prerequisites

1. **Kubernetes Cluster** (1.24+)
2. **kubectl** configured to access your cluster
3. **HashiCorp Vault** running and accessible from K8s
4. **Ingress Controller** (NGINX recommended)

## Files Overview

| File | Purpose |
|------|---------|
| `01-namespace.yaml` | Creates `vault-services` namespace |
| `02-configmap.yaml` | Application configuration |
| `03-secret.yaml` | Vault credentials (UPDATE THESE!) |
| `04-deployment.yaml` | Deployment with 2 replicas |
| `05-service.yaml` | ClusterIP service |
| `06-ingress.yaml` | External access via HTTPS |
| `07-rbac.yaml` | ServiceAccount and RBAC rules |
| `08-hpa.yaml` | Auto-scaling (2-5 replicas) |
| `09-pdb.yaml` | Pod Disruption Budget |
| `10-networkpolicy.yaml` | Network security policies |

## Quick Start

### 1. Update Vault Credentials

**Edit `03-secret.yaml`:**
```yaml
stringData:
  vault-username: "your-vault-user"
  vault-password: "your-vault-password"
```

### 2. Update Vault Connection

**Edit `04-deployment.yaml` - VaultConfig section:**
```yaml
- name: VaultConfig__BaseUrl
  value: "http://vault.vault.svc.cluster.local:8200"  # Update this
```

### 3. Deploy All Resources

**Using kubectl:**
```bash
# Create namespace
kubectl apply -f k8s/01-namespace.yaml

# Deploy everything else
kubectl apply -f k8s/02-configmap.yaml
kubectl apply -f k8s/03-secret.yaml
kubectl apply -f k8s/07-rbac.yaml
kubectl apply -f k8s/04-deployment.yaml
kubectl apply -f k8s/05-service.yaml
kubectl apply -f k8s/08-hpa.yaml
kubectl apply -f k8s/09-pdb.yaml
kubectl apply -f k8s/10-networkpolicy.yaml
kubectl apply -f k8s/06-ingress.yaml
```

**Or all at once:**
```bash
kubectl apply -f k8s/
```

### 4. Verify Deployment

```bash
# Check namespace
kubectl get ns | grep vault-services

# Check pods
kubectl get pods -n vault-services

# Check services
kubectl get svc -n vault-services

# Check deployment status
kubectl describe deployment vault-credential-service -n vault-services

# View logs
kubectl logs -f deployment/vault-credential-service -n vault-services
```

## Configuration

### Environment Variables

Edit `04-deployment.yaml` to customize:

```yaml
env:
- name: VaultConfig__BaseUrl
  value: "http://vault.vault.svc.cluster.local:8200"
- name: VaultConfig__SecretsEngine
  value: "secret"  # Change if using different engine
- name: Logging__LogLevel__Default
  value: "Information"  # Debug, Information, Warning, Error
```

### Ingress

**Update hostname in `06-ingress.yaml`:**
```yaml
- host: vault-credentials.example.com  # Change this
```

### Scaling

**Edit `08-hpa.yaml`:**
```yaml
minReplicas: 2
maxReplicas: 5  # Adjust as needed
metrics:
- resource:
    name: cpu
    target:
      averageUtilization: 70  # Adjust threshold
```

## Testing

### Port Forward to Service

```bash
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80
```

### Test Endpoints

```bash
# Health check
curl http://localhost:8080/api/credentials/health

# Get secret
curl http://localhost:8080/api/credentials/secret/db/prod/admin

# Service info
curl http://localhost:8080/api/credentials
```

### Check Pod Logs

```bash
# Get pod name
POD=$(kubectl get pods -n vault-services -l app=vault-credential-service -o jsonpath='{.items[0].metadata.name}')

# View logs
kubectl logs $POD -n vault-services

# Stream logs
kubectl logs -f $POD -n vault-services

# View previous logs if crashed
kubectl logs $POD -n vault-services --previous
```

### Describe Pod for Troubleshooting

```bash
kubectl describe pod $POD -n vault-services
```

## Monitoring

### Check HPA Status

```bash
kubectl get hpa -n vault-services
kubectl describe hpa vault-credential-service -n vault-services
```

### View Resource Usage

```bash
kubectl top pods -n vault-services
kubectl top nodes
```

## Updating Deployment

### Update Image

```bash
kubectl set image deployment/vault-credential-service \
  vault-service=pdasilva1/ecm-k8s-plugin:v1.1 \
  -n vault-services
```

### Update ConfigMap

```bash
kubectl apply -f k8s/02-configmap.yaml
kubectl rollout restart deployment/vault-credential-service -n vault-services
```

### Update Secret

```bash
kubectl apply -f k8s/03-secret.yaml
kubectl rollout restart deployment/vault-credential-service -n vault-services
```

## Cleanup

### Remove All Resources

```bash
kubectl delete namespace vault-services
```

### Remove Specific Resources

```bash
kubectl delete deployment vault-credential-service -n vault-services
kubectl delete service vault-credential-service -n vault-services
kubectl delete ingress vault-credential-service -n vault-services
```

## Security Best Practices

✅ **Implemented:**
- Non-root user (uid: 1000)
- Read-only ConfigMap mounts
- Network policies for ingress/egress
- RBAC with minimal permissions
- Secret management for credentials
- Pod Disruption Budget
- Resource limits

✅ **Recommended:**
- Use Sealed Secrets or External Secrets operator for secret management
- Enable Pod Security Policy (if available)
- Use TLS/HTTPS for Ingress
- Regular security scanning of images
- RBAC for namespace access control
- Audit logging enabled

## Troubleshooting

### Pod not starting

```bash
kubectl describe pod $POD -n vault-services
kubectl logs $POD -n vault-services --previous
```

### Can't reach service

```bash
# Test from another pod
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://vault-credential-service.vault-services/api/credentials/health
```

### Vault connection issues

```bash
# Check Vault connectivity
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://vault.vault.svc.cluster.local:8200/v1/sys/health
```

## Support

For issues or questions:
1. Check logs: `kubectl logs -f deployment/vault-credential-service -n vault-services`
2. Describe deployment: `kubectl describe deployment vault-credential-service -n vault-services`
3. Review configuration: `kubectl get configmap vault-service-config -n vault-services -o yaml`
