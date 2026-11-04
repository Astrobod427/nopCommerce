# Spec 003 — API pro (headless) pour `Misc.RestApi`

**Statut** : lot 1 en cours (branche `feature/restapi-pro`) · suite de la spec 002

## Contexte
Revue du 2026-10-30 (demande François : « une API béton, sécurisée, utilisable pour du headless, niveau pro ») :
le module 002 est sûr sur ce qu'il couvre (jetons aléatoires hachés, révocables, un identifiant + secret par
logiciel tiers, droits = permissions natives) mais ne couvre que le parcours d'achat de base (60 points d'accès,
contre plus de 1 100 annoncés pour l'API officielle payante). Manquent : protections anti-abus, accès depuis un
site web sur un autre domaine, paiements avec redirection, une bonne partie de la boutique et du back-office,
documentation Swagger.

Règle de travail : tout est développé et testé sur vps2 (staging nopcommerce.atrobod.cloud) avant vps1.

## Lot 1 — Sécurité
- **S1** — Limites de requêtes par adresse IP, réglables dans l'admin : une limite générale et une limite stricte
  sur la connexion, l'inscription, la session invitée et le jeton back-office. Dépassement → 429 + `Retry-After`.
- **S2** — Sites web sur un autre domaine (CORS) : liste des origines autorisées dans l'admin, requêtes
  préalables (preflight) gérées par le module.
- **S3** — En-têtes de sécurité sur toutes les réponses de l'API (`nosniff`, pas de mise en cache).
- **S4** — Journal : chaque écriture du back-office (création, modification, suppression, changement d'état)
  est inscrite au journal d'activité nopCommerce avec le client API ; les échecs de connexion aussi
  (corrige 002 : le type de journal n'était jamais créé, rien n'était inscrit).
- **S5** — Clients API restreints à des adresses IP (liste d'adresses ou de plages, vérifiée à l'obtention du
  jeton ET à chaque requête).
- **S6** — Double facteur : un compte avec Google Authenticator obtient d'abord un jeton d'attente de 5 minutes,
  puis la session après saisie du code ; 5 codes faux → jeton d'attente détruit.

## Lot 2 — Achat complet (headless)
- **A1** — Paiements avec redirection (PayPal Commerce d'abord) : la commande renvoie l'adresse de paiement.
- **A2** — Codes promo et cartes cadeaux sur le panier.
- **A3** — Recherche et filtres (prix, attributs, marques, tri), marques.
- **A4** — Traduction d'une adresse de page (slug) en produit / catégorie / marque / page de contenu.
- **A5** — Mot de passe oublié, confirmation d'e-mail.
- **A6** — Liste d'envies, avis produits, pages de contenu (topics), langues et devises.

## Lot 3 — Back-office complet et outillage
- **B1** — Photos produits (envoi, ordre), stock, attributs et variantes, marques, remises.
- **B2** — Remboursements, notes de commande.
- **B3** — Documentation OpenAPI 3 + Swagger servie par le module (activable).
- **B4** — Alertes vers les logiciels externes (webhooks signés) : commande créée/payée/expédiée, stock.
- **B5** — Protection contre le double envoi (`Idempotency-Key`) sur la commande et les écritures back-office.

## Hors périmètre
- Reproduire les 1 100 méthodes de l'API officielle : on couvre ce dont une boutique headless a besoin.
