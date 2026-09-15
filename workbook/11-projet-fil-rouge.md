# 11 — Projet fil rouge : Order API

## Objectif

Ce projet consolide le workbook dans une seule application qui évolue progressivement.

Le but n'est pas de construire une architecture parfaite dès le départ. On commence simple, puis on introduit les abstractions lorsqu'un problème concret apparaît.

À la fin, l'application doit permettre :

```text
consulter un catalogue produit
créer une commande
ajouter des items
capturer le prix au moment de la commande
calculer le total
confirmer une commande
exposer ces opérations en HTTP
persister avec EF Core
interroger efficacement les données
tester domaine, application, API et persistence
refactorer l'architecture si le besoin apparaît
```

---

# Étape 0 — Préparer la solution

Avec .NET 10 :

```bash
dotnet new sln -n OrderApi
dotnet new webapi --use-controllers -n OrderApi
dotnet new xunit -n OrderApi.Tests

dotnet sln add OrderApi/OrderApi.csproj
dotnet sln add OrderApi.Tests/OrderApi.Tests.csproj

dotnet add OrderApi.Tests reference OrderApi
```

`dotnet new sln` crée par défaut `OrderApi.slnx` avec .NET 10.

Checkpoint :

```text
dotnet --info fonctionne
dotnet build fonctionne
dotnet test fonctionne
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

Le prix représente **le prix actuel du catalogue**.

```csharp
public sealed class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product()
    {
        Name = string.Empty;
    }

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

## `OrderItem` : snapshot du prix

Une commande historique ne doit pas changer lorsque le catalogue change.

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

    private Order()
    {
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

### Tests à écrire immédiatement

- produit avec prix négatif refusé ;
- quantité <= 0 refusée ;
- sous-total correct ;
- total correct ;
- commande vide non confirmable ;
- commande confirmée non modifiable ;
- `CreatedAt` est renseigné ;
- le prix d'un item reste stable même si le catalogue change ensuite.

<details>
<summary>Pourquoi ajouter les tests maintenant ?</summary>

Ces règles sont purement métier et ne nécessitent ni ASP.NET Core ni EF Core. C'est le moment le plus simple pour les documenter avec des tests.
</details>

---

# Étape 2 — Introduire les abstractions sans async

Pour rester cohérent avec le chapitre 3, commence par des contrats synchrones :

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

Pour une recherche fréquente par ID :

```csharp
Dictionary<Guid, Order>
```

est plus naturel qu'un scan répété d'une `List<Order>`.

---

# Étape 3 — Construire `OrderService`

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

Opérations à implémenter :

```text
CreateOrder
AddItem
GetById
GetAll
Confirm
```

Pour ajouter un item :

```text
ProductId
  ↓
IProductCatalog
  ↓
Product courant
  ↓
capturer Name + Price
  ↓
new OrderItem(...)
  ↓
order.AddItem(...)
```

### Exercice

Implémente `AddItem` sans passer l'objet `Product` directement dans `OrderItem`.

<details>
<summary>Indice</summary>

Le service possède le catalogue. Il peut obtenir le produit puis transmettre au domaine les données historiques à capturer : `ProductId`, `Name`, `Price`.
</details>

---

# Étape 4 — Faire évoluer le projet vers async

Après le chapitre 5, transforme les frontières I/O :

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
}
```

```csharp
public interface IProductCatalog
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Product>> GetAllAsync(
        CancellationToken cancellationToken);
}
```

Fais évoluer les méthodes du service en conséquence.

### Ne rends pas le domaine async

Ceci reste synchrone :

```csharp
order.AddItem(item);
order.Confirm();
```

Il n'y a aucune I/O dans ces règles.

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

Lors de `ConfirmAsync` :

```text
charger la commande
 ↓
order.Confirm()
 ↓
persister si nécessaire
 ↓
notifier
```

Réfléchis à ce qui doit se passer si la notification échoue : ce choix dépend du contrat métier et prépare les discussions plus avancées sur transactions/messages, hors scope initial.

---

# Étape 6 — Comprendre le stockage mémoire et les lifetimes

Si `InMemoryOrderRepository` possède :

```csharp
private readonly Dictionary<Guid, Order> _orders = [];
```

et est `Scoped`, chaque nouvelle requête HTTP recevra normalement une nouvelle instance : les données semblent disparaître.

Pour le faux stockage pédagogique :

```csharp
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddSingleton<IProductCatalog, InMemoryProductCatalog>();
builder.Services.AddScoped<IOrderNotifier, ConsoleOrderNotifier>();
builder.Services.AddScoped<OrderService>();
```

### Attention

Un singleton mutable est partagé entre requêtes. Son stockage doit être conçu pour la concurrence.

Tu peux utiliser par exemple :

```csharp
ConcurrentDictionary<Guid, Order>
```

ou séparer :

```text
Scoped repository
   ↓
Singleton in-memory store
```

---

# Étape 7 — Exposer l'API HTTP

Endpoints :

```text
GET  /products
GET  /products/{id}
POST /orders
POST /orders/{id}/items
GET  /orders/{id}
GET  /orders
POST /orders/{id}/confirm
```

DTOs :

```csharp
public record CreateOrderRequest(Guid CustomerId);

public sealed class AddOrderItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    decimal Total,
    IReadOnlyCollection<OrderItemResponse> Items);
```

Comportements :

```text
POST /orders                 → 201
GET /orders/{id} trouvé      → 200
GET /orders/{id} absent      → 404
quantité invalide            → 400
commande déjà confirmée      → 409 selon le contrat choisi
```

Utilise `CreatedAtAction` pour la création.

---

# Étape 8 — Centraliser les erreurs et vérifier OpenAPI

Ajoute :

```csharp
builder.Services.AddProblemDetails();
```

```csharp
app.UseExceptionHandler();
```

et OpenAPI :

```csharp
builder.Services.AddOpenApi();
```

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

Vérifie que le document reflète réellement les endpoints et DTOs attendus.

---

# Étape 9 — Ajouter EF Core + SQLite

Packages :

```bash
dotnet package add Microsoft.EntityFrameworkCore.Sqlite
dotnet package add Microsoft.EntityFrameworkCore.Design
```

Configuration :

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Database"));
});
```

Le modèle relationnel cible :

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

**Pas de colonne `Orders.Total` dans cette version.**

---

# Étape 10 — Mapper sans casser l'encapsulation

Dans `OnModelCreating` :

```csharp
var order = modelBuilder.Entity<Order>();

order.HasKey(x => x.Id);

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
```

Le domaine conserve :

```csharp
private readonly List<OrderItem> _items = [];
public IReadOnlyCollection<OrderItem> Items => _items;
```

Ne réouvre pas un setter public juste pour EF Core.

---

# Étape 11 — Migration

```bash
dotnet ef migrations add InitialCreate
```

Avant d'appliquer :

- lis le fichier ;
- vérifie les trois tables ;
- vérifie `OrderItems.OrderId` ;
- vérifie l'absence de `Orders.Total`.

Puis :

```bash
dotnet ef database update
```

---

# Étape 12 — Passer du repository mémoire à EF

Créer :

```text
EfOrderRepository
EfProductCatalog
```

Puis changer la composition :

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
```

Le service doit changer le moins possible.

### Pourquoi le lifetime change ?

```text
repository mémoire singleton
→ sa collection était elle-même le stockage

repository EF scoped
→ les données vivent dans la base
→ le repository utilise un DbContext scoped
```

---

# Étape 13 — Requêtes de lecture efficaces

Résumé :

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

Avant d'exécuter :

```csharp
Console.WriteLine(query.ToQueryString());
```

Puis :

```csharp
var summaries = await query
    .ToListAsync(cancellationToken);
```

Questions :

- où le filtre est-il exécuté ?
- quand la requête part-elle en base ?
- pourquoi `AsNoTracking()` ?
- pourquoi projeter ?
- pourquoi ne pas écrire simplement `.Where(x => x.Total > 100m)` si `Total` est une propriété C# calculée non mappée ?

---

# Étape 14 — Chargement des relations

Commande complète :

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstOrDefaultAsync(
        x => x.Id == id,
        cancellationToken);
```

Liste de résumés : préfère souvent une projection plutôt que charger des graphes complets.

Exercice : provoque volontairement un scénario N+1, compte les requêtes, puis refactore.

---

# Étape 15 — Tests unitaires

Écris au minimum :

```text
Order.Confirm commande vide
Order.AddItem commande confirmée
OrderItem prix snapshot
OrderService.AddItem produit absent
OrderService.AddItem capture le prix
OrderService.Confirm appelle le notifier
```

Utilise d'abord des fakes simples.

---

# Étape 16 — Tests d'intégration

Utilise `WebApplicationFactory<Program>` avec SQLite in-memory comme montré au chapitre 9.

Scénario essentiel :

```text
POST /orders
→ 201 + Location
→ POST /orders/{id}/items
→ POST /orders/{id}/confirm
→ GET Location
→ 200
→ total et statut corrects
```

Ce test doit traverser :

```text
HTTP
routing/binding
DI
service
EF Core
SQLite
```

---

# Étape 17 — Refactorer seulement maintenant

Observe :

- domaine noyé dans ASP.NET Core ?
- EF utilisé partout ?
- DTOs HTTP utilisés dans le métier ?
- projet difficile à naviguer ?
- dépendances devenues confuses ?

Si oui, envisage :

```text
OrderApi.Api
OrderApi.Application
OrderApi.Domain
OrderApi.Infrastructure
OrderApi.Tests
```

Pour chaque extraction, justifie le problème qu'elle résout.

---

# Étape 18 — Challenges patterns

## Strategy

Ajouter :

```text
Standard
Express
International
```

pour les frais de livraison.

## Adapter

Créer :

```text
ThirdPartyEmailClient
      ↓ adapter
IOrderNotifier
```

## Decorator

Ajouter du logging autour du notifier sans modifier son implémentation principale.

---

# Critères de fin de parcours

Tu dois pouvoir répondre sans relire le workbook :

1. Pourquoi `Order` protège-t-il ses invariants ?
2. Pourquoi `OrderItem` capture-t-il `UnitPrice` ?
3. Pourquoi le code commence-t-il synchrone avant de passer à `Task` ?
4. Pourquoi `OrderService` reçoit-il ses dépendances ?
5. Pourquoi le repository mémoire a-t-il un lifetime différent du repository EF ?
6. Pourquoi un singleton mutable doit-il être thread-safe ?
7. Pourquoi `Order.Total` n'est-il pas une colonne dans notre schéma ?
8. Comment EF persiste-t-il la collection privée `_items` ?
9. Quand une requête `IQueryable` est-elle exécutée ?
10. Pourquoi projeter un `OrderSummary` ?
11. Pourquoi ne pas lancer plusieurs opérations sur le même `DbContext` ?
12. Quelle différence entre un test domaine, service et HTTP ?
13. Pourquoi SQLite in-memory est-il plus pertinent qu'EF InMemory pour certains tests relationnels ?
14. Pourquoi une nouvelle couche doit-elle résoudre un problème identifiable ?
15. Quel problème concret justifie chaque pattern ajouté ?

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

Ils ne sont pas nécessaires pour valider les fondamentaux de ce workbook.
