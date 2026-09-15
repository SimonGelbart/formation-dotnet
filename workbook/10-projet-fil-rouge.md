# 10 — Projet fil rouge : Order API

## Objectif

Ce projet sert à consolider l'ensemble du workbook dans une seule application qui évolue progressivement.

Le but n'est pas de construire une architecture parfaite dès le départ. On commence simple, puis on introduit les abstractions lorsqu'un problème concret apparaît.

À la fin, l'application doit permettre :

```text
Créer un produit
Créer une commande
Ajouter des items
Calculer le total
Confirmer une commande
Stocker les commandes
Lire une commande
Lister les commandes
Exposer ces opérations en HTTP
Persister les données avec EF Core
Tester les règles métier et l'API
```

---

# Étape 0 — Préparer la solution

Créer :

```text
OrderApi.sln
├── OrderApi
└── OrderApi.Tests
```

Commandes possibles :

```bash
dotnet new sln -n OrderApi
dotnet new webapi -n OrderApi
dotnet new xunit -n OrderApi.Tests

dotnet sln add OrderApi/OrderApi.csproj
dotnet sln add OrderApi.Tests/OrderApi.Tests.csproj
```

Ajouter la référence de test nécessaire vers le projet principal.

### Checkpoint

- `dotnet build` fonctionne ;
- `dotnet test` fonctionne ;
- tu sais expliquer le rôle du `.sln` et des `.csproj`.

---

# Étape 1 — Modèle métier simple

Créer au minimum :

```text
Product
Order
OrderItem
```

## Contraintes

Un produit :

- possède un `Id` ;
- possède un nom ;
- possède un prix >= 0.

Un item de commande :

- référence un produit ;
- possède une quantité > 0 ;
- sait calculer son sous-total.

Une commande :

- possède des items ;
- calcule son total à partir des items ;
- ne laisse pas le total être modifié directement ;
- ne peut pas être confirmée si elle est vide.

### Exemple de direction

```csharp
public class OrderItem
{
    public Product Product { get; }
    public int Quantity { get; }

    public decimal Subtotal => Product.Price * Quantity;

    public OrderItem(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Product = product;
        Quantity = quantity;
    }
}
```

Ne copie pas forcément cet exemple tel quel. L'objectif est de réfléchir aux invariants.

### Tests à écrire

- une quantité <= 0 est refusée ;
- le sous-total est correct ;
- le total de commande est correct ;
- une commande vide ne peut pas être confirmée.

---

# Étape 2 — Repository en mémoire

Créer un contrat :

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

Puis une implémentation :

```text
InMemoryOrderRepository
```

Cette implémentation peut utiliser une collection en mémoire.

### Question de conception

Pour une recherche fréquente par `Guid`, quelle collection est la plus naturelle ?

```text
List<Order>
ou
Dictionary<Guid, Order> ?
```

Justifie le choix en termes d'usage et de complexité.

---

# Étape 3 — `OrderService`

Créer un service applicatif qui orchestre les opérations de commande.

```csharp
public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    // ...
}
```

Le service ne doit pas faire :

```csharp
new InMemoryOrderRepository()
```

Il exprime sa dépendance dans le constructeur.

### Opérations possibles

```text
CreateOrderAsync
GetByIdAsync
GetAllAsync
ConfirmAsync
```

### Checkpoint

Tu dois pouvoir remplacer le repository fourni au constructeur sans modifier `OrderService`.

---

# Étape 4 — Ajouter une notification

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

# Étape 5 — Enregistrer les dépendances

Dans `Program.cs` :

```csharp
builder.Services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderNotifier, ConsoleOrderNotifier>();
builder.Services.AddScoped<OrderService>();
```

### Questions

1. Pourquoi `Scoped` est-il un choix possible ici ?
2. Que se passerait-il si le repository mémoire devenait `Transient` ?
3. Que changerait un `Singleton` mutable ?

L'objectif n'est pas de trouver une réponse universelle mais de raisonner sur le cycle de vie.

---

# Étape 6 — Exposer une API HTTP

Créer :

```text
POST /orders
GET  /orders/{id}
GET  /orders
POST /orders/{id}/confirm
```

Créer des DTOs dédiés :

```csharp
public record CreateOrderRequest(...);
public record OrderResponse(...);
```

Ne retourne pas automatiquement les entités internes directement au client.

### Comportements attendus

```text
POST /orders            → 201 si création réussie
GET /orders/{id}        → 200 si trouvé
GET /orders/{id}        → 404 si absent
POST /orders/{id}/confirm → erreur client cohérente si impossible
```

### Checkpoint

Pour chaque endpoint, explique :

- ce qui appartient au Controller ;
- ce qui appartient au service ;
- ce qui appartient à l'entité métier.

---

# Étape 7 — Ajouter validation et gestion des erreurs

Cas à gérer :

- quantité invalide ;
- produit invalide ;
- commande inexistante ;
- confirmation impossible ;
- erreur inattendue.

Évite de copier un `try/catch` générique dans chaque endpoint.

Réfléchis à une gestion centralisée des erreurs inattendues et à une traduction cohérente des erreurs métier vers HTTP.

### Exercice

Construis un tableau :

| Situation | Type d'erreur | Code HTTP attendu |
|---|---|---|
| commande absente | ressource absente | ? |
| quantité = 0 | entrée invalide | ? |
| erreur DB inattendue | serveur | ? |

Justifie chaque code.

---

# Étape 8 — Ajouter EF Core

Créer :

```text
AppDbContext
```

et mapper les entités persistées.

Créer une implémentation :

```text
EfOrderRepository
```

qui respecte toujours :

```text
IOrderRepository
```

Puis changer uniquement l'enregistrement DI :

```csharp
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
```

### Test architectural concret

Si `OrderService` doit être massivement réécrit lors du passage mémoire → EF Core, demande-toi si des détails de persistence ont fui dans la logique applicative.

---

# Étape 9 — Migrations

Créer une première migration et appliquer le schéma.

Vérifier les tables obtenues :

```text
Orders
OrderItems
Products
```

Pour chaque relation, identifie :

- la primary key ;
- la foreign key ;
- la cardinalité.

### Exercice

Avant d'exécuter une migration, lis le fichier généré et explique les opérations principales qu'elle va appliquer.

---

# Étape 10 — Requêtes LINQ avec EF Core

Ajouter un endpoint ou service qui retourne :

- les commandes confirmées ;
- les commandes d'un client ;
- les cinq commandes les plus chères ;
- un résumé `Id + Total + Status`.

Exemple de projection :

```csharp
var summaries = await dbContext.Orders
    .AsNoTracking()
    .Where(x => x.Status == OrderStatus.Confirmed)
    .OrderByDescending(x => x.Total)
    .Select(x => new OrderSummary(
        x.Id,
        x.Total,
        x.Status))
    .Take(5)
    .ToListAsync(cancellationToken);
```

### Questions

- quelles opérations sont traduites en SQL ?
- à quel moment la requête est-elle exécutée ?
- pourquoi utiliser `AsNoTracking()` ici ?
- pourquoi projeter seulement les champs nécessaires ?

---

# Étape 11 — Tests unitaires du service

Créer un `FakeOrderRepository` et éventuellement un `FakeOrderNotifier`.

Tester au minimum :

```text
CreateOrderAsync
ConfirmAsync
GetByIdAsync absent
GetByIdAsync présent
```

### Exemple de principe

```csharp
var repository = new FakeOrderRepository();
var notifier = new FakeOrderNotifier();
var service = new OrderService(repository, notifier);
```

Le test ne doit pas nécessiter une vraie base SQL pour vérifier les règles applicatives.

---

# Étape 12 — Tests d'intégration

Tester la frontière HTTP réelle.

Scénarios :

```text
POST /orders valide → 201
GET /orders/{id} inconnu → 404
requête invalide → 400
commande créée puis relue → données cohérentes
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

---

# Étape 13 — Refactoring architectural

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

# Étape 14 — Challenge design patterns

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
2. Pourquoi `OrderService` ne construit-il pas directement son repository ?
3. Pourquoi `IOrderRepository` aide-t-il lors du passage mémoire → EF Core ?
4. Quelle différence entre `IEnumerable<T>` et `IQueryable<T>` est importante ici ?
5. À quel moment une requête EF Core est-elle réellement exécutée ?
6. Pourquoi utiliser `async` pour les accès DB et HTTP ?
7. Pourquoi certains tests utilisent-ils des fakes alors que d'autres lancent l'API complète ?
8. Pourquoi le Controller ne doit-il pas contenir toute la logique métier ?
9. Quand une nouvelle couche architecturale vaut-elle son coût ?
10. Quel problème concret justifie chaque design pattern utilisé ?

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
