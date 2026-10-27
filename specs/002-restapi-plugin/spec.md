# Spec 002 — Module API natif nopCommerce (`Misc.RestApi`)

**Statut** : développement en cours (branche `feature/restapi-plugin`) · **Kanban** : `t_c3f4fcb8` (board `webshops`)

## Contexte
L'API maison actuelle (SimpleApi, en prod) est protégée par une seule clé partagée embarquée dans l'app :
quiconque la récupère peut lister/modifier/supprimer tous les clients et commandes, et passer commande au nom
de n'importe qui. La « connexion » n'est pas une vraie session, les prix sont bruts (sans taxes ni remises).
L'API officielle nopCommerce est payante. Demande de François (2026-10-26) : un module API **compatible avec
les modules nopCommerce et pleinement gérable en tant que module nopCommerce**, reprenant ce qui a été fait
mais adapté au système natif.

## Exigences
- **E1** — Plugin nopCommerce standard (`Misc.RestApi`) : installation, configuration, désinstallation
  depuis l'admin ; activable/désactivable sans redéployer.
- **E2** — Deux usages séparés, activables chacun : **boutique** (app mobile, clients) et **back-office**
  (logiciels tiers).
- **E3** — Boutique : le client se connecte avec son compte nopCommerce et obtient une vraie session à durée
  limitée ; il ne voit et ne touche que ses propres données.
- **E4** — Back-office : chaque logiciel tiers est un « client API » (identifiant + secret, secret jamais
  stocké en clair) lié à un compte nopCommerce ; ses droits = ceux des rôles de ce compte (permissions natives).
- **E5** — Prix, taxes, remises, panier et commande passent par les services nopCommerce (mêmes résultats
  que la boutique web).
- **E6** — Sessions expirées purgées automatiquement (tâche planifiée nopCommerce).
- **E7** — Réglages de l'app mobile (couleurs, logo) gérés depuis l'admin du plugin.
- **E8** — Remplace SimpleApi : SimpleApi est retirée du code et de la prod. Décision François (2026-10-27) :
  SimpleApi servait surtout à l'app mobile, peu importante → retrait sans attendre ; l'app mobile
  (`src/MobileApp`, prototype Flutter) est hors service jusqu'à sa migration vers RestApi (spec à venir).

## Hors périmètre
- Compatibilité avec l'API payante officielle nopCommerce.
