# Helm Chart Installation Guide

This guide provides detailed instructions for installing the ECM Plugin using Helm.

## Prerequisites

Before installing the Helm chart, ensure you have:

1. **Kubernetes Cluster** (v1.19+)
   ```bash
   kubectl version --short
   ```

2. **Helm 3** installed
   ```bash
   helm version
   ```

3. **kubectl** configured to access your cluster
   ```bash
   kubectl cluster-info
   ```

4. **HashiCorp Vault** instance accessible from the cluster

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/pdasilva11/ecm-k8s-plugin.git
cd ecm-k8s-plugin/helm
```

### 2. Review and Customize Values

```bash
# View default values
cat ecm-plugin/values.yaml

# Copy and customize for your environment
cp ecm-plugin/values.yaml my-values.yaml
```

Edit `my-values.yaml` and update:
- `secrets.vaultUsername` and `secrets.vaultPassword`
- `app.vault.baseUrl` (your Vault server URL)
- `ingress.hosts` (your domain)
- Resource limits as needed

### 3. Install the Chart

**Development Environment:**
```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f ecm-plugin/values-development.yaml
```

**Production Environment:**
```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f ecm-plugin/values-production.yaml \
  --set secrets.vaultUsername=your-username \
  --set secrets.vaultPassword=your-secure-password
```

### 4. Verify Installation

```bash
# Check release status
helm status ecm-plugin -n vault-services

# Check pods
kubectl get pods -n vault-services -l app=vault-credential-service

# Check all resources
kubectl get all -n vault-services
```

### 5. Test the Service

```bash
# Port forward to test locally
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80

# Test health endpoint
curl http://localhost:8080/api/credentials/health
```

## Installation Methods

### Method 1: Using Custom Values File

1. Create your custom values file:

```yaml
# custom-values.yaml
replicaCount: 3

image:
  tag: "v1.0.0"

app:
  vault:
    baseUrl: "https://vault.mycompany.com:8200"

secrets:
  vaultUsername: "my-vault-user"
  vaultPassword: "my-secure-password"

ingress:
  hosts:
    - host: vault-api.mycompany.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: vault-api-tls
      hosts:
        - vault-api.mycompany.com

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 200m
    memory: 512Mi
```

2. Install:

```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f custom-values.yaml
```

### Method 2: Using --set Flags

```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  --set replicaCount=3 \
  --set image.tag=v1.0.0 \
  --set app.vault.baseUrl=https://vault.mycompany.com:8200 \
  --set secrets.vaultUsername=my-user \
  --set secrets.vaultPassword=my-password
```

### Method 3: Using Existing Secrets

If you want to manage secrets separately:

1. Create the secret:

```bash
kubectl create namespace vault-services

kubectl create secret generic vault-credential-service-credentials \
  --from-literal=vault-username=your-username \
  --from-literal=vault-password=your-password \
  -n vault-services
```

2. Modify your values file to skip secret creation or update the secret template.

### Method 4: Using Sealed Secrets or External Secrets

For production environments, consider using:

- **Sealed Secrets**: Encrypt secrets that can be safely stored in Git
- **External Secrets Operator**: Sync secrets from external secret management systems

Example with External Secrets:

```yaml
# external-secret.yaml
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: vault-credentials
  namespace: vault-services
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: vault-backend
    kind: SecretStore
  target:
    name: vault-credential-service-credentials
  data:
    - secretKey: vault-username
      remoteRef:
        key: vault/credentials
        property: username
    - secretKey: vault-password
      remoteRef:
        key: vault/credentials
        property: password
```

## Upgrading

### Upgrade with New Values

```bash
helm upgrade ecm-plugin ./ecm-plugin \
  -n vault-services \
  -f custom-values.yaml
```

### Upgrade Only Specific Values

```bash
helm upgrade ecm-plugin ./ecm-plugin \
  -n vault-services \
  --reuse-values \
  --set image.tag=v1.1.0
```

### Rollback

```bash
# View release history
helm history ecm-plugin -n vault-services

# Rollback to previous version
helm rollback ecm-plugin -n vault-services

# Rollback to specific revision
helm rollback ecm-plugin 2 -n vault-services
```

## Uninstalling

```bash
# Uninstall the release
helm uninstall ecm-plugin -n vault-services

# Delete the namespace (if desired)
kubectl delete namespace vault-services
```

## Customization Examples

### Enable Autoscaling

```yaml
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 20
  targetCPUUtilizationPercentage: 60
  targetMemoryUtilizationPercentage: 70
```

### Disable Ingress (Use NodePort)

```yaml
service:
  type: NodePort

ingress:
  enabled: false
```

### Custom Affinity Rules

```yaml
affinity:
  nodeAffinity:
    requiredDuringSchedulingIgnoredDuringExecution:
      nodeSelectorTerms:
      - matchExpressions:
        - key: node-type
          operator: In
          values:
          - application
```

### Add Node Selector

```yaml
nodeSelector:
  disktype: ssd
  environment: production
```

### Add Tolerations

```yaml
tolerations:
  - key: "dedicated"
    operator: "Equal"
    value: "vault-services"
    effect: "NoSchedule"
```

## Troubleshooting

### Check Release Status

```bash
helm status ecm-plugin -n vault-services
helm get all ecm-plugin -n vault-services
```

### View Rendered Templates

```bash
helm template ecm-plugin ./ecm-plugin -n vault-services -f custom-values.yaml
```

### Debug Installation

```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f custom-values.yaml \
  --debug \
  --dry-run
```

### Common Issues

#### 1. ImagePullBackOff

```bash
# Check if image exists and is accessible
kubectl describe pod -n vault-services <pod-name>

# Verify image name and tag
helm get values ecm-plugin -n vault-services
```

#### 2. CrashLoopBackOff

```bash
# Check logs
kubectl logs -n vault-services -l app=vault-credential-service --tail=100

# Check events
kubectl get events -n vault-services --sort-by='.lastTimestamp'
```

#### 3. Vault Connection Issues

```bash
# Test Vault connectivity from pod
kubectl exec -n vault-services <pod-name> -- curl -v http://vault.vault.svc.cluster.local:8200/v1/sys/health

# Check vault URL in config
kubectl describe deployment -n vault-services vault-credential-service
```

#### 4. Health Check Failures

```bash
# Check health endpoint
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80
curl -v http://localhost:8080/api/credentials/health

# Check probe configuration
kubectl describe pod -n vault-services <pod-name>
```

## Advanced Configuration

### Using with Istio Service Mesh

```yaml
podAnnotations:
  sidecar.istio.io/inject: "true"
  traffic.sidecar.istio.io/excludeOutboundPorts: "8200"  # Exclude Vault port
```

### Enabling Pod Security Standards

```yaml
podSecurityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault

securityContext:
  allowPrivilegeEscalation: false
  capabilities:
    drop:
    - ALL
  readOnlyRootFilesystem: true
```

### Custom Resource Limits by Environment

```bash
# Development
helm install ecm-plugin ./ecm-plugin \
  -f values-development.yaml \
  -n dev

# Staging
helm install ecm-plugin ./ecm-plugin \
  -f values-staging.yaml \
  -n staging

# Production
helm install ecm-plugin ./ecm-plugin \
  -f values-production.yaml \
  -n production
```

## Monitoring and Observability

### Prometheus Integration

The chart includes Prometheus annotations by default:

```yaml
podAnnotations:
  prometheus.io/scrape: "true"
  prometheus.io/port: "8080"
  prometheus.io/path: "/metrics"
```

### Grafana Dashboard

Create a dashboard to monitor:
- Pod CPU/Memory usage
- Request rates
- Health check status
- HPA metrics

### Logging

View aggregated logs:

```bash
# All pods
kubectl logs -n vault-services -l app=vault-credential-service --tail=100 -f

# Specific pod
kubectl logs -n vault-services <pod-name> -f
```

## Security Best Practices

1. **Never commit secrets to Git**
   - Use `--set` flags or external secret management
   - Use `.gitignore` for custom values files with secrets

2. **Use specific image tags** in production (not `latest`)

3. **Enable Network Policies** to restrict traffic

4. **Use RBAC** with minimal required permissions

5. **Enable Pod Security Standards**

6. **Use TLS/SSL** for ingress

7. **Regularly update** the chart and container images

8. **Use Pod Disruption Budgets** for high availability

## CI/CD Integration

### GitLab CI Example

```yaml
deploy:
  stage: deploy
  script:
    - helm upgrade --install ecm-plugin ./helm/ecm-plugin
        --namespace vault-services
        --create-namespace
        --values helm/ecm-plugin/values-production.yaml
        --set image.tag=$CI_COMMIT_TAG
        --set secrets.vaultUsername=$VAULT_USERNAME
        --set secrets.vaultPassword=$VAULT_PASSWORD
        --wait
  only:
    - tags
```

### GitHub Actions Example

```yaml
- name: Deploy Helm Chart
  run: |
    helm upgrade --install ecm-plugin ./helm/ecm-plugin \
      --namespace vault-services \
      --create-namespace \
      --values helm/ecm-plugin/values-production.yaml \
      --set image.tag=${{ github.ref_name }} \
      --set secrets.vaultUsername=${{ secrets.VAULT_USERNAME }} \
      --set secrets.vaultPassword=${{ secrets.VAULT_PASSWORD }} \
      --wait
```

## Support and Documentation

- **Chart README**: `ecm-plugin/README.md`
- **Deployment Guide**: `../K8S_DEPLOYMENT_GUIDE.md`
- **Repository**: https://github.com/pdasilva11/ecm-k8s-plugin
- **Issues**: https://github.com/pdasilva11/ecm-k8s-plugin/issues
