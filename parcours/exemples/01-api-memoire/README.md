# Point de contrôle 1 — API mémoire

Correction exécutable du [chapitre 5](../../05-api-http.md). SDK .NET 10 requis.

Depuis la racine du dépôt :

```bash
cd parcours/exemples/01-api-memoire
dotnet build
dotnet run --urls http://localhost:5080
```

Dans un second terminal : `curl -i http://localhost:5080/products`. Attendu : 200 et `[]`. Ouvre ensuite [requetes.http](requetes.http) dans un client HTTP compatible ou utilise les commandes du chapitre 5.

Le premier POST retourne un id et Location. Remplace la variable `productId` du fichier HTTP par cet id. Les zéros sont seulement un emplacement à remplacer.

À lire : Program → Controller → service → repository → Product. Les fichiers sont classés par dossier, mais partagent le namespace `Catalogue` pour limiter le bruit syntaxique.

Les données disparaissent à l'arrêt. Le singleton garde un dictionnaire pour des essais locaux séquentiels ; ce n'est pas un stockage de production concurrent. Arrête cette API avant de lancer la référence SQLite sur le même port.
