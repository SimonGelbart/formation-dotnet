# 8 — SQL et Entity Framework Core

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- comprendre les concepts SQL minimums nécessaires à EF Core ;
- expliquer le rôle d'un ORM ;
- utiliser `DbContext` et `DbSet<T>` ;
- configurer SQLite et une chaîne de connexion ;
- créer et appliquer des migrations ;
- persister un modèle métier encapsulé ;
- comprendre les relations et propriétés de navigation ;
- distinguer `IEnumerable<T>` et `IQueryable<T>` ;
- comprendre tracking et `AsNoTracking()` ;
- comprendre eager / explicit / lazy loading ;
- inspecter le SQL avec `ToQueryString()` ;
- reconnaître N+1 et autres problèmes de requêtes.

Ce chapitre garde **un seul modèle Order cohérent** :

```text
Order
- Id
- CustomerId
- Status
- CreatedAt
- Items

OrderItem
- Id
- OrderId
- ProductId
- ProductName
- UnitPrice
- Quantity
```

`Order.Total` est calculé à partir des items. Il n'est pas présenté ici comme une colonne stockée.

---

# 1. SQL avant EF Core

EF Core permet d'écrire des requêtes en C#, mais la base reste relationnelle.

Schéma simplifié :

```text
Orders
┌────────────┬────────────┬───────────┬────────────────────┐
│ Id         │ CustomerId │ Status    │ CreatedAt          │
└────────────┴────────────┴───────────┴────────────────────┘

OrderItems
┌────────────┬────────────┬───────────┬─────────────┬───────────┬──────────┐
│ Id         │ OrderId    │ ProductId │ ProductName │ UnitPrice │ Quantity │
└────────────┴────────────┴───────────┴─────────────┴───────────┴──────────┘
```

Le total d'une commande peut être obtenu par agrégation :

```sql
SELECT o.Id,
       SUM(i.UnitPrice * i.Quantity) AS Total
FROM Orders o
JOIN OrderItems i ON i.OrderId = o.Id
GROUP BY o.Id;
```

Cette décision évite une contradiction entre :

```text
Total calculé dans le domaine
```

et :

```text
Total stocké automatiquement en base
```

On pourrait choisir de stocker un total pour des raisons métier ou de performance, mais ce serait alors une décision explicite avec une stratégie de cohérence.

---

## 2. Tables, clés et relations

Une table regroupe des lignes structurées en colonnes.

### Primary Key

Identifie une ligne :

```text
Orders.Id
OrderItems.Id
```

### Foreign Key

Relie des tables :

```text
OrderItems.OrderId → Orders.Id
```

La relation est :

```text
Order 1 ───── * OrderItems
```

Il faut reconnaître au minimum :

```text
one-to-one
one-to-many
many-to-many
```

---

## 3. Lire une requête SQL

```sql
SELECT Id, CustomerId, Status
FROM Orders
WHERE Status = 1
ORDER BY CreatedAt DESC;
```

Lis-la comme une pipeline :

```text
Orders
 ↓ filtrer les confirmées
 ↓ sélectionner certaines colonnes
 ↓ trier par date décroissante
```

Cette lecture prépare LINQ to Entities.

---

## 4. Jointures

```sql
SELECT o.Id, i.ProductName, i.Quantity
FROM Orders o
JOIN OrderItems i ON i.OrderId = o.Id;
```

Une navigation EF peut provoquer une jointure ou plusieurs requêtes selon la manière dont la requête est écrite et les relations chargées.

---

## 5. Index

Un index peut accélérer les recherches sur des colonnes réellement utilisées comme critères.

Exemples potentiels :

```text
Orders.CustomerId
Orders.CreatedAt
OrderItems.OrderId
```

Un index a aussi un coût : stockage et maintenance lors des écritures.

> Ajouter un index parce qu'une colonne existe n'est pas une stratégie. Il doit répondre à un besoin de requête.

---

## 6. Transactions

Une transaction regroupe plusieurs opérations dans une unité cohérente.

```text
ajouter Order
ajouter OrderItems
sauvegarder
```

Un `SaveChanges` relationnel est généralement transactionnel pour l'ensemble des modifications qu'il envoie. Plusieurs `SaveChanges` ou plusieurs systèmes externes demandent davantage de réflexion.

---

# 7. Installer EF Core avec SQLite

Pour le workbook, SQLite est pratique : vraie base relationnelle, installation légère, fichier local.

Dans le projet API :

```bash
dotnet package add Microsoft.EntityFrameworkCore.Sqlite
```

Pour les migrations :

```bash
dotnet package add Microsoft.EntityFrameworkCore.Design
```

Puis vérifier l'outil :

```bash
dotnet ef --version
```

Le chapitre 6 explique la différence entre package NuGet et outil `dotnet`.

---

# 8. `DbContext`

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // configuration détaillée plus bas
    }
}
```

`DbContext` représente une unité de travail courte avec la base et maintient notamment un change tracker.

### Important

Un `DbContext` :

- est normalement de courte durée ;
- n'est pas thread-safe ;
- ne doit pas servir à plusieurs opérations concurrentes simultanées.

---

# 9. Configuration DI et connexion

`appsettings.json` :

```json
{
  "ConnectionStrings": {
    "Database": "Data Source=orders.db"
  }
}
```

`Program.cs` :

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Database"));
});
```

`AddDbContext` enregistre normalement le contexte avec un lifetime scoped.

---

# 10. Garder le domaine encapsulé

Le domaine appris au chapitre 2 doit rester protecteur.

```csharp
public class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public decimal Total => _items.Sum(x => x.Subtotal);

    private Order()
    {
        // utilisé par EF Core
    }

    public Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                "A confirmed order cannot be modified.");

        _items.Add(item);
    }

    public void Confirm()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException(
                "An empty order cannot be confirmed.");

        Status = OrderStatus.Confirmed;
    }
}
```

### Pourquoi des setters privés ?

EF Core peut mapper des propriétés avec setters privés. Le code applicatif ne reçoit pas pour autant le droit de les modifier arbitrairement.

### Pourquoi un constructeur privé ?

Il donne à EF une voie simple de matérialisation sans exposer un constructeur métier incomplet aux consommateurs.

Ce n'est pas la seule stratégie possible, mais elle est facile à comprendre pour ce workbook.

---

# 11. Mapper la collection privée `_items`

Le but est de **ne pas remplacer** :

```csharp
private readonly List<OrderItem> _items = [];
public IReadOnlyCollection<OrderItem> Items => _items;
```

par :

```csharp
public List<OrderItem> Items { get; set; } = [];
```

juste pour satisfaire l'ORM.

Configuration :

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var order = modelBuilder.Entity<Order>();

    order.HasKey(x => x.Id);

    order.Property(x => x.Status)
        .HasConversion<int>();

    order.HasMany(x => x.Items)
        .WithOne()
        .HasForeignKey(x => x.OrderId)
        .OnDelete(DeleteBehavior.Cascade);

    order.Navigation(x => x.Items)
        .HasField("_items")
        .UsePropertyAccessMode(PropertyAccessMode.Field);

    var item = modelBuilder.Entity<OrderItem>();

    item.HasKey(x => x.Id);
    item.Property(x => x.ProductName).HasMaxLength(200);
    item.Property(x => x.UnitPrice).HasPrecision(18, 2);
}
```

Exemple cohérent d'`OrderItem` :

```csharp
public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal Subtotal => UnitPrice * Quantity;

    private OrderItem()
    {
    }

    public OrderItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
```

### Point pédagogique important

L'ORM doit s'adapter au modèle lorsque c'est raisonnable ; ne détruis pas l'encapsulation uniquement pour obtenir une convention plus facile à mapper.

---

# 12. Créer et sauvegarder

```csharp
var order = new Order(customerId);

dbContext.Orders.Add(order);
await dbContext.SaveChangesAsync(cancellationToken);
```

`Add` marque l'entité comme nouvelle dans le contexte. L'écriture réelle se produit à `SaveChangesAsync`.

`AddAsync` existe mais n'est pas à enseigner comme réflexe par défaut : son intérêt asynchrone concerne surtout certains générateurs de valeurs spéciaux.

---

# 13. Lire

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstOrDefaultAsync(
        x => x.Id == id,
        cancellationToken);
```

La syntaxe ressemble à LINQ sur collections, mais ici EF Core analyse l'expression et traduit ce qu'il sait traduire vers SQL.

---

# 14. `IEnumerable<T>` vs `IQueryable<T>`

`IEnumerable<T>` exprime surtout :

> cette séquence peut être énumérée.

Cela ne veut pas dire automatiquement « `List<T>` déjà en mémoire ».

`IQueryable<T>` transporte en plus une représentation de la requête qu'un provider peut analyser.

```text
Expression LINQ
    ↓
EF Core
    ↓
SQL
    ↓
Database
```

```csharp
var query = dbContext.Orders
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.CreatedAt)
    .Take(10);
```

La requête n'est généralement pas encore envoyée.

```csharp
var orders = await query.ToListAsync(cancellationToken);
```

matérialise le résultat.

---

# 15. Ne pas matérialiser trop tôt

À éviter si on veut filtrer en base :

```csharp
var all = await dbContext.Orders
    .Include(x => x.Items)
    .ToListAsync(cancellationToken);

var expensive = all
    .Where(x => x.Total >= 100m)
    .ToList();
```

Toutes les commandes ont déjà été chargées.

Mieux : exprimer un calcul traduisible dans la requête :

```csharp
var expensive = await dbContext.Orders
    .Where(x => x.Items.Sum(
        i => i.UnitPrice * i.Quantity) >= 100m)
    .ToListAsync(cancellationToken);
```

Ici le provider peut traduire l'agrégation vers SQL.

---

# 16. Voir le SQL avec `ToQueryString()`

```csharp
var query = dbContext.Orders
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.CreatedAt)
    .Take(10);

Console.WriteLine(query.ToQueryString());
```

### Exercice

1. ajoute un `Where` ;
2. ajoute un `Select` ;
3. affiche le SQL ;
4. matérialise trop tôt volontairement ;
5. observe quelle partie n'existe plus dans le SQL.

---

# 17. Tracking

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstAsync(x => x.Id == id, cancellationToken);

order.Confirm();
await dbContext.SaveChangesAsync(cancellationToken);
```

EF suit les entités chargées et détecte les changements.

Lecture seule :

```csharp
var orders = await dbContext.Orders
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

`AsNoTracking()` réduit le travail du change tracker quand les entités ne seront pas modifiées puis sauvegardées.

---

# 18. Charger les relations

### Eager loading

```csharp
.Include(x => x.Items)
```

### Explicit loading

Charger volontairement une navigation plus tard via le contexte.

### Lazy loading

Charger une navigation au moment où elle est accédée, si l'application a explicitement configuré ce mécanisme.

Le lazy loading n'est pas activé automatiquement et peut masquer le nombre réel de requêtes.

---

# 19. Migrations

Créer :

```bash
dotnet ef migrations add InitialCreate
```

Lire le fichier généré **avant** de l'appliquer.

Puis :

```bash
dotnet ef database update
```

Lister :

```bash
dotnet ef migrations list
```

Les migrations font partie du code versionné du projet.

---

# 20. N+1

Scénario :

```text
1 requête pour 100 commandes
+ 1 requête par commande pour ses items
= 101 requêtes
```

Le problème n'est pas la propriété de navigation en elle-même. Il apparaît lorsqu'une stratégie de chargement déclenche des requêtes répétées.

Pose-toi toujours :

- combien de requêtes ?
- combien de lignes ?
- combien de colonnes ?
- filtrage en base ou en mémoire ?
- ai-je besoin d'entités complètes ?

---

# 21. Projection : lire juste ce dont on a besoin

```csharp
public record OrderSummary(
    Guid Id,
    decimal Total,
    OrderStatus Status);
```

```csharp
var summariesQuery = dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .OrderByDescending(x => x.CreatedAt)
    .Select(x => new OrderSummary(
        x.Id,
        x.Items.Sum(i => i.UnitPrice * i.Quantity),
        x.Status))
    .Take(20);
```

Observe d'abord :

```csharp
Console.WriteLine(summariesQuery.ToQueryString());
```

Puis :

```csharp
var summaries = await summariesQuery
    .ToListAsync(cancellationToken);
```

Cette requête ne dépend pas de `Order.Total` comme propriété C# calculée ; elle exprime directement le calcul traduisible.

---

## Exercice — mapping + migration

À partir du modèle encapsulé :

1. configure `Order` et `OrderItem` dans `OnModelCreating` ;
2. génère `InitialCreate` ;
3. vérifie que **la table `Orders` ne contient pas de colonne `Total`** ;
4. vérifie que `OrderItems` contient `UnitPrice` et `Quantity` ;
5. vérifie la foreign key `OrderId` ;
6. applique la migration ;
7. crée une commande, relis-la avec ses items ;
8. affiche une projection `OrderSummary` et son SQL.

<details>
<summary>Critères de réussite</summary>

- le domaine garde sa collection privée ;
- EF remplit la navigation via le backing field ;
- `OrderItem.UnitPrice` est persisté ;
- `Order.Total` reste un calcul du domaine ;
- les lectures de résumé calculent le total dans une expression traduisible ;
- la base contient une vraie relation `Orders` → `OrderItems`.
</details>

---

## Application au projet fil rouge

Remplace progressivement `InMemoryOrderRepository` par `EfOrderRepository` sans réécrire les règles métier.

Le passage mémoire → EF doit surtout changer :

```text
composition / DI
persistence
mapping
requêtes
```

pas :

```text
règles de confirmation
prix historique
calcul métier d'un item
```

C'est le test concret du découplage construit depuis le début du workbook.
