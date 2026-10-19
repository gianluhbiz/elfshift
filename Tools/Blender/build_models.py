# -*- coding: utf-8 -*-
"""
Costruisce i modelli di Elfshift con Blender e li salva in Assets/Models/Source/*.bytes.

    python Tools/Blender/build_models.py                 # tutti i modelli
    python Tools/Blender/build_models.py Workbench       # solo quelli il cui nome contiene "Workbench"
    python Tools/Blender/build_models.py Sleigh --preview CARTELLA   # + foto di controllo (PNG)

Serve Python 3.13 con il modulo "bpy" (pip install bpy). Il giocatore NON deve eseguirlo:
i file .bytes sono già nel progetto. Serve solo a chi vuole modificare i modelli.

Ogni file in Tools/Blender/models/*.py definisce un dizionario MODELS = {"Nome": funzione, ...}.
La funzione crea gli oggetti nella scena (vuota) e può restituire un dizionario di opzioni per elfkit.export
(es. {"ao_distance": 0.5, "ground": None}).
"""
import importlib
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "models"))

import elfkit as k  # noqa: E402


def all_models():
    found = {}
    for fname in sorted(os.listdir(os.path.join(HERE, "models"))):
        if not fname.endswith(".py") or fname.startswith("_"):
            continue
        module = importlib.import_module(fname[:-3])
        for name, fn in getattr(module, "MODELS", {}).items():
            found[name] = fn
    return found


def main():
    args = sys.argv[1:]
    preview = None
    if "--preview" in args:
        i = args.index("--preview")
        preview = args[i + 1]
        del args[i:i + 2]
    filt = args[0] if args else ""
    models = all_models()
    done = 0
    for name, fn in models.items():
        if filt and filt.lower() not in name.lower():
            continue
        k.reset()
        options = fn() or {}
        path = k.export(name, **options)
        if preview:
            os.makedirs(preview, exist_ok=True)
            yaw = options.get("preview_yaw", 35.0) if isinstance(options, dict) else 35.0
            k.reset()
            k.load_emsh(path)
            k.render_preview(os.path.join(preview, name + ".png"), yaw=yaw, samples=24, size=(720, 540))
        done += 1
    print("[build_models] modelli costruiti:", done)


if __name__ == "__main__":
    main()
