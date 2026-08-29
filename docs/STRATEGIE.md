# MixoLogger — Cadrage stratégique

> Positionnement, modèle économique visé, séquencement.
> Document compagnon de [MVP.md](MVP.md), qui reste la référence technique.
>
> Dernière mise à jour : 2026-08-29

---

## 1. Objectif long terme retenu

**Monétisation par la donnée agrégée**, vendue aux acteurs du secteur de la boisson
(marques de spiritueux, distributeurs, sirops).

La valeur n'est pas l'espace publicitaire, c'est la réponse à une question qu'aucune marque
ne sait traiter aujourd'hui : *qu'est-ce que les gens ont réellement chez eux, et avec quoi
le mélangent-ils ?* Les panels existants mesurent l'acte d'achat ; « Mon Bar » mesurerait la
**possession et l'usage**, ce qui est structurellement différent et rare.

### Précisions indispensables

| Point | Formulation exacte |
|-------|--------------------|
| **Ce qui se vend** | Des **statistiques agrégées et anonymisées** (« la marque X est présente dans N % des bars, co-détenue avec Y, utilisée dans tel type de recette »). |
| **Ce qui ne se vend pas** | La donnée personnelle brute ou ré-identifiable. Juridiquement risqué, réputationnellement fatal pour une app de niche qui vit du bouche-à-oreille. |
| **Prérequis absolu** | **Une audience.** Sans volume, la donnée n'a aucune valeur statistique : personne n'achète un panel de 200 personnes. |
| **Horizon** | Très long terme. Ce n'est pas un plan exécutable, c'est une direction qui sert à trancher les arbitrages d'aujourd'hui. |

---

## 2. Pourquoi l'approche « faire publier les marques et les barmans » a été écartée pour l'instant

L'idée initiale — convaincre des barmans professionnels et des entreprises de publier leurs
recettes pour faire de la pub et fidéliser — n'est pas mauvaise, elle est **mal séquencée** :
c'est une stratégie de phase 3 envisagée en phase 0.

1. **Inversion du rapport de force.** Une marque publie là où il y a une audience. Avec
   5 utilisateurs, on demande une faveur ; on n'offre pas un canal. Le temps d'un chef de bar
   est la ressource la plus rare du secteur : un contact grillé sur un produit non fonctionnel
   ne se rattrape pas.
2. **Les marques ont déjà mieux.** Diageo Bar Academy, Campari Academy, Pernod Ricard, Monin,
   Giffard : portails de recettes, programmes trade, et surtout une distribution
   Instagram / TikTok qu'une application ne peut pas concurrencer.
3. **Conflit avec la proposition de valeur.** « Que puis-je faire avec ce que j'ai ? » dit
   *n'achète rien*. Une marque veut dire *achète cette bouteille*. Transformer la bibliothèque
   en catalogue sponsorisé détruirait précisément ce qui différencie MixoLogger des milliers
   d'apps de recettes existantes.
4. **Loi Évin** — cf. §4.1.

> **À retenir** : l'idée revient naturellement plus tard, mais dans l'autre sens. Ce n'est pas
> « publiez chez nous gratuitement », c'est « voici ce que nos utilisateurs possèdent et
> mélangent réellement ». Le second se vend ; le premier se quémande.

---

## 3. Positionnement retenu

**Grand public d'abord, gestion de bar comme cœur du produit.**

Une bibliothèque de recettes est une commodité : il en existe des milliers, gratuites et mieux
fournies. Ce qui retient un utilisateur, c'est **son état personnel** : son stock, ses recettes,
ses notes. C'est aussi, et ce n'est pas un hasard, exactement la donnée qui a de la valeur en §1.

D'où la priorité produit : **faire fonctionner « Mon Bar » pour un utilisateur lambda**
(F3 + F4 du MVP), avant tout le reste.

### Piste B2B conservée en réserve

Un bar professionnel a un problème quotidien de stock, de coût matière par cocktail et
d'ingénierie de carte — avec un budget en face. Le domaine actuel (`Bar`, `Volume`,
`VolumeConverter`, `MakeCocktail`) en est déjà l'ossature. Cette piste :

- se vend beaucoup plus facilement que du contenu gratuit — on apporte de la valeur en premier ;
- financerait le développement bien avant que la donnée agrégée n'ait du volume ;
- ne se lance qu'après validation par entretiens (§5), pas sur intuition.

---

## 4. Contraintes juridiques à intégrer dès maintenant

### 4.1 Loi Évin — publicité pour l'alcool

Code de la santé publique, art. L.3323-2 et suivants. Les services de communication en ligne
sont un support **autorisé** (depuis 2009), mais sous conditions strictes :

- Contenu publicitaire limité aux **éléments objectifs** : degré volumique, origine,
  dénomination, composition, mode d'élaboration et de consommation, caractéristiques
  olfactives et gustatives.
- **Message sanitaire obligatoire** sur toute publicité.
- Exclusion des supports « principalement destinés à la jeunesse ».
- **Parrainage interdit.**

Conséquences concrètes : classification 18+ sur les stores, et surtout **tout contrat
publicitaire ou de contenu de marque doit être validé par un juriste avant signature**, pas
après. Le contenu éditorial et œnotouristique n'est pas de la publicité (précision apportée par
la loi Santé de 2016), ce qui laisse de la marge — mais la frontière est à cadrer.

### 4.2 RGPD — la contrainte qui tombe maintenant, pas plus tard

Le MVP note « pas de contrainte RGPD formelle (usage privé, entre proches) ». **Cette phrase
devient fausse à la seconde où l'application s'ouvre au public**, et plus encore si la donnée
est destinée à être valorisée.

Le point critique : **on ne régularise pas rétroactivement une collecte.** Une donnée collectée
sans base légale ni information claire n'est pas exploitable plus tard, même agrégée. À prévoir
donc **dès la conception du modèle multi-utilisateur** (lot C du MVP) :

- base légale et information claire de l'utilisateur sur l'usage statistique ;
- consentement distinct pour cet usage, refusable sans dégrader le service ;
- minimisation : ne pas collecter ce dont on n'a pas besoin ;
- export et suppression du compte et de ses données.

Le coût est faible si c'est prévu dès le modèle de données. Il est prohibitif s'il faut le
rétro-adapter sur une base existante.

### 4.3 Contenu contributif

- Une recette est **très faiblement protégeable** en droit d'auteur : une liste d'ingrédients
  ne l'est pas ; le texte rédigé et les photos le sont. À cadrer dans les CGU (licence concédée
  par le contributeur) dès l'ouverture de F5.
- La modération d'un contenu alcool ouvert au public engage la responsabilité de l'éditeur.

---

## 5. Séquencement

| Phase | Contenu | Sortie attendue |
|-------|---------|-----------------|
| **0 — maintenant, sans écrire de code** | Entretiens avec 5 à 10 barmans. Pas de pitch, pas de démo : questions ouvertes sur la gestion de stock, les coûts, la façon dont ils diffusent leurs recettes. | Valide ou invalide la piste B2B (§3) pour 0 €, avant de construire dessus. |
| **1 — priorité produit** | Lots A et B du MVP, puis **« Mon Bar » réellement utilisable** : stock modifiable, référentiel d'ingrédients, `CanMake` fonctionnel. | Une application qu'on peut montrer. |
| **2 — audience** | Ouvrir au-delà des 5 proches. Objectif de traction honnête : **50 utilisateurs qui ouvrent l'app chaque semaine**. | Le seul actif qui rende les phases suivantes possibles. |
| **3 — monétisation** | Selon ce que la phase 0 a validé : outil B2B payant, ou donnée agrégée. | — |

Aucune phase ne saute son tour. En particulier : **aucun contact marque avant la phase 2.**

---

## 6. Conséquences sur les décisions ouvertes du MVP

Ce cadrage tranche plusieurs points laissés ouverts en [MVP.md](MVP.md) §6.1 :

| Question MVP | Réponse induite par la stratégie |
|--------------|----------------------------------|
| **§6.1-2 — un bar par personne ou un commun ?** | **Un par personne.** C'est le cœur du produit *et* l'unité de la donnée valorisable. Implique `Bar.OwnerId` et l'identification de l'appelant. |
| **§6.1-3 — recettes communes ?** | **Bibliothèque partagée**, édition réservée à l'auteur. Une bibliothèque privée par utilisateur tue l'intérêt de la découverte. |
| **§6.1-4 — notes par utilisateur ?** | **Oui** : moyenne affichée + note personnelle. Le signal d'usage individuel fait partie de la valeur. |
| **§6.1-6 — quantités non volumiques ?** | **Oui, indispensable.** Et plus largement, un référentiel d'ingrédients structuré — cf. §7. |

---

## 7. Le référentiel d'ingrédients est l'actif stratégique

Point le plus important de ce document sur le plan technique.

Aujourd'hui un ingrédient est une chaîne libre créée à la volée (`new Ingredient("Rhum")`).
Or « Rhum », « Rhum blanc », « Rhum ambré » et « Havana Club 3 ans » désignent des choses
différentes à des niveaux différents. Une recette dit *rhum blanc* ; l'utilisateur possède
*Bacardi Carta Blanca*. Sans hiérarchie **catégorie → type → produit de marque**, la
fonctionnalité cœur (F4, « qu'est-ce que je peux faire ? ») ne matchera jamais dans la vraie vie.

Et c'est **le même objet** qui rend la donnée vendable : une possession rattachée à un produit
identifié vaut quelque chose ; une chaîne de caractères libre ne vaut rien.

> Construire ce référentiel proprement est le meilleur investissement du projet : c'est à la fois
> le correctif du bug B1 et la fondation du modèle économique.
