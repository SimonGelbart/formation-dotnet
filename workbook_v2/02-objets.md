# 02 — Construire des objets cohérents

**Pratiquer :** constructeur, encapsulation, contrôle d'une modification. **Comprendre :** valeur/référence, visibilité et immutabilité. **Repérer :** `record`, `init`, `required`.

Résultat : un produit ne peut pas avoir de nom vide ou de prix négatif.

## Pourquoi changer notre classe ?

Avec un setter public, `product.Price = -30m` était autorisé. L'encapsulation consiste ici à faire passer les modifications par une méthode qui applique la règle.

Un constructeur est appelé par `new`. Il reçoit les données nécessaires pour créer un objet cohérent. Une propriété expose la donnée ; un champ est un emplacement de stockage interne. Le compilateur crée le stockage d'une propriété automatique comme `Price`.

## Programme complet — `Sandbox/Program.cs`

```csharp
var product = new Product("Clavier", 30m);
product.ChangePrice(40m);
Console.WriteLine(product.Price); // 40

try
{
    product.ChangePrice(-1m);
}
catch (ArgumentOutOfRangeException exception)
{
    Console.WriteLine(exception.ParamName); // price
}
Console.WriteLine(product.Price); // toujours 40

public class Product
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public Product(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nom obligatoire", nameof(name));
        Name = name.Trim();
        ChangePrice(price);
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));
        Price = price;
    }
}
```

`private set` permet à la classe de modifier le prix mais interdit `product.Price = ...` à l'extérieur. `throw` signale un échec ; `catch` permet ici de l'observer. Nous approfondirons les exceptions au chapitre 6.

Dans les applications de référence, la méthode s'appelle `Update(name, price)` et valide les deux valeurs avant de les affecter. L'idée reste la même.

## Copie d'une valeur, partage d'un objet

Ajoute cet extrait avant la déclaration de classe du programme :

```csharp
var first = 10;
var second = first;
second = 20;
Console.WriteLine(first); // 10

var other = product;
other.ChangePrice(50m);
Console.WriteLine(product.Price); // 50
```

Un `int` est un type valeur : l'affectation copie sa valeur. Une variable contenant une classe porte une référence : les deux variables peuvent désigner le même objet. Copier la référence ne copie pas l'objet.

Lorsqu'on passe un objet à une méthode, on copie par défaut sa référence. Modifier l'objet reste visible par l'appelant ; réaffecter le paramètre à un nouvel objet ne remplace pas la variable de l'appelant. Inutile de maîtriser `ref`, `in` et `out` maintenant.

`string` est un type référence, mais son contenu est immuable. « Référence » et « modifiable » ne veulent donc pas dire la même chose.

## Visibilité et durée de modification

| Mot-clé | Idée à retenir |
|---|---|
| `public` | Utilisable de l'extérieur |
| `private` | Réservé au type |
| `internal` | Réservé au même projet compilé dans notre exemple |
| `protected` | Accessible aussi aux classes dérivées |
| `static` | Appartient au type, pas à une instance, comme `Math.Max` |
| `const` | Valeur constante connue à la compilation |
| `readonly` | Champ assignable à la déclaration ou dans le constructeur |
| `init` | Propriété affectable à l'initialisation |

`readonly List<string>` empêche de remplacer la liste après construction ; il n'empêche pas `Add`. C'est une distinction utile, pas un détail à mémoriser isolément.

Un `record` est pratique pour transporter des données : `public record ProductLabel(string Name);`. Il fournit une égalité basée sur ses données. Deux classes ordinaires distinctes ne deviennent pas égales uniquement parce que leurs propriétés se ressemblent. Un record contenant une liste ne rend pas la liste immuable.

`required` demande l'initialisation d'un membre à la création. Il ne prouve pas qu'un nom est valide. Nous garderons les constructeurs et les classes ordinaires pour le domaine.

## Exercice

Interdis les prix supérieurs à `1000000m` dans `ChangePrice`. Vérifie qu'un échec conserve l'ancien prix. Le prix `0m` doit rester accepté.

<details>
<summary>Correction</summary>

Teste `price < 0 || price > 1000000m` avant l'affectation. L'ordre compte : on vérifie d'abord, on modifie ensuite. La borne haute est un exercice ; la référence impose cette borne au contrat HTTP et garde seulement la règle du prix non négatif dans le domaine.
</details>

## Premier test à rencontrer

Après avoir configuré le projet du [chapitre 9](09-tests.md), lance le test `NegativePriceIsRejected`. Lis ses trois parties : préparer les données, tenter l'action, vérifier l'échec. Tu n'as pas encore besoin de comprendre le serveur de test.

## Trois questions

1. Que protège le constructeur ?
2. Pourquoi une modification via `other` change-t-elle `product` ?
3. Pourquoi `readonly` ne signifie-t-il pas « objet entièrement immuable » ?

Suite : [03 — Collections et LINQ](03-collections-linq.md).
