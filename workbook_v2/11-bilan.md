# Bilan — Réaliser une évolution seul

Le but est d'utiliser les notions ensemble. Tu peux consulter le workbook et la documentation : travailler de mémoire n'est pas le critère.

## Défi principal : description d'un produit

Ajoute une description facultative au produit, persistée et renvoyée par l'API.

Critères :

1. L'ancien POST sans description fonctionne encore.
2. POST et PUT acceptent une description ; GET la renvoie.
3. La donnée survit à un redémarrage.
4. Un test vérifie cette évolution.
5. Les tests déjà présents restent verts.

Travaille dans une copie de l'API SQLite ou dans ton propre projet. Ajoute une nouvelle migration ; ne supprime pas la première pour masquer l'évolution.

<details>
<summary>Pistes de correction</summary>

Ajouter `string? Description` au modèle et au contrat entrant. Étendre le constructeur et `Update`, puis transmettre la valeur depuis Controller et service. Ajouter le champ au DTO de réponse et à son mapping. Générer `AddProductDescription`, inspecter la colonne nullable, appliquer la migration, puis faire POST → GET → redémarrage → GET. Adapter les appels existants ou fournir un paramètre optionnel cohérent. Vérifier la valeur dans un test HTTP.
</details>

## Défi complémentaire : un problème de conception

Un fournisseur externe propose un autre catalogue. Explique où placer son client HTTP et quel contrat il pourrait implémenter. Identifie ce qui doit rester dans le domaine et ce qui dépend du fournisseur. Tu n'as pas besoin de brancher un vrai service pour justifier ce choix.

## Se situer

| Niveau | Je sais… |
|---|---|
| Pratiquer | Créer un objet, manipuler une liste, lire un Controller, modifier un endpoint, sauvegarder et tester une règle |
| Comprendre | Expliquer l'encapsulation, l'interface, la DI, await, le tracking et une relation SQL |
| Repérer | Identifier un middleware, une requête différée, un problème N+1 ou un pattern utile |

Si tu bloques sur une syntaxe, cherche-la. Si tu bloques sur le trajet d'une donnée, reprends un seul appel depuis HTTP jusqu'à la base avec le débogueur.

## Après ce parcours

Choisis une suite selon le besoin du projet : authentification/autorisation, tests avec le moteur de production, déploiement, pagination et performances, appels externes robustes. CQRS, messaging et microservices ne sont pas une étape obligatoire après une première API.
