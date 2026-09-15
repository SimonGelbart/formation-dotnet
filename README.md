# Formation .NET — Ligne directrice

Objectif : remettre à niveau un développeur ayant déjà une expérience front-end, en s'appuyant sur ses acquis JavaScript/TypeScript pour l'amener progressivement vers C# et le développement backend .NET.

## 1. Passer de TypeScript à C#

- Typage statique et compilation
- `var` vs `any`
- Types valeur vs types référence
- `null` et Nullable Reference Types
- Classes, objets et instances
- Champs vs propriétés
- Constructeurs
- Modificateurs d'accès : `public`, `private`, `protected`, `internal`
- Encapsulation
- Mutabilité et immutabilité
- `const`, `readonly`, `init`, `record`
- `static` vs membre d'instance

## 2. Comprendre la programmation objet et le découplage

- Héritage
- Polymorphisme
- `virtual`, `override`, `abstract`, `sealed`
- `ToString()`, `Equals()` et `GetHashCode()`
- Classe concrète vs classe abstraite vs interface
- Différences entre une interface TypeScript et une interface C#
- Dépendances entre objets
- Pourquoi éviter de créer systématiquement les dépendances concrètes avec `new`
- Injection de dépendances
  - injection par constructeur
  - injection par méthode
  - injection par propriété
- Principes SOLID, avec priorité à SRP, DIP et ISP

## 3. Maîtriser le C# du quotidien

- Génériques
- Collections
  - `Array`
  - `List<T>`
  - `Dictionary<TKey, TValue>`
  - `HashSet<T>`
  - `Queue<T>` / `Stack<T>`
- Notions de complexité : O(1), O(log n), O(n), O(n²)
- Lambdas
- Delegates
- `Func<T>` et `Action<T>`
- `IEnumerable<T>`
- LINQ
  - `Where`
  - `Select`
  - `Any` / `All`
  - `First` / `FirstOrDefault`
  - `Single` / `SingleOrDefault`
  - `OrderBy`
  - `GroupBy`
  - `ToList`
  - exécution différée
- Méthodes et classes d'extension
- Exceptions
- `IDisposable` et `using`
- Programmation asynchrone
  - `Task`
  - `Task<T>`
  - `async` / `await`
  - `Task.WhenAll`
  - `CancellationToken`

## 4. Passer au développement backend .NET

- Conteneur d'injection de dépendances .NET
- Lifetimes : `Transient`, `Scoped`, `Singleton`
- Tests unitaires
  - Arrange / Act / Assert
  - fake / mock
  - intérêt du découplage pour les tests
- Architecture et séparation des responsabilités
  - Controller
  - Service
  - Repository
  - Presentation / Application / Domain / Infrastructure
- Couplage et cohésion
- Design patterns appliqués
  - Strategy
  - Factory
  - Decorator
  - Adapter
  - Repository

## 5. Projet fil rouge

Construire progressivement une petite API .NET permettant de mettre les notions en pratique plutôt que de les apprendre uniquement de manière théorique.

Exemple : API de gestion de commandes avec :

- endpoints HTTP
- Controllers
- Services
- Repositories
- interfaces
- injection de dépendances
- LINQ
- async/await
- gestion des erreurs
- tests
- séparation en couches

## Approche pédagogique

Toujours partir autant que possible d'un parallèle avec le front-end :

- `Array.filter` → `Where`
- `Array.map` → `Select`
- `Array.some` → `Any`
- `Array.every` → `All`
- `Promise<T>` → `Task<T>`
- interface TypeScript → interface C#, en explicitant leurs différences
- immutabilité du state front → records et objets immuables

L'objectif n'est pas d'apprendre des règles ou des patterns par cœur, mais de comprendre les problèmes qu'ils permettent de résoudre et les compromis qu'ils impliquent.
