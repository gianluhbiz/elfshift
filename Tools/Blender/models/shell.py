# -*- coding: utf-8 -*-
"""Parti in legno della stanza: boiserie a pannelli, pilastri con saette, travi del soffitto.
Sono "moduli" che ShellModels.cs ripete lungo le pareti."""
import math
import random

import elfkit as k


def wainscot():
    """Modulo di boiserie largo 1 m: zoccolo, cornice, pannello in rilievo e corrimano.
    Origine: a terra, sulla superficie della parete; il davanti guarda -Y (verso la stanza)."""
    random.seed(11)
    W = 1.0
    # fondo (tavola piatta contro il muro)
    k.box("Fondo", (W, 0.03, 1.2), (0, -0.015, 0.6), mat="WoodMid", bevel=0.004, segments=1)
    # zoccolo
    k.box("Zoccolo", (W, 0.045, 0.16), (0, -0.045, 0.08), mat="WoodDark", bevel=0.01, segments=2)
    k.box("ZoccoloBordo", (W, 0.025, 0.03), (0, -0.06, 0.17), mat="WoodDark", bevel=0.008, segments=1)
    # montanti e traversi della cornice
    for x in (-0.47, 0.47):
        k.box("Montante", (0.06, 0.03, 0.98), (x, -0.045, 0.69), mat="WoodMid", bevel=0.008, segments=1)
    k.box("TraversoBasso", (W, 0.03, 0.08), (0, -0.045, 0.22), mat="WoodMid", bevel=0.008, segments=1)
    k.box("TraversoAlto", (W, 0.03, 0.08), (0, -0.045, 1.12), mat="WoodMid", bevel=0.008, segments=1)
    # pannello in rilievo (bugnato): base piatta + cuscino smussato
    k.box("Pannello", (0.84, 0.02, 0.78), (0, -0.04, 0.67), mat="WoodMid", bevel=0.004, segments=1)
    k.box("Bugna", (0.7, 0.03, 0.64), (0, -0.05, 0.67), mat="WoodMid", bevel=0.03, segments=2)
    # modanatura attorno al pannello
    for (sx, sz, x, z) in ((0.86, 0.02, 0, 0.27), (0.86, 0.02, 0, 1.07), (0.02, 0.82, -0.43, 0.67), (0.02, 0.82, 0.43, 0.67)):
        k.box("Modanatura", (sx, 0.02, sz), (x, -0.058, z), mat="WoodDark", bevel=0.007, segments=1)
    # corrimano con naso arrotondato
    k.box("Corrimano", (W, 0.1, 0.05), (0, -0.05, 1.205), mat="WoodDark", bevel=0.02, segments=3)
    k.box("CorrimanoSotto", (W, 0.05, 0.03), (0, -0.03, 1.17), mat="WoodDark", bevel=0.01, segments=1)
    return {"ao_distance": 0.12, "ground": 0.0}


def post():
    """Pilastro 0,3 x 0,3 x 5 m con smussi, basamento, capitello e due saette (lungo X) fino al soffitto."""
    random.seed(12)
    H = 5.0
    k.box("Fusto", (0.28, 0.28, H - 0.25), (0, 0, 0.25 + (H - 0.25) / 2), mat="WoodDark", bevel=0.035, segments=2)
    k.box("Base", (0.36, 0.36, 0.22), (0, 0, 0.11), mat="WoodDeep", bevel=0.03)
    k.box("BaseCornice", (0.33, 0.33, 0.05), (0, 0, 0.245), mat="WoodDeep", bevel=0.015)
    k.box("Capitello", (0.36, 0.36, 0.14), (0, 0, H - 0.07), mat="WoodDeep", bevel=0.025)
    # saette a 45 gradi
    L = 0.85
    for side in (-1, 1):
        cx = side * (0.14 + L * 0.35)
        cz = H - 0.14 - L * 0.35
        brace = k.box("Saetta", (L, 0.18, 0.14), (cx, 0, cz), rot=(0, -side * 45, 0), mat="WoodDark", bevel=0.025)
        k.wobble(brace, 0.004, 0.5, levels=1)
        # tasselli di legno (perni) sulle giunzioni
        for (px, pz) in ((side * 0.17, H - 0.62), (side * 0.62, H - 0.18)):
            k.cylinder("Perno", 0.022, 0.03, (px, -0.105, pz), rot=(90, 0, 0), mat="WoodDeep", bevel=0.006, verts=12)
    return {"ao_distance": 0.25, "ground": 0.0}


def beam():
    """Trave di 4 m (lungo X), 0,4 x 0,3, squadrata a mano: smussi irregolari e segni d'ascia (leggere ondulazioni)."""
    random.seed(13)
    k.box("Trave", (4.0, 0.4, 0.3), (0, 0, 0), mat="WoodDark", bevel=0.035, segments=2)
    # chiodi forgiati sulla faccia inferiore
    for x in (-1.6, -0.5, 0.6, 1.7):
        k.cylinder("Chiodo", 0.018, 0.012, (x + random.uniform(-0.1, 0.1), random.uniform(-0.1, 0.1), -0.152), mat="Iron", bevel=0.004, verts=8)
    return {"ao_distance": 0.2, "ground": None}


MODELS = {
    "Shell_Wainscot": wainscot,
    "Shell_Post": post,
    "Shell_Beam": beam,
}
