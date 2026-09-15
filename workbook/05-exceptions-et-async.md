# 5 — Exceptions, ressources et asynchronisme

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- utiliser `try`, `catch`, `finally` et `throw` ;
- éviter les exceptions silencieusement avalées ;
- comprendre le rôle de `IDisposable`, `IAsyncDisposable`, `using` et `await using` ;
- distinguer mémoire managée et ressources externes ;
- comprendre `Task`, `Task<T>`, `async` et `await` ;
- distinguer travail I/O-bound et CPU-bound ;
- utiliser `Task.WhenAll` pour des opérations indépendantes ;
- comprendre l'intérêt de `CancellationToken` ;
- éviter les usages naïfs de `.Result`, `.Wait()`, `async void` et `Task.Run`.

---

## 1. Exceptions : signaler un échec exceptionnel

Une exception indique qu'une opération n'a pas pu se dérouler normalement.

```csharp
if (quantity <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(quantity));
}
```

L'exception remonte la pile d'appels jusqu'à ce qu'un code décide de la traiter.

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

Le bloc `finally` est exécuté lors de la sortie du bloc `try`, qu'une exception ait été levée ou non, sauf situations exceptionnelles de terminaison brutale du processus.

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

Ce code cache complètement l'échec. L'application continue sans que personne ne sache ce qui s'est passé.

### Règle pratique

> Ne capture une exception que si tu sais quoi en faire : la transformer, l'enrichir, la journaliser au bon niveau ou proposer un comportement de récupération.

---

## 3. `throw` et conservation de la stack trace

Dans un `catch`, ceci relance l'exception actuelle :

```csharp
throw;
```

Évite de remplacer inutilement la stack trace avec :

```csharp
throw exception;
```

L'information sur le chemin qui a mené à l'erreur est extrêmement utile au diagnostic.

---

## 4. Exceptions métier vs erreurs techniques

Quelques exemples techniques :

- fichier introuvable ;
- connexion réseau interrompue ;
- timeout ;
- contrainte SQL violée.

Quelques erreurs liées au domaine :

- commande vide impossible à confirmer ;
- quantité invalide ;
- transition de statut interdite.

Toutes les erreurs métier n'ont pas forcément besoin d'une exception. Selon le contexte, un type `Result`, une validation explicite ou une réponse HTTP adaptée peut être préférable.

Le point important est d'éviter d'utiliser les exceptions comme un simple `if/else` déguisé pour le fonctionnement normal.

---

## 5. `IDisposable` et `using`

Le garbage collector gère la mémoire managée, mais une application manipule aussi des ressources externes :

- fichiers ;
- sockets ;
- handles système ;
- flux ;
- certaines ressources de base de données.

Ces ressources doivent parfois être libérées explicitement.

```csharp
using var stream = File.OpenRead(path);
```

À la sortie de la portée, `Dispose()` est appelé automatiquement.

Forme longue :

```csharp
using (var stream = File.OpenRead(path))
{
    // utilisation du flux
}
```

### `IAsyncDisposable` et `await using`

Certaines ressources ont une libération qui elle-même peut nécessiter une opération asynchrone.

```csharp
await using var resource = await OpenAsyncResource();
```

Le compilateur appelle alors `DisposeAsync()` à la fin de la portée.

### Checkpoint

Pourquoi le garbage collector ne suffit-il pas à lui seul pour toutes les ressources ?

<details>
<summary>Réponse</summary>

Parce que toutes les ressources ne sont pas simplement de la mémoire managée. Certaines représentent des ressources du système d'exploitation ou des connexions externes dont la libération doit être déterministe.
</details>

---

## 6. `Task` et `Task<T>`

Parallèle front-end :

```text
Promise<User>  ↔  Task<User>
```

Une méthode asynchrone peut retourner :

```csharp
Task
```

ou :

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

Un `Task` représente une opération qui peut être terminée maintenant ou plus tard. `await` permet d'en attendre le résultat sans bloquer volontairement le thread appelant pendant une attente asynchrone.

---

## 7. `async` / `await`

```csharp
public async Task<User?> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    return user;
}
```

`await` permet d'écrire une séquence asynchrone avec un flux de contrôle proche du code synchrone.

Le mot clé `async` permet notamment l'utilisation de `await` et le compilateur transforme la méthode en machine d'état.

### `async` n'est pas « nouveau thread »

`await` ne signifie pas automatiquement :

> « exécute cette opération sur un autre thread ».

Pour une attente I/O, aucun thread n'a besoin de rester bloqué uniquement pour attendre la réponse distante.

---

## 8. I/O-bound vs CPU-bound

### I/O-bound

Une grande partie du temps est passée à attendre un système externe :

```text
base de données
HTTP
fichier
réseau
```

C'est le cas typique de l'async backend.

```csharp
var order = await dbContext.Orders.FirstAsync(...);
```

### CPU-bound

Le processeur travaille réellement pendant une durée importante :

```text
compression
calcul scientifique
encodage
traitement d'image
```

`async` ne rend pas magiquement ce calcul moins coûteux.

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

## 9. Pourquoi l'async est important en backend

Une API passe beaucoup de temps à attendre :

- la base de données ;
- une autre API ;
- un fichier ;
- le réseau.

Bloquer inutilement un thread pendant toute cette attente limite la capacité du serveur à traiter d'autres requêtes.

L'async n'est donc pas principalement :

> « rendre une opération individuelle plus rapide ».

Il sert surtout à utiliser plus efficacement les ressources pendant les attentes d'I/O et améliore la scalabilité du serveur.

---

## 10. Deux opérations indépendantes avec `Task.WhenAll`

Version séquentielle :

```csharp
var customer = await GetCustomerAsync(id);
var offers = await GetOffersAsync(id);
```

Si les deux appels sont réellement indépendants :

```csharp
var customerTask = GetCustomerAsync(id);
var offersTask = GetOffersAsync(id);

await Task.WhenAll(customerTask, offersTask);

var customer = await customerTask;
var offers = await offersTask;
```

On évite d'attendre la fin du premier appel avant même de démarrer le second.

### Attention : concurrence ≠ toujours autorisée

Avant de paralléliser deux opérations, vérifie que les objets utilisés supportent cet usage.

En particulier, **ne lance pas deux opérations EF Core simultanées sur la même instance de `DbContext`**. Un contexte doit terminer une opération avant d'en commencer une autre.

À éviter :

```csharp
var ordersTask = dbContext.Orders.ToListAsync(cancellationToken);
var customersTask = dbContext.Customers.ToListAsync(cancellationToken);

await Task.WhenAll(ordersTask, customersTask);
```

si les deux tâches utilisent exactement le même `DbContext`.

### Exercice

Une page doit charger :

- le profil utilisateur depuis une API ;
- ses notifications depuis une autre API ;
- ses préférences depuis un troisième service.

Les trois clients sont indépendants. Écris une version qui démarre les trois opérations avant de les attendre ensemble.

Puis demande-toi : seraient-elles toujours parallélisables si elles utilisaient toutes la même instance de `DbContext` ?

---

## 11. `.Result` et `.Wait()`

Code à éviter par défaut dans une chaîne asynchrone :

```csharp
var user = GetUserAsync(id).Result;
```

ou :

```csharp
GetUserAsync(id).Wait();
```

Ces appels bloquent le thread et cassent la propagation naturelle de l'asynchronisme. Selon le contexte, ils peuvent également contribuer à des problèmes de deadlock.

Règle pratique :

> Quand une chaîne d'appel devient asynchrone, laisse généralement `async` / `await` remonter jusqu'à la frontière appropriée.

---

## 12. Éviter `async void`

Une méthode asynchrone retourne normalement :

```csharp
Task
Task<T>
```

Évite :

```csharp
public async void SaveAsync()
```

car l'appelant ne peut ni attendre normalement la fin de l'opération, ni observer facilement son exception.

L'usage principal légitime de `async void` est celui des **event handlers** dont la signature impose `void`.

---

## 13. `CancellationToken`

Une opération peut devenir inutile avant sa fin :

- le client HTTP ferme la connexion ;
- l'utilisateur annule une action ;
- l'application s'arrête.

On peut propager un token :

```csharp
public Task<Order?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
{
    ...
}
```

Puis le transmettre aux appels qui le supportent.

L'annulation est coopérative : elle indique aux opérations qu'elles devraient s'arrêter si possible. Elle ne « tue » pas arbitrairement le code en cours.

### Mauvais réflexe

Recevoir un token puis ne jamais le transmettre :

```csharp
public async Task<Order?> GetAsync(CancellationToken cancellationToken)
{
    return await dbContext.Orders.FirstOrDefaultAsync(); // token oublié
}
```

Mieux :

```csharp
return await dbContext.Orders
    .FirstOrDefaultAsync(cancellationToken);
```

---

## Application au projet fil rouge

Faire en sorte que :

- les méthodes du repository retournent des `Task` lorsqu'elles font réellement de l'I/O ou doivent respecter un contrat asynchrone ;
- `OrderService` propage l'asynchronisme ;
- un `CancellationToken` soit propagé jusqu'aux appels qui le supportent ;
- les exceptions métier ne soient pas silencieusement avalées ;
- les éventuelles ressources jetables soient correctement libérées ;
- aucune opération EF Core parallèle ne partage le même `DbContext`.

### Checkpoint final

Tu dois pouvoir expliquer :

1. pourquoi `Task<Order>` n'est pas un `Order` ;
2. pourquoi `async` n'est pas synonyme de parallélisme ou de nouveau thread ;
3. différence entre I/O-bound et CPU-bound ;
4. quand `Task.WhenAll` est pertinent et quand il peut être dangereux ;
5. pourquoi `.Result` / `.Wait()` sont à éviter par défaut ;
6. pourquoi `async void` est rarement approprié ;
7. pourquoi un `catch (Exception) { }` est dangereux ;
8. pourquoi `using` et `await using` existent alors qu'il y a un garbage collector.
