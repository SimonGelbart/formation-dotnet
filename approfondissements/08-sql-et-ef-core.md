# 8 — SQL et Entity Framework Core

> **Prérequis conseillé :** [parcours principal — 07](../parcours/07-persistance.md).
>
> **Niveau :** À approfondir pour EF et SQL · Nuance pour traduction et chargement · Référence pour mapping avancé.
>
> **Statut des exemples :** extraits indépendants et variantes de conception. Ils ne constituent pas une suite de modifications à appliquer à Catalogue.Api. Les types manquants sont à définir dans une expérience séparée. Pour le code exécutable et ses signatures exactes, consulte les [applications du parcours](../parcours/README.md#environnement-et-applications-de-référence).

## Questions abordées

Cette référence aide à comprendre, selon ton besoin :

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
- reconnaître N+1 et les matérialisations trop précoces.

Ce chapitre garde un seul modèle cohérent :

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

`Order.Total` est calculé à partir des items. Il n'est pas stocké dans une colonne `Orders.Total`.

---

## Variante utilisée dans ce chapitre

Ce modèle ajoute CustomerId et un constructeur `Order(customerId)`. Il n'est pas celui de Catalogue.Api, qui utilise `new Order()` et `AddItem(Product, quantity)`. Les tables de lignes sont nommées OrderItems dans ces exemples SQL, contre OrderItem dans la référence principale. Adapte le nom au schéma de ta migration.

Les dates sont stockées en DateTime UTC pour les tris SQLite. Les opérations sur decimal et leur traduction dépendent du provider et de sa version : inspecte la requête et exécute-la sur la base choisie avant de réutiliser une projection. Pour le premier parcours, le total est calculé après chargement des lignes. Une projection de résumé n'a pas besoin de charger les entités ; une règle métier utilisant Items, elle, exige que les lignes soient disponibles.

# 1. SQL avant EF Core

EF Core permet d'écrire des requêtes en C#, mais la base reste relationnelle.

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

Pour calculer le total de toutes les commandes, y compris les commandes vides :

```sql
SELECT o.Id,
       COALESCE(SUM(i.UnitPrice * i.Quantity), 0) AS Total
FROM Orders o
LEFT JOIN OrderItems i ON i.OrderId = o.Id
GROUP BY o.Id;
```

Pourquoi `LEFT JOIN` ? Une commande `Draft` peut exister avant d'avoir des items. Un `INNER JOIN` l'exclurait du résultat.

`COALESCE(..., 0)` transforme le total `NULL` d'une commande sans item en `0`.

Cette requête montre aussi une idée importante : le total peut être **calculé à la lecture** sans devenir automatiquement une colonne persistée.

---

## 2. Tables, clés et relations

Une **Primary Key** identifie une ligne :

```text
Orders.Id
OrderItems.Id
```

Une **Foreign Key** relie deux tables :

```text
OrderItems.OrderId → Orders.Id
```

La relation est :

```text
Order 1 ───── * OrderItems
```

Reconnais au minimum :

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
 ↓ filtrer
 ↓ sélectionner
 ↓ trier
```

Cette lecture prépare LINQ to Entities.

---

## 4. Jointures

```sql
SELECT o.Id, i.ProductName, i.Quantity
FROM Orders o
JOIN OrderItems i ON i.OrderId = o.Id;
```

`JOIN` ne garde que les commandes ayant une ligne correspondante.

`LEFT JOIN` garde aussi les commandes sans item.

EF Core peut produire des jointures ou plusieurs requêtes selon la requête LINQ et la stratégie de chargement choisie.

---

## 5. Index

Un index peut accélérer les recherches sur des colonnes réellement utilisées comme critères.

Exemples possibles :

```text
Orders.CustomerId
Orders.CreatedAt
OrderItems.OrderId
```

Un index coûte aussi du stockage et du travail lors des écritures.

> Ajoute un index pour répondre à un besoin de requête, pas simplement parce qu'une colonne existe.

---

## 6. Transactions

Une transaction regroupe plusieurs changements dans une unité cohérente.

```text
ajouter Order
ajouter OrderItems
sauvegarder
```

Un `SaveChanges` relationnel est généralement transactionnel pour les modifications qu'il envoie lors de cet appel. Plusieurs `SaveChanges` ou plusieurs systèmes externes demandent davantage de réflexion.

---

# 7. Installer EF Core avec SQLite

Dans le projet API :

```bash
dotnet package add Microsoft.EntityFrameworkCore.Sqlite
dotnet package add Microsoft.EntityFrameworkCore.Design
```

Puis :

```bash
dotnet ef --version
```

SQLite est pratique ici : c'est une vraie base relationnelle, légère et locale.

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

Un `DbContext` :

- représente une unité de travail courte ;
- maintient un change tracker ;
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

Le domaine appris plus tôt doit rester protecteur.

```csharp
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public decimal Total => _items.Sum(x => x.Subtotal);

    private Order()
    {
    }

    public Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                "A confirmed order cannot be modified.");

        var item = new OrderItem(
            Id,
            productId,
            productName,
            unitPrice,
            quantity);

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

La commande crée elle-même ses lignes. Une ligne appartenant à cette commande reçoit donc immédiatement le bon `OrderId`.

---

# 11. `OrderItem` et la relation

```csharp
public sealed class OrderItem
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

    internal OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(
                "Product name is required.",
                nameof(productName));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
```

`internal` permet à `Order` de créer une ligne dans le même projet métier sans exposer ce constructeur comme API publique générale.

---

# 12. Mapper la collection privée `_items`

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

Le domaine garde :

```csharp
private readonly List<OrderItem> _items = [];
public IReadOnlyCollection<OrderItem> Items => _items;
```

Il n'est pas nécessaire de réouvrir un setter public simplement pour satisfaire l'ORM.

---

# 13. Créer et sauvegarder

```csharp
var order = new Order(customerId);

dbContext.Orders.Add(order);
await dbContext.SaveChangesAsync(cancellationToken);
```

`Add` marque l'entité comme nouvelle. L'écriture réelle se produit à `SaveChangesAsync`.

`AddAsync` existe, mais n'est pas le réflexe par défaut pour une entité classique dont la clé est déjà disponible côté application.

---

# 14. Sauvegarder une modification existante

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstAsync(x => x.Id == id, cancellationToken);

order.Confirm();

await dbContext.SaveChangesAsync(cancellationToken);
```

Le change tracker voit que `Status` a changé, mais **la base n'est modifiée qu'au `SaveChangesAsync`**.

Dans notre repository pédagogique, cette frontière reste explicite :

```csharp
public Task SaveChangesAsync(
    CancellationToken cancellationToken)
{
    return _dbContext.SaveChangesAsync(cancellationToken);
}
```

L'implémentation mémoire peut faire de cette méthode un no-op.

---

# 15. Lire et charger les relations

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstOrDefaultAsync(
        x => x.Id == id,
        cancellationToken);
```

La syntaxe ressemble à LINQ sur collections, mais EF Core analyse l'expression et traduit ce qu'il sait traduire vers SQL.

### Eager loading

```csharp
.Include(x => x.Items)
```

### Explicit loading

Charger volontairement une navigation plus tard via le contexte.

### Lazy loading

Charger une navigation au moment où elle est accédée, si ce mécanisme a été explicitement configuré.

Le lazy loading peut rendre le nombre réel de requêtes moins visible.

---

# 16. `IEnumerable<T>` vs `IQueryable<T>`

`IEnumerable<T>` exprime surtout :

> cette séquence peut être énumérée.

`IQueryable<T>` transporte en plus une représentation de requête qu'un provider peut analyser.

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

La requête n'est généralement pas encore exécutée.

```csharp
var orders = await query.ToListAsync(cancellationToken);
```

matérialise le résultat.

---

# 17. Ne pas matérialiser trop tôt

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

Mieux :

```csharp
var expensive = await dbContext.Orders
    .Where(x => x.Items.Sum(
        i => i.UnitPrice * i.Quantity) >= 100m)
    .ToListAsync(cancellationToken);
```

Le provider peut traduire le calcul vers SQL.

---

# 18. Voir le SQL avec `ToQueryString()`

```csharp
var query = dbContext.Orders
    .Where(x => x.CustomerId == customerId)
    .OrderByDescending(x => x.CreatedAt)
    .Take(10);

Console.WriteLine(query.ToQueryString());
```

Exercice : ajoute un `Where`, un `Select`, inspecte le SQL, puis matérialise volontairement trop tôt et compare.

---

# 19. Tracking et `AsNoTracking()`

EF suit par défaut les entités chargées afin de détecter leurs modifications.

Pour une lecture seule :

```csharp
var orders = await dbContext.Orders
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

`AsNoTracking()` évite une partie du travail du change tracker lorsque les entités ne seront pas modifiées puis sauvegardées.

---

# 20. Migrations

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

# 21. N+1

Scénario :

```text
1 requête pour 100 commandes
+ 1 requête par commande pour ses items
= 101 requêtes
```

Le problème vient d'une stratégie de chargement qui déclenche des requêtes répétées, pas de la simple présence d'une navigation.

Pose-toi toujours :

- combien de requêtes ?
- combien de lignes ?
- combien de colonnes ?
- filtrage en base ou en mémoire ?
- ai-je besoin d'entités complètes ?

---

# 22. Projection : lire juste ce dont on a besoin

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

Observe :

```csharp
Console.WriteLine(summariesQuery.ToQueryString());
```

Puis matérialise :

```csharp
var summaries = await summariesQuery
    .ToListAsync(cancellationToken);
```

Cette requête exprime directement un calcul que le provider peut traduire, au lieu de dépendre de la propriété C# `Order.Total` non mappée.

---

## Exercice — mapping + migration

1. configure `Order` et `OrderItem` ;
2. génère `InitialCreate` ;
3. vérifie que `Orders` ne contient pas de colonne `Total` ;
4. vérifie `OrderItems.OrderId`, `UnitPrice` et `Quantity` ;
5. applique la migration ;
6. crée une commande avec un item ;
7. relis-la avec ses items ;
8. confirme-la puis appelle `SaveChangesAsync` ;
9. relis-la dans un nouveau scope et vérifie le statut ;
10. affiche le SQL d'une projection `OrderSummary`.

<details>
<summary>Critères de réussite</summary>

- le domaine garde sa collection privée ;
- chaque `OrderItem` possède le bon `OrderId` dès sa création ;
- EF remplit la navigation via le backing field ;
- `OrderItem.UnitPrice` est persisté ;
- `Order.Total` reste un calcul du domaine ;
- une modification n'est persistée qu'après `SaveChangesAsync` ;
- les lectures de résumé calculent le total dans une expression traduisible.
</details>

---

## Expérience facultative — variante indépendante

Remplace progressivement `InMemoryOrderRepository` par `EfOrderRepository` sans réécrire les règles métier.

Le passage mémoire → EF doit surtout changer :

```text
composition / DI
persistence
mapping
requêtes
```

Le domaine conserve ses règles : confirmation, prix historique, quantité et calcul du total.
