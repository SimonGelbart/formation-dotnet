# Carte des notions — parcours principal et approfondissements

Cette carte indique le niveau visé dans le parcours principal et où retrouver les sujets qui méritent une seconde lecture.

| Notion | Emplacement | Niveau visé |
|---|---|---|
| SDK/runtime, CLI, débogage | [00](00-demarrer.md) | Pratiquer / comprendre |
| Types, var, null, propriétés, méthodes, decimal | [01](01-csharp.md) | Pratiquer |
| Constructeurs, encapsulation, invariants | [02](02-objets.md) | Pratiquer |
| Valeur/référence, passage des paramètres | [02](02-objets.md) | Comprendre |
| Visibilité, static, const, readonly, init | [02](02-objets.md) | Comprendre |
| Record, required, égalité | [02](02-objets.md), [03](03-collections-linq.md) | Repérer |
| Collections et génériques | [03](03-collections-linq.md) | Pratiquer |
| Lambdas, delegates, Func, Action | [03](03-collections-linq.md) | Pratiquer / comprendre |
| LINQ, IEnumerable, matérialisation | [03](03-collections-linq.md) | Pratiquer / comprendre |
| HashSet, Equals/GetHashCode, complexité | [03](03-collections-linq.md) | Repérer |
| Méthodes d'extension, Queue/Stack | [03](03-collections-linq.md) | Repérer |
| Interfaces, composition, DI par constructeur | [04](04-dependances.md) | Pratiquer |
| Héritage, abstract, override, virtual, sealed | [04](04-dependances.md) | Comprendre |
| HTTP, Controller, DTO, binding, validation | [05](05-api-http.md) | Pratiquer |
| Middleware, lifetimes, CORS | [05](05-api-http.md) | Comprendre / repérer |
| Exceptions, Task, await, cancellation | [06](06-async-erreurs.md) | Pratiquer / comprendre |
| using, ressources, WhenAll, CPU/I/O | [06](06-async-erreurs.md) | Comprendre |
| Configuration, logging, HttpClientFactory | [Annexe](annexes/client-http.md) | Comprendre sur un exemple |
| Projet, solution, NuGet, packages/outils | [07](07-persistance.md) | Pratiquer / comprendre |
| SQL, DbContext, migrations, CRUD | [07](07-persistance.md) | Pratiquer |
| IQueryable, tracking, AsNoTracking | [07](07-persistance.md) | Comprendre |
| OpenAPI, index, transactions | [07](07-persistance.md) | Comprendre / repérer |
| Relations SQL, Include, prix historique | [08](08-commandes.md) | Pratiquer |
| Collection privée, mapping, N+1 | [08](08-commandes.md) | Comprendre / repérer |
| Erreurs métier → HTTP / ProblemDetails | [08](08-commandes.md) | Comprendre sur le code fourni |
| Tests unitaires, fake, intégration | [09](09-tests.md) | Pratiquer / comprendre |
| SOLID, cohésion, couplage, responsabilités | [10](10-conception.md) | Comprendre |
| Strategy | [10](10-conception.md) | Pratiquer |
| Factory, Adapter, Decorator, couches | [10](10-conception.md) | Repérer |

## Ce qui attend une seconde lecture

Injection par propriété/méthode, égalité personnalisée complète, ref/in/out, détails runtime des nullables, global.json et politiques de SDK, Options avancées, chargement explicite/lazy détaillé, optimisation SQL et projections complexes.

La [référence d'approfondissement](../approfondissements/README.md) développe ces sujets. Il n'est pas nécessaire de la lire en parallèle à chaque chapitre.
