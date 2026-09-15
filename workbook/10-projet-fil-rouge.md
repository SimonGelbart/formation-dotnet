# 10 — Projet fil rouge : Order API

## Objectif

Ce projet sert à consolider l'ensemble du workbook dans une seule application qui évolue progressivement.

Le but n'est pas de construire une architecture parfaite dès le départ. On commence simple, puis on introduit les abstractions lorsqu'un problème concret apparaît.

À la fin, l'application doit permettre :

```text
Créer / consulter des produits
Créer une commande
Ajouter des items à partir du catalogue
Capturer le prix au moment de la commande
Calculer le total
Confirmer une commande
Stocker les commandes
Lire et lister les commandes
Exposer ces opérations en HTTP
Persister les données avec EF Core
Tester les règles métier et l'API
```

Le projet doit rester cohérent entre ses versions « mémoire » et « EF Core ».

---

# Étape 0 — Préparer la solution

Avec .NET 10 :

```bash
dotnet new sln -n OrderApi
dotnet new webapi --use-controllers -n OrderApi
dotnet new xunit -n OrderApi.Tests

dotnet sln add OrderApi/OrderApi.csproj
dotnet sln add OrderApi.Tests/OrderApi.Tests.csproj
```

`dotnet new sln` crée par défaut un fichier `OrderApi.slnx` avec .NET 10.

Ajouter ensuite la référence du projet de tests vers le projet principal :

```bash
dotnet add OrderApi.Tests reference OrderApi
```

### Checkpoint

- `dotnet --info` montre le SDK attendu ;
- `dotnet build` fonctionne ;
- `dotnet test` fonctionne ;
- tu sais expliquer le rôle du `.slnx` et des `.csproj`.

---

# Étape 1 — Modèle métier simple

Créer au minimum :

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

Le statut appartient à un ensemble fini. Un `enum` évite les chaînes arbitraires et les fautes de frappe.

## Produit

Un produit :

- possède un `Id` ;
- possède un nom ;
- possède un prix catalogue >= 0.

Le prix du produit représente **le prix actuel du catalogue**.

## Item de commande : capturer l'historique

Une ligne de commande ne doit pas recalculer son sous-total depuis le prix actuel de `Product`.

Sinon :

```text
Commande créée lundi à 10 €
        ↓
Prix catalogue modifié mardi à 15 €
        ↓
Ancienne commande semble maintenant coûter 15 € ❌
```

Un item doit donc capturer le prix au moment où il est ajouté à la commande.

Exemple :

```csharp
public class OrderItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }

    public decimal Subtotal => UnitPrice * Quantity;

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

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
```

## Commande

Une commande :

- possède des items ;
- calcule son total à partir de `UnitPrice × Quantity` ;
- ne laisse pas le total être modifié directement ;
- ne peut pas être confirmée si elle est vide ;
- passe de `Draft` à `Confirmed` selon une règle explicite.

Exemple de collection encapsulée :

```csharp
public class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; } = Guid.NewGuid();
    public Guid CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyCollection<OrderItem> Items => _items;
    public decimal Total => _items.Sum(x => x.Subtotal);

    public Order(Guid customerId)
    {
        CustomerId = customerId;
    }

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("A confirmed order cannot be modified.");

        _items.Add(item);
    }

    public void Confirm()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("An empty order cannot be confirmed.");

        Status = OrderStatus.Confirmed;
    }
}
```

### Tests à écrire

- une quantité <= 0 est refusée ;
- un prix négatif est refusé ;
- le sous-total est correct ;
- le total de commande est correct ;
- une commande vide ne peut pas être confirmée ;
- une commande confirmée ne peut plus recevoir d'item ;
- changer le prix catalogue d'un produit ne modifie pas un `OrderItem` déjà créé.

---

# Étape 2 — Catalogue produit et repository de commandes en mémoire

## Catalogue produit

Puisque l'API reçoit un `ProductId`, il faut quelque part résoudre ce produit et obtenir son prix actuel.

Créer un contrat simple :

```csharp
public interface IProductCatalog
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}
```

Une première implémentation peut être :

```text
InMemoryProductCatalog
```

Lors de l'ajout d'un item :

```text
ProductId demandé
      ↓
IProductCatalog
      ↓
Product actuel
      ↓
copie ProductId + Name + Price dans OrderItem
```

Le domaine de commande conserve ainsi l'historique du prix.

## Repository de commandes

Créer :

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

Puis :

```text
InMemoryOrderRepository
```

Pour une recherche fréquente par `Guid`, une structure naturelle est :

```csharp
Dictionary<Guid, Order>
```

ou, si elle doit être utilisée en singleton par plusieurs requêtes concurrentes, une structure ou une stratégie d'accès adaptée à la concurrence.

---

# Étape 3 — Comprendre le lifetime du repository mémoire

C'est un exercice volontairement important.

Supposons que `InMemoryOrderRepository` possède sa propre collection :

```csharp
private readonly Dictionary<Guid, Order> _orders = [];
```

Si tu écris :

```csharp
builder.Services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
```

ASP.NET Core crée normalement une nouvelle instance scoped par requête HTTP.

Conséquence :

```text
POST /orders
→ repository A contient la commande

GET /orders/{id}
→ nouvelle requête
→ repository B vide
→ 404
```

Ce n'est pas ce que l'on veut pour notre faux stockage.

### Solution pédagogique simple

Pendant la phase mémoire :

```csharp
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
```

Mais cela introduit une nouvelle contrainte : un singleton peut être utilisé par plusieurs requêtes en même temps. Son état mutable doit être thread-safe.

Par exemple, on peut utiliser :

```csharp
ConcurrentDictionary<Guid, Order>
```

ou protéger correctement les accès.

### Variante plus avancée

Séparer :

```text
Scoped InMemoryOrderRepository
          ↓
Singleton InMemoryOrderStore
```

Cette variante montre que le lifetime du **service** et celui du **stockage** ne sont pas nécessairement identiques.

### Exercice

Créer un `InstanceId` avec un GUID et observer la différence entre :

```text
Transient
Scoped
Singleton
```

sur deux résolutions dans une même requête, puis sur deux requêtes différentes.

---

# Étape 4 — `OrderService`

Créer un service applicatif qui orchestre les opérations de commande.

```csharp
public class OrderService
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

    // ...
}
```

Le service ne doit pas faire :

```csharp
new InMemoryOrderRepository()
new InMemoryProductCatalog()
```

Il exprime ses dépendances dans le constructeur.

### Création d'un item

Le service peut orchestrer :

```text
ProductId
  ↓
chercher le produit
  ↓
prendre le prix actuel
  ↓
créer OrderItem snapshot
  ↓
order.AddItem(...)
```

### Opérations possibles

```text
CreateOrderAsync
AddItemAsync
GetByIdAsync
GetAllAsync
ConfirmAsync
```

### Checkpoint

Tu dois pouvoir remplacer le repository et le catalogue fournis au constructeur sans modifier le cœur de `OrderService`.

---

# Étape 5 — Ajouter une notification

Créer :

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

Lors de la confirmation d'une commande, le service demande au notifier d'envoyer la notification.

### But pédagogique

Vérifier que `OrderService` dépend de ce que le notifier **sait faire**, pas de sa technologie concrète.

Plus tard, un adapter pourrait connecter une API mail réelle sans modifier le cœur de la logique.

---

# Étape 6 — Enregistrer les dépendances

Pendant la phase mémoire, un choix possible est :

```csharp
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddSingleton<IProductCatalog, InMemoryProductCatalog>();
builder.Services.AddScoped<IOrderNotifier, ConsoleOrderNotifier>();
builder.Services.AddScoped<OrderService>();
```

Ce choix n'est pas universel : il correspond à notre besoin de conserver un faux stockage entre les requêtes.

### Questions

1. Pourquoi le repository mémoire perdrait-il ses données s'il possédait sa collection interne et était `Scoped` ?
2. Pourquoi un singleton mutable doit-il être thread-safe ?
3. Pourquoi un vrai `DbContext` EF Core sera-t-il au contraire généralement scoped plus tard ?
4. Pourquoi le lifetime dépend-il de la responsabilité de l'objet ?

---

# Étape 7 — Exposer une API HTTP

Créer au minimum :

```text
GET  /products
GET  /products/{id}
POST /orders
POST /orders/{id}/items
GET  /orders/{id}
GET  /orders
POST /orders/{id}/confirm
```

Le catalogue peut être préchargé au départ ; l'objectif n'est pas encore de faire un back-office produit complet.

Créer des DTOs dédiés :

```csharp
public record CreateOrderRequest(Guid CustomerId);

public record AddOrderItemRequest(
    Guid ProductId,
    int Quantity);

public record OrderResponse(...);
```

Ne retourne pas automatiquement les entités internes directement au client.

### Comportements attendus

```text
POST /orders               → 201 si création réussie
GET /orders/{id}           → 200 si trouvé
GET /orders/{id}           → 404 si absent
POST /orders/{id}/items    → 404 si produit/commande absent selon le contrat choisi
POST /orders/{id}/confirm  → erreur client cohérente si impossible
```

### Checkpoint

Pour chaque endpoint, explique :

- ce qui appartient au Controller ;
- ce qui appartient au service ;
- ce qui appartient à l'entité métier ;
- ce qui appartient au catalogue ou repository.

---

# Étape 8 — Ajouter validation et gestion des erreurs

Cas à gérer :

- quantité invalide ;
- produit inexistant ;
- commande inexistante ;
- confirmation impossible ;
- modification d'une commande confirmée ;
- erreur inattendue.

Évite de copier un `try/catch` générique dans chaque endpoint.

Réfléchis à une gestion centralisée des erreurs inattendues et à une traduction cohérente des erreurs métier vers HTTP.

### Exercice

Construis un tableau :

| Situation | Type d'erreur | Code HTTP attendu |
|---|---|---|
| commande absente | ressource absente | ? |
| produit absent | ressource absente / référence invalide | ? |
| quantité = 0 | entrée invalide | ? |
| commande déjà confirmée | conflit métier | ? |
| erreur DB inattendue | serveur | ? |

Il n'existe pas toujours une seule réponse universelle ; l'important est d'avoir un contrat cohérent et justifiable.

---

# Étape 9 — Ajouter EF Core

Installer les packages nécessaires au provider choisi et les outils de migration.

Créer :

```text
AppDbContext
```

et mapper :

```text
Products
Orders
OrderItems
```

Créer une implémentation :

```text
EfOrderRepository
```

qui respecte toujours :

```text
IOrderRepository
```

Le catalogue produit peut également devenir une implémentation EF Core de `IProductCatalog`.

Changer alors les enregistrements DI :

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
```

### Pourquoi le lifetime change ?

Le repository mémoire était singleton afin de jouer le rôle de stockage persistant dans le processus.

Avec EF Core, les données vivent dans la base. Les repositories eux-mêmes n'ont plus besoin de conserver l'état global en mémoire et utilisent un `DbContext` scoped.

### Test architectural concret

Si `OrderService` doit être massivement réécrit lors du passage mémoire → EF Core, demande-toi si des détails de persistance ont fui dans la logique applicative.

---

# Étape 10 — Migrations

Créer une première migration :

```bash
dotnet ef migrations add InitialCreate
```

Puis appliquer le schéma :

```bash
dotnet ef database update
```

Vérifier les tables obtenues :

```text
Products
Orders
OrderItems
```

Pour chaque relation, identifie :

- la primary key ;
- la foreign key ;
- la cardinalité.

### Exercice

Avant d'exécuter une migration, lis le fichier généré et explique les opérations principales qu'elle va appliquer.

---

# Étape 11 — LINQ avec EF Core

Ajouter des lectures permettant d'obtenir :

- les commandes confirmées ;
- les commandes d'un client ;
- les cinq commandes les plus chères ;
- un résumé `Id + Total + Status`.

### Attention au `Total`

Dans notre domaine, `Order.Total` est une propriété calculée en C# à partir des items.

Ne suppose pas que :

```csharp
.Where(x => x.Total >= 100m)
```

sera automatiquement traduisible si `Total` est uniquement une propriété calculée arbitraire.

Pour une requête EF, exprimer le calcul dans l'expression :

```csharp
var summariesQuery = dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .Select(x => new OrderSummary(
        x.Id,
        x.Items.Sum(i => i.UnitPrice * i.Quantity),
        x.Status))
    .OrderByDescending(x => x.Total)
    .Take(5);
```

Le code ci-dessus contient volontairement un piège : après le `Select`, le type est `OrderSummary`. Il faut donc que `OrderSummary` possède bien un membre `Total`, ou trier avant/projeter différemment.

Une version explicite peut être :

```csharp
var summaries = await dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .Select(x => new OrderSummary(
        x.Id,
        x.Items.Sum(i => i.UnitPrice * i.Quantity),
        x.Status))
    .OrderByDescending(x => x.Total)
    .Take(5)
    .ToListAsync(cancellationToken);
```

avec :

```csharp
public record OrderSummary(
    Guid Id,
    decimal Total,
    OrderStatus Status);
```

### Observer le SQL

Avant la matérialisation :

```csharp
Console.WriteLine(summariesQuery.ToQueryString());
```

Construis d'abord une variable `IQueryable<OrderSummary>`, affiche son SQL, puis appelle `ToListAsync`.

### Questions

- quelles opérations sont traduites en SQL ?
- à quel moment la requête est-elle exécutée ?
- pourquoi utiliser `AsNoTracking()` ici ?
- pourquoi projeter seulement les champs nécessaires ?
- qu'est-ce qui se passe si `ToListAsync()` est appelé avant `Where` ?

---

# Étape 12 — Chargement des relations et N+1

Créer deux scénarios :

### Scénario A — commande complète

Charger une commande et ses items :

```csharp
var order = await dbContext.Orders
    .Include(x => x.Items)
    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
```

### Scénario B — liste de résumés

Ne pas charger les entités complètes. Projeter directement :

```csharp
.Select(x => new OrderSummary(...))
```

### Exercice

Explique pourquoi une boucle qui déclenche une nouvelle requête pour les items de chaque commande peut produire un problème N+1.

Compare le nombre de requêtes réellement générées.

---

# Étape 13 — Tests unitaires du service

Créer un `FakeOrderRepository`, un `FakeProductCatalog` et éventuellement un `FakeOrderNotifier`.

Tester au minimum :

```text
CreateOrderAsync
AddItemAsync produit absent
AddItemAsync capture le prix courant
ConfirmAsync commande vide
ConfirmAsync commande valide
GetByIdAsync absent
GetByIdAsync présent
```

### Test historique important

1. ajouter un produit à 10 € ;
2. ajouter ce produit à une commande ;
3. modifier le prix catalogue à 15 € ;
4. vérifier que l'item existant reste à 10 €.

Ce test documente une vraie décision métier.

---

# Étape 14 — Tests d'intégration

Tester la frontière HTTP réelle.

Scénarios :

```text
POST /orders valide → 201
GET /orders/{id} inconnu → 404
requête invalide → 400
commande créée puis relue → données cohérentes
ajout d'un produit inconnu → erreur cohérente
```

Le but est de vérifier que plusieurs composants collaborent :

```text
HTTP
 ↓
ASP.NET Core
 ↓
DI
 ↓
Controller
 ↓
Service
 ↓
Persistence de test
```

Pour cette partie, le chapitre tests introduira une vraie infrastructure d'intégration (`WebApplicationFactory`) et expliquera pourquoi le provider EF `InMemory` n'est pas une simulation fidèle d'une base relationnelle.

---

# Étape 15 — Refactoring architectural

Seulement maintenant, observer le code et poser les questions :

- le projet est-il devenu difficile à naviguer ?
- le domaine est-il noyé dans ASP.NET Core ?
- EF Core est-il utilisé partout ?
- les DTOs HTTP contaminent-ils le métier ?
- certaines responsabilités ont-elles plusieurs raisons de changer ?

Si oui, envisager une séparation plus nette :

```text
OrderApi.Api
OrderApi.Application
OrderApi.Domain
OrderApi.Infrastructure
OrderApi.Tests
```

Mais chaque extraction doit répondre à un problème identifié.

---

# Étape 16 — Challenge design patterns

## Strategy

Ajouter plusieurs modes de calcul de frais de livraison :

```text
Standard
Express
International
```

Éviter une grosse cascade de `if/else` dans `OrderService`.

## Adapter

Simuler un client tiers :

```text
ThirdPartyEmailClient
```

et créer un adapter qui respecte :

```text
IOrderNotifier
```

## Decorator

Ajouter du logging autour du notifier sans modifier directement son implémentation principale.

### Objectif

Le pattern doit être introduit parce que le problème existe, pas pour cocher une case de formation.

---

# Critères de fin de parcours

Le projet est considéré comme réussi si tu sais expliquer les choix, pas seulement si le code compile.

Tu dois pouvoir répondre à ces questions :

1. Pourquoi `Order` protège-t-il lui-même certaines règles ?
2. Pourquoi `OrderItem` capture-t-il `UnitPrice` au lieu de relire `Product.Price` ?
3. Pourquoi `OrderService` ne construit-il pas directement son repository ?
4. Pourquoi le repository mémoire était-il singleton alors que le repository EF peut être scoped ?
5. Pourquoi un singleton mutable doit-il être thread-safe ?
6. Pourquoi `IOrderRepository` aide-t-il lors du passage mémoire → EF Core ?
7. Quelle différence entre `IEnumerable<T>` et `IQueryable<T>` est importante ici ?
8. À quel moment une requête EF Core est-elle réellement exécutée ?
9. Pourquoi une propriété C# calculée n'est-elle pas forcément traduisible en SQL ?
10. Pourquoi utiliser `async` pour les accès DB et HTTP ?
11. Pourquoi ne faut-il pas lancer plusieurs opérations concurrentes sur le même `DbContext` ?
12. Pourquoi certains tests utilisent-ils des fakes alors que d'autres lancent l'API complète ?
13. Pourquoi le Controller ne doit-il pas contenir toute la logique métier ?
14. Quand une nouvelle couche architecturale vaut-elle son coût ?
15. Quel problème concret justifie chaque design pattern utilisé ?

---

# Pour aller plus loin

Une fois ce projet maîtrisé, les sujets suivants peuvent être abordés séparément :

```text
Authentication / Authorization
Docker
Caching / Redis
Messaging
Observability
CQRS
Microservices
Cloud / Kubernetes
```

Ils ne sont pas nécessaires pour valider les fondamentaux de ce workbook.
