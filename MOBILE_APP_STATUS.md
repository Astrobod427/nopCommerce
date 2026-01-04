# État d'avancement - App Mobile nopCommerce (Flutter)

Date : 31 Décembre 2025

## 🏁 Ce qui est fonctionnel

### Backend (Plugin SimpleApi - C#)
- **ProductsController** : Support du filtrage par `categoryId`. Le endpoint de détail retourne désormais la `FullDescription` et la liste complète des `Images`.
- **CategoriesController** : Ajout de la récupération de l'image de catégorie (`ImageUrl`).
- **DTOs** : Mise à jour de `ProductDto` et `CategoryDto`.

### Frontend (Flutter)
- **Navigation** : Système d'onglets (Home, Catalog, Account, Settings).
- **Catalogue** : 
    - Liste des catégories.
    - Vue filtrée des produits par catégorie.
- **Produits** :
    - Grille de produits sur l'accueil.
    - Page détail avec carrousel d'images et rendu HTML de la description.
- **Panier (State Management)** :
    - Implémenté avec `Provider`.
    - Ajout/Suppression/Calcul du total.
    - Badge de notification sur l'icône du panier.

## 🛠 Structure du code
- `src/MobileApp/lib/src/models/` : Modèles `Product` et `Category`.
- `src/MobileApp/lib/src/services/` : `ApiService` (HTTP) et `CartProvider` (État).
- `src/MobileApp/lib/src/screens/` : Écrans pour l'accueil, le catalogue, les détails et le panier.

## 🚀 Prochaines étapes suggérées
1. **Authentification** : Créer les écrans de Login/Register et lier à `CustomersController`.
2. **Checkout** : Gérer la création de commande via l'API.
3. **Persistance** : Utiliser `shared_preferences` pour sauvegarder le panier localement.
4. **Recherche** : Ajouter une barre de recherche de produits.
