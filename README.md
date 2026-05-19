## Game Name

**One More Trap**

---

## Genre

2D Puzzle Platformer

---

## Repeated Action (Core Gameplay Loop)

* Move
* Jump
* Step on buttons
* Avoid traps
* Activate platforms and doors

---

## One-Sentence Idea

A player solves trap-based platform puzzles by activating mechanisms that change the environment.

---

## Vertical Slice (Smallest Playable Version)

A single level where the player:

* moves and jumps,
* activates a floor button,
* triggers a retractable platform,
* opens a door,
* and reaches the exit while avoiding spikes.

---

## Core Systems (Unity Systems / Scripts Needed First)

* Player movement system
* Jump system
* Collision detection
* Trigger system (`OnTriggerEnter2D`)
* Retractable ground script
* Floor button interaction
* Door activation system
* Spike trap system
* Camera system
* Basic UI / win condition

---

## GitHub Status

Repository created with Unity project setup and initial gameplay scripts.

```text
https://github.com/Yuan-Yuzhuo/One_More_Trap
```

---

## Biggest Risk

Balancing platform timing and collision behavior may cause gameplay bugs or inconsistent player interaction.
