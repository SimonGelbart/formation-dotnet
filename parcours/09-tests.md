# 09 — Tester un comportement

**Pratiquer :** un test métier et un fake simple. **Comprendre :** frontière unitaire/intégration. **Repérer :** préparation d'un serveur et d'une base de test.

Résultat : exécuter des tests qui détectent une régression du prix historique ou de la sauvegarde.

## Lancer un premier test

Le projet de référence est déjà configuré. Depuis `workbook_v2/exemples/02-api-sqlite` :

```bash
dotnet test Catalogue.Tests/Catalogue.Tests.csproj --filter NegativePriceIsRejected
```

Tu peux utiliser cette commande dès le chapitre 2. Aucune API à lancer et aucune base locale à créer pour ce test.

Pour créer un projet équivalent dans ton propre travail, utilise `dotnet new xunit`, ajoute une référence au projet API et compare le `.csproj` fourni. `dotnet test` compile, découvre puis exécute les tests.

## Arrange, Act, Assert

Extrait de test à lire :

```csharp
[Fact]
public void OrderKeepsOriginalPrice()
{
    // Arrange : préparer
    var product = new Product("Clavier", 30m);
    var order = new Order();
    order.AddItem(product, 2);

    // Act : faire évoluer le catalogue
    product.Update("Clavier", 50m);

    // Assert : vérifier le comportement
    Assert.Equal(60m, order.Total);
}
```

`[Fact]` désigne un test xUnit. `Assert.Equal` vérifie le résultat. Le test n'a besoin ni de serveur, ni de DI, ni de base de données. Il documente une règle.

Un test utile doit pouvoir échouer quand le comportement est cassé. Essaie temporairement de modifier le résultat attendu : le test devient rouge. Rétablis ensuite la bonne valeur.

## Tester le service avec un fake

Lis [ProductServiceTests.cs](exemples/02-api-sqlite/Catalogue.Tests/ProductServiceTests.cs). Le fake implémente le contrat du repository avec des listes en mémoire.

Il sépare les produits en attente et les produits sauvegardés. Le test vérifie qu'après création, le produit apparaît parmi les sauvegardés. Si tu retires `SaveChangesAsync` du service, ce test doit échouer.

Ce fake n'imite pas tout EF. Il simule seulement ce qui est nécessaire au test. Un mock permettrait de configurer ou vérifier des interactions via un outil ; aucune bibliothèque de mocks n'est nécessaire pour commencer.

## Tester l'intégration HTTP + base

Lis [ApiTests.cs](exemples/02-api-sqlite/Catalogue.Tests/ApiTests.cs), d'abord la méthode de test et seulement ensuite la factory.

Le scénario crée un produit et une commande, vérifie 400/404/409, ajoute une ligne, modifie le catalogue, confirme, puis relit via le header Location. Les requêtes successives obtiennent des contextes distincts : la relecture vérifie bien la persistance.

`WebApplicationFactory` démarre l'application en mémoire. SQLite utilise une connexion en mémoire dédiée au test. La base existe tant que cette connexion reste ouverte. Une nouvelle factory et une nouvelle connexion par test isolent les scénarios.

La factory remplace la configuration de la base locale : le test ne touche pas `catalogue.db`. `EnsureCreated` y crée le schéma directement ; **ce test ne valide pas les migrations**. Les migrations restent à vérifier sur une base locale via le parcours du chapitre 7.

SQLite est une base relationnelle. Le provider EF nommé `InMemory` est autre chose et ne prouve pas qu'une requête SQL fonctionne. Si le moteur réel change, certains tests devront viser ce moteur.

## Lancer tous les tests

```bash
dotnet test Catalogue.sln
```

| Test | Ce qu'il cherche à protéger |
|---|---|
| Domaine | Prix non négatif, quantité positive, statut, prix historique |
| Service | Création suivie d'une sauvegarde |
| Intégration | Contrat HTTP, DI, mapping EF, relations et persistance |

La différence entre unitaire et intégration concerne les composants réellement traversés, pas seulement la vitesse du test.

## Exercice

Ajoute un test qui vérifie qu'une deuxième confirmation est refusée.

<details>
<summary>Correction : à ajouter dans DomainTests</summary>

```csharp
[Fact]
public void ConfirmTwiceIsRejected()
{
    var order = new Order();
    order.AddItem(new Product("Clavier", 30m), 1);
    order.Confirm();

    Assert.Throws<OrderConflictException>(() => order.Confirm());
}
```

Le test vérifie un comportement observable ; il ne dépend pas du nom de la liste privée.
</details>

## Trois questions

1. Pourquoi un test de règle métier n'a-t-il pas besoin d'HTTP ?
2. Quelle erreur le fake de sauvegarde permet-il de détecter ?
3. Que vérifie la relecture HTTP que le test domaine ne vérifie pas ?

Suite : [10 — Conception](10-conception.md).
