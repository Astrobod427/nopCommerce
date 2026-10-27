# Tâches — Spec 001

- [x] T001 Fusionner upstream `develop` (2026-10-26) dans `upgrade/net10-2026-10`, résoudre les conflits — E1
- [x] T002 Compiler + tests (SimpleApi 6/6) — E1
- [x] T003 Staging vps2 sur copie de la base prod : 12 migrations OK, boutique/admin/API OK — E2
- [x] T004 Corriger les volumes de `docker-compose.prod.yml` (= stack Portainer compose/27) : plus de volume sur `/app/Plugins` ni sur tout `/app/wwwroot` (masquaient plugins, CSS et JS de l'image) ; seules les données gardées (`images/thumbs`, `images/uploaded`, `icons`, `files`, `sitemaps`, `db_backups`) en sous-dossiers du volume `nopcommerce_content` existant — E3 (staging 2026-10-27 : même disposition avec les données de prod, boutique/icônes/sitemap OK, e2e 38/38)
- [x] T005 Traduire en « Suisse FR » les 1 251 textes absents après migration (338 nouveaux 5.00 + 911 de modules restés en anglais, aussi en « Français ») : `i18n/fr-CH-5.00-complement.xml` (importable dans l'admin) et `.sql` (n'ajoute que les manquants, LanguageId 7) — E5 (staging 2026-10-27 : 8 060 textes, boutique OK)
- [ ] T009d Prod : après la migration, appliquer `i18n/fr-CH-5.00-complement.sql` puis redémarrer nopcommerce
- [ ] T006 Feu vert de François — constitution II
- [ ] T007 Sauvegarde de la base prod (vps1) juste avant — E4
- [ ] T008 Fusionner `upgrade/net10-2026-10` dans `develop`, publier l'image — E1
- [ ] T009 Déployer sur vps1 (Portainer `compose/27`), garder l'image précédente — E1, E6
- [ ] T009b Prod : désinstaller SimpleApi dans l'admin puis (le dossier `Nop.Plugin.Api.SimpleApi` du volume `nopcommerce-stack_nopcommerce_plugins` ne sera plus monté après T004) — spec 002 E8
- [ ] T009c Décision François : `Payments.Manual` (actif en prod) et `Widgets.AccessiBe` (installé) sont retirés de nopCommerce 5.00 ; ils disparaîtront avec T004
- [ ] T010 Vérifier la prod réelle (boutique, admin, plugins chargés, version 5.00) — E1
