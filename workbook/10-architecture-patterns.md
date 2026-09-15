# 10 — Architecture et design patterns

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- raisonner en termes de responsabilités, cohésion et couplage ;
- comprendre qu'une architecture n'est pas une arborescence de dossiers ;
- choisir une séparation en couches seulement lorsqu'elle apporte de la valeur ;
- comprendre le sens des dépendances entre projets ;
- placer les règles métier au bon endroit ;
- reconnaître Strategy, Factory, Adapter, Decorator et Repository ;
- introduire un pattern à partir d'un problème concret, pas pour « cocher une case ».

---

# 1. Une architecture contrôle les dépendances

Une architecture ne se résume pas à :

```text
Controllers/
Services/
Repositories/
```

Elle décrit surtout :

- qui connaît qui ;
- quelles responsabilités vivent où ;
- quelles dépendances sont autorisées ;
- quelles parties peuvent évoluer indépendamment.

Une architecture très simple peut suffire :

```text
Controller
   ↓
Service
   ↓
Repository
   ↓
Database
```

Si l'application reste petite et compréhensible, ajouter des couches peut coûter plus qu'elles ne rapportent.

---

# 2. Cohésion et couplage

## Cohésion

Une classe cohésive regroupe des éléments qui appartiennent naturellement à la même responsabilité.

```text
Order
→ ajouter un item
→ confirmer
→ calculer le total
```

Ces comportements décrivent le même concept métier.

## Couplage

Deux composants sont couplés lorsque l'un dépend de l'autre.

Le but n'est pas d'avoir **zéro couplage**. Une application doit relier des composants.

On cherche plutôt :

```text
forte cohésion
+
couplage maîtrisé
```

---

# 3. Où placer la logique métier ?

Version problématique :

```csharp
[HttpPost("{id}/confirm")]
public async Task<IActionResult> Confirm(
    Guid id,
    CancellationToken cancellationToken)
{
    // lecture DB
    // 30 lignes de validation métier
    // changement de statut
    // notification
    // mapping HTTP
}
```

Le Controller mélange :

```text
HTTP
application
métier
infrastructure
```

Une séparation plus claire :

```csharp
await orderService.ConfirmAsync(
    id,
    cancellationToken);
```

et dans le domaine :

```csharp
order.Confirm();
```

### Règle de réflexion

Demande-toi :

> cette règle a-t-elle du sens même si demain l'application n'est plus appelée via HTTP ?

Si oui, elle n'appartient probablement pas au Controller.

---

# 4. Quand découper en plusieurs projets ?

Tu peux commencer avec :

```text
OrderApi
OrderApi.Tests
```

Puis, si le projet grossit, envisager :

```text
OrderApi.Api
OrderApi.Application
OrderApi.Domain
OrderApi.Infrastructure
OrderApi.Tests
```

### Exemple de responsabilités

```text
Api
→ ASP.NET Core, HTTP, DTOs

Application
→ orchestration des use cases

Domain
→ concepts et règles métier

Infrastructure
→ EF Core, fichiers, clients externes
```

### Le sens des références compte

Une séparation en quatre projets ne sert à rien si tout référence tout.

Un graphe possible :

```text
Api ─────────────→ Application
 │                     ↓
 │                   Domain
 │
 └──────────────→ Infrastructure
                       ↓
                     Domain
```

L'infrastructure peut implémenter des contrats définis plus haut dans l'application ou le domaine selon le découpage choisi.

Il n'existe pas une seule architecture obligatoire ; l'important est de pouvoir **expliquer le sens des dépendances**.

---

# 5. Ne pas multiplier les abstractions gratuitement

Mauvais raisonnement :

> une bonne application .NET possède une interface pour chaque classe et un repository pour chaque table.

Meilleure question :

> quel problème concret cette abstraction résout-elle ?

Une abstraction ajoute aussi :

- des fichiers ;
- du mapping ;
- de la navigation ;
- du vocabulaire ;
- de la maintenance.

Si elle ne protège aucune frontière utile, elle peut être prématurée.

---

# 6. Refactoring guidé du projet fil rouge

Avant de créer plusieurs projets, observe d'abord les symptômes :

- les DTOs HTTP sont utilisés dans le domaine ;
- EF Core est référencé dans toutes les classes ;
- les Controllers contiennent du métier ;
- la composition DI devient difficile à lire ;
- les tests doivent connaître trop de détails techniques.

Puis déplace une responsabilité à la fois.

### Exercice

Pour chaque extraction proposée, complète :

```text
Je déplace __________ vers __________
parce que __________
ce qui réduit la dépendance entre __________ et __________.
```

Si tu ne peux pas compléter cette phrase clairement, l'extraction n'est peut-être pas justifiée.

---

# 7. Design patterns : partir du problème

Un pattern est un **nom donné à une solution récurrente**.

Mauvaise démarche :

```text
j'ai appris Factory
→ je cherche où mettre une Factory
```

Meilleure démarche :

```text
j'ai un problème récurrent de création
→ une Factory ressemble peut-être à une bonne solution
```

---

# 8. Strategy

## Problème

Plusieurs algorithmes de frais de livraison existent :

```text
Standard
Express
International
```

Une cascade de `if` grossit dans le service.

Contrat :

```csharp
public interface IShippingStrategy
{
    decimal Calculate(Order order);
}
```

Implémentations :

```csharp
public sealed class StandardShippingStrategy
    : IShippingStrategy
{
    public decimal Calculate(Order order) => 5m;
}
```

```csharp
public sealed class ExpressShippingStrategy
    : IShippingStrategy
{
    public decimal Calculate(Order order) => 15m;
}
```

Consommateur :

```csharp
public sealed class ShippingCalculator
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

### Problème résolu

> encapsuler des algorithmes interchangeables derrière un même contrat.

---

# 9. Factory

## Problème

La stratégie doit être choisie à partir d'une information runtime :

```csharp
public enum ShippingMode
{
    Standard,
    Express
}
```

```csharp
public sealed class ShippingStrategyFactory
{
    public IShippingStrategy Create(ShippingMode mode)
    {
        return mode switch
        {
            ShippingMode.Standard
                => new StandardShippingStrategy(),

            ShippingMode.Express
                => new ExpressShippingStrategy(),

            _ => throw new ArgumentOutOfRangeException(
                nameof(mode))
        };
    }
}
```

### Nuance

Ceci n'a pas besoin d'une Factory :

```csharp
var product = new Product(...);
```

Une Factory vaut son coût lorsqu'elle encapsule une vraie décision ou construction complexe.

Et elle ne doit pas devenir une manière détournée de reconstruire à la main tout le graphe que le conteneur DI sait déjà composer.

---

# 10. Adapter

## Problème

Une bibliothèque externe expose :

```csharp
ThirdPartyMailClient.SendMessageAsync(...)
```

mais notre application veut :

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
public sealed class ThirdPartyMailAdapter
    : IOrderNotifier
{
    private readonly ThirdPartyMailClient _client;

    public ThirdPartyMailAdapter(
        ThirdPartyMailClient client)
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
SDK tiers
```

### Problème résolu

> protéger notre application du contrat particulier d'un composant externe.

---

# 11. Decorator

## Problème

Ajouter du logging sans modifier le notifier principal.

```csharp
public sealed class LoggingOrderNotifier
    : IOrderNotifier
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

Le Decorator implémente le **même contrat** et enveloppe un autre composant.

```text
OrderService
    ↓
IOrderNotifier
    ↓
LoggingOrderNotifier
    ↓
ThirdPartyMailAdapter
```

### Problème résolu

> ajouter un comportement transversal autour d'un composant sans changer son contrat principal.

---

# 12. Repository

## Problème

Le domaine ou l'application a besoin de retrouver et sauvegarder des objets sans dépendre partout d'EF Core.

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken);
}
```

### Nuance importante

EF Core possède déjà `DbContext` et `DbSet<T>`, qui offrent des abstractions puissantes.

Donc un repository supplémentaire n'est **pas automatiquement nécessaire**.

Repository peu utile s'il ne fait que recopier EF :

```text
GetAll
GetById
Add
Update
Delete
```

Un repository peut être plus pertinent lorsqu'il exprime des besoins du modèle :

```csharp
Task<Order?> GetForConfirmationAsync(...);
Task<IReadOnlyCollection<OrderSummary>>
    GetRecentForCustomerAsync(...);
```

La question n'est pas :

> « est-ce que Repository est un bon pattern ? »

mais :

> « cette frontière apporte-t-elle quelque chose ici ? »

---

# 13. Pattern ou simple code ?

Pour chaque cas, choisis uniquement si nécessaire :

1. trois algorithmes de remise interchangeables ;
2. SDK externe incompatible avec notre contrat ;
3. logging autour de plusieurs implémentations ;
4. création d'un simple `Product(Name, Price)` ;
5. stratégie choisie selon un mode runtime.

<details>
<summary>Correction possible</summary>

1. Strategy.
2. Adapter.
3. Decorator.
4. Aucun pattern particulier.
5. Une Factory peut être pertinente si cette décision constitue une responsabilité réelle.
</details>

---

# 14. Checkpoint architectural

Tu dois pouvoir expliquer :

- pourquoi davantage de couches n'est pas automatiquement meilleur ;
- différence entre cohésion et couplage ;
- pourquoi les règles métier ne devraient pas vivre dans le Controller ;
- ce qu'une séparation physique en projets change réellement ;
- pourquoi une interface pour chaque classe est souvent du bruit ;
- quel problème concret Strategy, Factory, Adapter et Decorator résolvent ;
- pourquoi un Repository autour d'EF Core mérite une justification.

Le chapitre suivant reprend **l'Order API entière** et applique ces décisions au fil de l'évolution du projet.
