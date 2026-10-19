# -*- coding: utf-8 -*-
"""
Personaggi di Elfshift: braccio in prima persona (manica, polsino di pelliccia, muffola a maglia).
Convenzioni elfkit: metri, Z in alto, davanti del modello verso -Y (in Unity: +Z).

Char_FPArm (braccio DESTRO; il sinistro si ottiene in Unity specchiando l'asse X):
  - il braccio va lungo +Z (in Unity: asse Y locale del pezzo "HandRight"), da 0 (manica, fuori schermo) a ~0.66 (punta);
  - il dorso della mano guarda +Y (verso la telecamera), il palmo -Y;
  - il pollice sta sul lato +X (in Unity: -X, cioè verso il centro dello schermo per il braccio destro).
"""
import math
import random

import elfkit as k
import bpy  # noqa: F401
import bmesh
from mathutils import Matrix, Vector, noise


def _obj_from_bm(name, bm, mats, smooth=True, tint=0.02):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    for m in mats:
        me.materials.append(k.material(m))
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    obj["tint"] = tuple(1.0 + random.uniform(-tint, tint) for _ in range(3))
    return obj


def _grid_surface(bm, func, nu, nv, close_v=True):
    """Superficie parametrica: func(u, v) -> Vector, u in [0,1] (righe), v in [0,1) (giro). Restituisce le righe di vertici."""
    rows = []
    for i in range(nu + 1):
        u = i / nu
        rows.append([bm.verts.new(func(u, j / nv)) for j in range(nv)])
    faces = []
    for i in range(nu):
        for j in range(nv if close_v else nv - 1):
            j2 = (j + 1) % nv
            faces.append((i, bm.faces.new((rows[i][j], rows[i][j2], rows[i + 1][j2], rows[i + 1][j]))))
    return rows, faces


# ============================================================================ braccio in prima persona
def _sleeve():
    """Manica verde a campana con pieghe morbide di stoffa."""
    bm = bmesh.new()
    z0, z1 = -0.06, 0.445

    def radius(u):
        # stretta in basso, si allarga verso il polso (manica "da elfo")
        r = 0.074 + 0.012 * u + 0.02 * max(0.0, (u - 0.62) / 0.38) ** 1.6
        return r

    def f(u, v):
        a = 2 * math.pi * v
        z = z0 + (z1 - z0) * u
        r = radius(u)
        # pieghe: onde attorno al tubo che cambiano lungo la lunghezza + rumore morbido
        fold = 0.05 * math.sin(a * 3 + u * 5.0) * (0.3 + 0.7 * u)
        fold += 0.035 * noise.noise(Vector((math.cos(a) * 1.5, math.sin(a) * 1.5, z * 9.0)))
        crease = 0.025 * math.sin(z * 55.0 + math.sin(a * 2) * 1.5) * max(0.0, 1 - abs(u - 0.55) * 2.5)
        rr = r * (1 + fold + crease)
        return Vector((math.cos(a) * rr, math.sin(a) * rr * 0.93, z))

    rows, faces = _grid_surface(bm, f, 22, 28)
    # orlo interno: piccolo bordo che rientra sotto la pelliccia
    lip = [bm.verts.new((v.co.x * 0.86, v.co.y * 0.86, z1 - 0.01)) for v in rows[-1]]
    n = len(lip)
    for j in range(n):
        j2 = (j + 1) % n
        bm.faces.new((rows[-1][j], rows[-1][j2], lip[j2], lip[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _obj_from_bm("Manica", bm, ["GreenElf"])


def _fur_cuff(z, major, minor, name="Pelliccia", seg=40, tseg=12):
    """Polsino di pelliccia: un anello gonfio e irregolare (ciuffi morbidi)."""
    bm = bmesh.new()

    def f(u, v):
        a = 2 * math.pi * u
        b = 2 * math.pi * v
        dirr = Vector((math.cos(a), math.sin(a), 0))
        fluff = 1.0 + 0.16 * noise.noise(Vector((math.cos(a) * 3.0, math.sin(a) * 3.0, b * 0.8))) \
            + 0.08 * math.sin(a * 11 + b * 2)
        r = minor * fluff
        return dirr * (major + r * math.cos(b)) + Vector((0, 0, z + r * math.sin(b) * 1.15))

    rows = []
    for i in range(seg):
        rows.append([bm.verts.new(f(i / seg, j / tseg)) for j in range(tseg)])
    for i in range(seg):
        i2 = (i + 1) % seg
        for j in range(tseg):
            j2 = (j + 1) % tseg
            bm.faces.new((rows[i][j], rows[i2][j], rows[i2][j2], rows[i][j2]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _obj_from_bm(name, bm, ["FurWhite"])


def _knit_cuff(z0, z1, r, name="Polsino"):
    """Polsino della muffola a coste (righe verticali in rilievo)."""
    bm = bmesh.new()
    ribs = 18

    def f(u, v):
        a = 2 * math.pi * v
        rr = r * (1 + 0.06 * math.cos(a * ribs)) * (1 - 0.05 * math.sin(u * math.pi))
        return Vector((math.cos(a) * rr, math.sin(a) * rr * 0.85, z0 + (z1 - z0) * u))

    _grid_surface(bm, f, 4, ribs * 4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _obj_from_bm(name, bm, ["Cream"])


MIT_S = 1.14     # muffole un po' "cicciottelle": si leggono meglio in prima persona
MIT_Z0 = 0.475   # inizio della muffola
MIT_LEN = 0.165 * MIT_S  # lunghezza fino alla punta
BEND_PIVOT = MIT_Z0 + 0.09 * MIT_S
BEND_DEG = 24.0


def _bend(p):
    """Piega la parte alta della muffola verso il palmo (-Y), come una mano che sta per afferrare."""
    if p.z <= BEND_PIVOT:
        return p
    t = min(1.0, (p.z - BEND_PIVOT) / 0.08)
    ang = math.radians(BEND_DEG) * t
    rel = Vector((p.x, p.y, p.z - BEND_PIVOT))
    rot = Matrix.Rotation(-ang, 3, "X")
    q = rot @ rel
    return Vector((q.x, q.y, q.z + BEND_PIVOT))


def _mitten_shape(u, v):
    """Forma della muffola: uovo schiacciato, stretto al polso, largo sulle nocche, dorso bombato."""
    # u: 0 = polso, 1 = punta; v: giro
    a = 2 * math.pi * v
    s = -math.cos(math.pi * u)  # da -1 a 1, poli arrotondati
    t = (s + 1) / 2
    width = (0.084 + 0.034 * math.sin(math.pi * min(1.0, t * 1.25)) - 0.02 * max(0.0, t - 0.8) / 0.2) * MIT_S
    thick = (0.052 + 0.016 * math.sin(math.pi * min(1.0, t * 1.1))) * MIT_S
    ring = math.sin(math.pi * u)  # 0 ai poli
    x = math.cos(a) * width / 2 * max(ring, 0.0)
    y = math.sin(a) * thick / 2 * max(ring, 0.0)
    # dorso più bombato del palmo
    if y > 0:
        y *= 1.12
    z = MIT_Z0 + t * MIT_LEN
    return _bend(Vector((x, y + 0.004, z)))


def _mitten():
    """Muffola rossa a maglia con due righe color panna vicino al polso."""
    bm = bmesh.new()
    nu, nv = 30, 32
    rows, faces = _grid_surface(bm, _mitten_shape, nu, nv)
    for (i, f) in faces:
        u = (i + 0.5) / nu
        t = (-math.cos(math.pi * u) + 1) / 2
        stripe = (0.13 < t < 0.19) or (0.25 < t < 0.31)
        f.material_index = 1 if stripe else 0
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _obj_from_bm("Muffola", bm, ["RedSanta", "Cream"])


def _thumb():
    """Pollice: salsicciotto morbido che parte dal lato della muffola e punta in avanti e verso l'interno."""
    S = MIT_S
    base = Vector((0.036 * S, -0.004, MIT_Z0 + 0.04 * S))
    pts = [base,
           base + Vector((0.02, -0.006, 0.022)) * S,
           base + Vector((0.032, -0.012, 0.05)) * S,
           base + Vector((0.034, -0.018, 0.072)) * S]
    smooth_pts = []
    for i in range(len(pts) - 1):
        for s in range(4):
            smooth_pts.append(pts[i].lerp(pts[i + 1], s / 4))
    smooth_pts.append(pts[-1])
    parts = [k.tube("Pollice", [tuple(p) for p in smooth_pts], 0.0215 * S, mat="RedSanta", sides=14, radius_end=0.0185 * S, caps=True)]
    tip = pts[-1]
    parts.append(k.sphere("PolliceTop", 0.0186 * S, loc=tuple(tip), scale=(1, 0.95, 1.05), mat="RedSanta", segments=14, rings=8))
    return parts


def _heart(loc_z):
    """Cuoricino color panna lavorato a maglia sul dorso della muffola."""
    pts = []
    for i in range(28):
        t = 2 * math.pi * i / 28
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x / 17.0 * 0.0145 * MIT_S, y / 17.0 * 0.0145 * MIT_S))
    # sagoma nel piano X-Z, spessore lungo Y; poi la appoggiamo sul dorso (normale +Y) seguendo la piega
    h = k.extrude_outline("Cuore", pts, 0.004, loc=(0, 0, 0), mat="Cream", bevel=0.0012)
    # posizione sulla superficie del dorso
    u = 0.5
    top = _mitten_shape(0.5, 0.25)  # punto del dorso (v=0.25 -> sin=1 -> +Y)
    h.location = (0.0, top.y + 0.0005, top.z + loc_z)
    h.rotation_euler = (math.radians(-12), 0, 0)
    return h


def _stitches():
    """Cuciture: trattini scuri lungo la cucitura laterale esterna della muffola."""
    parts = []
    for i in range(9):
        u = 0.36 + i * 0.065
        p = _mitten_shape(u, 0.5)            # lato -X (esterno per il braccio destro)
        q = _mitten_shape(u + 0.02, 0.5)
        mid = (p + q) / 2
        d = (q - p).normalized()
        st = k.box("Punto%d" % i, (0.004, 0.003, 0.009), loc=tuple(mid + Vector((-0.0015, 0, 0))), mat="RedDeep", bevel=0.0012, segments=1, tint=0.0)
        st.rotation_euler = d.to_track_quat("Z", "Y").to_euler()
    return parts


def _model_fp_arm():
    random.seed(7)
    _sleeve()
    _fur_cuff(0.45, 0.088, 0.024)
    _knit_cuff(0.43, 0.505, 0.047)
    _mitten()
    _thumb()
    _heart(0.0)
    _stitches()
    return {"ao_distance": 0.06, "ground": None, "preview_yaw": 150}


# ============================================================================ elfo intero (si vede solo come ombra)
def _model_elf_hand():
    """Mano piccola (muffola) del corpo intero: punta verso -Z, origine al polso."""
    random.seed(3)
    bm = bmesh.new()

    def f(u, v):
        p = _mitten_shape(u, v)
        return Vector((p.x * 0.72, p.y * 0.72, -(p.z - MIT_Z0) * 0.72))

    _grid_surface(bm, f, 16, 16)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _obj_from_bm("ManoElfo", bm, ["RedSanta"])
    return {"ao_distance": 0.03, "ground": None}


MODELS = {
    "Char_FPArm": _model_fp_arm,
}
