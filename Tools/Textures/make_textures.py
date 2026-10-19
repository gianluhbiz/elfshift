# -*- coding: utf-8 -*-
"""
Crea le texture "dipinte" del laboratorio e le salva in Assets/Textures/Source:
  - floor_planks.png : pavimento a listoni di legno (venature, nodi, toni diversi, fughe scure, bordi consumati)
  - wallpaper.png    : carta da parati color crema con righe tenui e rametti di agrifoglio
Le immagini si ripetono senza giunte (tileable). Serve Python 3 con numpy e Pillow:
    python Tools/Textures/make_textures.py
Il giocatore non deve eseguirlo: i PNG sono già nel progetto.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Textures", "Source"))
rng = np.random.default_rng(42)


def tile_noise(size, cells, octaves=4, seed=0, cells_y=None):
    """Rumore morbido che si ripete ai bordi (somma di ottave di rumore a valori su griglia periodica).
    Con cells_y diverso da cells il rumore è "stirato" (utile per le fibre del legno)."""
    r = np.random.default_rng(seed)
    out = np.zeros((size, size), np.float32)
    amp, total = 1.0, 0.0
    cy = cells if cells_y is None else cells_y
    for o in range(octaves):
        n = cells * (2 ** o)
        m = cy * (2 ** o)
        grid = r.random((m, n)).astype(np.float32)
        ys = np.linspace(0, m, size, endpoint=False)
        xs = np.linspace(0, n, size, endpoint=False)
        y0 = np.floor(ys).astype(int)
        x0 = np.floor(xs).astype(int)
        fy = ys - y0
        fx = xs - x0
        fy = fy * fy * (3 - 2 * fy)
        fx = fx * fx * (3 - 2 * fx)
        y1 = (y0 + 1) % m
        x1 = (x0 + 1) % n
        a = grid[np.ix_(y0, x0)]
        b = grid[np.ix_(y0, x1)]
        c = grid[np.ix_(y1, x0)]
        d = grid[np.ix_(y1, x1)]
        top = a + (b - a) * fx[None, :]
        bot = c + (d - c) * fx[None, :]
        out += amp * (top + (bot - top) * fy[:, None])
        total += amp
        amp *= 0.5
    return out / total


def hex_rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


# ============================================================================ pavimento
def floor_planks(size=1024, rows=8):
    """Listoni orizzontali: 'rows' file per immagine (l'immagine copre 2 m, quindi listoni da 25 cm)."""
    img = np.zeros((size, size, 3), np.float32)
    row_h = size // rows
    tones = [hex_rgb(c) for c in ("#C98A4E", "#BD7C43", "#D39758", "#B87645", "#C4834A", "#CE925A")]
    grain_noise = tile_noise(size, 3, 3, seed=1)
    fibers = tile_noise(size, 4, 4, seed=3, cells_y=90)   # fibre lunghe e sottili lungo i listoni
    fine = tile_noise(size, 48, 3, seed=2)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    shade = np.ones((size, size), np.float32)
    for r in range(rows):
        y0, y1 = r * row_h, (r + 1) * row_h
        # giunti sfalsati: ogni fila ha 1-2 tagli in posizioni diverse (con ripetizione orizzontale)
        cuts = [int(rng.integers(0, size // 2 - 150))] if rng.random() < 0.4 else [int(rng.integers(0, size // 2 - 150)), int(rng.integers(size // 2, size - 150))]
        offset = int(rng.integers(0, size))
        bounds = [(c + offset) % size for c in cuts]
        bounds = sorted(bounds)
        segments = []
        for i in range(len(bounds)):
            a = bounds[i]
            b = bounds[(i + 1) % len(bounds)]
            segments.append((a, b if b > a else b + size))
        for (a, b) in segments:
            tone = tones[int(rng.integers(0, len(tones)))] * float(rng.uniform(0.94, 1.05))
            phase = float(rng.uniform(0, 100))
            freq = float(rng.uniform(0.045, 0.07))
            for xr in range(a, b):
                x = xr % size
                col = slice(y0, y1)
                yl = yy[col, x] - y0
                # venature: linee lungo il listone, ondulate dal rumore
                g = np.sin((yl * 0.6 + grain_noise[col, x] * 90.0 + phase) * freq * 2 * math.pi)
                g = (0.5 + 0.5 * g) ** 3
                v = 0.9 + 0.07 * g + (fibers[col, x] - 0.5) * 0.28 + (fine[col, x] - 0.5) * 0.06
                img[col, x] = tone * v[:, None]
            # nodo del legno ogni tanto
            if rng.random() < 0.55:
                kx = int(rng.integers(a + 30, max(a + 31, b - 30))) % size
                ky = int(rng.integers(y0 + 25, y1 - 25))
                rad = float(rng.uniform(7, 13))
                for dy in range(-40, 41):
                    for dx in range(-60, 61):
                        X = (kx + dx) % size
                        Y = ky + dy
                        if Y < y0 + 3 or Y >= y1 - 3:
                            continue
                        d = math.sqrt((dx / 1.6) ** 2 + dy ** 2)
                        if d < rad:
                            k = 1 - d / rad
                            img[Y, X] *= 1 - 0.45 * k
                        elif d < rad * 3:
                            img[Y, X] *= 1 - 0.08 * math.sin((d - rad) * 0.9) ** 2
            # tagli di testa: fuga scura
            for x in (a % size, (a + 1) % size, (a + 2) % size):
                img[y0:y1, x] *= 0.55
        # fuga tra le file e bordi leggermente consumati (più scuri)
        for k in range(4):
            f = 0.55 + k * 0.12
            img[y0 + k, :] *= f
            img[y1 - 1 - k, :] *= min(1.0, f + 0.1)
    # sporco/usura generale a macchie larghe
    wear = tile_noise(size, 3, 3, seed=5)
    img *= (0.9 + 0.16 * wear)[:, :, None]
    img = np.clip(img, 0, 1)
    return Image.fromarray((img * 255).astype(np.uint8), "RGB")


# ============================================================================ carta da parati
def wallpaper(size=1024):
    """Crema con righe larghe appena accennate e piccoli rametti di agrifoglio in una griglia sfalsata."""
    S = size * 2  # disegno al doppio e poi riduco (bordi morbidi)
    base = hex_rgb("#F2E2C4")
    stripe = hex_rgb("#EAD5B1")
    img = np.ones((S, S, 3), np.float32) * base
    xx = np.arange(S)
    period = S // 4
    band = ((xx % period) < period // 2)
    img[:, band] = stripe
    # filetto sottile dorato ai bordi delle righe
    for edge in range(0, S, period // 2):
        for d in range(-2, 3):
            img[:, (edge + d) % S] = img[:, (edge + d) % S] * 0.92 + hex_rgb("#D9B47A") * 0.08
    paper = tile_noise(S, 32, 4, seed=9)
    img *= (0.95 + 0.07 * paper)[:, :, None]
    im = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8), "RGB")
    draw = ImageDraw.Draw(im)

    leaf = tuple(int(c * 255) for c in hex_rgb("#5F8F5A"))
    leaf_dark = tuple(int(c * 255) for c in hex_rgb("#4A7748"))
    berry = tuple(int(c * 255) for c in hex_rgb("#C2484A"))

    def holly(cx, cy, s, angle):
        # due foglie a punta + tre bacche
        for side in (-1, 1):
            a = angle + side * 0.55
            pts = []
            for t in np.linspace(0, 1, 9):
                w = math.sin(t * math.pi) * 0.32 * (1 + 0.18 * math.sin(t * math.pi * 4))
                along = t * 1.0
                px = cx + (math.cos(a) * along - math.sin(a) * w) * s
                py = cy + (math.sin(a) * along + math.cos(a) * w) * s
                pts.append((px, py))
            for t in np.linspace(1, 0, 9):
                w = -math.sin(t * math.pi) * 0.32 * (1 + 0.18 * math.sin(t * math.pi * 4))
                px = cx + (math.cos(a) * t - math.sin(a) * w) * s
                py = cy + (math.sin(a) * t + math.cos(a) * w) * s
                pts.append((px, py))
            draw.polygon(pts, fill=leaf if side < 0 else leaf_dark)
        for (bx, by) in ((-0.1, -0.08), (0.08, -0.12), (0.0, 0.06)):
            r = s * 0.11
            x = cx + bx * s
            y = cy + by * s
            draw.ellipse((x - r, y - r, x + r, y + r), fill=berry)

    step = S // 4
    for gy in range(4):
        for gx in range(4):
            cx = gx * step + step * 0.75 + (step / 2 if gy % 2 else 0)
            cy = gy * step + step * 0.5
            for ox in (-S, 0, S):
                for oy in (-S, 0, S):
                    holly(cx + ox, cy + oy, S * 0.045, -math.pi / 2 + (0.3 if (gx + gy) % 2 else -0.3))
    im = im.filter(ImageFilter.GaussianBlur(0.6)).resize((size, size), Image.LANCZOS)
    return im


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    floor_planks().save(os.path.join(OUT, "floor_planks.png"))
    wallpaper().save(os.path.join(OUT, "wallpaper.png"))
    print("texture salvate in", OUT)
