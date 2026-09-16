# Point de contrôle 2 — SQLite, commandes et tests

Correction des [chapitres 7](../../07-persistance.md), [8](../../08-commandes.md) et [9](../../09-tests.md). SDK .NET 10 requis. Deux projets : API et tests. Le catalogue mémoire précédent est indépendant.

## Premier lancement

Depuis la racine du dépôt :

```bash
cd workbook_v2/exemples/02-api-sqlite
dotnet restore Catalogue.sln
dotnet build Catalogue.sln
dotnet tool restore
cd Catalogue.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run --urls http://localhost:5080
```

La migration est à générer une seule fois : l'inspection du schéma fait partie de l'exercice. Elle contiendra Products, Orders et OrderItem. Après cette première préparation, relance seulement `dotnet run` depuis Catalogue.Api. Les modifications futures du modèle donnent lieu à de nouvelles migrations.

L'outil EF et les packages EF sont fixés à 10.0.0. Le manifeste local évite de dépendre d'un outil global d'une autre version. Si tu mets à jour le support, mets à jour ces versions ensemble.

## Scénario manuel

Ouvre [Catalogue.Api/requetes.http](Catalogue.Api/requetes.http). Exécute les requêtes dans l'ordre, en remplaçant productId puis orderId par les identifiants reçus. Le fichier indique les statuts et le total attendus. Les données sont créées par les requêtes : aucun seed caché n'est nécessaire.

Le scénario conserve un prix historique à 40 pour deux unités, même après changement du catalogue à 50. Le total final reste 80. Les ids changent à chaque nouvelle création.

## Tests

Depuis le dossier contenant Catalogue.sln :

```bash
dotnet test Catalogue.sln
```

Les tests HTTP utilisent leur propre base SQLite en mémoire et ne nécessitent ni serveur lancé, ni migration locale. Ils vérifient le schéma EF via EnsureCreated, pas les migrations. Le test de persistance manuelle après redémarrage complète cette vérification.

## Où lire quoi ?

| Question | Fichier |
|---|---|
| Comment la base est-elle configurée ? | [Program](Catalogue.Api/Program.cs) et [appsettings](Catalogue.Api/appsettings.json) |
| Comment enregistrer un produit ? | [ProductService](Catalogue.Api/Services/ProductService.cs) et [repository EF](Catalogue.Api/Data/EfProductRepository.cs) |
| Où sont les règles de commande ? | [Order](Catalogue.Api/Models/Order.cs) |
| Comment charger les lignes ? | [OrderService](Catalogue.Api/Services/OrderService.cs) |
| Pourquoi une erreur devient-elle 409 ? | [OrderConflictHandler](Catalogue.Api/OrderConflictHandler.cs) |
| Comment tester le parcours complet ? | [ApiTests](Catalogue.Tests/ApiTests.cs) |

## Dépannage

- Port occupé : arrête l'API mémoire ou l'ancien serveur.
- `dotnet ef` inconnu : lance `dotnet tool restore` dans le dossier contenant le manifeste.
- Table absente : crée puis applique la migration depuis Catalogue.Api.
- Base apparemment vide après redémarrage : vérifie le dossier courant, car le chemin SQLite est relatif.
- `InitialCreate` existe déjà : ne la recrée pas ; applique `database update`.
- Modification invisible en base : vérifie l'appel à SaveChangesAsync et la lecture dans une nouvelle requête.

Les fichiers de base, bin et obj sont ignorés par Git. Les migrations créées pendant l'exercice sont du code source à conserver.
