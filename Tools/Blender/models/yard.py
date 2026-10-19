# -*- coding: utf-8 -*-
"""Modelli del cortile: cassetta della posta, renne, cornici delle porte, cassetta di carote, balla di fieno.
Convenzioni elfkit: metri, Z in alto, il davanti guarda -Y (in Unity diventa +Z), origine a terra al centro."""
import math
import random

import elfkit as k

# ============================================================================ cassetta della posta
MB_W = 0.42        # larghezza della cassetta
MB_D = 0.56        # profondità
MB_Z0 = 0.9        # base della cassetta (sopra il palo)
MB_Z1 = 1.12       # inizio della volta
MB_R = MB_W / 2    # raggio della volta
# cerniera della bandierina (lato +X di Unity = -X di Blender), vicino al retro
FLAG_HINGE = (-(MB_W / 2 + 0.018), 0.13, 1.1)


def _mailbox_outline(inset=0.0):
    w = MB_W / 2 - inset
    pts = [(-w, MB_Z0 + inset), (w, MB_Z0 + inset), (w, MB_Z1)]
    r = MB_R - inset
    for i in range(1, 16):
        a = math.pi * i / 16
        pts.append((r * math.cos(a), MB_Z1 + r * math.sin(a)))
    pts.append((-w, MB_Z1))
    return pts


def mailbox_body():
    random.seed(21)
    # palo con piedino e mensola
    post = k.box("Palo", (0.11, 0.11, MB_Z0 - 0.02), (0, 0.05, (MB_Z0 - 0.02) / 2), mat="WoodMid", bevel=0.015, segments=2)
    k.wobble(post, 0.003, 0.4, levels=1)
    k.box("Piede", (0.24, 0.24, 0.08), (0, 0.05, 0.04), mat="WoodDark", bevel=0.02, segments=2)
    k.box("Mensola", (0.3, 0.42, 0.04), (0, 0.02, MB_Z0 - 0.02), mat="WoodDark", bevel=0.012, segments=2)
    for s in (-1, 1):
        k.box("Saetta", (0.04, 0.2, 0.04), (0, 0.05 + s * 0.09, MB_Z0 - 0.12), rot=(s * 45, 0, 0), mat="WoodDark", bevel=0.01, segments=1)
    # corpo a "pagnotta" rosso
    k.extrude_outline("Corpo", _mailbox_outline(), MB_D, loc=(0, 0.02, 0), mat="RedSanta", bevel=0.025)
    # sportello frontale leggermente in rilievo, rosso più scuro
    k.extrude_outline("Sportello", _mailbox_outline(0.03), 0.03, loc=(0, 0.02 - MB_D / 2 - 0.006, 0), mat="RedDeep", bevel=0.01)
    # bordo dorato attorno allo sportello
    rim = [(x, -MB_D / 2 + 0.02 - 0.012, z) for (x, z) in _mailbox_outline(0.012)]
    rim.append(rim[0])
    k.tube("Bordo", rim, 0.011, mat="Gold", sides=8, caps=False)
    # fessura per le lettere con cornice dorata
    fy = -MB_D / 2 + 0.02 - 0.024
    k.box("CorniceFessura", (0.25, 0.02, 0.06), (0, fy, 1.2), mat="Gold", bevel=0.008, segments=2)
    k.box("Fessura", (0.21, 0.03, 0.022), (0, fy - 0.004, 1.2), mat="Black", bevel=0.004, segments=1)
    # maniglia e stellina dorata
    k.sphere("Pomello", 0.022, (0, fy - 0.01, 1.07), mat="Gold", segments=12, rings=8)
    star = []
    for i in range(10):
        a = math.pi / 2 + i * math.pi / 5
        r = 0.05 if i % 2 == 0 else 0.022
        star.append((math.cos(a) * r, 1.0 + math.sin(a) * r))
    k.extrude_outline("Stella", star, 0.012, loc=(0, fy - 0.004, 0.0), mat="Gold", bevel=0.003)
    # cerniera della bandierina
    k.cylinder("Perno", 0.02, 0.03, (FLAG_HINGE[0] + 0.006, FLAG_HINGE[1], FLAG_HINGE[2]), rot=(0, 90, 0), mat="Gold", bevel=0.005, verts=12)
    # fasce dorate sul corpo
    for y in (-0.12, 0.16):
        band = [(x, y + 0.02, z) for (x, z) in _mailbox_outline(-0.006)]
        band.append(band[0])
        k.tube("Fascia", band, 0.009, mat="Gold", sides=8, caps=False)
    # cappello di neve sulla volta
    snow = k.sphere("Neve", 0.24, (0.01, 0.02, MB_Z1 + MB_R - 0.02), scale=(1.0, 1.25, 0.32), mat="Snow", segments=20, rings=10)
    k.wobble(snow, 0.012, 0.15, levels=1)
    # rametto di agrifoglio sul tetto
    for s in (-1, 1):
        k.sphere("Foglia", 0.05, (s * 0.045, -0.14, MB_Z1 + MB_R + 0.04), rot=(0, 0, s * 35), scale=(1.0, 0.45, 0.16), mat="GreenElf", segments=10, rings=6)
    for (bx, by) in ((0, -0.15), (0.02, -0.13), (-0.018, -0.125)):
        k.sphere("Bacca", 0.017, (bx, by, MB_Z1 + MB_R + 0.055), mat="RedSanta", segments=8, rings=6)
    # mucchietto di neve ai piedi del palo
    foot = k.sphere("NeveTerra", 0.3, (0.02, 0.05, 0.0), scale=(1.0, 0.9, 0.22), mat="Snow", segments=18, rings=8)
    k.wobble(foot, 0.015, 0.2, levels=1)
    return {"ao_distance": 0.25, "ground": 0.0, "preview_yaw": 30}


def mailbox_flag():
    """Bandierina con origine nella cerniera. Posizione "alzata": il braccio punta verso l'alto (+Z)."""
    k.box("Braccio", (0.018, 0.035, 0.24), (0, 0, 0.12), mat="Gold", bevel=0.006, segments=2)
    k.box("Bandiera", (0.014, 0.12, 0.08), (0, -0.045, 0.2), mat="RedSanta", bevel=0.008, segments=2)
    k.cylinder("Rondella", 0.026, 0.012, (0, 0, 0), rot=(0, 90, 0), mat="Gold", bevel=0.004, verts=12)
    return {"ao_distance": 0.05, "ground": None}


def mailbox_letters():
    """Tre letterine con ceralacca che spuntano dalla fessura (stessa origine della cassetta)."""
    random.seed(4)
    fy = -MB_D / 2 + 0.02 - 0.024
    for i, (dx, dz, yaw, col) in enumerate(((-0.03, 1.195, 8, "Cream"), (0.035, 1.202, -10, "White"), (0.0, 1.21, 2, "Cream"))):
        env = k.box("Busta", (0.16, 0.14, 0.006), (dx, fy - 0.02, dz), rot=(-6, 0, yaw), mat=col, bevel=0.002, segments=1)
        # ceralacca rossa sulla parte che sporge
        k.cylinder("Ceralacca", 0.018, 0.006, (dx + math.sin(math.radians(-yaw)) * 0.05, fy - 0.07, dz + 0.008), rot=(-6, 0, 0), mat="RedDeep", bevel=0.002, verts=12)
    return {"ao_distance": 0.05, "ground": None}


# ============================================================================ renna
# perno del collo (dove si attacca la testa), coordinate Blender
NECK_PIVOT = (0.0, -0.53, 1.12)


def reindeer_body():
    random.seed(31)
    # corpo paffuto e basso: zampe corte, schiena a circa 1,1 m
    body = k.sphere("Corpo", 1.0, (0, 0.04, 0.77), scale=(0.37, 0.6, 0.35), mat="ReindeerBrown", segments=24, rings=12)
    k.wobble(body, 0.01, 0.4, levels=1)
    k.sphere("Pancia", 1.0, (0, 0.02, 0.71), scale=(0.31, 0.5, 0.3), mat="Cream", segments=20, rings=10)
    k.sphere("Petto", 1.0, (0, -0.4, 0.76), scale=(0.16, 0.12, 0.17), mat="Cream", segments=16, rings=8)
    # collo verso la testa (la testa è un modello a parte con il perno in NECK_PIVOT)
    k.tube("Collo", [(0, -0.34, 0.86), (0, -0.44, 0.98), (0, -0.52, 1.1)], 0.165, mat="ReindeerBrown", sides=14, radius_end=0.13)
    # zampe corte e robuste, zoccoli scuri con polsino di pelo chiaro
    for sx in (-1, 1):
        for sy, bend in ((-1, -0.02), (1, 0.03)):
            x = sx * 0.165
            y = sy * 0.3
            k.tube("Zampa", [(x, y, 0.66), (x, y + bend * 0.5, 0.48), (x, y + bend, 0.3), (x, y, 0.1)], 0.1, mat="ReindeerBrown", sides=12, radius_end=0.075, caps=False)
            k.cylinder("Zoccolo", 0.08, 0.1, (x, y - 0.005, 0.05), mat="ReindeerDark", bevel=0.025, verts=16, radius_top=0.072)
            k.torus("Pelo", 0.078, 0.022, (x, y, 0.12), mat="Cream", major_seg=16, minor_seg=6)
    # codina
    k.sphere("Coda", 0.07, (0, 0.63, 0.9), scale=(1.0, 0.8, 1.2), mat="Cream", segments=12, rings=8)
    # collare rosso attorno al collo con campanella dorata
    k.torus("Collare", 0.158, 0.03, (0, -0.45, 1.0), rot=(35, 0, 0), mat="RedSanta", major_seg=24, minor_seg=8)
    k.sphere("Campanella", 0.05, (0, -0.6, 0.9), mat="Gold", segments=14, rings=8)
    k.torus("CampanellaBordo", 0.04, 0.009, (0, -0.6, 0.875), mat="Gold", major_seg=14, minor_seg=6)
    # sottopancia del finimento con fibbie
    _ring_around_body("Finimento", -0.14)
    for sx in (-1, 1):
        k.box("Fibbia", (0.02, 0.07, 0.07), (sx * 0.36, -0.14, 0.8), mat="Gold", bevel=0.006, segments=1)
    # copertina sul dorso con orlo dorato
    blanket = k.sphere("Copertina", 1.0, (0, 0.12, 0.93), scale=(0.35, 0.3, 0.2), mat="GreenElf", segments=20, rings=10)
    k.wobble(blanket, 0.006, 0.2, levels=1)
    return {"ao_distance": 0.3, "ground": 0.0, "preview_yaw": 40}


def _ring_around_body(name, y):
    """Anello rosso che abbraccia il corpo (ellisse che segue la sezione del corpo a quella profondità)."""
    pts = []
    rx, rz, cz = 0.36, 0.34, 0.77
    for i in range(41):
        a = 2 * math.pi * i / 40
        pts.append((rx * math.cos(a), y, cz + rz * math.sin(a)))
    return k.tube(name, pts, 0.028, mat="RedSanta", sides=8, caps=False)


def reindeer_head(variant):
    def build():
        random.seed(41 + variant)
        # origine = perno del collo; la testa guarda -Y
        head = k.sphere("Testa", 1.0, (0, -0.13, 0.12), scale=(0.19, 0.24, 0.19), mat="ReindeerBrown", segments=20, rings=10)
        k.wobble(head, 0.006, 0.3, levels=1)
        k.sphere("Muso", 1.0, (0, -0.31, 0.06), scale=(0.13, 0.13, 0.105), mat="Cream", segments=20, rings=10)
        if variant == 1:
            k.sphere("Naso", 0.055, (0, -0.435, 0.09), mat="BulbRed", segments=16, rings=10)
        else:
            k.sphere("Naso", 0.045, (0, -0.43, 0.09), scale=(1.15, 0.9, 0.85), mat="Black", segments=14, rings=8)
        for sx in (-1, 1):
            k.sphere("Occhio", 0.042, (sx * 0.088, -0.285, 0.17), scale=(0.9, 0.7, 1.1), mat="EyeDark", segments=12, rings=8)
            k.sphere("Luce", 0.012, (sx * 0.078, -0.318, 0.19), mat="White", segments=8, rings=6)
            k.sphere("Guancia", 0.03, (sx * 0.12, -0.3, 0.1), scale=(1.0, 0.5, 0.7), mat="Cheek", segments=10, rings=6)
            ear = k.sphere("Orecchio", 1.0, (sx * 0.2, -0.06, 0.22), rot=(0, sx * -40, sx * 20), scale=(0.1, 0.045, 0.05), mat="ReindeerBrown", segments=14, rings=8)
            k.sphere("OrecchioDentro", 1.0, (sx * 0.2, -0.095, 0.22), rot=(0, sx * -40, sx * 20), scale=(0.065, 0.02, 0.03), mat="Cream", segments=12, rings=6)
            # corna: tronco principale curvo + due rametti
            base = (sx * 0.08, -0.06, 0.28)
            main = [base, (sx * 0.13, -0.03, 0.42), (sx * 0.2, 0.04, 0.55), (sx * 0.26, 0.12, 0.66), (sx * 0.27, 0.2, 0.74)]
            k.tube("Corno", main, 0.03, mat="Antler", sides=8, radius_end=0.016)
            k.tube("Rametto", [(sx * 0.14, -0.025, 0.44), (sx * 0.13, -0.08, 0.54), (sx * 0.11, -0.1, 0.62)], 0.02, mat="Antler", sides=8, radius_end=0.012)
            k.tube("Rametto", [(sx * 0.215, 0.06, 0.58), (sx * 0.25, 0.02, 0.68), (sx * 0.25, -0.01, 0.75)], 0.018, mat="Antler", sides=8, radius_end=0.011)
            k.tube("Rametto", [(sx * 0.25, 0.11, 0.65), (sx * 0.33, 0.12, 0.7), (sx * 0.38, 0.13, 0.72)], 0.016, mat="Antler", sides=8, radius_end=0.01)
        # ciuffo sulla fronte
        for i in range(3):
            k.sphere("Ciuffo", 0.045, (0.035 * (i - 1), -0.17, 0.29 + 0.012 * (i % 2)), scale=(0.8, 1.2, 0.5), mat="ReindeerDark", segments=10, rings=6)
        return {"ao_distance": 0.2, "ground": None, "preview_yaw": 40}
    return build


# ============================================================================ cornice della porta (moduli)
def door_casing():
    """Tavola della cornice: larga 0,16, sporge 0,05 dal muro, lunga 1 (verticale, da z=0 a z=1). Il davanti guarda -Y."""
    k.box("Tavola", (0.16, 0.05, 1.0), (0, -0.025, 0.5), mat="WoodDark", bevel=0.012, segments=2, tint=0.0)
    # due modanature in rilievo
    for x in (-0.045, 0.045):
        k.box("Listello", (0.022, 0.012, 1.0), (x, -0.054, 0.5), mat="WoodDark", bevel=0.005, segments=1, tint=0.0)
    return {"ao_distance": 0.05, "ground": None}


def door_plinth():
    """Zoccolo alla base della cornice (0,2 x 0,06 x 0,26)."""
    k.box("Zoccolo", (0.2, 0.065, 0.26), (0, -0.0325, 0.13), mat="WoodDeep", bevel=0.015, segments=2)
    k.box("Cornicetta", (0.21, 0.075, 0.03), (0, -0.0375, 0.255), mat="WoodDeep", bevel=0.01, segments=1)
    return {"ao_distance": 0.05, "ground": None}


def door_corner():
    """Blocco d'angolo con rosetta intagliata (0,2 x 0,07 x 0,2), centrato in origine."""
    k.box("Blocco", (0.2, 0.07, 0.2), (0, -0.035, 0), mat="WoodDeep", bevel=0.015, segments=2)
    k.torus("Anello", 0.055, 0.012, (0, -0.072, 0), rot=(90, 0, 0), mat="WoodDark", major_seg=24, minor_seg=8)
    k.sphere("Bottone", 0.03, (0, -0.07, 0), scale=(1.0, 0.6, 1.0), mat="WoodDark", segments=14, rings=8)
    return {"ao_distance": 0.05, "ground": None}


def door_keystone():
    """Chiave di volta trapezoidale con stellina, base in origine (sta sopra l'architrave)."""
    outline = [(-0.09, -0.06), (0.09, -0.06), (0.12, 0.2), (-0.12, 0.2)]
    k.extrude_outline("Chiave", outline, 0.09, loc=(0, -0.045, 0), mat="WoodDark", bevel=0.015)
    star = []
    for i in range(10):
        a = math.pi / 2 + i * math.pi / 5
        r = 0.05 if i % 2 == 0 else 0.022
        star.append((math.cos(a) * r, 0.08 + math.sin(a) * r))
    k.extrude_outline("Stella", star, 0.015, loc=(0, -0.095, 0), mat="Gold", bevel=0.003)
    return {"ao_distance": 0.06, "ground": None}


# ============================================================================ decori del cortile
def carrot_crate():
    random.seed(51)
    W, D, H = 0.55, 0.38, 0.28
    # fondo e assi laterali con fessure
    k.box("Fondo", (W, D, 0.03), (0, 0, 0.015), mat="WoodMid", bevel=0.006, segments=1)
    for z in (0.07, 0.19):
        for sy in (-1, 1):
            k.jitter(k.box("Asse", (W, 0.022, 0.09), (0, sy * (D / 2 - 0.011), z), mat="WoodLight", bevel=0.006, segments=1), 0.5, 0.002)
        for sx in (-1, 1):
            k.box("Testata", (0.022, D, 0.09), (sx * (W / 2 - 0.011), 0, z), mat="WoodLight", bevel=0.006, segments=1)
    for sx in (-1, 1):
        for sy in (-1, 1):
            k.box("Angolo", (0.04, 0.04, H), (sx * (W / 2 - 0.02), sy * (D / 2 - 0.02), H / 2), mat="WoodMid", bevel=0.006, segments=1)
    # carote ammucchiate (coni arancioni con ciuffo verde)
    placed = 0
    for i in range(14):
        x = random.uniform(-W / 2 + 0.07, W / 2 - 0.07)
        y = random.uniform(-D / 2 + 0.07, D / 2 - 0.07)
        z = 0.2 + random.uniform(0, 0.08)
        tilt = random.uniform(55, 95)
        yaw = random.uniform(0, 360)
        c = k.cylinder("Carota", 0.03, 0.2, (x, y, z), rot=(tilt, 0, yaw), mat="Carrot", bevel=0.0, verts=10, radius_top=0.006)
        # ciuffo: tre foglioline in cima (lato grosso del cono, che sta in -Z locale)
        import mathutils
        rot = mathutils.Euler((math.radians(tilt), 0, math.radians(yaw)))
        top = mathutils.Vector((x, y, z)) + rot.to_matrix() @ mathutils.Vector((0, 0, -0.1))
        for j in range(3):
            k.sphere("Ciuffo", 0.03, tuple(top + mathutils.Vector((random.uniform(-0.02, 0.02), random.uniform(-0.02, 0.02), 0.02))),
                     rot=(random.uniform(0, 60), 0, random.uniform(0, 360)), scale=(0.35, 0.35, 1.3), mat="GreenLight", segments=6, rings=4)
        placed += 1
    # una carota appoggiata fuori
    k.cylinder("CarotaFuori", 0.032, 0.22, (0.36, -0.12, 0.03), rot=(90, 0, 70), mat="Carrot", bevel=0.0, verts=10, radius_top=0.006)
    return {"ao_distance": 0.15, "ground": 0.0}


def hay_bale():
    random.seed(61)
    R, L = 0.42, 0.7
    bale = k.cylinder("Balla", R, L, (0, 0, R), rot=(0, 90, 0), mat="Hay", bevel=0.09, verts=22, segments=2)
    k.wobble(bale, 0.025, 0.12, levels=1)
    for x in (-0.18, 0.18):
        k.torus("Spago", R + 0.004, 0.012, (x, 0, R), rot=(0, 90, 0), mat="HayDark", major_seg=26, minor_seg=5)
    # fili di paglia che spuntano
    for i in range(16):
        a = random.uniform(0, 2 * math.pi)
        x = random.choice((-1, 1)) * random.uniform(0.2, 0.36)
        p0 = (x, math.cos(a) * R * 0.98, R + math.sin(a) * R * 0.98)
        d = (random.uniform(-0.05, 0.05), math.cos(a) * 0.09, math.sin(a) * 0.09)
        k.tube("Paglia", [p0, (p0[0] + d[0], p0[1] + d[1], p0[2] + d[2])], 0.006, mat="Hay", sides=4, radius_end=0.002)
    # paglia sparsa a terra
    for i in range(8):
        a = random.uniform(0, 2 * math.pi)
        r = random.uniform(0.45, 0.65)
        k.box("Fieno", (0.14, 0.008, 0.008), (math.cos(a) * r, math.sin(a) * r, 0.005), rot=(0, 0, random.uniform(0, 180)), mat="HayDark", bevel=0.0, segments=1)
    return {"ao_distance": 0.25, "ground": 0.0}


MODELS = {
    "Yard_MailboxBody": mailbox_body,
    "Yard_MailboxFlag": mailbox_flag,
    "Yard_MailboxLetters": mailbox_letters,
    "Yard_ReindeerBody": reindeer_body,
    "Yard_ReindeerHead_0": reindeer_head(0),
    "Yard_ReindeerHead_1": reindeer_head(1),
    "Yard_DoorCasing": door_casing,
    "Yard_DoorPlinth": door_plinth,
    "Yard_DoorCorner": door_corner,
    "Yard_DoorKeystone": door_keystone,
    "Yard_CarrotCrate": carrot_crate,
    "Yard_HayBale": hay_bale,
}
