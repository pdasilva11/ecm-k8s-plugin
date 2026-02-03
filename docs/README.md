# ECM Kubernetes Plugin - Helm Repository

This directory hosts the Helm chart repository for the ECM Kubernetes Plugin via GitHub Pages.

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

### Install the Chart

```bash
helm install ecm-plugin ecm-plugin/ecm-plugin \
  --namespace vault-services \
  --create-namespace \
  --set app.ecm.sraSiteHostname=pra.yourcompany.com \
  --set app.ecm.sraClientId=your-pra-client-id \
  --set secrets.sraClientSecret=your-pra-secret \
  --set app.vault.baseUrl=http://vault.vault.svc.cluster.local:8200 \
  --set secrets.vaultUsername=vault-user \
  --set secrets.vaultPassword=vault-password
```

## Available Charts

- **ecm-plugin** - ECM Kubernetes Plugin for Vault Credential Service
  - Latest Version: 1.1.0
  - Chart URL: [ecm-plugin-1.1.0.tgz](https://pdasilva11.github.io/ecm-k8s-plugin/ecm-plugin-1.1.0.tgz)

## Repository Index

The repository index is maintained at: [index.yaml](https://pdasilva11.github.io/ecm-k8s-plugin/index.yaml)

## Documentation

For detailed documentation, see:
- [Main Repository](https://github.com/pdasilva11/ecm-k8s-plugin)
- [Deployment Guide](https://github.com/pdasilva11/ecm-k8s-plugin/blob/main/DEPLOYMENT_INSTRUCTIONS.md)
- [Quick Start](https://github.com/pdasilva11/ecm-k8s-plugin/blob/main/helm/QUICKSTART.md)
- [Installation Guide](https://github.com/pdasilva11/ecm-k8s-plugin/blob/main/helm/INSTALLATION.md)

## Chart Updates

This repository is automatically updated when new versions of the chart are released.

---

**Repository URL**: https://pdasilva11.github.io/ecm-k8s-plugin/
**Source Code**: https://github.com/pdasilva11/ecm-k8s-plugin
