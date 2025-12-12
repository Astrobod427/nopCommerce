# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

nopCommerce is an open-source ASP.NET Core e-commerce platform. This fork is configured for deployment on Hostinger VPS with Traefik reverse proxy.

## Build and Run Commands

```bash
# Restore and build
dotnet restore src/NopCommerce.sln
dotnet build src/NopCommerce.sln -c Debug

# Run locally
dotnet run --project src/Presentation/Nop.Web/Nop.Web.csproj

# Docker build
docker build -t nopcommerce .

# Docker development (with PostgreSQL)
docker compose -f docker-compose.nopcommerce.yml up --build
```

## Architecture

- **src/Libraries/** - Core business logic and services
- **src/Plugins/** - Plugin modules (payments, shipping, etc.)
- **src/Presentation/Nop.Web/** - Main web application
- **src/Tests/** - Unit tests

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

### 1. Configurer le registry ghcr.io dans Portainer

1. Portainer → **Settings** → **Registries** → **Add registry**
2. Sélectionner **Custom registry**
3. Remplir :
   - Name: `GitHub Container Registry`
   - Registry URL: `ghcr.io`
   - Username: `astrobod427`
   - Password: votre GitHub PAT (scope `read:packages`)

### 2. Déployer le stack via Portainer

1. Portainer → **Stacks** → **Add stack**
2. Name: `nopcommerce`
3. Coller le contenu de `docker-compose.prod.yml`
4. Ajouter la variable d'environnement :
   - `POSTGRES_PASSWORD` = (mot de passe sécurisé)
5. **Remplacer `VOTRE_DOMAINE.COM`** par votre domaine réel
6. **Deploy the stack**

### Mise à jour en production

Dans Portainer → **Stacks** → `nopcommerce` → **Editor** → **Update the stack** (cocher "Re-pull image")

### Volumes persistants

- `nopcommerce_appdata` - Configuration et données applicatives
- `nopcommerce_plugins` - Plugins installés
- `nopcommerce_wwwroot` - Fichiers statiques et uploads
- `postgres_data` - Base de données PostgreSQL

### Configuration DNS requise

Pointer vers l'IP du VPS :
- `votredomaine.com` → A record → 72.62.60.240
- `www.votredomaine.com` → CNAME → votredomaine.com

### Important: Label Traefik réseau

Le label `traefik.docker.network=portainer-stack_traefik-net` est obligatoire car le container est sur plusieurs réseaux. Sans ce label, Traefik ne peut pas router correctement.
