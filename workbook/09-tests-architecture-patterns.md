# 9 — Tests, architecture et design patterns

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer test unitaire et test d'intégration ;
- utiliser Arrange / Act / Assert ;
- tester un service avec un fake simple ;
- comprendre quand un mock est utile ;
- relier injection de dépendances et testabilité ;
- raisonner en termes de responsabilités et de dépendances ;
- comprendre les limites d'une architecture en couches ;
- reconnaître quelques design patterns dans des problèmes concrets.

---

## 1. Pourquoi tester ?

Un test automatisé sert à vérifier qu'un comportement important reste vrai lorsque le code évolue.

Un bon test doit surtout rendre explicite une règle attendue.

Exemple :

> Une commande vide ne peut pas être confirmée.

Le test devient une forme de documentation exécutable de cette règle.

---

## 2. Arrange / Act / Assert

Structure classique :

```csharp
[Fact]
public void Confirm_EmptyOrder_Throws()
{
    // Arrange
    var order = new Order();

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

---

## 3. Commencer par des fonctions et objets simples

Avant de mocker des systèmes complexes, teste ce qui peut l'être directement.

```csharp
[Fact]
public void AddItem_UpdatesTotal()
{
    var order = new Order();

    order.AddItem(price: 10m, quantity: 2);

    Assert.Equal(20m, order.Total);
}
```

Ce type de test est rapide, simple et très lisible.

---

## 4. Fake vs mock

### Fake

Une petite implémentation réellement utilisable pour le test.

```csharp
public class FakeOrderRepository : IOrderRepository
{
    public List<Order> Orders { get; } = [];

    public Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        Orders.Add(order);
        return Task.CompletedTask;
    }
}
```

Puis :

```csharp
var repository = new FakeOrderRepository();
var service = new OrderService(repository);
```

C'est souvent suffisant.

### Mock

Un mock permet notamment de configurer des comportements et vérifier des interactions sans écrire manuellement une implémentation complète.

Les mocks sont utiles, mais ne doivent pas devenir la définition d'un test unitaire.

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

---

## 7. Que tester dans le projet ?

### Domaine

- une quantité <= 0 est refusée ;
- une commande vide ne peut pas être confirmée ;
- le total est correctement calculé.

### Service

- une commande créée est enregistrée ;
- une commande absente produit le résultat attendu ;
- une notification est demandée au bon moment si cette règle existe.

### API

- `POST /orders` retourne le bon statut ;
- `GET /orders/{id}` retourne `404` si nécessaire ;
- une requête invalide retourne une erreur client cohérente.

---

## 8. Architecture : contrôler responsabilités et dépendances

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

## 9. Exemple en couches

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

---

## 10. Ne pas multiplier les couches gratuitement

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

## 11. Couplage et cohésion

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

## 12. Où placer la logique métier ?

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

## 13. Strategy

### Problème

Plusieurs stratégies de calcul de frais de livraison existent : standard, express, international.

On veut changer d'algorithme sans remplir le service de `if`.

```csharp
public interface IShippingStrategy
{
    decimal Calculate(Order order);
}
```

Implémentations :

```text
StandardShippingStrategy
ExpressShippingStrategy
InternationalShippingStrategy
```

Le pattern **Strategy** encapsule des algorithmes interchangeables derrière un contrat commun.

---

## 14. Factory

### Problème

La création d'un objet dépend de plusieurs paramètres et devient complexe.

Une Factory centralise cette logique de création.

Attention :

```csharp
new Order()
```

n'a pas besoin d'une Factory simplement parce que le pattern existe.

---

## 15. Adapter

### Problème

Une API externe expose :

```csharp
ThirdPartyMailClient.SendMessage(...)
```

mais ton application veut dépendre de :

```csharp
INotificationSender.SendAsync(...)
```

Un Adapter traduit un contrat vers l'autre :

```text
Application
    ↓
INotificationSender
    ↑
ThirdPartyMailAdapter
    ↓
Third-party SDK
```

---

## 16. Decorator

### Problème

Tu veux ajouter du logging ou du cache autour d'un service sans modifier sa logique principale.

```text
LoggingOrderService
       ↓
    OrderService
```

Un Decorator implémente le même contrat et délègue au composant enveloppé en ajoutant un comportement.

---

## 17. Repository

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

---

## Exercice — reconnaître le problème avant le pattern

Pour chaque cas, choisis un pattern uniquement s'il apporte réellement quelque chose :

1. Trois algorithmes de calcul de remise interchangeables.
2. Une API externe dont le contrat ne correspond pas à ton application.
3. Ajouter du logging autour de plusieurs implémentations d'un même service.
4. Créer une classe `Product` avec seulement `Name` et `Price`.

<details>
<summary>Correction possible</summary>

1. Strategy.
2. Adapter.
3. Decorator.
4. Aucun pattern complexe nécessaire ; un constructeur ou une initialisation simple suffit.
</details>

---

## Checkpoint final

Tu dois savoir répondre à ces questions :

- Pourquoi un fake peut être préférable à un mock dans certains tests ?
- Quelle frontière distingue un test unitaire d'un test d'intégration ?
- Pourquoi une architecture avec davantage de couches n'est-elle pas automatiquement meilleure ?
- Quelle différence fais-tu entre responsabilité, cohésion et couplage ?
- Pourquoi faut-il apprendre un design pattern à partir du problème qu'il résout ?
