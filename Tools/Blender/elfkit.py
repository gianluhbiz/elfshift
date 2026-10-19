# -*- coding: utf-8 -*-
"""
elfkit: piccola libreria per modellare in Blender (via script) i modelli di Elfshift
e salvarli nel formato EMSH letto da Unity (Assets/Editor/Generation/ImportedMesh.cs).

Uso tipico (in uno script dentro Tools/Blender/models):

    import elfkit as k
    k.reset()
    top = k.box("Piano", size=(2.0, 0.9, 0.08), loc=(0, 0, 0.9), mat="WoodMid", bevel=0.015)
    k.wobble(top, 0.004)                       # piccole irregolarità "fatte a mano"
    k.export("Workbench_Top")                  # -> Assets/Models/Source/Workbench_Top.bytes

Convenzioni:
  - si modella in METRI, con Z in alto e il "davanti" del modello rivolto verso -Y (come la vista frontale di Blender);
  - il punto (0,0,0) è l'origine del modello in Unity (di solito a terra, al centro);
  - i materiali hanno i NOMI della tavolozza di Unity (Assets/Editor/Generation/Palette.cs);
  - all'esportazione si calcola l'ambient occlusion (ombra negli angoli) e la si salva nei colori dei vertici.

Si esegue con il modulo Python "bpy" (Blender come libreria):  pip install bpy  (Python 3.13).
"""
import math
import os
import random
import re
import struct

import bpy  # deve essere importato prima di bmesh
import bmesh
import gzip
from mathutils import Euler, Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
SOURCE_DIR = os.path.join(REPO, "Assets", "Models", "Source")
PALETTE_CS = os.path.join(REPO, "Assets", "Editor", "Generation", "Palette.cs")


# ============================================================================ tavolozza
def _load_palette():
    """Legge Palette.cs: { "Chiave", M("#RRGGBB", liscio, metallo, emissione) }."""
    text = open(PALETTE_CS, encoding="utf-8").read()
    pal = {}
    for m in re.finditer(r'\{\s*"(\w+)"\s*,\s*M\("#([0-9A-Fa-f]{6})"([^)]*)\)\s*\}', text):
        key, hexcol, rest = m.group(1), m.group(2), m.group(3)
        nums = [float(x.strip().rstrip("f")) for x in rest.split(",") if x.strip()]
        smooth = nums[0] if len(nums) > 0 else 0.28
        metal = nums[1] if len(nums) > 1 else 0.0
        emis = nums[2] if len(nums) > 2 else 0.0
        rgb = tuple(int(hexcol[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
        pal[key] = (rgb, smooth, metal, emis)
    return pal


PALETTE = _load_palette()


def _srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def material(key):
    """Materiale di anteprima con il nome della tavolozza (in Unity verrà sostituito da quello vero)."""
    mat = bpy.data.materials.get(key)
    if mat is not None:
        return mat
    if key not in PALETTE:
        raise KeyError("Materiale non presente in Palette.cs: " + key)
    rgb, smooth, metal, emis = PALETTE[key]
    mat = bpy.data.materials.new(key)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    lin = tuple(_srgb_to_linear(c) for c in rgb) + (1.0,)
    bsdf.inputs["Base Color"].default_value = lin
    bsdf.inputs["Roughness"].default_value = max(0.05, 1.0 - smooth)
    bsdf.inputs["Metallic"].default_value = metal
    if emis > 0:
        bsdf.inputs["Emission Color"].default_value = lin
        bsdf.inputs["Emission Strength"].default_value = emis * 2.0
    return mat


# ============================================================================ scena
def reset():
    """Scena vuota (da chiamare all'inizio di ogni modello)."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(1234)


def _link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _finish(obj, mat, smooth=True, rot=(0, 0, 0), loc=(0, 0, 0), tint=0.03):
    if mat:
        obj.data.materials.append(material(mat))
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    obj.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    obj.location = Vector(loc)
    # leggera variazione di tinta per oggetto: le assi o i pezzi uguali non sembrano fotocopie
    obj["tint"] = tuple(1.0 + random.uniform(-tint, tint) for _ in range(3)) if tint > 0 else (1.0, 1.0, 1.0)
    return obj


def mesh_object(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    return _link(bpy.data.objects.new(name, me))


# ============================================================================ forme di base
def box(name, size, loc=(0, 0, 0), rot=(0, 0, 0), mat="WoodMid", bevel=0.01, segments=3, tint=0.03):
    """Scatola con spigoli smussati (bevel). size = (larghezza X, profondità Y, altezza Z), loc = centro."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    obj = mesh_object(name, bm)
    if bevel > 0:
        b = obj.modifiers.new("Smusso", "BEVEL")
        b.width = min(bevel, min(size) * 0.49)
        b.segments = segments
        b.limit_method = "NONE"
        b.harden_normals = False
    return _finish(obj, mat, True, rot, loc, tint)


def cylinder(name, radius, depth, loc=(0, 0, 0), rot=(0, 0, 0), mat="WoodMid", bevel=0.008, verts=32, segments=2, tint=0.03, radius_top=None):
    """Cilindro (asse Z) centrato in loc. Con radius_top diverso diventa un tronco di cono."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=verts,
                          radius1=radius, radius2=radius if radius_top is None else radius_top, depth=depth)
    obj = mesh_object(name, bm)
    if bevel > 0:
        b = obj.modifiers.new("Smusso", "BEVEL")
        b.width = bevel
        b.segments = segments
        b.limit_method = "ANGLE"
        b.angle_limit = math.radians(40)
    return _finish(obj, mat, True, rot, loc, tint)


def sphere(name, radius, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), mat="RedSanta", segments=32, rings=16, tint=0.02):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segments, v_segments=rings, radius=radius)
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    obj = mesh_object(name, bm)
    return _finish(obj, mat, True, rot, loc, tint)


def torus(name, major, minor, loc=(0, 0, 0), rot=(0, 0, 0), mat="Gold", major_seg=48, minor_seg=16, tint=0.02):
    bm = bmesh.new()
    for i in range(major_seg):
        a = 2 * math.pi * i / major_seg
        for j in range(minor_seg):
            b = 2 * math.pi * j / minor_seg
            r = major + minor * math.cos(b)
            bm.verts.new((r * math.cos(a), r * math.sin(a), minor * math.sin(b)))
    bm.verts.ensure_lookup_table()
    for i in range(major_seg):
        for j in range(minor_seg):
            a = i * minor_seg + j
            b = ((i + 1) % major_seg) * minor_seg + j
            c = ((i + 1) % major_seg) * minor_seg + (j + 1) % minor_seg
            d = i * minor_seg + (j + 1) % minor_seg
            bm.faces.new((bm.verts[a], bm.verts[b], bm.verts[c], bm.verts[d]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = mesh_object(name, bm)
    return _finish(obj, mat, True, rot, loc, tint)


def lathe(name, profile, loc=(0, 0, 0), rot=(0, 0, 0), mat="WoodMid", segments=40, tint=0.02):
    """Solido di rotazione attorno all'asse Z. profile = [(raggio, altezza), ...] dal basso verso l'alto.
    Un raggio 0 al primo/ultimo punto chiude la forma."""
    bm = bmesh.new()
    rings = []
    for (r, z) in profile:
        ring = []
        if r <= 1e-6:
            ring = [bm.verts.new((0, 0, z))]
        else:
            for i in range(segments):
                a = 2 * math.pi * i / segments
                ring.append(bm.verts.new((r * math.cos(a), r * math.sin(a), z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        A, B = rings[k], rings[k + 1]
        for i in range(segments):
            i2 = (i + 1) % segments
            if len(A) == 1 and len(B) == 1:
                continue
            if len(A) == 1:
                bm.faces.new((A[0], B[i], B[i2]))
            elif len(B) == 1:
                bm.faces.new((A[i], A[i2], B[0]))
            else:
                bm.faces.new((A[i], A[i2], B[i2], B[i]))
    if len(rings[0]) > 1:
        bm.faces.new(list(reversed(rings[0])))
    if len(rings[-1]) > 1:
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = mesh_object(name, bm)
    return _finish(obj, mat, True, rot, loc, tint)


def tube(name, points, radius, mat="RedSanta", sides=16, radius_end=None, caps=True, tint=0.02, twist=0.0):
    """Tubo morbido che segue una lista di punti 3D (curva). Il raggio può restringersi verso la fine."""
    pts = [Vector(p) for p in points]
    n = len(pts)
    bm = bmesh.new()
    rings = []
    prev_side = None
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
        f = i / max(1, n - 1)
        r = radius if radius_end is None else radius + (radius_end - radius) * f
        ring = []
        for j in range(sides):
            a = 2 * math.pi * j / sides + twist * f
            ring.append(bm.verts.new(p + (side * math.cos(a) + other * math.sin(a)) * r))
        rings.append(ring)
    for i in range(n - 1):
        for j in range(sides):
            j2 = (j + 1) % sides
            bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
    if caps:
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = mesh_object(name, bm)
    return _finish(obj, mat, True, (0, 0, 0), (0, 0, 0), tint)


def extrude_outline(name, outline, depth, loc=(0, 0, 0), rot=(0, 0, 0), mat="WoodMid", bevel=0.006, tint=0.02):
    """Sagoma 2D (lista di punti X,Z nel piano frontale) estrusa lungo Y per 'depth'. Utile per fianchi sagomati,
    pattini della slitta, stelle... La sagoma va data in senso antiorario."""
    bm = bmesh.new()
    front = [bm.verts.new((x, -depth / 2, z)) for (x, z) in outline]
    back = [bm.verts.new((x, depth / 2, z)) for (x, z) in outline]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    m = len(outline)
    for i in range(m):
        i2 = (i + 1) % m
        bm.faces.new((front[i], back[i], back[i2], front[i2]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = mesh_object(name, bm)
    if bevel > 0:
        b = obj.modifiers.new("Smusso", "BEVEL")
        b.width = bevel
        b.segments = 2
        b.limit_method = "ANGLE"
        b.angle_limit = math.radians(50)
    return _finish(obj, mat, True, rot, loc, tint)


# ============================================================================ ritocchi "artigianali"
def subdivide(obj, levels=2):
    """Superficie di suddivisione: rende morbide e organiche le forme (come la plastilina)."""
    m = obj.modifiers.new("Morbido", "SUBSURF")
    m.levels = levels
    m.render_levels = levels
    return obj


def wobble(obj, amount=0.005, scale=0.35, seed=None, levels=2):
    """Piccole irregolarità a bassa frequenza: niente linee perfettamente dritte (aspetto fatto a mano).
    Aggiunge prima una suddivisione semplice così anche le facce grandi si deformano."""
    s = obj.modifiers.new("Suddivisione", "SUBSURF")
    s.subdivision_type = "SIMPLE"
    s.levels = levels
    s.render_levels = levels
    tex = bpy.data.textures.new(obj.name + "_rumore", "CLOUDS")
    tex.noise_scale = scale
    tex.noise_depth = 1
    d = obj.modifiers.new("Irregolare", "DISPLACE")
    d.texture = tex
    d.strength = amount
    d.mid_level = 0.5
    d.texture_coords = "GLOBAL" if seed is None else "OBJECT"
    if seed is not None:
        empty = bpy.data.objects.new(obj.name + "_seme", None)
        _link(empty)
        empty.location = (seed * 3.17, seed * 1.91, seed * 2.53)
        d.texture_coords_object = empty
    return obj


def jitter(obj, rot_deg=1.5, offset=0.004):
    """Rotazione e spostamento casuali piccolissimi (per assi, libri, barattoli messi "a mano")."""
    obj.rotation_euler.x += math.radians(random.uniform(-rot_deg, rot_deg)) * 0.3
    obj.rotation_euler.y += math.radians(random.uniform(-rot_deg, rot_deg)) * 0.3
    obj.rotation_euler.z += math.radians(random.uniform(-rot_deg, rot_deg))
    obj.location.x += random.uniform(-offset, offset)
    obj.location.y += random.uniform(-offset, offset)
    return obj


def mirror_x(obj):
    m = obj.modifiers.new("Specchio", "MIRROR")
    m.use_axis[0] = True
    return obj


def duplicate(obj, loc=None, rot=None, name=None):
    new = obj.copy()
    new.data = obj.data.copy()
    if name:
        new.name = name
    _link(new)
    if loc is not None:
        new.location = Vector(loc)
    if rot is not None:
        new.rotation_euler = Euler(tuple(math.radians(a) for a in rot))
    new["tint"] = tuple(1.0 + random.uniform(-0.03, 0.03) for _ in range(3))
    return new


# ============================================================================ esportazione
def _evaluated_copy(obj, depsgraph):
    ev = obj.evaluated_get(depsgraph)
    me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=depsgraph)
    me.transform(obj.matrix_world)
    if obj.matrix_world.determinant() < 0:
        me.flip_normals()
    return me


def _join_meshes(objs):
    """Unisce tutti gli oggetti (con i modificatori applicati) in un'unica mesh temporanea triangolata.
    La tinta di ogni oggetto viene scritta nell'attributo colore "tint" (per angolo)."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    parts = []
    for obj in objs:
        if obj.type != "MESH":
            continue
        me = _evaluated_copy(obj, depsgraph)
        if len(me.polygons) == 0:
            bpy.data.meshes.remove(me)
            continue
        if len(me.materials) == 0:
            me.materials.append(material("WoodMid"))
        tint = obj.get("tint", (1.0, 1.0, 1.0))
        attr = me.color_attributes.new("tint", "FLOAT_COLOR", "CORNER")
        for d in attr.data:
            d.color = (tint[0], tint[1], tint[2], 1.0)
        parts.append(_link(bpy.data.objects.new("PEZZO_" + obj.name, me)))
    if not parts:
        raise RuntimeError("Niente da esportare")
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    bm = bmesh.new()
    bm.from_mesh(joined.data)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.to_mesh(joined.data)
    bm.free()
    joined.data.update()
    return joined


def _bake_ao(obj, distance, samples, ground):
    """Calcola l'ambient occlusion con Cycles e lo scrive in un attributo colore per angolo."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Mondo")
    scene.world.light_settings.distance = distance
    attr = obj.data.color_attributes.new("AO", "BYTE_COLOR", "CORNER")
    obj.data.color_attributes.active_color = attr
    floor = None
    if ground is not None:
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=50)
        for v in bm.verts:
            v.co.z = ground
        floor = mesh_object("PavimentoTemporaneo", bm)
    for o in bpy.context.scene.objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type="AO", target="VERTEX_COLORS")
    if floor is not None:
        bpy.data.objects.remove(floor)
    return attr


def export(model_name, objs=None, ao=True, ao_distance=0.35, ao_samples=48, ground=0.0, sharp_angle=40.0, keep=False, **_ignored):
    """Esporta gli oggetti (tutti quelli della scena se objs è None) nel file Assets/Models/Source/<nome>.bytes.
    ground = altezza di un pavimento temporaneo usato solo per l'ombra di contatto (None = nessun pavimento)."""
    if objs is None:
        objs = [o for o in bpy.context.scene.objects if o.type == "MESH" and not o.hide_render]
    obj = _join_meshes(objs)
    obj.name = "EXPORT_" + model_name
    me = obj.data
    mat_names = [m.name for m in me.materials]
    me.set_sharp_from_angle(angle=math.radians(sharp_angle))

    hidden = []
    for o in objs:
        if not o.hide_render:
            o.hide_render = True
            hidden.append(o)
    if ao:
        _bake_ao(obj, ao_distance, ao_samples, ground)
    for o in hidden:
        o.hide_render = False

    me = obj.data
    me.calc_loop_triangles()
    normals = me.corner_normals
    uv = None
    ao_attr = me.color_attributes.get("AO")
    tint_attr = me.attributes.get("tint")

    verts = []
    index_of = {}
    groups = {i: [] for i in range(len(mat_names))}
    for tri in me.loop_triangles:
        ids = []
        for li in tri.loops:
            loop = me.loops[li]
            co = me.vertices[loop.vertex_index].co
            n = normals[li].vector
            a = ao_attr.data[li].color[0] if ao_attr is not None else 1.0
            t = tint_attr.data[li].color if tint_attr is not None else (1, 1, 1, 1)
            # Blender (Z su, davanti -Y) -> Unity (Y su, davanti +Z, sinistrorso): x' = -x, y' = z, z' = -y
            p = (-co.x, co.z, -co.y)
            nn = (-n.x, n.z, -n.y)
            # coordinate UV "a scatola": proiezione sull'asse dominante della normale (metri)
            ax = max(range(3), key=lambda k: abs(nn[k]))
            uvv = (p[2], p[1]) if ax == 0 else ((p[0], p[2]) if ax == 1 else (p[0], p[1]))
            col = (max(0, min(255, int(round(t[0] * 255)))), max(0, min(255, int(round(t[1] * 255)))),
                   max(0, min(255, int(round(t[2] * 255)))), max(0, min(255, int(round(a * 255)))))
            key = (loop.vertex_index, round(nn[0], 3), round(nn[1], 3), round(nn[2], 3), col)
            idx = index_of.get(key)
            if idx is None:
                idx = len(verts)
                index_of[key] = idx
                verts.append((p, nn, uvv, col))
            ids.append(idx)
        # La conversione di assi è uno specchio: l'ordine dei vertici di Blender (antiorario) diventa
        # orario in Unity, che è proprio il verso delle facce visibili. Quindi nessuna inversione.
        groups[tri.material_index].extend(ids)

    os.makedirs(SOURCE_DIR, exist_ok=True)
    path = os.path.join(SOURCE_DIR, model_name + ".bytes")
    body = bytearray()
    body += struct.pack("<i", len(verts))
    body += b"".join(struct.pack("<3f", *v[0]) for v in verts)
    body += b"".join(struct.pack("<3f", *v[1]) for v in verts)
    body += b"".join(struct.pack("<2f", *v[2]) for v in verts)
    body += b"".join(struct.pack("<4B", *v[3]) for v in verts)
    used = [(i, n) for i, n in enumerate(mat_names) if groups[i]]
    body += struct.pack("<i", len(used))
    for i, name in used:
        b = name.encode("utf-8")
        body += struct.pack("<i", len(b)) + b
        body += struct.pack("<i", len(groups[i]))
        body += struct.pack("<%di" % len(groups[i]), *groups[i])
    # versione 2: tutto ciò che segue l'intestazione è compresso (file circa 2-3 volte più piccoli)
    with open(path, "wb") as f:
        f.write(b"EMSH")
        f.write(struct.pack("<i", 2))
        f.write(gzip.compress(bytes(body), 9, mtime=0))
    tris = sum(len(g) for g in groups.values()) // 3
    print("[elfkit] %s: %d vertici, %d triangoli, materiali %s -> %s" % (model_name, len(verts), tris, [n for _, n in used], path))
    if not keep:
        bpy.data.objects.remove(obj)
    return path


# ============================================================================ anteprima (controllo visivo)
def load_emsh(path, name=None, loc=(0, 0, 0), rot_z=0.0):
    """Rilegge un file EMSH in Blender (riconvertendo gli assi): serve a controllare l'esportazione."""
    data = open(path, "rb").read()
    assert data[:4] == b"EMSH"
    (version,) = struct.unpack_from("<i", data, 4)
    if version == 2:
        data = data[:8] + gzip.decompress(data[8:])
    _, n = struct.unpack_from("<ii", data, 4)
    o = 12
    P = [struct.unpack_from("<3f", data, o + 12 * i) for i in range(n)]
    o += 12 * n
    N = [struct.unpack_from("<3f", data, o + 12 * i) for i in range(n)]
    o += 12 * n
    o += 8 * n
    C = [struct.unpack_from("<4B", data, o + 4 * i) for i in range(n)]
    o += 4 * n
    (g,) = struct.unpack_from("<i", data, o)
    o += 4
    faces, fmat, names = [], [], []
    for gi in range(g):
        (ln,) = struct.unpack_from("<i", data, o)
        o += 4
        names.append(data[o:o + ln].decode("utf-8"))
        o += ln
        (cnt,) = struct.unpack_from("<i", data, o)
        o += 4
        idx = struct.unpack_from("<%di" % cnt, data, o)
        o += 4 * cnt
        for t in range(0, cnt, 3):
            faces.append((idx[t], idx[t + 1], idx[t + 2]))
            fmat.append(gi)
    me = bpy.data.meshes.new(name or os.path.basename(path))
    me.from_pydata([(-p[0], -p[2], p[1]) for p in P], [], faces)
    me.update()
    for nm in names:
        me.materials.append(_preview_material(nm))
    for i, poly in enumerate(me.polygons):
        poly.material_index = fmat[i]
        poly.use_smooth = True
    me.normals_split_custom_set_from_vertices([(-q[0], -q[2], q[1]) for q in N])
    col = me.color_attributes.new("Col", "BYTE_COLOR", "POINT")
    for i, c in enumerate(C):
        col.data[i].color = (c[0] / 255.0, c[1] / 255.0, c[2] / 255.0, c[3] / 255.0)
    obj = _link(bpy.data.objects.new(me.name, me))
    obj.location = loc
    obj.rotation_euler.z = math.radians(rot_z)
    return obj


def _preview_material(key):
    """Materiale di anteprima che imita il shader di Unity: colore x tinta x (AO attenuato)."""
    name = "PV_" + key
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    base = material(key)
    mat = base.copy()
    mat.name = name
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    rgb = nt.nodes.new("ShaderNodeRGB")
    rgb.outputs[0].default_value = bsdf.inputs["Base Color"].default_value
    mul = nt.nodes.new("ShaderNodeMix")
    mul.data_type = "RGBA"
    mul.blend_type = "MULTIPLY"
    mul.inputs["Factor"].default_value = 1.0
    nt.links.new(rgb.outputs[0], mul.inputs[6])
    nt.links.new(attr.outputs["Color"], mul.inputs[7])
    ao = nt.nodes.new("ShaderNodeMath")
    ao.operation = "MULTIPLY_ADD"
    ao.inputs[1].default_value = 0.55
    ao.inputs[2].default_value = 0.45
    nt.links.new(attr.outputs["Alpha"], ao.inputs[0])
    mul2 = nt.nodes.new("ShaderNodeMix")
    mul2.data_type = "RGBA"
    mul2.blend_type = "MULTIPLY"
    mul2.inputs["Factor"].default_value = 1.0
    nt.links.new(mul.outputs[2], mul2.inputs[6])
    nt.links.new(ao.outputs[0], mul2.inputs[7])
    nt.links.new(mul2.outputs[2], bsdf.inputs["Base Color"])
    return mat


def render_preview(path, objs=None, yaw=35.0, pitch=22.0, size=(900, 700), samples=40, distance_scale=1.0, background=(0.16, 0.2, 0.32), ground=True):
    """Foto di controllo con Cycles: luce calda principale, luce fredda di riempimento, pavimento neutro."""
    scene = bpy.context.scene
    if objs is None:
        objs = [o for o in scene.objects if o.type == "MESH"]
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector(map(min, mn, w))
            mx = Vector(map(max, mx, w))
    center = (mn + mx) / 2
    radius = (mx - mn).length / 2
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 50
    cam = _link(bpy.data.objects.new("Cam", cam_data))
    yaw_r, pit_r = math.radians(yaw), math.radians(pitch)
    d = radius * 2.9 * distance_scale
    # la camera guarda il davanti (-Y), ruotata di 'yaw' attorno a Z
    cam.location = center + Vector((math.sin(yaw_r) * math.cos(pit_r), -math.cos(yaw_r) * math.cos(pit_r), math.sin(pit_r))) * d
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    sun = bpy.data.lights.new("Sole", "SUN")
    sun.energy = 3.2
    sun.color = (1.0, 0.9, 0.78)
    sun.angle = math.radians(8)
    so = _link(bpy.data.objects.new("Sole", sun))
    so.rotation_euler = (math.radians(50), 0, math.radians(yaw - 40))
    fill = bpy.data.lights.new("Riempimento", "SUN")
    fill.energy = 0.8
    fill.color = (0.7, 0.8, 1.0)
    fo = _link(bpy.data.objects.new("Riempimento", fill))
    fo.rotation_euler = (math.radians(70), 0, math.radians(yaw + 140))
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Mondo")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (background[0], background[1], background[2], 1)
    bg.inputs[1].default_value = 1.0
    if ground:
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=radius * 8)
        for v in bm.verts:
            v.co.z = mn.z
        g = mesh_object("Pavimento", bm)
        g.data.materials.append(material("WoodMid"))
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 4
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.resolution_x, scene.render.resolution_y = size
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[elfkit] anteprima ->", path)
