# 5 — Exceptions, ressources et asynchronisme

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- utiliser `try`, `catch`, `finally` et `throw` ;
- éviter les exceptions silencieusement avalées ;
- comprendre le rôle de `IDisposable` et `using` ;
- distinguer mémoire managée et ressources externes ;
- comprendre `Task`, `Task<T>`, `async` et `await` ;
- utiliser `Task.WhenAll` pour des opérations indépendantes ;
- comprendre l'intérêt de `CancellationToken` ;
- éviter les usages naïfs de `.Result` et `.Wait()`.

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

Le bloc `finally` est exécuté qu'il y ait ou non une exception.

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

Forme longue équivalente conceptuellement :

```csharp
using (var stream = File.OpenRead(path))
{
    // utilisation du flux
}
```

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

---

## 7. `async` / `await`

```csharp
public async Task<User?> GetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    return user;
}
```

`await` permet d'attendre logiquement le résultat sans écrire une chaîne complexe de callbacks.

Le mot clé `async` permet notamment l'utilisation de `await` et transforme la méthode en machine d'état asynchrone.

---

## 8. Pourquoi l'async est important en backend

Une API passe beaucoup de temps à attendre :

- la base de données ;
- une autre API ;
- un fichier ;
- le réseau.

Bloquer inutilement un thread pendant toute cette attente limite la capacité du serveur à traiter d'autres requêtes.

L'async n'est donc pas principalement :

> « rendre le code plus rapide ».

Il sert surtout à utiliser plus efficacement les ressources pendant les attentes d'I/O.

---

## 9. Deux opérations indépendantes avec `Task.WhenAll`

Version séquentielle :

```csharp
var customer = await GetCustomerAsync(id);
var offers = await GetOffersAsync(id);
```

Si les deux appels sont indépendants :

```csharp
var customerTask = GetCustomerAsync(id);
var offersTask = GetOffersAsync(id);

await Task.WhenAll(customerTask, offersTask);

var customer = await customerTask;
var offers = await offersTask;
```

On évite d'attendre la fin du premier appel avant même de démarrer le second.

### Exercice

Une page doit charger :

- le profil utilisateur ;
- ses notifications ;
- ses préférences.

Les trois sources sont indépendantes. Écris une version qui démarre les trois opérations avant de les attendre ensemble.

---

## 10. `.Result` et `.Wait()`

Code à éviter par défaut dans une chaîne asynchrone :

```csharp
var user = GetUserAsync(id).Result;
```

ou :

```csharp
GetUserAsync(id).Wait();
```

Ces appels bloquent le thread et cassent la propagation naturelle de l'asynchronisme.

Règle pratique :

> Quand une chaîne d'appel devient asynchrone, laisse généralement `async` / `await` remonter jusqu'à la frontière appropriée.

---

## 11. `CancellationToken`

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

L'annulation est coopérative : elle indique aux opérations qu'elles devraient s'arrêter si possible.

---

## Application au projet fil rouge

Faire en sorte que :

- les méthodes du repository retournent des `Task` ;
- `OrderService` soit asynchrone ;
- un `CancellationToken` soit propagé ;
- les exceptions métier ne soient pas silencieusement avalées ;
- les éventuelles ressources jetables soient correctement libérées.

### Checkpoint final

Tu dois pouvoir expliquer :

1. pourquoi `Task<Order>` n'est pas un `Order` ;
2. pourquoi `async` n'est pas synonyme de parallélisme ;
3. quand `Task.WhenAll` est pertinent ;
4. pourquoi un `catch (Exception) { }` est dangereux ;
5. pourquoi `using` existe alors qu'il y a un garbage collector.
