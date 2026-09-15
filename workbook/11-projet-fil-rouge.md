# 11 — Projet fil rouge : Order API

## Objectif

Ce chapitre assemble le workbook dans une seule application.

Il ne répète pas tous les chapitres : il sert de **guide d'implémentation** et de checklist.

À la fin, l'application doit permettre :

```text
consulter un catalogue produit
créer une commande
ajouter des items
capturer le prix au moment de la commande
confirmer une commande
exposer ces opérations en HTTP
persister avec EF Core
tester domaine, service, API et persistence
```

---

# Étape 0 — Préparer la solution

```bash
dotnet new sln -n OrderApi
dotnet new webapi --use-controllers -n OrderApi
dotnet new xunit -n OrderApi.Tests

dotnet sln add OrderApi/OrderApi.csproj
dotnet sln add OrderApi.Tests/OrderApi.Tests.csproj

dotnet add OrderApi.Tests reference OrderApi
```

Avec .NET 10, `dotnet new sln` crée par défaut `OrderApi.slnx`.

Checkpoint :

```text
dotnet build
dotnet test
```

---

# Étape 1 — Construire le domaine

Créer :

```text
Product
Order
OrderItem
OrderStatus
```

## `OrderStatus`

```csharp
public enum OrderStatus
{
    Draft,
    Confirmed
}
```

## `Product`

Le prix représente le **prix actuel du catalogue**.

```csharp
public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Product() { }

    public Product(Guid id, string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        Id = id;
        Name = name;
        Price = price;
    }
}
```

## `OrderItem`

La ligne capture le prix historique.

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

    private OrderItem() { }

    internal OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(nameof(productName));

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

## `Order`

```csharp
public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public decimal Total => _items.Sum(x => x.Subtotal);

    private Order() { }

    public Order(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Status = OrderStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
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

        _items.Add(new OrderItem(
            Id,
            productId,
            productName,
            unitPrice,
            quantity));
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

Tests à écrire immédiatement :

- quantité <= 0 refusée ;
- prix négatif refusé ;
- `OrderItem.OrderId == Order.Id` ;
- total correct ;
- commande vide non confirmable ;
- commande confirmée non modifiable ;
- prix historique stable.

---

# Étape 2 — Introduire les abstractions en synchrone

Avant le chapitre async, commence simple :

```csharp
public interface IOrderRepository
{
    Order? GetById(Guid id);
    void Add(Order order);
    IReadOnlyCollection<Order> GetAll();
}
```

```csharp
public interface IProductCatalog
{
    Product? GetById(Guid id);
    IReadOnlyCollection<Product> GetAll();
}
```

Premières implémentations :

```text
InMemoryOrderRepository
InMemoryProductCatalog
```

Une recherche fréquente par ID justifie naturellement un :

```csharp
Dictionary<Guid, Order>
```

---

# Étape 3 — Construire `OrderService`

Le service orchestre les collaborateurs ; il ne recrée pas lui-même les repositories.

```csharp
public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductCatalog _products;

    public OrderService(
        IOrderRepository orders,
        IProductCatalog products)
    {
        _orders = orders;
        _products = products;
    }
}
```

Pour ajouter un item :

```text
ProductId
  ↓
chercher Product
  ↓
capturer Name + Price
  ↓
order.AddItem(...)
```

La commande, et non le service, crée réellement `OrderItem`.

---

# Étape 4 — Passer aux frontières async

Après le chapitre 5 :

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Order>> GetAllAsync(
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
```

Le catalogue devient également asynchrone si son implémentation fait de l'I/O.

Les règles métier restent synchrones :

```csharp
order.AddItem(...);
order.Confirm();
```

### Exemple de confirmation

```csharp
var order = await _orders.GetByIdAsync(
    id,
    cancellationToken);

if (order is null)
    return;

order.Confirm();

await _orders.SaveChangesAsync(cancellationToken);
```

La frontière est explicite :

```text
charger
→ modifier
→ sauvegarder
```

---

# Étape 5 — Ajouter une notification

```csharp
public interface IOrderNotifier
{
    Task OrderConfirmedAsync(
        Order order,
        CancellationToken cancellationToken);
}
```

Première implémentation :

```text
ConsoleOrderNotifier
```

Flux simple :

```text
charger
→ confirmer
→ sauvegarder
→ notifier
```

Le comportement exact si la notification échoue est un choix métier ; les systèmes de messaging sont hors scope initial.

---

# Étape 6 — Comprendre les lifetimes du faux stockage

Pour conserver un faux stockage entre requêtes :

```csharp
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddSingleton<IProductCatalog, InMemoryProductCatalog>();
builder.Services.AddScoped<IOrderNotifier, ConsoleOrderNotifier>();
builder.Services.AddScoped<OrderService>();
```

Un singleton mutable doit être utilisé avec prudence.

`ConcurrentDictionary<Guid, Order>` peut protéger les opérations sur le dictionnaire, mais **ne rend pas automatiquement les objets `Order` qu'il contient thread-safe**.

Le faux repository sert à apprendre les lifetimes ; ce n'est pas une base de données concurrente de production.

---

# Étape 7 — Exposer l'API HTTP

Endpoints minimums :

```text
GET  /products
GET  /products/{id}
POST /orders
POST /orders/{id}/items
GET  /orders/{id}
GET  /orders
POST /orders/{id}/confirm
```

DTOs minimums :

```csharp
public record CreateOrderRequest(Guid CustomerId);

public sealed class AddOrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}
```

Comportements :

```text
création réussie        → 201
ressource trouvée       → 200
ressource absente       → 404
entrée invalide         → 400
conflit d'état          → 409 selon le contrat choisi
```

Utilise :

```text
DTOs
CreatedAtAction
ProblemDetails
CancellationToken
OpenAPI
```

comme vu au chapitre 7.

---

# Étape 8 — Ajouter EF Core + SQLite

```bash
dotnet package add Microsoft.EntityFrameworkCore.Sqlite
dotnet package add Microsoft.EntityFrameworkCore.Design
```

Schéma cible :

```text
Products
- Id
- Name
- Price

Orders
- Id
- CustomerId
- Status
- CreatedAt

OrderItems
- Id
- OrderId
- ProductId
- ProductName
- UnitPrice
- Quantity
```

Il n'y a pas de colonne `Orders.Total` dans cette version.

Mapping important :

```csharp
order.HasMany(x => x.Items)
    .WithOne()
    .HasForeignKey(x => x.OrderId)
    .OnDelete(DeleteBehavior.Cascade);

order.Navigation(x => x.Items)
    .HasField("_items")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

Puis :

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Lis la migration avant de l'appliquer.

---

# Étape 9 — Implémenter le repository EF

Le repository EF utilise un `AppDbContext` scoped.

```csharp
public Task SaveChangesAsync(
    CancellationToken cancellationToken)
{
    return _dbContext.SaveChangesAsync(cancellationToken);
}
```

Composition :

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
```

Pourquoi le lifetime change ?

```text
repository mémoire singleton
→ sa collection était le stockage

repository EF scoped
→ les données vivent dans la base
→ le repository utilise un DbContext scoped
```

`OrderService` doit changer le moins possible.

---

# Étape 10 — Écrire des lectures efficaces

```csharp
public record OrderSummary(
    Guid Id,
    DateTimeOffset CreatedAt,
    decimal Total,
    OrderStatus Status);
```

```csharp
var query = dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .OrderByDescending(x => x.CreatedAt)
    .Select(x => new OrderSummary(
        x.Id,
        x.CreatedAt,
        x.Items.Sum(i => i.UnitPrice * i.Quantity),
        x.Status))
    .Take(5);
```

Avant la matérialisation :

```csharp
Console.WriteLine(query.ToQueryString());
```

Puis :

```csharp
var summaries = await query
    .ToListAsync(cancellationToken);
```

Tu dois savoir expliquer :

- pourquoi la requête n'est pas exécutée immédiatement ;
- pourquoi `AsNoTracking()` convient ici ;
- pourquoi on projette ;
- pourquoi `Order.Total` n'est pas utilisé directement dans la requête EF.

---

# Étape 11 — Tester

## Domaine

Teste directement :

```text
Order.Confirm
Order.AddItem
prix historique
OrderId de la ligne
Total
```

## Service

Utilise des fakes :

```text
produit absent
prix capturé
SaveChangesAsync appelé après modification
notification demandée après confirmation
```

## Intégration

Utilise `WebApplicationFactory<Program>` + SQLite in-memory.

Avant chaque scénario :

```csharp
await _factory.ResetDatabaseAsync();
```

puis seed uniquement les données nécessaires.

Scénario essentiel :

```text
reset DB
→ seed produit
→ POST /orders
→ POST /orders/{id}/items
→ POST /orders/{id}/confirm
→ GET /orders/{id}
→ vérifier statut + total
```

---

# Étape 12 — Refactorer seulement si le besoin apparaît

Observe :

- domaine noyé dans ASP.NET Core ?
- EF utilisé partout ?
- DTOs HTTP dans le métier ?
- projet difficile à naviguer ?
- dépendances confuses ?

Si oui, envisage :

```text
OrderApi.Api
OrderApi.Application
OrderApi.Domain
OrderApi.Infrastructure
OrderApi.Tests
```

Chaque extraction doit résoudre un problème identifiable.

---

# Étape 13 — Challenges patterns

Une fois le projet fonctionnel :

```text
Strategy
→ plusieurs modes de frais de livraison

Adapter
→ intégrer un client email tiers derrière IOrderNotifier

Decorator
→ ajouter du logging autour du notifier
```

N'ajoute pas un pattern uniquement parce qu'il existe dans le chapitre précédent.

---

# Critères de fin de parcours

Tu dois pouvoir expliquer sans relire le workbook :

1. pourquoi `Order` protège ses invariants ;
2. pourquoi `OrderItem` capture `UnitPrice` ;
3. pourquoi `Order` crée ses propres items et renseigne `OrderId` ;
4. pourquoi le code commence synchrone avant de passer à `Task` ;
5. pourquoi `OrderService` reçoit ses dépendances ;
6. pourquoi `SaveChangesAsync` est nécessaire après une modification EF ;
7. pourquoi le repository mémoire et le repository EF n'ont pas le même lifetime ;
8. pourquoi `ConcurrentDictionary` ne rend pas tout l'agrégat thread-safe ;
9. pourquoi `Order.Total` n'est pas une colonne ;
10. comment EF persiste la collection privée `_items` ;
11. quand une requête `IQueryable` est exécutée ;
12. pourquoi les tests d'intégration réinitialisent leur base ;
13. pourquoi une nouvelle couche doit résoudre un problème identifiable ;
14. quel problème concret justifie chaque pattern ajouté.

---

# Pour aller plus loin

Après maîtrise de ce projet :

```text
authentication / authorization
Docker
caching / Redis
messaging
observability
CQRS
microservices
cloud / Kubernetes
```

Ces sujets ne sont pas nécessaires pour valider les fondamentaux du workbook.
