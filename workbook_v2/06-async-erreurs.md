# 06 — Attendre et gérer les erreurs

**Pratiquer :** `Task`, `async`/`await`, propagation d'erreur. **Comprendre :** annulation, ressources, configuration et logs. **Repérer :** concurrence asynchrone.

Résultat : une console appelle l'API mémoire sans bloquer un thread pendant l'attente réseau.

## Programme complet — `Sandbox/Program.cs`

Laisse l'API mémoire tourner sur le port 5080. Le catalogue peut être vide : la requête reste valide.

```csharp
using System.Net.Http.Json;

using var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5080");
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var products = await client.GetFromJsonAsync<List<ProductResponse>>(
        "/products", timeout.Token);
    Console.WriteLine($"Produits reçus : {products?.Count ?? 0}");
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"Appel HTTP impossible : {exception.Message}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Attente annulée.");
}
finally
{
    Console.WriteLine("Appel terminé.");
}

public record ProductResponse(Guid Id, string Name, decimal Price);
```

Attendu : le nombre de produits, puis `Appel terminé`. Arrête ensuite l'API et relance la console : le message d'erreur devient visible.

## Ce que signifie await

`Task<T>` joue un rôle comparable à `Promise<T>` : l'opération produira un résultat, ou un échec. `await` attend son achèvement et permet de récupérer le résultat. Une méthode déclarée `async Task<Product>` peut ainsi retourner un Product après une attente. `Task` sans paramètre représente une opération sans résultat à récupérer.

`async` ne veut pas dire « nouveau thread ». Pour une attente réseau, le programme peut laisser le thread disponible pendant l'attente. Un calcul lourd, lui, continue de demander du travail au processeur.

Évite `.Result` et `.Wait()` dans une chaîne asynchrone : ils bloquent le thread. Retourne `Task` plutôt que `async void` pour que l'appelant puisse attendre et observer les erreurs. Les handlers d'événements sont un cas particulier à découvrir plus tard.

## Traiter une erreur au bon endroit

`throw` interrompt le chemin normal. `catch` traite un type d'échec. `finally` exécute le nettoyage à la sortie du bloc dans le fonctionnement normal du processus. Un `catch` vide cache l'erreur.

Dans un `catch`, `throw;` relance l'erreur en conservant sa trace. Évite `throw exception;`.

Une recherche sans résultat peut retourner `null`. C'est souvent une absence normale, pas une panne. Une erreur de connexion est un échec technique. Une commande déjà confirmée sera un conflit métier. Ces situations n'auront pas toutes le même traitement HTTP.

## Ressources et annulation

`using var` appelle `Dispose` à la fin de la portée, même quand une exception en fait sortir. Le garbage collector gère la mémoire ; il ne remplace pas toujours la libération rapide d'une ressource. `await using` est la variante à reconnaître lorsque la libération elle-même est asynchrone.

Le token demande une annulation coopérative. Dans une API, le token reçu par le Controller est transmis au service puis à EF ou HttpClient. Le recevoir sans le transmettre ne suffit pas.

L'instance HttpClient ci-dessus dure pendant tout ce court programme console. Dans une API qui tourne longtemps, préfère un client géré par `IHttpClientFactory`, comme dans la lecture guidée suivante.

## Lecture guidée — configuration, client typé et logs

Cette extension est facultative et n'est pas nécessaire au fil rouge. Les fichiers complets sont dans [l'annexe client HTTP](annexes/client-http.md). Elle permet à une seconde application d'appeler l'API mémoire.

À retenir : `appsettings.json` porte la configuration, `IConfiguration` la lit, `AddHttpClient` prépare le client et `ILogger<T>` écrit des logs structurés. Ne mets pas de vrais secrets dans un fichier versionné. Les variables d'environnement ou les user secrets permettent de les fournir séparément. `IOptions<T>` est une façon typée de regrouper la configuration, à approfondir plus tard.

## Exercice — comparer deux attentes

Programme complet indépendant :

```csharp
using System.Diagnostics;

var watch = Stopwatch.StartNew();
await Task.Delay(500);
await Task.Delay(500);
Console.WriteLine(watch.ElapsedMilliseconds);

watch.Restart();
await Task.WhenAll(Task.Delay(500), Task.Delay(500));
Console.WriteLine(watch.ElapsedMilliseconds);
```

Prédis les deux durées. Est-ce la preuve que deux threads ont calculé simultanément ?

<details>
<summary>Correction</summary>

Environ 1000 ms puis 500 ms, avec une marge liée à l'environnement. Les deux attentes se chevauchent ; ce ne sont pas deux calculs CPU. `WhenAll` convient aux opérations indépendantes. Ne l'utilise pas pour lancer plusieurs opérations sur le même DbContext.
</details>

## Trois questions

1. Quelle différence entre `Task<Product>` et `Product` ?
2. Pourquoi éviter un `catch` vide ?
3. À quoi sert de transmettre un token ?

Suite : [07 — Persistance](07-persistance.md).
