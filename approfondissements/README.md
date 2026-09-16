# Approfondissements .NET

Ce dossier complète le **parcours principal**, qui reste la formation à suivre dans l'ordre.

Commence par :

> [`parcours/README.md`](../parcours/README.md)

Le contenu présent ici n'est pas conçu comme une seconde formation linéaire. Utilise-le lorsqu'une notion rencontrée dans le parcours mérite une explication plus profonde, une nuance, un piège ou une décision de conception.

## Comment utiliser cette référence

Trois niveaux sont utilisés dans les chapitres :

- **À approfondir** : utile dans le travail quotidien et mérite d'être compris ;
- **Nuance** : évite un mauvais modèle mental ;
- **Référence** : utile ponctuellement, sans nécessité de mémorisation.

Tu peux donc ouvrir directement le chapitre qui répond à ta question, sans lire les précédents.

## Carte des approfondissements

| Je veux comprendre… | Chapitre de référence |
|---|---|
| où exécuter un exemple et comment déboguer | [0 — Bac à sable](00-demarrage.md) |
| le système de types C#, références, nullable, `readonly`, `record` | [1 — TypeScript vers C#](01-typescript-vers-csharp.md) |
| invariants, collections encapsulées, héritage, égalité, `static` | [2 — Modélisation objet](02-modelisation-objet.md) |
| DI vs conteneur, formes d'injection, lifetimes, dépendances captives | [3 — Abstraction et dépendances](03-abstraction-et-dependances.md) |
| complexité, hachage, exécution différée et ré-énumération LINQ | [4 — Collections et LINQ](04-collections-et-linq.md) |
| exceptions, ressources, I/O vs CPU, concurrence async et cancellation | [5 — Exceptions et async](05-exceptions-et-async.md) |
| SDK/runtime, `.csproj`, `.slnx`, `global.json`, NuGet et outils | [6 — Écosystème .NET](06-ecosysteme-dotnet-et-nuget.md) |
| ce qui se passe réellement pendant une requête ASP.NET Core | [7 — ASP.NET Core](07-aspnet-core.md) |
| comment LINQ devient du SQL, tracking, mapping, chargements et N+1 | [8 — SQL et EF Core](08-sql-et-ef-core.md) |
| quelle frontière tester et pourquoi | [9 — Tests](09-tests.md) |
| quand introduire une couche ou un design pattern | [10 — Architecture et patterns](10-architecture-patterns.md) |
| les décisions de conception derrière l'Order API | [11 — Étude de cas Order API](11-projet-fil-rouge.md) |

## Relation avec le parcours principal

Le parcours principal choisit volontairement un niveau différent selon les sujets : **Pratiquer**, **Comprendre** ou **Repérer**. Cette référence sert surtout à approfondir ce qui a été classé « Comprendre » ou « Repérer ».

Exemples :

```text
parcours : utiliser une interface et l'injection par constructeur
ici     : comprendre DI vs conteneur, method/property injection et captive dependency

parcours : filtrer avec LINQ
ici     : comprendre deferred execution, ré-énumération et coût des collections

parcours : persister avec EF Core
ici     : comprendre IQueryable, change tracker, backing fields, loading et SQL généré
```

## Méthode de lecture

Pour un sujet qui mérite une expérience :

```text
1. prédire
2. exécuter
3. observer
4. expliquer
5. modifier
6. comparer
```

Mais contrairement au parcours principal, tous les exemples de ce dossier ne constituent pas un parcours à reproduire de bout en bout.

## Référence technique

```text
.NET 10
ASP.NET Core
Entity Framework Core
xUnit
SQLite pour les exemples locaux
```

L'objectif est de comprendre des fondamentaux durables, pas d'apprendre une architecture ou une bibliothèque par cœur.

## Ce que cette référence ne cherche pas à faire

Elle ne doit pas :

- dupliquer les applications guidées de `parcours/` ;
- imposer une interface ou un repository partout ;
- ajouter des patterns simplement parce qu'ils existent ;
- transformer tous les détails du runtime ou d'EF Core en prérequis pour continuer la formation.

## Hors périmètre initial

```text
authentification / autorisation
Docker
cache distribué
messaging
observabilité avancée
CQRS / MediatR
microservices
Kubernetes / cloud
```

Ces sujets pourront venir ensuite selon les besoins du projet.
