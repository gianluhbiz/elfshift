# -*- coding: utf-8 -*-
"""
Modelli del gruppo "decorazioni" (Assets/Editor/Generation/DecorModels.cs):
giocattoli (orsetto, robot, tamburo, trottola, cubi, pallone), camino con fiamme, poltrona,
tavolino, tavolo espositore, lampadario, orologio a muro, bandierine.

Convenzione: Blender Z in alto, davanti verso -Y. Per comodità molte misure sono scritte nelle
coordinate di Unity (come nel codice C#) e convertite con U(x, y, z) -> (-x, -z, y).
"""
import math
import random

import bpy  # noqa: F401  (serve prima di bmesh)
import bmesh
from mathutils import Matrix, Vector

import elfkit as k


# ============================================================================ aiuti
def U(x, y, z):
    """Coordinate di Unity (Y su, davanti +Z) -> Blender (Z su, davanti -Y)."""
    return (-x, -z, y)


def _orient(obj, p0, p1):
    """Ruota/sposta un oggetto costruito lungo +Z (da 0 a L) perché vada da p0 a p1."""
    p0, p1 = Vector(p0), Vector(p1)
    d = p1 - p0
    obj.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    obj.location = p0
    return obj


def capsule(name, p0, p1, r0, r1=None, mat="CookieBrown", seg=20, tint=0.02, steps=6):
    """Capsula morbida (estremi arrotondati) da p0 a p1, raggio r0 -> r1."""
    r1 = r0 if r1 is None else r1
    L = (Vector(p1) - Vector(p0)).length
    prof = []
    for i in range(steps + 1):  # semisfera in basso
        a = -math.pi / 2 + (math.pi / 2) * i / steps
        prof.append((r0 * math.cos(a) if i > 0 else 0.0, r0 * math.sin(a) + 0.0))
    for i in range(1, steps + 1):  # semisfera in alto
        a = (math.pi / 2) * i / steps
        r = r1 * math.cos(a)
        prof.append((r if i < steps else 0.0, L + r1 * math.sin(a)))
    obj = k.lathe(name, prof, mat=mat, segments=seg, tint=tint)
    return _orient(obj, p0, p1)


def lumpy(obj, amount=0.01, scale=0.2, seed=0):
    """Deformazione irregolare SENZA suddivisione (per pietre e oggetti con tanti pezzi)."""
    tex = bpy.data.textures.new(obj.name + "_grumi", "CLOUDS")
    tex.noise_scale = scale
    d = obj.modifiers.new("Grumi", "DISPLACE")
    d.texture = tex
    d.strength = amount
    d.mid_level = 0.5
    d.texture_coords = "OBJECT"
    empty = bpy.data.objects.new(obj.name + "_seme", None)
    bpy.context.scene.collection.objects.link(empty)
    empty.location = (seed * 3.17 + 11, seed * 1.91 - 7, seed * 2.53 + 3)
    d.texture_coords_object = empty
    return obj


def smooth_sub(obj, levels=2):
    m = obj.modifiers.new("Morbido", "SUBSURF")
    m.levels = levels
    m.render_levels = levels
    return obj


def sector_materials(obj, mats, n):
    """Colora un oggetto a spicchi alterni (attorno all'asse Z) con i materiali indicati."""
    me = obj.data
    for m in mats:
        if m not in [x.name for x in me.materials]:
            me.materials.append(k.material(m))
    names = [x.name for x in me.materials]
    for p in me.polygons:
        a = (math.atan2(p.center.y, p.center.x) + math.pi) / (2 * math.pi)
        me.polygons[p.index].material_index = names.index(mats[int(a * n) % len(mats)])
    return obj


def teardrop(name, height, radius, mat, bend=0.0, seg=14, tint=0.0):
    """Fiammella / goccia: base tonda, punta in alto, con piega laterale (bend, in metri)."""
    prof = [(0.0, 0.0)]
    n = 10
    for i in range(1, n + 1):
        t = i / n
        r = radius * math.sin(math.pi * min(1.0, t * 1.15)) * (1.0 - t) ** 0.6 if t < 1 else 0.0
        prof.append((max(0.0, r) if i < n else 0.0, height * t))
    obj = k.lathe(name, prof, mat=mat, segments=seg, tint=tint)
    if bend:
        for v in obj.data.vertices:
            t = v.co.z / height
            v.co.x += bend * t * t
    return obj


def leaf_outline(length, width, spikes=3):
    """Sagoma di foglia di agrifoglio (punte sul bordo), nel piano X/Z, punta verso +X."""
    pts = []
    n = spikes * 2
    for i in range(n + 1):
        t = i / n
        x = t * length
        w = width * math.sin(math.pi * t) * (1.25 if i % 2 == 1 else 0.85)
        pts.append((x, w * 0.5))
    for i in range(n - 1, 0, -1):
        t = i / n
        x = t * length
        w = width * math.sin(math.pi * t) * (1.25 if i % 2 == 1 else 0.85)
        pts.append((x, -w * 0.5))
    return pts


def holly(prefix, center, scale=1.0, rot_z=0.0, plane="front"):
    """Rametto di agrifoglio: due/tre foglie e tre bacche rosse. plane = 'front' (verticale, verso -Y) o 'top'."""
    cx, cy, cz = center
    objs = []
    for i, ang in enumerate((25.0, 155.0, 270.0)):
        o = k.extrude_outline(prefix + "_foglia%d" % i, leaf_outline(0.16 * scale, 0.075 * scale), 0.012 * scale,
                              mat="GreenElf", bevel=0.0, tint=0.04)
        # sagoma nel piano X/Z -> la ruotiamo attorno a Y (asse "di profondità")
        o.rotation_euler = (0, math.radians(-ang - rot_z), 0)
        if plane == "top":
            o.rotation_euler = (math.radians(90), math.radians(-ang - rot_z), 0)
        o.location = (cx, cy, cz)
        objs.append(o)
    for j in range(3):
        a = j * 2.1 + 0.4
        off = (math.cos(a) * 0.025 * scale, 0, math.sin(a) * 0.025 * scale)
        if plane == "top":
            off = (off[0], off[2], 0)
        b = k.sphere(prefix + "_bacca%d" % j, 0.022 * scale, loc=(cx + off[0], cy + off[1] - (0.012 * scale if plane == "front" else 0), cz + off[2] + (0.012 * scale if plane == "top" else 0)),
                     mat="RedSanta", segments=8, rings=5)
        objs.append(b)
    return objs


# ============================================================================ GIOCATTOLI
def teddy(fur):
    """Orsetto seduto, alto ~0,6 m, origine a terra al centro. Davanti -Y."""
    def build():
        dark = "WoodDeep"
        # corpo a pera (tornio) un po' schiacciato davanti/dietro
        body = k.lathe("Corpo", [(0.0, 0.03), (0.12, 0.05), (0.17, 0.14), (0.165, 0.24), (0.13, 0.32), (0.07, 0.37), (0.0, 0.38)],
                       mat=fur, segments=14)
        body.scale = (1.0, 0.9, 1.0)
        smooth_sub(body, 1)
        k.sphere("Pancia", 0.085, loc=(0, -0.128, 0.2), scale=(1.0, 0.36, 1.12), mat="Cream", segments=10, rings=6)
        # testa e muso
        k.sphere("Testa", 0.135, loc=(0, -0.01, 0.47), scale=(1.08, 0.95, 0.95), mat=fur, segments=16, rings=10)
        k.sphere("Muso", 0.062, loc=(0, -0.11, 0.435), scale=(1.1, 0.8, 0.78), mat="Cream", segments=10, rings=6)
        k.sphere("Naso", 0.024, loc=(0, -0.158, 0.458), scale=(1.35, 0.8, 0.85), mat="Black", segments=10, rings=6)
        k.sphere("NasoLuce", 0.007, loc=(0.006, -0.176, 0.466), mat="White", segments=6, rings=4)
        # bocca: due archetti cuciti
        for s in (-1, 1):
            pts = [(0, -0.162, 0.432), (s * 0.012, -0.166, 0.418), (s * 0.03, -0.158, 0.42)]
            k.tube("Bocca%d" % s, pts, 0.0035, mat=dark, sides=4)
        k.tube("Cucitura", [(0, -0.163, 0.445), (0, -0.165, 0.432)], 0.0035, mat=dark, sides=4)
        # occhi a bottone con riflesso
        for s in (-1, 1):
            k.sphere("Occhio%d" % s, 0.019, loc=(s * 0.052, -0.118, 0.5), scale=(1, 0.6, 1.1), mat="Black", segments=10, rings=6)
            k.sphere("Luce%d" % s, 0.0055, loc=(s * 0.052 + 0.006, -0.13, 0.508), mat="White", segments=6, rings=4)
            # orecchie: disco morbido + interno chiaro
            ear = k.sphere("Orecchio%d" % s, 0.052, loc=(s * 0.1, 0.0, 0.575), scale=(1.0, 0.55, 1.0), mat=fur, segments=10, rings=6)
            ear.rotation_euler = (0, math.radians(-s * 20), 0)
            inner = k.sphere("OrecchioIn%d" % s, 0.032, loc=(s * 0.1 + s * -0.004, -0.024, 0.572), scale=(1.0, 0.35, 1.0), mat="Cream", segments=10, rings=6)
            inner.rotation_euler = (0, math.radians(-s * 20), 0)
            # braccia: capsule che scendono in avanti
            capsule("Braccio%d" % s, (s * 0.15, -0.03, 0.3), (s * 0.175, -0.085, 0.2), 0.054, 0.06, mat=fur, seg=10, steps=4)
            pw = k.sphere("Zampa%d" % s, 0.036, loc=(s * 0.18, -0.13, 0.185), scale=(1, 0.45, 1), mat="Cream", segments=10, rings=6)
            pw.rotation_euler = (math.radians(-35), 0, 0)
            # gambe distese in avanti con pianta del piede chiara
            capsule("Gamba%d" % s, (s * 0.095, -0.02, 0.07), (s * 0.115, -0.15, 0.065), 0.068, 0.072, mat=fur, seg=10, steps=4)
            pad = k.sphere("Pianta%d" % s, 0.048, loc=(s * 0.115, -0.218, 0.07), scale=(1, 0.35, 1.05), mat="Cream", segments=10, rings=6)
        # fiocco rosso al collo: due anse + nodo + codine
        for s in (-1, 1):
            loop = k.torus("Ansa%d" % s, 0.03, 0.013, loc=(s * 0.036, -0.12, 0.345), mat="RedSanta", major_seg=10, minor_seg=5)
            loop.rotation_euler = (math.radians(90), math.radians(s * 15), 0)
            loop.scale = (1.25, 1.0, 0.75)
            k.tube("Coda%d" % s, [(s * 0.01, -0.13, 0.335), (s * 0.03, -0.14, 0.3), (s * 0.04, -0.138, 0.275)], 0.011, mat="RedSanta", sides=6, radius_end=0.007)
        k.sphere("Nodo", 0.018, loc=(0, -0.135, 0.343), mat="RedDeep", segments=10, rings=6)
        k.torus("Collare", 0.105, 0.012, loc=(0, -0.01, 0.348), mat="RedSanta", major_seg=16, minor_seg=4).scale = (1.0, 0.92, 1.0)
        return {"ao_distance": 0.12, "ao_samples": 32, "sharp_angle": 75.0}
    return build


def robot(body):
    """Robot di latta a molla, alto ~0,7 m con antenna. Davanti -Y."""
    def build():
        trim = "MetalDark"
        # piedi arrotondati con punta
        for s in (-1, 1):
            k.box("Piede%d" % s, (0.095, 0.13, 0.06), loc=(s * 0.07, -0.012, 0.03), mat=trim, bevel=0.02, segments=2)
            k.cylinder("Caviglia%d" % s, 0.028, 0.08, loc=(s * 0.07, 0, 0.09), mat="MetalLight", bevel=0.006, verts=10, segments=1)
            for r in range(2):  # gambe a fisarmonica
                k.torus("Molla%d%d" % (s, r), 0.03, 0.011, loc=(s * 0.07, 0, 0.075 + r * 0.032), mat=trim, major_seg=10, minor_seg=4)
        # corpo
        k.box("Corpo", (0.24, 0.17, 0.28), loc=(0, 0, 0.27), mat=body, bevel=0.035, segments=2)
        k.box("Cintura", (0.25, 0.18, 0.03), loc=(0, 0, 0.15), mat="Gold", bevel=0.012, segments=1)
        k.box("Pannello", (0.15, 0.02, 0.11), loc=(0, -0.088, 0.3), mat="Cream", bevel=0.008, segments=1)
        k.box("Schermo", (0.11, 0.02, 0.05), loc=(0, -0.095, 0.315), mat="RedSanta", bevel=0.006, segments=1)
        for i, c in enumerate(("Gold", "BulbGreen", "Gold")):
            k.cylinder("Bottone%d" % i, 0.011, 0.012, loc=(-0.035 + i * 0.035, -0.098, 0.27), rot=(90, 0, 0), mat=c, bevel=0.0, verts=10)
        for sx in (-1, 1):  # rivetti ai lati del pannello
            for sz in (-1, 1):
                k.sphere("Rivetto%d%d" % (sx, sz), 0.007, loc=(sx * 0.098, -0.086, 0.3 + sz * 0.105), mat="MetalLight", segments=6, rings=4)
        # chiave a molla sulla schiena
        k.cylinder("Perno", 0.012, 0.05, loc=(0, 0.105, 0.29), rot=(90, 0, 0), mat="Gold", verts=10, bevel=0.003)
        for s in (-1, 1):
            w = k.torus("Chiave%d" % s, 0.028, 0.009, loc=(s * 0.032, 0.13, 0.29), mat="Gold", major_seg=10, minor_seg=4)
            w.rotation_euler = (0, math.radians(90), 0)
        # braccia a fisarmonica e mani a pinza
        for s in (-1, 1):
            k.sphere("Spalla%d" % s, 0.032, loc=(s * 0.135, 0, 0.37), mat=trim, segments=8, rings=5)
            for r in range(3):
                t = k.torus("Braccio%d%d" % (s, r), 0.026, 0.011, loc=(s * (0.15 + r * 0.011), -0.006 * r, 0.33 - r * 0.044), mat="MetalLight", major_seg=10, minor_seg=4)
                t.rotation_euler = (0, math.radians(s * 12), 0)
            claw = k.torus("Pinza%d" % s, 0.026, 0.009, loc=(s * 0.19, -0.02, 0.19), mat=trim, major_seg=10, minor_seg=4)
            claw.rotation_euler = (math.radians(90), 0, math.radians(90))
        # collo e testa
        k.cylinder("Collo", 0.04, 0.04, loc=(0, 0, 0.425), mat=trim, verts=18, bevel=0.006)
        k.box("Testa", (0.2, 0.15, 0.15), loc=(0, 0, 0.5), mat="MetalLight", bevel=0.035, segments=2)
        for s in (-1, 1):
            k.cylinder("Orecchio%d" % s, 0.03, 0.03, loc=(s * 0.11, 0, 0.5), rot=(0, 90, 0), mat="RedSanta", bevel=0.006, verts=10, segments=1)
            k.cylinder("OrecchioPunta%d" % s, 0.01, 0.03, loc=(s * 0.13, 0, 0.5), rot=(0, 90, 0), mat="Gold", bevel=0.0, verts=8)
            # occhi luminosi in una cornice
            k.torus("Cornice%d" % s, 0.026, 0.006, loc=(s * 0.05, -0.076, 0.52), mat=trim, major_seg=10, minor_seg=4).rotation_euler = (math.radians(90), 0, 0)
            k.sphere("Occhio%d" % s, 0.022, loc=(s * 0.05, -0.072, 0.52), scale=(1, 0.6, 1), mat="BulbBlue", segments=8, rings=5)
        k.box("Bocca", (0.1, 0.02, 0.026), loc=(0, -0.076, 0.465), mat="EyeDark", bevel=0.006, segments=1)
        for i in range(4):
            k.box("Dente%d" % i, (0.004, 0.022, 0.022), loc=(-0.03 + i * 0.02, -0.078, 0.465), mat="MetalLight", bevel=0.0)
        # antenna
        k.cylinder("Antenna", 0.007, 0.09, loc=(0, 0, 0.62), mat=trim, bevel=0.0, verts=8)
        k.sphere("Pallina", 0.024, loc=(0, 0, 0.675), mat="RedSanta", segments=8, rings=5)
        return {"ao_distance": 0.1, "ao_samples": 32}
    return build


def drum():
    """Tamburo giocattolo: raggio 0,15, alto 0,2. Origine al centro della base."""
    def build():
        k.lathe("Fusto", [(0.0, 0.012), (0.145, 0.012), (0.152, 0.1), (0.145, 0.188), (0.0, 0.188)], mat="RedSanta", segments=24)
        k.cylinder("Pelle", 0.143, 0.014, loc=(0, 0, 0.197), mat="Cream", bevel=0.004, verts=24, segments=1)
        k.cylinder("PelleSotto", 0.143, 0.014, loc=(0, 0, 0.004), mat="Cream", bevel=0.0, verts=24)
        for z in (0.014, 0.19):
            k.torus("Cerchio%.2f" % z, 0.152, 0.013, loc=(0, 0, z), mat="Gold", major_seg=28, minor_seg=5)
        # corde a zig-zag
        n = 10
        for i in range(n):
            a0 = 2 * math.pi * i / n
            a1 = 2 * math.pi * (i + 0.5) / n
            a2 = 2 * math.pi * (i + 1) / n
            r = 0.157
            pts = [(r * math.cos(a0), r * math.sin(a0), 0.03), (r * 1.01 * math.cos(a1), r * 1.01 * math.sin(a1), 0.172), (r * math.cos(a2), r * math.sin(a2), 0.03)]
            k.tube("Corda%d" % i, pts, 0.0035, mat="Snow", sides=4)
            k.sphere("Borchia%d" % i, 0.008, loc=(r * 1.01 * math.cos(a1), r * 1.01 * math.sin(a1), 0.172), mat="Gold", segments=6, rings=4)
        for s, (ax, ay) in enumerate(((-0.04, -0.03), (0.05, 0.0))):
            ang = 20 if s == 0 else -25
            stick = capsule("Bacchetta%d" % s, (ax - 0.09 * math.cos(math.radians(ang)), ay - 0.09 * math.sin(math.radians(ang)), 0.212),
                            (ax + 0.09 * math.cos(math.radians(ang)), ay + 0.09 * math.sin(math.radians(ang)), 0.222), 0.008, 0.008, mat="WoodLight", seg=8, steps=3)
            k.sphere("Pomo%d" % s, 0.016, loc=(ax + 0.1 * math.cos(math.radians(ang)), ay + 0.1 * math.sin(math.radians(ang)), 0.224), mat="Cream", segments=8, rings=5)
        return {"ao_distance": 0.08, "ao_samples": 32}
    return build


def spin_top(color):
    """Trottola: punta a terra, alta ~0,2. Origine sulla punta."""
    def build():
        s_bottom = k.lathe("Fondo", [(0.0, 0.0), (0.012, 0.006), (0.05, 0.04), (0.09, 0.085), (0.1, 0.105), (0.0, 0.105)], mat=color, segments=36)
        k.torus("Fascia", 0.1, 0.012, loc=(0, 0, 0.108), mat="ClothBlue", major_seg=40, minor_seg=8)
        s_dome = k.lathe("Cupola", [(0.0, 0.105), (0.098, 0.11), (0.092, 0.125), (0.075, 0.142), (0.045, 0.153), (0.0, 0.156)], mat="Snow", segments=36)
        sector_materials(s_dome, (color, "Snow"), 12)
        sector_materials(s_bottom, (color, "ClothYellow"), 12)
        k.cylinder("Manico", 0.016, 0.06, loc=(0, 0, 0.18), mat="Gold", bevel=0.005, verts=16)
        k.sphere("PomoManico", 0.022, loc=(0, 0, 0.215), mat="Gold", segments=16, rings=8)
        return {"ao_distance": 0.06, "ao_samples": 32}
    return build


def blocks():
    """Tre cubi con lettere/figure in rilievo: due sotto, uno sopra. Lato 0,11 (scala 1)."""
    def build():
        s = 0.11
        colors = ("ClothYellow", "ClothBlue", "RedSanta")
        frames = ("Cream", "Cream", "Cream")
        placements = [((-0.5 * s * 1.1, 0.5 * s), -7), ((0.5 * s * 1.1, 0.5 * s), 7), ((0.0, 1.5 * s), 18)]
        shapes = ("stella", "cuore", "cerchio")
        for i, ((ux, uy), yaw) in enumerate(placements):
            parts = []
            cube = k.box("Cubo%d" % i, (s, s, s), mat=frames[i], bevel=0.012, segments=2)
            parts.append(cube)
            # facce colorate un filo rientrate: cornice chiara + riquadro colorato
            for f, (nx, ny, nz) in enumerate(((0, -1, 0), (0, 1, 0), (1, 0, 0), (-1, 0, 0), (0, 0, 1))):
                size = (s * 0.78, 0.008, s * 0.78) if ny else ((0.008, s * 0.78, s * 0.78) if nx else (s * 0.78, s * 0.78, 0.008))
                pl = k.box("Faccia%d%d" % (i, f), size, loc=(nx * s * 0.5, ny * s * 0.5, nz * s * 0.5), mat=colors[(i + f) % 3], bevel=0.0)
                parts.append(pl)
            # figura in rilievo sul davanti
            if shapes[i] == "stella":
                outline = []
                for j in range(10):
                    a = math.pi / 2 + j * math.pi / 5
                    r = 0.034 if j % 2 == 0 else 0.015
                    outline.append((r * math.cos(a), r * math.sin(a)))
            elif shapes[i] == "cuore":
                outline = []
                for j in range(16):
                    t = 2 * math.pi * j / 16
                    x = 16 * math.sin(t) ** 3
                    y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
                    outline.append((x * 0.0019, y * 0.0019 + 0.004))
                outline.reverse()
            else:
                outline = [(0.028 * math.cos(2 * math.pi * j / 20), 0.028 * math.sin(2 * math.pi * j / 20)) for j in range(20)]
            fig = k.extrude_outline("Figura%d" % i, outline, 0.008, loc=(0, -s * 0.5 - 0.006, 0), mat="Snow", bevel=0.0)
            parts.append(fig)
            for p in parts:
                p.location = Matrix.Rotation(math.radians(-yaw), 4, "Z") @ p.location
                p.location += Vector(U(ux, uy, 0))
                p.rotation_euler.z += math.radians(-yaw)
        return {"ao_distance": 0.06, "ao_samples": 32}
    return build


def beach_ball():
    """Pallone a spicchi: raggio 0,16, appoggiato a terra (origine sotto)."""
    def build():
        r = 0.16
        colors = ("ClothPink", "Snow", "ClothYellow", "Snow", "ClothBlue", "Snow")
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=36, v_segments=16, radius=r)
        me = bpy.data.meshes.new("Pallone")
        bm.to_mesh(me)
        bm.free()
        obj = bpy.data.objects.new("Pallone", me)
        bpy.context.scene.collection.objects.link(obj)
        for c in colors:
            if k.material(c).name not in [m.name for m in me.materials]:
                me.materials.append(k.material(c))
        names = [m.name for m in me.materials]
        for p in me.polygons:
            p.use_smooth = True
            c = p.center
            a = (math.atan2(c.y, c.x) + math.pi) / (2 * math.pi)
            p.material_index = names.index(colors[int(a * 6) % 6])
        obj.location = (0, 0, r)
        obj.rotation_euler = (math.radians(20), math.radians(10), 0)
        obj["tint"] = (1, 1, 1)
        for sz in (-1, 1):
            cap = k.sphere("Tappo%d" % sz, r * 0.2, loc=(0, 0, 0), scale=(1, 1, 0.35), mat="Snow", segments=12, rings=4)
            cap.parent = obj
            cap.location = (0, 0, sz * r * 0.975)
        return {"ao_distance": 0.08, "ao_samples": 32, "sharp_angle": 75.0}
    return build


# ============================================================================ CAMINO
def ubox(name, center, size, mat, bevel=0.02, segments=2, tint=0.03):
    """Scatola con centro e misure date in coordinate di Unity."""
    return k.box(name, (size[0], size[2], size[1]), loc=U(*center), mat=mat, bevel=bevel, segments=segments, tint=tint)


def stone(name, center_b, size_b, mat="Stone", seed=0, bevel_frac=0.32, segments=2):
    o = k.box(name, size_b, loc=center_b, mat=mat, bevel=min(size_b) * bevel_frac, segments=segments, tint=0.07)
    lumpy(o, min(size_b) * 0.18, 0.08, seed)
    return o


STONE_MIX = ("StoneWarm", "StoneWarm", "StoneLight", "Stone")


def stone_face(prefix, origin, u_dir, v_dir, n_dir, width, height, mat=None, sw=(0.24, 0.36), sh=(0.17, 0.24), depth=0.09, seed=0, segments=2):
    """Riempie un rettangolo (origine = angolo, u = larghezza, v = altezza, n = normale verso fuori)
    con file di pietre arrotondate di misura irregolare. Tutto in coordinate di Blender."""
    rnd = random.Random(seed)
    o, u, v, n = Vector(origin), Vector(u_dir).normalized(), Vector(v_dir).normalized(), Vector(n_dir).normalized()
    y = 0.0
    row = 0
    out = []
    while y < height - 0.05:
        h = min(rnd.uniform(*sh), height - y)
        if height - y - h < 0.1:
            h = height - y
        x = -rnd.uniform(0, sw[0] * 0.5) if row % 2 else 0.0
        while x < width - 0.04:
            w = rnd.uniform(*sw)
            x0, x1 = max(0.0, x), min(width, x + w)
            if width - x1 < 0.09:
                x1 = width
            cw, ch = x1 - x0 - 0.025, h - 0.025
            if cw > 0.05:
                c = o + u * ((x0 + x1) / 2) + v * (y + h / 2) + n * (depth * 0.3 + rnd.uniform(-0.01, 0.012))
                # misure nel sistema u/v/n -> assi del mondo (le facce sono allineate agli assi)
                size = [0.0, 0.0, 0.0]
                for vec, val in ((u, cw), (v, ch), (n, depth)):
                    ax = max(range(3), key=lambda i: abs(vec[i]))
                    size[ax] = val
                st = stone(prefix + "_%d_%d" % (row, len(out)), tuple(c), tuple(size), mat=mat or rnd.choice(STONE_MIX), seed=seed * 100 + len(out), segments=segments)
                st.rotation_euler = (n.x * rnd.uniform(-0.03, 0.03), n.y * rnd.uniform(-0.03, 0.03), n.z * rnd.uniform(-0.03, 0.03))
                out.append(st)
            x = x1
        y += h
        row += 1
    return out


def log(name, center_u, radius, length, yaw=0.0, seed=0):
    """Ceppo con corteccia irregolare e sezioni chiare con anelli. Asse lungo X di Unity."""
    parts = []
    bark = k.cylinder(name + "_corteccia", radius, length, mat="Bark", bevel=radius * 0.25, verts=18, segments=2, tint=0.06)
    lumpy(bark, radius * 0.18, 0.06, seed)
    parts.append(bark)
    for sgn in (-1, 1):
        cap = k.cylinder(name + "_taglio%d" % sgn, radius * 0.86, 0.02, loc=(0, 0, sgn * (length / 2 - 0.004)), mat="WoodEnd", bevel=0.004, verts=18, tint=0.03)
        ring = k.torus(name + "_anello%d" % sgn, radius * 0.45, 0.006, loc=(0, 0, sgn * (length / 2 + 0.006)), mat="WoodMid", major_seg=18, minor_seg=4)
        dot = k.cylinder(name + "_cuore%d" % sgn, radius * 0.14, 0.012, loc=(0, 0, sgn * (length / 2 + 0.004)), mat="WoodMid", bevel=0.002, verts=10)
        parts += [cap, ring, dot]
    rot = Matrix.Rotation(math.radians(-yaw), 4, "Z") @ Matrix.Rotation(math.radians(90), 4, "Y")
    for p in parts:
        p.matrix_world = Matrix.Translation(Vector(U(*center_u))) @ rot @ Matrix.Translation(p.location)
    return parts


def stocking(prefix, x, color, heel="Snow"):
    """Calza appesa alla mensola (coordinate Unity: in alto a y 2,2, z 0,62)."""
    z = -0.62
    top = 2.2
    path = [(-x, z, top - 0.02), (-x, z, top - 0.25), (-x, z, top - 0.48), (-x - 0.02, z, top - 0.6),
            (-x - 0.08, z, top - 0.655), (-x - 0.16, z, top - 0.665), (-x - 0.21, z, top - 0.65)]
    k.tube(prefix + "_gamba", path, 0.115, mat=color, sides=14, radius_end=0.1)
    k.sphere(prefix + "_punta", 0.1, loc=(-x - 0.21, z, top - 0.65), scale=(1, 1, 1), mat=heel, segments=14, rings=8)
    k.sphere(prefix + "_tallone", 0.085, loc=(-x + 0.05, z, top - 0.6), scale=(0.9, 1.25, 1.0), mat=heel, segments=12, rings=8)
    cuff = k.torus(prefix + "_polsino", 0.125, 0.05, loc=(-x, z, top - 0.04), mat="White", major_seg=20, minor_seg=6)
    cuff.scale = (1, 1, 1.3)
    lumpy(cuff, 0.018, 0.03, int(abs(x) * 10) + 3)
    k.torus(prefix + "_anellino", 0.02, 0.005, loc=(-x, z - 0.0, top + 0.06), rot=(90, 0, 0), mat="Gold", major_seg=12, minor_seg=5)
    k.cylinder(prefix + "_gancio", 0.012, 0.06, loc=(-x, z + 0.03, top + 0.07), rot=(90, 0, 0), mat="Gold", bevel=0.003, verts=10)
    # stella cucita sulla calza
    outline = []
    for j in range(10):
        a = math.pi / 2 + j * math.pi / 5
        r = 0.05 if j % 2 == 0 else 0.022
        outline.append((r * math.cos(a), r * math.sin(a)))
    k.extrude_outline(prefix + "_stella", outline, 0.01, loc=(-x, z - 0.115, top - 0.32), mat="Gold", bevel=0.002)


def candle(prefix, center_u):
    cx, cy, cz = center_u
    k.lathe(prefix + "_piattino", [(0.0, 0.0), (0.075, 0.0), (0.08, 0.012), (0.07, 0.02), (0.0, 0.02)], loc=U(cx, cy, cz), mat="Gold", segments=24)
    body = k.lathe(prefix + "_cera", [(0.0, 0.02), (0.045, 0.02), (0.045, 0.26), (0.04, 0.28), (0.012, 0.283), (0.0, 0.283)], loc=U(cx, cy, cz), mat="Cream", segments=20)
    lumpy(body, 0.004, 0.02, int(cx * 10) + 7)
    for i, a in enumerate((0.4, 2.3, 4.4)):
        h = 0.06 + 0.04 * i
        dx, dz = 0.045 * math.cos(a), 0.045 * math.sin(a)
        capsule(prefix + "_goccia%d" % i, (-(cx) - dx, -(cz) - dz, cy + 0.27), (-(cx) - dx * 1.04, -(cz) - dz * 1.04, cy + 0.27 - h), 0.011, 0.014, mat="Cream", seg=8)
    k.cylinder(prefix + "_stoppino", 0.004, 0.03, loc=U(cx, cy + 0.295, cz), mat="Black", bevel=0, verts=6)
    fl = teardrop(prefix + "_fiamma", 0.09, 0.03, "LampGlow", bend=0.006, seg=12)
    fl.location = U(cx, cy + 0.297, cz)


def fireplace_body():
    def build():
        # --- nucleo di malta (le pietre ci stanno davanti) e fondo del focolare
        ubox("NucleoSx", (-1.45, 1.2, -0.02), (0.64, 2.1, 1.02), "PlasterDark", bevel=0.02)
        ubox("NucleoDx", (1.45, 1.2, -0.02), (0.64, 2.1, 1.02), "PlasterDark", bevel=0.02)
        ubox("NucleoArco", (0.0, 2.02, -0.02), (3.56, 0.46, 1.02), "PlasterDark", bevel=0.02)
        ubox("Fondo", (0.0, 0.95, -0.45), (2.3, 1.85, 0.12), "Soot", bevel=0.02)
        ubox("PavimentoFocolare", (0.0, 0.04, 0.05), (2.2, 0.08, 1.0), "Soot", bevel=0.02)
        # mattoncini anneriti sul fondo
        rnd = random.Random(5)
        for r in range(11):
            off = 0.0 if r % 2 == 0 else 0.19
            for c in range(6):
                x = -0.95 + off + c * 0.38
                if x > 1.0:
                    continue
                b = ubox("Fuliggine_%d_%d" % (r, c), (x, 0.14 + r * 0.16, -0.38), (0.35, 0.14, 0.04), "Soot", bevel=0.0, tint=0.15)
                b.location.y += rnd.uniform(-0.004, 0.004)
        # --- pietre: fronte, fianchi interni ed esterni dei pilastri, architrave
        sd = 1
        for side in (-1, 1):
            xin, xout = side * 1.1, side * 1.8
            # fronte (z = +0.55 Unity -> y = -0.55 Blender)
            x0 = min(-xin, -xout)
            stone_face("PietraF%d" % side, (x0, -0.53, 0.0), (1, 0, 0), (0, 0, 1), (0, -1, 0), 0.7, 1.76, seed=sd)
            sd += 1
            # fianco interno (verso l'apertura) ed esterno
            stone_face("PietraI%d" % side, (-xin, 0.5, 0.0), (0, -1, 0), (0, 0, 1), (side, 0, 0), 1.03, 1.76, sw=(0.34, 0.52), sh=(0.22, 0.3), seed=sd, depth=0.07, segments=1)
            sd += 1
            stone_face("PietraE%d" % side, (-xout, 0.5, 0.0), (0, -1, 0), (0, 0, 1), (-side, 0, 0), 1.03, 2.26, sw=(0.34, 0.52), sh=(0.24, 0.32), seed=sd, segments=1)
            sd += 1
        stone_face("Architrave", (-1.8, -0.53, 1.76), (1, 0, 0), (0, 0, 1), (0, -1, 0), 3.6, 0.5, sw=(0.3, 0.5), sh=(0.24, 0.26), seed=sd)
        # chiave di volta al centro dell'architrave
        stone("ChiaveVolta", (0.0, -0.6, 1.98), (0.34, 0.12, 0.42), mat="StoneLight", seed=77)
        # --- focolare: lastre di pietra davanti
        rnd = random.Random(9)
        x = -2.0
        i = 0
        while x < 2.0:
            w = rnd.uniform(0.7, 1.1) if x + 1.1 < 2.0 else 2.0 - x
            stone("Lastra%d" % i, (-(x + w / 2), -1.05, 0.08 + rnd.uniform(-0.006, 0.006)), (w - 0.03, 1.08, 0.15), mat=("StoneWarm", "StoneLight")[i % 2], seed=200 + i, bevel_frac=0.25)
            x += w
            i += 1
        # --- mensola di legno massiccio con modanatura e mensoline
        beam = ubox("Mensola", (0.0, 2.38, 0.12), (4.0, 0.22, 1.3), "WoodDark", bevel=0.04, segments=3)
        ubox("Modanatura", (0.0, 2.245, 0.74), (3.8, 0.06, 0.08), "WoodMid", bevel=0.02)
        ubox("ModanaturaFine", (0.0, 2.505, 0.775), (4.02, 0.03, 0.03), "WoodMid", bevel=0.01)
        bracket = [(0.0, 0.0), (0.0, -0.3), (0.06, -0.3), (0.06, -0.2), (0.1, -0.15), (0.16, -0.12), (0.2, -0.06), (0.22, 0.0)]
        for side in (-1, 1):
            o = k.extrude_outline("Mensolina%d" % side, [(-px, pz) for (px, pz) in bracket][::-1], 0.14, mat="WoodDark", bevel=0.01)
            o.rotation_euler = (0, 0, math.radians(90))
            o.location = U(side * 1.78, 2.27, 0.55)
        # --- legna nel focolare, alari e braci
        for i, (c, r, l, yaw) in enumerate((((-0.35, 0.2, 0.05), 0.11, 0.9, 12.0), ((0.3, 0.2, -0.05), 0.1, 0.85, -15.0), ((0.0, 0.38, 0.0), 0.1, 0.8, 3.0))):
            log("Ceppo%d" % i, c, r, l, yaw, seed=30 + i)
        for side in (-1, 1):
            k.box("Alare%d" % side, (0.05, 0.6, 0.05), loc=U(side * 0.6, 0.12, 0.05), mat="Iron", bevel=0.012)
            k.cylinder("AlarePalo%d" % side, 0.025, 0.28, loc=U(side * 0.6, 0.24, 0.36), mat="Iron", bevel=0.006, verts=12)
            k.sphere("AlarePomo%d" % side, 0.04, loc=U(side * 0.6, 0.4, 0.36), mat="Gold", segments=14, rings=8)
        for i in range(14):
            a = rnd.uniform(-0.75, 0.75)
            e = k.sphere("Brace%d" % i, rnd.uniform(0.03, 0.055), loc=U(a, 0.08, rnd.uniform(-0.25, 0.3)), scale=(1.2, 1.0, 0.55),
                         mat="FireOrange" if i % 3 else "Soot", segments=8, rings=5)
            lumpy(e, 0.01, 0.03, i + 50)
        # --- ghirlanda sulla mensola con palline e fiocchi
        p0, p1, p2, p3 = Vector(U(-1.9, 2.52, 0.72)), Vector(U(-1.0, 2.2, 0.8)), Vector(U(1.0, 2.2, 0.8)), Vector(U(1.9, 2.52, 0.72))
        pts = []
        N = 48
        for i in range(N + 1):
            t = i / N
            pts.append(tuple((1 - t) ** 3 * p0 + 3 * (1 - t) ** 2 * t * p1 + 3 * (1 - t) * t * t * p2 + t ** 3 * p3))
        g = k.tube("Ghirlanda", pts, 0.085, mat="GreenPine", sides=9, tint=0.0)
        lumpy(g, 0.05, 0.035, 11)
        for i in range(0, N + 1, 2):
            pv = Vector(pts[i])
            for j in range(2):
                tuft = k.sphere("Ciuffo%d_%d" % (i, j), 0.05, loc=tuple(pv + Vector((rnd.uniform(-0.05, 0.05), rnd.uniform(-0.07, -0.02), rnd.uniform(-0.05, 0.05)))),
                                scale=(1.4, 0.6, 0.8), mat="GreenDeep" if (i + j) % 3 == 0 else "GreenPine", segments=6, rings=4, tint=0.06)
                tuft.rotation_euler = (rnd.uniform(0, 3), rnd.uniform(0, 3), rnd.uniform(0, 3))
        for i in range(3, N, 6):
            pv = Vector(pts[i])
            mat = ("RedSanta", "Gold", "RedSanta", "ClothBlue")[(i // 6) % 4]
            k.sphere("Pallina%d" % i, 0.05, loc=tuple(pv + Vector((0, -0.07, -0.06))), mat=mat, segments=14, rings=8)
            k.cylinder("Cappuccio%d" % i, 0.014, 0.02, loc=tuple(pv + Vector((0, -0.07, -0.005))), mat="Gold", bevel=0.0, verts=8)
        for sx in (-1, 1):
            pv = Vector(U(sx * 1.9, 2.52, 0.72))
            for s2 in (-1, 1):
                lp = k.torus("Fiocco%d%d" % (sx, s2), 0.05, 0.02, loc=tuple(pv + Vector((s2 * 0.055, -0.06, 0.0))), mat="RedSanta", major_seg=14, minor_seg=6)
                lp.rotation_euler = (math.radians(90), math.radians(s2 * 15), 0)
                lp.scale = (1.3, 1.0, 0.8)
            k.sphere("FioccoNodo%d" % sx, 0.03, loc=tuple(pv + Vector((0, -0.075, 0.0))), mat="RedDeep", segments=12, rings=8)
        # --- calze e candele
        for i, (x, c) in enumerate(((-1.1, "RedSanta"), (0.0, "GreenElf"), (1.1, "RedSanta"))):
            stocking("Calza%d" % i, x, c)
        for side in (-1, 1):
            candle("Candela%d" % side, (side * 1.7, 2.49, 0.2))
        return {"ao_distance": 0.35, "ao_samples": 32}
    return build


def fireplace_chimney():
    """Cappa del camino in intonaco, alta 2,51 m (y Unity da 0 a 2,51; scalata in C# fino al soffitto)."""
    def build():
        h = 2.51
        o = ubox("Cappa", (0.0, h / 2, -0.1), (2.9, h, 0.8), "Plaster", bevel=0.05, segments=3)
        k.wobble(o, 0.012, 0.6, seed=4)
        # trave decorativa e qualche mattone a vista
        tr = ubox("Fascia", (0.0, 0.12, 0.3), (2.96, 0.1, 0.06), "WoodDark", bevel=0.02)
        k.wobble(tr, 0.003, 0.4, seed=5)
        rnd = random.Random(12)
        for patch, (px, py) in enumerate(((-0.9, 1.4), (0.75, 0.75), (0.95, 1.85))):
            for r in range(3):
                for c in range(3 - abs(r - 1)):
                    x = px + (c - 1) * 0.27 + (0.135 if r % 2 else 0)
                    ubox("Mattone%d_%d_%d" % (patch, r, c), (x, py + r * 0.115, 0.3), (0.25, 0.1, 0.03), "Brick", bevel=0.012, segments=1, tint=0.1)
        return {"ao_distance": 0.2, "ao_samples": 24, "ground": None}
    return build


def fireplace_flames(inner):
    def build():
        if not inner:
            specs = (((-0.3, 0.3, 0.02), 0.2, 0.75, 0.05), ((0.28, 0.3, -0.02), 0.18, 0.62, -0.06), ((0.0, 0.38, 0.0), 0.24, 0.9, 0.03))
            mat = "FireOrange"
        else:
            specs = (((0.0, 0.38, 0.04), 0.13, 0.6, -0.03), ((-0.28, 0.3, 0.05), 0.09, 0.4, 0.04))
            mat = "FireYellow"
        for i, (c, r, h, bend) in enumerate(specs):
            f = teardrop("Fiamma%d" % i, h, r, mat, bend=bend, seg=16)
            f.location = U(*c)
            if not inner:
                for j, (dx, sc) in enumerate(((-0.6, 0.55), (0.6, 0.45))):
                    t = teardrop("Lingua%d_%d" % (i, j), h * sc, r * 0.45, mat, bend=dx * 0.1, seg=10)
                    t.location = U(c[0] + dx * r, c[1] + 0.02, c[2] + 0.03)
        return {"ao": False}
    return build


# ============================================================================ POLTRONA
def puffy(name, center_u, size_u, mat, rot=(0, 0, 0), bevel=0.04, levels=2, tint=0.02):
    """Cuscino imbottito: scatola smussata + suddivisione (forma morbida)."""
    o = ubox(name, center_u, size_u, mat, bevel=bevel, segments=1, tint=tint)
    o.rotation_euler = tuple(math.radians(a) for a in rot)
    smooth_sub(o, levels)
    return o


def rounded_rect_path(cx, cy, z, w, d, r, n=6):
    """Percorso chiuso di un rettangolo arrotondato nel piano orizzontale (Blender)."""
    pts = []
    corners = ((w / 2 - r, d / 2 - r, 0), (-w / 2 + r, d / 2 - r, 90), (-w / 2 + r, -d / 2 + r, 180), (w / 2 - r, -d / 2 + r, 270))
    for (x, y, a0) in corners:
        for i in range(n + 1):
            a = math.radians(a0 + 90 * i / n)
            pts.append((cx + x + r * math.cos(a), cy + y + r * math.sin(a), z))
    pts.append(pts[0])
    return pts


def armchair(fabric):
    dark = "RedDeep" if fabric == "RedSanta" else "GreenDeep"
    blanket_stripe = "GreenElf" if fabric == "RedSanta" else "RedSanta"

    def build():
        # piedini torniti
        for sx in (-1, 1):
            for sz in (-1, 1):
                k.lathe("Piede%d%d" % (sx, sz), [(0.0, 0.0), (0.035, 0.0), (0.05, 0.03), (0.055, 0.07), (0.045, 0.11), (0.05, 0.14), (0.06, 0.16), (0.0, 0.16)],
                        loc=U(sx * 0.37, 0.0, sz * 0.33), mat="WoodDark", segments=16)
        # base imbottita con cordoncino
        puffy("Base", (0.0, 0.27, 0.0), (0.92, 0.22, 0.84), fabric, bevel=0.05)
        ubox("Zoccolo", (0.0, 0.175, 0.0), (0.86, 0.05, 0.78), "WoodDark", bevel=0.02)
        # schienale alto e tondeggiante, leggermente inclinato
        puffy("Schienale", (0.0, 0.74, -0.31), (0.86, 0.78, 0.22), fabric, rot=(-8, 0, 0), bevel=0.08)
        puffy("SchienaleCuscino", (0.0, 0.7, -0.2), (0.62, 0.52, 0.1), fabric, rot=(-8, 0, 0), bevel=0.04)
        for r in range(2):  # bottoni capitonné
            for c in range(3):
                x = -0.18 + c * 0.18 + (0.09 if r == 1 else 0) * 0
                if r == 1 and c == 2:
                    continue
                xx = (-0.18 + c * 0.18) if r == 0 else (-0.09 + c * 0.18)
                y = 0.62 + r * 0.17
                z = -0.142 - (y - 0.7) * math.tan(math.radians(8))
                k.sphere("Bottone%d%d" % (r, c), 0.018, loc=U(xx, y, z), scale=(1, 0.6, 1), mat="Gold", segments=12, rings=6)
        # cuscino della seduta con bordino
        puffy("Seduta", (0.0, 0.44, 0.06), (0.66, 0.15, 0.64), fabric, bevel=0.04)
        k.tube("CordoneSeduta", rounded_rect_path(0, -0.06, 0.492, 0.6, 0.58, 0.12), 0.011, mat=dark, sides=8, caps=False)
        # braccioli arrotolati
        for side in (-1, 1):
            puffy("Bracciolo%d" % side, (side * 0.43, 0.48, 0.0), (0.16, 0.36, 0.8), dark, bevel=0.05)
            roll = k.cylinder("Rotolo%d" % side, 0.095, 0.84, loc=U(side * 0.46, 0.66, 0.02), rot=(90, 0, 0), mat=fabric, bevel=0.03, verts=24, segments=2)
            smooth_sub(roll, 1)
            k.cylinder("RotoloFronte%d" % side, 0.075, 0.02, loc=U(side * 0.46, 0.66, 0.445), rot=(90, 0, 0), mat=dark, bevel=0.008, verts=20)
            k.sphere("RotoloBottone%d" % side, 0.022, loc=U(side * 0.46, 0.66, 0.46), scale=(1, 0.6, 1), mat="Gold", segments=12, rings=6)
        # cuscino giallo appoggiato allo schienale
        pil = puffy("Cuscino", (-0.18, 0.66, -0.1), (0.34, 0.32, 0.1), "ClothYellow", bevel=0.025, levels=2)
        pil.rotation_euler = (math.radians(14), math.radians(-12), math.radians(-18))
        # coperta piegata sulla seduta, con due righe colorate
        for i, (dy, w) in enumerate(((0.0, 0.42), (0.045, 0.4))):
            sl = puffy("Coperta%d" % i, (0.14, 0.535 + dy, 0.14), (w, 0.05, 0.3), "Cream", bevel=0.02, levels=2)
            sl.rotation_euler = (0, 0, math.radians(-12 - i * 4))
        for j, x in enumerate((0.05, 0.22)):
            st = puffy("Riga%d" % j, (x, 0.603, 0.14), (0.05, 0.012, 0.302), blanket_stripe, bevel=0.005, levels=1)
            st.rotation_euler = (0, 0, math.radians(-16))
            st.location = Matrix.Rotation(math.radians(-16), 4, "Z") @ (st.location - Vector(U(0.14, 0.58, 0.14))) + Vector(U(0.14, 0.58, 0.14))
            st.location.z = 0.603
        return {"ao_distance": 0.25, "ao_samples": 32}
    return build


# ============================================================================ TAVOLINO E TAVOLO ESPOSITORE
def cookie(prefix, center_b, r=0.04, iced=False):
    c = k.cylinder(prefix, r, 0.014, loc=center_b, mat="CookieBrown", bevel=0.006, verts=16, tint=0.05)
    lumpy(c, 0.003, 0.02, int(abs(center_b[0]) * 1000) % 50)
    if iced:
        k.cylinder(prefix + "_glassa", r * 0.8, 0.006, loc=(center_b[0], center_b[1], center_b[2] + 0.008), mat="Snow", bevel=0.003, verts=16)
        for j in range(4):
            a = j * 1.57 + 0.3
            k.sphere(prefix + "_zucc%d" % j, 0.005, loc=(center_b[0] + math.cos(a) * r * 0.45, center_b[1] + math.sin(a) * r * 0.45, center_b[2] + 0.012), mat="RedSanta", segments=6, rings=4)
    else:
        rnd = random.Random(int(abs(center_b[0]) * 1000))
        for j in range(5):
            a = rnd.uniform(0, 6.28)
            d = rnd.uniform(0, r * 0.7)
            k.sphere(prefix + "_goccia%d" % j, 0.006, loc=(center_b[0] + math.cos(a) * d, center_b[1] + math.sin(a) * d, center_b[2] + 0.007), scale=(1, 1, 0.6), mat="WoodDeep", segments=6, rings=4)


def side_table():
    def build():
        k.lathe("Piede", [(0.0, 0.0), (0.24, 0.0), (0.245, 0.02), (0.22, 0.045), (0.14, 0.06), (0.07, 0.085), (0.05, 0.12), (0.075, 0.2), (0.07, 0.3),
                          (0.045, 0.4), (0.04, 0.46), (0.07, 0.49), (0.075, 0.52), (0.0, 0.54)], mat="WoodDark", segments=28)
        k.lathe("Piano", [(0.0, 0.535), (0.33, 0.535), (0.36, 0.55), (0.362, 0.57), (0.35, 0.588), (0.0, 0.59)], mat="WoodLight", segments=40)
        k.torus("Bordo", 0.345, 0.006, loc=(0, 0, 0.555), mat="WoodMid", major_seg=48, minor_seg=6)
        # piatto con biscotti
        px, pz = U(-0.08, 0.59, 0.05)[0], U(-0.08, 0.59, 0.05)[1]
        k.lathe("Piatto", [(0.0, 0.0), (0.08, 0.0), (0.12, 0.015), (0.13, 0.022), (0.12, 0.02), (0.08, 0.008), (0.0, 0.008)], loc=(px, pz, 0.59), mat="Cream", segments=32)
        k.torus("PiattoBordo", 0.118, 0.004, loc=(px, pz, 0.6115), mat="RedSanta", major_seg=32, minor_seg=4)
        for i in range(4):
            a = i * 1.6
            cookie("Biscotto%d" % i, (px + math.cos(a) * 0.05, pz + math.sin(a) * 0.05, 0.605 + (0.016 if i == 3 else 0)), iced=(i % 2 == 0))
        # tazza di cioccolata con marshmallow
        mx, mz = U(0.18, 0.59, -0.1)[0], U(0.18, 0.59, -0.1)[1]
        k.lathe("Tazza", [(0.0, 0.0), (0.038, 0.0), (0.045, 0.01), (0.048, 0.09), (0.042, 0.092), (0.04, 0.02), (0.0, 0.02)], loc=(mx, mz, 0.59), mat="Cream", segments=24)
        k.torus("TazzaRiga", 0.0475, 0.005, loc=(mx, mz, 0.65), mat="RedSanta", major_seg=24, minor_seg=5)
        k.cylinder("Cioccolata", 0.041, 0.01, loc=(mx, mz, 0.67), mat="WoodDeep", bevel=0.002, verts=20)
        for j in range(3):
            k.box("Marsh%d" % j, (0.016, 0.016, 0.014), loc=(mx + (j - 1) * 0.017, mz + (0.01 if j == 1 else -0.006), 0.678), mat="White", bevel=0.004, segments=1)
        h = k.torus("Manico", 0.026, 0.008, loc=(mx - 0.055, mz, 0.635), mat="Cream", major_seg=16, minor_seg=6)
        h.rotation_euler = (math.radians(90), 0, 0)
        return {"ao_distance": 0.12, "ao_samples": 32}
    return build


def display_table():
    def build():
        k.lathe("Base", [(0.0, 0.0), (0.68, 0.0), (0.7, 0.03), (0.66, 0.06), (0.5, 0.08), (0.25, 0.1), (0.0, 0.1)], mat="WoodDark", segments=48)
        k.lathe("Colonna", [(0.0, 0.08), (0.18, 0.08), (0.15, 0.13), (0.11, 0.2), (0.13, 0.35), (0.1, 0.5), (0.085, 0.62), (0.13, 0.68), (0.16, 0.74), (0.0, 0.76)], mat="WoodMid", segments=32)
        k.lathe("Piano", [(0.0, 0.76), (1.03, 0.76), (1.05, 0.78), (1.03, 0.83), (0.0, 0.83)], mat="WoodLight", segments=56)
        # tovaglia rossa con pieghe e orlo ondulato
        bm = bmesh.new()
        segs, rows = 96, 7
        rings = []
        for r in range(rows + 1):
            t = r / rows
            ring = []
            for i in range(segs):
                a = 2 * math.pi * i / segs
                fold = math.sin(a * 14) * 0.022 * t
                rad = 1.075 + 0.02 * t + fold
                z = 0.835 - t * (0.3 + 0.035 * abs(math.sin(a * 7)))
                if r == 0:
                    rad, z = 1.07, 0.84
                ring.append(bm.verts.new((rad * math.cos(a), rad * math.sin(a), z)))
            rings.append(ring)
        for r in range(rows):
            for i in range(segs):
                j = (i + 1) % segs
                bm.faces.new((rings[r][i], rings[r][j], rings[r + 1][j], rings[r + 1][i]))
        top = [bm.verts.new((1.07 * math.cos(2 * math.pi * i / segs), 1.07 * math.sin(2 * math.pi * i / segs), 0.84)) for i in range(segs)]
        center = bm.verts.new((0, 0, 0.845))
        for i in range(segs):
            bm.faces.new((center, top[i], top[(i + 1) % segs]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        cloth = k.mesh_object("Tovaglia", bm)
        k._finish(cloth, "RedSanta", True, (0, 0, 0), (0, 0, 0), 0.0)
        sol = cloth.modifiers.new("Spessore", "SOLIDIFY")
        sol.thickness = 0.012
        # gallone dorato sull'orlo
        hem = []
        for i in range(segs * 2 + 1):
            a = 2 * math.pi * i / (segs * 2)
            rad = 1.095 + math.sin(a * 14) * 0.022
            z = 0.835 - (0.3 + 0.035 * abs(math.sin(a * 7))) + 0.012
            hem.append((rad * math.cos(a), rad * math.sin(a), z))
        k.tube("Gallone", hem, 0.012, mat="Gold", sides=6, caps=False)
        # centrino di pizzo crema con bordo smerlato
        outline = []
        for i in range(120):
            a = 2 * math.pi * i / 120
            rr = 0.95 + 0.035 * abs(math.sin(a * 18))
            outline.append((rr * math.cos(a), rr * math.sin(a)))
        bm = bmesh.new()
        vs = [bm.verts.new((x, y, 0.853)) for (x, y) in outline]
        bm.faces.new(vs)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        doily = k.mesh_object("Centrino", bm)
        k._finish(doily, "Cream", True, (0, 0, 0), (0, 0, 0), 0.0)
        sol = doily.modifiers.new("Spessore", "SOLIDIFY")
        sol.thickness = 0.006
        for i in range(36):
            a = 2 * math.pi * i / 36
            k.cylinder("Foro%d" % i, 0.018, 0.004, loc=(0.86 * math.cos(a), 0.86 * math.sin(a), 0.8545), mat="RedSanta", bevel=0.0, verts=10)
        k.torus("Filo", 0.78, 0.006, loc=(0, 0, 0.854), mat="Gold", major_seg=64, minor_seg=4)
        return {"ao_distance": 0.3, "ao_samples": 32}
    return build


# ============================================================================ LAMPADARIO
def chandelier():
    """Corona di ferro battuto e oro con 6 candele. Origine = centro dell'anello (in C# a y = -lunghezza cavo)."""
    def build():
        k.torus("Anello", 0.8, 0.03, mat="Iron", major_seg=56, minor_seg=7)
        helix = []
        for i in range(721):
            a = 2 * math.pi * i / 720
            b = a * 24
            rr = 0.8 + 0.034 * math.cos(b)
            helix.append((rr * math.cos(a), rr * math.sin(a), 0.034 * math.sin(b)))
        k.tube("Spirale", helix[::3], 0.009, mat="Gold", sides=4, caps=False)
        k.torus("AnelloInterno", 0.45, 0.022, loc=(0, 0, 0.18), mat="Iron", major_seg=36, minor_seg=5)
        k.lathe("Centro", [(0.0, -0.32), (0.015, -0.28), (0.03, -0.18), (0.06, -0.08), (0.1, 0.02), (0.1, 0.1), (0.07, 0.17), (0.04, 0.2), (0.03, 0.26), (0.0, 0.27)], mat="Gold", segments=16)
        k.torus("Gancio", 0.035, 0.008, loc=(0, 0, 0.6), rot=(90, 0, 0), mat="Gold", major_seg=16, minor_seg=6)
        for i in range(6):
            a = i * math.pi / 3
            ca, sa = math.cos(a), math.sin(a)
            # braccio a S che sale dal centro e scende sul piattino della candela
            path = []
            for j in range(25):
                t = j / 24
                rr = 0.06 + t * 0.74
                z = 0.17 * math.sin(math.pi * t * 0.9) - 0.02 + 0.06 * (1 - t)
                path.append((rr * ca, rr * sa, z))
            k.tube("Braccio%d" % i, path[::2], 0.013, mat="Iron", sides=6)
            # ricciolo decorativo sotto l'anello
            cur = []
            b = a + math.pi / 6
            for j in range(30):
                t = j / 29
                ang = t * 4.5
                rad = 0.08 * (1 - t * 0.8)
                cur.append(((0.8 - rad * math.sin(ang)) * math.cos(b), (0.8 - rad * math.sin(ang)) * math.sin(b), -0.03 - rad * (1 - math.cos(ang))))
            k.tube("Ricciolo%d" % i, cur[::2], 0.008, mat="Iron", sides=5, radius_end=0.005)
            # catenella verso il gancio
            k.tube("Catena%d" % i, [(0.45 * ca, 0.45 * sa, 0.18), (0.0, 0.0, 0.6)], 0.006, mat="Iron", sides=5, caps=False)
            # piattino, candela con colature, fiamma
            x, y = 0.8 * ca, 0.8 * sa
            k.lathe("Piattino%d" % i, [(0.0, 0.0), (0.03, 0.0), (0.07, 0.035), (0.075, 0.045), (0.05, 0.04), (0.0, 0.04)], loc=(x, y, 0.0), mat="Gold", segments=12)
            k.lathe("Candela%d" % i, [(0.0, 0.04), (0.038, 0.04), (0.038, 0.19), (0.033, 0.2), (0.0, 0.202)], loc=(x, y, 0.0), mat="Cream", segments=10)
            for d in range(1):
                ang = a + 1.2 + d * 2.6
                capsule("Colatura%d%d" % (i, d), (x + 0.038 * math.cos(ang), y + 0.038 * math.sin(ang), 0.195), (x + 0.04 * math.cos(ang), y + 0.04 * math.sin(ang), 0.14 - d * 0.03), 0.009, 0.011, mat="Cream", seg=6, steps=3)
            k.cylinder("Stoppino%d" % i, 0.004, 0.025, loc=(x, y, 0.21), mat="Black", bevel=0, verts=6)
            fl = teardrop("Fiamma%d" % i, 0.08, 0.026, "LampGlow", bend=0.004, seg=8)
            fl.location = (x, y, 0.212)
            # pallina rossa appesa tra due candele
            bx, by = 0.8 * math.cos(a + 0.52), 0.8 * math.sin(a + 0.52)
            k.cylinder("Filo%d" % i, 0.003, 0.06, loc=(bx, by, -0.06), mat="Gold", bevel=0, verts=5)
            k.sphere("Pallina%d" % i, 0.045, loc=(bx, by, -0.13), mat="RedSanta" if i % 2 == 0 else "Gold", segments=10, rings=6)
            k.cylinder("Tappo%d" % i, 0.012, 0.018, loc=(bx, by, -0.085), mat="Gold", bevel=0.0, verts=8)
        for i in range(3):
            a = i * 2 * math.pi / 3 + 0.26
            holly("Agrifoglio%d" % i, (0.8 * math.cos(a), 0.8 * math.sin(a), 0.03), scale=0.9, rot_z=math.degrees(a), plane="top")
        return {"ao_distance": 0.12, "ao_samples": 24, "ground": None}
    return build


def chandelier_mount():
    def build():
        k.lathe("Rosone", [(0.0, -0.1), (0.03, -0.1), (0.05, -0.07), (0.12, -0.04), (0.15, -0.01), (0.15, 0.0), (0.0, 0.0)], mat="WoodDark", segments=32)
        k.torus("RosoneBordo", 0.14, 0.012, loc=(0, 0, -0.02), mat="Gold", major_seg=32, minor_seg=6)
        return {"ao_distance": 0.05, "ao_samples": 16, "ground": None}
    return build


# ============================================================================ OROLOGIO
def wall_clock():
    """Orologio da parete: quadrante verso +Z di Unity (-Y in Blender), retro a z = 0."""
    def build():
        k.cylinder("Retro", 0.44, 0.04, loc=U(0, 0, 0.02), rot=(90, 0, 0), mat="WoodDark", bevel=0.012, verts=48)
        frame = k.torus("Cornice", 0.425, 0.048, loc=U(0, 0, 0.055), rot=(90, 0, 0), mat="WoodMid", major_seg=64, minor_seg=12)
        frame.scale = (1, 1, 0.75)
        for i in range(32):  # perline intagliate sulla cornice
            a = 2 * math.pi * i / 32
            k.sphere("Perla%d" % i, 0.014, loc=U(math.cos(a) * 0.47, math.sin(a) * 0.47, 0.05), mat="WoodDark", segments=6, rings=4)
        k.torus("Lunetta", 0.37, 0.013, loc=U(0, 0, 0.08), rot=(90, 0, 0), mat="Gold", major_seg=56, minor_seg=6)
        k.cylinder("Quadrante", 0.37, 0.02, loc=U(0, 0, 0.065), rot=(90, 0, 0), mat="Cream", bevel=0.004, verts=56)
        k.torus("Anellino", 0.3, 0.003, loc=U(0, 0, 0.076), rot=(90, 0, 0), mat="WoodDeep", major_seg=48, minor_seg=4)
        for i in range(12):
            a = i * math.pi / 6
            x, y = math.sin(a) * 0.33, math.cos(a) * 0.33
            if i % 3 == 0:
                o = k.box("Ora%d" % i, (0.026, 0.012, 0.075), loc=U(x, y, 0.078), mat="WoodDeep", bevel=0.005, segments=2)
            else:
                o = k.box("Ora%d" % i, (0.014, 0.01, 0.04), loc=U(x, y, 0.078), mat="WoodDeep", bevel=0.003, segments=1)
            o.rotation_euler = (0, math.radians(-i * 30), 0)
        for i in range(60):
            if i % 5 == 0:
                continue
            a = i * math.pi / 30
            k.sphere("Minuto%d" % i, 0.005, loc=U(math.sin(a) * 0.355, math.cos(a) * 0.355, 0.077), mat="WoodDeep", segments=4, rings=3)
        # lancette sagomate (ore: picca; minuti: sottile)
        hour = [(0.0, -0.04), (0.012, -0.03), (0.01, 0.12), (0.03, 0.15), (0.0, 0.2), (-0.03, 0.15), (-0.01, 0.12), (-0.012, -0.03)]
        mins = [(0.0, -0.05), (0.008, -0.04), (0.006, 0.24), (0.0, 0.29), (-0.006, 0.24), (-0.008, -0.04)]
        for name, shape, ang, dz in (("LancettaOre", hour, 300, 0.088), ("LancettaMinuti", mins, 50, 0.096)):
            o = k.extrude_outline(name, [(-x, y) for (x, y) in shape][::-1], 0.008, mat="WoodDeep", bevel=0.002)
            o.rotation_euler = (0, math.radians(ang), 0)
            o.location = U(0, 0, dz)
        k.cylinder("Perno", 0.022, 0.02, loc=U(0, 0, 0.1), rot=(90, 0, 0), mat="Gold", bevel=0.005, verts=16)
        k.sphere("PernoRosso", 0.012, loc=U(0, 0, 0.11), mat="RedSanta", segments=10, rings=6)
        # pendolo
        k.box("Asta", (0.03, 0.015, 0.42), loc=U(0, -0.66, 0.0), mat="WoodMid", bevel=0.006)
        k.cylinder("Peso", 0.1, 0.03, loc=U(0, -0.88, 0.0), rot=(90, 0, 0), mat="Gold", bevel=0.01, verts=32)
        k.torus("PesoAnello", 0.07, 0.006, loc=U(0, -0.88, 0.017), rot=(90, 0, 0), mat="Gold", major_seg=28, minor_seg=5)
        k.box("Attacco", (0.07, 0.03, 0.06), loc=U(0, -0.44, 0.0), mat="WoodDark", bevel=0.01)
        # rametto di agrifoglio con fiocco in alto
        holly("Agrifoglio", U(0, 0.43, 0.08), scale=1.3, plane="front")
        for s2 in (-1, 1):
            lp = k.torus("Fiocco%d" % s2, 0.045, 0.016, loc=U(s2 * 0.05, 0.47, 0.1), mat="RedSanta", major_seg=18, minor_seg=8)
            lp.rotation_euler = (math.radians(90), math.radians(s2 * 15), 0)
            lp.scale = (1.3, 1.0, 0.8)
            k.tube("FioccoCoda%d" % s2, [U(s2 * 0.01, 0.46, 0.1), U(s2 * 0.04, 0.4, 0.11), U(s2 * 0.06, 0.36, 0.1)], 0.012, mat="RedSanta", sides=8, radius_end=0.008)
        k.sphere("FioccoNodo", 0.022, loc=U(0, 0.47, 0.11), mat="RedDeep", segments=12, rings=8)
        return {"ao_distance": 0.12, "ao_samples": 24, "ground": None}
    return build


# ============================================================================ BANDIERINE
def bunting_flag(color):
    """Bandierina di stoffa: in alto (y = 0 Unity) l'orlo che avvolge il filo, punta in basso a -0,34."""
    def build():
        bm = bmesh.new()
        rows, cols = 8, 8
        grid = []
        for r in range(rows + 1):
            t = r / rows
            half = 0.17 * (1 - t) + 0.008
            row = []
            for c in range(cols + 1):
                u = -half + 2 * half * c / cols
                y = -0.02 - t * 0.32
                wave = 0.012 * math.sin(u * 18 + t * 3) + 0.01 * t * t
                row.append(bm.verts.new(U(u, y, wave)))
            grid.append(row)
        for r in range(rows):
            for c in range(cols):
                bm.faces.new((grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        flag = k.mesh_object("Bandierina", bm)
        k._finish(flag, color, True, (0, 0, 0), (0, 0, 0), 0.0)
        sol = flag.modifiers.new("Spessore", "SOLIDIFY")
        sol.thickness = 0.008
        sol.offset = 0
        k.tube("Orlo", [U(-0.18, -0.012, 0), U(0, -0.014, 0.002), U(0.18, -0.012, 0)], 0.014, mat=color, sides=8)
        # decoro: pallini chiari o una stellina
        deco = "Snow" if color != "Snow" else "RedSanta"

        def wave_at(u, y):
            t = (-0.02 - y) / 0.32
            return 0.012 * math.sin(u * 18 + t * 3) + 0.01 * t * t
        if color in ("RedSanta", "ClothBlue"):
            for (dx, dy) in ((-0.07, -0.08), (0.07, -0.08), (0.0, -0.14), (-0.035, -0.2), (0.035, -0.2), (0.0, -0.26)):
                k.cylinder("Pois%.2f%.2f" % (dx, dy), 0.014, 0.014, loc=U(dx, dy, wave_at(dx, dy)), rot=(90, 0, 0), mat=deco, bevel=0.0, verts=10)
        else:
            outline = []
            for j in range(10):
                a = math.pi / 2 + j * math.pi / 5
                rr = 0.045 if j % 2 == 0 else 0.02
                outline.append((rr * math.cos(a), rr * math.sin(a)))
            k.extrude_outline("Stella", outline, 0.014, loc=U(0, -0.12, wave_at(0.0, -0.12)), mat="Gold" if color != "ClothYellow" else "RedSanta", bevel=0.0)
        return {"ao": False}
    return build


# ============================================================================ registro
MODELS = {}
for _fur in ("CookieBrown", "WoodLight", "ClothPink", "Cream", "ClothPurple"):
    MODELS["Decor_Teddy_" + _fur] = teddy(_fur)
for _b in ("ClothBlue", "ClothTeal", "ClothOrange", "ClothPurple"):
    MODELS["Decor_Robot_" + _b] = robot(_b)
MODELS["Decor_Drum"] = drum()
for _c in ("ClothYellow", "RedSanta", "ClothPink"):
    MODELS["Decor_SpinTop_" + _c] = spin_top(_c)
MODELS["Decor_Blocks"] = blocks()
MODELS["Decor_BeachBall"] = beach_ball()
MODELS["Decor_Fireplace"] = fireplace_body()
MODELS["Decor_FireplaceChimney"] = fireplace_chimney()
MODELS["Decor_FireplaceFlames"] = fireplace_flames(False)
MODELS["Decor_FireplaceFlamesInner"] = fireplace_flames(True)
for _f in ("RedSanta", "GreenElf"):
    MODELS["Decor_Armchair_" + _f] = armchair(_f)
MODELS["Decor_SideTable"] = side_table()
MODELS["Decor_DisplayTable"] = display_table()
MODELS["Decor_Chandelier"] = chandelier()
MODELS["Decor_ChandelierMount"] = chandelier_mount()
MODELS["Decor_WallClock"] = wall_clock()
for _c in ("RedSanta", "GreenElf", "ClothYellow", "Snow", "ClothBlue"):
    MODELS["Decor_BuntingFlag_" + _c] = bunting_flag(_c)
