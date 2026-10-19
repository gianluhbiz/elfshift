# -*- coding: utf-8 -*-
"""
Oggetti di scena di Elfshift (gruppo "Prop_"): albero di Natale, finestre, bacheca, corona, lampade,
casse, barili, bastoncini di zucchero, pupazzo di neve, abeti, lampioni, staccionata, tappeto,
lucine, freccia dell'obiettivo, mucchi di neve.

Convenzioni (vedi elfkit.py): metri, Z in alto, il davanti del modello guarda verso -Y
(in Unity diventa +Z). L'origine è quella che si aspetta Assets/Editor/Generation/PropModels.cs.
"""
import math
import random

import bpy  # deve essere importato prima di bmesh
import bmesh
from mathutils import Matrix, Vector, noise

import elfkit as k


# ============================================================================ aiuti locali
def _new_obj(name, verts, faces, mats, face_mats=None, smooth=True, tint=0.03):
    """Crea un oggetto da liste di vertici/facce. mats = lista di chiavi della tavolozza."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    me.update()
    for m in mats:
        me.materials.append(k.material(m))
    if face_mats is not None:
        for i, p in enumerate(me.polygons):
            p.material_index = face_mats[i]
    obj = k._link(bpy.data.objects.new(name, me))
    for p in me.polygons:
        p.use_smooth = smooth
    obj["tint"] = tuple(1.0 + random.uniform(-tint, tint) for _ in range(3)) if tint > 0 else (1.0, 1.0, 1.0)
    # le normali devono puntare verso l'esterno
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    return obj


def radial(name, rows, seg, fn, mat, tint=0.03, cap_first=False, cap_last=False):
    """Superficie "a giro": per ogni riga t in rows e angolo theta, fn(t, theta) -> (x, y, z).
    Se fn restituisce lo stesso punto per tutti gli angoli (r = 0) la riga diventa un polo.
    cap_first/cap_last chiudono con una faccia piana le righe estreme (se non sono poli)."""
    verts, faces, ring_idx = [], [], []
    for t in rows:
        pts = [fn(t, 2 * math.pi * j / seg) for j in range(seg)]
        if max((Vector(p) - Vector(pts[0])).length for p in pts) < 1e-6:
            ring_idx.append([len(verts)])
            verts.append(pts[0])
        else:
            ring_idx.append(list(range(len(verts), len(verts) + seg)))
            verts.extend(pts)
    for a, b in zip(ring_idx[:-1], ring_idx[1:]):
        for j in range(seg):
            j2 = (j + 1) % seg
            if len(a) == 1 and len(b) == 1:
                continue
            if len(a) == 1:
                faces.append((a[0], b[j2], b[j]))
            elif len(b) == 1:
                faces.append((a[j], a[j2], b[0]))
            else:
                faces.append((a[j], a[j2], b[j2], b[j]))
    if cap_first and len(ring_idx[0]) > 1:
        faces.append(tuple(ring_idx[0]))
    if cap_last and len(ring_idx[-1]) > 1:
        faces.append(tuple(reversed(ring_idx[-1])))
    return _new_obj(name, verts, faces, [mat], tint=tint)


def bumpy(obj, amount=0.01, scale=3.0, seed=0.0):
    """Sposta i vertici lungo la normale con un rumore morbido (senza aggiungere triangoli)."""
    me = obj.data
    off = Vector((seed * 7.31, seed * 3.17, seed * 5.53))
    for v in me.vertices:
        n = noise.noise(v.co * scale + off)
        v.co += v.normal * n * amount
    me.update()
    return obj


class Tmpl:
    """Forma di base riutilizzabile (vertici + facce) da copiare molte volte in un Batch."""

    def __init__(self, bm):
        bm.verts.index_update()
        self.v = [v.co.copy() for v in bm.verts]
        self.f = [tuple(v.index for v in f.verts) for f in bm.faces]
        bm.free()


def tmpl_sphere(seg=8, rings=5):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    return Tmpl(bm)


def tmpl_ico(sub=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    return Tmpl(bm)


def tmpl_cyl(seg=8):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=1.0, radius2=1.0, depth=1.0)
    return Tmpl(bm)


def tmpl_box():
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    return Tmpl(bm)


def tmpl_leaf():
    """Fogliolina appuntita (lungo X), leggermente incurvata, con spessore."""
    bm = bmesh.new()
    outline = [(-1.0, 0.0), (-0.55, 0.42), (0.0, 0.5), (0.55, 0.36), (1.0, 0.0), (0.55, -0.36), (0.0, -0.5), (-0.55, -0.42)]
    top = [bm.verts.new((x, y, 0.12 * (1 - x * x))) for x, y in outline]
    bot = [bm.verts.new((x, y, -0.05 + 0.12 * (1 - x * x))) for x, y in outline]
    c1 = bm.verts.new((0, 0, 0.2))
    c2 = bm.verts.new((0, 0, 0.0))
    n = len(outline)
    for i in range(n):
        i2 = (i + 1) % n
        bm.faces.new((top[i], top[i2], c1))
        bm.faces.new((bot[i2], bot[i], c2))
        bm.faces.new((top[i], bot[i], bot[i2], top[i2]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return Tmpl(bm)


class Batch:
    """Accumula molte copie trasformate di forme di base in un unico oggetto (veloce, un solo materiale)."""

    def __init__(self, name, mat, smooth=True, tint=0.02):
        self.name, self.mat, self.smooth, self.tint = name, mat, smooth, tint
        self.v, self.f = [], []

    def add(self, tmpl, matrix):
        base = len(self.v)
        self.v.extend(matrix @ p for p in tmpl.v)
        self.f.extend(tuple(base + i for i in face) for face in tmpl.f)

    def build(self):
        if not self.v:
            return None
        return _new_obj(self.name, self.v, self.f, [self.mat], smooth=self.smooth, tint=self.tint)


def trs(loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    """Matrice da posizione, rotazione (gradi, XYZ) e scala."""
    from mathutils import Euler
    r = Euler(tuple(math.radians(a) for a in rot)).to_matrix().to_4x4()
    s = Matrix.Diagonal((scale[0], scale[1], scale[2], 1.0))
    return Matrix.Translation(Vector(loc)) @ r @ s


def look_matrix(loc, direction, up=(0, 0, 1), scale=(1, 1, 1)):
    """Matrice che orienta l'asse X locale lungo 'direction'."""
    d = Vector(direction).normalized()
    u = Vector(up)
    if abs(d.dot(u.normalized())) > 0.98:
        u = Vector((0, 1, 0))
    y = u.cross(d).normalized()
    z = d.cross(y).normalized()
    m = Matrix((d, y, z)).transposed().to_4x4()
    return Matrix.Translation(Vector(loc)) @ m @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1.0))


def lumpy_blob(name, loc, radii, mat="Snow", sub=3, amount=0.25, scale=2.2, seed=0.0, flat_bottom=None, tint=0.01):
    """Massa morbida e irregolare (neve, sassi): icosfera deformata dal rumore e schiacciata."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    off = Vector((seed * 3.1, seed * 1.7, seed * 2.3))
    for v in bm.verts:
        d = v.co.normalized()
        n = noise.noise(d * scale + off)
        v.co = d * (1.0 + n * amount)
        v.co = Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2]))
        if flat_bottom is not None and v.co.z < flat_bottom:
            v.co.z = flat_bottom + (v.co.z - flat_bottom) * 0.08
    obj = k.mesh_object(name, bm)
    obj.data.materials.append(k.material(mat))
    for p in obj.data.polygons:
        p.use_smooth = True
    obj.location = Vector(loc)
    obj["tint"] = tuple(1.0 + random.uniform(-tint, tint) for _ in range(3))
    return obj


def striped_tube(name, points, radius, mats, sides=12, stripes=4, turns_per_meter=6.0, caps=True, tint=0.01):
    """Tubo a righe elicoidali (bastoncino di zucchero). mats = (colore A, colore B)."""
    pts = [Vector(p) for p in points]
    n = len(pts)
    verts, faces, fmats, rings = [], [], [], []
    prev_side = None
    length = 0.0
    lengths = [0.0]
    for i in range(1, n):
        length += (pts[i] - pts[i - 1]).length
        lengths.append(length)
    for i, p in enumerate(pts):
        if i == 0:
            t = (pts[1] - pts[0]).normalized()
        elif i == n - 1:
            t = (pts[-1] - pts[-2]).normalized()
        else:
            t = (pts[i + 1] - pts[i - 1]).normalized()
        if prev_side is None:
            up = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
            side = t.cross(up).normalized()
        else:
            side = (prev_side - t * prev_side.dot(t)).normalized()
        prev_side = side
        other = t.cross(side).normalized()
        ring = []
        twist = lengths[i] * turns_per_meter * 2 * math.pi
        for j in range(sides):
            a = 2 * math.pi * j / sides + twist
            ring.append(len(verts))
            verts.append(p + (side * math.cos(a) + other * math.sin(a)) * radius)
        rings.append(ring)
    band = sides / (2.0 * stripes)
    for i in range(n - 1):
        for j in range(sides):
            j2 = (j + 1) % sides
            faces.append((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            # le colonne di facce seguono l'elica (anello ruotato lungo il tubo): righe con bordi lisci
            fmats.append(int(math.floor((j + 0.5) / band)) % 2)
    if caps:
        faces.append(tuple(reversed(rings[0])))
        fmats.append(1)
        faces.append(tuple(rings[-1]))
        fmats.append(1)
    return _new_obj(name, verts, faces, list(mats), fmats, tint=tint)


def gift(name, loc, size, paper, ribbon, rot_z=0.0, bow=True):
    """Pacco regalo: scatola morbida, nastro a croce e fiocco a due anelli. loc = centro della base."""
    x, y, z = loc
    sx, sy, sz = size
    m = trs((x, y, z), (0, 0, rot_z))
    parts = []
    b = k.box(name + "_carta", (sx, sy, sz), (0, 0, sz / 2), mat=paper, bevel=min(0.03, sz * 0.12), segments=3)
    parts.append(b)
    # coperchio leggermente più largo
    lid = k.box(name + "_coperchio", (sx + 0.025, sy + 0.025, sz * 0.18), (0, 0, sz - sz * 0.08), mat=paper, bevel=0.012, segments=2)
    parts.append(lid)
    rw = max(0.035, min(sx, sy) * 0.13)
    parts.append(k.box(name + "_nastroA", (rw, sy + 0.035, sz + 0.012), (0, 0, sz / 2 + 0.004), mat=ribbon, bevel=0.006, segments=2))
    parts.append(k.box(name + "_nastroB", (sx + 0.035, rw, sz + 0.012), (0, 0, sz / 2 + 0.004), mat=ribbon, bevel=0.006, segments=2))
    if bow:
        s = min(sx, sy) * 0.55
        for side in (-1, 1):
            loop = k.torus(name + "_anello", s * 0.32, s * 0.1, (side * s * 0.3, 0, sz + s * 0.2),
                           (0, 90 + side * 35, 0), mat=ribbon, major_seg=20, minor_seg=8)
            loop.scale = (1.0, 0.55, 1.0)
            parts.append(loop)
        parts.append(k.sphere(name + "_nodo", s * 0.13, (0, 0, sz + s * 0.07), mat=ribbon, segments=12, rings=8))
        for side in (-1, 1):
            parts.append(k.box(name + "_coda", (rw * 0.9, s * 0.5, 0.012), (side * s * 0.12, -s * 0.22, sz + 0.01),
                               (6, 0, side * 25), mat=ribbon, bevel=0.004, segments=1))
    for p in parts:
        p.matrix_basis = m @ p.matrix_basis
    return parts


def flat_tube(name, points, radius, mat, scale=(1.0, 0.4, 1.0), sides=12, radius_end=None):
    """Tubo schiacciato (nastri, code di sciarpa): la scala è applicata attorno al suo centro."""
    pts = [Vector(p) for p in points]
    c = sum(pts, Vector()) / len(pts)
    obj = k.tube(name, [p - c for p in pts], radius, mat=mat, sides=sides, radius_end=radius_end)
    obj.location = c
    obj.scale = scale
    return obj


def puffy_star(name, center, outer, inner, depth, mat="StarGlow", points=5, bevel=None):
    """Stella "gonfia" nel piano frontale (X,Z), spessore lungo Y."""
    outline = []
    for i in range(points * 2):
        a = math.pi / 2 + i * math.pi / points
        r = outer if i % 2 == 0 else inner
        outline.append((math.cos(a) * r, math.sin(a) * r))
    obj = k.extrude_outline(name, outline, depth, loc=center, mat=mat, bevel=bevel if bevel else depth * 0.42)
    obj.modifiers["Smusso"].segments = 4
    return obj


# ============================================================================ ALBERO DI NATALE
# livelli di rami: (raggio, quota della base, altezza). PropModels.cs usa gli stessi numeri
# (TreeTiers) per far correre il filo di lucine sulla superficie.
TREE_TIERS = [(1.30, 0.50, 1.15), (1.08, 1.05, 1.02), (0.88, 1.55, 0.92), (0.68, 2.02, 0.82), (0.46, 2.46, 0.72)]


def _tier_profile(R, H, droop, lobes, phase, snow=False, snow_from=0.42):
    """Restituisce fn(t, theta) per un "gonnellino" di rami a lobi morbidi (o per la neve sopra di esso).
    Il ramo: t 0..1 = dalla punta al bordo (superficie di sopra), t 1..2 = sotto, di nuovo verso il tronco.
    La neve: una fascia sul bordo visibile, che cola verso le punte dei lobi."""
    def surface(s, th):
        lobe = (0.5 + 0.5 * math.cos(lobes * th + phase)) ** 1.6
        small = math.cos(3 * lobes * th + phase * 2.0)
        r = R * s * (1 + 0.13 * (lobe - 0.5) * s * s + 0.035 * small * s * s)
        z = H * (1 - s) ** 1.1 - droop * lobe * s ** 3 - 0.02 * small * s ** 4
        return r, z, lobe

    def fn(t, th):
        if snow:
            lobe = (0.5 + 0.5 * math.cos(lobes * th + phase)) ** 1.6
            s_in = snow_from
            s_out = max(s_in + 0.02, 0.6 + 0.34 * lobe ** 1.5) if snow_from > 0 else 0.3 + 0.62 * lobe ** 1.5 + 0.04
            if t <= 1.0:
                s = s_in + (s_out - s_in) * t
                r, z, _ = surface(s, th)
                z += 0.03 * (1 - 0.4 * t * t)
                r += 0.012
            else:
                u = t - 1.0
                s = s_out - (s_out - s_in) * 0.2 * u
                r, z, _ = surface(s, th)
                z -= 0.035
        else:
            if t <= 1.0:
                r, z, _ = surface(t, th)
            else:
                u = t - 1.0
                r0, z0, _ = surface(1.0, th)
                r = r0 * (1 - u) * 0.98
                z = z0 - 0.04 * (1 - u) + (H * 0.22 + 0.04) * u
        return (r * math.cos(th), r * math.sin(th), z)
    return fn


def tier_point(R, H, droop, lobes, phase, s, th):
    """Punto sulla superficie del ramo (per appoggiarci palline)."""
    return Vector(_tier_profile(R, H, droop, lobes, phase)(s, th))


def christmas_tree():
    random.seed(11)
    parts = []
    # vaso di ceramica rossa con bordo arrotolato, terra e tronco
    parts.append(k.lathe("Vaso", [(0, 0), (0.33, 0), (0.37, 0.03), (0.40, 0.2), (0.42, 0.26), (0.47, 0.28), (0.48, 0.32),
                                  (0.46, 0.35), (0.41, 0.35), (0.39, 0.31), (0, 0.31)], mat="RedSanta", segments=40))
    parts.append(k.torus("BordoOro", 0.405, 0.018, (0, 0, 0.24), mat="Gold", major_seg=48, minor_seg=8))
    parts.append(k.torus("BordoOro2", 0.37, 0.012, (0, 0, 0.06), mat="Gold", major_seg=48, minor_seg=8))
    soil = lumpy_blob("Terra", (0, 0, 0.31), (0.39, 0.39, 0.03), mat="WoodDeep", sub=2, amount=0.3)
    parts.append(soil)
    trunk = k.cylinder("Tronco", 0.11, 0.5, (0, 0, 0.52), mat="Bark", bevel=0.01, verts=14, radius_top=0.08)
    bumpy(trunk, 0.008, 9.0)
    parts.append(trunk)

    greens = ["GreenPine", "GreenDeep", "GreenPine", "GreenDeep", "GreenPine"]
    rows_main = [0.0, 0.06, 0.15, 0.27, 0.4, 0.53, 0.66, 0.78, 0.88, 0.95, 1.0, 1.06, 1.3, 1.65, 2.0]
    ornaments = []
    for i, (R, zb, H) in enumerate(TREE_TIERS):
        lobes = 9 - i
        phase = i * 0.9
        droop = 0.13 * R
        tier = radial("Rami%d" % i, rows_main, 90, _tier_profile(R, H, droop, lobes, phase), greens[i], tint=0.03)
        tier.location.z = zb
        bumpy(tier, 0.022, 4.0, seed=i)
        parts.append(tier)
        # festoni dorati a "ghirlanda" tra le punte dei lobi (l'albero è in casa: niente neve)
        if 0 < i < len(TREE_TIERS) - 1 or i == 0:
            pts = []
            for j in range(lobes + 1):
                th0 = (2 * math.pi * j - phase) / lobes
                th1 = (2 * math.pi * (j + 1) - phase) / lobes
                for q in range(8):
                    u = q / 8
                    th = th0 + (th1 - th0) * u
                    p = tier_point(R, H, droop, lobes, phase, 0.8, th)
                    p = p + Vector((math.cos(th), math.sin(th), 0)) * (0.09 + 0.03 * math.sin(u * math.pi)) + Vector((0, 0, zb - 0.12 * math.sin(u * math.pi)))
                    pts.append(p)
            k.tube("Festone%d" % i, pts, 0.014, mat="Gold", sides=6, caps=False)
        # palline: appoggiate sui rami tra una colata di neve e l'altra, qualcuna appesa alle punte
        for j in range(lobes):
            th = (2 * math.pi * (j + 0.5) - phase) / lobes
            if i == len(TREE_TIERS) - 1 and j % 2:
                continue
            p = tier_point(R, H, droop, lobes, phase, 0.86, th)
            radial_dir = Vector((math.cos(th), math.sin(th), 0))
            p = p + radial_dir * 0.05 + Vector((0, 0, zb + 0.03))
            ornaments.append((tuple(p), (i * 3 + j) % 5, R))
            if j % 3 == 0 and i < 4:
                th2 = (2 * math.pi * j - phase) / lobes
                p2 = tier_point(R, H, droop, lobes, phase, 1.0, th2) + Vector((0, 0, zb - 0.1))
                ornaments.append((tuple(p2), (i + j + 2) % 5, R))
    ball_mats = ["RedSanta", "Gold", "ClothBlue", "RedDeep", "Cream"]
    for idx, (pos, mi, R) in enumerate(ornaments):
        rad = 0.07 if R > 0.6 else 0.055
        b = k.sphere("Pallina%d" % idx, rad, pos, mat=ball_mats[mi], segments=12, rings=8)
        parts.append(b)
        cap = k.cylinder("Tappo%d" % idx, rad * 0.32, rad * 0.35, (pos[0], pos[1], pos[2] + rad * 1.0), mat="Gold", bevel=0.003, verts=10)
        parts.append(cap)
        hook = k.torus("Gancio%d" % idx, rad * 0.22, rad * 0.06, (pos[0], pos[1], pos[2] + rad * 1.3), (90, 0, 0), mat="Gold", major_seg=10, minor_seg=4)
        parts.append(hook)

    # regali ai piedi dell'albero (posizioni di PropModels.cs, convertite: x' = -x, y' = -z)
    parts += gift("RegaloA", (-0.75, -0.55, 0), (0.42, 0.38, 0.34), "ClothBlue", "Gold", -20)
    parts += gift("RegaloB", (0.8, -0.6, 0), (0.38, 0.4, 0.3), "ClothYellow", "RedSanta", 25)
    parts += gift("RegaloC", (-0.1, -1.0, 0), (0.34, 0.34, 0.26), "RedSanta", "Cream", -5)
    parts += gift("RegaloD", (0.35, -1.05, 0), (0.24, 0.24, 0.22), "GreenElf", "Gold", -40)
    return {"ao_distance": 0.45, "ao_samples": 32}


def tree_star():
    puffy_star("Stella", (0, 0, 3.28), 0.26, 0.12, 0.1, mat="StarGlow")
    return {"ao": False}


# ============================================================================ LUCINE
def bulb_glass(mat):
    def build():
        # lampadina a goccia che pende verso il basso; l'origine è il punto d'attacco sul filo
        k.lathe("Vetro", [(0, -0.125), (0.012, -0.123), (0.026, -0.112), (0.036, -0.092), (0.04, -0.07),
                          (0.036, -0.05), (0.026, -0.036), (0.018, -0.028), (0, -0.028)], mat=mat, segments=12)
        return {"ao": False}
    return build


def bulb_cap():
    k.lathe("Portalampada", [(0, -0.034), (0.019, -0.034), (0.021, -0.028), (0.02, -0.008), (0.014, -0.0), (0, 0.004)],
            mat="GreenDeep", segments=10)
    return {"ao": False}


# ============================================================================ FINESTRA
WINDOW_W, WINDOW_H = 2.6, 2.4


def _curtain(name, x_inner, x_outer, top, bottom, tie_z, depth_y, mat="RedSanta", folds=5):
    """Tenda morbida a pieghe, raccolta da un fermatenda. x_inner = bordo verso la finestra."""
    cols, rows = 20, 16
    verts, faces = [], []
    width = abs(x_outer - x_inner)
    sgn = 1 if x_outer > x_inner else -1
    for r in range(rows + 1):
        v = r / rows
        z = top + (bottom - top) * v
        # quanto è "raccolta" la tenda a questa altezza (1 = aperta, piccola = stretta dal fermatenda)
        d = (z - tie_z) / (top - tie_z) if z > tie_z else (tie_z - z) / (tie_z - bottom)
        gather = 0.32 + 0.68 * (min(1.0, abs(d)) ** 0.8) if z > tie_z else 0.32 + 0.5 * (min(1.0, abs(d)) ** 0.7)
        for c in range(cols + 1):
            u = c / cols
            x = x_outer - sgn * width * u * gather
            amp = 0.045 * (0.5 + 0.5 * gather) + 0.01
            y = depth_y - math.sin(u * math.pi * 2 * folds) * amp * 0.5 - amp * 0.5
            if v > 0.96:  # piccolo risvolto in fondo
                y -= 0.01
            verts.append((x, y, z))
    for r in range(rows):
        for c in range(cols):
            a = r * (cols + 1) + c
            faces.append((a, a + 1, a + cols + 2, a + cols + 1))
    obj = _new_obj(name, verts, faces, [mat], tint=0.02)
    s = obj.modifiers.new("Spessore", "SOLIDIFY")
    s.thickness = 0.02
    s.offset = 0
    return obj


def window():
    random.seed(21)
    w, h = WINDOW_W, WINDOW_H
    t = 0.12
    # telaio esterno (profondità 0.34) e cornice interna verso la stanza (-Y)
    k.box("TelaioSu", (w + t, 0.34, t), (0, 0, h - t / 2), mat="WoodLight", bevel=0.025)
    k.box("TelaioGiu", (w + t, 0.34, t), (0, 0, t / 2), mat="WoodLight", bevel=0.025)
    k.box("TelaioSx", (t, 0.34, h), (-w / 2, 0, h / 2), mat="WoodLight", bevel=0.025)
    k.box("TelaioDx", (t, 0.34, h), (w / 2, 0, h / 2), mat="WoodLight", bevel=0.025)
    # cornice modanata interna
    m = 0.06
    k.box("CornSu", (w + t + 0.14, 0.05, m), (0, -0.18, h + 0.01), mat="WoodMid", bevel=0.015)
    k.box("CornSx", (m, 0.05, h + 0.06), (-(w + t) / 2 - 0.04, -0.18, h / 2), mat="WoodMid", bevel=0.015)
    k.box("CornDx", (m, 0.05, h + 0.06), ((w + t) / 2 + 0.04, -0.18, h / 2), mat="WoodMid", bevel=0.015)
    k.box("Capitello", (w + t + 0.3, 0.12, 0.09), (0, -0.16, h + 0.08), mat="WoodLight", bevel=0.02)
    # ante: telaietti con croce e piccoli listelli
    k.box("Montante", (0.075, 0.12, h - t), (0, 0.02, h / 2), mat="WoodMid", bevel=0.018)
    k.box("Traverso", (w - t, 0.12, 0.075), (0, 0.02, h * 0.55), mat="WoodMid", bevel=0.018)
    # listelli sottili: ogni anta ha 2 x 3 riquadri
    for sx in (-1, 1):
        k.box("ListelloV", (0.035, 0.07, h - 2 * t), (sx * w / 4, 0.02, h / 2), mat="WoodMid", bevel=0.01)
    for zz in (0.3, 0.8):
        k.box("ListelloO", (w - t, 0.07, 0.035), (0, 0.02, h * zz), mat="WoodMid", bevel=0.01)
    # davanzale con bordo arrotondato e mensole
    k.box("Davanzale", (w + 0.5, 0.5, 0.1), (0, -0.12, -0.04), mat="WoodMid", bevel=0.035)
    k.box("DavanzaleBordo", (w + 0.56, 0.06, 0.07), (0, -0.37, -0.03), mat="WoodLight", bevel=0.025)
    for sx in (-1, 1):
        k.extrude_outline("Mensola", [(0, 0), (0.0, -0.22), (0.06, -0.22), (0.18, -0.08), (0.18, 0)], 0.07,
                          loc=(sx * (w / 2 - 0.1), -0.25, -0.09), rot=(0, 0, 90), mat="WoodDark", bevel=0.01)
    # neve morbida sul davanzale (più alta verso l'esterno)
    for i, (x, sx, sz) in enumerate([(-w * 0.28, 0.62, 0.09), (w * 0.05, 0.45, 0.07), (w * 0.3, 0.55, 0.085)]):
        lumpy_blob("NeveDav%d" % i, (x, -0.05, 0.01), (sx, 0.2, sz), sub=2, amount=0.22, seed=i, flat_bottom=0.0)
    lumpy_blob("NeveFuori", (0, 0.2, 0.0), (w * 0.5, 0.12, 0.07), sub=2, amount=0.2, seed=7, flat_bottom=0.0)
    # tende rosse raccolte con fermatende dorati
    for sx in (-1, 1):
        x_in = sx * (w / 2 - 0.22)
        x_out = sx * (w / 2 + 0.3)
        _curtain("Tenda", x_in, x_out, h + 0.1, 0.12, h * 0.42, -0.28)
        cx = sx * (w / 2 + 0.11)
        tb = k.torus("Fermatenda", 0.11, 0.022, (cx, -0.28, h * 0.42), (90, 0, 0), mat="Gold", major_seg=24, minor_seg=8)
        tb.scale = (1.0, 1.0, 0.5)
        k.sphere("Nappina", 0.03, (cx - sx * 0.1, -0.33, h * 0.42 - 0.02), mat="Gold", segments=10, rings=6)
        k.lathe("Fiocco", [(0, 0), (0.025, 0.0), (0.03, -0.06), (0.04, -0.13), (0, -0.14)], loc=(cx - sx * 0.1, -0.33, h * 0.42 - 0.04), mat="Gold", segments=10)
    # bastone delle tende con pomoli
    k.cylinder("Bastone", 0.025, w + 0.9, (0, -0.28, h + 0.12), rot=(0, 90, 0), mat="WoodDeep", bevel=0.005, verts=12)
    for sx in (-1, 1):
        k.sphere("Pomolo", 0.055, (sx * (w + 0.9) / 2, -0.28, h + 0.12), mat="Gold", segments=14, rings=8)
        k.cylinder("Supporto", 0.015, 0.14, (sx * (w / 2 + 0.25), -0.21, h + 0.12), rot=(90, 0, 0), mat="Iron", bevel=0.003, verts=8)
    # festone di pino con foglioline, palline e fiocchi
    p0, p1, p2, p3 = Vector((-w / 2 - 0.15, -0.24, h + 0.06)), Vector((-w / 4, -0.3, h - 0.38)), Vector((w / 4, -0.3, h - 0.38)), Vector((w / 2 + 0.15, -0.24, h + 0.06))
    pts = []
    for i in range(25):
        u = i / 24
        pts.append(p0 * (1 - u) ** 3 + p1 * 3 * u * (1 - u) ** 2 + p2 * 3 * u * u * (1 - u) + p3 * u ** 3)
    k.tube("FestoneAnima", pts, 0.05, mat="GreenDeep", sides=8)
    leaf = tmpl_leaf()
    ba = Batch("FestoneFoglie", "GreenPine")
    bb = Batch("FestoneFoglie2", "GreenDeep")
    random.seed(5)
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        tan = (b - a).normalized()
        for jj in range(4):
            p = a.lerp(b, random.random())
            ang = random.uniform(0, 2 * math.pi)
            side = tan.cross(Vector((0, 0, 1))).normalized()
            up = tan.cross(side).normalized()
            off = (side * math.cos(ang) + up * math.sin(ang))
            d = (tan * random.uniform(-0.6, 0.6) + off).normalized()
            m = look_matrix(p + off * 0.055, d, scale=(0.06, 0.022, 0.03))
            (ba if jj % 2 else bb).add(leaf, m)
    ba.build()
    bb.build()
    for i, idx in enumerate(range(2, len(pts) - 1, 3)):
        p = pts[idx]
        k.sphere("PallinaF", 0.04, (p.x, p.y - 0.06, p.z - 0.05), mat="RedSanta" if i % 2 == 0 else "Gold", segments=12, rings=8)
    for sx in (-1, 1):
        p = p0 if sx < 0 else p3
        for side in (-1, 1):
            loop = k.torus("FioccoAnello", 0.06, 0.02, (p.x + side * 0.06, p.y - 0.06, p.z - 0.02), (90, side * 30, 0), mat="RedSanta", major_seg=16, minor_seg=6)
            loop.scale = (1.0, 0.6, 1.0)
        k.sphere("FioccoNodo", 0.03, (p.x, p.y - 0.07, p.z - 0.02), mat="RedDeep", segments=10, rings=6)
        for side in (-1, 1):
            k.box("FioccoCoda", (0.035, 0.012, 0.16), (p.x + side * 0.03, p.y - 0.07, p.z - 0.11), (0, side * 18, 0), mat="RedSanta", bevel=0.004, segments=1)
    return {"ao_distance": 0.3, "ao_samples": 32, "ground": None}


# ============================================================================ BACHECA
def cork_board():
    random.seed(31)
    W, H = 2.0, 1.3
    # cornice a listelli con angoli sovrapposti
    k.box("Fondo", (W - 0.1, 0.05, H - 0.1), (0, 0.02, 0), mat="WoodDark", bevel=0.01)
    for sz in (-1, 1):
        k.box("CorniceO", (W + 0.04, 0.11, 0.12), (0, -0.0, sz * (H / 2 - 0.05)), mat="WoodMid", bevel=0.025)
    for sx in (-1, 1):
        k.box("CorniceV", (0.12, 0.1, H - 0.1), (sx * (W / 2 - 0.05), 0.003, 0), mat="WoodMid", bevel=0.025)
        for sz in (-1, 1):
            k.cylinder("Chiodo", 0.016, 0.02, (sx * (W / 2 - 0.05), -0.06, sz * (H / 2 - 0.05)), rot=(90, 0, 0), mat="Iron", bevel=0.004, verts=10)
    cork = k.box("Sughero", (W - 0.2, 0.05, H - 0.2), (0, -0.02, 0), mat="Cork", bevel=0.008)
    k.wobble(cork, 0.004, 0.3)
    # letterine appuntate (spostate per non toccarsi)
    lx = [-0.62, -0.2, 0.24, 0.66, -0.42, 0.42, 0.02]
    lz = [0.18, 0.24, 0.17, 0.21, -0.27, -0.22, -0.28]
    la = [6, -5, 4, -8, -4, 7, 2]
    pin_mats = ["RedSanta", "ClothBlue", "GreenElf", "Gold"]
    for i in range(len(lx)):
        x, z, a = -lx[i], lz[i], la[i]
        group = []
        w_, h_ = (0.32, 0.42) if i != 6 else (0.36, 0.24)
        paper = k.box("Lettera%d" % i, (w_, 0.008, h_), (0, 0, 0), mat="Cream", bevel=0.003, segments=1)
        group.append(paper)
        if i != 6:
            group.append(k.box("Intestazione", (w_ * 0.5, 0.004, 0.035), (-w_ * 0.12, -0.007, h_ * 0.29), mat="RedSanta" if i % 2 == 0 else "GreenElf", bevel=0.001, segments=1))
            for r in range(4):
                ln = w_ * (0.62 if r < 3 else 0.4)
                group.append(k.box("Riga", (ln, 0.003, 0.009), (-(w_ * 0.62 - ln) / 2, -0.006, h_ * 0.12 - r * 0.06), mat="WoodDark", bevel=0.001, segments=1))
            group.append(k.box("Francobollo", (0.06, 0.004, 0.07), (w_ * 0.3, -0.007, h_ * 0.33), mat="ClothBlue", bevel=0.002, segments=1))
        else:
            # busta con sigillo di ceralacca
            group.append(k.extrude_outline("Lembo", [(-w_ / 2, h_ / 2), (0, 0.0), (w_ / 2, h_ / 2)][::-1], 0.004, loc=(0, -0.006, 0), mat="Plaster", bevel=0.001))
            group.append(k.cylinder("Sigillo", 0.03, 0.01, (0, -0.012, 0.0), rot=(90, 0, 0), mat="RedDeep", bevel=0.004, verts=14))
        pin = pin_mats[i % len(pin_mats)]
        group.append(k.sphere("Puntina", 0.022, (0, -0.03, h_ / 2 - 0.05), mat=pin, segments=12, rings=8))
        group.append(k.cylinder("PuntinaGambo", 0.006, 0.03, (0, -0.012, h_ / 2 - 0.05), rot=(90, 0, 0), mat="MetalLight", bevel=0.001, verts=6))
        m = trs((x, -0.055, z), (0, a, 0))
        for o in group:
            o.matrix_basis = m @ o.matrix_basis
    # stella decorativa e due rametti di agrifoglio in alto
    puffy_star("Stella", (0, -0.05, H / 2 + 0.12), 0.15, 0.07, 0.06, mat="Gold")
    leaf = tmpl_leaf()
    hb = Batch("Agrifoglio", "GreenDeep")
    for sx in (-1, 1):
        for j, ang in enumerate((-25, 20, 65)):
            d = Vector((sx * math.cos(math.radians(ang)), 0, math.sin(math.radians(ang))))
            hb.add(leaf, look_matrix(Vector((sx * 0.24, -0.08, H / 2 - 0.02)) + d * 0.07, d, up=(0, -1, 0), scale=(0.075, 0.035, 0.03)))
        for j in range(3):
            k.sphere("Bacca", 0.022, (sx * (0.2 + j * 0.03), -0.11, H / 2 - 0.05 + (j % 2) * 0.03), mat="RedSanta", segments=10, rings=6)
    hb.build()
    return {"ao_distance": 0.2, "ao_samples": 32, "ground": None}


# ============================================================================ CORONA
def wreath():
    random.seed(41)
    R = 0.6
    base = k.torus("Base", R, 0.12, (0, 0, 0), (90, 0, 0), mat="GreenDeep", major_seg=40, minor_seg=10)
    leaf = tmpl_leaf()
    batches = [Batch("FoglieA", "GreenPine"), Batch("FoglieB", "GreenDeep"), Batch("FoglieC", "GreenLight", tint=0.0)]
    count = 280
    for i in range(count):
        a = random.uniform(0, 2 * math.pi)
        b = random.uniform(-math.pi * 0.95, math.pi * 0.25)  # soprattutto sul davanti e sui lati
        rr = R + math.cos(b) * 0.12
        center = Vector((rr * math.cos(a), -math.sin(-b) * 0.12 * -1, rr * math.sin(a)))
        center = Vector((rr * math.cos(a), math.sin(b) * 0.12, rr * math.sin(a)))
        normal = Vector((math.cos(b) * math.cos(a), math.sin(b), math.cos(b) * math.sin(a)))
        tangent = Vector((-math.sin(a), 0, math.cos(a)))
        d = (tangent * random.choice((-1, 1)) * random.uniform(0.6, 1.0) + normal * random.uniform(0.2, 0.6)).normalized()
        size = random.uniform(0.75, 1.15)
        m = look_matrix(center + normal * 0.035, d, up=normal, scale=(0.075 * size, 0.045 * size, 0.05))
        batches[0 if i % 5 < 2 else (1 if i % 5 < 4 else 2)].add(leaf, m)
    for bt in batches:
        bt.build()
    # bacche a grappoli, pigne e palline dorate
    for g in range(9):
        a = g * 2 * math.pi / 9 + 0.35
        c = Vector(((R + 0.03) * math.cos(a), -0.11, (R + 0.03) * math.sin(a)))
        if g % 3 == 0:
            k.sphere("PallinaOro", 0.045, tuple(c + Vector((0, -0.02, 0))), mat="Gold", segments=14, rings=8)
        elif g % 3 == 1:
            for j in range(3):
                off = Vector((math.cos(j * 2.1) * 0.025, -0.01 * j, math.sin(j * 2.1) * 0.025))
                k.sphere("Bacca", 0.022, tuple(c + off), mat="RedSanta", segments=10, rings=6)
        else:
            pc = k.lathe("Pigna", [(0, -0.05), (0.022, -0.04), (0.032, -0.01), (0.03, 0.02), (0.018, 0.045), (0, 0.055)],
                         loc=tuple(c), rot=(0, math.degrees(a) + 90, 0), mat="Bark", segments=10)
            bumpy(pc, 0.008, 40.0)
    # fiocco rosso in basso con anelli e code
    bc = Vector((0, -0.14, -R))
    for side in (-1, 1):
        loop = k.torus("Anello", 0.14, 0.042, tuple(bc + Vector((side * 0.15, 0, 0.03))), (90, side * 28, 0), mat="RedSanta", major_seg=24, minor_seg=10)
        loop.scale = (1.0, 0.5, 1.0)
        k.subdivide(loop, 1)
    k.sphere("Nodo", 0.055, tuple(bc + Vector((0, -0.02, 0))), scale=(1.0, 0.8, 1.0), mat="RedDeep", segments=16, rings=10)
    for side in (-1, 1):
        tail = []
        for i in range(8):
            u = i / 7
            tail.append(bc + Vector((side * (0.03 + u * 0.09), 0.01, -0.03 - u * 0.24)) + Vector((side * math.sin(u * 3) * 0.02, 0, 0)))
        flat_tube("Coda", tail, 0.03, "RedSanta", scale=(1.0, 0.35, 1.0), sides=10, radius_end=0.034)
    return {"ao_distance": 0.18, "ao_samples": 32, "ground": None}


# ============================================================================ LAMPADA APPESA
def lamp_rose():
    k.lathe("Rosone", [(0, 0), (0.11, 0), (0.11, -0.02), (0.09, -0.05), (0.03, -0.06), (0, -0.065)], mat="WoodDark", segments=24)
    k.torus("Anellino", 0.1, 0.008, (0, 0, -0.02), mat="Gold", major_seg=24, minor_seg=6)
    return {"ao_distance": 0.1, "ground": None}


def lamp_shade():
    # paralume smaltato: esterno rosso, interno crema, bordo dorato; origine = punto di aggancio del filo
    k.lathe("CappuccioMetallo", [(0, 0.01), (0.03, 0.01), (0.045, -0.02), (0.05, -0.07), (0, -0.07)], mat="Gold", segments=20)
    k.torus("Ghiera", 0.05, 0.01, (0, 0, -0.06), mat="Gold", major_seg=20, minor_seg=6)
    outer = [(0.055, -0.05), (0.08, -0.09), (0.16, -0.17), (0.26, -0.25), (0.33, -0.31), (0.355, -0.34)]
    inner = [(0.33, -0.335), (0.3, -0.305), (0.235, -0.245), (0.145, -0.17), (0.07, -0.1), (0.045, -0.075)]
    k.lathe("Paralume", outer + inner, mat="RedSanta", segments=40)
    k.lathe("Interno", [(0.32, -0.33), (0.29, -0.3), (0.225, -0.24), (0.14, -0.165), (0.065, -0.1), (0.0, -0.085)], mat="Cream", segments=40)
    k.torus("BordoOro", 0.345, 0.014, (0, 0, -0.338), mat="Gold", major_seg=48, minor_seg=8)
    k.cylinder("Portalampada", 0.03, 0.08, (0, 0, -0.12), mat="MetalDark", bevel=0.005, verts=14)
    k.sphere("Lampadina", 0.085, (0, 0, -0.25), scale=(1, 1, 1.15), mat="LampGlow", segments=20, rings=12)
    return {"ao_distance": 0.15, "ground": None}


# ============================================================================ CASSA
CRATE_SIZE = (0.9, 0.9, 0.7)  # larghezza X, profondità Y, altezza Z (in Unity: 0.9 x 0.7 x 0.9)


def crate():
    random.seed(51)
    sx, sy, sz = CRATE_SIZE
    post = 0.085
    k.box("Interno", (sx - 0.08, sy - 0.08, sz - 0.08), (0, 0, sz / 2), mat="WoodDeep", bevel=0.01)
    for x in (-1, 1):
        for y in (-1, 1):
            p = k.box("Spigolo", (post, post, sz + 0.01), (x * (sx / 2 - post / 2), y * (sy / 2 - post / 2), sz / 2), mat="WoodDark", bevel=0.014)
            k.jitter(p, 0.5, 0.002)
    slats = 3
    gap = 0.03
    sh = (sz - 0.04 - gap * (slats - 1)) / slats
    nails = Batch("Chiodi", "Iron")
    nail = tmpl_sphere(8, 4)
    for face in range(4):
        ang = face * 90
        m = trs((0, 0, 0), (0, 0, ang))
        length = (sx if face % 2 == 0 else sy) - 2 * post + 0.01
        depth = (sy if face % 2 == 0 else sx) / 2
        for s in range(slats):
            z = 0.02 + sh / 2 + s * (sh + gap)
            b = k.box("Asse", (length, 0.03, sh), (0, -depth + 0.01, z), mat="WoodLight", bevel=0.012, segments=2)
            b.matrix_basis = m @ trs((random.uniform(-0.01, 0.01), 0, 0), (random.uniform(-1.2, 1.2), 0, 0)) @ b.matrix_basis
            for e in (-1, 1):
                for zz in (-0.25, 0.25):
                    nails.add(nail, m @ trs((e * (length / 2 - 0.03), -depth - 0.008, z + zz * sh), (0, 0, 0), (0.012, 0.006, 0.012)))
        # traversa diagonale su ogni lato
        dl = math.hypot(length, sz - 0.1)
        d = k.box("Diagonale", (dl - 0.04, 0.025, 0.07), (0, -depth - 0.012, sz / 2), (0, math.degrees(math.atan2(sz - 0.1, length)) * (1 if face % 2 else -1), 0), mat="WoodMid", bevel=0.01)
        d.matrix_basis = m @ d.matrix_basis
    # coperchio di assi con due traverse
    planks = 4
    pw = (sx - 0.02) / planks
    for i in range(planks):
        p = k.box("Coperchio", (pw - 0.012, sy - 0.02, 0.035), (-sx / 2 + 0.01 + pw * (i + 0.5), 0, sz + 0.012), mat="WoodMid", bevel=0.01, segments=2)
        k.jitter(p, 0.6, 0.003)
    for y in (-1, 1):
        k.box("TraversaCoperchio", (sx - 0.08, 0.07, 0.025), (0, y * (sy / 2 - 0.12), sz + 0.04), mat="WoodDark", bevel=0.008)
    nails.build()
    return {"ao_distance": 0.25, "ao_samples": 32}


# ============================================================================ BARILE
def _barrel_radius(z):
    # profilo bombato: 0.30 sotto e sopra, 0.36 a metà (altezza 0.8)
    u = (z - 0.4) / 0.4
    return 0.3 + 0.06 * (1 - u * u)


def barrel():
    random.seed(61)
    staves = 18
    rows = [i / 10 * 0.8 for i in range(11)]
    gap = 0.012
    for s in range(staves):
        a0 = 2 * math.pi * s / staves + gap / 2 / 0.33
        a1 = 2 * math.pi * (s + 1) / staves - gap / 2 / 0.33
        verts, faces = [], []
        cols = 3
        for zi, z in enumerate(rows):
            r = _barrel_radius(z)
            for c in range(cols + 1):
                a = a0 + (a1 - a0) * c / cols
                verts.append((r * math.cos(a), r * math.sin(a), z))
                verts.append(((r - 0.03) * math.cos(a), (r - 0.03) * math.sin(a), z))
        stride = (cols + 1) * 2
        for zi in range(len(rows) - 1):
            for c in range(cols):
                o = zi * stride + c * 2
                faces.append((o, o + 2, o + 2 + stride, o + stride))           # esterno
                faces.append((o + 1, o + 1 + stride, o + 3 + stride, o + 3))   # interno
        for zi in range(len(rows) - 1):
            o = zi * stride
            faces.append((o, o + stride, o + stride + 1, o + 1))
            o2 = zi * stride + cols * 2
            faces.append((o2, o2 + 1, o2 + 1 + stride, o2 + stride))
        last = (len(rows) - 1) * stride
        for c in range(cols):
            o = c * 2
            faces.append((o, o + 1, o + 3, o + 2))
            o = last + c * 2
            faces.append((o, o + 2, o + 3, o + 1))
        obj = _new_obj("Doga%d" % s, verts, faces, ["WoodMid" if s % 3 else "WoodLight"], tint=0.05)
        b = obj.modifiers.new("Smusso", "BEVEL")
        b.width = 0.006
        b.segments = 2
        b.limit_method = "ANGLE"
        b.angle_limit = math.radians(50)
    # cerchi di ferro con ribattini
    rivets = Batch("Ribattini", "MetalLight")
    rv = tmpl_sphere(8, 4)
    for zc in (0.1, 0.27, 0.53, 0.7):
        r = _barrel_radius(zc) + 0.004
        k.lathe("Cerchio", [(r - 0.01, zc - 0.028), (r + 0.006, zc - 0.028), (r + 0.01, zc - 0.02), (r + 0.01, zc + 0.02), (r + 0.006, zc + 0.028), (r - 0.01, zc + 0.028)],
                mat="Iron", segments=48)
        for j in range(12):
            a = j * 2 * math.pi / 12 + zc
            rivets.add(rv, trs(((r + 0.011) * math.cos(a), (r + 0.011) * math.sin(a), zc), (0, 0, 0), (0.008, 0.008, 0.008)))
    rivets.build()
    # coperchio un po' incassato con le scanalature tra le assi
    k.cylinder("Coperchio", 0.285, 0.03, (0, 0, 0.76), mat="WoodLight", bevel=0.006, verts=40)
    for x in (-0.14, 0.0, 0.14):
        k.box("Scanalatura", (0.008, 2 * math.sqrt(0.28 ** 2 - x * x) - 0.02, 0.006), (x, 0, 0.775), mat="WoodDeep", bevel=0.002, segments=1)
    k.cylinder("Fondo", 0.29, 0.03, (0, 0, 0.03), mat="WoodDark", bevel=0.005, verts=32)
    return {"ao_distance": 0.25, "ao_samples": 32}


def candy_cane():
    """Bastoncino alto 0.7 m (base in 0,0,0), il manico curva verso -Y (il "davanti" di Unity)."""
    height, hr = 0.7, 0.1
    pts = [Vector((0, 0, 0)), Vector((0, 0, height * 0.25)), Vector((0, 0, height * 0.5)), Vector((0, 0, height * 0.75)), Vector((0, 0, height - 0.1))]
    for i in range(1, 13):
        a = i / 12 * math.pi
        pts.append(Vector((0, -(hr - math.cos(a) * hr), height - 0.1 + math.sin(a) * hr)))
    pts.append(pts[-1] + Vector((0, 0, -0.03)))
    fine = []
    for a, b in zip(pts[:-1], pts[1:]):
        n = max(1, int((b - a).length / 0.008))
        for i in range(n):
            fine.append(a.lerp(b, i / n))
    fine.append(pts[-1])
    striped_tube("Bastoncino", fine, 0.028, ("RedSanta", "White"), sides=16, stripes=2, turns_per_meter=7.0)
    return {"ao_distance": 0.05, "ground": None}


# ============================================================================ PUPAZZO DI NEVE
def snowman():
    random.seed(71)
    lumpy_blob("Base", (0, 0, 0.42), (0.52, 0.5, 0.44), sub=4, amount=0.07, scale=1.6, seed=1, flat_bottom=-0.4)
    lumpy_blob("Pancia", (0, 0, 1.06), (0.39, 0.38, 0.36), sub=4, amount=0.06, scale=1.8, seed=2)
    lumpy_blob("Testa", (0, 0, 1.56), (0.28, 0.27, 0.27), sub=4, amount=0.05, scale=2.0, seed=3)
    lumpy_blob("NeveTerra", (0, 0, 0.0), (0.95, 0.9, 0.09), sub=3, amount=0.3, scale=2.4, seed=4, flat_bottom=0.0)
    # occhi, bocca e bottoni di carbone (sassolini irregolari)
    coal = []
    for sx in (-1, 1):
        coal.append(((sx * 0.09, -0.245, 1.63), 0.033))
    for i in range(5):
        a = (i - 2) * 0.3
        coal.append(((math.sin(a) * 0.15, -0.255 + abs(a) * 0.03, 1.49 - math.cos(a) * 0.0 + abs(a) * abs(a) * 0.12), 0.019))
    for i in range(3):
        zb = 1.18 - i * 0.16
        dz = (zb - 1.06) / 0.36
        coal.append(((0, -0.38 * math.sqrt(max(0.0, 1 - dz * dz)) - 0.008, zb), 0.034))
    for i, (p, r) in enumerate(coal):
        lumpy_blob("Carbone", p, (r, r * 0.8, r), mat="Black", sub=1, amount=0.25, seed=10 + i, tint=0.0)
    # naso a carota leggermente curvo con solchi
    pts = [Vector((0, -0.24, 1.56)), Vector((0.005, -0.33, 1.555)), Vector((0.012, -0.42, 1.545)), Vector((0.02, -0.5, 1.53))]
    nose = k.tube("Carota", pts, 0.042, mat="Carrot", sides=12, radius_end=0.008)
    bumpy(nose, 0.004, 30.0)
    # sciarpa: anello morbido attorno al collo + coda con frange e righe
    sc = k.torus("Sciarpa", 0.25, 0.065, (0, 0, 1.33), (6, 0, 0), mat="RedSanta", major_seg=32, minor_seg=12)
    sc.scale = (1.05, 1.05, 0.75)
    bumpy(sc, 0.012, 8.0)
    tail_pts = [Vector((0.12, -0.24, 1.3)), Vector((0.16, -0.31, 1.18)), Vector((0.18, -0.35, 1.05)), Vector((0.19, -0.37, 0.95))]
    tail = flat_tube("CodaSciarpa", tail_pts, 0.06, "RedSanta", scale=(1.0, 0.4, 1.0), sides=12, radius_end=0.065)
    bumpy(tail, 0.008, 9.0)
    for zz in (1.08, 1.0):
        st = k.torus("Riga", 0.064, 0.012, (0.18, -0.355, zz), (0, 0, 0), mat="Cream", major_seg=16, minor_seg=6)
        st.scale = (1.0, 0.45, 1.0)
    fr = Batch("Frange", "RedSanta")
    cy = tmpl_cyl(6)
    for i in range(6):
        fr.add(cy, trs((0.14 + i * 0.016, -0.375, 0.88), (0, random.uniform(-8, 8), 0), (0.006, 0.006, 0.07)))
    fr.build()
    # cilindro con fascia rossa e rametto di agrifoglio
    hat = [(0, 0), (0.29, 0), (0.31, 0.015), (0.3, 0.035), (0.19, 0.03), (0.18, 0.05), (0.175, 0.28), (0.165, 0.3), (0, 0.305)]
    k.lathe("Cappello", hat, rot=(0, -8, 0), loc=(0.02, 0, 1.78), mat="Black", segments=36)
    band = k.torus("Fascia", 0.181, 0.022, (0.02 - 0.09 * math.sin(math.radians(8)), 0, 1.78 + 0.09), (0, -8, 0), mat="RedSanta", major_seg=32, minor_seg=6)
    band.scale = (1, 1, 1.6)
    leaf = tmpl_leaf()
    hb = Batch("Agrifoglio", "GreenDeep")
    for ang in (-30, 30):
        d = Vector((math.cos(math.radians(ang + 90)), -0.4, math.sin(math.radians(ang + 90)) * 0.4)).normalized()
        hb.add(leaf, look_matrix(Vector((-0.08, -0.17, 1.89)) + d * 0.05, d, up=(0, -1, 0), scale=(0.06, 0.028, 0.025)))
    hb.build()
    for j in range(3):
        k.sphere("Bacca", 0.018, (-0.08 + (j - 1) * 0.025, -0.19, 1.89 + (j % 2) * 0.015), mat="RedSanta", segments=10, rings=6)
    # braccia di ramo con dita
    for sx in (-1, 1):
        arm = [Vector((sx * 0.3, 0, 1.12)), Vector((sx * 0.5, -0.02, 1.24)), Vector((sx * 0.68, -0.04, 1.38)), Vector((sx * 0.84, -0.05, 1.52))]
        a = k.tube("Braccio", arm, 0.024, mat="Bark", sides=8, radius_end=0.012)
        bumpy(a, 0.006, 12.0)
        for f, (dx, dz) in enumerate(((0.1, 0.09), (0.12, 0.0), (0.06, 0.12))):
            k.tube("Dito", [arm[-1], arm[-1] + Vector((sx * dx, -0.01, dz))], 0.011, mat="Bark", sides=6, radius_end=0.005)
        k.tube("Rametto", [arm[1], arm[1] + Vector((sx * 0.05, -0.02, 0.12))], 0.012, mat="Bark", sides=6, radius_end=0.005)
    return {"ao_distance": 0.4, "ao_samples": 32}


# ============================================================================ ABETE DI SFONDO
def pine():
    """Abete alto 5 m (in C# viene scalato all'altezza voluta). Pochi triangoli: ce ne sono 70."""
    random.seed(81)
    tr = k.cylinder("Tronco", 0.2, 1.0, (0, 0, 0.5), mat="Bark", bevel=0.02, verts=8, radius_top=0.14)
    z, R = 0.7, 1.3
    rows = [0.0, 0.25, 0.55, 0.8, 1.0, 1.08, 1.5]
    rows_s = [0.0, 0.35, 0.7, 1.0, 1.2]
    for i in range(5):
        H = 1.5
        lobes = 7 - (i // 2)
        f = _tier_profile(R, H, 0.12 * R, lobes, i * 1.3)
        t = radial("Rami%d" % i, rows, 21, f, "GreenPine" if i % 2 == 0 else "GreenDeep", tint=0.04)
        t.location.z = z
        bumpy(t, 0.05 * R, 1.5, seed=i)
        s = radial("Neve%d" % i, rows_s, 21, _tier_profile(R, H, 0.12 * R, lobes, i * 1.3, snow=True), "Snow", tint=0.0)
        s.location.z = z
        bumpy(s, 0.03 * R, 2.0, seed=i + 5)
        z += H * 0.6
        R *= 0.8
    return {"ao_distance": 0.6, "ao_samples": 24}


# ============================================================================ LAMPIONE
def lantern_post():
    random.seed(91)
    k.lathe("Base", [(0, 0), (0.14, 0), (0.14, 0.03), (0.11, 0.06), (0.1, 0.1), (0.07, 0.13), (0.06, 0.22), (0.05, 0.25), (0, 0.25)], mat="Iron", segments=24)
    k.lathe("Palo", [(0, 0.24), (0.042, 0.24), (0.036, 0.9), (0.05, 0.93), (0.05, 0.97), (0.034, 1.0), (0.032, 1.72), (0.05, 1.75), (0.06, 1.8), (0, 1.8)],
            mat="Iron", segments=16)
    # riccioli decorativi sotto la lanterna
    for sx in (-1, 1):
        pts = []
        for i in range(18):
            u = i / 17
            a = u * math.pi * 1.6
            r = 0.09 * (1 - u * 0.55)
            pts.append(Vector((sx * (0.035 + 0.09 - math.cos(a) * r), 0, 1.6 + math.sin(a) * r + u * 0.05)))
        k.tube("Ricciolo", pts, 0.011, mat="Iron", sides=8)
    # lanterna: base, montanti, vetro luminoso, tetto a piramide con pinnacolo
    k.box("PiattoLanterna", (0.3, 0.3, 0.05), (0, 0, 1.83), mat="Iron", bevel=0.012)
    for x in (-1, 1):
        for y in (-1, 1):
            k.box("Montante", (0.03, 0.03, 0.34), (x * 0.125, y * 0.125, 2.02), mat="Iron", bevel=0.006)
    for side in range(4):
        m = trs((0, 0, 0), (0, 0, side * 90))
        for zz in (1.88, 2.17):
            b = k.box("Traversino", (0.25, 0.02, 0.02), (0, -0.125, zz), mat="Iron", bevel=0.004)
            b.matrix_basis = m @ b.matrix_basis
    k.box("Vetro", (0.22, 0.22, 0.3), (0, 0, 2.02), mat="LampGlow", bevel=0.03)
    k.lathe("Tetto", [(0, 2.18), (0.23, 2.18), (0.25, 2.2), (0.2, 2.24), (0.06, 2.34), (0.03, 2.36), (0, 2.36)], rot=(0, 0, 45), mat="Iron", segments=4)
    k.sphere("Pinnacolo", 0.035, (0, 0, 2.39), mat="Iron", segments=12, rings=8)
    k.torus("Anello", 0.03, 0.007, (0, 0, 2.43), (90, 0, 0), mat="Iron", major_seg=12, minor_seg=4)
    lumpy_blob("NeveTetto", (0, 0, 2.26), (0.2, 0.2, 0.06), sub=3, amount=0.25, seed=3, flat_bottom=-0.02)
    return {"ao_distance": 0.2}


# ============================================================================ STACCIONATA
def fence_post():
    p = k.box("Palo", (0.14, 0.14, 0.86), (0, 0, 0.43), mat="WoodDark", bevel=0.025)
    k.jitter(p, 1.0, 0.0)
    k.lathe("Punta", [(0.0, 0.86), (0.09, 0.86), (0.085, 0.9), (0.04, 0.95), (0, 0.96)], rot=(0, 0, 45), mat="WoodDark", segments=4)
    lumpy_blob("Neve", (0, 0, 0.92), (0.11, 0.11, 0.06), sub=2, amount=0.25, seed=2, flat_bottom=-0.03)
    return {"ao_distance": 0.2}


def fence_span():
    """Tratto di 1 m lungo X (centrato in 0): due traverse con chiodi e neve sopra. In C# si allunga in X."""
    random.seed(97)
    for z, sd in ((0.7, 1), (0.38, 2)):
        b = k.box("Traversa", (1.04, 0.07, 0.09), (0, -0.07, z), mat="WoodLight", bevel=0.018)
        k.jitter(b, 0.8, 0.0)
        for x in (-0.49, 0.49):
            k.cylinder("Chiodo", 0.012, 0.01, (x, -0.11, z), rot=(90, 0, 0), mat="Iron", bevel=0.003, verts=8)
    # rotolo di neve morbido sulla traversa alta
    pts = [Vector((-0.5 + i / 12.0, -0.07, 0.75 + 0.008 * math.sin(i * 1.7))) for i in range(13)]
    sn = flat_tube("Neve", pts, 0.04, "Snow", scale=(1.0, 1.0, 0.75), sides=10)
    bumpy(sn, 0.01, 8.0, seed=3)
    return {"ao_distance": 0.15, "ground": None}


# ============================================================================ TAPPETO
RUG_W, RUG_D = 10.0, 6.5


def rug():
    random.seed(101)
    W, D = RUG_W, RUG_D
    # strati sovrapposti (dal basso): bordo crema, campo rosso, filetto crema, campo interno
    k.box("Bordo", (W, D, 0.024), (0, 0, 0.012), mat="RugCream", bevel=0.012, segments=2)
    k.box("Campo", (W - 0.36, D - 0.36, 0.028), (0, 0, 0.014), mat="Rug", bevel=0.01, segments=2)
    k.box("Filetto", (W - 0.9, D - 0.9, 0.032), (0, 0, 0.016), mat="RugCream", bevel=0.008, segments=2)
    k.box("CampoInterno", (W - 1.06, D - 1.06, 0.036), (0, 0, 0.018), mat="Rug", bevel=0.008, segments=2)
    # medaglione centrale a rombo e motivi agli angoli
    def diamond(name, cx, cy, sx, sy, z, mat):
        o = k.extrude_outline(name, [(sx, 0), (0, sy), (-sx, 0), (0, -sy)], 0.004, loc=(cx, cy, z), rot=(90, 0, 0), mat=mat, bevel=0.002)
        return o
    diamond("Medaglione", 0, 0, 1.6, 1.1, 0.038, "RugCream")
    diamond("Medaglione2", 0, 0, 1.4, 0.95, 0.041, "GreenDeep")
    diamond("Medaglione3", 0, 0, 0.7, 0.48, 0.044, "RugCream")
    diamond("Medaglione4", 0, 0, 0.45, 0.3, 0.047, "Rug")
    for sx in (-1, 1):
        for sy in (-1, 1):
            diamond("Angolo", sx * (W / 2 - 1.05), sy * (D / 2 - 1.05), 0.32, 0.32, 0.04, "GreenDeep")
            diamond("Angolo2", sx * (W / 2 - 1.05), sy * (D / 2 - 1.05), 0.14, 0.14, 0.043, "RugCream")
    # piccoli rombi lungo il filetto
    for i in range(14):
        x = -W / 2 + 0.95 + i * (W - 1.9) / 13
        for sy in (-1, 1):
            diamond("Motivo", x, sy * (D / 2 - 0.18), 0.07, 0.07, 0.03, "RugCream" if i % 2 else "GreenDeep")
    # frange sui lati corti
    fr = Batch("Frange", "RugCream", tint=0.0)
    bx = tmpl_box()
    n = int(D / 0.11)
    for sx in (-1, 1):
        for i in range(n):
            y = -D / 2 + 0.06 + i * (D - 0.12) / (n - 1)
            ln = 0.16 + random.uniform(-0.02, 0.02)
            fr.add(bx, trs((sx * (W / 2 + ln / 2 - 0.02), y, 0.006), (0, 0, random.uniform(-6, 6)), (ln, 0.035, 0.01)))
    fr.build()
    return {"ao_distance": 0.1, "ao_samples": 24}


# ============================================================================ FRECCIA DELL'OBIETTIVO
def objective_arrow():
    # freccia "gonfia" che punta in basso (punta a z = 0) e anello dorato
    outline = [(0, 0.0), (0.3, 0.42), (0.11, 0.42), (0.11, 1.12), (-0.11, 1.12), (-0.11, 0.42), (-0.3, 0.42)]
    a = k.extrude_outline("Freccia", outline[::-1], 0.14, mat="StarGlow", bevel=0.05)
    a.modifiers["Smusso"].segments = 4
    k.torus("Anello", 0.34, 0.025, (0, 0, 0.2), mat="Gold", major_seg=40, minor_seg=8)
    return {"ao": False}


# ============================================================================ MUCCHIO DI NEVE
def snow_drift():
    """Mucchio unitario (raggio 1): in C# viene scalato con i raggi voluti."""
    lumpy_blob("Neve", (0, 0, 0), (1.0, 1.0, 1.0), sub=4, amount=0.12, scale=1.4, seed=9)
    return {"ao": False}


MODELS = {
    "Prop_ChristmasTree": christmas_tree,
    "Prop_TreeStar": tree_star,
    "Prop_Bulb_BulbWarm": bulb_glass("BulbWarm"),
    "Prop_Bulb_BulbRed": bulb_glass("BulbRed"),
    "Prop_Bulb_BulbGreen": bulb_glass("BulbGreen"),
    "Prop_Bulb_BulbBlue": bulb_glass("BulbBlue"),
    "Prop_BulbCap": bulb_cap,
    "Prop_Window": window,
    "Prop_CorkBoard": cork_board,
    "Prop_Wreath": wreath,
    "Prop_LampRose": lamp_rose,
    "Prop_LampShade": lamp_shade,
    "Prop_Crate": crate,
    "Prop_Barrel": barrel,
    "Prop_CandyCane": candy_cane,
    "Prop_Snowman": snowman,
    "Prop_Pine": pine,
    "Prop_LanternPost": lantern_post,
    "Prop_FencePost": fence_post,
    "Prop_FenceSpan": fence_span,
    "Prop_Rug": rug,
    "Prop_ObjectiveArrow": objective_arrow,
    "Prop_SnowDrift": snow_drift,
}
