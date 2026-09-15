# 3 — Abstraction, interfaces et dépendances

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer classe concrète, classe abstraite et interface ;
- expliquer pourquoi on dépend souvent d'un contrat plutôt que d'une implémentation ;
- définir ce qu'est une dépendance ;
- expliquer l'injection de dépendances ;
- distinguer injection par constructeur, méthode et propriété ;
- comprendre ce que le conteneur DI intégré à .NET sait réellement faire ;
- comprendre la différence entre DI et conteneur de DI ;
- comprendre les lifetimes `Transient`, `Scoped` et `Singleton` ;
- identifier un problème de durée de vie, notamment un singleton mutable ou une dépendance scoped capturée trop longtemps ;
- utiliser SOLID comme outil de raisonnement et non comme liste à réciter.

---

## 1. Classe concrète, classe abstraite, interface

### Classe concrète

On peut l'instancier directement :

```csharp
var sender = new EmailSender();
```

### Classe abstraite

Elle peut contenir de l'état et du comportement partagé, mais ne peut pas être instanciée directement.

```csharp
public abstract class NotificationSender
{
    public abstract Task SendAsync(string message);
}
```

### Interface

Elle exprime surtout un **contrat** :

```csharp
public interface INotificationSender
{
    Task SendAsync(string message);
}
```

Plusieurs implémentations peuvent respecter ce contrat :

```csharp
public class EmailSender : INotificationSender
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"Email: {message}");
        return Task.CompletedTask;
    }
}
```

```csharp
public class SmsSender : INotificationSender
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"SMS: {message}");
        return Task.CompletedTask;
    }
}
```

### Classe abstraite ou interface ?

Une interface est souvent adaptée lorsqu'on veut surtout exprimer une capacité :

> « cet objet sait envoyer une notification ».

Une classe abstraite devient plus intéressante lorsqu'il existe réellement du comportement ou de l'état partagé entre les implémentations.

Si `EmailSender` et `SmsSender` n'ont rien d'autre en commun que la méthode `SendAsync`, l'interface est probablement le contrat le plus simple.

### Parallèle TypeScript

Une interface TypeScript est souvent utilisée pour décrire la forme d'une donnée. En C#, une interface est très souvent utilisée comme **contrat comportemental** entre composants.

---

## 2. Dépendre d'une abstraction

Version fortement couplée :

```csharp
public class OrderService
{
    private readonly EmailSender _sender = new();
}
```

`OrderService` décide lui-même :

- quel type concret utiliser ;
- comment le construire ;
- quand le créer.

Version découplée :

```csharp
public class OrderService
{
    private readonly INotificationSender _sender;

    public OrderService(INotificationSender sender)
    {
        _sender = sender;
    }
}
```

`OrderService` exprime désormais uniquement son besoin :

> « J'ai besoin de quelque chose capable d'envoyer une notification. »

Il ne décide plus de la manière dont cette notification est envoyée.

### `new` n'est pas le problème

Ne retiens surtout pas :

> « il ne faut jamais utiliser `new` ».

Ceci est parfaitement normal :

```csharp
var order = new Order();
var item = new OrderItem(...);
```

Le problème apparaît plutôt lorsqu'un composant métier ou applicatif **cache la création d'une dépendance externe ou remplaçable** :

```csharp
public class OrderService
{
    private readonly SqlOrderRepository _repository = new();
}
```

La vraie question est :

> cet objet est-il une donnée que je possède, ou un collaborateur dont mon composant dépend ?

---

## 3. Qu'est-ce qu'une dépendance ?

Une dépendance est un objet dont un autre objet a besoin pour réaliser son travail.

Exemples :

```text
OrderService
├── IOrderRepository
└── INotificationSender
```

`OrderService` dépend de ces deux collaborateurs.

### Définition

> Injecter une dépendance consiste à la fournir depuis l'extérieur plutôt qu'à laisser l'objet la créer lui-même.

---

## 4. Les formes d'injection

### Injection par constructeur

```csharp
public OrderService(IOrderRepository repository)
{
    _repository = repository;
}
```

À privilégier pour les dépendances obligatoires.

Avantage : un `OrderService` ne peut pas être construit dans un état incomplet.

### Injection par méthode

Conceptuellement, une dépendance peut être fournie seulement à la méthode qui en a besoin :

```csharp
public Report GenerateReport(IFormatter formatter)
{
    ...
}
```

Dans ASP.NET Core, on rencontre aussi ce principe à la frontière HTTP : une dépendance peut être fournie directement à une action ou à un route handler.

### Injection par propriété

Conceptuellement :

```csharp
public ILogger? Logger { get; set; }
```

Elle rend la dépendance potentiellement absente et le cycle de vie moins évident.

### Important : concept DI vs conteneur Microsoft

Les trois formes ci-dessus existent comme techniques de conception, mais le **conteneur DI intégré à .NET est centré sur l'injection par constructeur** et ne réalise pas automatiquement l'injection de propriétés.

Dans une application ASP.NET Core classique, le réflexe par défaut doit donc être :

> dépendance obligatoire → constructeur.

---

## 5. DI ≠ conteneur de DI

Ce code utilise déjà l'injection de dépendances :

```csharp
var repository = new InMemoryOrderRepository();
var sender = new EmailSender();

var service = new OrderService(repository, sender);
```

Aucun framework n'est nécessaire.

Un conteneur de DI automatise cette **composition** :

```csharp
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<INotificationSender, EmailSender>();
```

Puis ASP.NET Core peut construire les objets pour nous.

La DI est donc le principe ; le conteneur est un outil qui applique ce principe à grande échelle.

---

## 6. Lifetimes

### `Transient`

Une nouvelle instance est créée à chaque résolution.

### `Scoped`

Dans une application web ASP.NET Core, un scope est normalement créé pour chaque requête HTTP. Les services scoped résolus dans cette requête partagent donc la même instance.

### `Singleton`

Une seule instance est conservée pour toute la durée de vie de l'application.

Représentation mentale :

```text
Singleton
──────────────────────── application

Scoped
──── request A ────
                  ──── request B ────

Transient
─ instance
  ─ instance
    ─ instance
```

### Singleton mutable

Un singleton peut être utilisé par plusieurs requêtes en même temps. S'il contient un état mutable partagé, cet état doit être conçu pour l'accès concurrent.

```csharp
public class Counter
{
    public int Value { get; set; }
}
```

En singleton, plusieurs requêtes pourraient lire et modifier `Value` simultanément.

### Dépendance captive

Autre problème classique : faire dépendre un objet très long-lived d'un objet plus court-lived.

```text
Singleton
   ↓
Scoped service
```

Le singleton risque alors de conserver une instance scoped au-delà de la durée pour laquelle elle a été conçue.

C'est notamment une raison pour laquelle il faut raisonner sur les lifetimes, pas seulement apprendre trois définitions.

---

## 7. Exercice d'observation des lifetimes

Créer :

```csharp
public class InstanceId
{
    public Guid Id { get; } = Guid.NewGuid();
}
```

L'injecter dans deux services utilisés pendant une même requête, puis tester successivement :

```csharp
AddTransient<InstanceId>()
AddScoped<InstanceId>()
AddSingleton<InstanceId>()
```

Observe les GUID :

- sont-ils identiques dans une même requête ?
- changent-ils entre deux requêtes ?

Ne lis pas seulement la définition des lifetimes : **observe leur comportement**.

---

## 8. SOLID sans récitation

SOLID sert à mettre des mots sur des problèmes de conception.

### SRP — Single Responsibility Principle

Considère :

```csharp
public class OrderService
{
    public void CreateOrder()
    {
        // validation
        // calcul du total
        // SQL
        // email
        // PDF
        // logs
    }
}
```

Question : **combien de raisons différentes peuvent forcer cette classe à changer ?**

Si elle gère la base, les emails, les PDF et le métier, elle concentre plusieurs responsabilités.

### DIP — Dependency Inversion Principle

Une formulation utile est : les composants contenant les politiques importantes de l'application ne devraient pas être forcés de dépendre directement des détails techniques qu'ils orchestrent.

Couplage direct :

```text
OrderService
    ↓
SqlOrderRepository
```

Une frontière possible :

```text
OrderService
    ↓
IOrderRepository
    ↑
SqlOrderRepository
```

Mais attention : **DIP ne signifie pas « une interface devant chaque classe »**. Une abstraction doit exprimer une frontière utile.

### ISP — Interface Segregation Principle

Préférer plusieurs contrats cohérents à une interface énorme qui force chaque implémentation à supporter des méthodes inutiles.

Mauvais exemple :

```csharp
public interface IRepository
{
    void Add();
    void Delete();
    void ExportToPdf();
    void SendEmail();
    void RebuildSearchIndex();
}
```

Si les implémentations n'ont besoin que d'une petite partie du contrat, l'interface mélange probablement plusieurs responsabilités.

---

## Exercice — code review

Analyse ce code :

```csharp
public class OrderService
{
    public void Create(Order order)
    {
        var repository = new SqlOrderRepository();
        var mail = new EmailSender();

        repository.Save(order);
        mail.Send(order);
    }
}
```

Identifie au moins trois problèmes de conception.

<details>
<summary>Correction possible</summary>

- `OrderService` choisit lui-même ses implémentations concrètes ;
- il est difficile à tester sans toucher SQL et email ;
- remplacer l'infrastructure implique de modifier le service ;
- la création des collaborateurs et l'orchestration métier sont mélangées ;
- le cycle de vie des dépendances est caché.

Le problème n'est pas la présence du mot-clé `new` en soi : c'est le fait que le service construit lui-même des dépendances techniques qu'on voudrait pouvoir remplacer.
</details>

---

## Application au projet fil rouge

Créer :

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
```

Puis :

```csharp
public class InMemoryOrderRepository : IOrderRepository
{
    ...
}
```

Enfin injecter le repository dans `OrderService`.

### Checkpoint

Tu dois pouvoir expliquer :

1. pourquoi `new Order()` est normal alors que `new SqlOrderRepository()` dans `OrderService` peut poser problème ;
2. pourquoi constructor injection est le choix par défaut dans le conteneur .NET ;
3. ce que change un lifetime `Scoped` dans une API ;
4. pourquoi un singleton mutable doit être conçu pour la concurrence ;
5. pourquoi créer une interface pour chaque classe n'est pas une application correcte de DIP.

Le but est de pouvoir remplacer l'implémentation mémoire plus tard par EF Core sans réécrire la logique métier.
