# -*- coding: utf-8 -*-
"""Materie prime nuove: rotolo di stoffa e pacchetto di metallo (oggetti che si tengono in mano).
Misure simili al fascio di legna: circa 0,5 m di lunghezza, base nell'origine."""
import math
import random

import elfkit as k


def cloth():
    """Rotolo di stoffa rossa a quadretti (strisce) avvolto, con il lembo libero e un nastro dorato."""
    random.seed(21)
    L = 0.5
    # rotolo principale (asse lungo X): cilindro morbido con spirale sulle teste
    roll = k.cylinder("Rotolo", 0.11, L, (0, 0, 0.11), rot=(0, 90, 0), mat="RedSanta", bevel=0.02, verts=28, segments=3)
    k.wobble(roll, 0.004, 0.25, levels=1)
    # strisce chiare (tessuto a righe): anelli leggermente sporgenti
    for x in (-0.17, -0.06, 0.06, 0.17):
        k.cylinder("Riga", 0.1115, 0.035, (x, 0, 0.11), rot=(0, 90, 0), mat="Cream", bevel=0.006, verts=28, segments=1)
    # teste del rotolo: spirale di stoffa (dischi concentrici leggermente rientrati)
    for side in (-1, 1):
        for i, r in enumerate((0.085, 0.06, 0.035)):
            k.cylinder("Spirale", r, 0.012, (side * (L / 2 + 0.002 - i * 0.004), 0, 0.11), rot=(0, 90, 0),
                       mat="RedDeep" if i % 2 == 0 else "RedSanta", bevel=0.003, verts=20, segments=1)
        k.cylinder("Anima", 0.018, 0.02, (side * (L / 2 + 0.004), 0, 0.11), rot=(0, 90, 0), mat="WoodLight", bevel=0.003, verts=12, segments=1)
    # nastro dorato con fiocco
    k.torus("Nastro", 0.121, 0.009, (0.0, 0, 0.11), rot=(0, 90, 0), mat="Gold", major_seg=32, minor_seg=6)
    for sx in (-1, 1):
        b = k.sphere("Fiocco", 0.03, (sx * 0.028, -0.02, 0.225), scale=(1.3, 0.6, 0.8), rot=(0, sx * 25, 0), mat="Gold", segments=12, rings=8)
    k.sphere("Nodo", 0.016, (0, -0.02, 0.226), mat="Gold", segments=10, rings=6)
    return {"ao_distance": 0.08, "ground": 0.0}


def metal():
    """Pacchetto di metallo: due lamiere legate con una cinghia, un ingranaggio e qualche bullone sopra."""
    random.seed(22)
    for i, (z, rot) in enumerate(((0.02, 0), (0.055, 4))):
        p = k.box("Lamiera", (0.44, 0.28, 0.03), (0, 0, z), rot=(0, 0, rot), mat="MetalLight" if i == 0 else "MetalDark", bevel=0.008, segments=2)
        # piccoli fori da rivetto agli angoli
        for (x, y) in ((-0.19, -0.11), (0.19, -0.11), (-0.19, 0.11), (0.19, 0.11)):
            k.cylinder("Rivetto", 0.009, 0.006, (x, y, z + 0.016), mat="Iron", bevel=0.002, verts=10, segments=1)
    # cinghia di cuoio
    for x in (-0.12, 0.12):
        k.box("Cinghia", (0.04, 0.3, 0.08), (x, 0, 0.04), mat="WoodDeep", bevel=0.008, segments=1)
        k.box("Fibbia", (0.05, 0.02, 0.03), (x, -0.152, 0.05), mat="Gold", bevel=0.004, segments=1)
    # ingranaggio sopra: anello con denti
    gx, gy, gz = 0.05, 0.02, 0.085
    k.cylinder("Ingranaggio", 0.075, 0.025, (gx, gy, gz), mat="Gold", bevel=0.005, verts=32, segments=1)
    for t in range(12):
        a = 2 * math.pi * t / 12
        k.box("Dente", (0.022, 0.026, 0.025), (gx + math.cos(a) * 0.085, gy + math.sin(a) * 0.085, gz), rot=(0, 0, math.degrees(a)), mat="Gold", bevel=0.004, segments=1)
    k.cylinder("Foro", 0.025, 0.028, (gx, gy, gz + 0.001), mat="MetalDark", bevel=0.004, verts=16, segments=1)
    # bulloni sparsi
    for (x, y) in ((-0.13, 0.06), (-0.09, -0.07)):
        k.cylinder("Bullone", 0.018, 0.016, (x, y, 0.082), mat="Iron", bevel=0.003, verts=6, segments=1)
        k.cylinder("Gambo", 0.008, 0.05, (x, y, 0.1), mat="MetalLight", bevel=0.002, verts=10, segments=1)
    return {"ao_distance": 0.06, "ground": 0.0}


MODELS = {
    "Item_Cloth": cloth,
    "Item_Metal": metal,
}
