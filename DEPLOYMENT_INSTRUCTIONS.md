# ECM Kubernetes Plugin - Deployment Guide

Welcome to the ECM Kubernetes Plugin deployment guide. This document will help you deploy the Vault Credential Service to your Kubernetes cluster using Helm.

## Overview

The ECM Kubernetes Plugin provides a secure, scalable solution for managing vault credentials in Kubernetes environments. This Helm chart deploys a production-ready service with enterprise-grade features including high availability, security controls, and automated scaling.

## Prerequisites

Before deploying, ensure you have:

- **Kubernetes cluster** (v1.19 or higher)
- **Helm** (v3.0 or higher)
- **kubectl** configured to access your cluster
- **HashiCorp Vault** instance (accessible from your cluster)
- Sufficient permissions to create resources in your cluster

### Verify Prerequisites

```bash
# Check Kubernetes version
kubectl version --short

# Check Helm version
helm version

# Verify cluster access
kubectl cluster-info

# Check available namespaces
kubectl get namespaces
```

## Installation Methods

### Method 1: Install from Helm Repository (Recommended)

The easiest way to install the ECM Plugin is directly from our Helm repository:

```bash
# Add the ECM Plugin Helm repository
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin

# Update your local Helm repository cache
helm repo update

# Search for available charts
helm search repo ecm-plugin

# Install the chart
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --create-namespace \
  --set secrets.vaultUsername=<your-vault-username> \
  --set secrets.vaultPassword=<your-vault-password>
```

### Method 2: Install from Source

Clone the repository and install from local source:

```bash
# Clone the repository
git clone https://github.com/pdasilva11/ecm-k8s-plugin.git
cd ecm-k8s-plugin/helm

# Install the chart
helm install ecm-plugin ./ecm-plugin \
  --namespace vault-services \
  --create-namespace \
  --values ./ecm-plugin/values.yaml
```

### Method 3: Install Specific Version

Install a specific version of the chart:

```bash
# List available versions
helm search repo ecm-plugin/ecm-plugin --versions

# Install specific version
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --version 1.0.0 \
  --namespace vault-services \
  --create-namespace
```

## Configuration

### Quick Configuration

Create a custom values file for your environment:

```yaml
# my-values.yaml
replicaCount: 3

image:
  tag: "1.0.0"

app:
  # BeyondTrust PRA/SRA Configuration
  ecm:
    sraSiteHostname: "pra.yourcompany.com"
    sraClientId: "your-pra-client-id"

  # HashiCorp Vault Configuration
  vault:
    baseUrl: "https://vault.yourcompany.com:8200"
    secretsEngine: "secret"

secrets:
  # HashiCorp Vault credentials
  vaultUsername: "your-vault-username"
  vaultPassword: "your-vault-password"

  # BeyondTrust PRA credentials
  sraClientSecret: "your-pra-client-secret"

ingress:
  enabled: true
  hosts:
    - host: vault-api.yourcompany.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: vault-api-tls
      hosts:
        - vault-api.yourcompany.com

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 250m
    memory: 512Mi
```

Install with your custom values:

```bash
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --create-namespace \
  --values my-values.yaml
```

### Environment-Specific Deployments

#### Development Environment

```bash
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace dev \
  --create-namespace \
  --values https://raw.githubusercontent.com/pdasilva11/ecm-k8s-plugin/main/helm/ecm-plugin/values-development.yaml
```

#### Production Environment

```bash
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace production \
  --create-namespace \
  --values https://raw.githubusercontent.com/pdasilva11/ecm-k8s-plugin/main/helm/ecm-plugin/values-production.yaml \
  --set app.ecm.sraSiteHostname=$PRA_HOSTNAME \
  --set app.ecm.sraClientId=$PRA_CLIENT_ID \
  --set secrets.sraClientSecret=$PRA_CLIENT_SECRET \
  --set secrets.vaultUsername=$VAULT_USERNAME \
  --set secrets.vaultPassword=$VAULT_PASSWORD
```

## Key Configuration Parameters

### BeyondTrust PRA/SRA Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `app.ecm.sraSiteHostname` | BeyondTrust PRA hostname | `pra.example.com` |
| `app.ecm.sraClientId` | PRA OAuth client ID | `your-pra-client-id` |
| `secrets.sraClientSecret` | PRA OAuth client secret | Required |

### HashiCorp Vault Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `app.vault.baseUrl` | Vault server URL | `http://vault.vault.svc.cluster.local:8200` |
| `app.vault.secretsEngine` | Vault secrets engine path | `secret` |
| `secrets.vaultUsername` | Vault username | `vault-user` |
| `secrets.vaultPassword` | Vault password | Required |

### General Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of pod replicas | `2` |
| `image.repository` | Container image repository | `pdasilva1/ecm-k8s-plugin` |
| `image.tag` | Container image tag | `latest` |
| `ingress.enabled` | Enable ingress | `true` |
| `ingress.hosts` | Ingress hostnames | `vault-credentials.example.com` |
| `autoscaling.enabled` | Enable HPA | `true` |
| `autoscaling.minReplicas` | Minimum replicas | `2` |
| `autoscaling.maxReplicas` | Maximum replicas | `10` |
| `resources.limits.cpu` | CPU limit | `500m` |
| `resources.limits.memory` | Memory limit | `512Mi` |

For a complete list of configuration options, see the [values.yaml](https://github.com/pdasilva11/ecm-k8s-plugin/blob/main/helm/ecm-plugin/values.yaml) file.

## Post-Installation

### Verify Deployment

```bash
# Check Helm release status
helm status ecm-plugin --namespace vault-services

# List all resources
kubectl get all --namespace vault-services

# Check pod status
kubectl get pods --namespace vault-services --selector app=vault-credential-service

# View pod logs
kubectl logs --namespace vault-services --selector app=vault-credential-service --tail=50
```

### Access the Service

#### Using Port Forward (for testing)

```bash
kubectl port-forward --namespace vault-services \
  service/vault-credential-service 8080:80

# In another terminal, test the health endpoint
curl http://localhost:8080/api/credentials/health
```

#### Using Ingress (production)

If ingress is enabled, access the service at your configured hostname:

```bash
curl https://vault-api.yourcompany.com/api/credentials/health
```

### Monitoring

Check the HorizontalPodAutoscaler:

```bash
kubectl get hpa --namespace vault-services
```

View metrics (if Prometheus is configured):

```bash
kubectl port-forward --namespace vault-services \
  service/vault-credential-service 8080:80

curl http://localhost:8080/metrics
```

## Upgrading

### Upgrade to Latest Version

```bash
# Update Helm repository
helm repo update

# Upgrade the release
helm upgrade ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --reuse-values
```

### Upgrade with New Configuration

```bash
helm upgrade ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --values my-values.yaml
```

### Upgrade to Specific Version

```bash
helm upgrade ecm-plugin ecm-plugin/ecm-plugin \
  --version 1.1.0 \
  --namespace vault-services \
  --reuse-values
```

## Rollback

If an upgrade causes issues, rollback to a previous version:

```bash
# View release history
helm history ecm-plugin --namespace vault-services

# Rollback to previous version
helm rollback ecm-plugin --namespace vault-services

# Rollback to specific revision
helm rollback ecm-plugin 2 --namespace vault-services
```

## Uninstalling

To remove the ECM Plugin from your cluster:

```bash
# Uninstall the Helm release
helm uninstall ecm-plugin --namespace vault-services

# Optionally, delete the namespace
kubectl delete namespace vault-services
```

## Security Best Practices

### Credentials Management

**Never commit credentials to version control!**

#### Option 1: Use Environment Variables

```bash
# Set BeyondTrust PRA credentials
export PRA_HOSTNAME="pra.yourcompany.com"
export PRA_CLIENT_ID="your-pra-client-id"
export PRA_CLIENT_SECRET="your-pra-client-secret"

# Set HashiCorp Vault credentials
export VAULT_USERNAME="your-vault-username"
export VAULT_PASSWORD="your-vault-password"

helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --create-namespace \
  --set app.ecm.sraSiteHostname=$PRA_HOSTNAME \
  --set app.ecm.sraClientId=$PRA_CLIENT_ID \
  --set secrets.sraClientSecret=$PRA_CLIENT_SECRET \
  --set secrets.vaultUsername=$VAULT_USERNAME \
  --set secrets.vaultPassword=$VAULT_PASSWORD
```

#### Option 2: Use Kubernetes Secrets

```bash
# Create a secret manually with both PRA and Vault credentials
kubectl create secret generic vault-credential-service-credentials \
  --from-literal=vault-username=your-vault-username \
  --from-literal=vault-password=your-vault-password \
  --from-literal=sra-client-secret=your-pra-client-secret \
  --namespace vault-services

# Then install chart with PRA hostname and client ID
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --create-namespace \
  --set app.ecm.sraSiteHostname="pra.yourcompany.com" \
  --set app.ecm.sraClientId="your-pra-client-id"
```

#### Option 3: Use External Secret Management

For production environments, consider using:
- **Sealed Secrets** - Encrypt secrets for safe storage in Git
- **External Secrets Operator** - Sync from external secret stores
- **HashiCorp Vault** - Native Vault integration
- **AWS Secrets Manager, Azure Key Vault, GCP Secret Manager** - Cloud provider solutions

### Network Security

The chart includes NetworkPolicies by default. Ensure they are enabled:

```yaml
networkPolicy:
  enabled: true
```

### TLS/SSL

For production, always enable TLS on ingress:

```yaml
ingress:
  enabled: true
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  tls:
    - secretName: vault-service-tls
      hosts:
        - vault-api.yourcompany.com
```

## Troubleshooting

### Pods Not Starting

```bash
# Describe the pod to see events
kubectl describe pod --namespace vault-services <pod-name>

# Check pod logs
kubectl logs --namespace vault-services <pod-name>

# Check if images can be pulled
kubectl get events --namespace vault-services --sort-by='.lastTimestamp'
```

### Service Unavailable

```bash
# Check service endpoints
kubectl get endpoints --namespace vault-services

# Verify service configuration
kubectl describe service vault-credential-service --namespace vault-services

# Test service connectivity
kubectl run test-pod --rm -i --tty --image=curlimages/curl --namespace vault-services -- sh
# Inside the pod:
curl http://vault-credential-service/api/credentials/health
```

### Health Check Failures

```bash
# Check probe configuration
kubectl describe deployment --namespace vault-services vault-credential-service

# View pod logs for errors
kubectl logs --namespace vault-services --selector app=vault-credential-service

# Port forward and test manually
kubectl port-forward --namespace vault-services svc/vault-credential-service 8080:80
curl http://localhost:8080/api/credentials/health
```

### Vault Connection Issues

```bash
# Test Vault connectivity from a pod
kubectl exec --namespace vault-services <pod-name> -- \
  curl -v http://vault.vault.svc.cluster.local:8200/v1/sys/health

# Check Vault configuration
kubectl get deployment --namespace vault-services vault-credential-service -o yaml | \
  grep -A 10 "env:"
```

### High Memory/CPU Usage

```bash
# Check resource usage
kubectl top pods --namespace vault-services

# View HPA status
kubectl get hpa --namespace vault-services

# Adjust resource limits in values.yaml
```

## Support and Documentation

### Documentation

- **Quick Start Guide**: [QUICKSTART.md](helm/QUICKSTART.md)
- **Comprehensive Installation Guide**: [INSTALLATION.md](helm/INSTALLATION.md)
- **Helm Chart README**: [helm/README.md](helm/README.md)
- **Configuration Reference**: [values.yaml](helm/ecm-plugin/values.yaml)

### Community Support

- **GitHub Repository**: https://github.com/pdasilva11/ecm-k8s-plugin
- **Issues**: https://github.com/pdasilva11/ecm-k8s-plugin/issues
- **Releases**: https://github.com/pdasilva11/ecm-k8s-plugin/releases

### Getting Help

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Review the comprehensive documentation in the `helm/` directory
3. Search existing [GitHub Issues](https://github.com/pdasilva11/ecm-k8s-plugin/issues)
4. Open a new issue with detailed information:
   - Kubernetes version
   - Helm version
   - Chart version
   - Error messages and logs
   - Steps to reproduce

## Feature Overview

### High Availability

- ✅ Horizontal Pod Autoscaler for automatic scaling
- ✅ Pod Disruption Budget to ensure availability during updates
- ✅ Pod anti-affinity rules to spread across nodes
- ✅ Rolling update strategy for zero-downtime deployments

### Security

- ✅ Non-root container execution
- ✅ Pod Security Context
- ✅ Network Policies for traffic control
- ✅ RBAC (Role-Based Access Control)
- ✅ Secrets management
- ✅ TLS/SSL support via Ingress

### Observability

- ✅ Prometheus metrics annotations
- ✅ Health probes (startup, liveness, readiness)
- ✅ Structured logging
- ✅ Resource monitoring

### Flexibility

- ✅ Multiple environment configurations
- ✅ Customizable resource limits
- ✅ Configurable ingress with TLS
- ✅ Support for node selectors, tolerations, and affinity

## Migration Guide

### Migrating from Raw Kubernetes Manifests

If you're currently using the raw Kubernetes manifests in the `k8s/` directory:

1. **Review current configuration**:
   ```bash
   kubectl get all -n <current-namespace> -o yaml > current-deployment.yaml
   ```

2. **Create equivalent values file** based on your current settings

3. **Test in a separate namespace first**:
   ```bash
   helm install ecm-plugin-test ecm-plugin/ecm-plugin \
     --namespace test \
     --create-namespace \
     --values my-values.yaml
   ```

4. **Once validated, deploy to production**:
   ```bash
   # Delete old resources
   kubectl delete -f k8s/ -n <current-namespace>

   # Install with Helm
   helm install ecm-plugin ecm-plugin/ecm-plugin \
     --namespace <current-namespace> \
     --values my-values.yaml
   ```

## CI/CD Integration

### GitLab CI Example

```yaml
deploy-to-k8s:
  stage: deploy
  script:
    - helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin
    - helm repo update
    - helm upgrade --install ecm-plugin ecm-plugin/ecm-plugin
        --namespace production
        --create-namespace
        --values values-production.yaml
        --set image.tag=$CI_COMMIT_TAG
        --set secrets.vaultUsername=$VAULT_USERNAME
        --set secrets.vaultPassword=$VAULT_PASSWORD
        --wait
  only:
    - tags
```

### GitHub Actions Example

```yaml
- name: Deploy to Kubernetes
  run: |
    helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin
    helm repo update
    helm upgrade --install ecm-plugin ecm-plugin/ecm-plugin \
      --namespace production \
      --create-namespace \
      --values values-production.yaml \
      --set image.tag=${{ github.ref_name }} \
      --set secrets.vaultUsername=${{ secrets.VAULT_USERNAME }} \
      --set secrets.vaultPassword=${{ secrets.VAULT_PASSWORD }} \
      --wait
```

## FAQ

### Q: Can I customize the Kubernetes resources?

Yes! You can override any value in the chart. See [values.yaml](helm/ecm-plugin/values.yaml) for all available options.

### Q: How do I enable autoscaling?

Autoscaling is enabled by default. Configure it in your values file:

```yaml
autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
```

### Q: Can I disable certain features?

Yes, most features can be disabled. For example:

```yaml
ingress:
  enabled: false

autoscaling:
  enabled: false

networkPolicy:
  enabled: false
```

### Q: How do I use a different Docker image?

Override the image settings:

```yaml
image:
  repository: your-registry/your-image
  tag: "your-tag"
  pullPolicy: IfNotPresent
```

### Q: How often should I update the chart?

Check for updates regularly. Subscribe to the [GitHub repository](https://github.com/pdasilva11/ecm-k8s-plugin) for release notifications.

## License

This project is provided as-is. Please review the license terms in the repository.

---

**Version**: 1.0.0
**Last Updated**: 2026-02-03
**Repository**: https://github.com/pdasilva11/ecm-k8s-plugin
**Helm Repository**: https://pdasilva11.github.io/ecm-k8s-plugin
