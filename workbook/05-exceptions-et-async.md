# 5 — Exceptions, ressources et asynchronisme

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- utiliser `try`, `catch`, `finally` et `throw` ;
- éviter les exceptions silencieusement avalées ;
- comprendre `IDisposable`, `IAsyncDisposable`, `using` et `await using` ;
- distinguer mémoire managée et ressources externes ;
- comprendre `Task`, `Task<T>`, `async` et `await` ;
- distinguer travail I/O-bound et CPU-bound ;
- utiliser `Task.WhenAll` pour des opérations indépendantes ;
- comprendre `CancellationToken` ;
- éviter les usages naïfs de `.Result`, `.Wait()`, `async void` et `Task.Run` ;
- faire évoluer un contrat synchrone vers un contrat asynchrone de manière justifiée.

---

## 1. Exceptions : signaler un échec

```csharp
if (quantity <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(quantity));
}
```

Une exception remonte la pile d'appels jusqu'à ce qu'un code décide de la traiter.

---

## 2. `try` / `catch` / `finally`

```csharp
try
{
    ProcessOrder(order);
}
catch (InvalidOperationException exception)
{
    Console.WriteLine(exception.Message);
}
finally
{
    Console.WriteLine("Operation finished");
}
```

Le bloc `finally` est exécuté lors de la sortie du `try`, sauf situations exceptionnelles de terminaison brutale du processus.

### Mauvaise pratique

```csharp
try
{
    ProcessOrder(order);
}
catch (Exception)
{
}
```

Ce code cache complètement l'échec.

> Ne capture une exception que si tu sais quoi en faire : récupérer, transformer, enrichir ou journaliser au bon niveau.

---

## 3. `throw` et stack trace

Dans un `catch` :

```csharp
throw;
```

relance l'exception actuelle en conservant correctement sa trace.

Évite :

```csharp
throw exception;
```

qui réinitialise une partie utile du contexte de pile.

---

## 4. Erreur métier vs erreur technique

Exemples techniques :

- fichier introuvable ;
- timeout réseau ;
- connexion SQL interrompue.

Exemples métier :

- commande vide impossible à confirmer ;
- quantité invalide ;
- transition de statut interdite.

Toutes les erreurs métier n'ont pas forcément besoin d'une exception. Selon le contexte, un type `Result`, une validation explicite ou un autre résultat métier peut être plus adapté.

Le point important : ne transforme pas les exceptions en simple `if/else` du flux normal.

---

## 5. `IDisposable` et `using`

Le garbage collector gère la mémoire managée, mais certaines ressources doivent être libérées de façon déterministe.

```csharp
using var stream = File.OpenRead(path);
```

À la fin de la portée, `Dispose()` est appelé.

Ressources concernées selon les APIs :

```text
fichiers
streams
handles système
sockets
certaines connexions / ressources externes
```

### `IAsyncDisposable`

```csharp
await using var resource = await OpenAsyncResource();
```

Le compilateur appelle `DisposeAsync()` à la fin de la portée.

---

## 6. `Task` et `Task<T>`

Parallèle front-end :

```text
Promise<User>  ↔  Task<User>
```

Une opération sans résultat peut retourner :

```csharp
Task
```

Une opération qui produira un utilisateur :

```csharp
Task<User>
```

Un `Task<User>` n'est pas un `User`.

```text
GetUserAsync()
      ↓
  Task<User>
      ↓ await
     User
```

Un `Task` représente une opération qui peut être déjà terminée ou se terminer plus tard.

---

## 7. `async` / `await`

```csharp
public async Task<User?> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    return user;
}
```

`await` permet d'écrire un flux asynchrone avec une structure proche du code synchrone.

### `async` n'est pas « nouveau thread »

`await` ne signifie pas automatiquement :

> « exécute ça sur un autre thread ».

Pour une attente I/O, le thread n'a pas besoin de rester bloqué uniquement pour attendre la réponse distante.

---

## 8. I/O-bound vs CPU-bound

### I/O-bound

Le programme attend surtout un système externe :

```text
base de données
HTTP
fichier
réseau
```

Exemple :

```csharp
var order = await dbContext.Orders.FirstAsync(...);
```

### CPU-bound

Le processeur travaille réellement :

```text
compression
calcul intensif
encodage
traitement d'image
```

`async` ne rend pas ce calcul moins coûteux.

### Ne pas emballer artificiellement les I/O dans `Task.Run`

À éviter :

```csharp
await Task.Run(() => dbContext.Orders.ToList());
```

Si une API asynchrone existe :

```csharp
await dbContext.Orders.ToListAsync(cancellationToken);
```

utilise-la directement.

---

## 9. Faire évoluer le repository du chapitre 3

Au chapitre 3, nous avons volontairement utilisé :

```csharp
public interface IOrderRepository
{
    Order? GetById(Guid id);
    void Add(Order order);
}
```

Cette forme est suffisante pour apprendre l'abstraction et la DI sans introduire l'async trop tôt.

Une vraie persistance fera cependant de l'I/O. Faisons maintenant évoluer le contrat :

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

### Pourquoi ce refactoring est intéressant ?

Tu sais maintenant expliquer chaque élément :

```text
Task<Order?>
→ résultat disponible maintenant ou plus tard

Async suffix
→ convention qui annonce une opération asynchrone

CancellationToken
→ possibilité coopérative d'abandonner l'opération
```

L'async n'est plus une syntaxe tombée du ciel : elle répond à la nature I/O du futur repository EF Core.

### Implémentation mémoire

Une implémentation mémoire n'effectue pas réellement d'I/O. Elle peut cependant respecter le même contrat :

```csharp
public Task<Order?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
{
    _orders.TryGetValue(id, out var order);
    return Task.FromResult(order);
}
```

Ne rends pas artificiellement la méthode `async` s'il n'y a aucun `await` utile.

---

## 10. Pourquoi l'async est important en backend

Une API attend souvent :

- une base de données ;
- une autre API ;
- un fichier ;
- le réseau.

Bloquer un thread pendant toute cette attente limite la capacité du serveur à traiter d'autres requêtes.

L'async n'est donc pas principalement :

> « rendre l'opération individuelle plus rapide ».

Il améliore surtout l'utilisation des ressources pendant les attentes I/O.

---

## 11. `Task.WhenAll`

Séquentiel :

```csharp
var customer = await GetCustomerAsync(id);
var offers = await GetOffersAsync(id);
```

Si les deux opérations sont réellement indépendantes :

```csharp
var customerTask = GetCustomerAsync(id);
var offersTask = GetOffersAsync(id);

await Task.WhenAll(customerTask, offersTask);

var customer = await customerTask;
var offers = await offersTask;
```

### Exercice chronométré

Crée deux méthodes simulant chacune une attente d'une seconde :

```csharp
await Task.Delay(1000);
```

Mesure :

1. deux appels attendus séquentiellement ;
2. deux tâches démarrées puis attendues avec `Task.WhenAll`.

Tu devrais observer approximativement :

```text
séquentiel  ≈ 2 s
concurrent  ≈ 1 s
```

Puis réponds :

> cela prouve-t-il que deux threads ont travaillé pendant une seconde ?

<details>
<summary>Correction</summary>

Non. `Task.Delay` représente une attente asynchrone ; il ne monopolise pas un thread pendant toute la durée du délai. Concurrence asynchrone et parallélisme CPU sont deux notions différentes.
</details>

### Attention à EF Core

Ne lance pas deux opérations EF Core simultanées sur la **même instance de `DbContext`** : ce contexte n'est pas conçu pour des opérations concurrentes.

---

## 12. `.Result` et `.Wait()`

À éviter par défaut dans une chaîne asynchrone :

```csharp
var user = GetUserAsync(id).Result;
```

```csharp
GetUserAsync(id).Wait();
```

Ces appels bloquent le thread et cassent la propagation naturelle de l'asynchronisme.

> Quand une chaîne devient asynchrone, laisse généralement `async` / `await` remonter jusqu'à la frontière appropriée.

---

## 13. Éviter `async void`

Une méthode asynchrone retourne normalement :

```csharp
Task
Task<T>
```

Évite :

```csharp
public async void SaveAsync()
```

L'appelant ne peut pas attendre normalement sa fin ni observer facilement son exception.

L'usage principal légitime est celui des event handlers dont la signature impose `void`.

---

## 14. `CancellationToken`

Une opération peut devenir inutile avant sa fin :

- client HTTP déconnecté ;
- action utilisateur annulée ;
- application en arrêt.

```csharp
public Task<Order?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
{
    ...
}
```

L'annulation est coopérative : elle indique aux opérations qu'elles devraient s'arrêter si possible.

### Mauvais réflexe

Recevoir le token puis ne jamais le transmettre :

```csharp
public async Task<Order?> GetAsync(CancellationToken cancellationToken)
{
    return await dbContext.Orders.FirstOrDefaultAsync();
}
```

Mieux :

```csharp
return await dbContext.Orders
    .FirstOrDefaultAsync(cancellationToken);
```

---

## Application au projet fil rouge

À ce stade :

- fais évoluer `IOrderRepository` vers `Task` / `Task<T>` ;
- fais évoluer `OrderService` vers des méthodes `Async` lorsque ses dépendances sont asynchrones ;
- propage un `CancellationToken` ;
- garde les règles métier pures synchrones lorsqu'elles n'ont aucune I/O (`order.Confirm()`, calcul de total, etc.) ;
- ne rends pas tout `async` mécaniquement.

### Checkpoint final

Tu dois pouvoir expliquer :

1. pourquoi `Task<Order>` n'est pas un `Order` ;
2. pourquoi `async` n'est pas synonyme de parallélisme ou de nouveau thread ;
3. différence I/O-bound / CPU-bound ;
4. pourquoi le repository est devenu asynchrone alors que `Order.Confirm()` reste synchrone ;
5. quand `Task.WhenAll` est pertinent ;
6. pourquoi deux opérations EF ne doivent pas partager un même `DbContext` simultanément ;
7. pourquoi `.Result`, `.Wait()` et `async void` sont à éviter par défaut ;
8. pourquoi `using` et `await using` existent malgré le garbage collector.
