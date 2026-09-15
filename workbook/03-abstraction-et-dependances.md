# 3 — Abstraction, interfaces et dépendances

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer classe concrète, classe abstraite et interface ;
- comprendre une différence importante entre les interfaces TypeScript et C# ;
- expliquer pourquoi on dépend souvent d'un contrat plutôt que d'une implémentation ;
- définir ce qu'est une dépendance ;
- expliquer l'injection de dépendances ;
- distinguer injection par constructeur, méthode et propriété ;
- comprendre ce que le conteneur DI intégré à .NET sait réellement faire ;
- comprendre la différence entre DI et conteneur de DI ;
- comprendre les lifetimes `Transient`, `Scoped` et `Singleton` ;
- identifier un problème de durée de vie ;
- utiliser SOLID comme outil de raisonnement et non comme liste à réciter.

> Dans ce chapitre, les exemples de repository sont volontairement **synchrones**. `Task`, `async` et `CancellationToken` seront introduits au chapitre 5, où nous ferons évoluer ces contrats.

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
    public abstract void Send(string message);
}
```

### Interface

Elle exprime surtout un **contrat** :

```csharp
public interface INotificationSender
{
    void Send(string message);
}
```

Plusieurs implémentations peuvent respecter ce contrat :

```csharp
public class EmailSender : INotificationSender
{
    public void Send(string message)
    {
        Console.WriteLine($"Email: {message}");
    }
}
```

```csharp
public class SmsSender : INotificationSender
{
    public void Send(string message)
    {
        Console.WriteLine($"SMS: {message}");
    }
}
```

### Classe abstraite ou interface ?

Une interface est souvent adaptée lorsqu'on veut surtout exprimer une capacité :

> « cet objet sait envoyer une notification ».

Une classe abstraite devient plus intéressante lorsqu'il existe réellement du comportement ou de l'état partagé entre les implémentations.

Si `EmailSender` et `SmsSender` n'ont rien d'autre en commun que `Send`, l'interface est probablement le contrat le plus simple.

---

## 2. Interface TypeScript vs interface C# : structurelle vs nominale

En TypeScript, le typage est largement **structurel** : un objet peut satisfaire une interface simplement parce qu'il possède la bonne forme.

```ts
interface Sender {
  send(message: string): void;
}

class EmailSender {
  send(message: string) {}
}

const sender: Sender = new EmailSender();
```

`EmailSender` n'a pas besoin d'écrire explicitement `implements Sender` pour être compatible si sa structure convient.

En C#, la relation avec une interface est **explicitement déclarée** :

```csharp
public interface ISender
{
    void Send(string message);
}

public class EmailSender : ISender
{
    public void Send(string message)
    {
    }
}
```

Une classe possédant par hasard une méthode `Send` avec la bonne signature n'est pas pour autant un `ISender`.

### Pourquoi cette différence est utile à comprendre ?

Quand tu lis :

```csharp
public class EmailSender : ISender
```

le code annonce explicitement :

> « `EmailSender` s'engage à respecter ce contrat. »

---

## 3. Dépendre d'une abstraction

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

`OrderService` exprime uniquement son besoin :

> « J'ai besoin de quelque chose capable d'envoyer une notification. »

### `new` n'est pas le problème

Ne retiens surtout pas :

> « il ne faut jamais utiliser `new` ».

Ceci est normal :

```csharp
var order = new Order();
var item = new OrderItem(...);
```

Le problème apparaît plutôt lorsqu'un composant applicatif cache la création d'une dépendance externe ou remplaçable :

```csharp
public class OrderService
{
    private readonly SqlOrderRepository _repository = new();
}
```

La bonne question est :

> cet objet est-il une donnée que je possède, ou un collaborateur dont mon composant dépend ?

---

## 4. Qu'est-ce qu'une dépendance ?

Une dépendance est un objet dont un autre objet a besoin pour réaliser son travail.

```text
OrderService
├── IOrderRepository
└── INotificationSender
```

> Injecter une dépendance consiste à la fournir depuis l'extérieur plutôt qu'à laisser l'objet la créer lui-même.

---

## 5. Les formes d'injection

### Injection par constructeur

```csharp
public OrderService(IOrderRepository repository)
{
    _repository = repository;
}
```

À privilégier pour les dépendances obligatoires.

### Injection par méthode

```csharp
public Report GenerateReport(IFormatter formatter)
{
    ...
}
```

Une dépendance est fournie uniquement à l'opération qui en a besoin.

### Injection par propriété

Conceptuellement :

```csharp
public ILogger? Logger { get; set; }
```

Elle rend la dépendance potentiellement absente et le cycle de vie moins évident.

### Important : concept DI vs conteneur Microsoft

Les trois formes existent comme techniques de conception, mais le **conteneur DI intégré à .NET est centré sur l'injection par constructeur** et ne réalise pas automatiquement l'injection de propriétés.

Réflexe par défaut :

> dépendance obligatoire → constructeur.

---

## 6. DI ≠ conteneur de DI

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

La DI est le principe ; le conteneur est un outil.

---

## 7. Lifetimes

Les lifetimes seront observés concrètement avec ASP.NET Core au chapitre 7. Pour l'instant, comprends leur intention.

### `Transient`

Une nouvelle instance est créée à chaque résolution.

### `Scoped`

Une même instance est réutilisée à l'intérieur d'un scope. Dans ASP.NET Core, un scope correspond normalement à une requête HTTP.

### `Singleton`

Une seule instance est conservée pour toute la durée de vie de l'application.

```text
Singleton
──────────────────────── application

Scoped
──── scope A ────
               ──── scope B ────

Transient
─ instance
  ─ instance
    ─ instance
```

### Singleton mutable

Un singleton peut être utilisé par plusieurs opérations simultanément. S'il contient un état mutable partagé, cet état doit être conçu pour la concurrence.

### Dépendance captive

Un objet très long-lived ne devrait pas conserver par erreur une dépendance conçue pour vivre moins longtemps :

```text
Singleton
   ↓
Scoped service
```

Le chapitre ASP.NET Core permettra d'observer concrètement ces comportements.

---

## 8. SOLID sans récitation

### SRP — Single Responsibility Principle

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

### DIP — Dependency Inversion Principle

Une formulation utile : les composants contenant les politiques importantes de l'application ne devraient pas être forcés de dépendre directement des détails techniques qu'ils orchestrent.

```text
OrderService
    ↓
SqlOrderRepository
```

peut devenir :

```text
OrderService
    ↓
IOrderRepository
    ↑
SqlOrderRepository
```

Mais DIP ne signifie pas « une interface devant chaque classe ».

### ISP — Interface Segregation Principle

Préférer des contrats cohérents à une interface énorme qui force chaque implémentation à supporter des opérations inutiles.

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

Cette interface mélange clairement plusieurs responsabilités.

---

## Exercice — code review

Analyse :

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

Identifie au moins trois problèmes.

<details>
<summary>Correction possible</summary>

- le service choisit ses implémentations concrètes ;
- il est difficile à tester sans SQL/email ;
- remplacer l'infrastructure implique de modifier le service ;
- création des collaborateurs et orchestration sont mélangées ;
- le cycle de vie des dépendances est caché.

Le problème n'est pas le mot-clé `new` en lui-même : c'est le fait de construire ici des dépendances techniques remplaçables.
</details>

---

## Application au projet fil rouge

À ce stade, garde le contrat simple et synchrone :

```csharp
public interface IOrderRepository
{
    Order? GetById(Guid id);
    void Add(Order order);
}
```

Puis :

```csharp
public class InMemoryOrderRepository : IOrderRepository
{
    ...
}
```

Injecte ensuite le repository dans `OrderService`.

Au chapitre 5, ce contrat évoluera volontairement vers :

```text
Task / Task<T>
CancellationToken
```

lorsque ces notions auront été expliquées.

### Checkpoint

Tu dois pouvoir expliquer :

1. pourquoi `new Order()` est normal alors que `new SqlOrderRepository()` dans `OrderService` peut poser problème ;
2. la différence entre compatibilité structurelle TypeScript et implémentation explicite d'une interface C# ;
3. pourquoi constructor injection est le choix par défaut ;
4. ce que signifient Transient, Scoped et Singleton sans encore dépendre d'ASP.NET Core ;
5. pourquoi un singleton mutable mérite une attention particulière ;
6. pourquoi créer une interface pour chaque classe n'est pas une application correcte de DIP.
