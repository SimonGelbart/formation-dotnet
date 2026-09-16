# 07 — Conserver les produits avec SQLite

**Pratiquer :** migration, lecture et sauvegarde. **Comprendre :** SQL, ORM, tracking, `IQueryable` et NuGet. **Repérer :** index et transactions.

Résultat : un produit est encore présent après l'arrêt puis le redémarrage de l'API.

## Transformer ton application

Commence par l’[atelier SQLite](ateliers/02-ajouter-sqlite.md) : il fait évoluer ton API HTTP en conservant ses routes. Il ne demande pas encore d’ajouter les commandes. Reviens ensuite lire ce chapitre et comparer avec la référence.

## Changer d'application de référence

Arrête l'API mémoire. Ouvre [l'API SQLite](exemples/02-api-sqlite/README.md). Elle contient aussi les commandes du chapitre suivant ; pour le moment, suis uniquement `ProductsController`, `ProductService`, `EfProductRepository` et `AppDbContext`.

Cette seconde application est indépendante. Les produits de la mémoire ne sont pas importés : crée de nouvelles données via HTTP.

## SQL avant l'ORM

Une table rassemble des lignes de même structure. `Products` contient `Id`, `Name` et `Price`. `Id` est la clé primaire : elle identifie une ligne.

Requête SQL de lecture à reconnaître (elle ne constitue pas du code C# à coller) :

```sql
SELECT Id, Name, Price
FROM Products
WHERE Name = 'Clavier'
ORDER BY Name;
```

On choisit les colonnes, la table, le filtre et l'ordre. Un ORM comme EF Core transforme de nombreuses opérations sur des objets en commandes SQL. La base existe toujours : il reste utile de comprendre ce qu'on lui demande.

## Projet, solution et dépendances

Un `.csproj` décrit un projet ; une solution regroupe des projets. La référence fournit une solution `.sln`, toujours utilisable avec .NET 10. `dotnet new sln` peut créer le format `.slnx` avec ce SDK.

Dans le `.csproj` de l'API, `PackageReference` ajoute une bibliothèque NuGet : rôle comparable à une dépendance npm. Le projet de tests utilise aussi `ProjectReference` pour référencer le code source de l'API. Des packages peuvent dépendre d'autres packages : ce sont les dépendances transitives.

Depuis `parcours/exemples/02-api-sqlite` :

```bash
dotnet restore Catalogue.sln
dotnet build Catalogue.sln
dotnet tool restore
```

`restore` récupère les dépendances. `build` compile (et restaure implicitement si nécessaire). `tool restore` installe l'outil local `dotnet-ef`, déclaré dans `.config/dotnet-tools.json`. Cet outil de développement est distinct des bibliothèques référencées par le projet.

## Créer la base : une fois au démarrage du parcours

```bash
cd Catalogue.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run --urls http://localhost:5080
```

La migration initiale est volontairement à générer dans l'exercice. Ne relance pas `migrations add InitialCreate` à chaque démarrage. Lis les tables créées dans le fichier généré, puis conserve les migrations avec ton code. Si tu modifies ensuite le modèle, crée une nouvelle migration avec un autre nom.

`database update` applique le schéma. Sans cette étape, un GET peut échouer avec une table absente. Le fichier `catalogue.db` est créé dans le dossier courant : utilise les commandes depuis `Catalogue.Api` pour éviter de créer plusieurs bases par erreur.

Teste POST et GET avec [requetes.http](exemples/02-api-sqlite/Catalogue.Api/requetes.http). Arrête puis relance le serveur depuis le même dossier. Le catalogue reste présent.

## Les responsabilités d'EF

[AppDbContext](exemples/02-api-sqlite/Catalogue.Api/Data/AppDbContext.cs) décrit les entités. `DbSet<Product>` est l'accès à la collection persistée. `AddDbContext` configure SQLite et fournit normalement un contexte par scope, donc ici par requête.

[EfProductRepository](exemples/02-api-sqlite/Catalogue.Api/Data/EfProductRepository.cs) remplace la mémoire. Son contrat est désormais asynchrone pour les lectures et la sauvegarde. Le passage à async est une évolution du contrat, pas un remplacement parfaitement transparent des signatures.

Extrait de son comportement :

```csharp
// Le produit devient une nouvelle entité suivie, sans écriture immédiate.
_db.Products.Add(product);
// Les changements sont envoyés à la base.
await _db.SaveChangesAsync(cancellationToken);
```

`Add` reste synchrone. `SaveChangesAsync` marque la frontière de sauvegarde. Même convention pour une modification : charger une entité suivie, appeler `Update` sur l'objet, puis sauvegarder.

La liste utilise `AsNoTracking` car elle sert à la lecture. `GetByIdAsync` garde le tracking parce que le service peut modifier le produit. Un contexte est une unité de travail courte ; ne le conserve pas en singleton et ne l'utilise pas simultanément dans plusieurs tâches.

## LINQ sur la base

Extrait à essayer dans une méthode ayant accès au contexte :

```csharp
var query = _db.Products
    .Where(p => p.Name.StartsWith("C"))
    .OrderBy(p => p.Name);

Console.WriteLine(query.ToQueryString());
var products = await query.ToListAsync(cancellationToken);
```

Il faut `using Microsoft.EntityFrameworkCore;` pour ces extensions. `IQueryable<Product>` transporte une requête qu'EF peut traduire. `ToListAsync` l'exécute. Si tu charges toute la table avant de filtrer, le filtre s'effectue ensuite en mémoire.

La traduction dépend du provider. SQLite possède notamment des limites sur certaines opérations avec `decimal` et `DateTimeOffset`. Nos exemples conservent les prix en decimal, calculent les totaux des commandes après chargement des lignes et stockent leur date en `DateTime` UTC. Nous ne prétendons pas que toute propriété C# calculée est traduisible en SQL.

## Exercice

Dans `ProductService.UpdateAsync`, commente temporairement l'appel à `SaveChangesAsync`. Modifie un produit par PUT puis relis-le avec un nouveau GET. Rétablis ensuite la sauvegarde.

<details>
<summary>Correction</summary>

Le PUT peut retourner 204, mais le GET retrouve l'ancien prix : la modification était seulement en mémoire dans le contexte de la requête. Après rétablissement de `SaveChangesAsync`, le nouveau prix est conservé.
</details>

## Repères à conserver

Un index peut accélérer les recherches, au prix d'un coût en écriture et en stockage. Une transaction permet de valider ensemble des changements ; un appel relationnel à `SaveChanges` est normalement transactionnel. On n'ajoute pas des index ou des transactions manuelles sans besoin concret.

OpenAPI est déjà configuré dans la référence. Pour l'observer, arrête le serveur puis lance :

```bash
dotnet run --urls http://localhost:5080 --environment Development
```

Ouvre `http://localhost:5080/openapi/v1.json`. C'est un document JSON ; une interface Swagger n'est pas automatiquement fournie.

## Trois questions

1. Pourquoi une modification peut-elle être visible en C# sans être sauvegardée ?
2. Où s'effectue le filtre avant ou après `ToListAsync` ?
3. Pourquoi le repository EF est-il scoped alors que la mémoire était singleton ?

Suite : [08 — Commandes](08-commandes.md).
