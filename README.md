# Enterprise C# Reverse Proxy

A production-ready, dynamically configurable reverse proxy built on **ASP.NET Core** and **YARP** (Yet Another Reverse Proxy).
Configuration is managed centrally by a **Control Plane** API and propagated to one or more **Proxy Nodes** via long-polling — no restarts required.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Clients                                 │
└──────────────────────────────┬──────────────────────────────────┘
                               │  HTTP traffic
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Proxy Node (YARP)  :5000                     │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  ConfigWatcherService  ──long-poll──►  Control Plane     │   │
│  │  ControlPlaneConfigProvider  (IProxyConfigProvider)      │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬──────────────────────────────────┘
                               │  forwards to upstream
                               ▼
               ┌───────────────────────────────┐
               │       Backend Services        │
               │  (Cluster destinations)       │
               └───────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  Control Plane API  :5100                       │
│                                                                 │
│   POST/PUT/DELETE  routes, clusters, destinations               │
│   GET  /api/v1/config/snapshot   ──► full config dump          │
│   GET  /api/v1/config/watch      ──► long-poll change feed     │
│   Swagger UI available at  http://localhost:5100/               │
└─────────────────────────────────────────────────────────────────┘
```

### Data flow

1. An operator calls the **Control Plane REST API** to create/update routes and clusters.
2. The control plane bumps its internal **version counter** and unblocks all waiting watchers.
3. Each **Proxy Node** receives the new `ProxySnapshot` from its long-poll request and feeds it into YARP's `IProxyConfigProvider`.
4. YARP reloads routing tables **without restarting** the process.

---

## Project Structure

```
EnterpriseCsharpReverseProxy/
├── EnterpriseCsharpReverseProxy.sln
├── src/
│   ├── ControlPlane/
│   │   ├── Controllers/
│   │   │   ├── RoutesController.cs        # CRUD for proxy routes
│   │   │   ├── ClustersController.cs      # CRUD for clusters & destinations
│   │   │   └── ConfigController.cs        # Snapshot + long-poll watch endpoint
│   │   ├── Middleware/
│   │   │   └── ApiKeyAuthMiddleware.cs    # X-Api-Key authentication
│   │   ├── Models/
│   │   │   ├── RouteConfig.cs             # Route, match, transform, auth models
│   │   │   ├── ClusterConfig.cs           # Cluster, session affinity, HTTP client
│   │   │   ├── DestinationConfig.cs       # Individual backend destination
│   │   │   ├── HealthCheckConfig.cs       # Active & passive health check config
│   │   │   ├── LoadBalancingConfig.cs     # Load balancing policy enum
│   │   │   └── ProxySnapshot.cs           # Versioned full-config snapshot
│   │   └── Services/
│   │       ├── IConfigurationStore.cs
│   │       ├── InMemoryConfigurationStore.cs  # Thread-safe; swap for DB
│   │       ├── IConfigChangeNotifier.cs
│   │       ├── ConfigChangeNotifier.cs        # CancellationToken-based fan-out
│   │       └── ConfigValidationService.cs
│   └── ProxyNode/
│       └── Services/
│           ├── ControlPlaneClient.cs          # HTTP client wrapper
│           ├── ControlPlaneConfigProvider.cs  # YARP IProxyConfigProvider impl
│           └── ConfigWatcherService.cs        # BackgroundService long-poll loop
└── tests/
    └── ControlPlane.Tests/
        └── Services/
            ├── InMemoryConfigurationStoreTests.cs
            └── ConfigValidationServiceTests.cs
```

---

## Prerequisites

| Tool | Minimum version |
|---|---|
| .NET SDK | 9.0 |
| (optional) Docker | 24+ |

---

## Getting Started

### 1 — Clone & restore

```bash
git clone <repo-url>
cd EnterpriseCsharpReverseProxy
dotnet restore
```

### 2 — Configure the API key

Edit `src/ControlPlane/appsettings.json` and set a strong key:

```json
{
  "ControlPlane": {
    "ApiKey": "your-secret-key-here"
  }
}
```

Set the same key in `src/ProxyNode/appsettings.json`:

```json
{
  "ProxyNode": {
    "ControlPlaneUrl": "http://localhost:5100",
    "ControlPlaneApiKey": "your-secret-key-here"
  }
}
```

> **Security note:** Never commit real secrets. Use environment variables or a secrets manager in production:
> `export ControlPlane__ApiKey=my-secret`

### 3 — Run the Control Plane

```bash
cd src/ControlPlane
dotnet run
# Swagger UI → http://localhost:5100
```

### 4 — Run a Proxy Node

```bash
cd src/ProxyNode
dotnet run
# Proxy listening → http://localhost:5000
```

### 5 — Register a cluster and route

```bash
API=http://localhost:5100
KEY=your-secret-key-here

# Create a cluster pointing at httpbin.org
curl -s -X PUT "$API/api/v1/clusters/demo-cluster" \
  -H "X-Api-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "clusterId": "demo-cluster",
    "loadBalancing": { "policy": "RoundRobin" },
    "destinations": {
      "d1": { "destinationId": "d1", "address": "https://httpbin.org" }
    }
  }'

# Create a route that matches /get/** and forwards to the cluster
curl -s -X PUT "$API/api/v1/routes/demo-route" \
  -H "X-Api-Key: $KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "routeId": "demo-route",
    "clusterId": "demo-cluster",
    "order": 1,
    "match": { "path": "/get/{**catch-all}" }
  }'

# Test the proxy
curl http://localhost:5000/get
```

---

## API Reference

### Routes  `GET|PUT|DELETE /api/v1/routes`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/routes` | List all routes |
| `GET` | `/api/v1/routes/{routeId}` | Get a single route |
| `PUT` | `/api/v1/routes/{routeId}` | Create or replace a route |
| `DELETE` | `/api/v1/routes/{routeId}` | Delete a route |

### Clusters  `GET|PUT|DELETE /api/v1/clusters`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/clusters` | List all clusters |
| `GET` | `/api/v1/clusters/{clusterId}` | Get a single cluster |
| `PUT` | `/api/v1/clusters/{clusterId}` | Create or replace a cluster |
| `DELETE` | `/api/v1/clusters/{clusterId}` | Delete a cluster |
| `PUT` | `/api/v1/clusters/{clusterId}/destinations/{id}` | Upsert a destination |
| `DELETE` | `/api/v1/clusters/{clusterId}/destinations/{id}` | Remove a destination |

### Config  `GET /api/v1/config`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/config/snapshot` | Full config dump (routes + clusters) |
| `GET` | `/api/v1/config/watch?knownVersion=N` | Long-poll — blocks until version > N |
| `GET` | `/api/v1/config/version` | Current version number |
| `PUT` | `/api/v1/config/snapshot` | Atomically replace all config |

Full interactive docs are available at **`http://localhost:5100`** (Swagger UI).

---

## Configuration Models

### Route

```json
{
  "routeId": "my-route",
  "clusterId": "my-cluster",
  "order": 1,
  "match": {
    "path": "/api/{**catch-all}",
    "hosts": ["api.example.com"],
    "methods": ["GET", "POST"]
  },
  "transforms": {
    "pathPrefix": "/v2",
    "requestHeaders": [
      { "name": "X-Forwarded-For", "value": "{RemoteIpAddress}", "append": true }
    ]
  }
}
```

### Cluster

```json
{
  "clusterId": "my-cluster",
  "loadBalancing": { "policy": "LeastRequests" },
  "healthCheck": {
    "active": {
      "enabled": true,
      "interval": "00:00:10",
      "timeout": "00:00:05",
      "path": "/health"
    }
  },
  "destinations": {
    "node-1": { "destinationId": "node-1", "address": "https://node1.internal" },
    "node-2": { "destinationId": "node-2", "address": "https://node2.internal" }
  }
}
```

### Load balancing policies

| Value | Behaviour |
|---|---|
| `RoundRobin` | Distribute requests evenly in turn |
| `LeastRequests` | Send to the destination with fewest active requests |
| `Random` | Pick a destination at random |
| `PowerOfTwoChoices` | Pick the better of two random choices |
| `FirstAlphabetical` | Always prefer the alphabetically first healthy destination |

---

## Running Tests

```bash
dotnet test
```

---

## Production Considerations

| Area | Recommendation |
|---|---|
| **Config persistence** | Replace `InMemoryConfigurationStore` with a PostgreSQL or Redis-backed store |
| **Secrets** | Use environment variables, Azure Key Vault, or AWS Secrets Manager |
| **TLS** | Terminate TLS at the proxy node; configure `HttpClientConfig.DangerousAcceptAnyServerCertificate = false` |
| **Multiple proxy nodes** | All nodes long-poll the same control plane — horizontal scaling works out of the box |
| **Control plane HA** | Run multiple control plane replicas behind a load balancer with a shared database |
| **Authentication** | Swap `ApiKeyAuthMiddleware` for JWT/OAuth2 for fine-grained RBAC |
| **Observability** | Add OpenTelemetry tracing + Prometheus metrics via `prometheus-net.AspNetCore` |

---

## License

MIT
