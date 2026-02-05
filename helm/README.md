# ECM Kubernetes Plugin - Helm Charts

Production-ready Helm chart for deploying the HashiCorp Vault to BeyondTrust PRA Sync Service to Kubernetes.

**Latest Chart Version: v2.2.0**

## 📁 Directory Structure

```
helm/
├── ecm-plugin/                      # Main Helm chart
│   ├── Chart.yaml                   # Chart metadata
│   ├── values.yaml                  # Default configuration values
│   ├── values-sync.yaml             # Sync service values
│   ├── values-development.yaml      # Development environment values
│   ├── values-production.yaml       # Production environment values
│   ├── .helmignore                  # Files to ignore when packaging
│   ├── README.md                    # Detailed chart documentation
│   └── templates/                   # Kubernetes manifest templates
│       ├── _helpers.tpl             # Template helper functions
│       ├── NOTES.txt                # Post-installation notes
│       ├── configmap.yaml           # Application configuration
│       ├── deployment.yaml          # Main ECM plugin deployment
│       ├── sync-deployment.yaml     # Sync service deployment
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

### Install Sync Service from GitHub Helm Repository

```bash
# Add the Helm repository
helm repo add ecm-plugin https://pdasilva11.github.io/ecm-k8s-plugin/
helm repo update

# Install the sync service (v2.2.0)
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

### Upgrade

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

See [QUICKSTART.md](QUICKSTART.md) for more examples.

## 📚 Documentation

- **[QUICKSTART.md](QUICKSTART.md)** - Get started in 5 minutes
- **[INSTALLATION.md](INSTALLATION.md)** - Comprehensive installation guide with examples
- **[GITHUB_HOSTING.md](GITHUB_HOSTING.md)** - Host chart on GitHub Pages (Helm repository)
- **[ecm-plugin/README.md](ecm-plugin/README.md)** - Detailed chart documentation

## ✨ Features

### Sync Service
- Syncs secrets from HashiCorp Vault (KV v2) to BeyondTrust PRA Vault
- Auto-detects secret type: `username_password` or `opaque_token`
- Version-based change detection — updates PRA when Vault secrets change
- Assigns accounts to configurable PRA account groups
- Continuous sync every 5 minutes (configurable)

### Security
- Non-root container execution
- Pod Security Context configuration
- Secret management for credentials via Kubernetes Secrets
- RBAC (Role-Based Access Control)

### Flexibility
- Configurable sync interval
- Configurable PRA account group
- Support for multiple secret types
- CI/CD ready with automated Docker image builds

## 📝 Helm `--set` Flags Reference

| Flag | Description | Required |
|------|-------------|----------|
| `replicaCount=0` | Disable main ECM plugin (sync-only mode) | Yes |
| `autoscaling.enabled=false` | Prevent HPA from overriding replicaCount | Yes |
| `syncService.enabled=true` | Enable the sync service | Yes |
| `app.ecm.sraSiteHostname` | PRA instance hostname | Yes |
| `app.ecm.sraClientId` | PRA OAuth2 client ID | Yes |
| `app.ecm.accountGroup` | PRA account group name or ID | Yes |
| `secrets.sraClientSecret` | PRA OAuth2 client secret | Yes |
| `app.vault.baseUrl` | HashiCorp Vault API endpoint | Yes |
| `secrets.vaultUsername` | Vault username | Yes |
| `secrets.vaultPassword` | Vault password | Yes |
| `app.vault.secretsEngine` | KV v2 secrets engine path | Yes |

## 🔄 Maintenance

### Rollback
```bash
helm rollback ecm-plugin -n vault-services
```

### Uninstall
```bash
helm uninstall ecm-plugin -n vault-services
```

### Restart sync pod (after new Docker image)
```bash
kubectl rollout restart deployment vault-credential-service-sync -n vault-services
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

### View Sync Logs
```bash
kubectl logs -n vault-services -l component=sync --tail=100 -f
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
