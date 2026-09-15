# 9 — Tests, architecture et design patterns

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer test unitaire et test d'intégration ;
- utiliser Arrange / Act / Assert ;
- tester un service avec un fake simple ;
- comprendre quand un mock est utile ;
- relier injection de dépendances et testabilité ;
- lancer l'API dans un test d'intégration avec `WebApplicationFactory` ;
- comprendre pourquoi le provider EF Core `InMemory` n'est pas une base relationnelle de test fidèle ;
- raisonner en termes de responsabilités et de dépendances ;
- comprendre les limites d'une architecture en couches ;
- reconnaître et implémenter quelques design patterns dans des problèmes concrets.

---

## 1. Pourquoi tester ?

Un test automatisé sert à vérifier qu'un comportement important reste vrai lorsque le code évolue.

Un bon test doit surtout rendre explicite une règle attendue.

Exemple :

> Une commande vide ne peut pas être confirmée.

Le test devient une forme de documentation exécutable de cette règle.

### Ce qu'un test n'est pas

Un test n'a pas besoin de reproduire la structure exacte du code. Il doit décrire un **comportement observable**.

Un test trop couplé aux détails internes peut casser lors d'un simple refactoring alors que le comportement reste correct.

---

## 2. Arrange / Act / Assert

Structure classique :

```csharp
[Fact]
public void Confirm_EmptyOrder_Throws()
{
    // Arrange
    var order = new Order(Guid.NewGuid());

    // Act
    var action = () => order.Confirm();

    // Assert
    Assert.Throws<InvalidOperationException>(action);
}
```

### Arrange

Préparer le scénario.

### Act

Exécuter l'action testée.

### Assert

Vérifier le comportement obtenu.

La structure n'a pas besoin d'être commentée dans chaque test si le code est déjà lisible. C'est avant tout un modèle mental.

---

## 3. Commencer par des objets simples

Avant de mocker des systèmes complexes, teste ce qui peut l'être directement.

```csharp
[Fact]
public void AddItem_UpdatesTotal()
{
    var order = new Order(Guid.NewGuid());
    var item = new OrderItem(
        Guid.NewGuid(),
        "Keyboard",
        10m,
        2);

    order.AddItem(item);

    Assert.Equal(20m, order.Total);
}
```

Ce type de test est rapide, simple et très lisible.

### Autre test métier utile

```csharp
[Fact]
public void OrderItem_KeepsCapturedUnitPrice()
{
    var item = new OrderItem(
        Guid.NewGuid(),
        "Keyboard",
        10m,
        2);

    Assert.Equal(10m, item.UnitPrice);
    Assert.Equal(20m, item.Subtotal);
}
```

Le test documente le fait que la commande conserve son prix historique.

---

## 4. Fake vs mock

### Fake

Une petite implémentation réellement utilisable pour le test.

```csharp
public class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyCollection<Order>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Order> orders = _orders.Values.ToList();
        return Task.FromResult(orders);
    }
}
```

Puis :

```csharp
var repository = new FakeOrderRepository();
var service = new OrderService(repository, ...);
```

C'est souvent suffisant.

### Mock

Un mock permet notamment de configurer des comportements et vérifier des interactions sans écrire manuellement une implémentation complète.

Il devient utile lorsqu'un scénario doit exprimer précisément :

```text
« cette dépendance doit être appelée une fois »
« si ce client retourne X, le service doit produire Y »
```

### Attention aux tests trop orientés interactions

Un test qui vérifie chaque appel de méthode interne peut devenir très fragile.

Préférer lorsque possible :

> vérifier le résultat métier observable plutôt que reconstruire l'implémentation dans le test.

### Règle pratique

> Commence par le moyen le plus simple permettant d'exprimer le scénario.

---

## 5. DI et testabilité

Sans injection :

```csharp
public class OrderService
{
    private readonly SqlOrderRepository _repository = new();
}
```

Tester `OrderService` impose potentiellement une vraie infrastructure SQL.

Avec injection :

```csharp
public OrderService(IOrderRepository repository)
{
    _repository = repository;
}
```

Le test peut fournir :

```csharp
new FakeOrderRepository()
```

La testabilité n'est pas l'unique raison d'utiliser l'injection de dépendances, mais elle rend immédiatement visible le bénéfice du découplage.

---

## 6. Test unitaire vs test d'intégration

### Test unitaire

On teste une petite unité logique en contrôlant ses frontières.

```text
OrderService
   ↓
Fake repository
```

### Test d'intégration

On vérifie que plusieurs composants réels collaborent correctement.

```text
HTTP
 ↓
ASP.NET Core
 ↓
Controller
 ↓
Service
 ↓
EF Core
 ↓
Database de test
```

La différence n'est pas simplement :

> test rapide vs test lent.

Elle concerne principalement la **frontière réellement testée**.

### Une suite saine contient les deux

Les tests unitaires donnent un feedback précis et rapide sur les règles isolées.

Les tests d'intégration détectent les problèmes qui n'existent qu'entre composants :

- routing ;
- sérialisation JSON ;
- DI ;
- configuration EF ;
- requêtes SQL ;
- codes HTTP.

---

## 7. Premier test d'intégration avec `WebApplicationFactory`

Ajouter au projet de tests le package :

```bash
dotnet package add Microsoft.AspNetCore.Mvc.Testing
```

Puis un test peut démarrer l'application ASP.NET Core en mémoire et obtenir un vrai `HttpClient` :

```csharp
public class OrdersApiTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_UnknownOrder_Returns404()
    {
        var response = await _client.GetAsync(
            $"/orders/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
```

Selon la structure du projet et la visibilité du type généré par les top-level statements, il peut être nécessaire de rendre `Program` accessible au projet de tests.

### Ce que ce test traverse réellement

```text
HttpClient
   ↓
routing
   ↓
model binding
   ↓
DI
   ↓
Controller
   ↓
service
```

Il teste donc bien plus qu'une méthode de Controller appelée directement avec `new`.

---

## 8. Remplacer une dépendance pour les tests d'intégration

`WebApplicationFactory` peut être personnalisée pour remplacer une infrastructure réelle par une infrastructure de test.

Conceptuellement :

```text
Application normale
IOrderRepository → EfOrderRepository

Test d'intégration
IOrderRepository → TestOrderRepository
```

Cela permet par exemple de conserver :

```text
HTTP + routing + DI + Controller + Service
```

tout en contrôlant la persistance selon ce qu'on souhaite réellement tester.

Attention : si le but du test est justement de vérifier EF Core et SQL, remplacer le repository enlèverait la partie que l'on veut valider.

---

## 9. EF Core `InMemory` n'est pas une base relationnelle

EF Core fournit un provider appelé `InMemory`, mais il ne simule pas fidèlement une base SQL relationnelle.

Il peut notamment se comporter différemment sur :

- contraintes relationnelles ;
- transactions ;
- traduction de certaines requêtes ;
- comportement SQL spécifique au provider.

Donc :

> un test qui passe avec EF `InMemory` ne prouve pas qu'une requête fonctionnera avec PostgreSQL, SQL Server ou SQLite.

### Alternatives selon le besoin

Pour un exercice local simple :

```text
SQLite in-memory
```

permet de tester une vraie base relationnelle légère.

Pour la fidélité maximale :

```text
même moteur de base que la production
```

est préférable, au prix d'une infrastructure de test plus lourde.

Le choix dépend de la frontière testée.

---

## 10. Que tester dans le projet ?

### Domaine

- une quantité <= 0 est refusée ;
- une commande vide ne peut pas être confirmée ;
- une commande confirmée ne peut plus être modifiée ;
- le total est correctement calculé ;
- le prix historique d'un item ne change pas avec le catalogue.

### Service

- une commande créée est enregistrée ;
- un produit absent est correctement géré ;
- une notification est demandée au bon moment ;
- le bon prix produit est capturé dans l'item.

### API

- `POST /orders` retourne `201` ;
- `GET /orders/{id}` retourne `404` si nécessaire ;
- une requête invalide retourne `400` ;
- `CreatedAtAction` pointe vers une ressource relisible ;
- une commande créée peut être relue dans un scénario de bout en bout.

---

## 11. Architecture : contrôler responsabilités et dépendances

Une architecture n'est pas une arborescence de dossiers. C'est principalement :

- une répartition des responsabilités ;
- un sens de dépendance entre composants ;
- des frontières explicites.

Architecture simple :

```text
Controller
   ↓
Service
   ↓
Repository
   ↓
Database
```

Elle peut être suffisante pour beaucoup d'applications.

---

## 12. Exemple en couches

Une organisation plus structurée peut ressembler à :

```text
Presentation / API
       ↓
Application
       ↓
Domain

Infrastructure
    ↑ implémente certains contrats
```

### Presentation

Connaît HTTP et ASP.NET Core.

### Application

Orchestre les cas d'usage.

### Domain

Contient les concepts et règles métier centrales lorsque le domaine le justifie.

### Infrastructure

Contient les détails techniques : EF Core, clients externes, fichiers, etc.

### Le sens des références compte

Une séparation en quatre projets n'apporte rien si chaque projet référence tous les autres dans tous les sens.

Le découpage physique doit refléter les dépendances que l'on cherche réellement à contrôler.

---

## 13. Ne pas multiplier les couches gratuitement

Mauvais raisonnement :

> Une bonne application .NET doit avoir exactement quatre projets, un repository pour chaque table et une interface pour chaque classe.

Meilleur raisonnement :

> Quelle séparation apporte réellement de la valeur ici ?

Une couche supplémentaire ajoute aussi :

- des fichiers ;
- des mappings ;
- des abstractions ;
- du coût de navigation ;
- de la maintenance.

### Question à poser en refactoring

> Quelle dépendance ou responsabilité suis-je en train d'améliorer en créant cette abstraction ?

Si la réponse est floue, l'abstraction est peut-être prématurée.

---

## 14. Couplage et cohésion

### Cohésion

Une classe cohésive regroupe des éléments qui appartiennent naturellement à la même responsabilité.

### Couplage

Deux composants sont couplés lorsque l'un dépend des détails de l'autre.

On cherche en général :

```text
forte cohésion
faible couplage inutile
```

Pas zéro couplage : une application doit bien relier ses composants.

---

## 15. Où placer la logique métier ?

Exemple :

```csharp
[HttpPost("{id}/confirm")]
public IActionResult Confirm(Guid id)
{
    // 50 lignes de règles métier ici
}
```

Le Controller devient alors responsable à la fois de HTTP et du métier.

Mieux :

```csharp
await orderService.ConfirmAsync(id, cancellationToken);
```

ou, pour une règle appartenant naturellement à l'entité :

```csharp
order.Confirm();
```

Le bon emplacement dépend de la nature de la règle, mais le Controller ne doit pas devenir le centre de toute la logique applicative.

---

# Design patterns : partir du problème

## 16. Strategy

### Problème

Plusieurs stratégies de calcul de frais de livraison existent : standard, express, international.

On veut changer d'algorithme sans remplir le service de `if`.

```csharp
public interface IShippingStrategy
{
    decimal Calculate(Order order);
}
```

```csharp
public sealed class StandardShippingStrategy : IShippingStrategy
{
    public decimal Calculate(Order order) => 5m;
}
```

```csharp
public sealed class ExpressShippingStrategy : IShippingStrategy
{
    public decimal Calculate(Order order) => 15m;
}
```

Le service travaille avec le contrat :

```csharp
public class ShippingCalculator
{
    private readonly IShippingStrategy _strategy;

    public ShippingCalculator(IShippingStrategy strategy)
    {
        _strategy = strategy;
    }

    public decimal Calculate(Order order)
        => _strategy.Calculate(order);
}
```

Le pattern **Strategy** encapsule des algorithmes interchangeables derrière un contrat commun.

---

## 17. Factory

### Problème

Le choix ou la création d'une stratégie dépend d'informations runtime.

```csharp
public enum ShippingMode
{
    Standard,
    Express
}
```

Une factory simple :

```csharp
public class ShippingStrategyFactory
{
    public IShippingStrategy Create(ShippingMode mode)
    {
        return mode switch
        {
            ShippingMode.Standard => new StandardShippingStrategy(),
            ShippingMode.Express => new ExpressShippingStrategy(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }
}
```

### Nuance

Ceci n'a pas besoin d'une factory :

```csharp
var product = new Product(...);
```

Une Factory est utile quand elle **encapsule réellement une décision ou une construction non triviale**.

Et dans une application utilisant DI, la factory ne doit pas devenir une manière détournée de recréer manuellement tout le graphe d'objets que le conteneur sait déjà construire.

---

## 18. Adapter

### Problème

Une librairie tierce expose :

```csharp
ThirdPartyMailClient.SendMessageAsync(...)
```

mais ton application veut dépendre de :

```csharp
public interface IOrderNotifier
{
    Task OrderConfirmedAsync(
        Order order,
        CancellationToken cancellationToken);
}
```

Adapter :

```csharp
public class ThirdPartyMailAdapter : IOrderNotifier
{
    private readonly ThirdPartyMailClient _client;

    public ThirdPartyMailAdapter(ThirdPartyMailClient client)
    {
        _client = client;
    }

    public Task OrderConfirmedAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        return _client.SendMessageAsync(
            $"Order {order.Id} confirmed",
            cancellationToken);
    }
}
```

```text
Application
    ↓
IOrderNotifier
    ↑
ThirdPartyMailAdapter
    ↓
Third-party SDK
```

L'Adapter protège le reste de l'application du contrat particulier de la librairie externe.

---

## 19. Decorator

### Problème

Tu veux ajouter du logging autour du notifier sans modifier son implémentation principale.

```csharp
public class LoggingOrderNotifier : IOrderNotifier
{
    private readonly IOrderNotifier _inner;
    private readonly ILogger<LoggingOrderNotifier> _logger;

    public LoggingOrderNotifier(
        IOrderNotifier inner,
        ILogger<LoggingOrderNotifier> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task OrderConfirmedAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Sending confirmation for order {OrderId}",
            order.Id);

        await _inner.OrderConfirmedAsync(
            order,
            cancellationToken);
    }
}
```

Le Decorator implémente le **même contrat** et délègue au composant enveloppé en ajoutant un comportement.

```text
OrderService
     ↓
IOrderNotifier
     ↓
LoggingOrderNotifier
     ↓
ThirdPartyMailAdapter
```

---

## 20. Repository

Le Repository fournit une abstraction d'accès à un ensemble d'objets persistés.

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(...);
    Task AddAsync(...);
}
```

### Nuance importante

EF Core fournit déjà `DbContext` et `DbSet<T>`, qui apportent eux-mêmes des abstractions riches sur la persistance.

Un Repository supplémentaire n'est donc pas automatiquement nécessaire. Il est pertinent lorsqu'il apporte une frontière métier ou architecturale utile, pas uniquement pour envelopper chaque méthode EF une par une.

Mauvais repository « miroir EF » :

```text
GetAll
GetById
Add
Update
Delete
```

créé automatiquement pour chaque table sans besoin métier.

Repository plus intentionnel :

```csharp
Task<Order?> GetForConfirmationAsync(...);
Task<IReadOnlyCollection<OrderSummary>> GetRecentForCustomerAsync(...);
```

selon les besoins réels de l'application.

---

## Exercice — reconnaître le problème avant le pattern

Pour chaque cas, choisis un pattern uniquement s'il apporte réellement quelque chose :

1. Trois algorithmes de calcul de remise interchangeables.
2. Une API externe dont le contrat ne correspond pas à ton application.
3. Ajouter du logging autour de plusieurs implémentations d'un même service.
4. Créer une classe `Product` avec seulement `Name` et `Price`.
5. Choisir une stratégie de livraison à partir d'un `ShippingMode` reçu au runtime.

<details>
<summary>Correction possible</summary>

1. Strategy.
2. Adapter.
3. Decorator.
4. Aucun pattern complexe nécessaire ; un constructeur ou une initialisation simple suffit.
5. Une Factory peut être utile si la décision de création devient une responsabilité claire.
</details>

---

## Exercice — écrire un test d'intégration

À partir de l'Order API :

1. ajouter `Microsoft.AspNetCore.Mvc.Testing` ;
2. démarrer l'API avec `WebApplicationFactory<Program>` ;
3. appeler `GET /orders/{id}` avec un GUID inconnu ;
4. vérifier `404` ;
5. créer ensuite une commande puis la relire ;
6. décider explicitement si ce test utilise un faux repository, SQLite in-memory ou une vraie base de test ;
7. expliquer ce que ce choix **teste réellement** et ce qu'il ne teste pas.

---

## Checkpoint final

Tu dois savoir répondre à ces questions :

- Pourquoi un fake peut être préférable à un mock dans certains tests ?
- Quelle frontière distingue un test unitaire d'un test d'intégration ?
- Qu'est-ce que `WebApplicationFactory` permet de tester qu'un appel direct de Controller ne teste pas ?
- Pourquoi EF Core `InMemory` ne valide-t-il pas fidèlement le comportement d'une base SQL ?
- Pourquoi une architecture avec davantage de couches n'est-elle pas automatiquement meilleure ?
- Quelle différence fais-tu entre responsabilité, cohésion et couplage ?
- Pourquoi faut-il apprendre un design pattern à partir du problème qu'il résout ?
- Quelle différence vois-tu maintenant entre Strategy, Factory, Adapter et Decorator ?
