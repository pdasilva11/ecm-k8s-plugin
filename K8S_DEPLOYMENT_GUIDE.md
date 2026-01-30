# Kubernetes Deployment Guide - Vault Credential Injection Service

**Version:** 1.0
**Last Updated:** January 2024
**Application:** Vault Credential Injection Service for PRA
**Repository:** https://github.com/pdasilva11/ecm-k8s-plugin

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Pre-Deployment Checklist](#pre-deployment-checklist)
4. [Installation Steps](#installation-steps)
5. [Configuration Guide](#configuration-guide)
6. [Verification & Testing](#verification--testing)
7. [Monitoring & Logging](#monitoring--logging)
8. [Scaling & Performance](#scaling--performance)
9. [Backup & Recovery](#backup--recovery)
10. [Troubleshooting](#troubleshooting)
11. [Maintenance](#maintenance)
12. [Security Hardening](#security-hardening)
13. [Advanced Configuration](#advanced-configuration)

---

## Prerequisites

### Kubernetes Cluster Requirements

```
Kubernetes Version:    1.24+
Minimum Nodes:         2
Node OS:               Linux (Ubuntu 20.04+, CentOS 8+, RHEL 8+)
Total Memory:          4GB minimum (2GB recommended per node)
Total CPU:             2 cores minimum (1 core per node)
Disk Space:            20GB minimum per node
Container Runtime:     containerd, Docker, or CRI-O
Network Plugin:        Flannel, Calico, or Weave (CNI compatible)
```

### Required Tools

```bash
# Check versions
kubectl version --client
docker version (if using Docker)
helm version (optional, for Helm charts)
kustomize version (optional, for customization)

# Minimum versions
kubectl:   1.24.0+
docker:    20.10.0+ (if using Docker)
helm:      3.0.0+ (optional)
```

### Access Requirements

```
• Admin/cluster-admin privileges
• Ability to create namespaces
• Ability to create RBAC resources
• Network access to:
  - Docker Hub (or your image registry)
  - HashiCorp Vault
  - Kubernetes API server
```

### Cluster Validation

```bash
# Check cluster connectivity
kubectl cluster-info

# Check node status
kubectl get nodes

# Check API resources
kubectl api-resources

# Check available storage classes
kubectl get storageclass

# Check available namespaces
kubectl get namespaces
```

---

## Architecture Overview

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Kubernetes Cluster                       │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │          vault-services Namespace                     │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ Ingress Controller (nginx)                       │ │ │
│  │  │ vault-credentials.example.com:443                │ │ │
│  │  └────────────────────┬─────────────────────────────┘ │ │
│  │                       │                                │ │
│  │  ┌────────────────────▼─────────────────────────────┐ │ │
│  │  │ Service (ClusterIP)                              │ │ │
│  │  │ vault-credential-service:80 → Pod:8080           │ │ │
│  │  └────────────────────┬─────────────────────────────┘ │ │
│  │                       │                                │ │
│  │   ┌───────────────────┼───────────────────┐            │ │
│  │   │                   │                   │            │ │
│  │  ┌▼──────────────┐   ┌▼──────────────┐   ┌▼──────────┐ │ │
│  │  │ Pod 1         │   │ Pod 2         │   │ Pod N     │ │ │
│  │  │ (Ready: 1/2)  │   │ (Ready: 1/2)  │   │           │ │ │
│  │  │               │   │               │   │           │ │ │
│  │  │ Container:    │   │ Container:    │   │ Container:│ │ │
│  │  │ vault-service │   │ vault-service │   │ vault-...│ │ │
│  │  │ Port: 8080    │   │ Port: 8080    │   │ Port:8080│ │ │
│  │  │               │   │               │   │           │ │ │
│  │  │ Resources:    │   │ Resources:    │   │ Resources:│ │ │
│  │  │ CPU: 100m     │   │ CPU: 100m     │   │ CPU: 100m│ │ │
│  │  │ Mem: 256Mi    │   │ Mem: 256Mi    │   │ Mem: 256Mi│ │ │
│  │  └───────────────┘   └───────────────┘   └───────────┘ │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ ConfigMap: vault-service-config                 │ │ │
│  │  │ • appsettings.json                              │ │ │
│  │  │ • Logging configuration                         │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ Secret: vault-credentials                       │ │ │
│  │  │ • vault-username (base64 encoded)               │ │ │
│  │  │ • vault-password (base64 encoded)               │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ HorizontalPodAutoscaler: vault-credential-...   │ │ │
│  │  │ • Min Replicas: 2                               │ │ │
│  │  │ • Max Replicas: 5                               │ │ │
│  │  │ • CPU Threshold: 70%                            │ │ │
│  │  │ • Memory Threshold: 80%                         │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ ServiceAccount: vault-credential-service        │ │ │
│  │  │ • Role: vault-credential-service                │ │ │
│  │  │ • Permissions: Get ConfigMap, Secret            │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ NetworkPolicy: Ingress/Egress rules             │ │ │
│  │  │ • Ingress: From Ingress Controller, same NS     │ │ │
│  │  │ • Egress: To Vault, DNS, Kubernetes API        │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │          External Services (Outside K8s)             │ │
│  │                                                       │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │ HashiCorp Vault                                  │ │ │
│  │  │ vault.vault.svc.cluster.local:8200               │ │ │
│  │  │ (or external vault:8200)                         │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Resource Relationships

```
Namespace: vault-services
│
├── ServiceAccount: vault-credential-service
│   └── Role: vault-credential-service
│       └── RoleBinding: vault-credential-service
│
├── ConfigMap: vault-service-config
│   └── Used by: Deployment Pods
│
├── Secret: vault-credentials
│   └── Used by: Deployment Pods
│
├── Deployment: vault-credential-service
│   ├── Replicas: 2-5 (auto-scaled)
│   ├── Pods: vault-credential-service-xxxxx
│   │   └── Containers: vault-service (port 8080)
│   └── Managed by: HorizontalPodAutoscaler
│
├── Service: vault-credential-service
│   └── Exposes: ClusterIP:80 → Pod:8080
│
├── Ingress: vault-credential-service
│   └── Routes: vault-credentials.example.com → Service
│
├── HorizontalPodAutoscaler: vault-credential-service
│   └── Scales: Deployment replicas based on metrics
│
├── PodDisruptionBudget: vault-credential-service
│   └── Ensures: Minimum 1 pod always available
│
└── NetworkPolicy: vault-credential-service
    └── Controls: Ingress/Egress traffic
```

---

## Pre-Deployment Checklist

### 1. Cluster Preparation

```bash
# ✅ Verify cluster access
kubectl cluster-info
kubectl get nodes

# ✅ Check cluster version compatibility
kubectl version

# ✅ Verify available resources
kubectl describe nodes | grep -E "Allocatable:|Allocated resources"

# ✅ Check storage availability
kubectl get sc

# ✅ Verify ingress controller is running
kubectl get pods -n ingress-nginx
# or
kubectl get pods -n kube-system | grep ingress
```

### 2. Image Registry Preparation

```bash
# ✅ Verify image is available in registry
docker pull pdasilva1/ecm-k8s-plugin:latest

# ✅ Check image size
docker image ls pdasilva1/ecm-k8s-plugin

# ✅ If using private registry, create image pull secret
kubectl create secret docker-registry regcred \
  --docker-server=<registry> \
  --docker-username=<username> \
  --docker-password=<password> \
  -n vault-services
```

### 3. Vault Configuration

```bash
# ✅ Verify Vault is accessible
curl -s http://vault.vault.svc.cluster.local:8200/v1/sys/health | jq .

# ✅ Verify userpass auth method is enabled
vault auth list | grep userpass

# ✅ Verify service account exists
vault read auth/userpass/users/vault-user

# ✅ Verify secret engine is enabled
vault secrets list | grep secret/

# ✅ Create test secret for validation
vault kv put secret/test/credentials \
  username="testuser" \
  password="testpass"
```

### 4. Network & DNS

```bash
# ✅ Verify DNS resolution
nslookup vault.vault.svc.cluster.local

# ✅ Verify network connectivity
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://vault.vault.svc.cluster.local:8200/v1/sys/health

# ✅ Check ingress DNS
nslookup vault-credentials.example.com

# ✅ Verify firewall rules (if applicable)
# Ensure ports 80, 443, 8080 are open if needed
```

### 5. RBAC & Permissions

```bash
# ✅ Verify admin access
kubectl auth can-i create namespaces --as=system:admin

# ✅ Verify RBAC API is enabled
kubectl api-resources | grep rbac

# ✅ Check existing roles (for reference)
kubectl get roles -A | head -10
```

---

## Installation Steps

### Step 1: Clone Repository

```bash
# Clone the repository
git clone https://github.com/pdasilva11/ecm-k8s-plugin.git
cd ecm-k8s-plugin

# Verify structure
ls -la k8s/
```

**Expected Files:**
```
k8s/
├── 01-namespace.yaml
├── 02-configmap.yaml
├── 03-secret.yaml
├── 04-deployment.yaml
├── 05-service.yaml
├── 06-ingress.yaml
├── 07-rbac.yaml
├── 08-hpa.yaml
├── 09-pdb.yaml
├── 10-networkpolicy.yaml
├── kustomization.yaml
└── README.md
```

### Step 2: Configure Vault Credentials

**Edit `k8s/03-secret.yaml`:**

```bash
# Option 1: Edit manually
vim k8s/03-secret.yaml

# Option 2: Use kubectl to create secret
kubectl create secret generic vault-credentials \
  --from-literal=vault-username="your-vault-user" \
  --from-literal=vault-password="your-vault-password" \
  -n vault-services \
  --dry-run=client -o yaml > k8s/03-secret.yaml
```

**Content to update:**
```yaml
stringData:
  vault-username: "your-vault-username"
  vault-password: "your-vault-password"
```

**Security Note:** These will be base64 encoded in the cluster. Use a secret management solution (Sealed Secrets, External Secrets Operator) for production.

### Step 3: Configure Vault Connection

**Edit `k8s/04-deployment.yaml` - Update Vault URL:**

```bash
# Find the Vault configuration section
grep -n "VaultConfig__BaseUrl" k8s/04-deployment.yaml

# Edit the file
vim k8s/04-deployment.yaml
```

**Update these environment variables:**

```yaml
env:
- name: VaultConfig__BaseUrl
  value: "http://vault.vault.svc.cluster.local:8200"  # Change if Vault is external
- name: VaultConfig__Username
  valueFrom:
    secretKeyRef:
      name: vault-credentials
      key: vault-username
- name: VaultConfig__Password
  valueFrom:
    secretKeyRef:
      name: vault-credentials
      key: vault-password
- name: VaultConfig__SecretsEngine
  value: "secret"  # Change if using different engine (e.g., "kv", "kv2")
```

**Vault URL Examples:**
```
Internal Vault in Kubernetes:
  http://vault.vault.svc.cluster.local:8200

External Vault (DNS):
  http://vault.example.com:8200

External Vault (IP):
  http://10.0.1.50:8200
```

### Step 4: Configure Ingress (Optional but Recommended)

**Edit `k8s/06-ingress.yaml`:**

```bash
# Update hostname
vim k8s/06-ingress.yaml
```

**Update these fields:**

```yaml
spec:
  tls:
  - hosts:
    - vault-credentials.example.com  # Change to your domain
    secretName: vault-service-tls
  rules:
  - host: vault-credentials.example.com  # Change to your domain
    http:
      paths:
      - path: /
        backend:
          service:
            name: vault-credential-service
```

**DNS Configuration:**
```bash
# Add DNS record (A or CNAME)
vault-credentials.example.com CNAME ingress.example.com

# Verify DNS resolution
nslookup vault-credentials.example.com
```

### Step 5: Deploy to Kubernetes

**Option A: Deploy All Resources at Once (Recommended)**

```bash
# Apply all manifests in the correct order
kubectl apply -f k8s/

# Verify deployment
kubectl get all -n vault-services

# Check pod status
kubectl get pods -n vault-services -w
```

**Option B: Deploy Step-by-Step (For Troubleshooting)**

```bash
# 1. Create namespace
kubectl apply -f k8s/01-namespace.yaml
kubectl get ns

# 2. Create ConfigMap and Secret
kubectl apply -f k8s/02-configmap.yaml
kubectl apply -f k8s/03-secret.yaml
kubectl get cm,secret -n vault-services

# 3. Create RBAC
kubectl apply -f k8s/07-rbac.yaml
kubectl get sa,role,rolebinding -n vault-services

# 4. Create Deployment
kubectl apply -f k8s/04-deployment.yaml
kubectl get deployment -n vault-services -w

# 5. Create Service
kubectl apply -f k8s/05-service.yaml
kubectl get svc -n vault-services

# 6. Create HPA and PDB
kubectl apply -f k8s/08-hpa.yaml
kubectl apply -f k8s/09-pdb.yaml
kubectl get hpa,pdb -n vault-services

# 7. Create NetworkPolicy
kubectl apply -f k8s/10-networkpolicy.yaml
kubectl get networkpolicy -n vault-services

# 8. Create Ingress
kubectl apply -f k8s/06-ingress.yaml
kubectl get ingress -n vault-services
```

**Option C: Use Kustomize**

```bash
# Build and apply using kustomize
kubectl apply -k k8s/

# Or view the generated YAML
kubectl kustomize k8s/ | less
```

### Step 6: Verify Deployment

```bash
# Check all resources created
kubectl get all -n vault-services

# Expected output:
# NAME                                        READY   STATUS    RESTARTS   AGE
# pod/vault-credential-service-xxxxx-xxxxx   1/1     Running   0          2m
# pod/vault-credential-service-xxxxx-xxxxx   1/1     Running   0          2m
#
# NAME                                  TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)
# service/vault-credential-service      ClusterIP   10.96.xxx.xxx    <none>        80/TCP
#
# NAME                                         READY   UP-TO-DATE   AVAILABLE   AGE
# deployment.apps/vault-credential-service    2/2     2            2           2m
#
# NAME                                                    DESIRED   CURRENT   READY   AGE
# replicaset.apps/vault-credential-service-xxxxx        2         2         2       2m
#
# NAME                                                       REFERENCE                             TARGETS            MINPODS   MAXPODS   REPLICAS   AGE
# horizontalpodautoscaler.autoscaling/vault-credential...   Deployment/vault-credential-service   <unknown>/70%      2         5         2          2m

# Check deployment status in detail
kubectl describe deployment vault-credential-service -n vault-services

# Check pod logs
kubectl logs -f deployment/vault-credential-service -n vault-services
```

---

## Configuration Guide

### Environment Variables

All configuration is done via environment variables in the Deployment:

```yaml
env:
# Vault Configuration
- name: VaultConfig__BaseUrl
  value: "http://vault.vault.svc.cluster.local:8200"

- name: VaultConfig__Username
  valueFrom:
    secretKeyRef:
      name: vault-credentials
      key: vault-username

- name: VaultConfig__Password
  valueFrom:
    secretKeyRef:
      name: vault-credentials
      key: vault-password

- name: VaultConfig__SecretsEngine
  value: "secret"

# ASP.NET Core Configuration
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"

- name: ASPNETCORE_URLS
  value: "http://+:8080"

# Logging Configuration
- name: Logging__LogLevel__Default
  value: "Information"  # Debug, Information, Warning, Error
```

### ConfigMap Configuration

**Edit `k8s/02-configmap.yaml` for advanced settings:**

```yaml
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "Microsoft.AspNetCore": "Information"
        }
      },
      "AllowedHosts": "*"
    }
```

### Resource Limits & Requests

**Adjust in `k8s/04-deployment.yaml`:**

```yaml
resources:
  requests:
    cpu: 100m           # Minimum CPU
    memory: 256Mi       # Minimum Memory
  limits:
    cpu: 500m           # Maximum CPU
    memory: 512Mi       # Maximum Memory
```

**Recommendations by Environment:**
- **Development:** requests: 50m/128Mi, limits: 200m/256Mi
- **Staging:** requests: 100m/256Mi, limits: 500m/512Mi
- **Production:** requests: 200m/512Mi, limits: 1000m/1Gi

### Replica Configuration

**Adjust in `k8s/08-hpa.yaml`:**

```yaml
spec:
  minReplicas: 2      # Minimum pods running
  maxReplicas: 5      # Maximum pods when scaling up
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70   # Scale up at 70% CPU
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80   # Scale up at 80% Memory
```

### Health Check Tuning

**Adjust in `k8s/04-deployment.yaml`:**

```yaml
startupProbe:
  httpGet:
    path: /api/credentials/health
    port: http
  initialDelaySeconds: 10   # Wait before first probe
  periodSeconds: 5          # Check every 5 seconds
  timeoutSeconds: 3         # Timeout per probe
  failureThreshold: 10      # Fail after 10 failed attempts

livenessProbe:
  httpGet:
    path: /api/credentials/health
    port: http
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 3
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /api/credentials/health
    port: http
  initialDelaySeconds: 10
  periodSeconds: 5
  timeoutSeconds: 2
  failureThreshold: 2
```

---

## Verification & Testing

### 1. Verify Pod Status

```bash
# Get pod details
kubectl get pods -n vault-services -o wide

# Describe a specific pod for detailed info
kubectl describe pod <pod-name> -n vault-services

# Check pod events
kubectl get events -n vault-services --sort-by='.lastTimestamp'
```

### 2. Test Service Connectivity

```bash
# Port forward to the service
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80 &

# Test endpoints
curl http://localhost:8080/api/credentials/health
curl http://localhost:8080/api/credentials
curl http://localhost:8080/api/credentials/secret/test/credentials

# Clean up port forward
pkill -f "port-forward"
```

### 3. Execute into Pod

```bash
# Get pod name
POD=$(kubectl get pods -n vault-services -l app=vault-credential-service -o jsonpath='{.items[0].metadata.name}')

# Execute shell into pod
kubectl exec -it $POD -n vault-services -- /bin/sh

# Inside pod:
curl http://localhost:8080/api/credentials/health
echo $VaultConfig__BaseUrl
```

### 4. Test from Another Pod

```bash
# Create a test pod
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -n vault-services -- \
  curl http://vault-credential-service/api/credentials/health

# Or with a longer-running container for multiple tests
kubectl run debug --image=curlimages/curl --restart=Never -n vault-services -- sleep 3600

# Connect to it
kubectl exec -it debug -n vault-services -- /bin/sh

# Inside:
curl http://vault-credential-service:80/api/credentials/health
curl http://vault-credential-service:80/api/credentials/secret/test/credentials
```

### 5. Complete Integration Test

```bash
#!/bin/bash

echo "=== Vault Credential Service Integration Test ==="

NS="vault-services"

# 1. Check deployment
echo "1. Checking deployment status..."
kubectl get deployment -n $NS vault-credential-service
kubectl rollout status deployment/vault-credential-service -n $NS

# 2. Check pods
echo "2. Checking pod status..."
kubectl get pods -n $NS -l app=vault-credential-service

# 3. Check service
echo "3. Checking service..."
kubectl get svc -n $NS vault-credential-service

# 4. Port forward and test
echo "4. Testing API endpoints..."
kubectl port-forward -n $NS svc/vault-credential-service 8080:80 &
PF_PID=$!
sleep 2

echo "Testing health endpoint..."
curl -s http://localhost:8080/api/credentials/health | jq .

echo "Testing service info endpoint..."
curl -s http://localhost:8080/api/credentials | jq .

echo "Testing secret retrieval..."
curl -s http://localhost:8080/api/credentials/secret/test/credentials | jq .

kill $PF_PID

echo "✅ Integration test complete"
```

---

## Monitoring & Logging

### 1. View Logs

```bash
# Stream logs from deployment
kubectl logs -f deployment/vault-credential-service -n vault-services

# View logs from specific pod
kubectl logs <pod-name> -n vault-services

# View logs from all pods with app label
kubectl logs -l app=vault-credential-service -n vault-services --all-containers=true

# View previous logs (if pod crashed)
kubectl logs <pod-name> -n vault-services --previous

# View logs with timestamps
kubectl logs <pod-name> -n vault-services --timestamps=true

# View last 100 lines
kubectl logs <pod-name> -n vault-services --tail=100

# Stream logs from all pods
kubectl logs -l app=vault-credential-service -n vault-services -f --all-containers=true
```

### 2. Prometheus Metrics

**If using Prometheus:**

```bash
# Metrics are exposed on /metrics endpoint
kubectl port-forward -n vault-services svc/vault-credential-service 8080:80

# Scrape metrics
curl http://localhost:8080/metrics

# Common metrics:
# - http_requests_total
# - http_request_duration_seconds
# - http_server_requests_seconds
```

### 3. Resource Usage

```bash
# Check current resource usage
kubectl top pods -n vault-services
kubectl top nodes

# Get resource requests vs limits
kubectl get pods -n vault-services -o json | \
  jq '.items[] | {name: .metadata.name, resources: .spec.containers[].resources}'
```

### 4. HPA Status

```bash
# Check autoscaler status
kubectl get hpa -n vault-services

# Detailed HPA info
kubectl describe hpa vault-credential-service -n vault-services

# Watch HPA scaling
kubectl get hpa -n vault-services -w
```

### 5. Events Monitoring

```bash
# Get recent events
kubectl get events -n vault-services --sort-by='.lastTimestamp'

# Watch events in real-time
kubectl get events -n vault-services -w

# Filter events by type
kubectl get events -n vault-services --field-selector type=Warning

# Get events for specific resource
kubectl describe pod <pod-name> -n vault-services | grep -A 20 "Events:"
```

---

## Scaling & Performance

### Horizontal Scaling (HPA)

The HorizontalPodAutoscaler automatically scales the deployment based on CPU and memory usage:

```yaml
minReplicas: 2      # Always keep 2 pods
maxReplicas: 5      # Never exceed 5 pods
Triggers:
  - CPU > 70%       # Scale up
  - Memory > 80%    # Scale up
```

**Manual Scaling:**

```bash
# Scale manually
kubectl scale deployment vault-credential-service \
  --replicas=3 \
  -n vault-services

# Check current replicas
kubectl get deployment vault-credential-service -n vault-services -o wide
```

### Vertical Scaling (Resource Limits)

Adjust CPU and memory limits in the Deployment:

```yaml
resources:
  requests:
    cpu: 100m       # Initial allocation
    memory: 256Mi
  limits:
    cpu: 500m       # Maximum allowed
    memory: 512Mi
```

### Performance Tuning

```bash
# 1. Check pod density
kubectl get nodes -o json | jq '.items[] | {name: .metadata.name, pods: .status.allocatable.pods}'

# 2. Check node capacity
kubectl describe node <node-name> | grep -A 5 "Capacity:"

# 3. Adjust pod disruption budget if scaling is slow
kubectl edit pdb vault-credential-service -n vault-services

# 4. Monitor scaling decisions
kubectl get hpa vault-credential-service -n vault-services -w

# 5. Check for pending pods (scaling bottleneck)
kubectl get pods -n vault-services --field-selector=status.phase=Pending
```

---

## Backup & Recovery

### ConfigMap Backup

```bash
# Backup ConfigMap
kubectl get configmap vault-service-config -n vault-services -o yaml > configmap-backup.yaml

# Restore ConfigMap
kubectl apply -f configmap-backup.yaml

# Verify
kubectl get configmap vault-service-config -n vault-services -o yaml
```

### Deployment Backup

```bash
# Backup entire deployment
kubectl get deployment vault-credential-service -n vault-services -o yaml > deployment-backup.yaml

# Backup all resources in namespace
kubectl get all -n vault-services -o yaml > namespace-backup.yaml

# Restore
kubectl apply -f namespace-backup.yaml
```

### Recovery from Crash

```bash
# Check rollout history
kubectl rollout history deployment/vault-credential-service -n vault-services

# Rollback to previous version
kubectl rollout undo deployment/vault-credential-service -n vault-services

# Rollback to specific revision
kubectl rollout undo deployment/vault-credential-service \
  --to-revision=2 \
  -n vault-services

# Check rollout status
kubectl rollout status deployment/vault-credential-service -n vault-services
```

### Persistent Data Recovery

```bash
# Describe PVC (if used)
kubectl describe pvc -n vault-services

# Check snapshot status
kubectl get volumesnapshot -n vault-services

# Restore from snapshot
kubectl apply -f volumesnapshot-restore.yaml
```

---

## Troubleshooting

### 1. Pod Fails to Start

```bash
# Check pod status
kubectl describe pod <pod-name> -n vault-services

# Common causes and solutions:

# ❌ ImagePullBackOff
# Solution: Check image exists and registry credentials
kubectl get pod <pod-name> -n vault-services -o jsonpath='{.status.containerStatuses[0].state}'

# ❌ CrashLoopBackOff
# Solution: Check logs
kubectl logs <pod-name> -n vault-services --previous

# ❌ Pending (waiting for resources)
# Solution: Check node capacity
kubectl describe nodes
kubectl top nodes

# ❌ Pending (PVC not bound)
# Solution: Check storage class
kubectl get pvc -n vault-services
kubectl describe pvc <pvc-name> -n vault-services
```

### 2. Service Unreachable

```bash
# Check service exists
kubectl get svc -n vault-services vault-credential-service

# Check service endpoints
kubectl get endpoints -n vault-services vault-credential-service

# Check DNS resolution
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  nslookup vault-credential-service.vault-services.svc.cluster.local

# Check network policies
kubectl get networkpolicy -n vault-services
kubectl describe networkpolicy vault-credential-service -n vault-services

# Test service directly
kubectl port-forward svc/vault-credential-service 8080:80 -n vault-services
curl http://localhost:8080/api/credentials/health
```

### 3. Vault Connection Issues

```bash
# Test Vault accessibility from pod
kubectl exec -it <pod-name> -n vault-services -- \
  curl http://vault.vault.svc.cluster.local:8200/v1/sys/health

# Check Vault URL configuration
kubectl get deployment vault-credential-service -n vault-services -o json | \
  jq '.spec.template.spec.containers[0].env[] | select(.name=="VaultConfig__BaseUrl")'

# Check Vault credentials in secret
kubectl get secret vault-credentials -n vault-services -o json | \
  jq '.data | map_values(@base64d)'

# Test Vault authentication
kubectl exec -it <pod-name> -n vault-services -- /bin/sh
# Inside pod:
curl -X POST http://vault.vault.svc.cluster.local:8200/v1/auth/userpass/login/vault-user \
  -d '{"password":"vault-password"}'
```

### 4. High Memory/CPU Usage

```bash
# Check resource usage
kubectl top pod <pod-name> -n vault-services

# Check configured limits
kubectl get pod <pod-name> -n vault-services -o json | \
  jq '.spec.containers[0].resources'

# Increase resource limits
kubectl set resources deployment vault-credential-service \
  --limits=cpu=1000m,memory=1Gi \
  --requests=cpu=200m,memory=512Mi \
  -n vault-services

# Monitor scaling
kubectl get hpa vault-credential-service -n vault-services -w
```

### 5. Slow Scaling

```bash
# Check HPA status
kubectl describe hpa vault-credential-service -n vault-services

# Check metrics
kubectl get hpa vault-credential-service -n vault-services

# Check for pending pods
kubectl get pods -n vault-services --field-selector=status.phase=Pending

# Adjust HPA cooldown
kubectl edit hpa vault-credential-service -n vault-services
# Reduce scaleDownStabilizationWindowSeconds and periodSeconds

# Check resource availability
kubectl describe nodes | grep -A 5 "Allocatable:"
```

### 6. SSL/TLS Certificate Issues

```bash
# Check ingress TLS status
kubectl describe ingress vault-credential-service -n vault-services

# Check certificate existence
kubectl get certificate -n vault-services

# Check cert-manager status (if using)
kubectl get pods -n cert-manager
kubectl describe certificate vault-service-tls -n vault-services

# Manually issue certificate
kubectl create secret tls vault-service-tls \
  --cert=path/to/cert.crt \
  --key=path/to/key.key \
  -n vault-services
```

---

## Maintenance

### Regular Updates

```bash
# Update image
kubectl set image deployment/vault-credential-service \
  vault-service=pdasilva1/ecm-k8s-plugin:v1.1 \
  -n vault-services

# Monitor rollout
kubectl rollout status deployment/vault-credential-service -n vault-services

# Check history
kubectl rollout history deployment/vault-credential-service -n vault-services
```

### Configuration Updates

```bash
# Update ConfigMap
kubectl edit configmap vault-service-config -n vault-services

# Update Secret
kubectl create secret generic vault-credentials \
  --from-literal=vault-username="newuser" \
  --from-literal=vault-password="newpass" \
  -n vault-services \
  --dry-run=client -o yaml | kubectl apply -f -

# Restart pods to apply changes
kubectl rollout restart deployment/vault-credential-service -n vault-services
```

### Cleanup

```bash
# Remove specific resource
kubectl delete service vault-credential-service -n vault-services

# Remove entire deployment
kubectl delete deployment vault-credential-service -n vault-services

# Remove namespace (deletes all resources in it)
kubectl delete namespace vault-services

# Remove lingering pods
kubectl get pods -n vault-services
kubectl delete pod <pod-name> -n vault-services
```

---

## Security Hardening

### RBAC Hardening

```bash
# Verify RBAC is enforced
kubectl get role -n vault-services vault-credential-service

# Further restrict permissions if needed
kubectl edit role vault-credential-service -n vault-services
```

### NetworkPolicy Hardening

```bash
# Verify NetworkPolicy is in place
kubectl get networkpolicy -n vault-services

# Test blocked traffic
kubectl run -it --rm test --image=curlimages/curl --restart=Never -n default -- \
  curl http://vault-credential-service.vault-services
# Should be blocked (403 or timeout)
```

### Secret Security

```bash
# Never commit secrets to git - use Sealed Secrets
# Install Sealed Secrets controller
kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.18.0/controller.yaml

# Create sealed secret
echo -n "password" | kubectl create secret generic vault-credentials \
  --dry-run=client --from-file=vault-password=/dev/stdin -o json | \
  kubeseal -f -

# Or use External Secrets Operator for dynamic secret management
```

### Pod Security

```bash
# Verify non-root user is enforced
kubectl get pod <pod-name> -n vault-services -o json | \
  jq '.spec.securityContext'

# Check container security context
kubectl get pod <pod-name> -n vault-services -o json | \
  jq '.spec.containers[0].securityContext'
```

---

## Advanced Configuration

### Using Helm

```bash
# Create Helm chart structure (optional)
helm create ecm-k8s-plugin

# Deploy using Helm
helm install vault-service ./ecm-k8s-plugin \
  -n vault-services \
  --create-namespace \
  -f values.yaml
```

### Using Kustomize Overlays

```bash
# Create overlay structure
kustomize/
├── base/
│   ├── deployment.yaml
│   ├── service.yaml
│   └── kustomization.yaml
└── overlays/
    ├── dev/
    │   ├── kustomization.yaml
    │   └── patch.yaml
    ├── staging/
    └── prod/

# Deploy specific overlay
kubectl apply -k kustomize/overlays/prod/
```

### Multi-Region Deployment

```bash
# Deploy to multiple clusters
for CLUSTER in us-east us-west eu-west; do
  kubectl config use-context $CLUSTER
  kubectl apply -f k8s/
done
```

### GitOps with ArgoCD

```bash
# Install ArgoCD
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

# Create ArgoCD Application
kubectl apply -f - <<EOF
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: vault-service
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/pdasilva11/ecm-k8s-plugin
    targetRevision: HEAD
    path: k8s
  destination:
    server: https://kubernetes.default.svc
    namespace: vault-services
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
EOF
```

---

## Conclusion

This deployment guide provides a comprehensive approach to deploying the Vault Credential Injection Service on Kubernetes. Follow the steps carefully, test thoroughly, and monitor the deployment for optimal performance and security.

**Key Takeaways:**
- ✅ Always validate prerequisites before deployment
- ✅ Configure Vault credentials securely
- ✅ Monitor logs and resource usage regularly
- ✅ Use RBAC and NetworkPolicies for security
- ✅ Scale based on metrics, not guesses
- ✅ Keep backups and practice recovery
- ✅ Update and maintain regularly

**Support & Documentation:**
- GitHub: https://github.com/pdasilva11/ecm-k8s-plugin
- Docker Hub: https://hub.docker.com/r/pdasilva1/ecm-k8s-plugin
- API Documentation: See `API_FLOW_DIAGRAM.md`

