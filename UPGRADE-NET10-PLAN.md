# Plan d'intégration upstream nopCommerce + migration .NET 10

> Objectif : intégrer les ~236 commits de `upstream/develop` (nopSolutions/nopCommerce, v5.00, **.NET 10**) dans notre fork `develop`, en préservant nos 28 commits custom (plugin SimpleApi / API mobile, plugins Feed Ricardo & MarketplaceManager).
>
> **Faisabilité validée** par un merge d'essai complet : `Build succeeded`, 0 erreur, 40 projets, code custom inclus. Le coût d'adaptation du code est minime ; le vrai travail est l'infra (Docker/CI) et la validation runtime (migrations DB).

## Contexte mesuré

- Divergence : 28 commits ahead / 236 behind `upstream/develop`.
- Upstream cible `net10.0` (global.json SDK 10.0.100, Directory.Build.props), Dockerfile en image .NET 10 / Alpine.
- Conflits de merge : **4 fichiers** seulement (`.gitignore`, `Dockerfile`, `src/NopCommerce.sln`, `src/Libraries/Nop.Data/NopDbStartup.cs`).

## Phase 0 — Sauvegarde (avant tout)

- [ ] **Backup de la base PostgreSQL de prod** (les 236 commits contiennent des migrations de schéma potentiellement irréversibles).
- [ ] `develop` propre et poussée sur `origin`.
- [ ] Tag de repli : `git tag pre-net10-backup && git push origin pre-net10-backup`.

## Phase 1 — Merge dans une branche d'intégration

- [ ] `git fetch upstream`
- [ ] `git switch -c upgrade/net10 develop`
- [ ] `git merge upstream/develop`
- [ ] Résoudre les 4 conflits :
  - `.gitignore` → version upstream.
  - `src/Libraries/Nop.Data/NopDbStartup.cs` → version upstream (passe `NopMySql5TypeMap` → `NopMySql8TypeMap`, ce n'est pas du custom).
  - `src/NopCommerce.sln` → **fusion union** : conserver nos projets (`Nop.Plugin.Api.SimpleApi` + tests) ET les projets ajoutés upstream (Polls, Forums, Twilio, Jotform…).
  - `Dockerfile` → **NE PAS prendre brutalement la version upstream** : fusionner notre logique de build des plugins custom (Ricardo, MarketplaceManager) AVEC la nouvelle base .NET 10 / Alpine upstream. Point d'attention majeur (voir Phase 4).

## Phase 2 — Adaptation des modules custom (validée à l'essai)

- [ ] `<TargetFramework>` `net9.0` → `net10.0` dans :
  - `src/Plugins/Nop.Plugin.Api.SimpleApi/*.csproj`
  - `src/Plugins/Nop.Plugin.Feed.Ricardo/*.csproj`
  - `src/Plugins/Nop.Plugin.Feed.MarketplaceManager/*.csproj`
  - `src/Tests/Nop.Plugin.Api.SimpleApi.Tests/*.csproj`
- [ ] Aligner les versions de packages de test dans `Nop.Plugin.Api.SimpleApi.Tests` : `Microsoft.NET.Test.Sdk` 18.3.0, `NUnit` 4.5.1, `NUnit3TestAdapter` 6.2.0.
- [ ] **Code** : `SimpleApiPlugin.cs` → ajouter `using Nop.Services.Helpers;` (`IWebHelper` déplacé de `Nop.Core` vers `Nop.Services.Helpers`).
- [ ] **Test** : `CategoriesControllerTests.cs` → ajouter un mock `Nop.Services.Media.IPictureService` (2e paramètre du constructeur du controller).

## Phase 3 — Build & tests

- [ ] `dotnet build src/NopCommerce.sln -c Debug` → doit être `Build succeeded` (validé à l'essai).
- [ ] `dotnet test` → **exécuter** la suite (l'essai n'a fait que compiler ; cette étape révèle les régressions de comportement).

## Phase 4 — Infra Docker / CI (non couvert par l'essai)

- [ ] **Dockerfile** : valider que le build des plugins custom fonctionne sur la base .NET 10 / Alpine ; vérifier les dépendances natives Alpine (libgdiplus, ICU/culture, libtiff) nécessaires aux images produits, GeoLite, PDF.
- [ ] **CI GitHub Actions** : passer le SDK à .NET 10 dans les workflows ; vérifier le build + push de l'image vers `ghcr.io`.
- [ ] S'assurer que le VPS / runtime cible exécute bien l'image .NET 10.

## Phase 5 — Validation en staging (impératif avant prod)

- [ ] Déployer l'image sur un environnement de test (pas la prod).
- [ ] **Rejouer les migrations DB sur une COPIE de la base de prod** et vérifier le succès.
- [ ] Tester de bout en bout : storefront, admin, **API mobile** (login, catalog, cart, checkout), feeds Ricardo & MarketplaceManager.

## Phase 6 — Mise en production

- [ ] Backup DB prod (à nouveau, juste avant).
- [ ] Merge `upgrade/net10` → `develop`, tag de version.
- [ ] Déploiement Portainer (re-pull image).
- [ ] Surveillance logs + **plan de rollback** : revenir au tag `pre-net10-backup` et restaurer la DB si une migration échoue.

## Risques principaux

1. **Migrations DB** (5 mois de changements de schéma) — irréversibles : backup + test sur copie obligatoires.
2. **Dockerfile** — fusion soigneuse pour ne pas perdre le build des plugins custom sur la nouvelle base Alpine.
3. **Runtime non testé à l'essai** — la compilation réussit, mais le comportement à l'exécution (DI, plugins, API mobile) doit être validé en staging.
4. **Dépendances natives Alpine** — différentes de Debian/bookworm (images, polices, culture).

## Estimation d'effort

- Merge + adaptation code custom : **faible** (quelques heures, validé).
- Infra Docker / CI : ~0,5–1 jour.
- Staging + migrations + tests fonctionnels : ~1 jour.
- **Total prudent : ~2–3 jours.**
