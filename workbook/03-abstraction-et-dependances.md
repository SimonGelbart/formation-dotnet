# 3 — Abstraction, interfaces et dépendances

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- distinguer classe concrète, classe abstraite et interface ;
- expliquer pourquoi on dépend souvent d'un contrat plutôt que d'une implémentation ;
- définir ce qu'est une dépendance ;
- expliquer l'injection de dépendances ;
- distinguer injection par constructeur, méthode et propriété ;
- comprendre la différence entre DI et conteneur de DI ;
- comprendre les lifetimes `Transient`, `Scoped` et `Singleton` ;
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

### Parallèle TypeScript

Une interface TypeScript est souvent utilisée pour décrire la forme d'une donnée. En C#, une interface est très souvent utilisée comme **contrat comportemental** entre composants.

---

## 2. Dépendre d'une abstraction

Mauvaise version :

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

Cela crée un couplage fort.

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

```csharp
public Report GenerateReport(IFormatter formatter)
{
    ...
}
```

Pertinente lorsqu'une dépendance n'est nécessaire que pour une opération précise.

### Injection par propriété

```csharp
public ILogger? Logger { get; set; }
```

Elle rend la dépendance potentiellement absente et le cycle de vie moins évident. Ce n'est généralement pas le premier choix pour une dépendance obligatoire.

### Règle pratique

> Pour les dépendances nécessaires au fonctionnement normal d'un service, commence par l'injection par constructeur.

---

## 5. DI ≠ conteneur de DI

Ce code utilise déjà l'injection de dépendances :

```csharp
var repository = new InMemoryOrderRepository();
var sender = new EmailSender();

var service = new OrderService(repository, sender);
```

Aucun framework n'est nécessaire.

Un conteneur de DI automatise la composition :

```csharp
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<INotificationSender, EmailSender>();
```

Puis ASP.NET Core peut construire les objets pour nous.

---

## 6. Lifetimes

### `Transient`

Une nouvelle instance est créée à chaque résolution.

### `Scoped`

Dans une application web, on utilise généralement une même instance dans la durée d'une requête HTTP.

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

### Question importante

Pourquoi un singleton qui contient un état mutable peut-il être risqué ?

Parce que plusieurs requêtes peuvent partager et modifier le même état.

---

## 7. SOLID sans récitation

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

Couplage fort :

```text
OrderService
    ↓
SqlOrderRepository
```

Couplage via contrat :

```text
OrderService
    ↓
IOrderRepository
    ↑
SqlOrderRepository
```

L'objectif n'est pas de mettre une interface partout, mais de contrôler les dépendances qui comptent réellement.

### ISP — Interface Segregation Principle

Préférer plusieurs petits contrats cohérents à une interface énorme qui force chaque implémentation à supporter des méthodes inutiles.

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
- la création et la logique métier sont mélangées ;
- le cycle de vie des dépendances est caché.
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

Le but est de pouvoir remplacer l'implémentation mémoire plus tard par EF Core sans réécrire la logique métier.
