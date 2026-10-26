# Constitution — Webshops (fork nopCommerce Astrobod427)

## Principes

### I. Les specs sont la source de vérité
Ce qui est décidé vit dans `specs/NNN-*/` (spec.md = le quoi, plan.md = le comment, tasks.md = l'avancement).
Le Kanban Hermes (board `webshops`) ne fait que refléter : chaque carte pointe vers sa spec, et toute
décision prise en conversation est d'abord écrite ici, puis répercutée sur la carte.

### II. François valide, la prod ne bouge pas sans lui
Rien ne part sur vps1 sans son feu vert explicite. Toute action destructrice (suppression, reset de base,
écrasement de volume) est annoncée avant d'être faite.

### III. Toujours tester sur une copie de la prod
Toute montée de version ou migration passe d'abord par le staging de vps2 (`~/projects/webshops/staging`,
127.0.0.1:5010) sur une copie fraîche de la base prod. Sauvegarde de la base prod juste avant chaque déploiement.

### IV. Rester un nopCommerce standard
Le code maison prend la forme de plugins nopCommerce gérables depuis l'admin (installer, configurer,
désinstaller, droits par rôles clients). Pas de modification du cœur sauf nécessité documentée ici.
Les fusions avec l'upstream `nopSolutions/nopCommerce` doivent rester simples.

### V. Sécurité d'abord sur ce qui est ouvert sur internet
Aucun secret partagé embarqué dans une app. Toute API agit au nom d'un compte nopCommerce réel et
avec ses droits (ACL). Prix, taxes et remises calculés par les services nopCommerce, jamais à la main.

### VI. Pas de maquette en prod
Un plugin simulé (faux jetons, données codées en dur) ne part pas en prod. Il est soit terminé, soit retiré.

## Cadre technique
- nopCommerce 5.00 / .NET 10 (`upgrade/net10-2026-10`), PostgreSQL 16, Docker.
- Dépôt : `github.com/Astrobod427/nopCommerce` ; branche de prod `develop` ; upstream `nopSolutions/nopCommerce`.
- Prod : vps1, stack Portainer `compose/27`, image `ghcr.io/astrobod427/nopcommerce`.
- GrandNode (`Astrobod427/grandnode2`) : hors périmètre — choix de François du 2026-09-29, la vraie prod sera nopCommerce.

## Méthode
- Une fonctionnalité = un dossier `specs/NNN-nom/` + une carte Kanban qui y renvoie.
- Une tâche est cochée dans `tasks.md` quand elle est vérifiée sur le réel (staging ou prod), pas quand le code compile.
- Commit et push systématiques : François déploie depuis git.

**Version** : 1.0.0 | **Ratifiée** : 2026-10-26 | **Dernier amendement** : 2026-10-26
