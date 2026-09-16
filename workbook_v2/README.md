# Workbook v2 — Apprendre .NET en construisant une API

Tu connais déjà les variables, les fonctions et les collections en JavaScript ou TypeScript. Tu découvres C#, le backend ou .NET : ce parcours est pour toi. Il ne suppose pas de connaissance de SQL.

**Objectif : comprendre, modifier et tester une petite API de catalogue et de commandes.** On conserve les notions importantes, mais on ne demande pas de toutes les maîtriser au même niveau.

## Trois niveaux d'apprentissage

- **Pratiquer** : refaire avec peu d'aide, en consultant la syntaxe si nécessaire.
- **Comprendre** : expliquer l'idée et modifier un exemple guidé.
- **Repérer** : reconnaître la notion et retrouver sa documentation. Ce n'est pas un prérequis pour continuer.

Une approximation utile est autorisée : « une interface décrit ce qu'un objet sait faire » suffit au début. On précise une nuance lorsqu'elle aide à comprendre un comportement réel.

## Parcours

| Étape | Résultat visible | Notions principales |
|---|---|---|
| [00 — Démarrer](00-demarrer.md) | Un programme qui s'exécute | SDK, projet, terminal, débogueur |
| [01 — Lire et écrire du C#](01-csharp.md) | Un produit créé et affiché | Types, classes, propriétés, méthodes, null |
| [02 — Des objets cohérents](02-objets.md) | Un prix invalide refusé | Constructeurs, encapsulation, références, immutabilité |
| [03 — Manipuler un catalogue](03-collections-linq.md) | Une liste filtrée et transformée | Collections, génériques, lambdas, LINQ |
| [04 — Faire collaborer les objets](04-dependances.md) | Un catalogue remplaçable | Interfaces, composition, DI, polymorphisme |
| [05 — Exposer une API](05-api-http.md) | Des GET et POST fonctionnels | HTTP, Controllers, DTO, validation, lifetimes |
| [06 — Attendre et gérer les erreurs](06-async-erreurs.md) | Un appel HTTP asynchrone | Exceptions, Task, await, HttpClient, logs |
| [07 — Sauvegarder les produits](07-persistance.md) | Des données après redémarrage | SQL, EF Core, migrations, tracking |
| [08 — Ajouter les commandes](08-commandes.md) | Une commande confirmée | Relations, règles métier, historique, erreurs HTTP |
| [09 — Tester](09-tests.md) | Des tests métier, service et HTTP | xUnit, fake, intégration |
| [10 — Faire évoluer la conception](10-conception.md) | Deux calculs de livraison | Responsabilités, SOLID, Strategy, architecture |
| [Bilan et exercice autonome](11-bilan.md) | Une évolution menée seul | Modification de bout en bout |

La [carte des notions](notions.md) explique où retrouver les sujets du premier workbook.

## Comment travailler

Pour chaque chapitre : lis l'objectif, prédis ce que fera le code, exécute-le, change une chose, puis fais l'exercice avant de lire la correction. Trois questions terminent le chapitre. Tu peux les expliquer avec tes mots : réciter une définition n'est pas demandé.

Les blocs **Programme complet** remplacent tout `Sandbox/Program.cs`. Les blocs **Extrait** servent à lire une idée ou à modifier le fichier indiqué ; ils ne sont pas des programmes autonomes. Les types se placent après le code exécuté dans les exemples console. Ne colle pas les exemples successifs les uns sous les autres.

## Environnement et applications de référence

Référence : **SDK .NET 10**, ASP.NET Core, EF Core 10, SQLite et xUnit. Un IDE C# et un terminal suffisent. Les versions de packages des exemples sont fixées pour donner un point de départ commun ; les mises à jour se font ensemble et se valident par build et tests.

Deux applications indépendantes sont disponibles :

- [API en mémoire](exemples/01-api-memoire/README.md) : état de référence à la fin du chapitre 5.
- [API SQLite et commandes](exemples/02-api-sqlite/README.md) : état de référence des chapitres 7 à 9, avec tests.

Elles portent volontairement des noms de types identiques. **Ne les assemble pas dans la même application.** Arrête la première avant de lancer la seconde sur le même port. Les exercices console restent dans un troisième projet `Sandbox`.

Tu peux construire progressivement ton propre projet et comparer, ou copier le point de contrôle pour reprendre après un blocage. Le code de référence est une correction à lire par petites étapes, pas du code à mémoriser.

## Ce qui reste volontairement simple

Une seule API, une seule base, un projet de tests. Le repository de produits sert à apprendre l'interface et le remplacement mémoire/EF. Le service de commandes utilise directement EF pour montrer qu'une interface devant chaque classe n'est pas obligatoire.

Les règles métier sont réelles, mais le modèle est réduit : pas de clients, paiement, stock, TVA réelle ou expédition. L'API est un exercice local sans authentification. Docker, sécurité applicative, microservices et messaging viendront dans une autre formation.

## Réussir le parcours

Tu sais suivre le trajet JSON → Controller → service → base → réponse, protéger une règle simple, expliquer pourquoi un collaborateur est injecté, sauvegarder une modification et écrire un test utile. Tu sais aussi demander une précision lorsque le code ne se comporte pas comme prévu.
