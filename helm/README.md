# ECM Kubernetes Plugin - Helm Charts

Production-ready Helm chart for deploying the ECM Plugin Vault Credential Service to Kubernetes.

## 📁 Directory Structure

```
helm/
├── ecm-plugin/                      # Main Helm chart
│   ├── Chart.yaml                   # Chart metadata
│   ├── values.yaml                  # Default configuration values
│   ├── values-development.yaml      # Development environment values
│   ├── values-production.yaml       # Production environment values
│   ├── .helmignore                  # Files to ignore when packaging
│   ├── README.md                    # Detailed chart documentation
│   └── templates/                   # Kubernetes manifest templates
│       ├── _helpers.tpl             # Template helper functions
│       ├── NOTES.txt                # Post-installation notes
│       ├── configmap.yaml           # Application configuration
│       ├── deployment.yaml          # Application deployment
│       ├── service.yaml             # Kubernetes service
│       ├── serviceaccount.yaml      # Service account
│       ├── secret.yaml              # Vault credentials
│       ├── ingress.yaml             # Ingress resource
│       ├── rbac.yaml                # Role and RoleBinding
│       ├── hpa.yaml                 # Horizontal Pod Autoscaler
│       ├── pdb.yaml                 # Pod Disruption Budget
│       └── networkpolicy.yaml       # Network policies
├── QUICKSTART.md                    # Quick start guide
├── INSTALLATION.md                  # Comprehensive installation guide
└── README.md                        # This file
```

## 🚀 Quick Start

### Method 1: Install from GitHub Helm Repository (Recommended)

Once the repository is set up (see [GITHUB_HOSTING.md](GITHUB_HOSTING.md)):

```bash
# Add the Helm repository
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin

# Update repositories
helm repo update

# Install the chart
helm install ecm-plugin ecm-plugin/ecm-plugin \
  -n vault-services \
  --create-namespace \
  --set app.ecm.sraSiteHostname=pra.yourcompany.com \
  --set app.ecm.sraClientId=your-pra-client-id \
  --set secrets.sraClientSecret=your-pra-secret \
  --set secrets.vaultUsername=your-vault-user \
  --set secrets.vaultPassword=your-vault-password
```

### Method 2: Install from Local Source

```bash
# Clone the repository
git clone https://github.com/pdasilva11/ecm-k8s-plugin.git
cd ecm-k8s-plugin/helm

# Development
helm install ecm-plugin ./ecm-plugin -n vault-services --create-namespace

# Production
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f ecm-plugin/values-production.yaml \
  --set app.ecm.sraSiteHostname=pra.yourcompany.com \
  --set app.ecm.sraClientId=your-pra-client-id \
  --set secrets.sraClientSecret=your-pra-secret \
  --set secrets.vaultUsername=your-vault-user \
  --set secrets.vaultPassword=your-vault-password
```

See [QUICKSTART.md](QUICKSTART.md) for more examples.

## 📚 Documentation

- **[QUICKSTART.md](QUICKSTART.md)** - Get started in 5 minutes
- **[INSTALLATION.md](INSTALLATION.md)** - Comprehensive installation guide with examples
- **[GITHUB_HOSTING.md](GITHUB_HOSTING.md)** - Host chart on GitHub Pages (Helm repository)
- **[ecm-plugin/README.md](ecm-plugin/README.md)** - Detailed chart documentation

## ✨ Features

### High Availability
- ✅ Horizontal Pod Autoscaler (HPA) for automatic scaling
- ✅ Pod Disruption Budget (PDB) for availability during updates
- ✅ Pod anti-affinity rules to spread across nodes
- ✅ Rolling update strategy for zero-downtime deployments
- ✅ Configurable replica count (default: 2, production: 3+)

### Security
- ✅ Non-root container execution
- ✅ Pod Security Context configuration
- ✅ Network Policies for traffic control
- ✅ RBAC (Role-Based Access Control)
- ✅ Secret management for sensitive data
- ✅ TLS/SSL support via Ingress

### Observability
- ✅ Prometheus metrics annotations
- ✅ Startup, liveness, and readiness probes
- ✅ Structured logging configuration
- ✅ Resource requests and limits

### Flexibility
- ✅ Multiple environment configurations (dev, staging, prod)
- ✅ Configurable ingress with TLS
- ✅ Customizable resource limits
- ✅ Support for NodeSelector, tolerations, and affinity
- ✅ Parameterized configuration via values.yaml

## 🎯 Key Components

### 1. Deployment
- Manages application pods with configurable replicas
- Includes health probes and resource management
- Supports rolling updates and rollbacks

### 2. Service
- ClusterIP service with session affinity
- Routes traffic to application pods
- Configurable port mappings

### 3. Ingress
- Optional ingress with TLS support
- Configurable hostname and paths
- Nginx ingress controller annotations

### 4. ConfigMap & Secrets
- Application configuration via ConfigMap
- Secure credential storage in Secrets
- Environment-specific settings

### 5. Autoscaling (HPA)
- CPU and memory-based autoscaling
- Configurable min/max replicas
- Custom target utilization percentages

### 6. Security Resources
- ServiceAccount for pod identity
- RBAC for Kubernetes API access
- NetworkPolicy for traffic control

## 📝 Configuration

### Common Configurations

```yaml
# Scaling
replicaCount: 3
autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 20

# Image
image:
  repository: pdasilva1/ecm-k8s-plugin
  tag: "v1.0.0"
  pullPolicy: IfNotPresent

# Resources
resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 250m
    memory: 512Mi

# Application Configuration
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
  # BeyondTrust PRA credentials
  sraClientSecret: "your-pra-client-secret"

  # HashiCorp Vault credentials
  vaultUsername: "your-vault-username"
  vaultPassword: "your-vault-password"

# Ingress
ingress:
  enabled: true
  hosts:
    - host: vault-api.yourcompany.com
      paths:
        - path: /
          pathType: Prefix
```

## 🔧 Installation Methods

### Method 1: Default Values
```bash
helm install ecm-plugin ./ecm-plugin -n vault-services --create-namespace
```

### Method 2: Custom Values File
```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  -f my-values.yaml
```

### Method 3: Set Individual Values
```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --create-namespace \
  --set replicaCount=3 \
  --set image.tag=v1.0.0
```

### Method 4: Environment-Specific
```bash
# Development
helm install ecm-plugin ./ecm-plugin -f ecm-plugin/values-development.yaml -n dev

# Production
helm install ecm-plugin ./ecm-plugin -f ecm-plugin/values-production.yaml -n prod
```

## 🔄 Maintenance

### Upgrade
```bash
helm upgrade ecm-plugin ./ecm-plugin -n vault-services -f my-values.yaml
```

### Rollback
```bash
helm rollback ecm-plugin -n vault-services
```

### Uninstall
```bash
helm uninstall ecm-plugin -n vault-services
```

## 🐛 Troubleshooting

### View Generated Manifests
```bash
helm template ecm-plugin ./ecm-plugin -f my-values.yaml
```

### Debug Installation
```bash
helm install ecm-plugin ./ecm-plugin \
  -n vault-services \
  --dry-run --debug
```

### Check Release Status
```bash
helm status ecm-plugin -n vault-services
helm get all ecm-plugin -n vault-services
```

### View Logs
```bash
kubectl logs -n vault-services -l app=vault-credential-service --tail=100 -f
```

## 📋 Requirements

| Component | Version | Required |
|-----------|---------|----------|
| Kubernetes | 1.19+ | Yes |
| Helm | 3.0+ | Yes |
| HashiCorp Vault | Any | Yes |
| Ingress Controller | Any | Optional |
| Cert Manager | Any | Optional |
| Prometheus | Any | Optional |

## 🌟 Best Practices

1. **Use specific image tags** in production (not `latest`)
2. **Store secrets securely** (use external secret management)
3. **Enable autoscaling** for production workloads
4. **Configure resource limits** based on your workload
5. **Enable network policies** to restrict traffic
6. **Use TLS** for ingress endpoints
7. **Monitor** with Prometheus metrics
8. **Test upgrades** in non-production first
9. **Use Pod Disruption Budgets** to ensure availability
10. **Regular updates** of chart and container images

## 🤝 Contributing

Contributions are welcome! Please:
1. Test your changes thoroughly
2. Update documentation
3. Follow Helm best practices
4. Submit pull requests to the main repository

## 📖 Additional Resources

- [Helm Documentation](https://helm.sh/docs/)
- [Kubernetes Best Practices](https://kubernetes.io/docs/concepts/configuration/overview/)
- [Project Repository](https://github.com/pdasilva11/ecm-k8s-plugin)
- [Original K8S Guide](../K8S_DEPLOYMENT_GUIDE.md)

## 📞 Support

- **Issues**: https://github.com/pdasilva11/ecm-k8s-plugin/issues
- **Documentation**: See INSTALLATION.md and chart README.md

## 📄 License

This Helm chart is provided as-is for use with the ECM Kubernetes Plugin.

---

**Quick Links:**
- [Quick Start Guide](QUICKSTART.md)
- [Installation Guide](INSTALLATION.md)
- [Chart Documentation](ecm-plugin/README.md)
- [GitHub Repository](https://github.com/pdasilva11/ecm-k8s-plugin)
