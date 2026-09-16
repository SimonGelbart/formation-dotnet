# 05 — Exposer le catalogue en HTTP

**Pratiquer :** GET, POST, DTO et validation. **Comprendre :** routage, DI, middleware et lifetimes. **Repérer :** CORS et OpenAPI.

Résultat : créer un produit avec une requête HTTP, puis le retrouver.

## Partir d'un point de contrôle

Ouvre [l'API mémoire](exemples/01-api-memoire/README.md) et suis ses commandes. Depuis la racine du dépôt :

```bash
cd parcours/exemples/01-api-memoire
dotnet run --urls http://localhost:5080
```

Le terminal reste occupé par le serveur. Ouvre un second terminal pour les requêtes. Pour arrêter le serveur : `Ctrl+C`.

Cette référence est complète : commence par lire le GET, puis le POST. Les PUT et DELETE sont disponibles pour l'exercice ; tu peux les ignorer au premier passage.

Pour reconstruire le même projet toi-même, crée ailleurs une API avec `dotnet new webapi --use-controllers -n MonCatalogue --framework net10.0`, supprime les exemples WeatherForecast s'ils sont présents, puis ajoute les fichiers de référence un à un.

## Une requête et une réponse

Un client envoie une méthode (GET, POST…), une adresse, éventuellement des headers et un corps JSON. Le serveur retourne un statut et éventuellement un corps. ASP.NET Core fait le lien entre HTTP et tes méthodes C#.

```bash
curl -i http://localhost:5080/products
```

Sur Windows PowerShell, utilise `curl.exe` si `curl` désigne une commande PowerShell. Le fichier [requetes.http](exemples/01-api-memoire/requetes.http) est une autre option si ton éditeur prend en charge ce format.

Attendu au premier démarrage : statut `200`, corps `[]`.

Crée ensuite un produit avec le fichier HTTP ou cette commande Bash :

```bash
curl -i -X POST http://localhost:5080/products -H 'Content-Type: application/json' -d '{"name":"Clavier","price":30}'
```

Attendu : statut `201`, header `Location` et corps de la forme :

```json
{"id":"un-guid-généré","name":"Clavier","price":30}
```

Copie l'adresse de `Location` dans un GET. Tu dois retrouver le même produit. Une erreur `Connection refused` signifie généralement que l'API n'est pas lancée sur le port demandé.

## Les fichiers à lire, dans cet ordre

1. [Program.cs](exemples/01-api-memoire/Program.cs) : configure les services puis démarre l'application.
2. [ProductsController.cs](exemples/01-api-memoire/Controllers/ProductsController.cs) : traduit HTTP en appels de service.
3. [ProductService.cs](exemples/01-api-memoire/Services/ProductService.cs) : orchestre les opérations.
4. [MemoryProductRepository.cs](exemples/01-api-memoire/Data/MemoryProductRepository.cs) : stocke les objets.

`[Route("products")]` définit le préfixe. `[HttpGet("{id:guid}")]` indique une route GET avec un identifiant Guid. Le framework transforme la route ou le JSON en paramètres : c'est le model binding. Un Guid mal formé ne correspond pas à cette route contrainte.

Dans `Program.cs`, `builder.Services...` configure la construction des objets ; `app...` configure le traitement des requêtes. Un middleware peut agir avant et après l'étape suivante. `UseExceptionHandler` est un middleware de gestion des erreurs inattendues. `MapControllers` expose les actions des Controllers.

## Pourquoi des DTO ?

[ProductRequest](exemples/01-api-memoire/Contracts/ProductContracts.cs) décrit les données entrantes. `ProductResponse` décrit la réponse. Le client ne décide pas de l'identifiant du produit et ne reçoit que les champs du contrat HTTP.

`[Required]` vérifie le nom, `[Range]` le prix. Avec `[ApiController]`, une requête invalide reçoit `400` avant l'action. Essaie un prix à `-1`. La classe Product protège également ses règles pour les appels qui ne passent pas par HTTP.

| Situation | Statut dans notre API |
|---|---|
| Lecture réussie | 200 |
| Création réussie avec localisation | 201 |
| Modification ou suppression réussie | 204, sans corps |
| JSON ou entrée invalide | 400 |
| Identifiant valide mais absent | 404 |
| Échec inattendu | 500 |

## Observer les lifetimes

Le repository mémoire est **Singleton** : la même instance conserve les produits entre requêtes. Le service est **Scoped** : une instance par requête dans cette API. **Transient** créerait une nouvelle instance à chaque demande au conteneur.

Expérience : remplace temporairement l'enregistrement du repository par `AddScoped`. Redémarre, crée un produit puis liste le catalogue. La liste est vide : chaque requête obtient un nouveau stockage ! Remets `AddSingleton`.

Le stockage mémoire est destiné à des essais séquentiels locaux. Son état mutable partagé n'est pas conçu pour des clients concurrents. Avec EF, la base conservera les données et le contexte sera scoped.

## Exercice

Utilise PUT pour changer le prix, puis DELETE pour supprimer le produit. Observe le GET après suppression et le contenu d'une réponse 204.

<details>
<summary>Correction</summary>

Utilise l'identifiant retourné à la création et un JSON `{"name":"Clavier","price":40}` pour PUT. PUT puis DELETE retournent 204. Le GET suivant retourne 404. Une réponse 204 ne contient pas de JSON à désérialiser.
</details>

## Repères navigateur et contrat

CORS concerne les accès entre origines depuis un navigateur, par exemple un front sur le port 5173 et cette API sur 5080. Si tu branches un front, autorise son origine explicitement avec `AddCors` et `UseCors`. CORS ne remplace pas une authentification ; `curl` n'est pas soumis à ces règles de navigateur.

OpenAPI décrit les routes et les schémas JSON. La référence SQLite le configure au chapitre 7. Les Minimal APIs constituent une autre syntaxe d'endpoints ; apprendre les deux styles maintenant n'est pas nécessaire.

## Trois questions

1. Qui transforme le JSON en objet C# ?
2. Pourquoi le repository mémoire doit-il survivre à une requête ?
3. Comment distinguer une entrée invalide d'un produit introuvable ?

Suite : [06 — Async et erreurs](06-async-erreurs.md).
