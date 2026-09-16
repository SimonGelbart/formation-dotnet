# 04 — Faire collaborer les objets

**Pratiquer :** interface et injection par constructeur. **Comprendre :** composition et polymorphisme. **Repérer :** héritage, classe abstraite et SOLID.

Résultat : changer l'origine du catalogue sans modifier le service qui l'utilise.

## Une dépendance est un collaborateur

Un service qui consulte un catalogue a besoin de ce catalogue. Il peut le construire lui-même, ou le recevoir. Recevoir le collaborateur permet de choisir son implémentation à l'extérieur : c'est l'injection de dépendances.

## Programme complet — `Sandbox/Program.cs`

```csharp
IProductCatalog catalog = new MemoryCatalog();
var service = new CatalogService(catalog);
Console.WriteLine(service.Count()); // 2

var emptyService = new CatalogService(new EmptyCatalog());
Console.WriteLine(emptyService.Count()); // 0

public record Product(string Name);

public interface IProductCatalog
{
    IReadOnlyList<Product> GetAll();
}

public class MemoryCatalog : IProductCatalog
{
    public IReadOnlyList<Product> GetAll()
        => new List<Product> { new("Clavier"), new("Souris") };
}

public class EmptyCatalog : IProductCatalog
{
    public IReadOnlyList<Product> GetAll() => new List<Product>();
}

public class CatalogService
{
    private readonly IProductCatalog _catalog;

    public CatalogService(IProductCatalog catalog)
    {
        _catalog = catalog;
    }

    public int Count() => _catalog.GetAll().Count;
}
```

L'interface décrit les opérations attendues. Les deux classes promettent de les fournir en écrivant `: IProductCatalog`. En TypeScript, un objet ayant la bonne forme peut suffire ; en C#, cette relation doit être déclarée.

Le service reçoit un contrat, mais l'objet réel peut être `MemoryCatalog` ou `EmptyCatalog`. Le même appel produit le comportement de l'implémentation choisie : c'est du polymorphisme.

`IReadOnlyList` permet ici de lire les éléments sans exposer des méthodes d'ajout par ce contrat. Il ne garantit pas une immutabilité profonde.

## Injection et conteneur

Nous venons d'utiliser la DI sans framework. ASP.NET Core automatisera la construction grâce à son conteneur. Les objets restent construits quelque part : `new` n'est pas interdit. Créer une donnée comme `new Product(...)` est normal.

Dans l'API du chapitre suivant, le contrat devient `IProductRepository` car on ajoute aussi des opérations d'écriture. Le mot repository désigne ici le collaborateur chargé de retrouver et stocker les produits. Ce choix rend le remplacement du stockage visible ; il ne constitue pas une obligation universelle.

## Composition et héritage

Le service **utilise** un catalogue : composition. L'héritage exprime plutôt qu'un type **est une sorte de** type de base.

Programme complet de découverte, à essayer séparément :

```csharp
Message message = new WelcomeMessage();
Console.WriteLine(message.Text()); // Bonjour

public abstract class Message
{
    public abstract string Text();
}

public sealed class WelcomeMessage : Message
{
    public override string Text() => "Bonjour";
}
```

`abstract` empêche de créer directement `Message` et impose ici l'implémentation de `Text`. `override` fournit cette implémentation. `sealed` interdit d'hériter de `WelcomeMessage`. Une méthode `virtual` aurait déjà un comportement par défaut que l'on pourrait remplacer.

Une interface suffit lorsqu'on veut surtout partager un contrat. Une classe de base peut être utile quand les types partagent réellement du comportement ou de l'état. Pour nos services, la composition suffit.

## Exercice

Dans le premier programme, ajoute `SingleProductCatalog` qui retourne un seul produit. Branche-le sans modifier `CatalogService`.

<details>
<summary>Correction</summary>

```csharp
public class SingleProductCatalog : IProductCatalog
{
    public IReadOnlyList<Product> GetAll()
        => new List<Product> { new("Écran") };
}
```

Remplace uniquement l'instanciation du catalogue par `new SingleProductCatalog()`. Le résultat vaut `1`.
</details>

## Trois questions

1. Qui choisit l'implémentation utilisée par le service ?
2. Pourquoi une interface ne signifie-t-elle pas « classe sans code » ?
3. Quelle différence entre utiliser un collaborateur et hériter d'une classe ?

Suite : [05 — API HTTP](05-api-http.md).
