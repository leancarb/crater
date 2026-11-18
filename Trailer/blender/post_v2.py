"""
CRATER — montaje de la segunda apertura (solo con los cuatro kits). Usa el motor de post.py.

  python -I post_v2.py music <out.wav>
  python -I post_v2.py video <render_dir> <out.mp4> [--wav musica.wav] [--preview N,M]
"""
import os, sys, importlib.util

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location('post', os.path.join(HERE, 'post.py'))
ps = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(ps)

ps.reconfig([('v1', 192, 0), ('v2', 168, 14), ('v3', 168, 12), ('v4', 192, 12),
             ('v5', 192, 12), ('v6', 144, 12), ('v7', 168, 12), ('v8', 180, 16)], hold=36)
at = ps.at

ps.CREDITS = [
    (at('v1', 24),  at('v1', 104), 'bl', 'UN JUEGO EN PRIMERA PERSONA', 'UN FAROL Y DOS FILTROS'),
    (at('v1', 124), at('v1', 190), 'tr', 'EL VALLE', 'LA ESTACIÓN DE OBSERVACIÓN'),
    (at('v2', 26),  at('v2', 120), 'br', '01 · EXPLANADA Y PASAJE', 'EL CRÁTER SE ABRE'),
    (at('v3', 24),  at('v3', 120), 'tl', 'LOS CUADROS DE HISTORIA', 'CUENTAN SIN PALABRAS'),
    (at('v4', 30),  at('v4', 128), 'tr', '03 · ROTONDA', 'EL FAROL'),
    (at('v5', 30),  at('v5', 130), 'bl', 'FILTRO SOL', 'ENCIENDE LAS ANCLAS'),
    (at('v6', 20),  at('v6', 110), 'br', 'FILTRO LUNA', 'DISUELVE LA REJA'),
    (at('v7', 16),  at('v7', 100), 'tl', '05 · CRESTA', 'PARA SALIR HAY QUE APAGARLO'),
]
ps.PROXIMAMENTE = at('v8', 110)


def title_layers():
    import numpy as np
    t = ps.centered_layer('CRÁTER', 132, int(ps.H * 0.36), font='Cinzel-600.ttf', track=34, alpha=246)
    # la luz blanca del fin del eclipse inunda todo entre la Cresta y el epilogo
    blanco = np.ones((ps.H, ps.W, 4), np.float32); blanco[..., :3] = (1.0, 0.985, 0.96)
    return [(at('v7', 132), at('v8', 30), blanco, 22), (at('v8', 34), ps.TOTAL + 100, t, 30)]


def events():
    sec = lambda shot, fr: at(shot, fr) / ps.FPS
    return dict(plunge=sec('v2', 1), pads=sec('v3', 1), chimes=sec('v6', 40), double=sec('v4', 1),
                booms=[(sec('v2', 1), 0.55), (sec('v3', 1), 0.3), (sec('v4', 1), 0.4), (sec('v5', 1), 0.4),
                       (sec('v5', 34), 0.3), (sec('v5', 74), 0.3), (sec('v6', 1), 0.35), (sec('v7', 1), 0.45)],
                ticks=(sec('v5', 96), sec('v5', 150)), osti_end=sec('v7', 56), flash=sec('v7', 112), title=sec('v7', 152),
                prog='i-iv-VI-V')


if __name__ == '__main__':
    a = sys.argv[1:]
    if a[0] == 'music':
        ps.music(a[1], events())
    elif a[0] == 'video':
        ps.EXTRA = title_layers()
        wav = a[a.index('--wav') + 1] if '--wav' in a else None
        prev = [int(x) for x in a[a.index('--preview') + 1].split(',')] if '--preview' in a else None
        ps.video(a[1], a[2], wav, prev)
    elif a[0] == 'edit':
        for e in ps.EDIT: print(e)
        print('total', ps.TOTAL, ps.TOTAL / ps.FPS)
