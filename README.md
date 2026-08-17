# Ocelot Admin

A self-hosted administration panel and control plane for managing [Ocelot](https://github.com/ThreeMammals/Ocelot) API Gateway configurations.

Ocelot Admin provides a structured UI for managing routes without manually editing large Ocelot JSON files or Consul KV values.

> **Status:** Early release / preview

---

## Why Ocelot Admin?

Managing Ocelot configuration manually becomes difficult as the number of routes grows.

A typical deployment may require editing configuration such as:

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/product/{everything}",
      "UpstreamHttpMethod": ["GET", "POST"],
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "ServiceName": "product",
      "LoadBalancerOptions": {
        "Type": "RoundRobin"
      }
    }
  ]
}
```

When the configuration contains dozens or hundreds of routes, manually editing JSON or updating Consul KV can become error-prone.

Ocelot Admin provides a UI for managing these configurations while still keeping the original Ocelot JSON as the source format.

---

## Features

### Gateway Management

Register and manage multiple Ocelot gateways from one application.

Supported configuration stores:

- File
- Consul KV

---

### Structured Route Management

Manage Ocelot routes through structured forms instead of manually editing JSON.

Supported route operations include:

- View routes
- Search routes
- Filter by HTTP method
- Filter by service
- Pagination
- Add route
- Edit route
- Duplicate route
- Delete route

Currently supported route properties include:

- `UpstreamPathTemplate`
- `UpstreamHttpMethod`
- `RouteIsCaseSensitive`
- `DownstreamScheme`
- `DownstreamPathTemplate`
- `ServiceName`
- `DelegatingHandlers`
- `LoadBalancerOptions`
- `QoSOptions`
- `HttpHandlerOptions`

Support for more Ocelot properties will be added over time.

---

### Lossless Configuration Editing

Ocelot Admin preserves configuration properties that are not yet supported by the structured UI.

Unknown properties are retained when the configuration is:

```text
Read
→ Deserialize
→ Edit
→ Serialize
→ Publish
```

This allows newer or custom Ocelot options to coexist with Ocelot Admin.

---

### Raw JSON Editor

The full Ocelot configuration can also be edited directly.

Structured forms and the raw JSON editor operate on the same draft configuration.

---

### Draft-Based Editing

Changes do not immediately modify the live gateway configuration.

```text
Live Configuration
       ↓
     Draft
       ↓
Edit / Validate
       ↓
Review Changes
       ↓
     Publish
```

Drafts are stored locally in SQLite until they are published or discarded.

---

### Configuration Validation

Draft configurations can be validated before publishing.

Validation distinguishes between:

- Errors
- Warnings

Publishing is blocked when validation errors exist.

---

### Configuration Diff

Before publishing, Ocelot Admin compares the draft configuration with the current live configuration.

The publish preview shows:

- Added routes
- Modified routes
- Removed routes
- Global configuration changes

---

### Configuration History

Before a new configuration is published, Ocelot Admin stores a snapshot of the previous live configuration.

Historical configurations can be restored as a new draft and reviewed before publishing again.

---

### Consul Integration

Ocelot Admin can read and publish Ocelot configuration stored in Consul KV.

Supported Consul functionality includes:

- Consul connectivity testing
- KV configuration reading
- KV publishing
- ACL token support
- Configuration validation
- Publish verification
- Optimistic concurrency protection using Consul CAS

Consul `ModifyIndex` is captured when a draft is created.

When publishing, Ocelot Admin uses Compare-And-Set to avoid accidentally overwriting configuration that was changed by another user or process.

```text
Read Consul
    ↓
ModifyIndex = 120

Create Draft
    ↓
SourceVersion = 120

Publish
    ↓
CAS 120
    ↓

┌───────────────┬────────────────────┐
│ Still 120     │ Changed externally │
│               │                    │
│ Publish       │ Reject             │
│ succeeds      │ publish            │
└───────────────┴────────────────────┘
```

---

## Architecture

Ocelot Admin is a single ASP.NET Core application.

```text
Ocelot Admin
│
├── Blazor UI
├── ASP.NET Core
│
├── EF Core
│   └── SQLite
│
├── Configuration Stores
│   ├── File
│   └── Consul KV
│
└── Ocelot Configuration Engine
    ├── Drafts
    ├── Validation
    ├── Diff
    ├── History
    └── Publishing
```

Ocelot Admin itself is **not** an Ocelot gateway.

It is a control plane used to manage Ocelot gateway configuration.

---

## Docker

Ocelot Admin is designed to run as a self-hosted Docker container.

Supported platforms:

- `linux/amd64`
- `linux/arm64`

### Docker Run

```bash
docker run -d \
  --name ocelot-admin \
  -p 8080:8080 \
  -v ocelot-admin-data:/app/volume \
  hanlinthedev/ocelot-admin:latest
```

Open:

```text
http://localhost:8080
```

---

## Docker Compose

```yaml
services:
  ocelot-admin:
    image: hanlinthedev/ocelot-admin:latest
    container_name: ocelot-admin

    ports:
      - "8080:8080"

    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: "Data Source=/app/volume/ocelot-admin.db"

    volumes:
      - ocelot-admin-data:/app/volume

    restart: unless-stopped

volumes:
  ocelot-admin-data:
```

Start:

```bash
docker compose up -d
```

---

## Persistent Storage

Ocelot Admin uses SQLite for its internal data.

The database contains:

- Registered gateways
- Gateway settings
- Draft configurations
- Configuration history

Inside the container, the database is stored at:

```text
/app/volume/ocelot-admin.db
```

The `/app/volume` directory should therefore be mounted to persistent storage.

Example:

```bash
-v ocelot-admin-data:/app/volume
```

Database migrations are automatically applied when the application starts.

---

## Using Consul

When adding a Consul-backed gateway, provide:

```text
Consul Address
Configuration Key
ACL Token (optional)
```

Example:

```text
Consul Address:
http://consul:8500

Configuration Key:
ocelot/uat/config
```

### Important when running inside Docker

Inside a container:

```text
localhost
```

refers to the Ocelot Admin container itself.

If Consul is another Docker Compose service named `consul`, use:

```text
http://consul:8500
```

If Consul runs on another server, use that server's hostname or IP address.

---

## Using File-Based Configuration

Ocelot Admin can also manage an Ocelot JSON file.

When running inside Docker, the configuration directory must be mounted into the container.

For example:

```yaml
services:
  ocelot-admin:
    image: hanlinthedev/ocelot-admin:latest

    ports:
      - "8080:8080"

    volumes:
      - ocelot-admin-data:/app/volume
      - /home/user/my-gateway:/configs/my-gateway
```

Then configure the gateway path in Ocelot Admin as:

```text
/configs/my-gateway/ocelot.json
```

The path must be accessible from inside the container.

---

## Building From Source

Requirements:

- .NET 10 SDK
- Docker, optional

Clone the repository:

```bash
git clone https://github.com/hanlinthedev/OcelotAdmin.git
cd OcelotAdmin
```

Restore:

```bash
dotnet restore
```

Run:

```bash
dotnet run
```

The SQLite database will be created automatically under:

```text
volume/ocelot-admin.db
```

---

## Building the Docker Image

Build for the current architecture:

```bash
docker build -t ocelot-admin:local .
```

Build multi-platform images:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t YOUR_DOCKERHUB_USERNAME/ocelot-admin:latest \
  --push .
```

For a versioned release:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t YOUR_DOCKERHUB_USERNAME/ocelot-admin:0.1.0 \
  -t YOUR_DOCKERHUB_USERNAME/ocelot-admin:latest \
  --push .
```

---

## Current Limitations

Ocelot Admin is currently an early release.

The following are not yet included:

- User authentication
- Multi-user access control
- Client credential management
- Authentication scheme management
- Route-level client authorization management
- Automatic merging of concurrent configuration changes
- Full structured UI coverage for every Ocelot setting
- Remote agent for accessing files on another host

The application should currently be treated as a trusted internal administration tool.

Do not expose it directly to the public internet without an appropriate authentication or network-access layer.

---

## Planned Features

Future versions may include:

- Client application management
- API key / client secret management
- Route-level client authorization
- Authentication provider management
- More Ocelot route options
- Global configuration editor
- Improved configuration diff viewer
- Configuration versioning
- Audit logging
- Multi-user authentication and authorization
- Additional configuration-store providers
- Remote gateway agents

---

## Project Philosophy

Ocelot Admin aims to provide a management layer on top of Ocelot without taking ownership of the gateway runtime itself.

The project follows three principles:

1. **Ocelot configuration remains portable**

   Configuration should remain valid Ocelot JSON and should not require proprietary metadata.

2. **Unsupported properties must not be destroyed**

   Using the UI should not remove configuration that Ocelot Admin does not yet understand.

3. **Publishing should be safe**

   Drafts, validation, history, verification, and concurrency protection should reduce the risk of accidental gateway configuration changes.

---

## Contributing

Contributions, issues, feature requests, and discussions are welcome.

If you find a bug or want to propose a feature, open an issue in the GitHub repository.

---

## License


Ocelot Admin is licensed under the
**Mozilla Public License 2.0 (MPL-2.0)**.

You may use, modify, fork, and use this project commercially, subject to
the terms of the MPL-2.0.

The MPL uses file-level copyleft. Modifications to MPL-covered files that
are distributed must remain available under the MPL-2.0, while separate
files may be licensed under different terms.

See [LICENSE](LICENSE) for the full license terms.