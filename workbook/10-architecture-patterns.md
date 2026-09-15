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

## Couplage

Deux composants sont couplés lorsque l'un dépend de l'autre.

Le but n'est pas d'avoir zéro couplage. Une application doit relier ses composants.

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
    // validation métier
    // changement de statut
    // notification
    // mapping HTTP
}
```

Le Controller mélange HTTP, application, métier et infrastructure.

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

Responsabilités possibles :

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

Le sens des références compte. Une séparation en quatre projets ne sert à rien si tout référence tout.

---

# 5. Ne pas multiplier les abstractions gratuitement

Mauvais raisonnement :

> une bonne application .NET possède une interface pour chaque classe et un repository pour chaque table.

Meilleure question :

> quel problème concret cette abstraction résout-elle ?

Une abstraction ajoute aussi des fichiers, du mapping, de la navigation et de la maintenance.

---

# 6. Refactoring guidé du projet fil rouge

Avant de créer plusieurs projets, observe d'abord les symptômes :

- DTOs HTTP utilisés dans le domaine ;
- EF Core référencé partout ;
- Controllers contenant du métier ;
- composition DI difficile à lire ;
- tests connaissant trop de détails techniques.

Puis déplace une responsabilité à la fois.

### Exercice

Complète :

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

**Strategy** encapsule des algorithmes interchangeables derrière un même contrat.

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

Une Factory vaut son coût lorsqu'elle encapsule une vraie décision ou une construction non triviale.

Elle ne doit pas devenir une manière détournée de reconstruire à la main tout le graphe que le conteneur DI sait déjà composer.

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

**Adapter** protège l'application du contrat particulier d'un composant externe.

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

Le Decorator implémente le même contrat et enveloppe un autre composant.

---

# 12. Repository

## Problème

L'application veut retrouver et sauvegarder des commandes sans dépendre partout d'EF Core.

Pour ce workbook, le contrat reste volontairement simple :

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    void Add(Order order);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
```

`Add` reste synchrone parce qu'il ajoute l'objet au stockage local ou au change tracker. `SaveChangesAsync` représente la vraie sauvegarde I/O.

La frontière est explicite :

```text
charger
→ modifier
→ sauvegarder
```

Ce choix est pédagogique. Une autre architecture pourrait laisser l'application utiliser directement `DbContext` ou séparer cette responsabilité différemment.

### Nuance importante

EF Core possède déjà `DbContext` et `DbSet<T>`.

Un repository supplémentaire n'est donc **pas automatiquement nécessaire**.

Évite le repository générique créé mécaniquement pour chaque table :

```text
GetAll
GetById
Add
Update
Delete
```

Une frontière repository est plus intéressante lorsqu'elle correspond à un besoin réel de l'application.

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
- pourquoi un Repository autour d'EF Core mérite une justification ;
- pourquoi `Add` peut rester synchrone alors que `SaveChangesAsync` est asynchrone.

Le chapitre suivant reprend **l'Order API entière** et applique ces décisions au fil de l'évolution du projet.
