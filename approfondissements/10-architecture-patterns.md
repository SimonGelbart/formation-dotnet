# 10 — Architecture et design patterns : approfondissement

> **Prérequis conseillé :** [workbook v2 — 10 Conception](../workbook_v2/10-conception.md).
>
> **Niveau :** À approfondir pour responsabilités/couplage · Nuance pour Repository · Référence pour Factory, Adapter et Decorator.

La v2 montre quand une conception commence à mériter une abstraction. Ce chapitre sert à raisonner sur les **coûts et bénéfices** de ces choix.

## 1. Une architecture contrôle surtout les dépendances

Une architecture n'est pas seulement :

```text
Controllers/
Services/
Repositories/
```

Elle répond surtout à :

- qui connaît qui ?
- quelle responsabilité vit où ?
- quelles dépendances sont autorisées ?
- quelles parties peuvent évoluer indépendamment ?

Une application simple peut rester :

```text
Controller
   ↓
Service
   ↓
DbContext / Repository
```

Ajouter une couche doit résoudre un problème identifiable.

## 2. Cohésion et couplage

### Cohésion

Une classe cohésive regroupe des comportements qui appartiennent à la même responsabilité.

```text
Order
→ ajouter un item
→ confirmer
→ calculer le total
```

### Couplage

Deux composants sont couplés lorsque l'un dépend de l'autre.

Le but n'est pas zéro couplage. Une application doit relier ses composants.

On cherche plutôt :

```text
forte cohésion
+
dépendances compréhensibles
```

## 3. Où placer une règle métier ?

Si une règle reste vraie même sans HTTP, elle n'appartient probablement pas au Controller.

Mauvais symptôme :

```csharp
[HttpPost("{id}/confirm")]
public async Task<IActionResult> Confirm(Guid id)
{
    // charger en DB
    // vérifier que la commande n'est pas vide
    // changer le statut
    // envoyer une notification
    // choisir le code HTTP
}
```

Séparation plus claire :

```text
Controller
→ traduit HTTP

Service applicatif
→ orchestre le cas d'usage

Order
→ protège la règle métier
```

## 4. Quand découper en plusieurs projets ?

Commence simple si le projet est simple :

```text
OrderApi
OrderApi.Tests
```

Des projets séparés deviennent intéressants lorsque les frontières apportent quelque chose :

```text
Api
→ HTTP et composition

Application
→ cas d'usage

Domain
→ règles métier

Infrastructure
→ EF, fichiers, clients externes
```

Avant une extraction, complète :

```text
Je déplace ______ vers ______
parce que ______
ce qui réduit la dépendance entre ______ et ______.
```

Si la phrase est artificielle, l'extraction l'est peut-être aussi.

## 5. Une abstraction a aussi un coût

Une interface, une couche ou un pattern ajoute :

```text
fichiers
navigation
indirection
mapping éventuel
configuration DI
surface de maintenance
```

Question à poser :

> quel problème devient réellement plus simple grâce à cette abstraction ?

## 6. Strategy

### Symptôme

Plusieurs algorithmes doivent être interchangeables :

```text
Standard
Express
International
```

### Solution possible

```csharp
public interface IShippingStrategy
{
    decimal Calculate(Order order);
}
```

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

### Coût

Un contrat et plusieurs types à naviguer.

### Ne pas l'utiliser si…

Il n'existe qu'un calcul simple sans variation réelle.

## 7. Factory

### Symptôme

Le choix ou la construction d'un objet devient une responsabilité non triviale.

```csharp
public enum ShippingMode
{
    Standard,
    Express
}
```

```csharp
public IShippingStrategy Create(ShippingMode mode)
{
    return mode switch
    {
        ShippingMode.Standard => new StandardShippingStrategy(),
        ShippingMode.Express => new ExpressShippingStrategy(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
```

### Coût

Une indirection de construction supplémentaire.

### Ne pas l'utiliser si…

Le code construit simplement :

```csharp
new Product(name, price)
```

ou si le conteneur DI sait déjà composer le graphe sans décision métier particulière.

## 8. Adapter

### Symptôme

Un SDK tiers expose un contrat qui fuit dans l'application :

```text
ThirdPartyMailClient.SendMessageAsync
```

alors que le métier attend :

```csharp
public interface IOrderNotifier
{
    Task OrderConfirmedAsync(
        Order order,
        CancellationToken cancellationToken);
}
```

### Solution possible

```text
Application
    ↓
IOrderNotifier
    ↑
ThirdPartyMailAdapter
    ↓
SDK tiers
```

### Coût

Mapping et type supplémentaire.

### Ne pas l'utiliser si…

Le SDK n'est utilisé qu'à un endroit trivial et son contrat ne contamine aucune autre partie intéressante de l'application.

## 9. Decorator

### Symptôme

Ajouter un comportement transversal autour d'un collaborateur existant sans modifier son implémentation principale.

Exemple : logging autour d'un notifier.

```csharp
public sealed class LoggingOrderNotifier : IOrderNotifier
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

        await _inner.OrderConfirmedAsync(order, cancellationToken);
    }
}
```

### Coût

Chaîne d'objets plus difficile à suivre si les decorators se multiplient.

### Ne pas l'utiliser si…

Une simple ligne locale suffit et le comportement ne doit pas être réutilisé ou composé.

## 10. Repository

### Symptôme

L'application veut une frontière de persistence adaptée à ses cas d'usage plutôt que dépendre d'EF partout.

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

### Coût

Une couche supplémentaire qui peut finir par simplement recopier `DbSet<T>`.

### Nuance importante

EF Core fournit déjà `DbContext` et `DbSet<T>`. Utiliser directement `DbContext` dans un service applicatif peut être parfaitement raisonnable.

Un repository est plus intéressant lorsqu'il exprime une frontière ou des opérations utiles au métier de l'application, pas lorsqu'il ajoute mécaniquement :

```text
GetAll
GetById
Add
Update
Delete
```

pour chaque table.

## 11. SOLID comme questions de revue

Ne récite pas les acronymes. Pose des questions :

```text
SRP
→ combien de raisons différentes font changer cette classe ?

OCP
→ ajouter un comportement impose-t-il de modifier tous les consommateurs ?

LSP
→ l'implémentation respecte-t-elle réellement les attentes du contrat ?

ISP
→ le consommateur dépend-il d'opérations dont il n'a pas besoin ?

DIP
→ une règle importante dépend-elle inutilement d'un détail technique ?
```

## 12. Pattern ou code simple ?

Pour chaque situation, commence par le problème :

| Situation | Première piste |
|---|---|
| plusieurs algorithmes interchangeables | Strategy |
| contrat externe incompatible | Adapter |
| comportement autour d'un collaborateur | Decorator |
| construction runtime non triviale | éventuellement Factory |
| simple création de donnée | aucun pattern particulier |
| persistence déjà naturellement exprimée par EF | peut-être aucun Repository supplémentaire |

## 13. Checkpoint

Tu dois pouvoir expliquer :

- pourquoi plus de couches n'est pas automatiquement mieux ;
- différence entre cohésion et couplage ;
- pourquoi une règle métier ne devrait pas dépendre de HTTP ;
- ce qu'un `ProjectReference` change réellement dans le sens des dépendances ;
- pourquoi une interface par classe crée souvent du bruit ;
- quel problème concret Strategy, Factory, Adapter et Decorator résolvent ;
- pourquoi un Repository autour d'EF mérite une justification ;
- quel coût supplémentaire introduit chaque abstraction.

Pour voir ces choix appliqués ensemble, consulte l'[étude de cas Order API](11-projet-fil-rouge.md).