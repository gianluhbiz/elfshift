# -*- coding: utf-8 -*-
"""
Mobili di Elfshift (scaffali, banchi di lavoro, slitta, pacchi regalo) modellati in Blender.

Per non sbagliare le misure, qui si lavora DIRETTAMENTE nelle coordinate di Unity usate da
Assets/Editor/Generation/FurnitureModels.cs (Y in alto, il davanti del mobile verso +Z, metri):
le funzioni u*() convertono da sole verso Blender (Z in alto, davanti verso -Y).
"""
import math
import random

import bpy  # noqa: F401  (deve essere importato prima di bmesh)
import bmesh
from mathutils import Matrix, Vector

import elfkit as k

# ---------------------------------------------------------------------------- misure (come in C#)
SHELF_W, SHELF_D, SHELF_H = 2.4, 0.75, 2.3
BENCH_W, BENCH_D, BENCH_TOP = 2.2, 1.0, 0.9

# Unity -> Blender: x' = -x, y' = -z, z' = y (uno specchio: è quello che elfkit.export inverte)
_M = Matrix(((-1, 0, 0), (0, 0, -1), (0, 1, 0)))


def P(x, y, z):
    return Vector((-x, -z, y))


def S(x, y, z):
    """Dimensioni Unity (larghezza X, altezza Y, profondità Z) -> dimensioni del box di elfkit (X, Y, Z di Blender)."""
    return (x, z, y)


def _ru(rx, ry, rz):
    """Matrice di rotazione di Unity (Quaternion.Euler: prima Z, poi X, poi Y)."""
    return (Matrix.Rotation(math.radians(ry), 3, "Y") @ Matrix.Rotation(math.radians(rx), 3, "X")
            @ Matrix.Rotation(math.radians(rz), 3, "Z"))


def R(rx=0.0, ry=0.0, rz=0.0):
    """Rotazione di Unity -> Euler di Blender."""
    return (_M @ _ru(rx, ry, rz) @ _M.transposed()).to_euler()


def rot_vec(v, rot):
    return _ru(*rot) @ Vector(v)


# ---------------------------------------------------------------------------- forme in coordinate Unity
def ubox(name, center, size, mat, bevel=0.01, rot=(0, 0, 0), seg=2, tint=0.03):
    o = k.box(name, S(*size), P(*center), mat=mat, bevel=bevel, segments=seg, tint=tint)
    o.rotation_euler = R(*rot)
    return o


def ucyl(name, center, radius, length, mat, rot=(0, 0, 0), bevel=0.006, verts=24, seg=2, tint=0.03, radius_top=None):
    """Cilindro CENTRATO in 'center', asse Y di Unity (ruotabile)."""
    o = k.cylinder(name, radius, length, P(*center), mat=mat, bevel=bevel, verts=verts, segments=seg, tint=tint, radius_top=radius_top)
    o.rotation_euler = R(*rot)
    return o


def ucyl_base(name, base, radius, height, mat, rot=(0, 0, 0), **kw):
    """Cilindro con la BASE in 'base' (come MeshBuilder.Cyl di C#)."""
    c = Vector(base) + rot_vec((0, height / 2, 0), rot)
    return ucyl(name, tuple(c), radius, height, mat, rot, **kw)


def usphere(name, center, radius, mat, scale=(1, 1, 1), rot=(0, 0, 0), seg=20, rings=10, tint=0.02):
    o = k.sphere(name, radius, P(*center), scale=S(*scale), mat=mat, segments=seg, rings=rings, tint=tint)
    o.rotation_euler = R(*rot)
    return o


def utorus(name, center, major, minor, mat, rot=(0, 0, 0), mseg=32, nseg=8, tint=0.02):
    """Anello nel piano XZ di Unity (asse Y), ruotabile."""
    o = k.torus(name, major, minor, P(*center), mat=mat, major_seg=mseg, minor_seg=nseg, tint=tint)
    o.rotation_euler = R(*rot)
    return o


def ulathe(name, base, profile, mat, rot=(0, 0, 0), seg=24, tint=0.02):
    """Solido di rotazione attorno all'asse Y di Unity. profile = [(raggio, altezza)] dalla base."""
    o = k.lathe(name, profile, P(*base), mat=mat, segments=seg, tint=tint)
    o.rotation_euler = R(*rot)
    return o


def utube(name, points, radius, mat, sides=12, radius_end=None, caps=True, tint=0.02):
    return k.tube(name, [tuple(P(*p)) for p in points], radius, mat=mat, sides=sides, radius_end=radius_end, caps=caps, tint=tint)


def uprism(name, outline, depth, frame, mat, bevel=0.006, bevel_seg=2, tint=0.02):
    """Sagoma 2D (u, v) estrusa di 'depth' lungo w. frame(u, v, w) -> punto Unity.
    Ideale per fianchi sagomati, lame, ingranaggi, stelle..."""
    bm = bmesh.new()
    a = [bm.verts.new(P(*frame(u, v, -depth / 2))) for (u, v) in outline]
    b = [bm.verts.new(P(*frame(u, v, depth / 2))) for (u, v) in outline]
    bm.faces.new(a)
    bm.faces.new(list(reversed(b)))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = k.mesh_object(name, bm)
    if bevel > 0:
        m = o.modifiers.new("Smusso", "BEVEL")
        m.width = bevel
        m.segments = bevel_seg
        m.limit_method = "ANGLE"
        m.angle_limit = math.radians(30)
    # normali "pesate": le facce grandi restano piatte (niente strisce d'ombra sulle sagome)
    wn = o.modifiers.new("Normali", "WEIGHTED_NORMAL")
    wn.keep_sharp = True
    wn.weight = 100
    return k._finish(o, mat, True, (0, 0, 0), (0, 0, 0), tint)


def front_frame(cx, cy, cz):
    """Piano frontale (u = X, v = Y), spessore lungo Z: per sagome viste da davanti."""
    return lambda u, v, w: (cx + u, cy + v, cz + w)


def top_frame(cx, cy, cz, yaw=0.0):
    """Piano orizzontale (u = X, v = Z), spessore lungo Y: per sagome appoggiate sul piano."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    return lambda u, v, w: (cx + u * c + v * s, cy + w, cz - u * s + v * c)


def side_frame(cx, cy, cz):
    """Piano laterale (u = Z, v = Y), spessore lungo X: per fianchi (slitta)."""
    return lambda u, v, w: (cx + w, cy + v, cz + u)


def wob(o, amount=0.004, scale=0.4, levels=1):
    """Come elfkit.wobble ma con meno suddivisioni (per restare nel budget di triangoli)."""
    k.wobble(o, amount, scale)
    sub = o.modifiers.get("Suddivisione")
    if sub is not None:
        if levels <= 0:
            o.modifiers.remove(sub)
        else:
            sub.levels = levels
            sub.render_levels = levels
    return o


def flatten(o, sx=1.0, sy=1.0, sz=1.0):
    """Schiaccia un oggetto attorno al SUO centro (scala in assi Unity), senza spostarlo."""
    vs = o.data.vertices
    c = sum((v.co for v in vs), Vector()) / max(1, len(vs))
    bs = S(sx, sy, sz)
    m = Matrix.Translation(c) @ Matrix.Diagonal((bs[0], bs[1], bs[2], 1.0)) @ Matrix.Translation(-c)
    o.data.transform(m)
    return o


def soft(o, levels=1):
    return k.subdivide(o, levels)


def jit(o, deg=2.0, off=0.004):
    return k.jitter(o, deg, off)


def smooth_curve(points, steps=6):
    """Curva morbida (Catmull-Rom) che passa per i punti 2D o 3D dati."""
    pts = [Vector(p) for p in points]
    out = []
    for i in range(len(pts) - 1):
        p0 = pts[i - 1] if i > 0 else pts[i]
        p1, p2 = pts[i], pts[i + 1]
        p3 = pts[i + 2] if i + 2 < len(pts) else pts[i + 1]
        for s in range(steps):
            t = s / steps
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(pts[-1])
    return [tuple(p) for p in out]


def ribbon(name, points, width, thick, mat, up=(0, 1, 0), normal=None, tint=0.02):
    """Nastro PIATTO lungo un percorso (coordinate Unity): larghezza 'width', spessore 'thick'.
    La larghezza segue 'normal' (vettore fisso) oppure è orizzontale rispetto a 'up'."""
    pts = [Vector(p) for p in points]
    n = len(pts)
    bm = bmesh.new()
    rings = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        if normal is not None:
            wd = Vector(normal).normalized()
        else:
            wd = t.cross(Vector(up))
            if wd.length < 1e-4:
                wd = Vector((1, 0, 0))
            wd.normalize()
        td = t.cross(wd).normalized()
        ring = []
        for (a, b) in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            q = p + wd * (a * width / 2) + td * (b * thick / 2)
            ring.append(bm.verts.new(P(q.x, q.y, q.z)))
        rings.append(ring)
    for i in range(n - 1):
        for j in range(4):
            j2 = (j + 1) % 4
            bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
    bm.faces.new(rings[0])
    bm.faces.new(list(reversed(rings[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = k.mesh_object(name, bm)
    return k._finish(o, mat, True, (0, 0, 0), (0, 0, 0), tint)


def gear_outline(radius, teeth=10, depth_frac=0.16):
    pts = []
    root = radius * (1 - depth_frac)
    for i in range(teeth):
        a0 = 2 * math.pi * i / teeth
        step = 2 * math.pi / teeth
        for (fa, r) in ((0.0, root), (0.12, root), (0.24, radius), (0.5, radius), (0.62, root), (0.88, root)):
            a = a0 + fa * step
            pts.append((r * math.cos(a), r * math.sin(a)))
    return pts


def star_outline(points, r_out, r_in):
    pts = []
    for i in range(points * 2):
        a = math.pi / 2 + i * math.pi / points
        r = r_out if i % 2 == 0 else r_in
        pts.append((r * math.cos(a), r * math.sin(a)))
    return pts


def rounded_rect(w, h, r, steps=4):
    pts = []
    for (cx, cy, a0) in ((w / 2 - r, -h / 2 + r, -90), (w / 2 - r, h / 2 - r, 0), (-w / 2 + r, h / 2 - r, 90), (-w / 2 + r, -h / 2 + r, 180)):
        for s in range(steps + 1):
            a = math.radians(a0 + 90 * s / steps)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def nail(center, axis_rot=(90, 0, 0), r=0.009):
    """Testa di chiodo (piccolo disco di ferro), di default rivolta verso +Z."""
    return ucyl("Chiodo", center, r, 0.006, "Iron", rot=axis_rot, bevel=0.002, verts=8, seg=1, tint=0.0)


# ============================================================================ elementi riutilizzabili
def log(center, radius, length, yaw=0.0, roll=0.0, rng=None):
    """Tronchetto: corteccia irregolare, sezioni chiare con anello scuro. Asse lungo X di Unity (yaw lo gira)."""
    rng = rng or random
    prof = [(0.0, 0.0), (radius * 0.86, 0.0)]
    n = 3
    for i in range(n + 1):
        t = i / n
        r = radius * (1.0 + rng.uniform(-0.05, 0.05))
        prof.append((r, length * (0.02 + 0.96 * t)))
    prof += [(radius * 0.86, length), (0.0, length)]
    rot = (0.0, yaw, 90.0 + roll)
    base = Vector(center) + rot_vec((0, -length / 2, 0), rot)
    bark = ulathe("Corteccia", tuple(base), prof, "Bark", rot=rot, seg=11, tint=0.06)
    objs = [bark]
    for end in (0.0, 1.0):
        y = -length / 2 - 0.004 + end * (length + 0.008)
        c = Vector(center) + rot_vec((0, y, 0), rot)
        objs.append(ucyl("Taglio", tuple(c), radius * 0.84, 0.012, "WoodEnd", rot=rot, bevel=0.0, verts=11, seg=1, tint=0.04))
        c2 = Vector(center) + rot_vec((0, y * 1.012, 0), rot)
        objs.append(ucyl("Anello", tuple(c2), radius * 0.36, 0.012, "WoodLight", rot=rot, bevel=0.0, verts=8, seg=1, tint=0.04))
    return objs


def log_bundle(origin, scale=1.0):
    """Fascina di 3 tronchetti legata con due nastri rossi e un fiocco (come ItemModels.AddLogBundle)."""
    ox, oy, oz = origin
    r = 0.1 * scale
    ln = 0.56 * scale
    log((ox, oy + r, oz - r * 1.05), r, ln, roll=random.uniform(-2, 2))
    log((ox, oy + r, oz + r * 1.05), r, ln, roll=random.uniform(-2, 2))
    log((ox, oy + r + r * 1.8, oz), r, ln, roll=random.uniform(-2, 2))
    # nastro che abbraccia i tre tronchi (triangolo arrotondato attorno alle sezioni)
    centers = [(oy + r, oz - r * 1.05), (oy + r, oz + r * 1.05), (oy + r * 2.8, oz)]
    for i in (-1, 1):
        x = ox + i * 0.15 * scale
        pts = []
        for j in range(41):
            t = 2 * math.pi * j / 40
            dy, dz = math.cos(t), math.sin(t)
            cy, cz = max(centers, key=lambda c: c[0] * dy + c[1] * dz)
            pts.append((x, cy + dy * (r + 0.008), cz + dz * (r + 0.008)))
        ribbon("Nastro", pts, 0.04 * scale, 0.012, "RedSanta", normal=(1, 0, 0))
    bow((ox + 0.15 * scale, oy + r * 3.15, oz), 0.11 * scale, "RedSanta", yaw=90)


def bow(center, size, mat, yaw=0.0, tails=True):
    """Fiocco di nastro piatto: due asole inclinate, nodo centrale e due codine che ricadono.
    'center' = punto d'appoggio del fiocco (sopra il pacco)."""
    c = Vector(center)
    side = rot_vec((1, 0, 0), (0, yaw, 0))
    fwd = rot_vec((0, 0, 1), (0, yaw, 0))
    up = Vector((0, 1, 0))
    for sgn in (-1, 1):
        # asola: anello a goccia che si apre di lato e verso l'alto (inclinato di ~35 gradi)
        out = (side * sgn * math.cos(math.radians(35)) + up * math.sin(math.radians(35))).normalized()
        pts = []
        for i in range(17):
            a = 2 * math.pi * i / 16
            along = (0.5 - 0.5 * math.cos(a)) * size * 0.62
            across = math.sin(a) * size * 0.2 * (0.35 + 0.65 * (0.5 - 0.5 * math.cos(a)))
            pts.append(tuple(c + up * size * 0.05 + out * along + up.cross(out).normalized() * 0.0 + (out.cross(fwd)).normalized() * across * sgn))
        ribbon("Asola", pts, size * 0.26, size * 0.035, mat, normal=tuple(fwd))
    usphere("Nodo", tuple(c + up * size * 0.06), size * 0.1, mat, scale=(1.2, 0.85, 1.0), rot=(0, yaw, 0), seg=12, rings=8)
    if tails:
        for sgn in (-1, 1):
            d = (fwd * 0.8 + side * sgn * 0.6).normalized()
            pts = [tuple(c + up * size * 0.03), tuple(c + d * size * 0.25 + up * size * 0.015), tuple(c + d * size * 0.48 - up * size * 0.0)]
            ribbon("Coda", pts, size * 0.18, size * 0.03, mat, up=(0, 1, 0))


def gift(base, size, box_mat, rib_mat, yaw=0.0):
    """Pacco regalo: scatola morbida, nastro a croce in rilievo e fiocco. 'base' = centro della base (Unity)."""
    bx, by, bz = base
    sx, sy, sz = size
    c = (bx, by + sy / 2, bz)
    rot = (0, yaw, 0)
    ubox("Scatola", c, (sx, sy, sz), box_mat, bevel=min(0.03, sy * 0.12), rot=rot, seg=3, tint=0.03)
    ubox("NastroA", c, (sx * 1.015, sy * 1.015, sz * 0.15), rib_mat, bevel=0.006, rot=rot, seg=2)
    ubox("NastroB", c, (sx * 0.15, sy * 1.015, sz * 1.015), rib_mat, bevel=0.006, rot=rot, seg=2)
    s = min(sx, sz)
    bow((bx, by + sy + 0.004, bz), s * 0.55, rib_mat, yaw=yaw + 30)
    # nastro che passa sopra il pacco: piccoli rigonfiamenti dove si incrocia
    ubox("Incrocio", (bx, by + sy + 0.002, bz), (sx * 0.2, 0.01, sz * 0.2), rib_mat, bevel=0.004, rot=rot, seg=2)


def gear(center, radius, mat, thick=0.06, teeth=10):
    """Ingranaggio verticale rivolto verso +Z, con mozzo e bulloni."""
    cx, cy, cz = center
    uprism("Ingranaggio", gear_outline(radius, teeth), thick, front_frame(cx, cy, cz), mat, bevel=0.006, bevel_seg=1)
    ucyl("Mozzo", (cx, cy, cz + thick * 0.5), radius * 0.32, thick * 0.6, "Iron", rot=(90, 0, 0), bevel=0.004, verts=16)
    ucyl("Foro", (cx, cy, cz + thick * 0.82), radius * 0.12, 0.01, "Black", rot=(90, 0, 0), bevel=0.002, verts=12, seg=1)
    for i in range(4):
        a = math.radians(45 + i * 90)
        nail((cx + math.cos(a) * radius * 0.55, cy + math.sin(a) * radius * 0.55, cz + thick * 0.5 + 0.002), r=radius * 0.06)


# ============================================================================ SCAFFALE
def shelf_frame():
    w, d, h = SHELF_W, SHELF_D, SHELF_H
    # fianchi con "cornice" frontale più scura
    for sx in (-1, 1):
        x = sx * (w / 2 - 0.05)
        side = ubox("Fianco", (x, h / 2, 0), (0.1, h, d), "WoodMid", bevel=0.022)
        wob(side, 0.004, 0.5, levels=0)
        ubox("Cornice", (x, h / 2 + 0.01, d / 2 - 0.005), (0.13, h + 0.02, 0.04), "WoodDark", bevel=0.014)
        # piedini
        ubox("Piede", (x, 0.03, d / 2 - 0.08), (0.15, 0.06, 0.16), "WoodDeep", bevel=0.02)
        ubox("Piede", (x, 0.03, -d / 2 + 0.08), (0.15, 0.06, 0.16), "WoodDeep", bevel=0.02)
    # schienale a doghe verticali (con un fondo pieno dietro, così tra le doghe non passa luce)
    ubox("Fondo", (0, h / 2, -d / 2 + 0.006), (w - 0.12, h - 0.04, 0.012), "WoodDeep", bevel=0.0, seg=1, tint=0.0)
    n = 7
    inner = w - 0.14
    pw = inner / n
    for i in range(n):
        x = -inner / 2 + pw * (i + 0.5)
        p = ubox("Doga", (x, h / 2, -d / 2 + 0.03), (pw - 0.01, h - 0.06, 0.04), "WoodDark", bevel=0.008, tint=0.07)
        jit(p, 0.3, 0.001)
    # ripiani con bordino frontale e chiodi
    board_y = [0.2, 0.85, 1.5, 2.26]
    for i, y in enumerate(board_y):
        b = ubox("Ripiano", (0, y, 0.005), (w - 0.1, 0.07, d - 0.03), "WoodLight", bevel=0.018, tint=0.04)
        wob(b, 0.003, 0.4, levels=0)
        if i < 3:
            ubox("Bordino", (0, y + 0.075, d / 2 - 0.025), (w - 0.14, 0.09, 0.03), "WoodDeep", bevel=0.012)
            for sx in (-1, 1):
                nail((sx * (w / 2 - 0.16), y + 0.075, d / 2 - 0.008))
            # mensoline di rinforzo sotto il ripiano
            for sx in (-1, 1):
                x = sx * (w / 2 - 0.1)
                tri = [(0.0, 0.0), (0.0, -0.14), (0.12, 0.0)]
                frame = (lambda xx: (lambda u, v, ww: (xx - sx * u, y - 0.035 + v, -d / 2 + 0.16 + ww)))(x)
                uprism("Mensola", tri, 0.04, frame, "WoodMid", bevel=0.006)
    # coronamento in alto con modanatura
    top = ubox("Cappello", (0, h + 0.03, 0.02), (w + 0.12, 0.08, d + 0.1), "WoodMid", bevel=0.028)
    wob(top, 0.003, 0.5, levels=0)
    ucyl("Modanatura", (0, h - 0.03, d / 2 + 0.005), 0.035, w - 0.02, "WoodDeep", rot=(0, 0, 90), bevel=0.004, verts=14)
    # zoccolo
    ubox("Zoccolo", (0, 0.09, d / 2 - 0.05), (w - 0.12, 0.15, 0.04), "WoodDeep", bevel=0.012)
    # neve morbida sopra, con piccoli ghiaccioli sul davanti
    blanket = usphere("Neve", (0.0, h + 0.06, 0.03), 1.0, "Snow", scale=(1.2, 0.09, 0.42), seg=20, rings=8)
    soft(blanket, 1)
    wob(blanket, 0.03, 0.25, levels=0)
    for (x, z, r, hy) in ((-0.6, 0.0, 0.32, 0.5), (0.35, 0.08, 0.38, 0.42), (0.85, -0.05, 0.22, 0.6)):
        m = usphere("Mucchio", (x, h + 0.1, z), r, "Snow", scale=(1.0, hy, 0.8), seg=12, rings=6)
        soft(m, 1)
        wob(m, 0.02, 0.2, levels=0)
    for (x, ln) in ((-0.9, 0.09), (-0.62, 0.14), (-0.3, 0.07), (0.15, 0.11), (0.55, 0.16), (0.8, 0.08)):
        ulathe("Ghiacciolo", (x, h + 0.075 - ln, d / 2 + 0.045), [(0.0, 0.0), (0.012, ln * 0.45), (0.022, ln * 0.85), (0.02, ln), (0.0, ln + 0.004)],
               "SnowShade", seg=8)
    return {"ao_distance": 0.45}


def shelf_sign(kind):
    def build():
        w, d, h = SHELF_W, SHELF_D, SHELF_H
        cz = d / 2 - 0.1
        cy = h + 0.32
        # tavoletta ad arco con bordo e pannello interno
        outline = [(0.5, -0.16)]
        for i in range(17):
            a = math.pi * i / 16
            outline.append((0.5 * math.cos(a), 0.08 + 0.12 * math.sin(a)))
        outline.append((-0.5, -0.16))
        uprism("Insegna", outline, 0.06, front_frame(0, cy, cz), "WoodDeep", bevel=0.012)
        inner = [(x * 0.86, y * 0.82 + 0.005) for (x, y) in outline]
        uprism("Pannello", inner, 0.02, front_frame(0, cy, cz + 0.032), "Cream", bevel=0.005)
        for sx in (-1, 1):
            nail((sx * 0.42, cy - 0.11, cz + 0.032))
        # sostegni con cappucci tondi
        for sx in (-1, 1):
            ubox("Paletto", (sx * 0.36, h + 0.12, cz), (0.05, 0.12, 0.05), "WoodDark", bevel=0.01)
        # simbolo in rilievo
        ic = (0, cy + 0.02, cz + 0.05)
        if kind == "Wood":
            ucyl("Tronco", ic, 0.115, 0.03, "Bark", rot=(90, 0, 0), bevel=0.006, verts=24)
            ucyl("Taglio", (ic[0], ic[1], ic[2] + 0.012), 0.095, 0.02, "WoodEnd", rot=(90, 0, 0), bevel=0.004, verts=24)
            for r in (0.065, 0.035):
                utorus("Anello", (ic[0], ic[1], ic[2] + 0.022), r, 0.004, "WoodMid", rot=(90, 0, 0), mseg=24, nseg=4)
        elif kind == "Cloth":
            uprism("Filo", rounded_rect(0.13, 0.15, 0.02), 0.04, front_frame(ic[0], ic[1], ic[2]), "ClothPink", bevel=0.006)
            for sy in (-1, 1):
                uprism("Flangia", rounded_rect(0.2, 0.035, 0.015), 0.045, front_frame(ic[0], ic[1] + sy * 0.09, ic[2]), "WoodLight", bevel=0.006)
            for i in range(4):
                ubox("Giro", (ic[0], ic[1] - 0.05 + i * 0.033, ic[2] + 0.022), (0.135, 0.006, 0.006), "RedLight", bevel=0.002, seg=1)
            ucyl("Ago", (ic[0] + 0.12, ic[1], ic[2] + 0.03), 0.006, 0.26, "MetalLight", rot=(0, 0, -35), bevel=0.002, verts=8, radius_top=0.002)
        elif kind == "Metal":
            gear(ic, 0.12, "MetalLight", thick=0.035, teeth=9)
        else:
            uprism("Stella", star_outline(5, 0.13, 0.058), 0.04, front_frame(ic[0], ic[1], ic[2]), "ClothYellow", bevel=0.01, bevel_seg=3)
        return {"ao_distance": 0.2}
    return build


def shelf_goods(kind):
    def build():
        b0, b1, b2 = 0.235, 0.885, 1.535
        random.seed(sum(map(ord, kind)))
        if kind == "Wood":
            for row in range(2):
                count = 10 if row == 0 else 9
                for i in range(count):
                    x = -0.9 + i * 0.2 + (0.1 if row == 1 else 0.0)
                    log((x, b0 + 0.1 + row * 0.17, 0.0), 0.098, 0.62, yaw=90 + random.uniform(-3, 3), roll=random.uniform(-2, 2))
            for i in range(3):
                log_bundle((-0.72 + i * 0.72, b1, 0.0), 0.95)
            for i in range(2):
                log_bundle((-0.45 + i * 0.9, b2, 0.02), 0.95)
            # un'accetta appoggiata sul ripiano alto
            ax = (0.85, b2, 0.12)
            ucyl("Manico", (ax[0], ax[1] + 0.02, ax[2]), 0.016, 0.42, "WoodMid", rot=(0, 20, 90), bevel=0.005, verts=10)
            uprism("Lama", [(-0.02, -0.05), (0.06, -0.07), (0.07, 0.06), (-0.02, 0.04)], 0.02,
                   top_frame(ax[0] - 0.17, ax[1] + 0.012, ax[2] + 0.07, yaw=20), "MetalLight", bevel=0.004)
        elif kind == "Cloth":
            colors = ["ClothBlue", "ClothPink", "ClothPurple", "ClothYellow", "ClothTeal", "ClothOrange", "RedSanta", "GreenElf"]
            for row in range(2):
                count = 8 if row == 0 else 7
                for i in range(count):
                    x = -0.95 + i * 0.27 + (0.135 if row == 1 else 0.0)
                    c = colors[(i * 3 + row * 2) % len(colors)]
                    y = b0 + 0.13 + row * 0.235
                    bolt = ucyl("Rotolo", (x, y, 0.0), 0.125, 0.6, c, rot=(90, 0, 0), bevel=0.02, verts=16, seg=2, tint=0.05)
                    jit(bolt, 1.0, 0.003)
                    ucyl("Anima", (x, y, 0.302), 0.05, 0.02, "Cream", rot=(90, 0, 0), bevel=0.004, verts=12)
                    utorus("Strato", (x, y, 0.3), 0.09, 0.004, c, rot=(90, 0, 0), mseg=16, nseg=3)
                    if (i + row) % 3 == 0 and row == 1:
                        # lembo di stoffa che scende sul davanti
                        pts = [(x - 0.05, y + 0.1, 0.29), (x - 0.06, y - 0.05, 0.33), (x - 0.04, y - 0.2, 0.33)]
                        flap = utube("Lembo", pts, 0.045, c, sides=8, radius_end=0.04)
                        flatten(flap, 1.0, 1.0, 0.25)
            for s in range(4):
                for layer in range(5):
                    c = colors[(s + layer) % len(colors)]
                    yaw = (3 if layer % 2 == 0 else -4) + random.uniform(-2, 2)
                    ubox("Piega", (-0.8 + s * 0.52, b1 + 0.045 + layer * 0.075, 0.02), (0.46, 0.07, 0.4), c, bevel=0.032, rot=(0, yaw, 0), seg=3)
                    # bordo cucito
                    ubox("Cucitura", (-0.8 + s * 0.52, b1 + 0.045 + layer * 0.075, 0.22), (0.4, 0.006, 0.006), "Cream", bevel=0.002, rot=(0, yaw, 0), seg=1)
            for i in range(6):
                x = -0.85 + i * 0.34
                c = colors[(i + 2) % len(colors)]
                ulathe("Rocchetto", (x, b2, 0.0), [(0.0, 0.0), (0.1, 0.0), (0.1, 0.028), (0.06, 0.03)], "WoodLight", seg=16)
                prof = [(0.06, 0.028)]
                for j in range(9):
                    prof.append((0.074 + (0.004 if j % 2 else 0.0), 0.04 + j * 0.019))
                prof.append((0.06, 0.205))
                ulathe("Filo", (x, b2, 0.0), prof, c, seg=16)
                ulathe("Rocchetto", (x, b2 + 0.2, 0.0), [(0.06, 0.0), (0.1, 0.002), (0.1, 0.03), (0.0, 0.03)], "WoodLight", seg=16)
        elif kind == "Metal":
            for col in range(2):
                cx = -0.62 + col * 1.0
                for level in range(4):
                    n = 4 - level
                    for i in range(n):
                        x = cx - (n - 1) * 0.1 + i * 0.2
                        mat = "MetalLight" if level % 2 == 0 else "MetalDark"
                        trap = [(-0.095, -0.05), (0.095, -0.05), (0.075, 0.05), (-0.075, 0.05)]
                        bar = uprism("Lingotto", trap, 0.4, front_frame(x, b0 + 0.055 + level * 0.105, 0.0), mat, bevel=0.012, bevel_seg=2, tint=0.04)
                        jit(bar, 1.2, 0.003)
            gx = [-0.85, -0.3, 0.2, 0.7]
            gr = [0.2, 0.16, 0.22, 0.15]
            for g in range(4):
                gear((gx[g], b1 + gr[g] + 0.02, -0.15), gr[g], "MetalDark" if g % 2 == 0 else "Gold")
            # tubi impilati con flange
            for i, (dy, dz) in enumerate(((0.05, 0.12), (0.05, -0.02), (0.15, 0.05))):
                ucyl("Tubo", (0.25, b2 + dy, dz), 0.05, 0.9, "MetalLight", rot=(0, 0, 90), bevel=0.006, verts=16)
                for sx in (-1, 1):
                    ucyl("Flangia", (0.25 + sx * 0.44, b2 + dy, dz), 0.062, 0.03, "MetalDark", rot=(0, 0, 90), bevel=0.005, verts=16)
            # oliatore e barattolo di bulloni
            ulathe("Oliatore", (-0.8, b2, 0.0), [(0.0, 0.0), (0.12, 0.0), (0.12, 0.16), (0.09, 0.2), (0.03, 0.24), (0.0, 0.24)], "MetalDark", seg=24)
            ucyl("Beccuccio", (-0.72, b2 + 0.29, 0.0), 0.012, 0.22, "Gold", rot=(0, 0, -50), bevel=0.003, verts=8, radius_top=0.007)
            ulathe("Barattolo", (-0.35, b2, 0.0), [(0.0, 0.0), (0.1, 0.0), (0.1, 0.19), (0.092, 0.2), (0.0, 0.2)], "Iron", seg=20)
            for i in range(7):
                a = i * 0.9
                ucyl("Bullone", (-0.35 + math.cos(a) * 0.05, b2 + 0.21, math.sin(a) * 0.05), 0.018, 0.02, "MetalLight", rot=(random.uniform(-20, 20), 0, random.uniform(-20, 20)),
                     bevel=0.003, verts=6, seg=1)
        return {"ao_distance": 0.3}
    return build


# ============================================================================ BANCO DI LAVORO
def bench_base():
    w, d, top = BENCH_W, BENCH_D, BENCH_TOP
    # piano fatto di assi
    n = 5
    pw = (d - 0.02) / n
    for i in range(n):
        z = -d / 2 + 0.01 + pw * (i + 0.5)
        p = ubox("Asse", (0, top - 0.05, z), (w - 0.16, 0.1, pw - 0.008), "WoodLight", bevel=0.014, tint=0.06)
        wob(p, 0.003, 0.45, levels=0)
    for sx in (-1, 1):
        ubox("Testata", (sx * (w / 2 - 0.045), top - 0.05, 0), (0.09, 0.11, d + 0.02), "WoodMid", bevel=0.02)
        for sz in (-1, 1):
            nail((sx * (w / 2 - 0.045), top + 0.006, sz * 0.36), axis_rot=(0, 0, 0))
    # grembiule con cassetti sul davanti (+Z)
    ubox("Fascia", (0, top - 0.17, d / 2 - 0.07), (w - 0.12, 0.14, 0.05), "WoodMid", bevel=0.014)
    ubox("Fascia", (0, top - 0.17, -d / 2 + 0.07), (w - 0.12, 0.14, 0.05), "WoodMid", bevel=0.014)
    for sx in (-1, 1):
        ubox("Fascia", (sx * (w / 2 - 0.12), top - 0.17, 0), (0.05, 0.14, d - 0.2), "WoodMid", bevel=0.014)
        ubox("Cassetto", (sx * 0.45, top - 0.17, d / 2 - 0.035), (0.62, 0.11, 0.025), "WoodDark", bevel=0.01)
        usphere("Pomello", (sx * 0.45, top - 0.17, d / 2 - 0.01), 0.022, "Gold", scale=(1, 1, 0.8), seg=12, rings=6)
    # gambe robuste, leggermente rastremate, con piedini
    for sx in (-1, 1):
        for sz in (-1, 1):
            x, z = sx * (w / 2 - 0.12), sz * (d / 2 - 0.12)
            leg = uprism("Gamba", [(-0.065, 0.0), (0.065, 0.0), (0.072, top - 0.12), (-0.072, top - 0.12)], 0.14,
                         front_frame(x, 0.0, z), "WoodDark", bevel=0.02, bevel_seg=2)
            wob(leg, 0.003, 0.4, levels=0)
            ubox("Piedino", (x, 0.02, z), (0.17, 0.04, 0.17), "WoodDeep", bevel=0.015)
    # traverse e ripiano basso a doghe
    for sx in (-1, 1):
        ubox("Traversa", (sx * (w / 2 - 0.12), 0.2, 0), (0.08, 0.07, d - 0.2), "WoodMid", bevel=0.015)
    for i in range(3):
        z = -0.25 + i * 0.25
        s = ubox("Doga", (0, 0.25, z), (w - 0.3, 0.05, 0.22), "WoodMid", bevel=0.012, tint=0.06)
        jit(s, 0.4, 0.002)
    # cassetta degli scarti sotto il banco
    cx, cy, cz = -0.5, 0.28, 0.0
    for sz in (-1, 1):
        ubox("Cassa", (cx, cy + 0.07, cz + sz * 0.19), (0.5, 0.14, 0.025), "WoodDeep", bevel=0.008)
    for sx in (-1, 1):
        ubox("Cassa", (cx + sx * 0.24, cy + 0.07, cz), (0.025, 0.14, 0.38), "WoodDeep", bevel=0.008)
    for i in range(5):
        p = ubox("Scarto", (cx - 0.15 + i * 0.075, cy + 0.12 + random.uniform(0, 0.05), cz + random.uniform(-0.1, 0.1)),
                 (0.05, 0.22, 0.07), "WoodLight", bevel=0.01, rot=(random.uniform(-15, 15), random.uniform(0, 90), random.uniform(-15, 15)), tint=0.08)
    # pannello forato sul retro (-Z) con cornice e montanti
    bz = -d / 2 + 0.02
    for sx in (-1, 1):
        ubox("Montante", (sx * (w / 2 - 0.15), top + 0.55, bz), (0.08, 1.1, 0.07), "WoodDark", bevel=0.015)
    ubox("Pannello", (0, top + 0.62, bz), (w - 0.38, 0.8, 0.035), "WoodMid", bevel=0.01)
    ubox("CorniceSu", (0, top + 1.04, bz + 0.005), (w - 0.2, 0.07, 0.07), "WoodDeep", bevel=0.015)
    ubox("CorniceGiu", (0, top + 0.2, bz + 0.005), (w - 0.3, 0.05, 0.06), "WoodDeep", bevel=0.012)
    for ix in range(13):
        for iy in range(5):
            x = -0.84 + ix * 0.14
            y = top + 0.32 + iy * 0.15
            ucyl("Foro", (x, y, bz + 0.018), 0.011, 0.004, "WoodDeep", rot=(90, 0, 0), bevel=0.0, verts=6, seg=1, tint=0.0)
    return {"ao_distance": 0.4}


def hanging_peg(x, y, z):
    ucyl("Piolo", (x, y, z + 0.04), 0.008, 0.07, "Iron", rot=(90, 0, 0), bevel=0.002, verts=8)


def hammer(head_center, axis_rot, length=0.36):
    """Martello: manico affusolato + testa con penna a granchio. axis_rot orienta il manico (asse Y di Unity)."""
    hc = Vector(head_center)
    hand_c = hc + rot_vec((0, -length / 2, 0), axis_rot)
    ucyl("Manico", tuple(hand_c), 0.018, length, "WoodMid", rot=axis_rot, bevel=0.006, verts=12, radius_top=0.014)
    grip = hc + rot_vec((0, -length + 0.06, 0), axis_rot)
    ucyl("Impugnatura", tuple(grip), 0.021, 0.11, "RedSanta", rot=axis_rot, bevel=0.006, verts=12)
    # testa: cilindro trasversale + penna
    head_axis = rot_vec((1, 0, 0), axis_rot)
    head = ucyl("Testa", tuple(hc + head_axis * 0.03), 0.028, 0.11, "MetalDark", bevel=0.006, verts=12)
    head.rotation_euler = (_M @ head_axis).to_track_quat("Z", "Y").to_euler()
    claw = [(0.0, -0.022), (0.0, 0.022), (-0.06, 0.012), (-0.09, -0.01), (-0.07, -0.006), (-0.05, -0.012)]
    hx = hc + rot_vec((-0.02, 0, 0), axis_rot)
    uprism("Penna", claw, 0.03, lambda u, v, w_: tuple(hx + rot_vec((u, v, w_), axis_rot)), "MetalDark", bevel=0.004)


def bench_props(kind):
    def build():
        w, d, top = BENCH_W, BENCH_D, BENCH_TOP
        bz = -d / 2 + 0.02
        random.seed(sum(map(ord, kind)))
        if kind == "Construction":
            # martello appeso
            hanging_peg(-0.65, top + 0.86, bz)
            hammer((-0.65, top + 0.83, bz + 0.07), (0, 0, 0))
            # sega appesa: lama con dentini + impugnatura sagomata
            blade = [(-0.3, -0.06)]
            for i in range(24):
                x = -0.28 + i * 0.022
                blade += [(x, -0.06 - (0.014 if i % 2 == 0 else 0.0))]
            blade += [(0.2, -0.06), (0.2, 0.06), (-0.3, 0.025)]
            hanging_peg(-0.12, top + 0.8, bz)
            uprism("Lama", blade, 0.008, lambda u, v, w_: (-0.12 + u, top + 0.7 + v + u * 0.12, bz + 0.07 + w_), "MetalLight", bevel=0.002, bevel_seg=1)
            handle = [(0.18, -0.06), (0.36, -0.08), (0.38, 0.06), (0.24, 0.1), (0.18, 0.07)]
            uprism("ImpugnaturaSega", handle, 0.035, lambda u, v, w_: (-0.12 + u, top + 0.7 + v + u * 0.12, bz + 0.07 + w_), "WoodMid", bevel=0.01, bevel_seg=3)
            ucyl("ForoSega", (-0.12 + 0.29, top + 0.7 + 0.01 + 0.29 * 0.12, bz + 0.09), 0.025, 0.006, "WoodDeep", rot=(90, 0, 0), bevel=0.0, verts=12, seg=1)
            # pinze
            hanging_peg(0.55, top + 0.88, bz)
            for sx in (-1, 1):
                pts = [(0.55 + sx * 0.012, top + 0.86, bz + 0.07), (0.55 + sx * 0.03, top + 0.74, bz + 0.07), (0.55 + sx * 0.05, top + 0.56, bz + 0.07)]
                utube("Manico", pts, 0.016, "RedSanta", sides=8, radius_end=0.014)
            ulathe("Ganasce", (0.55, top + 0.86, bz + 0.07), [(0.0, 0.0), (0.022, 0.01), (0.018, 0.08), (0.0, 0.1)], "MetalLight", seg=10)
            # squadra da falegname (a L)
            sq = [(0.0, 0.0), (0.3, 0.0), (0.3, 0.04), (0.04, 0.04), (0.04, 0.3), (0.0, 0.3)]
            hanging_peg(0.86, top + 0.84, bz)
            uprism("Squadra", sq, 0.02, front_frame(0.72, top + 0.52, bz + 0.065), "GreenElf", bevel=0.004)
            for i in range(6):
                ubox("Tacca", (0.74 + 0.04 + i * 0.04, top + 0.555, bz + 0.077), (0.004, 0.02, 0.003), "Cream", bevel=0.0, seg=1, tint=0.0)
            # morsa robusta sul banco
            vx, vy, vz = -0.9, top, -0.2
            ubox("Base", (vx, vy + 0.03, vz), (0.24, 0.06, 0.28), "Iron", bevel=0.015)
            ubox("Corpo", (vx, vy + 0.12, vz - 0.06), (0.18, 0.14, 0.12), "Iron", bevel=0.02)
            ubox("Ganascia", (vx, vy + 0.19, vz - 0.0), (0.2, 0.06, 0.03), "MetalLight", bevel=0.008)
            ubox("Ganascia", (vx, vy + 0.19, vz + 0.09), (0.2, 0.06, 0.03), "MetalLight", bevel=0.008)
            ubox("Mobile", (vx, vy + 0.12, vz + 0.1), (0.16, 0.12, 0.05), "Iron", bevel=0.015)
            ucyl("Vite", (vx, vy + 0.1, vz + 0.2), 0.014, 0.22, "MetalLight", rot=(90, 0, 0), bevel=0.003, verts=10)
            ucyl("Leva", (vx, vy + 0.1, vz + 0.3), 0.01, 0.24, "MetalLight", rot=(0, 0, 90), bevel=0.003, verts=8)
            for sx in (-1, 1):
                usphere("Pomolo", (vx + sx * 0.125, vy + 0.1, vz + 0.3), 0.018, "RedSanta", seg=10, rings=6)
            # martello appoggiato sul banco
            hammer((0.88, top + 0.03, -0.31), (90, 78, 0))
            # trucioli arricciati
            for (x, z, a) in ((-0.3, -0.3, 20), (0.2, -0.38, -30), (-0.1, -0.42, 60), (0.38, -0.3, 10)):
                pts = []
                for i in range(14):
                    t = i / 13 * 2.4 * math.pi
                    r = 0.035 - i * 0.0015
                    p = rot_vec((math.cos(t) * r + i * 0.004, 0.01 + math.sin(t) * r * 0.6 + 0.02, 0), (0, a, 0))
                    pts.append((x + p.x, top + p.y, z + p.z))
                utube("Truciolo", pts, 0.009, "WoodEnd", sides=6, caps=False)
            # matita e barattolo di chiodi
            ucyl("Matita", (0.2, top + 0.012, -0.2), 0.009, 0.18, "ClothYellow", rot=(0, 35, 90), bevel=0.002, verts=6)
            ulathe("Barattolo", (0.45, top, -0.38), [(0.0, 0.0), (0.06, 0.0), (0.06, 0.1), (0.054, 0.11), (0.0, 0.11)], "MetalLight", seg=16)
        elif kind == "Paint":
            paints = ["RedSanta", "ClothBlue", "ClothYellow", "GreenElf", "ClothPurple"]
            for i, c in enumerate(paints):
                x = -0.9 + i * 0.45
                z = -0.28 + random.uniform(-0.03, 0.03)
                ulathe("Latta", (x, top, z), [(0.0, 0.0), (0.088, 0.0), (0.09, 0.006), (0.09, 0.15), (0.094, 0.155), (0.094, 0.165), (0.0, 0.165)], "MetalLight", seg=16)
                ulathe("Etichetta", (x, top + 0.035, z), [(0.0915, 0.0), (0.0915, 0.08)], c, seg=16)
                ucyl("Coperchio", (x, top + 0.17, z), 0.08, 0.012, c, bevel=0.003, verts=16, seg=1)
                # colature di vernice
                for j in range(3):
                    a = random.uniform(0, 2 * math.pi)
                    ln = random.uniform(0.03, 0.08)
                    px, pz = x + math.cos(a) * 0.093, z + math.sin(a) * 0.093
                    ucyl("Colatura", (px, top + 0.165 - ln / 2, pz), 0.012, ln, c, bevel=0.0, verts=6)
                    usphere("Goccia", (px, top + 0.165 - ln, pz), 0.014, c, seg=6, rings=4)
                # manico a filo
                pts = [(x - 0.09, top + 0.15, z), (x - 0.07, top + 0.26, z), (x, top + 0.29, z), (x + 0.07, top + 0.26, z), (x + 0.09, top + 0.15, z)]
                utube("ManicoLatta", smooth_curve(pts, 3), 0.004, "MetalDark", sides=6)
                # barattolini appesi al pannello
                hanging_peg(x, top + 0.78, bz)
                ulathe("Vasetto", (x, top + 0.62, bz + 0.08), [(0.0, 0.0), (0.05, 0.0), (0.05, 0.11), (0.04, 0.12), (0.0, 0.12)], c, seg=16)
            # pennelli in un barattolo
            jx, jz = 0.75, 0.25
            ulathe("Barattolo", (jx, top, jz), [(0.0, 0.0), (0.07, 0.0), (0.072, 0.14), (0.066, 0.145), (0.0, 0.145)], "MetalDark", seg=18)
            for i in range(3):
                tilt = (i - 1) * 12
                base = Vector((jx + (i - 1) * 0.02, top + 0.06, jz))
                ucyl("Manico", tuple(base + rot_vec((0, 0.14, 0), (0, 0, tilt))), 0.011, 0.28, "WoodMid", rot=(0, 0, tilt), bevel=0.003, verts=8, radius_top=0.008)
                tip = base + rot_vec((0, 0.29, 0), (0, 0, tilt))
                ucyl("Ghiera", tuple(tip), 0.015, 0.04, "MetalLight", rot=(0, 0, tilt), bevel=0.003, verts=10)
                usphere("Setole", tuple(tip + rot_vec((0, 0.045, 0), (0, 0, tilt))), 0.017, paints[i], scale=(1, 1.8, 0.7), rot=(0, 0, tilt), seg=10, rings=6)
            # tavolozza a fagiolo con foro per il pollice e macchie di colore
            pal = []
            for i in range(28):
                a = 2 * math.pi * i / 28
                r = 0.2 * (1.0 - 0.18 * max(0.0, math.cos(a - 0.4)) ** 6)
                pal.append((math.cos(a) * r * 1.1, math.sin(a) * r * 0.85))
            uprism("Tavolozza", pal, 0.018, top_frame(-0.2, top + 0.01, 0.25, yaw=-15), "WoodEnd", bevel=0.006)
            ucyl("Pollice", (-0.2 + 0.13, top + 0.02, 0.25 + 0.02), 0.025, 0.004, "WoodDeep", bevel=0.0, verts=12, seg=1)
            for i in range(4):
                usphere("Colore", (-0.2 + math.cos(i * 1.4 + 2) * 0.11, top + 0.02, 0.25 + math.sin(i * 1.4 + 2) * 0.1), 0.035, paints[i], scale=(1, 0.35, 1), seg=12, rings=6)
            # straccio stropicciato
            rag = usphere("Straccio", (0.3, top + 0.025, 0.3), 0.11, "Cream", scale=(1.2, 0.3, 0.9), seg=14, rings=8)
            soft(rag, 1)
            wob(rag, 0.02, 0.08, levels=0)
        else:  # Wrap
            # supporto con rotolo di carta regalo a righe
            for sx in (-1, 1):
                ubox("Sostegno", (sx * 0.5, top + 0.18, -0.3), (0.06, 0.36, 0.08), "WoodDark", bevel=0.012)
                ubox("Piede", (sx * 0.5, top + 0.02, -0.3), (0.1, 0.04, 0.2), "WoodDark", bevel=0.012)
            ucyl("Asta", (0, top + 0.3, -0.3), 0.018, 1.06, "MetalLight", rot=(0, 0, 90), bevel=0.004, verts=10)
            ucyl("Rotolo", (0, top + 0.3, -0.3), 0.14, 0.9, "RedSanta", rot=(0, 0, 90), bevel=0.012, verts=28, seg=2)
            for i in range(7):
                utorus("Riga", (-0.39 + i * 0.13, top + 0.3, -0.3), 0.14, 0.012, "Cream", rot=(0, 0, 90), mseg=28, nseg=5)
            # foglio srotolato che scende sul banco
            sheet = ubox("Foglio", (0, top + 0.006, -0.08), (0.86, 0.006, 0.36), "RedSanta", bevel=0.002, seg=1)
            for i in range(7):
                ubox("RigaFoglio", (-0.39 + i * 0.13, top + 0.01, -0.08), (0.03, 0.004, 0.36), "Cream", bevel=0.0, seg=1)
            # bobine di nastro
            ribbons = ["GreenElf", "Gold", "ClothBlue"]
            for i, c in enumerate(ribbons):
                x = -0.8 + i * 0.28
                ulathe("Bobina", (x, top, 0.25), [(0.03, 0.0), (0.1, 0.0), (0.1, 0.07), (0.03, 0.07)], c, seg=24)
                ucyl("Anima", (x, top + 0.035, 0.25), 0.032, 0.074, "WoodEnd", bevel=0.004, verts=14)
                pts = [(x + 0.1, top + 0.035, 0.25), (x + 0.14, top + 0.01, 0.3), (x + 0.16, top + 0.004, 0.38)]
                tail = utube("Coda", pts, 0.025, c, sides=6)
                flatten(tail, 1.0, 0.25, 1.0)
            gift((0.7, top, 0.2), (0.32, 0.26, 0.32), "GreenElf", "Gold", 15)
            # forbici
            sx, sz = 0.1, 0.32
            for s in (-1, 1):
                ucyl("Anello", (sx - 0.05, top + 0.01, sz + s * 0.03), 0.028, 0.016, "RedSanta", bevel=0.005, verts=16)
                ucyl("Buco", (sx - 0.05, top + 0.012, sz + s * 0.03), 0.016, 0.02, "WoodLight", bevel=0.002, verts=12, seg=1, tint=0.0)
                uprism("Lama", [(0.0, -0.012), (0.17, 0.0), (0.0, 0.012)], 0.006,
                       top_frame(sx - 0.02, top + 0.012, sz + s * 0.006, yaw=s * 6), "MetalLight", bevel=0.002, bevel_seg=1)
            ucyl("Perno", (sx - 0.02, top + 0.018, sz), 0.008, 0.012, "Gold", bevel=0.002, verts=8)
            # nastro adesivo
            ulathe("Scotch", (-0.25, top, 0.3), [(0.025, 0.0), (0.05, 0.0), (0.05, 0.03), (0.025, 0.03)], "SnowShade", seg=18)
        return {"ao_distance": 0.3}
    return build


def bench_pads():
    top = BENCH_TOP
    for i, x in enumerate((-0.65, -0.05, 0.72)):
        out = i == 2
        r = 0.27 if out else 0.24
        ulathe("Vassoio", (x, top, 0.08), [(0.0, 0.0), (r, 0.0), (r, 0.01), (r - 0.012, 0.016), (r - 0.024, 0.013), (0.0, 0.012)],
               "Cream" if out else "WoodDark", seg=36)
        utorus("Bordo", (x, top + 0.013, 0.08), r - 0.008, 0.009, "Gold" if out else "WoodDeep", mseg=36, nseg=6)
        if out:
            uprism("Stella", star_outline(5, 0.07, 0.032), 0.004, top_frame(x, top + 0.015, 0.08), "Gold", bevel=0.0)
    arrow = [(-0.11, -0.022), (0.04, -0.022), (0.04, -0.06), (0.12, 0.0), (0.04, 0.06), (0.04, 0.022), (-0.11, 0.022)]
    uprism("Freccia", [(u, -v) for (u, v) in reversed(arrow)], 0.006, top_frame(0.34, top + 0.004, 0.08), "WoodDeep", bevel=0.002, bevel_seg=1)
    return {"ao_distance": 0.15}


# ============================================================================ SLITTA
def sleigh():
    random.seed(7)
    # pattini dorati con riccio anteriore + sostegni
    for side in (-1, 1):
        x = side * 0.62
        path = [(x, 0.26, -1.62), (x, 0.14, -1.5), (x, 0.1, -1.2), (x, 0.1, 0.2), (x, 0.1, 1.2), (x, 0.13, 1.65),
                (x, 0.3, 1.95), (x, 0.55, 2.0), (x, 0.72, 1.85), (x, 0.74, 1.66), (x, 0.64, 1.6), (x, 0.58, 1.68)]
        utube("Pattino", smooth_curve(path, 5), 0.042, "Gold", sides=12)
        usphere("Ricciolo", (x, 0.58, 1.68), 0.05, "Gold", seg=12, rings=8)
        for pz in (-1.0, 0.1, 1.0):
            # sostegno a "lira": due curve dorate che si aprono verso il pattino
            for sz in (-1, 1):
                st = [(x, 0.47, pz), (x, 0.36, pz + sz * 0.03), (x, 0.2, pz + sz * 0.13), (x, 0.11, pz + sz * 0.15)]
                utube("Sostegno", smooth_curve(st, 4), 0.026, "Gold", sides=10)
            ubox("Attacco", (x, 0.47, pz), (0.09, 0.06, 0.12), "WoodDark", bevel=0.02)
    for z in (-1.0, 1.0):
        ucyl("Traversa", (0, 0.16, z), 0.03, 1.24, "WoodDark", rot=(0, 0, 90), bevel=0.006, verts=12)
    # fondo a doghe
    for i in range(6):
        x = -0.5 + i * 0.2
        ubox("Doga", (x, 0.5, 0.0), (0.19, 0.07, 2.7), "WoodDeep", bevel=0.015, tint=0.06)
    # fianchi sagomati (profilo morbido) con bordo dorato
    top_edge = [(1.62, 1.16), (1.42, 1.02), (0.9, 0.9), (0.2, 0.86), (-0.5, 0.92), (-0.95, 1.1), (-1.2, 1.34), (-1.32, 1.47), (-1.42, 1.46)]
    front = [(1.15, 0.46), (1.45, 0.55), (1.64, 0.76), (1.7, 0.98), (1.62, 1.16)]
    profile = [(-1.42, 0.46)] + smooth_curve(front, 4)[:-1] + smooth_curve(top_edge, 5) + [(-1.44, 1.3), (-1.44, 0.5)]
    for side in (-1, 1):
        x = side * 0.62
        uprism("Fianco", profile, 0.07, side_frame(x, 0.0, 0.0), "RedSanta", bevel=0.018, bevel_seg=3, tint=0.02)
        rim = [(x + side * 0.02, y + 0.012, z) for (z, y) in smooth_curve(front[2:] + top_edge[1:], 5)]
        utube("Bordo", rim, 0.032, "Gold", sides=10)
        # pannello decorativo ovale con cornice dorata e stella
        inner, ring = [], []
        for i in range(32):
            a = 2 * math.pi * i / 32
            inner.append((0.0 + math.cos(a) * 0.62, 0.7 + math.sin(a) * 0.12))
            ring.append((x + side * 0.05, 0.7 + math.sin(a) * 0.125, 0.0 + math.cos(a) * 0.625))
        uprism("Riquadro", inner, 0.012, side_frame(x + side * 0.04, 0.0, 0.0), "RedDeep", bevel=0.004)
        ring.append(ring[0])
        utube("Cornice", ring, 0.011, "Gold", sides=6, caps=False)
        uprism("Stella", star_outline(5, 0.085, 0.038), 0.012, lambda u, v, w_, xx=x, sd=side: (xx + sd * (0.052 + w_), 0.7 + v, u), "Gold", bevel=0.003)
        for dz in (-0.38, 0.38):
            usphere("Perla", (x + side * 0.05, 0.7, dz), 0.022, "Gold", seg=10, rings=6)
        # riccio dorato davanti, in cima al profilo
        scroll = []
        for i in range(30):
            t = i / 29
            a = -math.pi / 2 + t * 1.6 * 2 * math.pi
            r = 0.16 * (1 - t * 0.75)
            scroll.append((x + side * 0.01, 1.2 + math.sin(a) * r * -1.0 + 0.0, 1.52 + math.cos(a) * r))
        utube("Riccio", scroll, 0.042, "Gold", sides=10, radius_end=0.024)
        # lanterna sul davanti (gabbia di ferro; la luce è un pezzo separato)
        lx, ly, lz = x, 1.3, 1.2
        ucyl("Asta", (lx, 1.1, lz), 0.014, 0.2, "Iron", bevel=0.003, verts=8)
        ucyl("BaseLanterna", (lx, ly - 0.08, lz), 0.06, 0.03, "Iron", bevel=0.006, verts=12)
        ulathe("Cappello", (lx, ly + 0.07, lz), [(0.07, 0.0), (0.075, 0.01), (0.03, 0.06), (0.01, 0.08), (0.0, 0.085)], "Iron", seg=12)
        usphere("Pomolo", (lx, ly + 0.17, lz), 0.018, "Gold", seg=8, rings=5)
        for i in range(4):
            a = math.radians(45 + i * 90)
            ucyl("Sbarra", (lx + math.cos(a) * 0.058, ly, lz + math.sin(a) * 0.058), 0.007, 0.15, "Iron", bevel=0.002, verts=6)
    # schienale imbottito capitonné e sedile
    back = ubox("Schienale", (0, 1.1, -1.28), (1.2, 0.74, 0.16), "RedDeep", bevel=0.07, rot=(-14, 0, 0), seg=3)
    soft(back, 1)
    for ix in range(4):
        for iy in range(2):
            p = Vector((0, 1.1, -1.28)) + rot_vec((-0.39 + ix * 0.26, -0.13 + iy * 0.26, 0.085), (-14, 0, 0))
            usphere("Bottone", tuple(p), 0.022, "Gold", scale=(1, 1, 0.6), rot=(-14, 0, 0), seg=10, rings=6)
    ucyl("Cimasa", (0, 1.5, -1.38), 0.045, 1.32, "Gold", rot=(0, 0, 90), bevel=0.006, verts=12)
    seat = ubox("Sedile", (0, 0.62, -0.62), (1.1, 0.2, 0.9), "Cream", bevel=0.08, seg=3)
    soft(seat, 1)
    wob(seat, 0.008, 0.3, levels=0)
    ubox("Frontale", (0, 0.78, 1.48), (1.18, 0.5, 0.07), "RedDeep", bevel=0.025, rot=(18, 0, 0))
    ubox("Retro", (0, 0.85, -1.36), (1.18, 0.8, 0.06), "RedSanta", bevel=0.02)
    # coperta a quadri piegata sul sedile
    blanket = ubox("Coperta", (-0.18, 0.75, -0.7), (0.55, 0.07, 0.5), "GreenDeep", bevel=0.03, rot=(0, 12, 0), seg=2)
    soft(blanket, 1)
    for i in range(3):
        ubox("Riga", (-0.18, 0.79, -0.85 + i * 0.15), (0.55, 0.004, 0.025), "Cream", bevel=0.0, rot=(0, 12, 0), seg=1)
    # sacco dei regali (forma morbida e irregolare) con collo arricciato e corda
    sack = usphere("Sacco", (0, 0.95, 0.5), 0.5, "GreenDeep", scale=(1.0, 0.95, 1.12), seg=24, rings=14)
    soft(sack, 1)
    wob(sack, 0.05, 0.6, levels=0)
    neck = ulathe("Collo", (0, 1.22, 0.5), [(0.34, 0.0), (0.24, 0.08), (0.15, 0.15), (0.14, 0.19), (0.2, 0.25), (0.27, 0.29), (0.25, 0.31), (0.17, 0.27), (0.0, 0.22)], "GreenDeep", seg=28)
    wob(neck, 0.03, 0.05, levels=1)
    utorus("Corda", (0, 1.39, 0.5), 0.155, 0.028, "Gold", mseg=24, nseg=8)
    for (dx, dz) in ((0.12, 0.17), (0.2, 0.13)):
        utube("Capo", [(dx * 0.8, 1.39, 0.5 + dz * 0.8), (dx + 0.06, 1.27, 0.5 + dz + 0.1), (dx + 0.05, 1.12, 0.5 + dz + 0.16)], 0.02, "Gold", sides=8, radius_end=0.016)
    # toppa cucita sul sacco
    patch = usphere("Toppa", (-0.3, 0.98, 0.98), 0.12, "ClothYellow", scale=(1, 1, 0.25), rot=(0, -30, 0), seg=12, rings=6)
    gift((0.3, 1.25, 0.2), (0.3, 0.26, 0.3), "ClothBlue", "Gold", 20)
    gift((-0.28, 1.2, 0.7), (0.34, 0.24, 0.3), "ClothPink", "Cream", -15)
    gift((0.0, 1.32, 0.5), (0.26, 0.22, 0.26), "ClothYellow", "RedSanta", 5)
    gift((0.28, 0.69, -0.62), (0.28, 0.22, 0.28), "GreenElf", "Cream", 40)
    # rametto di agrifoglio sul davanti
    for i in range(3):
        a = -40 + i * 40
        leaf = usphere("Foglia", (0.0 + math.sin(math.radians(a)) * 0.08, 0.98 + math.cos(math.radians(a)) * 0.04, 1.53), 0.06, "GreenElf",
                       scale=(1.4, 0.55, 0.2), rot=(0, 0, a), seg=10, rings=6)
    for i in range(3):
        usphere("Bacca", (-0.03 + i * 0.03, 0.97 + (i % 2) * 0.03, 1.56), 0.022, "RedSanta", seg=10, rings=6)
    # campanelli
    for side in (-1, 1):
        for j in range(3):
            bx = side * 0.66
            bz = 0.6 - j * 0.45
            ulathe("Campanello", (bx, 0.84, bz), [(0.0, 0.0), (0.035, 0.0), (0.03, 0.02), (0.02, 0.05), (0.008, 0.06), (0.0, 0.065)], "Gold", rot=(180, 0, 0), seg=12)
    return {"ao_distance": 0.5, "preview_yaw": 125.0}


def sleigh_lamps():
    for side in (-1, 1):
        usphere("Luce", (side * 0.62, 1.3, 1.2), 0.05, "LampGlow", scale=(1, 1.25, 1), seg=12, rings=8)
    return {"ao": False}


# ============================================================================ PACCHI REGALO (unitari)
GIFT_COMBOS = [("ClothBlue", "Gold"), ("ClothYellow", "RedSanta"), ("RedSanta", "Cream"), ("GreenElf", "Gold"),
               ("RedSanta", "Gold"), ("ClothBlue", "Cream"), ("ClothPink", "Cream"), ("GreenElf", "Cream")]


def gift_unit(box_mat, rib_mat):
    def build():
        # Pacco di 1 x 1 x 1 m con la base nell'origine: in Unity viene scalato alla misura richiesta.
        gift((0, 0, 0), (1.0, 1.0, 1.0), box_mat, rib_mat, 0.0)
        return {"ao_distance": 0.5}
    return build


MODELS = {
    "Furniture_Shelf_Frame": shelf_frame,
    "Furniture_Shelf_Sign_Wood": shelf_sign("Wood"),
    "Furniture_Shelf_Sign_Cloth": shelf_sign("Cloth"),
    "Furniture_Shelf_Sign_Metal": shelf_sign("Metal"),
    "Furniture_Shelf_Sign_Toys": shelf_sign("Toys"),
    "Furniture_ShelfGoods_Wood": shelf_goods("Wood"),
    "Furniture_ShelfGoods_Cloth": shelf_goods("Cloth"),
    "Furniture_ShelfGoods_Metal": shelf_goods("Metal"),
    "Furniture_Bench_Base": bench_base,
    "Furniture_Bench_Props_Construction": bench_props("Construction"),
    "Furniture_Bench_Props_Paint": bench_props("Paint"),
    "Furniture_Bench_Props_Wrap": bench_props("Wrap"),
    "Furniture_Bench_Pads": bench_pads,
    "Furniture_Sleigh": sleigh,
    "Furniture_Sleigh_Lamps": sleigh_lamps,
}
for _b, _r in GIFT_COMBOS:
    MODELS["Furniture_Gift_%s_%s" % (_b, _r)] = gift_unit(_b, _r)
