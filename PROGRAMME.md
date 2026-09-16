# Programme de référence — Formation .NET

> Parcours principal : [Apprendre .NET en construisant une API](parcours/README.md). Une progression guidée, du catalogue aux commandes, avec niveaux d’apprentissage, exercices corrigés et applications de référence.
>
> Référence complémentaire : [Approfondissements .NET](approfondissements/README.md), à consulter au besoin pour les nuances, mécanismes internes et décisions de conception.

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

## 4. Comprendre l'écosystème .NET

- .NET SDK vs runtime
- Structure d'une solution et d'un projet
- `.sln` / `.csproj`
- Références entre projets (`ProjectReference`)
- Commandes principales du CLI :
  - `dotnet new`
  - `dotnet restore`
  - `dotnet build`
  - `dotnet run`
  - `dotnet test`
- NuGet
  - ajouter et supprimer un package
  - `PackageReference`
  - restauration des dépendances
  - versions et dépendances transitives

## 5. Passer au développement backend avec ASP.NET Core

- `Program.cs` et démarrage de l'application
- Pipeline HTTP
- Middleware
- Routing
- Controllers et endpoints
- Introduction aux Minimal APIs
- DTOs
- Model binding
- Validation des entrées
- Codes de statut HTTP
- Gestion globale des erreurs
- Conteneur d'injection de dépendances .NET
- Lifetimes : `Transient`, `Scoped`, `Singleton`

### Configuration et logging

- `appsettings.json`
- configuration par environnement
- variables d'environnement
- `IConfiguration`
- Options pattern : `IOptions<T>`
- gestion des secrets
- `ILogger<T>`

### Appels HTTP sortants

- `HttpClient`
- sérialisation / désérialisation JSON
- `HttpClientFactory`
- parallèle avec `fetch` / Axios côté front

## 6. Persistance des données : SQL et EF Core

### Fondamentaux SQL

- tables et colonnes
- clés primaires et étrangères
- relations
- jointures
- index
- transactions

### Entity Framework Core

- rôle d'un ORM
- `DbContext`
- `DbSet<T>`
- configuration d'EF Core avec la DI
- migrations
- CRUD
- relations et propriétés de navigation
- requêtes LINQ sur la base
- `IEnumerable<T>` vs `IQueryable<T>`
- tracking vs `AsNoTracking()`
- `SaveChangesAsync()`
- chargement des données liées
- attention aux performances et au nombre de requêtes générées

## 7. Tests, architecture et conception

### Tests

- Tests unitaires
  - Arrange / Act / Assert
  - fake / mock
  - intérêt du découplage pour les tests
- Tests d'intégration
- Différence entre test unitaire et test d'intégration

### Architecture et séparation des responsabilités

- Controller
- Service
- Repository
- Presentation / Application / Domain / Infrastructure
- Couplage et cohésion
- Où placer la logique métier
- Éviter d'ajouter des couches sans besoin réel

### Design patterns appliqués

- Strategy
- Factory
- Decorator
- Adapter
- Repository

L'objectif n'est pas de mémoriser les patterns, mais de comprendre le problème auquel chacun répond et les compromis qu'il introduit.

## 8. Projet fil rouge

Construire progressivement une petite API .NET permettant de mettre les notions en pratique plutôt que de les apprendre uniquement de manière théorique.

Exemple : API de gestion de commandes avec :

- endpoints HTTP
- Controllers
- DTOs et validation
- Services
- Repositories
- interfaces
- injection de dépendances
- configuration et logging
- LINQ
- async / await
- EF Core
- migrations
- base SQL
- gestion des erreurs
- appel éventuel à une API externe via `HttpClient`
- tests unitaires
- tests d'intégration
- séparation en couches

## Approche pédagogique

Toujours partir autant que possible d'un parallèle avec le front-end :

- `Array.filter` → `Where`
- `Array.map` → `Select`
- `Array.some` → `Any`
- `Array.every` → `All`
- `Promise<T>` → `Task<T>`
- `fetch` / Axios → `HttpClient`
- interface TypeScript → interface C#, en explicitant leurs différences
- immutabilité du state front → records et objets immuables
- configuration front par environnement → configuration ASP.NET Core

L'objectif n'est pas d'apprendre des règles ou des patterns par cœur, mais de comprendre les problèmes qu'ils permettent de résoudre et les compromis qu'ils impliquent.

## Hors périmètre initial

À garder pour une seconde étape, une fois les bases précédentes maîtrisées :

- authentification et autorisation
- JWT / Bearer tokens / claims
- Docker
- Redis / cache distribué
- messaging (`RabbitMQ`, `Kafka`, etc.)
- microservices
- CQRS / MediatR
- observabilité avancée
- Kubernetes / cloud
