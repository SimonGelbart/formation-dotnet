# 8 — SQL et Entity Framework Core

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- comprendre les concepts SQL minimums nécessaires au travail avec EF Core ;
- expliquer le rôle d'un ORM ;
- utiliser `DbContext` et `DbSet<T>` ;
- comprendre les migrations ;
- réaliser des opérations CRUD simples ;
- comprendre les relations et propriétés de navigation ;
- distinguer `IEnumerable<T>` et `IQueryable<T>` ;
- comprendre le tracking et `AsNoTracking()` ;
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

- une **colonne** décrit un champ ;
- une **ligne** représente un enregistrement ;
- une **table** regroupe des enregistrements de même nature.

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

1. prendre la table `Orders` ;
2. garder les lignes avec `Total >= 100` ;
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

Même si EF Core masque souvent la syntaxe SQL, il est important de comprendre qu'une navigation entre entités peut entraîner des jointures ou plusieurs requêtes.

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
3. enregistrer le paiement
```

Selon le besoin, on veut que l'ensemble soit validé ou annulé de manière cohérente.

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

Le `DbContext` représente une session de travail avec la base.

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

`DbSet<Order>` représente le point d'entrée vers les entités `Order`.

---

## 11. Configuration avec la DI

Dans `Program.cs` :

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // configuration du provider et de la connexion
});
```

Le contexte est ensuite injecté dans les services qui en ont besoin.

Il est généralement `Scoped`, ce qui correspond naturellement au cycle de vie d'une requête web.

---

## 12. Créer et sauvegarder

```csharp
var order = new Order(...);

await dbContext.Orders.AddAsync(order, cancellationToken);
await dbContext.SaveChangesAsync(cancellationToken);
```

`AddAsync` prépare l'ajout dans le contexte. `SaveChangesAsync` déclenche réellement les écritures nécessaires en base.

---

## 13. Lire

```csharp
var order = await dbContext.Orders
    .FirstOrDefaultAsync(
        x => x.Id == id,
        cancellationToken);
```

On retrouve la syntaxe LINQ, mais la source n'est plus une simple collection mémoire.

---

## 14. `IEnumerable<T>` vs `IQueryable<T>`

C'est une différence fondamentale.

### `IEnumerable<T>`

On travaille conceptuellement sur des objets déjà disponibles côté .NET.

```text
Objects in memory
      ↓
LINQ to Objects
```

### `IQueryable<T>`

On construit une représentation d'une requête pouvant être traduite par un provider.

```text
LINQ expression
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
    .Where(x => x.Total > 100m)
    .OrderByDescending(x => x.Total)
    .Take(10);
```

À ce stade, la requête peut ne pas avoir été exécutée.

Puis :

```csharp
var orders = await query.ToListAsync(cancellationToken);
```

La requête est matérialisée.

### Question essentielle

Que se passe-t-il si tu fais `ToList()` trop tôt puis continues à filtrer ?

Tu risques de charger beaucoup plus de données en mémoire avant le filtrage.

---

## 15. Tracking

Par défaut, EF Core peut suivre les entités chargées afin de détecter leurs modifications.

```csharp
var order = await dbContext.Orders
    .FirstAsync(x => x.Id == id, cancellationToken);

order.Confirm();
await dbContext.SaveChangesAsync(cancellationToken);
```

EF sait alors quelles modifications enregistrer.

Pour une lecture seule :

```csharp
var orders = await dbContext.Orders
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

`AsNoTracking()` évite le coût du suivi lorsque l'on ne compte pas modifier les entités chargées.

---

## 16. Relations et propriétés de navigation

Exemple :

```csharp
public class Order
{
    public Guid Id { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
```

`Items` peut représenter une navigation vers des lignes d'une autre table.

Attention : le simple fait d'avoir une propriété de navigation ne signifie pas forcément que toutes les données seront toujours chargées automatiquement.

Il faut comprendre les stratégies de chargement choisies par l'application.

---

## 17. Migrations

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

Les migrations sont versionnées afin que plusieurs environnements puissent appliquer les mêmes changements de schéma.

### Important

Une migration mérite d'être relue. Elle modifie la structure de la base et peut avoir des conséquences sur les données existantes.

---

## 18. Le piège N+1

Supposons :

```text
1 requête pour charger 100 commandes
+ 1 requête par commande pour charger ses items
```

On obtient potentiellement :

```text
1 + 100 = 101 requêtes
```

Même si le code C# semble simple, le coût réel est important.

### Réflexe à acquérir

Quand tu écris une requête EF Core, demande-toi :

- combien de lignes sont chargées ?
- combien de colonnes ?
- combien de requêtes ?
- le filtrage se fait-il en base ou en mémoire ?

---

## Exercice — traduction mentale

Considère :

```csharp
var result = await dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Total >= 100m)
    .OrderByDescending(x => x.Total)
    .Select(x => new OrderSummary(x.Id, x.Total))
    .Take(20)
    .ToListAsync(cancellationToken);
```

Explique :

1. quelles opérations doivent idéalement être exécutées en base ;
2. ce que change `AsNoTracking()` ;
3. pourquoi le `Select` peut réduire les données transférées ;
4. ce que déclenche `ToListAsync()`.

---

## Application au projet fil rouge

Remplacer progressivement :

```text
InMemoryOrderRepository
```

par une implémentation utilisant `AppDbContext`.

Le contrat `IOrderRepository` et la logique de `OrderService` devraient changer le moins possible.

C'est un test concret de la qualité du découplage réalisé dans les chapitres précédents.
