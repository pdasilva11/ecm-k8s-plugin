# ECM Plugin Helm Chart

A Helm chart for deploying the ECM Kubernetes Plugin - Vault Credential Service to Kubernetes clusters.

## Overview

This Helm chart deploys a .NET Core web API service that integrates with HashiCorp Vault for credential management and injection. The service provides a secure API for credential operations within Kubernetes environments.

## Prerequisites

- Kubernetes 1.19+
- Helm 3.0+
- A running HashiCorp Vault instance (accessible from the cluster)
- PersistentVolume provisioner support in the underlying infrastructure (optional)

## Installation

### Add the Helm Chart

If this chart is published to a repository:

```bash
helm repo add ecm-plugin https://your-repo-url
helm repo update
```

### Install from local directory

```bash
cd helm
helm install ecm-plugin ./ecm-plugin -n vault-services --create-namespace
```

### Install with custom values

```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f custom-values.yaml
```

## Configuration

The following table lists the configurable parameters of the ECM Plugin chart and their default values.

### Global Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `replicaCount` | Number of replicas | `2` |
| `nameOverride` | Override the chart name | `""` |
| `fullnameOverride` | Override the full name | `"vault-credential-service"` |

### Image Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `image.repository` | Image repository | `pdasilva1/ecm-k8s-plugin` |
| `image.pullPolicy` | Image pull policy | `Always` |
| `image.tag` | Image tag | `"latest"` |

### Service Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `service.type` | Kubernetes service type | `ClusterIP` |
| `service.port` | Service port | `80` |
| `service.targetPort` | Container port | `8080` |
| `service.sessionAffinity` | Session affinity | `ClientIP` |

### Ingress Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ingress.enabled` | Enable ingress | `true` |
| `ingress.className` | Ingress class name | `"nginx"` |
| `ingress.hosts[0].host` | Hostname | `vault-credentials.example.com` |
| `ingress.tls` | TLS configuration | See values.yaml |

### Resource Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `resources.limits.cpu` | CPU limit | `500m` |
| `resources.limits.memory` | Memory limit | `512Mi` |
| `resources.requests.cpu` | CPU request | `100m` |
| `resources.requests.memory` | Memory request | `256Mi` |

### Autoscaling Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `autoscaling.enabled` | Enable HPA | `true` |
| `autoscaling.minReplicas` | Minimum replicas | `2` |
| `autoscaling.maxReplicas` | Maximum replicas | `10` |
| `autoscaling.targetCPUUtilizationPercentage` | Target CPU % | `70` |
| `autoscaling.targetMemoryUtilizationPercentage` | Target Memory % | `80` |

### Application Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| `app.environment` | ASP.NET environment | `Production` |
| `app.vault.baseUrl` | Vault server URL | `http://vault.vault.svc.cluster.local:8200` |
| `app.vault.secretsEngine` | Vault secrets engine | `secret` |

### Security Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `secrets.vaultUsername` | Vault username | `vault-user` |
| `secrets.vaultPassword` | Vault password | `change-me-to-your-vault-password` |
| `podSecurityContext.runAsNonRoot` | Run as non-root | `true` |
| `podSecurityContext.runAsUser` | User ID | `1000` |

## Examples

### Basic Installation

```bash
helm install my-ecm-plugin ./ecm-plugin -n vault-services --create-namespace
```

### Install with Custom Values

Create a file named `my-values.yaml`:

```yaml
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

Install with custom values:

```bash
helm install my-ecm-plugin ./ecm-plugin -n vault-services -f my-values.yaml
```

### Upgrade the Release

```bash
helm upgrade my-ecm-plugin ./ecm-plugin -n vault-services -f my-values.yaml
```

### Uninstall the Release

```bash
helm uninstall my-ecm-plugin -n vault-services
```

## Monitoring

The chart includes Prometheus annotations for metrics scraping:

```yaml
podAnnotations:
  prometheus.io/scrape: "true"
  prometheus.io/port: "8080"
  prometheus.io/path: "/metrics"
```

## Health Checks

The deployment includes three types of probes:

- **Startup Probe**: Checks if the application has started
- **Liveness Probe**: Checks if the application is running
- **Readiness Probe**: Checks if the application is ready to serve traffic

All probes use the `/api/credentials/health` endpoint.

## Security Features

- Non-root container execution
- Network policies for ingress/egress control
- RBAC configuration
- Pod Security Context
- Secrets management for sensitive data
- TLS/SSL support via Ingress

## High Availability

The chart includes several features for high availability:

- **Horizontal Pod Autoscaler (HPA)**: Automatically scales based on CPU/Memory usage
- **Pod Disruption Budget (PDB)**: Ensures minimum availability during disruptions
- **Pod Anti-Affinity**: Spreads pods across different nodes
- **Rolling Update Strategy**: Zero-downtime deployments

## Troubleshooting

### Check Pod Status

```bash
kubectl get pods -n vault-services -l app=vault-credential-service
```

### View Logs

```bash
kubectl logs -n vault-services -l app=vault-credential-service --tail=100 -f
```

### Check Service

```bash
kubectl get svc -n vault-services vault-credential-service
```

### Test Health Endpoint

```bash
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80
curl http://localhost:8080/api/credentials/health
```

### Validate Helm Release

```bash
helm list -n vault-services
helm status my-ecm-plugin -n vault-services
```

### Debug Rendering

To see what Kubernetes manifests will be generated:

```bash
helm template my-ecm-plugin ./ecm-plugin -n vault-services -f my-values.yaml
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This chart is provided as-is for use with the ECM Kubernetes Plugin.

## Support

For issues and questions:
- GitHub Issues: https://github.com/pdasilva11/ecm-k8s-plugin/issues
- Documentation: See K8S_DEPLOYMENT_GUIDE.md in the repository
