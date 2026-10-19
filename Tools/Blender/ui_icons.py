# -*- coding: utf-8 -*-
"""
Disegna in 3D (con Cycles) le icone dell'interfaccia e le salva come PNG trasparenti in Assets/UI/Icons.
    python Tools/Blender/ui_icons.py
Come per i modelli, il giocatore non deve eseguirlo: le immagini sono già nel progetto.
"""
import math
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import elfkit as k  # noqa: E402
import bpy  # noqa: E402
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402

OUT = os.path.join(k.REPO, "Assets", "UI", "Icons")


def setup_render(path, size=256, ortho=2.4, elev=20.0):
    sc = bpy.context.scene
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = ortho
    cam = k._link(bpy.data.objects.new("Cam", cam_data))
    cam.location = (0, -6 * math.cos(math.radians(elev)), 6 * math.sin(math.radians(elev)))
    cam.rotation_euler = (Vector((0, 0, 0)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    key = bpy.data.lights.new("Chiave", "AREA")
    key.energy = 420
    key.size = 3
    key.color = (1.0, 0.93, 0.82)
    ko = k._link(bpy.data.objects.new("Chiave", key))
    ko.location = (-2.5, -3.5, 4)
    ko.rotation_euler = (Vector((0, 0, 0)) - ko.location).to_track_quat("-Z", "Y").to_euler()
    rim = bpy.data.lights.new("Bordo", "AREA")
    rim.energy = 260
    rim.size = 2
    rim.color = (0.8, 0.88, 1.0)
    ro = k._link(bpy.data.objects.new("Bordo", rim))
    ro.location = (3, 2.5, 3)
    ro.rotation_euler = (Vector((0, 0, 0)) - ro.location).to_track_quat("-Z", "Y").to_euler()
    sc.world = bpy.data.worlds.new("Mondo")
    sc.world.use_nodes = True
    sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.5, 0.48, 1)
    sc.world.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 96
    sc.cycles.use_denoising = True
    sc.render.film_transparent = True
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[ui_icons] ->", path)


def star_points(n, r_out, r_in):
    pts = []
    for i in range(n * 2):
        a = math.pi / 2 + i * math.pi / n
        r = r_out if i % 2 == 0 else r_in
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return pts


def icon_star():
    k.reset()
    outline = star_points(5, 1.0, 0.5)
    # stella "gonfia": sagoma estrusa, smussata e ammorbidita
    s = k.extrude_outline("Stella", outline, 0.36, mat="Gold", bevel=0.12)
    s.modifiers["Smusso"].segments = 4
    k.subdivide(s, 2)
    s.rotation_euler = (math.radians(-12), 0, math.radians(-8))
    setup_render(os.path.join(OUT, "icon_star.png"))


def icon_cookie():
    k.reset()
    random.seed(7)
    body = k.cylinder("Biscotto", 0.95, 0.26, mat="CookieBrown", bevel=0.1, verts=48, segments=4)
    k.wobble(body, 0.06, scale=0.5, seed=3)
    # gocce di cioccolato distribuite a spirale (angolo aureo): sparse ma uniformi
    for i in range(11):
        a = i * 2.39996 + random.uniform(-0.2, 0.2)
        r = 0.78 * math.sqrt((i + 0.5) / 11.0)
        chip = k.sphere("Goccia", random.uniform(0.08, 0.12), loc=(math.cos(a) * r, math.sin(a) * r, 0.12),
                        scale=(1, 1, 0.7), mat="WoodDeep", segments=12, rings=8)
        k.wobble(chip, 0.02, scale=0.2, seed=i)
    setup_render(os.path.join(OUT, "icon_cookie.png"), elev=55.0)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    icon_star()
    icon_cookie()
