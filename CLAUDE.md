# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nopCommerce is an open-source ASP.NET Core e-commerce platform running on .NET 9. This fork is configured for deployment on Hostinger VPS with Traefik reverse proxy and PostgreSQL.

## Build and Run Commands

```bash
# Restore and build
dotnet restore src/NopCommerce.sln
dotnet build src/NopCommerce.sln -c Debug

# Run locally
dotnet run --project src/Presentation/Nop.Web/Nop.Web.csproj

# Run tests (NUnit with FluentAssertions and Moq)
dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj

# Run a single test
dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Docker build
docker build -t nopcommerce .

# Docker development (with hot reload)
docker compose -f docker-compose.nopcommerce.yml up --build
```

## Architecture

### Core Libraries (src/Libraries/)
- **Nop.Core** - Domain entities, caching, events, infrastructure (NopEngine, ITypeFinder), Autofac DI
- **Nop.Data** - Data access layer with IRepository<T>, supports PostgreSQL/MySQL/MSSQL via FluentMigrator
- **Nop.Services** - Business logic services for all domains (catalog, orders, customers, etc.)

### Presentation Layer (src/Presentation/)
- **Nop.Web** - Main MVC application with Areas/Admin for admin panel
- **Nop.Web.Framework** - Shared MVC infrastructure, startup classes (INopStartup implementations)

### Plugin System (src/Plugins/)
Plugins implement specific interfaces and are loaded dynamically. Categories: Payments, Shipping, Tax, Widgets, ExternalAuth, Misc.

### Key Patterns
- **INopStartup**: Service registration at startup (ordered by priority)
- **IStartupTask**: One-time initialization tasks run on app start
- **IOrderedMapperProfile**: AutoMapper profiles for entity-to-model mapping
- **IRepository<T>**: Generic repository pattern for data access

## Release Workflow

Single branch workflow with semantic version tags.

```bash
# 1. Commit your changes
git add .
git commit -m "feat: description of changes"

# 2. Create a version tag when ready to release
git tag v1.0.0
git push origin develop --tags

# This triggers GitHub Actions to:
# - Build Docker image
# - Push to ghcr.io/astrobod427/nopcommerce:v1.0.0 and :latest
```

### Image Registry

Images are published to GitHub Container Registry:
- `ghcr.io/astrobod427/nopcommerce:latest`
- `ghcr.io/astrobod427/nopcommerce:v1.0.0` (version-specific)

## Production Deployment (VPS Hostinger KVM4)

### 1. Prérequis sur le VPS

Créer les fichiers de configuration sur le VPS **avant** de déployer le stack :

```bash
# Créer le répertoire
mkdir -p /opt/docker/nopcommerce

# Script d'init PostgreSQL (extension citext requise par nopCommerce)
cat > /opt/docker/nopcommerce/init.sql << 'EOF'
-- Active l'extension citext pour la base de données NopCommerce
\c nopcommerce
CREATE EXTENSION IF NOT EXISTS citext;
EOF

# Script entrypoint (génère dataSettings.json au démarrage)
cat > /opt/docker/nopcommerce/entrypoint.sh << 'EOF'
#!/bin/sh
set -e

cat > /app/App_Data/dataSettings.json << SETTINGS
{
  "DataProvider": "postgresql",
  "ConnectionString": "Host=postgres;Port=5432;Database=nopcommerce;Username=nopcommerce;Password=${POSTGRES_PASSWORD}",
  "SQLCommandTimeout": null,
  "RawDataSettings": {}
}
SETTINGS

echo "dataSettings.json generated successfully"
exec dotnet Nop.Web.dll
EOF

chmod +x /opt/docker/nopcommerce/entrypoint.sh
```

### 2. Configurer le registry ghcr.io dans Portainer

1. Portainer → **Settings** → **Registries** → **Add registry**
2. Sélectionner **Custom registry**
3. Remplir :
   - Name: `GitHub Container Registry`
   - Registry URL: `ghcr.io`
   - Username: `astrobod427`
   - Password: votre GitHub PAT (scope `read:packages`)

### 3. Déployer le stack via Portainer

1. Portainer → **Stacks** → **Add stack**
2. Name: `nopcommerce`
3. Coller le contenu de `docker-compose.prod.yml`
4. Ajouter la variable d'environnement :
   - `POSTGRES_PASSWORD` = (mot de passe sécurisé)
5. **Deploy the stack**

### Mise à jour en production

Dans Portainer → **Stacks** → `nopcommerce` → **Editor** → **Update the stack** (cocher "Re-pull image")

### Volumes persistants

- `nopcommerce_appdata` - Configuration et données applicatives
- `nopcommerce_plugins` - Plugins installés
- `nopcommerce_wwwroot` - Fichiers statiques et uploads
- `postgres_data` - Base de données PostgreSQL

### Configuration DNS requise

Pointer vers l'IP du VPS :
- `nopcommerce.petitnuage.cloud` → A record → 72.62.60.240

### URL de production

- Site: `https://nopcommerce.petitnuage.cloud`

### Important: Label Traefik réseau

Le label `traefik.docker.network=portainer-stack_traefik-net` est obligatoire car le container est sur plusieurs réseaux. Sans ce label, Traefik ne peut pas router correctement.
