# Cours HTML interactif — hors ligne

Cette interface transforme les fichiers Markdown du dépôt en un cours HTML navigable. Elle ne dépend plus de GitHub, d'un CDN ou d'une connexion Internet pendant l'utilisation.

## Démarrer sous Windows

Depuis le dépôt cloné/copier localement :

```bat
course\start.cmd
```

Le navigateur s'ouvre sur :

```text
http://127.0.0.1:8765/course/index.html
```

## Démarrer sous macOS / Linux

```bash
sh course/start.sh
```

Le serveur local est écrit en .NET et n'utilise aucun package NuGet externe. Il sert uniquement les fichiers présents dans la copie locale du dépôt.

## Pourquoi utiliser le serveur local ?

Le cours lit directement les fichiers `.md` locaux afin qu'ils restent la source de vérité. Les navigateurs interdisent généralement à une page ouverte en `file://` de lire librement les fichiers voisins. Le petit serveur local contourne uniquement cette restriction du navigateur : **aucune requête Internet n'est effectuée**.

## Fonctionnement

- liste des chapitres définie localement, sans appel à l'API GitHub ;
- lecture des `.md` depuis la copie locale du dépôt ;
- moteur Markdown embarqué directement dans `course/index.html` ;
- aucune police Google et aucun script CDN ;
- classement en parcours principal, ateliers, annexes, approfondissements et documentation ;
- navigation précédent/suivant et raccourcis clavier ← / → ;
- liens entre fichiers Markdown transformés en navigation interne lorsque le chapitre existe ;
- recherche dans le sommaire ;
- progression sauvegardée dans `localStorage` ;
- copie des blocs de code ;
- accès à la source Markdown locale ;
- mise en page responsive et impression d'un chapitre.

## Prérequis

Le SDK .NET 10, déjà utilisé par la formation, suffit pour lancer le serveur local. Aucun téléchargement supplémentaire n'est nécessaire.
