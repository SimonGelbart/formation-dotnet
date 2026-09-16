# 08 — Ajouter les commandes

**Pratiquer :** une relation et des règles métier. **Comprendre :** prix historique, chargement des lignes et traduction d'un conflit en HTTP. **Repérer :** mapping d'une collection encapsulée.

Résultat : créer une commande, ajouter deux produits et la confirmer.

## Le besoin métier

Le catalogue donne le prix actuel. Une ligne de commande conserve le nom et le prix au moment de l'ajout. Modifier le catalogue ne doit pas changer une ancienne commande.

Nos règles : quantité strictement positive ; commande vide non confirmable ; commande confirmée non modifiable ; deuxième confirmation refusée. Un produit gratuit reste autorisé.

Un `enum OrderStatus` donne deux états nommés : `Draft` et `Confirmed`. Il évite les fautes de frappe d'une chaîne libre.

## Lire le domaine

Ouvre [Order.cs](exemples/02-api-sqlite/Catalogue.Api/Models/Order.cs), puis [OrderItem.cs](exemples/02-api-sqlite/Catalogue.Api/Models/OrderItem.cs).

La commande possède une liste privée. `AddItem` vérifie l'état et la quantité puis crée la ligne en copiant le nom et le prix. `Confirm` vérifie les règles avant de changer l'état. `Total` calcule la somme des lignes.

Le code extérieur reçoit une collection en lecture seule. Il ne peut pas appeler `order.Items.Clear()` et contourner les règles. Le constructeur de ligne est `internal` : la création est réservée au projet ; l'usage normal passe par la commande.

## La relation en base

`Orders.Id` identifie la commande. `OrderItem.OrderId` est la clé étrangère vers cette commande : une commande possède plusieurs lignes, chaque ligne appartient à une commande.

Une jointure permet de lire les deux tables :

```sql
SELECT o.Id, i.ProductName, i.Quantity
FROM Orders o
JOIN OrderItem i ON i.OrderId = o.Id;
```

`OrderItem` est le nom conventionnel de la table de lignes dans cette référence : vérifie-le dans la migration. Un `JOIN` ne retourne que les commandes avec des lignes ; un `LEFT JOIN` conserverait aussi les commandes vides.

`ProductId` est ici conservé comme référence historique sans clé étrangère vers le catalogue. C'est un choix du modèle réduit : supprimer un produit n'efface pas l'historique. `Total` n'est pas une colonne.

## Le mapping fourni

Les quelques lignes de [AppDbContext](exemples/02-api-sqlite/Catalogue.Api/Data/AppDbContext.cs) indiquent à EF la relation et la liste privée `_items` à remplir. Le mapping est fourni pour garder l'encapsulation sans transformer ce chapitre en cours avancé sur l'ORM.

Les identifiants de lignes sont créés par le domaine ; leur génération automatique EF est désactivée pour qu'une nouvelle ligne ajoutée à une commande chargée soit reconnue comme nouvelle.

La lecture de commande utilise `Include(o => o.Items)`. Sans chargement des lignes, on ne pourrait pas calculer correctement son total ou vérifier si elle est vide. Le lazy loading n'est pas activé. Lire automatiquement les lignes une commande après l'autre peut produire le problème N+1 : une requête initiale, puis une requête par commande.

## Suivre une opération complète

[OrderService](exemples/02-api-sqlite/Catalogue.Api/Services/OrderService.cs) charge les objets, appelle la règle, puis sauvegarde. Il utilise directement le contexte : nous n'ajoutons pas un second repository identique uniquement pour respecter une convention.

[OrdersController](exemples/02-api-sqlite/Catalogue.Api/Controllers/OrdersController.cs) gère le contrat HTTP. [OrderConflictHandler](exemples/02-api-sqlite/Catalogue.Api/OrderConflictHandler.cs) traduit uniquement `OrderConflictException` en `409` avec un corps ProblemDetails. Les erreurs inattendues restent des `500` gérées globalement.

| Cas | Résultat |
|---|---|
| Création de commande | 201 avec Location |
| Ajout ou confirmation réussie | 204 |
| Commande ou produit absent | 404 |
| Quantité nulle ou négative | 400 grâce à la validation HTTP |
| Commande vide à confirmer | 409 |
| Ajout après confirmation ou deuxième confirmation | 409 |

`AddProblemDetails` seul ne décide pas des erreurs métier : le handler fait explicitement cette correspondance. Une absence est remontée par un résultat `false` ou `null`, pour que le Controller retourne 404 au lieu de réussir silencieusement.

## Exercice de bout en bout

Suis les requêtes 1 à 14 de [requetes.http](exemples/02-api-sqlite/Catalogue.Api/requetes.http). Les variables `productId` et `orderId` doivent être remplacées par les identifiants reçus.

1. Crée un produit et passe son prix à 40.
2. Crée une commande vide ; tente une confirmation.
3. Ajoute deux unités du produit.
4. Change le prix catalogue à 50.
5. Confirme puis relis la commande.
6. Essaie d'ajouter encore une ligne.
7. Supprime le produit et relis la commande.

<details>
<summary>Correction et résultats attendus</summary>

La confirmation vide retourne 409. L'ajout puis la confirmation retournent 204. La commande confirmée conserve deux unités à 40, donc un total de 80. Un nouvel ajout retourne 409. La suppression du produit ne change pas la ligne historique : le nom, le prix et le total restent présents.
</details>

## Trois questions

1. Pourquoi copier le prix au lieu de toujours lire `Product.Price` ?
2. Pourquoi charger les lignes avant de confirmer ?
3. Quel composant choisit 409 et quel objet protège la règle ?

Suite : [09 — Tests](09-tests.md).
