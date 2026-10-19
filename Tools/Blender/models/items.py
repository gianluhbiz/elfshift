# -*- coding: utf-8 -*-
"""
Oggetti trasportabili di Elfshift: tronchetto singolo, fascio di legna e trenino giocattolo.
Convenzioni elfkit: metri, Z in alto, davanti del modello verso -Y (in Unity: +Z).
Origine: al centro della base (fascio, trenino) o al centro del tronchetto (Item_Log).
"""
import math
import random

import elfkit as k
import bpy  # noqa: F401  (serve prima di bmesh)
import bmesh
from mathutils import Vector, noise

# Misure "di riferimento" usate anche dal codice C# (ItemModels.cs) per scalare i modelli.
LOG_RADIUS = 0.1
LOG_LENGTH = 0.56


# ============================================================================ tronchetto
def _bark_log(name, radius, length, seed=0, stub=False):
    """Tronchetto lungo l'asse X, centrato nell'origine.
    Corteccia con creste longitudinali irregolari, bordo di corteccia sporgente e sezione chiara
    con anelli di crescita (anelli concentrici di colore diverso)."""
    sides = 18
    rings_x = 8
    bm = bmesh.new()
    rnd = random.Random(seed)
    phase = rnd.uniform(0, 10)

    def bark_r(a, x):
        p = Vector((math.cos(a) * 1.3 + phase, math.sin(a) * 1.3, x * 3.0))
        ridge = 0.5 + 0.5 * math.sin(a * 9 + noise.noise(p) * 2.5)
        bump = noise.noise(Vector((math.cos(a) * 2.0, math.sin(a) * 2.0, x * 1.6 + phase)))
        return radius * (1.0 + 0.07 * ridge + 0.05 * bump)

    half = length / 2
    xs = [-half + length * i / (rings_x - 1) for i in range(rings_x)]
    side_rings = []
    for x in xs:
        side_rings.append([bm.verts.new((x, bark_r(2 * math.pi * j / sides, x) * math.cos(2 * math.pi * j / sides),
                                        bark_r(2 * math.pi * j / sides, x) * math.sin(2 * math.pi * j / sides)))
                           for j in range(sides)])
    bark_faces = []
    for i in range(rings_x - 1):
        A, B = side_rings[i], side_rings[i + 1]
        for j in range(sides):
            j2 = (j + 1) % sides
            bark_faces.append(bm.faces.new((A[j], A[j2], B[j2], B[j])))

    end_faces = {"Bark": [], "WoodEnd": [], "WoodLight": [], "WoodMid": []}
    for sgn, outer in ((-1, side_rings[0]), (1, side_rings[-1])):
        x0 = sgn * half
        # bordo della corteccia: un anello che rientra verso l'interno
        lip = [bm.verts.new((x0 + sgn * 0.002, v.co.y * 0.9, v.co.z * 0.9)) for v in outer]
        # sezione: leggermente incassata, con anelli concentrici
        radii = [(0.84, "Bark"), (0.62, "WoodEnd"), (0.57, "WoodLight"), (0.36, "WoodEnd"),
                 (0.31, "WoodLight"), (0.12, "WoodEnd"), (0.0, "WoodMid")]
        prev = lip
        prev_mat = "Bark"
        rings = [(outer, lip, "Bark")]
        for f, mat in radii:
            if f <= 0:
                break
            wob = [1.0 + 0.04 * noise.noise(Vector((math.cos(2 * math.pi * j / sides) * 2, math.sin(2 * math.pi * j / sides) * 2, seed + f * 7)))
                   for j in range(sides)]
            ring = [bm.verts.new((x0 - sgn * 0.004, radius * f * wob[j] * math.cos(2 * math.pi * j / sides),
                                  radius * f * wob[j] * math.sin(2 * math.pi * j / sides))) for j in range(sides)]
            rings.append((prev, ring, prev_mat if prev is lip else mat))
            prev = ring
            prev_mat = mat
        for (A, B, mat) in rings:
            for j in range(sides):
                j2 = (j + 1) % sides
                end_faces[mat].append(bm.faces.new((A[j], A[j2], B[j2], B[j])))
        center = bm.verts.new((x0 - sgn * 0.003, 0, 0))
        for j in range(sides):
            j2 = (j + 1) % sides
            end_faces["WoodMid"].append(bm.faces.new((prev[j], prev[j2], center)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mats = ["Bark", "WoodEnd", "WoodLight", "WoodMid", "WoodDeep"]
    for f in bark_faces:
        c = f.calc_center_median()
        a = math.atan2(c.z, c.y)
        p = Vector((math.cos(a) * 1.3 + phase, math.sin(a) * 1.3, c.x * 3.0))
        ridge = 0.5 + 0.5 * math.sin(a * 9 + noise.noise(p) * 2.5)
        brk = noise.noise(Vector((c.x * 9.0, a * 2.0, seed * 0.7)))
        f.material_index = 4 if (ridge < 0.2 and brk > -0.05) else 0
    for key, faces in end_faces.items():
        for f in faces:
            f.material_index = mats.index(key)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    for m in mats:
        me.materials.append(k.material(m))
    for p in me.polygons:
        p.use_smooth = True
    obj["tint"] = tuple(1.0 + rnd.uniform(-0.04, 0.04) for _ in range(3))
    parts = [obj]
    if stub:
        # moncone di un rametto tagliato
        a = rnd.uniform(0.6, 1.2)
        base = Vector((rnd.uniform(-0.1, 0.1), math.cos(a) * radius * 0.95, math.sin(a) * radius * 0.95))
        d = Vector((0.25, math.cos(a), math.sin(a))).normalized()
        st = k.tube(name + "_ramo", [base - d * 0.02, base + d * 0.03, base + d * 0.055], radius * 0.28, mat="Bark",
                    sides=10, radius_end=radius * 0.22, caps=True)
        cap = k.cylinder(name + "_taglio", radius * 0.2, 0.006, loc=tuple(base + d * 0.056), mat="WoodEnd", bevel=0.0015, verts=10)
        cap.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
        parts += [st, cap]
    return parts


def _model_log():
    _bark_log("Tronchetto", LOG_RADIUS, LOG_LENGTH, seed=3, stub=True)
    return {"ao_distance": 0.12, "ground": None, "preview_yaw": 25}


# ============================================================================ fascio di legna
def _rope_path(centers, r, t, steps=72):
    """Percorso di una corda tesa attorno a più cerchi (nel piano Y-Z)."""
    pts = []
    for i in range(steps):
        a = 2 * math.pi * i / steps
        d = Vector((math.cos(a), math.sin(a)))
        best = max(centers, key=lambda c: c.dot(d))
        p = best + d * (r + t)
        pts.append(p)
    return pts


def _twine(name, x, centers, r):
    """Spago bicolore (rosso e panna) attorcigliato, attorno al fascio alla posizione x."""
    path2d = _rope_path(centers, r, 0.009, steps=40)
    n = len(path2d)
    parts = []
    for strand, mat in ((0, "RedSanta"), (1, "Cream")):
        pts = []
        for i in range(n + 1):
            p = path2d[i % n]
            q = path2d[(i + 1) % n]
            tang = (q - p).normalized()
            out = Vector((tang.y, -tang.x))  # normale verso l'esterno (percorso antiorario)
            if out.dot(p - sum(centers, Vector((0, 0))) / len(centers)) < 0:
                out = -out
            ang = i * 2 * math.pi / n * 8 + strand * math.pi
            off_x = math.cos(ang) * 0.0048
            off_r = math.sin(ang) * 0.0048
            pts.append((x + off_x, p.x + out.x * off_r, p.y + out.y * off_r))
        parts.append(k.tube(name + "_" + mat, pts, 0.0055, mat=mat, sides=5, caps=False, tint=0.0))
    return parts


def _bow(name, loc, scale=1.0):
    """Fiocchetto di spago: due asole e due codine."""
    x, y, z = loc
    parts = []
    for side in (-1, 1):
        loop = []
        for i in range(17):
            a = 2 * math.pi * i / 16
            loop.append((x + 0.004 * math.sin(a), y + side * (0.028 + 0.026 * math.cos(a)) * scale, z + 0.012 + 0.018 * math.sin(a) * scale + 0.01 * scale))
        parts.append(k.tube(name + "_asola%d" % side, loop, 0.0055 * scale, mat="RedSanta", sides=8, caps=False, tint=0.0))
        tail = [(x, y, z + 0.006), (x + 0.004, y + side * 0.018 * scale, z - 0.006), (x + 0.012, y + side * 0.03 * scale, z - 0.03 * scale)]
        parts.append(k.tube(name + "_coda%d" % side, tail, 0.005 * scale, mat="RedSanta", sides=8, radius_end=0.0035, caps=True, tint=0.0))
    parts.append(k.sphere(name + "_nodo", 0.011 * scale, loc=(x, y, z + 0.006), mat="RedDeep", segments=12, rings=8))
    return parts


def _model_bundle():
    r = LOG_RADIUS
    L = LOG_LENGTH
    # centri nel piano (Y, Z) di Blender: in Unity z = -Y, y = Z (come in ItemModels.AddLogBundle)
    centers = [Vector((-r * 1.05, r)), Vector((r * 1.05, r)), Vector((0.0, r * 2.8))]
    lengths = [L * 1.0, L * 0.96, L * 1.03]
    offsets = [0.012, -0.018, 0.004]
    for i, c in enumerate(centers):
        parts = _bark_log("Tronco%d" % i, r, lengths[i], seed=11 + i * 7, stub=(i == 2))
        for p in parts:
            p.location.x += offsets[i]
            p.location.y += c.x
            p.location.z += c.y
        parts[0].rotation_euler.x = math.radians(i * 47)  # creste diverse per ogni tronco
    for x in (-0.15, 0.15):
        _twine("Spago%.2f" % x, x, centers, r)
    _bow("Fiocco", (0.15, -0.0, r * 2.8 + r + 0.012), 1.0)
    return {"ao_distance": 0.1, "ground": 0.0, "preview_yaw": 30}


# ============================================================================ trenino
def _box(*args, **kw):
    """Scatola smussata con 2 segmenti di smusso (bastano per oggetti piccoli)."""
    kw.setdefault("segments", 2)
    return k.box(*args, **kw)


def _wheel(name, x_out, y, radius, side):
    """Ruota di legno dipinta: cerchione rosso, mozzo dorato con bullone, lieve smusso."""
    parts = []
    w = k.cylinder(name, radius, 0.032, loc=(x_out, y, radius + 0.002), rot=(0, 90, 0), mat="RedSanta", bevel=0.007, verts=18, segments=1)
    parts.append(w)
    hub = k.cylinder(name + "_mozzo", radius * 0.38, 0.016, loc=(x_out + side * 0.02, y, radius + 0.002), rot=(0, 90, 0), mat="Gold", bevel=0.004, verts=12)
    parts.append(hub)
    bolt = k.sphere(name + "_bullone", 0.007, loc=(x_out + side * 0.028, y, radius + 0.002), mat="MetalLight", segments=8, rings=4)
    parts.append(bolt)
    return parts


def _model_train():
    # Unity +Z (davanti, locomotiva) = Blender -Y
    # --- telaio con bordino dorato
    _box("Telaio", (0.2, 0.92, 0.05), loc=(0, 0.0, 0.105), mat="WoodDeep", bevel=0.012)
    for sx in (-1, 1):
        _box("Filetto%d" % sx, (0.006, 0.88, 0.012), loc=(sx * 0.1, 0.0, 0.112), mat="Gold", bevel=0.002, segments=1)
    # --- caldaia
    k.cylinder("Caldaia", 0.095, 0.34, loc=(0, -0.25, 0.225), rot=(90, 0, 0), mat="RedSanta", bevel=0.02, verts=22, segments=2)
    for yy in (-0.15, -0.25, -0.35):
        k.torus("Fascia%.2f" % yy, 0.096, 0.0065, loc=(0, yy, 0.225), rot=(90, 0, 0), mat="Gold", major_seg=22, minor_seg=5)
    # camera a fumo frontale e fanale
    k.cylinder("CameraFumo", 0.1, 0.07, loc=(0, -0.445, 0.225), rot=(90, 0, 0), mat="MetalDark", bevel=0.012, verts=22)
    k.cylinder("Fanale", 0.036, 0.03, loc=(0, -0.485, 0.24), rot=(90, 0, 0), mat="Gold", bevel=0.006, verts=18)
    k.sphere("Luce", 0.026, loc=(0, -0.497, 0.24), scale=(1, 0.6, 1), mat="LampGlow", segments=16, rings=8)
    # camino svasato
    k.lathe("Camino", [(0.0, 0.0), (0.03, 0.0), (0.03, 0.05), (0.036, 0.075), (0.05, 0.11), (0.054, 0.122), (0.048, 0.13), (0.0, 0.13)],
            loc=(0, -0.38, 0.3), mat="Iron", segments=20)
    k.torus("CaminoBordo", 0.05, 0.006, loc=(0, -0.38, 0.425), mat="Gold", major_seg=20, minor_seg=6)
    # duomo e fischietto
    k.lathe("Duomo", [(0.0, 0.0), (0.042, 0.0), (0.04, 0.02), (0.03, 0.04), (0.0, 0.05)], loc=(0, -0.25, 0.305), mat="Gold", segments=20)
    k.cylinder("Fischio", 0.009, 0.035, loc=(0, -0.2, 0.33), mat="Gold", bevel=0.002, verts=10)
    # spazzaneve frontale
    k.extrude_outline("Spazzaneve", [(-0.07, 0.03), (0.0, 0.03), (0.0, 0.13), (-0.012, 0.13)], 0.18,
                      loc=(0, -0.455, 0), rot=(0, 0, 90), mat="RedDeep", bevel=0.006)
    # --- cabina
    _box("Cabina", (0.2, 0.17, 0.2), loc=(0, -0.0, 0.23), mat="RedDeep", bevel=0.018)
    for sx in (-1, 1):
        _box("Finestra%d" % sx, (0.01, 0.085, 0.075), loc=(sx * 0.099, 0.0, 0.265), mat="EyeDark", bevel=0.012, segments=3)
        _box("Cornice%d" % sx, (0.008, 0.1, 0.09), loc=(sx * 0.097, 0.0, 0.265), mat="Cream", bevel=0.01, segments=2)
        for yy in (-0.07, 0.07):
            k.sphere("Rivetto%d%.2f" % (sx, yy), 0.006, loc=(sx * 0.101, yy, 0.16), mat="Gold", segments=8, rings=5)
    _box("FinestraDavanti", (0.11, 0.01, 0.06), loc=(0, -0.084, 0.28), mat="EyeDark", bevel=0.01)
    # tetto arcuato che sporge
    _box("Tetto", (0.24, 0.215, 0.026), loc=(0, 0.0, 0.343), mat="Cream", bevel=0.011)
    _box("TettoAlto", (0.17, 0.16, 0.022), loc=(0, 0.0, 0.363), mat="Cream", bevel=0.01)
    # --- vagone con assi
    _box("Vagone", (0.2, 0.36, 0.12), loc=(0, 0.27, 0.175), mat="GreenElf", bevel=0.016)
    for i, yy in enumerate((0.17, 0.27, 0.37)):
        for sx in (-1, 1):
            _box("Asse%d%d" % (i, sx), (0.006, 0.006, 0.1), loc=(sx * 0.1, yy - 0.05 if i else yy - 0.05, 0.175), mat="GreenDeep", bevel=0.002, segments=1)
    _box("Bordo", (0.215, 0.375, 0.022), loc=(0, 0.27, 0.238), mat="GreenDeep", bevel=0.008)
    # carico: cubetti giocattolo
    for (pos, size, mat, rz) in [((0, 0.18, 0.29), 0.088, "ClothYellow", 12), ((0.01, 0.3, 0.29), 0.088, "ClothBlue", -9),
                                 ((-0.005, 0.245, 0.375), 0.078, "ClothPink", 25)]:
        _box("Cubetto" + mat, (size, size, size), loc=pos, rot=(0, 0, rz), mat=mat, bevel=0.014)
        a = math.radians(rz)
        off = Vector((math.sin(a) * (size / 2 + 0.001), -math.cos(a) * (size / 2 + 0.001), 0))
        k.sphere("Pallino" + mat, 0.014, loc=(pos[0] + off.x, pos[1] + off.y, pos[2]), rot=(0, 0, rz), scale=(1, 0.3, 1), mat="Cream", segments=12, rings=6)
    # gancio tra locomotiva e vagone
    k.cylinder("Gancio", 0.012, 0.12, loc=(0, 0.11, 0.125), rot=(90, 0, 0), mat="Gold", bevel=0.003, verts=10)
    # --- ruote e biella
    for i, yy in enumerate((-0.38, -0.2, -0.02, 0.18, 0.36)):
        rad = 0.07 if i < 3 else 0.06
        for sx in (-1, 1):
            _wheel("Ruota%d%d" % (i, sx), sx * 0.115, yy, rad, sx)
    for sx in (-1, 1):
        _box("Biella%d" % sx, (0.008, 0.37, 0.014), loc=(sx * 0.142, -0.2, 0.072), mat="MetalLight", bevel=0.004, segments=1)
    return {"ao_distance": 0.08, "ground": 0.0, "preview_yaw": 55}


MODELS = {
    "Item_Log": _model_log,
    "Item_Wood": _model_bundle,
    "Item_Train": _model_train,
}
