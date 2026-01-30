# Vault Credential Injection Service - API Flow Diagram

## System Architecture & Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         PRA (Privileged Remote Access)                  │
│                                                                         │
│  "I need credentials to access db/prod/admin"                          │
└────────────────────────────────┬────────────────────────────────────────┘
                                  │
                                  │ HTTP Request
                                  │ GET /api/credentials/secret/db/prod/admin
                                  ▼
        ┌─────────────────────────────────────────────────────────────┐
        │     Vault Credential Injection Service (K8s Pod)            │
        │              Port: 8080 / Protocol: HTTP                    │
        │                                                             │
        │  ┌──────────────────────────────────────────────────────┐  │
        │  │ CredentialsController                                │  │
        │  │                                                      │  │
        │  │ Routes:                                              │  │
        │  │ • GET  /api/credentials/health                       │  │
        │  │ • GET  /api/credentials/secret/{secretPath}          │  │
        │  │ • GET  /api/credentials                              │  │
        │  └──────┬───────────────────────────────────────────────┘  │
        │         │                                                   │
        │         ▼                                                   │
        │  ┌──────────────────────────────────────────────────────┐  │
        │  │ VaultService                                         │  │
        │  │                                                      │  │
        │  │ Methods:                                             │  │
        │  │ • AuthenticateAsync()                                │  │
        │  │ • GetSecretAsync(secretPath)                         │  │
        │  │ • HealthCheckAsync()                                 │  │
        │  └──────┬───────────────────────────────────────────────┘  │
        │         │                                                   │
        └─────────┼───────────────────────────────────────────────────┘
                  │
                  │ 1. Authenticate with userpass
                  │ POST /v1/auth/userpass/login/{username}
                  │
                  │ 2. Retrieve Secret
                  │ GET /v1/secret/data/{secretPath}
                  │ (with X-Vault-Token header)
                  │
                  ▼
        ┌─────────────────────────────────────────────────────────────┐
        │         HashiCorp Vault (vault.vault.svc:8200)              │
        │                                                             │
        │  • Authentication Engine (userpass)                         │
        │  • Secrets Engine (KV v2)                                   │
        │  • Audit Logging                                            │
        └─────────────────────────────────────────────────────────────┘
                  │
                  │ Response: Secret with credentials
                  ▼
        ┌─────────────────────────────────────────────────────────────┐
        │     Service Returns JSON Response                           │
        │                                                             │
        │  {                                                          │
        │    "path": "db/prod/admin",                                 │
        │    "username": "dbadmin",                                   │
        │    "password": "SecurePassword123!",                        │
        │    "data": {                                                │
        │      "host": "db.prod.example.com",                         │
        │      "port": "5432",                                        │
        │      "database": "production"                               │
        │    }                                                        │
        │  }                                                          │
        └─────────────────────────────────────────────────────────────┘
                  │
                  ▼
        ┌─────────────────────────────────────────────────────────────┐
        │  PRA Injects Credentials into Session                       │
        │                                                             │
        │  ✅ User authenticated                                      │
        │  ✅ Database session established                            │
        │  ✅ Credentials securely injected                           │
        └─────────────────────────────────────────────────────────────┘
```

---

## API Endpoints Reference

### 1️⃣ Health Check Endpoint

**Purpose:** Check if service and Vault connectivity are operational

```
GET /api/credentials/health
```

**Request:**
```http
GET http://vault-service:8080/api/credentials/health HTTP/1.1
Accept: application/json
```

**Response (200 - Healthy):**
```json
{
  "status": "healthy",
  "message": "Vault connection is operational"
}
```

**Response (503 - Unhealthy):**
```json
{
  "status": "unhealthy",
  "message": "Vault is not responding"
}
```

**Use Cases:**
- Kubernetes liveness probes
- Load balancer health checks
- Monitoring systems

---

### 2️⃣ Get Secret Endpoint

**Purpose:** Retrieve credentials from Vault for injection into PRA

```
GET /api/credentials/secret/{secretPath}
```

**Parameters:**
- `secretPath` (path parameter, required): Path to secret in Vault
  - Example: `db/prod/admin`
  - Example: `app/api/token`
  - Example: `windows/domain/service-account`

**Request:**
```http
GET http://vault-service:8080/api/credentials/secret/db/prod/admin HTTP/1.1
Accept: application/json
```

**Response (200 - Success):**
```json
{
  "path": "db/prod/admin",
  "username": "dbadmin",
  "password": "SuperSecret123!",
  "data": {
    "username": "dbadmin",
    "password": "SuperSecret123!",
    "host": "db.prod.example.com",
    "port": "5432",
    "database": "production",
    "ssl": "require"
  }
}
```

**Response (500 - Error):**
```json
{
  "error": "Failed to retrieve secret",
  "details": "Vault authentication failed: 401"
}
```

**Common Secret Paths:**
```
Database:
  db/prod/admin
  db/prod/readonly
  db/staging/admin

Applications:
  app/api/key
  app/db/connection-string

Windows:
  windows/domain/admin
  windows/service-accounts/iis

API Keys:
  external/stripe/key
  external/aws/credentials
```

**Use Cases:**
- PRA requesting credentials for database access
- Application requesting API keys
- Service-to-service authentication
- Privileged account injection

---

### 3️⃣ Service Info Endpoint

**Purpose:** Get service information and available endpoints

```
GET /api/credentials
```

**Request:**
```http
GET http://vault-service:8080/api/credentials HTTP/1.1
Accept: application/json
```

**Response (200):**
```json
{
  "service": "Vault Credential Injection Service",
  "version": "1.0",
  "endpoints": {
    "getSecret": "/api/credentials/secret/{secretPath}",
    "health": "/api/credentials/health"
  }
}
```

**Use Cases:**
- Service discovery
- API documentation
- Integration validation

---

## Request/Response Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRA Makes Request                            │
├─────────────────────────────────────────────────────────────────┤
│  GET /api/credentials/secret/db/prod/admin                      │
│  Host: vault-service:8080                                       │
│  Accept: application/json                                       │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │ CredentialsController      │
        │ receives request           │
        └────────┬───────────────────┘
                 │
                 ▼
        ┌────────────────────────────────────┐
        │ Extract secretPath = "db/prod/admin"
        └────────┬───────────────────────────┘
                 │
                 ▼
        ┌────────────────────────────────────┐
        │ Call VaultService.GetSecretAsync() │
        └────────┬───────────────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
    ┌──────────────┐  ┌──────────────────────┐
    │ Authenticate │  │ Get Secret from Vault│
    │ to Vault     │  │                      │
    │              │  │ POST /v1/auth/      │
    │ POST /v1/auth│  │ userpass/login/user │
    │ /userpass/   │  │                      │
    │ login/user   │  │ GET /v1/secret/data/│
    │              │  │ db/prod/admin        │
    └──────┬───────┘  └──────┬───────────────┘
           │                 │
           └────────┬────────┘
                    ▼
        ┌────────────────────────────────┐
        │ Parse Vault Response           │
        │                                │
        │ Extract:                       │
        │ - username: dbadmin            │
        │ - password: xxxxxx             │
        │ - all other fields             │
        └────────┬───────────────────────┘
                 │
                 ▼
        ┌────────────────────────────────┐
        │ Return JSON Response to PRA    │
        │                                │
        │ {                              │
        │   "path": "db/prod/admin",     │
        │   "username": "dbadmin",       │
        │   "password": "xxxxxx",        │
        │   "data": { ... }              │
        │ }                              │
        └────────┬───────────────────────┘
                 │
                 ▼
        ┌────────────────────────────────┐
        │ PRA Receives Credentials       │
        │                                │
        │ Uses for Session Injection     │
        └────────────────────────────────┘
```

---

## API Response Status Codes

| Code | Meaning | When It Happens |
|------|---------|-----------------|
| **200** | Success | Secret retrieved successfully |
| **400** | Bad Request | Invalid secret path format |
| **401** | Unauthorized | Vault authentication failed |
| **403** | Forbidden | Service lacks permission to read secret |
| **404** | Not Found | Secret doesn't exist in Vault |
| **500** | Internal Error | Service error or Vault unreachable |
| **503** | Service Unavailable | Health check indicates unhealthy state |

---

## Example Curl Commands

### Check Service Health
```bash
curl -X GET http://localhost:8080/api/credentials/health \
  -H "Accept: application/json"
```

### Get Service Info
```bash
curl -X GET http://localhost:8080/api/credentials \
  -H "Accept: application/json"
```

### Retrieve Database Credentials
```bash
curl -X GET http://localhost:8080/api/credentials/secret/db/prod/admin \
  -H "Accept: application/json" \
  -s | jq .
```

### Retrieve API Key
```bash
curl -X GET http://localhost:8080/api/credentials/secret/app/api/stripe-key \
  -H "Accept: application/json" \
  -s | jq .
```

### With Error Handling
```bash
curl -X GET http://localhost:8080/api/credentials/secret/invalid/path \
  -H "Accept: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq .
```

---

## PRA Integration Examples

### Example 1: Database Connection

**Request:**
```bash
curl http://vault-service:8080/api/credentials/secret/db/prod/admin
```

**Response:**
```json
{
  "path": "db/prod/admin",
  "username": "dbadmin",
  "password": "P@ssw0rd123!",
  "data": {
    "host": "db.prod.example.com",
    "port": "5432",
    "database": "production"
  }
}
```

**PRA Action:**
```
Connect to: db.prod.example.com:5432
Database: production
Username: dbadmin
Password: P@ssw0rd123!
```

---

### Example 2: API Key Injection

**Request:**
```bash
curl http://vault-service:8080/api/credentials/secret/external/stripe/api-key
```

**Response:**
```json
{
  "path": "external/stripe/api-key",
  "username": "stripe-api",
  "password": "sk_live_1234567890abcdef",
  "data": {
    "api_key": "sk_live_1234567890abcdef",
    "env": "production"
  }
}
```

**PRA Action:**
```
Use header: Authorization: Bearer sk_live_1234567890abcdef
For Stripe API calls
```

---

### Example 3: Windows Service Account

**Request:**
```bash
curl http://vault-service:8080/api/credentials/secret/windows/domain/iis-service
```

**Response:**
```json
{
  "path": "windows/domain/iis-service",
  "username": "DOMAIN\\iis-svc",
  "password": "C0mpl3xP@ss!",
  "data": {
    "domain": "DOMAIN",
    "username": "iis-svc",
    "password": "C0mpl3xP@ss!"
  }
}
```

**PRA Action:**
```
Inject credentials for IIS Application Pool
Username: DOMAIN\iis-svc
Password: C0mpl3xP@ss!
```

---

## Data Flow Summary

```
PRA Request
    ↓
Service Receives Request
    ↓
Extract Secret Path
    ↓
Authenticate to Vault (once per request)
    ↓
Request Secret from Vault
    ↓
Parse Vault Response
    ↓
Return JSON to PRA
    ↓
PRA Uses Credentials
    ↓
Session Established ✅
```

---

## Configuration for Kubernetes

```yaml
# Environment variables set in Deployment
VaultConfig__BaseUrl: "http://vault.vault.svc.cluster.local:8200"
VaultConfig__Username: "vault-user"
VaultConfig__Password: "vault-password"
VaultConfig__SecretsEngine: "secret"

# Service exposed on port 8080
Service Port: 80 → Pod Port: 8080

# Accessible within cluster as:
http://vault-credential-service.vault-services.svc.cluster.local
http://vault-credential-service.vault-services
http://vault-credential-service (if in same namespace)
```

---

## Monitoring & Observability

**Health Check Integration:**
```yaml
livenessProbe:
  httpGet:
    path: /api/credentials/health
    port: 8080

readinessProbe:
  httpGet:
    path: /api/credentials/health
    port: 8080
```

**Logging:**
- All requests logged with timestamp and path
- Authentication attempts logged
- Errors logged with details

**Metrics:**
- HTTP endpoint hits
- Response times
- Error rates
- Vault connectivity status

---

## Security Considerations

✅ **Implemented:**
- HTTPS support (configurable)
- Non-root container user
- Resource limits
- Network policies
- Secret stored in K8s Secrets

⚠️ **Recommendations:**
- Enable TLS for Vault communication
- Use Sealed Secrets for credential storage
- Implement API rate limiting
- Enable audit logging
- Restrict network access
- Regular secret rotation

