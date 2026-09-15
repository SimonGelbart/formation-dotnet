# 4 — Génériques, collections et LINQ

## Objectifs

À la fin de ce chapitre, tu dois savoir :

- comprendre à quoi servent les génériques ;
- choisir une collection adaptée au besoin ;
- raisonner simplement en termes de complexité `O(1)`, `O(n)`, `O(n²)` ;
- lire et écrire des lambdas ;
- comprendre `Func<T>` et `Action<T>` ;
- utiliser les principaux opérateurs LINQ ;
- comprendre l'exécution différée ;
- distinguer une séquence `IEnumerable<T>` d'une collection matérialisée ;
- comprendre le principe des méthodes d'extension.

---

## 1. Génériques

En TypeScript, tu connais déjà ce principe :

```ts
Array<User>
Promise<User>
```

En C# :

```csharp
List<User>
Task<User>
Dictionary<Guid, User>
```

Le paramètre générique permet de réutiliser une structure sans perdre l'information de type.

Exemple :

```csharp
public class Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
}
```

On peut alors écrire :

```csharp
Result<User>
Result<Order>
Result<Product>
```

### Pourquoi ne pas utiliser `object` ?

Avec `T`, le compilateur conserve le vrai type. On évite les casts et on obtient une API plus claire.

---

## 2. Les collections courantes

### `Array`

Taille fixe après création.

```csharp
var values = new int[] { 1, 2, 3 };
```

### `List<T>`

Collection ordonnée dynamique.

```csharp
var users = new List<User>();
users.Add(user);
```

### `Dictionary<TKey, TValue>`

Associe une clé à une valeur.

```csharp
var usersById = new Dictionary<Guid, User>();
usersById[user.Id] = user;
```

### `HashSet<T>`

Ensemble de valeurs uniques.

### `Queue<T>`

Premier entré, premier sorti.

### `Stack<T>`

Dernier entré, premier sorti.

---

## 3. Complexité : choisir selon l'usage

Supposons 100 000 utilisateurs et une recherche répétée par ID.

Avec une liste :

```csharp
var user = users.FirstOrDefault(x => x.Id == id);
```

Dans le pire cas, on inspecte chaque élément : environ `O(n)`.

Avec un dictionnaire :

```csharp
var user = usersById[id];
```

La recherche par clé est en moyenne proche de `O(1)`.

### Intuition

```text
O(1)   : le coût reste globalement stable
O(n)   : le travail augmente avec le nombre d'éléments
O(n²)  : le travail peut exploser avec deux boucles imbriquées
```

Le but n'est pas de faire de l'algorithmique avancée mais d'éviter les mauvais choix évidents.

### Exercice

Tu dois vérifier pour chaque commande si son `CustomerId` existe dans une liste de 50 000 clients.

Version A :

```csharp
foreach (var order in orders)
{
    var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);
}
```

Comment améliorer la structure de données si cette recherche est fréquente ?

<details>
<summary>Correction</summary>

Construire par exemple un `Dictionary<Guid, Customer>` indexé par ID pour éviter de rescanner toute la liste pour chaque commande.
</details>

---

## 4. Lambdas

JavaScript :

```js
users.filter(user => user.age >= 18)
```

C# :

```csharp
users.Where(user => user.Age >= 18)
```

La partie :

```csharp
user => user.Age >= 18
```

est une lambda : une fonction anonyme.

---

## 5. `Func` et `Action`

Une lambda peut être stockée dans un delegate.

```csharp
Func<User, bool> isAdult = user => user.Age >= 18;
```

`Func<User, bool>` signifie :

> fonction qui reçoit un `User` et retourne un `bool`.

`Action<T>` représente une fonction qui ne retourne pas de valeur :

```csharp
Action<User> printUser = user => Console.WriteLine(user.Name);
```

### Pourquoi c'est important ?

LINQ accepte justement des fonctions comme paramètres.

---

## 6. `IEnumerable<T>`

`IEnumerable<T>` représente une séquence que l'on peut parcourir.

```csharp
IEnumerable<User> users = ...;
```

Cela ne garantit pas que l'objet soit une `List<User>`.

Une API peut retourner `IEnumerable<User>` lorsqu'elle veut surtout exprimer :

> « voici une séquence d'utilisateurs à parcourir ».

---

## 7. Les opérateurs LINQ essentiels

### `Where`

Filtrer :

```csharp
var activeUsers = users.Where(x => x.IsActive);
```

### `Select`

Transformer :

```csharp
var names = users.Select(x => x.Name);
```

### `Any` / `All`

```csharp
var hasAdmin = users.Any(x => x.IsAdmin);
var allActive = users.All(x => x.IsActive);
```

### `First` / `FirstOrDefault`

```csharp
var user = users.FirstOrDefault(x => x.Id == id);
```

`First` lève une exception si aucun élément n'est trouvé. `FirstOrDefault` retourne la valeur par défaut, généralement `null` pour un type référence.

### `Single` / `SingleOrDefault`

Exprime qu'il ne doit y avoir **au maximum qu'un élément correspondant**. Une seconde correspondance provoque une exception.

### `OrderBy`

```csharp
var sorted = users.OrderBy(x => x.Name);
```

### `GroupBy`

```csharp
var byCountry = users.GroupBy(x => x.Country);
```

### `ToList`

Matérialise la séquence dans une liste.

---

## 8. Lire une chaîne LINQ

```csharp
var names = users
    .Where(user => user.IsActive)
    .OrderBy(user => user.Name)
    .Select(user => user.Name)
    .ToList();
```

Lis-la comme une pipeline :

```text
users
 ↓ garder les actifs
 ↓ trier par nom
 ↓ ne garder que le nom
 ↓ matérialiser dans une List<string>
```

### Exercice

Avant d'exécuter ce code, décris chaque transformation en français :

```csharp
var result = orders
    .Where(x => x.Total >= 100m)
    .OrderByDescending(x => x.Total)
    .Take(5)
    .Select(x => new { x.Id, x.Total })
    .ToList();
```

---

## 9. Exécution différée

Considère :

```csharp
var adults = users.Where(x => x.Age >= 18);

users.Add(new User { Name = "Bob", Age = 30 });

foreach (var adult in adults)
{
    Console.WriteLine(adult.Name);
}
```

La requête LINQ n'est pas forcément exécutée au moment du `Where`. Elle peut être exécutée au moment de l'énumération.

Le nouvel utilisateur peut donc faire partie du résultat.

Avec :

```csharp
var adults = users.Where(x => x.Age >= 18).ToList();
```

la séquence est matérialisée immédiatement dans une nouvelle liste.

### À retenir

> Une expression LINQ peut représenter un calcul à exécuter plus tard, pas nécessairement un résultat déjà calculé.

---

## 10. Méthodes d'extension

Lorsque tu écris :

```csharp
users.Where(...)
```

`Where` n'est pas nécessairement une méthode définie directement dans le type concret de `users`.

On peut créer ses propres méthodes d'extension :

```csharp
public static class OrderEnumerableExtensions
{
    public static IEnumerable<Order> Confirmed(
        this IEnumerable<Order> orders)
    {
        return orders.Where(x => x.Status == "Confirmed");
    }
}
```

Puis :

```csharp
var confirmed = orders.Confirmed();
```

Une méthode d'extension est fondamentalement une méthode statique rendue plus agréable à appeler.

---

## Application au projet fil rouge

Ajouter des opérations permettant :

- de lister uniquement les commandes confirmées ;
- de calculer le total des commandes ;
- de récupérer les cinq commandes les plus chères ;
- de grouper les commandes par client ;
- de rechercher efficacement une commande par ID dans l'implémentation mémoire.

### Checkpoint

Tu dois pouvoir expliquer sans regarder le chapitre :

1. différence entre `List<T>` et `Dictionary<TKey,TValue>` ;
2. différence entre `Where` et `Select` ;
3. différence entre `FirstOrDefault` et `SingleOrDefault` ;
4. pourquoi `ToList()` change le moment d'exécution d'une requête LINQ.
