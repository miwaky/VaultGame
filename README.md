# 🏚️ Shelter Command
## Game Design Document

---

# 📌 Overview

| Item | Description |
|-----|-------------|
| Genre | Gestion / Simulation / Narratif |
| Perspective | FPS (bureau de surveillance) |
| Plateforme | PC |
| Moteur | Unity |
| Style graphique | Low Poly PSX |
| Durée d'une partie | Variable (survie continue) |

---

# 🎮 High Concept

**Shelter Command** est un jeu de **gestion d’abri post‑apocalyptique en vue FPS**.

Le joueur incarne le **superviseur d’un bunker souterrain** chargé de gérer un petit groupe de survivants.

Depuis un bureau équipé d’ordinateurs et de caméras de surveillance, le joueur doit :

- surveiller les habitants
- organiser leur travail
- gérer les ressources
- préparer des explorations
- répondre aux communications radio des explorateurs

Le monde extérieur est dangereux, et chaque décision peut avoir des conséquences importantes.

---

# 🎯 Pillars (Piliers du jeu)

## 1️⃣ Surveillance

Le joueur observe la vie du bunker via un système de **caméras orientables**.

## 2️⃣ Gestion humaine

Chaque survivant possède :

- des statistiques
- une personnalité
- un comportement

## 3️⃣ Pression de survie

Les ressources critiques sont :

- nourriture
- eau

Le joueur doit équilibrer production et consommation.

## 4️⃣ Narration émergente

Les explorateurs déclenchent des **événements radio narratifs interactifs**.

---

# 🔁 Core Gameplay Loop

Configurer les tâches
↓
Observer les habitants via caméras
↓
Produire ressources
↓
Transporter les objets
↓
Stocker les ressources
↓
Préparer explorations
↓
Gérer événements radio
↓
Fin de journée

---

# 🕒 Cycle Journalier

Chaque journée est divisée en trois phases.

| Phase | Heure | Description |
|------|------|-------------|
| PréTravail | 06:00 → 07:00 | Planification |
| Travail | 07:00 → 19:00 | Production |
| PostTravail | 19:00 → 00:00 | Repos et événements |

---

# 👥 Population

Population initiale :

5 travailleurs
2 non travailleurs
Total : 7 habitants


Les non travailleurs peuvent être :

- blessés
- enfants
- personnes âgées

---

# 🧠 PNJ

## Stats

Chaque PNJ possède un pool de : 100 à 150 points

| Stat | Description |
|-----|-------------|
| Force | Capacité physique |
| Intelligence | Analyse |
| Technique | Réparation |
| Social | Relations |
| Endurance | Résistance |

---

## Traits

Chaque PNJ possède :
1 trait positif
1 trait négatif


### Traits positifs

- Travailleur
- Courageux
- Calme
- Solidaire
- Ingénieux

### Traits négatifs

- Peureux
- Paresseux
- Belliqueux
- Paranoïaque
- Égoïste

---

## Relations

Variable :
HasRelation = true / false


Utilisée pour :

- événements
- conflits
- narration

---

# 🎭 Apparence PNJ

Les PNJ utilisent des prefabs aléatoires.

MalePNJPrefabs[]
FemalePNJPrefabs[]

Initialisation :

Si PNJ.Gender = Male
→ prefab homme aléatoire

Si PNJ.Gender = Female
→ prefab femme aléatoire


---

# 🎬 Animations PNJ

Animations utilisées :
Idle
Movement
Work


| Animation | Condition |
|----------|-----------|
| Idle | PNJ inactif |
| Movement | PNJ en déplacement |
| Work | PNJ dans une zone de travail |

---

# 🛠 Système de tâches

Les tâches sont définies via l’ordinateur du bureau.

Exemples :
Repos
Ferme
Eau
Soigner
Se faire soigner
Stockage
Exploration


Les PNJ se déplacent vers leur **zone de travail**.

---

# 🍞 Ressources

Ressources principales :
Food
Water

Consommation quotidienne :
1 Food / habitant
1 Water / habitant


Exemple :

7 habitants
→ 7 Food
→ 7 Water



---

# 🌾 Production

Production calculée **chaque heure**.

### Nourriture
Food += WorkersFarm × 0.4


### Eau
Water += WorkersWater × 0.35


---

# 📦 Stockage

Les ressources existent **physiquement dans le monde**.

Production : 

Spawn objet physique

Le joueur doit :
ramasser
transporter
ranger


---

## Étagères

Stockage via :
Shelves
Cabinets


Chaque étagère possède :
slots physiques


Stockage **illimité**.

---

# 📹 Caméras

Le joueur surveille l’abri via des caméras orientables.

Rotation :
Yaw : -60° → +60°
Pitch : -20° → +30°


Les caméras permettent de :

- surveiller les PNJ
- observer les conflits
- détecter des événements

---

# 🧭 Exploration

Les explorations sont préparées via l’ordinateur.

Le joueur choisit :
Explorateurs
Ressources
Destination

Carte :
Grid A1 → J10


Position abri : G5


---

# 🚶 Temps de trajet
Exemple :
G5 → E5
distance = 2 jours


---

# 🥫 Consommation en exploration

Chaque explorateur consomme :
1 Food / jour
1 Water / jour


---

# 📡 Communication Radio

Les explorateurs communiquent via **talkie‑walkie**.

Déclenchement :

RadioCallEvent


Effets :

- blocage mouvement joueur
- interface dialogue
- choix narratifs

---

# 💬 Dialogue System

Structure :
Explorateur
Texte
Choix
Conséquences


Exemple :
Explorateur : "On a trouvé un supermarché abandonné."
Choix :
Fouiller
Observer
Ignorer

---

# 📜 ScriptableObject Dialogue

Structure :
DialogueID
Speaker
Text
Choices[]
NextDialogue
EventTrigger


---

# 🧰 Dialogue Editor Tool

Outil Unity permettant de :

- créer dialogues
- organiser branches
- relier événements

Permet de créer facilement **de nombreux événements narratifs**.

---

# 🌍 Exploration Events

Exemples d'événements :

- supermarché abandonné
- camp de bandits
- survivant blessé
- tunnel
- station essence
- maison abandonnée
- dépôt militaire

---

# 🎮 Demo Version

La démo doit présenter **les mécaniques principales du jeu**.

---

# Objectif de la démo

Permettre au joueur de comprendre :

- gestion des habitants
- gestion ressources
- exploration
- narration radio

---

# Contenu de la démo

## Abri disponible
Bureau
Ferme
Salle d'eau
Stockage
Dortoir

---

## Population
5 travailleurs
2 non travailleurs


---

## Systèmes disponibles

La démo inclut :

- système de tâches
- production ressources
- stockage physique
- caméras
- exploration
- événements radio

---

## Exploration

Carte limitée autour de :
G5

Événements disponibles :

- supermarché abandonné
- camp de bandits
- survivant blessé
- maison abandonnée

---

## Durée

La démo doit durer environ :
30 à 60 minutes


---

## Fin de la démo

La démo se termine lorsque : 10 jours sont écoulés
ou
les ressources critiques sont épuisées


---

# ☠️ Conditions de défaite

Le joueur perd si :
Population = 0
Food = 0
Water = 0
Rébellion


---

# 🏁 Conditions de victoire

Plusieurs fins possibles :

- stabilisation de l’abri
- expansion de la colonie
- découverte de l’origine de l’apocalypse

---

# 🗺 Development Roadmap

## Prototype

- PNJ
- ressources
- tâches
- stockage
- exploration simple

---

## Alpha

- événements narratifs
- dialogues radio
- équilibrage

---

## Beta

- contenu exploration
- optimisation
- polish gameplay
