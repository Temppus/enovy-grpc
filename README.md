# gRPC Load Balancing with Envoy Proxy

A ready example demonstrating **gRPC load balancing** using **Envoy Proxy** as a service mesh / API gateway. This project showcases how to scale gRPC services horizontally.

## Overview

This project demonstrates a common microservices pattern where:

1. **Multiple gRPC server instances** handle requests (horizontal scaling)
2. **Envoy Proxy** acts as a load balancer and service discovery mechanism
3. **gRPC Client** connects to Envoy (not directly to servers)
4. **Round-robin load balancing** distributes requests across healthy servers

### Why Envoy for gRPC?

Native gRPC load balancing is challenging because HTTP/2 multiplexes requests over a single TCP connection. Traditional load balancers (L4) only balance at connection time, not per-request. Envoy operates at L7 (application layer) and understands HTTP/2/gRPC, enabling true per-request load balancing.

---

## Architecture

### Docker Compose Deployment

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Docker Network (grpc-network)                    │
│                                                                             │
│  ┌───────────────┐         ┌─────────────────────────────────────────────┐  │
│  │               │         │              Envoy Proxy                    │  │
│  │  gRPC Client  │────────▶│                                             │  │
│  │  (Worker)     │  HTTP/2 │  • Round-Robin Load Balancing               │  │
│  │               │         │  • gRPC Health Checks                       │  │
│  └───────────────┘         │  • Circuit Breaker                          │  │
│                            │  • Retry Policies                           │  │
│                            │  • Outlier Detection                        │  │
│                            └──────────────┬──────────────────────────────┘  │
│                                           │                                 │
│                           ┌───────────────┼───────────────┐                 │
│                           │               │               │                 │
│                           ▼               ▼               ▼                 │
│                    ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│                    │ gRPC       │  │ gRPC       │  │ gRPC       │           │
│                    │ Server #1  │  │ Server #2  │  │ Server #3  │           │
│                    │ :8080      │  │ :8080      │  │ :8080      │           │
│                    └────────────┘  └────────────┘  └────────────┘           │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

External Access Points:
  • gRPC via Envoy:     localhost:5000
  • Envoy Admin:        localhost:9901
```


## Quick Start

**One command to run everything:**

```bash
cd GrpcEnvoy
docker compose up --build --scale grpc-server=3
```

This will:
1. Build the gRPC Server and Client images
2. Start 3 instances of the gRPC Server
3. Start Envoy Proxy with load balancing configured
4. Start the gRPC Client (sends requests every second)

**Watch the load balancing in action:**
- Client logs show responses from different server hostnames
- Envoy admin dashboard: http://localhost:9901

---

## How to run

### Option 1: Run everything inside Docker Compose

```bash
cd GrpcEnvoy

# Build and run with 3 server instances
docker compose up --build --scale grpc-server=3
```

### Option 2: Start only Envoy and GRPC Servers

```bash
cd GrpcEnvoy

# Start Envoy and servers in background
docker compose up -d --build --scale grpc-server=3 envoy grpc-server
```


## Envoy Admin Dashboard

Access the Envoy admin interface at: **http://localhost:9901**

### Service Discovery

Envoy config (`GrpcEnvoy/envoy.yaml`) uses **STRICT_DNS** to discover gRPC server instances:

1. Docker Compose creates DNS entries for `grpc-server`
2. When scaled (`--scale grpc-server=3`), Docker DNS returns all container IPs
3. Envoy resolves `grpc-server:8080` and gets multiple addresses
4. Envoy creates upstream connections to each discovered host