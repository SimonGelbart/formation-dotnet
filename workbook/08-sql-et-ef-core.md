# 8 — SQL et Entity Framework Core

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- comprendre les concepts SQL minimums nécessaires au travail avec EF Core ;
- expliquer le rôle d'un ORM ;
- utiliser `DbContext` et `DbSet<T>` ;
- configurer un provider et une chaîne de connexion ;
- comprendre les migrations et utiliser les commandes principales ;
- réaliser des opérations CRUD simples ;
- comprendre les relations et propriétés de navigation ;
- distinguer `IEnumerable<T>` et `IQueryable<T>` sans réduire le premier à « données en mémoire » ;
- comprendre le tracking et `AsNoTracking()` ;
- comprendre les stratégies de chargement des relations ;
- inspecter une requête générée avec `ToQueryString()` ;
- identifier les risques de performance liés aux requêtes générées.

---

## 1. Pourquoi apprendre SQL avant EF Core ?

EF Core permet d'écrire beaucoup de requêtes en C#, mais la base reste relationnelle.

Sans modèle mental SQL, ceci :

```csharp
context.Orders
    .Where(x => x.Total > 100m)
    .Take(10);
```

peut sembler être un simple traitement de collection.

En réalité, EF Core peut traduire cette expression en une requête SQL exécutée par la base.

Il faut donc comprendre ce que fait la base derrière l'abstraction.

---

## 2. Table, ligne, colonne

Une table peut être vue comme un ensemble structuré de lignes.

```text
Orders
┌────────────┬────────────┬─────────┐
│ Id         │ CustomerId │ Total   │
├────────────┼────────────┼─────────┤
│ ...        │ ...        │ 120.00  │
│ ...        │ ...        │  42.50  │
└────────────┴────────────┴─────────┘
```

- une **colonne** décrit une donnée stockée ;
- une **ligne** représente un enregistrement ;
- une **table** regroupe des enregistrements de même nature.

Le modèle relationnel n'est pas identique au modèle objet. Une partie du travail d'EF Core consiste justement à faire le pont entre les deux.

---

## 3. Clés primaires et étrangères

### Primary Key

Identifie de manière unique une ligne.

```text
Orders.Id
```

### Foreign Key

Référence une ligne d'une autre table.

```text
Orders.CustomerId → Customers.Id
```

Cela permet de représenter les relations entre données.

---

## 4. Relations

Exemple : un client possède plusieurs commandes.

```text
Customer 1 ───── * Orders
```

Une commande possède plusieurs items :

```text
Order 1 ───── * OrderItems
```

Il faut être capable de reconnaître au minimum :

- one-to-one ;
- one-to-many ;
- many-to-many.

---

## 5. Requête SQL minimale

Lire :

```sql
SELECT Id, CustomerId, Total
FROM Orders
WHERE Total >= 100
ORDER BY Total DESC;
```

Interprétation :

1. lire `Orders` ;
2. filtrer les lignes avec `Total >= 100` ;
3. sélectionner certaines colonnes ;
4. trier par total décroissant.

Cette manière de penser fera écho à LINQ.

---

## 6. Jointures

Si les données sont séparées dans plusieurs tables, une jointure permet de les rapprocher.

```sql
SELECT o.Id, c.Name
FROM Orders o
JOIN Customers c ON c.Id = o.CustomerId;
```

Même si EF Core masque souvent la syntaxe SQL, il est important de comprendre qu'une navigation entre entités peut entraîner des jointures ou plusieurs requêtes selon la manière dont la requête est écrite et chargée.

---

## 7. Index

Un index aide la base à retrouver plus rapidement certaines données.

Sans index adapté, une recherche peut nécessiter de parcourir beaucoup de lignes.

Un index a aussi un coût :

- stockage supplémentaire ;
- maintenance lors des écritures.

### Idée clé

> Un index doit correspondre à des besoins réels de requête, pas être ajouté au hasard.

---

## 8. Transactions

Une transaction regroupe plusieurs opérations dans une unité logique.

Exemple :

```text
1. créer une commande
2. créer ses items
3. enregistrer un événement associé
```

Selon le besoin, on veut que l'ensemble soit validé ou annulé de manière cohérente.

EF Core utilise déjà des transactions dans certains scénarios, notamment autour d'un `SaveChanges`. Il faut néanmoins comprendre le concept SQL pour savoir quand plusieurs opérations doivent être coordonnées explicitement.

---

## 9. Qu'est-ce qu'un ORM ?

Un ORM fait le pont entre le modèle objet et le stockage relationnel.

```text
C# objects
    ↕
Entity Framework Core
    ↕
SQL database
```

Il ne supprime pas la nécessité de comprendre la base de données.

Il automatise notamment :

- une partie du mapping ;
- la génération de requêtes ;
- le suivi des modifications ;
- certaines opérations de schéma via les migrations.

---

## 10. `DbContext`

Le `DbContext` représente une unité de travail avec la base et maintient notamment un change tracker pour les entités suivies.

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
}
```

`DbSet<Order>` fournit un point d'entrée pour interroger et modifier les entités `Order`.

### Important

Un `DbContext` est conçu pour une durée de vie courte. Il n'est pas thread-safe et ne doit pas servir simultanément à plusieurs opérations concurrentes.

---

## 11. Configuration avec la DI

Dans `Program.cs` :

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Database"));
});
```

Puis dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "Database": "Data Source=orders.db"
  }
}
```

Le provider (`UseSqlite`, `UseSqlServer`, `UseNpgsql`, etc.) détermine le moteur utilisé et vient généralement d'un package NuGet spécifique.

`AddDbContext` enregistre normalement le contexte avec un lifetime scoped, adapté au modèle « une unité de travail par requête » de nombreuses API.

---

## 12. Créer et sauvegarder

Pour la majorité des entités :

```csharp
var order = new Order(...);

dbContext.Orders.Add(order);
await dbContext.SaveChangesAsync(cancellationToken);
```

`Add` ne signifie pas que la ligne est immédiatement écrite en base. L'entité est marquée comme devant être ajoutée.

C'est `SaveChangesAsync` qui déclenche les écritures nécessaires.

### Pourquoi ne pas enseigner `AddAsync` par défaut ?

`AddAsync` existe, mais son intérêt asynchrone concerne surtout certains générateurs de valeurs spéciaux. Pour une entité classique dont la clé est déjà disponible côté application, `Add` est généralement plus simple et approprié.

---

## 13. Lire

```csharp
var order = await dbContext.Orders
    .FirstOrDefaultAsync(
        x => x.Id == id,
        cancellationToken);
```

On retrouve la syntaxe LINQ, mais la source est un provider EF Core capable de traduire une partie de l'expression en SQL.

---

## 14. `IEnumerable<T>` vs `IQueryable<T>`

Cette différence est fondamentale mais souvent trop simplifiée.

### `IEnumerable<T>`

`IEnumerable<T>` exprime avant tout :

> « cette séquence peut être énumérée ».

Cela ne garantit pas à lui seul que les données sont déjà stockées dans une `List<T>` en mémoire.

Avec LINQ to Objects, les delegates C# sont exécutés par .NET lors de l'énumération.

### `IQueryable<T>`

`IQueryable<T>` transporte en plus une représentation de l'expression de requête qu'un provider peut analyser et traduire.

```text
Expression LINQ
      ↓
EF Core provider
      ↓
SQL
      ↓
Database
```

Exemple :

```csharp
var query = dbContext.Orders
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.CreatedAt)
    .Take(10);
```

À ce stade, la requête n'a généralement pas encore été envoyée à la base.

Puis :

```csharp
var orders = await query.ToListAsync(cancellationToken);
```

La requête est matérialisée.

### `ToList()` trop tôt

Mauvais ordre :

```csharp
var orders = await dbContext.Orders.ToListAsync(cancellationToken);
var expensive = orders.Where(x => x.Total >= 100m);
```

Toutes les commandes ont déjà été chargées avant le filtrage.

Mieux lorsque l'expression est traduisible :

```csharp
var orders = await dbContext.Orders
    .Where(x => x.Total >= 100m)
    .ToListAsync(cancellationToken);
```

Le filtre peut être exécuté directement par la base.

---

## 15. Voir le SQL avec `ToQueryString()`

Pendant l'apprentissage ou le diagnostic, il est très utile d'inspecter ce qu'EF Core prévoit d'envoyer à la base.

```csharp
var query = dbContext.Orders
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.CreatedAt)
    .Take(10);

Console.WriteLine(query.ToQueryString());
```

### Exercice d'observation

1. écris un `Where` + `Select` + `Take` ;
2. affiche `ToQueryString()` ;
3. déplace volontairement la matérialisation plus tôt ;
4. observe ce qui reste exécuté côté SQL et ce qui passe côté .NET.

Le but est de rendre visible la frontière entre requête traduite et traitement en mémoire.

---

## 16. Tracking

Par défaut, EF Core suit les entités retournées par les requêtes d'entités afin de détecter leurs modifications.

```csharp
var order = await dbContext.Orders
    .FirstAsync(x => x.Id == id, cancellationToken);

order.Confirm();
await dbContext.SaveChangesAsync(cancellationToken);
```

Pour une lecture seule :

```csharp
var orders = await dbContext.Orders
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

`AsNoTracking()` réduit le travail du change tracker lorsque l'on ne compte pas modifier puis sauvegarder les entités chargées.

Ce n'est pas une règle « toujours mettre `AsNoTracking` », mais un choix lié à l'intention de la requête.

---

## 17. Relations et propriétés de navigation

Exemple :

```csharp
public class Order
{
    public Guid Id { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
```

`Items` peut représenter une navigation vers des lignes d'une autre table.

Le simple fait d'avoir une propriété de navigation ne signifie pas que toutes les données seront automatiquement disponibles.

### Eager loading

Charger explicitement une relation dans la même requête logique :

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstAsync(x => x.Id == id, cancellationToken);
```

### Explicit loading

Demander plus tard le chargement d'une relation via le contexte.

### Lazy loading

Le chargement se produit lorsqu'on accède à la navigation, si l'application est configurée pour ce mécanisme.

Le lazy loading n'est pas activé automatiquement dans toutes les applications et peut rendre le nombre réel de requêtes moins visible.

---

## 18. Migrations

Une migration décrit une évolution du schéma correspondant à une évolution du modèle.

```text
Model C# v1
    ↓
Migration 1
    ↓
Schema DB v1

Model C# v2
    ↓
Migration 2
    ↓
Schema DB v2
```

Avec l'outil `dotnet-ef` :

```bash
dotnet ef migrations add InitialCreate
```

Puis :

```bash
dotnet ef database update
```

Pour inspecter les migrations :

```bash
dotnet ef migrations list
```

Les migrations sont normalement versionnées afin que plusieurs environnements puissent appliquer les mêmes évolutions de schéma.

### Important

Une migration mérite d'être relue. Elle modifie la structure de la base et peut avoir des conséquences sur les données existantes.

---

## 19. Le piège N+1

Supposons un code ou un mécanisme de lazy/explicit loading qui produit :

```text
1 requête pour charger 100 commandes
+ 1 requête par commande pour charger ses items
```

On obtient potentiellement :

```text
1 + 100 = 101 requêtes
```

Le N+1 n'est donc pas causé simplement par la présence d'une propriété de navigation. Il apparaît lorsqu'une stratégie de chargement provoque des requêtes supplémentaires répétées.

### Réflexe à acquérir

Quand tu écris une requête EF Core, demande-toi :

- combien de lignes sont chargées ?
- combien de colonnes ?
- combien de requêtes ?
- le filtrage se fait-il en base ou en mémoire ?
- ai-je besoin des entités complètes ou seulement d'une projection ?

---

## 20. Projection : ne charger que ce qui est nécessaire

Pour un écran de liste, charger toute l'entité et toutes ses relations est souvent inutile.

```csharp
var summaries = await dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .Select(x => new OrderSummary(
        x.Id,
        x.Status,
        x.Items.Sum(i => i.UnitPrice * i.Quantity)))
    .Take(20)
    .ToListAsync(cancellationToken);
```

Une projection peut :

- sélectionner moins de colonnes ;
- éviter de matérialiser des graphes d'entités complets ;
- exprimer directement le contrat de lecture attendu.

Elle montre aussi une nuance importante : une propriété C# calculée arbitraire n'est pas forcément traduisible en SQL. Dans les requêtes EF, pense en termes d'expressions que le provider sait traduire.

---

## Exercice — traduction mentale

Considère :

```csharp
var result = await dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .OrderByDescending(x => x.CreatedAt)
    .Select(x => new OrderSummary(
        x.Id,
        x.Status,
        x.Items.Sum(i => i.UnitPrice * i.Quantity)))
    .Take(20)
    .ToListAsync(cancellationToken);
```

Explique :

1. quelles opérations doivent idéalement être exécutées en base ;
2. ce que change `AsNoTracking()` ;
3. pourquoi le `Select` peut réduire les données transférées ;
4. ce que déclenche `ToListAsync()` ;
5. comment vérifier le SQL prévu avec `ToQueryString()` ;
6. pourquoi lancer cette requête en parallèle avec une autre sur le même `DbContext` serait une mauvaise idée.

---

## Application au projet fil rouge

Remplacer progressivement :

```text
InMemoryOrderRepository
```

par une implémentation utilisant `AppDbContext`.

Le contrat `IOrderRepository` et la logique de `OrderService` devraient changer le moins possible.

Pour les commandes, conserver le **prix unitaire au moment de l'achat** dans `OrderItem` plutôt que de toujours lire le prix courant du catalogue. Cela permet aux anciennes commandes de conserver leur vérité historique si le prix d'un produit change plus tard.

Lors des requêtes de résumé, calcule le total à partir de :

```text
OrderItem.UnitPrice × OrderItem.Quantity
```

ou choisis explicitement une stratégie de persistance du total. Ne suppose pas qu'une propriété C# calculée sera automatiquement traduite en SQL.

C'est un test concret de la qualité du découplage et du modèle de données réalisés dans les chapitres précédents.
