# 10 — Faire évoluer la conception

**Pratiquer :** justifier une séparation et une stratégie interchangeable. **Comprendre :** responsabilités, couplage et SOLID. **Repérer :** Factory, Adapter, Decorator et architecture en projets.

Résultat : choisir deux modes de livraison sans accumuler des conditions dans le service de commandes.

## Relire l'application existante

Le Controller connaît HTTP. Le service orchestre un cas d'usage. Product et Order protègent des règles. Le repository de produits et le contexte EF assurent la persistance.

Une classe cohésive regroupe ce qui appartient à la même responsabilité. Le couplage décrit les dépendances entre composants. Il ne peut pas être nul : le service doit bien appeler ses collaborateurs. On cherche des dépendances compréhensibles et justifiées.

Le repository de produits a permis de pratiquer un contrat puis de remplacer la mémoire. Le service de commandes utilise directement EF pour éviter de répéter une couche sans nouveau besoin. Dans un projet réel, l'équipe peut choisir une convention uniforme ; ici les deux solutions servent à comprendre le compromis.

## SOLID à partir de questions

| Question | Principe associé |
|---|---|
| Cette classe mélange-t-elle plusieurs raisons de changer ? | SRP : responsabilité unique |
| Puis-je ajouter un comportement sans modifier tous les consommateurs ? | OCP : extension |
| Puis-je remplacer une implémentation sans casser les attentes du contrat ? | LSP : substitution |
| Le consommateur dépend-il d'opérations dont il n'a pas besoin ? | ISP : interfaces cohérentes |
| Mes règles importantes dépendent-elles inutilement d'un détail technique ? | DIP : inversion des dépendances |

On pratique surtout SRP et DIP. Pas besoin de connaître les sigles par cœur. Ajouter une interface devant chaque classe n'applique pas automatiquement ces principes.

## Programme complet — Strategy dans `Sandbox/Program.cs`

```csharp
IShippingStrategy strategy = new StandardShipping();
var checkout = new Checkout(strategy);
Console.WriteLine(checkout.Total(100m)); // 105

var express = new Checkout(new ExpressShipping());
Console.WriteLine(express.Total(100m)); // 115

public interface IShippingStrategy
{
    decimal Calculate(decimal subtotal);
}

public class StandardShipping : IShippingStrategy
{
    public decimal Calculate(decimal subtotal) => 5m;
}

public class ExpressShipping : IShippingStrategy
{
    public decimal Calculate(decimal subtotal) => 15m;
}

public class Checkout
{
    private readonly IShippingStrategy _shipping;
    public Checkout(IShippingStrategy shipping) => _shipping = shipping;
    public decimal Total(decimal subtotal) => subtotal + _shipping.Calculate(subtotal);
}
```

Strategy est le nom donné à ce remplacement d'algorithme par un contrat commun. Tu connaissais déjà les interfaces et le polymorphisme : le pattern leur donne ici un usage précis.

## Exercice

Ajoute `FreeAboveThresholdShipping` : livraison gratuite à partir de 100 euros, sinon 5 euros. Ne modifie pas Checkout. Teste 99 puis 100.

<details>
<summary>Correction</summary>

```csharp
public class FreeAboveThresholdShipping : IShippingStrategy
{
    public decimal Calculate(decimal subtotal) => subtotal >= 100m ? 0m : 5m;
}
```

Les totaux attendus sont 104 et 100. Crée une instance de Checkout avec cette stratégie pour les observer.
</details>

## Reconnaître trois autres patterns

- **Factory** : choisir quel objet construire. Si une option utilisateur choisit standard ou express, une petite fonction avec un `switch` peut effectuer ce choix. Une classe Factory devient utile quand cette décision a assez de contenu pour être isolée.
- **Adapter** : traduire le contrat d'un outil externe vers celui attendu par l'application. Par exemple, un fournisseur expose `SendMessage` alors que l'application attend `NotifyOrder`.
- **Decorator** : envelopper un collaborateur de même contrat pour ajouter un comportement. Par exemple, écrire un log puis appeler le notifier intérieur.

Exercice oral : associe « choix de construction », « traduction de contrat », « ajout de comportement autour » à ces trois noms. Réponse : Factory, Adapter, Decorator. Leur implémentation complète peut attendre un besoin réel.

## Faut-il plusieurs projets ?

L'application reste lisible avec un projet API et un projet de tests. Si elle grossit, des projets distincts peuvent rendre les dépendances plus explicites :

| Projet possible | Responsabilité |
|---|---|
| Api | HTTP et composition de l'application |
| Application | Cas d'usage et contrats nécessaires |
| Domain | Objets et règles métier |
| Infrastructure | EF, fichiers, clients externes |

Le domaine ne devrait alors pas avoir besoin d'ASP.NET Core ou d'EF pour exprimer ses règles. L'application peut dépendre du domaine, l'infrastructure implémenter ses contrats, et l'API assembler le tout. Créer les dossiers sans contrôler les références ne suffit pas.

Avant une extraction, complète : « Je déplace ___ vers ___ parce que ___ ». Si le bénéfice n'est pas clair, garde le code simple.

## Trois questions

1. Quel problème Strategy résout-il dans l'exercice ?
2. Pourquoi une interface par classe ne garantit-elle pas une bonne conception ?
3. Quel problème concret justifierait un projet Domain séparé ?

Suite : [Bilan](11-bilan.md).
