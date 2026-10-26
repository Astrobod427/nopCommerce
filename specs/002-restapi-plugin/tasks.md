# Tâches — Spec 002

- [x] T001 Squelette du plugin (plugin.json 5.00, réglages, installation/désinstallation) — E1
- [x] T002 Domaine : clients API, sessions (jetons hachés), migration de tables — E3, E4
- [x] T003 Authentification (middleware + filtre d'autorisation) et permissions natives — E3, E4
- [x] T004 Tâche planifiée de purge des sessions expirées — E6
- [x] T005 Endpoints boutique : connexion, catalogue avec vrais prix, panier, commande, mes commandes — E3, E5
- [x] T006 Endpoints back-office : produits, catégories, clients, commandes, expéditions, retours — E4
- [x] T007 Pages d'admin : réglages, gestion des clients API, réglages app mobile — E1, E4, E7
- [x] T008 Tests (accès refusé hors de ses données, prix = boutique web) — E3, E5
- [x] T009 Validation sur le staging vps2 — constitution III
- [ ] T010 Migrer l'app mobile, retirer SimpleApi — E8

## Validation 2026-10-26 (staging vps2, copie de la base prod)
`tests/e2e_restapi.py` : **38/38 OK** — invité, inscription (panier conservé), connexion, prix API = prix boutique web,
adresse, livraison, paiement, commande ; B ne voit ni la commande ni l'adresse de A (404) ; jeton boutique refusé
sur le back-office et inversement (403) ; client API créé depuis l'admin, jeton par identifiant + secret, mauvais secret
(401), client lié à un compte sans droit (403) ; déconnexion = jeton révoqué (401).
Corrigé en route : pages d'admin du module en erreur 500 (`NopHtml` absent de `_ViewImports.cshtml`).
