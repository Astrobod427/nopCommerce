# Spec 001 — Passage de la prod à nopCommerce 5.00 / .NET 10

**Statut** : staging validé, en attente du feu vert de François · **Kanban** : `t_4965ae3a` (board `webshops`)
**Branche** : `upgrade/net10-2026-10`

## Contexte
Le fork était bâti sur l'upstream `develop` du 2025-12-12 (5.00 en développement, .NET 9) + ~30 commits maison.
La base prod est déjà au schéma 5.00-dev. Revenir à la stable 4.90.x serait un retour en arrière (migrations) :
la seule voie est d'intégrer l'upstream `develop` (.NET 10).

## Exigences
- **E1** — La prod tourne sur le code fork + upstream `develop` (5.00, .NET 10), sans perte de données.
- **E2** — Les migrations passent sur une copie de la base prod avant tout déploiement (constitution III).
- **E3** — Une mise à jour de l'image remplace réellement les plugins : le volume `Plugins` de la prod
  ne doit plus masquer ceux de l'image.
- **E4** — Sauvegarde de la base prod faite juste avant le déploiement, restauration testée possible.
- **E5** — Les textes nouveaux de la 5.00 sont traduits dans la langue « Suisse FR ».
- **E6** — Retour arrière possible : l'image et la base d'avant restent disponibles jusqu'à validation.

## Hors périmètre
- Le remplacement de l'API maison (voir spec 002).
- Ricardo / MarketplaceManager : maquettes, ne partent pas en prod (constitution VI).

## Constats du 2026-10-26 (staging vps2)
- Fusion OK (4 conflits), compile, tests SimpleApi 6/6.
- Copie de la base prod : 12 migrations sans erreur ; boutique, admin, API répondent.
- Base prod = données de démo nopCommerce (47 produits, 5 commandes, 23 clients).
