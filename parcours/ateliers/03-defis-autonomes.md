# Deux défis avant le bilan final

Lis les critères et essaie sans ouvrir les corrections. Déplie les indices un par un. Chaque défi doit être suivi d'une explication avec tes mots et d'un point de contrôle dans ton espace personnel.

## Défi 1 — Rechercher des produits

**Quand :** après le chapitre 5 et l'atelier HTTP. **Où :** ton MonCatalogue à l'étape service. **Temps indicatif :** 30–60 minutes.

Ajoute un paramètre facultatif `name` à GET /products. Il recherche une partie du nom sans tenir compte de la casse. Sans paramètre ou avec des espaces seuls, le catalogue complet est retourné.

Crée trois produits : Clavier, Souris et Clavier compact. Critères :

- GET /products retourne les trois ;
- GET /products?name=CLAV retourne les deux claviers ;
- GET /products?name=absent retourne 200 avec un tableau vide ;
- GET /products?name=%20%20 retourne les trois ;
- POST et GET par identifiant fonctionnent encore.

<details><summary>Indice 1 — contrat HTTP</summary>

Ajoute `[FromQuery] string? name` à l'action GET. Passe cette valeur au service. Le Controller garde le statut HTTP ; la recherche appartient au service.
</details>

<details><summary>Indice 2 — recherche</summary>

Teste `string.IsNullOrWhiteSpace(name)`. Sinon, utilise `Where` et `Contains(name.Trim(), StringComparison.OrdinalIgnoreCase)` sur la liste en mémoire. Le résultat doit être matérialisé pour correspondre au retour attendu.
</details>

<details><summary>Correction possible</summary>

Dans ProductService de l'atelier mémoire :

```csharp
public IReadOnlyList<Product> GetAll(string? name)
{
    if (string.IsNullOrWhiteSpace(name)) return _products.ToList();
    return _products
        .Where(p => p.Name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
        .ToList();
}
```

Dans le Controller :

```csharp
[HttpGet]
public IActionResult GetAll([FromQuery] string? name) => Ok(_service.GetAll(name));
```

Cette correction est pour LINQ en mémoire. La surcharge avec StringComparison n'est pas à transposer aveuglément en requête EF. Pour SQLite, choisis et vérifie une expression traduisible et ses règles de casse.
</details>

**Expliquer :** pourquoi l'absence de résultat ne correspond-elle pas à un produit individuel introuvable ? Pourquoi placer la recherche dans le service ?

## Défi 2 — Renommer un produit sans perdre son prix

**Quand :** après le chapitre 9. **Où :** une copie complète de `parcours/exemples/02-api-sqlite` dans ton espace personnel. **Temps indicatif :** 60–90 minutes.

Ajoute `PATCH /products/{id}/name`, avec un JSON `{"name":"Nouveau nom"}`. Le prix ne doit pas changer. Réutilise les règles existantes de Product ; ne crée pas de migration puisqu'il n'y a pas de nouvelle colonne.

Critères :

- produit existant, nom valide : 204 ; un nouveau GET donne le nouveau nom et l'ancien prix ;
- nom vide ou composé d'espaces : 400 ;
- identifiant valide mais absent : 404 ;
- résultat conservé après redémarrage ;
- ligne de commande déjà créée : nom et prix historiques inchangés ;
- un test HTTP couvre au minimum le succès, la relecture et l'entrée invalide ; les anciens tests passent.

<details><summary>Indice 1 — modèle</summary>

Le domaine possède déjà `Update(name, price)`. Le service peut lui transmettre le nouveau nom et le prix chargé. Le client ne doit pas fournir le prix pour cette opération.
</details>

<details><summary>Indice 2 — sauvegarde</summary>

Ajoute un DTO avec Required et une méthode RenameAsync. Charge le produit par le repository suivi, retourne false s'il manque, appelle Update puis SaveChangesAsync. L'action traduit false en 404.
</details>

<details><summary>Correction possible</summary>

Dans le namespace Catalogue, ajoute le DTO (avec `using System.ComponentModel.DataAnnotations;`) :

```csharp
public sealed class RenameProductRequest
{
    [Required]
    public string Name { get; set; } = "";
}
```

Dans ProductService :

```csharp
public async Task<bool> RenameAsync(Guid id, string name, CancellationToken ct)
{
    var product = await _repository.GetByIdAsync(id, ct);
    if (product is null) return false;
    product.Update(name, product.Price);
    await _repository.SaveChangesAsync(ct);
    return true;
}
```

Dans ProductsController :

```csharp
[HttpPatch("{id:guid}/name")]
public async Task<IActionResult> Rename(Guid id, RenameProductRequest request, CancellationToken ct)
{
    if (!await _service.RenameAsync(id, request.Name, ct)) return NotFound();
    return NoContent();
}
```

Dans ApiTests, ajoute ce test utilisant la factory existante :

```csharp
[Fact]
public async Task RenameKeepsPriceAndRejectsBlankName()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await factory.CreateDatabaseAsync();
    var created = await client.PostAsJsonAsync("/products", new { name = "Clavier", price = 30 });
    Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    var product = (await created.Content.ReadFromJsonAsync<ProductResponse>())!;
    var renamed = await client.PatchAsJsonAsync($"/products/{product.Id}/name", new { name = "Clavier compact" });
    Assert.Equal(HttpStatusCode.NoContent, renamed.StatusCode);
    var read = await client.GetFromJsonAsync<ProductResponse>($"/products/{product.Id}");
    Assert.NotNull(read);
    Assert.Equal("Clavier compact", read.Name);
    Assert.Equal(30m, read.Price);
    var invalid = await client.PatchAsJsonAsync($"/products/{product.Id}/name", new { name = " " });
    Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
}
```

Complète avec le cas 404 et le contrôle du nom historique en reprenant le scénario de commandes existant. La vérification manuelle après redémarrage complète les tests en mémoire.
</details>

**Expliquer :** pourquoi n'y a-t-il pas de migration ? Pourquoi un DTO spécifique ? Pourquoi le nom historique doit-il rester stable ?

Quand ces deux défis sont compris, passe au [bilan Description](../11-bilan.md), qui ajoute cette fois une évolution de schéma.
