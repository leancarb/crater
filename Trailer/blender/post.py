"""
CRATER — montaje de la secuencia de apertura renderizada en Blender.

  python -I post.py music <out.wav>
  python -I post.py video <render_dir> <out.mp4> [--wav musica.wav] [--preview N]

Une los planos con fundidos, agrega brillo (bloom), grano y vinetas, escala a 1080p,
superpone los creditos de esquina (al estilo de la apertura de Silo) y codifica con ffmpeg.
"""
import sys, os, math, subprocess
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.environ.get('CRATER_FONTS', os.path.join(HERE, 'fonts'))


def ffmpeg_exe():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        return 'ffmpeg'


FPS = 24
W, H = 1920, 1080

# (plano, cuadros, fundido con el anterior)
SHOTS = [('s1', 192, 0), ('s2', 156, 14), ('s3', 318, 12), ('s5', 168, 12),
         ('s6', 168, 12), ('s7', 144, 12), ('s8', 120, 10), ('s9', 180, 8)]
HOLD = 30          # el ultimo cuadro (titulo) se sostiene
FADE_IN, FADE_OUT = 20, 40


def timeline():
    t, out = 0, []
    for name, n, xf in SHOTS:
        t -= xf
        out.append((name, t, n, xf))
        t += n
    return out, t + HOLD


EDIT, TOTAL = timeline()
START = {name: st for name, st, n, xf in EDIT}
EXTRA = []          # capas extra: (cuadro inicial, cuadro final, capa RGBA, rampa)


def reconfig(shots, hold=None):
    """cambia la lista de planos (para montar otra version con el mismo motor)"""
    global SHOTS, EDIT, TOTAL, START, HOLD
    SHOTS = shots
    if hold is not None: HOLD = hold
    EDIT, TOTAL = timeline()
    START = {name: st for name, st, n, xf in EDIT}


def at(shot, frame):
    """cuadro de salida (0-based) del cuadro `frame` (1-based) de un plano"""
    return START[shot] + frame - 1


# ------------------------------------------------------------------ creditos
# (cuadro inicial, cuadro final, esquina, linea chica, linea grande)
CREDITS = [
    (at('s1', 24),  at('s1', 104), 'bl', 'DURANTE UN ECLIPSE TOTAL', 'UN CRÁTER SE ABRE EN EL VALLE'),
    (at('s1', 126), at('s1', 190), 'tr', '00 · PRÓLOGO EXTERIOR', 'LA CAPILLA'),
    (at('s2', 30),  at('s2', 128), 'br', '01 · EXPLANADA Y PASAJE', 'EL POZO'),
    (at('s3', 30),  at('s3', 120), 'tl', '02 · UMBRAL', 'LA ESCALERA'),
    (at('s3', 236), at('s3', 306), 'bl', 'FILTRO LUNA', 'DISUELVE LA MATERIA HUECA'),
    (at('s5', 24),  at('s5', 120), 'tr', '03 · ROTONDA', 'EL FAROL'),
    (at('s6', 30),  at('s6', 126), 'br', 'FILTRO SOL', 'ENCIENDE LAS ANCLAS'),
    (at('s7', 24),  at('s7', 116), 'tl', '06 · ESTACIÓN DE OBSERVACIÓN', 'AL ATARDECER'),
    (at('s8', 16),  at('s8', 106), 'bl', '04 · CRUCE   05 · CRESTA', 'PARA SALIR HAY QUE APAGARLO'),
]
PROXIMAMENTE = at('s9', 140)
PROX_Y = 0.66       # altura del «PRÓXIMAMENTE» (fraccion del alto)


def text_layer(small, big, corner):
    from PIL import Image, ImageDraw, ImageFont
    f1 = ImageFont.truetype(os.path.join(FONTS, 'IBMPlexSans-500.ttf'), 17)
    f2 = ImageFont.truetype(os.path.join(FONTS, 'Italiana-400.ttf'), 44)
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)

    def spaced(txt, font, track):
        widths = [font.size * 0.42 if ch == ' ' else d.textlength(ch, font=font) for ch in txt]
        return widths, sum(widths) + track * (len(txt) - 1)

    def draw(txt, font, track, x, y, right, fill):
        widths, total = spaced(txt, font, track)
        if right: x -= total
        for ch, w_ in zip(txt, widths):
            d.text((x, y), ch, font=font, fill=fill); x += w_ + track

    mx, my = int(W * 0.065), int(H * 0.085)
    right = corner[1] == 'r'; top = corner[0] == 't'
    x = W - mx if right else mx
    y0 = my if top else H - my - 78
    draw(small, f1, 5.4, x, y0, right, (226, 214, 196, 190))
    draw(big, f2, 4.0, x, y0 + 26, right, (240, 232, 218, 245))
    return np.asarray(im).astype(np.float32) / 255.0


def centered_layer(txt, size, y, font='IBMPlexSans-300.ttf', track=9.0, alpha=220):
    from PIL import Image, ImageDraw, ImageFont
    f = ImageFont.truetype(os.path.join(FONTS, font), size)
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    widths = [size * 0.42 if ch == ' ' else d.textlength(ch, font=f) for ch in txt]; total = sum(widths) + track * (len(txt) - 1)
    x = (W - total) / 2
    for ch, w_ in zip(txt, widths):
        d.text((x, y), ch, font=f, fill=(236, 226, 210, alpha)); x += w_ + track
    return np.asarray(im).astype(np.float32) / 255.0


def env(f, a, b, ramp=12):
    if f < a or f > b: return 0.0
    return min(1.0, (f - a) / ramp, (b - f) / ramp)


# ------------------------------------------------------------------ imagen
def load(render, shot, frame):
    from PIL import Image
    p = os.path.join(render, shot, f'{frame:04d}.png')
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float32) / 255.0


def source(render, f):
    """cuadro de salida f -> mezcla de los planos activos"""
    acc = None
    for name, st, n, xf in EDIT:
        if f < st or f >= st + n:
            continue
        img = load(render, name, f - st + 1)
        if acc is None:
            acc = img
        else:
            w = (f - st + 1) / (xf + 1)       # fundido con el plano anterior
            acc = acc * (1 - w) + img * w
    if acc is None:     # sostenido final
        name, st, n, xf = EDIT[-1]
        acc = load(render, name, n)
    return acc


def grade(img, f, rng, vign, grain_s):
    from scipy.ndimage import gaussian_filter, zoom
    from PIL import Image
    # bloom: lo brillante se esparce, en dos radios
    lum = img.max(axis=2, keepdims=True)
    bright = img * np.clip((lum - 0.62) / 0.38, 0, 1)
    small = bright[::2, ::2]
    b1 = np.stack([gaussian_filter(small[..., c], 5) for c in range(3)], -1)
    b2 = np.stack([gaussian_filter(small[..., c], 18) for c in range(3)], -1)
    bloom = zoom(b1 * 0.55 + b2 * 0.45, (2, 2, 1), order=1)[:img.shape[0], :img.shape[1]]
    img = img + bloom * 0.55
    # tono: negros apenas levantados y calidos, contraste suave
    img = np.clip(img, 0, 1)
    img = img * img * (3 - 2 * img) * 0.35 + img * 0.65
    img = img * 0.97 + np.array([0.012, 0.008, 0.004], np.float32)
    up = np.asarray(Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).resize((W, H), Image.LANCZOS)).astype(np.float32) / 255.0
    up = up * vign
    g = rng.standard_normal((H, W), dtype=np.float32)[..., None] * grain_s
    return up + g * (0.35 + 0.65 * (1 - up))


def video(render, out, wav=None, preview=None):
    rng = np.random.default_rng(5)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    r = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    vign = (1 - 0.32 * np.clip(r - 0.45, 0, 1) ** 1.6)[..., None]
    layers = [(a, b, text_layer(s, g, c)) for a, b, c, s, g in CREDITS]
    prox = centered_layer('PRÓXIMAMENTE', 22, int(H * PROX_Y))
    frames = range(TOTAL) if preview is None else preview
    if preview is not None:
        from PIL import Image
        os.makedirs(out, exist_ok=True)
    else:
        cmd = [ffmpeg_exe(), '-y', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-']
        if wav: cmd += ['-i', wav]
        cmd += ['-c:v', 'libx264', '-preset', 'slow', '-crf', '21', '-maxrate', '3200k', '-bufsize', '6400k',
                '-pix_fmt', 'yuv420p', '-movflags', '+faststart']
        if wav: cmd += ['-c:a', 'aac', '-b:a', '192k', '-shortest']
        cmd += [out]
        proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=subprocess.DEVNULL)
    for f in frames:
        img = grade(source(render, f), f, rng, vign, 0.013)
        for a, b, lay in layers:
            k = env(f, a, b)
            if k > 0:
                al = lay[..., 3:4] * k
                img = img * (1 - al) + lay[..., :3] * al
        k = env(f, PROXIMAMENTE, TOTAL + 100, 18)
        if k > 0:
            al = prox[..., 3:4] * k; img = img * (1 - al) + prox[..., :3] * al
        for a, b, lay, ramp in EXTRA:
            k = env(f, a, b, ramp)
            if k > 0:
                al = lay[..., 3:4] * k; img = img * (1 - al) + lay[..., :3] * al
        fade = min(1.0, (f + 1) / FADE_IN, (TOTAL - f) / FADE_OUT)
        img = np.clip(img * fade, 0, 1)
        px = (img * 255 + 0.5).astype(np.uint8)
        if preview is not None:
            Image.fromarray(px).save(os.path.join(out, f'{f:05d}.png'))
        else:
            proc.stdin.write(px.tobytes())
        if f % 120 == 0: print('cuadro', f, '/', TOTAL, flush=True)
    if preview is None:
        proc.stdin.close(); proc.wait()


# ------------------------------------------------------------------ musica
SR = 48000


def note_hz(n):
    """n: nombre tipo 'D4', 'Bb3', 'F#2'"""
    names = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}
    k = names[n[0]]; i = 1
    if n[1] == 'b': k -= 1; i = 2
    elif n[1] == '#': k += 1; i = 2
    octv = int(n[i:])
    return 440.0 * 2 ** ((k + 12 * (octv + 1) - 69) / 12)


def adsr(n, a, d, s, r):
    e = np.ones(n, np.float32) * s
    na, nd, nr = int(a * SR), int(d * SR), int(r * SR)
    na = min(na, n); e[:na] = np.linspace(0, 1, na, endpoint=False)
    nd = min(nd, n - na); e[na:na + nd] = np.linspace(1, s, nd, endpoint=False)
    nr = min(nr, n); e[n - nr:] *= np.linspace(1, 0, nr)
    return e


def bowed(freq, dur, amp, bright=10, vib=0.004, attack=0.012, decay=0.14, sus=0.45, rel=0.09, seed=0):
    n = int(dur * SR); t = np.arange(n) / SR
    ph = 2 * np.pi * freq * (t + vib / (2 * np.pi * 5.2) * np.sin(2 * np.pi * 5.2 * t + seed))
    y = np.zeros(n, np.float32)
    for h in range(1, bright + 1):
        if freq * h > 9000: break
        y += np.sin(h * ph).astype(np.float32) / h ** 1.15
    return y * adsr(n, attack, decay, sus, rel) * amp


def pad(freqs, dur, amp, attack=1.6, rel=1.8):
    n = int(dur * SR); t = np.arange(n) / SR
    y = np.zeros(n, np.float32)
    for f in freqs:
        for det in (-0.12, 0.0, 0.13):
            ph = 2 * np.pi * f * (1 + det / 100) * t
            for h in range(1, 7):
                y += (np.sin(h * ph + h * det) / h ** 1.6).astype(np.float32)
    return y / (len(freqs) * 3) * adsr(n, attack, 0.1, 1.0, rel) * amp


def boom(dur, amp, f0=78, f1=30):
    n = int(dur * SR); t = np.arange(n) / SR
    f = f1 + (f0 - f1) * np.exp(-t * 5)
    ph = 2 * np.pi * np.cumsum(f) / SR
    y = np.sin(ph) * np.exp(-t * 1.6)
    rng = np.random.default_rng(1)
    nz = rng.standard_normal(n) * np.exp(-t * 18) * 0.25
    from scipy.signal import butter, lfilter
    b, a = butter(2, 260 / (SR / 2)); nz = lfilter(b, a, nz)
    return ((y + nz) * amp).astype(np.float32)


def riser(dur, amp):
    from scipy.signal import butter, sosfilt
    n = int(dur * SR); t = np.arange(n) / SR
    rng = np.random.default_rng(2)
    nz = rng.standard_normal(n)
    out = np.zeros(n)
    segs = 24
    for i in range(segs):        # ruido filtrado cuyo centro sube
        a, b = i * n // segs, (i + 1) * n // segs
        fc = 300 * (12 ** (i / segs))
        sos = butter(2, [fc * 0.7 / (SR / 2), min(0.95, fc * 1.4 / (SR / 2))], 'band', output='sos')
        out[a:b] = sosfilt(sos, nz[max(0, a - 2000):b])[-(b - a):]
    gl = np.sin(2 * np.pi * np.cumsum(220 * 2 ** (2 * t / dur)) / SR) * 0.25
    e = (t / dur) ** 2.2
    return ((out * 0.8 + gl) * e * amp).astype(np.float32)


def tick(amp):
    from scipy.signal import butter, lfilter
    n = int(0.03 * SR); rng = np.random.default_rng(3)
    y = rng.standard_normal(n) * np.exp(-np.arange(n) / SR * 260)
    b, a = butter(2, [1800 / (SR / 2), 4200 / (SR / 2)], 'band')
    return (lfilter(b, a, y) * amp).astype(np.float32)


def reverb(x, secs=3.2, wet=0.32, seed=0):
    from scipy.signal import fftconvolve
    n = int(secs * SR); t = np.arange(n) / SR
    rng = np.random.default_rng(seed)
    ir = rng.standard_normal(n) * np.exp(-t / 0.9) * (1 - np.exp(-t / 0.01))
    ir /= np.sqrt(np.sum(ir ** 2))
    y = fftconvolve(x, ir)[:len(x)]
    return x * (1 - wet) + y * wet * 1.1


def music_events_v1():
    sec = lambda shot, fr: at(shot, fr) / FPS
    return dict(plunge=sec('s2', 1), pads=sec('s3', 1), chimes=sec('s3', 250), double=sec('s5', 1),
                booms=[(sec('s2', 1), 0.55), (sec('s3', 1), 0.35), (sec('s3', 250), 0.3), (sec('s5', 1), 0.4), (sec('s6', 1), 0.35),
                       (sec('s7', 1), 0.45), (sec('s8', 1), 0.4), (sec('s9', 1), 0.5)],
                ticks=(sec('s7', 1), sec('s8', 1)), osti_end=sec('s9', 118), flash=sec('s9', 96), title=sec('s9', 118),
                prog='i-VI-III-VII')


def music(out_wav, ev=None):
    ev = ev or music_events_v1()
    dur = TOTAL / FPS + 1.5
    N = int(dur * SR)
    L = np.zeros(N, np.float32); R = np.zeros(N, np.float32)

    def put(sig, t0, pan=0.0, gain=1.0):
        i = int(t0 * SR)
        if i >= N: return
        sig = sig[:N - i] * gain
        L[i:i + len(sig)] += sig * math.sqrt(0.5 * (1 - pan))
        R[i:i + len(sig)] += sig * math.sqrt(0.5 * (1 + pan))

    T_PLUNGE, T_STAIRS, T_REJA, T_SPIRAL = ev['plunge'], ev['pads'], ev['chimes'], ev['double']
    T_FLASH, T_TITLE, T_OSTI = ev['flash'], ev['title'], ev['osti_end']
    END = TOTAL / FPS

    # 1 · drone grave en Re, todo el tema
    t = np.arange(N) / SR
    dr = (np.sin(2 * np.pi * 36.71 * t) * 0.55 + np.sin(2 * np.pi * 73.42 * t + 0.4) * 0.35 + np.sin(2 * np.pi * 110.0 * t) * 0.1)
    dr *= (0.75 + 0.25 * np.sin(2 * np.pi * 0.11 * t))
    de = np.clip(t / 5.0, 0, 1) * np.clip((T_TITLE - t) / 0.4 + 1, 0, 1) * 0.5
    if T_OSTI < T_TITLE - 0.5:      # el drone baja cuando se apaga el farol y vuelve con la luz
        de = de * (1 - 0.6 * np.clip((t - T_OSTI) / 0.6, 0, 1) * np.clip((T_FLASH - t) / 1.0, 0, 1))
    dr = (dr * de).astype(np.float32)
    put(dr, 0, 0, 0.30)

    # 2 · brillo del eclipse (agudos tremolo) al llegar la totalidad
    sh = (np.sin(2 * np.pi * note_hz('D6') * t) + 0.6 * np.sin(2 * np.pi * note_hz('A6') * t + 1)) * (0.6 + 0.4 * np.sin(2 * np.pi * 6.1 * t))
    se = np.clip((t - 2.2) / 1.6, 0, 1) * np.clip((T_PLUNGE + 1.5 - t) / 2.5, 0, 1)
    put((sh * se).astype(np.float32), 0, 0.2, 0.035)

    # 3 · ostinato de cuerdas (corcheas), progresion i-VI-III-VII en Re menor
    bpm = 100; e8 = 60 / bpm / 2
    if ev.get('prog') == 'i-iv-VI-V':
        prog = [('D3', 'A3', 'F4', 'A4', 'D4', 'A3', 'F4', 'A4'),
                ('G2', 'D3', 'Bb3', 'D4', 'G3', 'D3', 'Bb3', 'D4'),
                ('Bb2', 'F3', 'D4', 'F4', 'Bb3', 'F3', 'D4', 'F4'),
                ('A2', 'E3', 'C#4', 'E4', 'A3', 'E3', 'C#4', 'E4')]
        chords = [('D3', 'F3', 'A3', 'D4'), ('G2', 'Bb2', 'D3', 'G3'), ('Bb2', 'D3', 'F3', 'Bb3'), ('A2', 'C#3', 'E3', 'A3')]
    else:
        prog = [('D3', 'A3', 'F4', 'A4', 'D4', 'A3', 'F4', 'A4'),
                ('Bb2', 'F3', 'D4', 'F4', 'Bb3', 'F3', 'D4', 'F4'),
                ('F3', 'C4', 'A4', 'C5', 'F4', 'C4', 'A4', 'C5'),
                ('C3', 'G3', 'E4', 'G4', 'C4', 'G3', 'E4', 'G4')]
        chords = [('D3', 'F3', 'A3', 'D4'), ('Bb2', 'D3', 'F3', 'Bb3'), ('F2', 'A3', 'C4', 'F4'), ('C3', 'E3', 'G3', 'C4')]
    bar = 8 * e8
    tt = T_PLUNGE; i = 0
    while tt < T_OSTI - 0.05:
        ch = prog[(i // 8 // 2) % 4][i % 8]
        prog_t = (tt - T_PLUNGE) / (T_OSTI - T_PLUNGE)
        amp = 0.12 + 0.5 * prog_t ** 1.3
        acc = 1.25 if i % 4 == 0 else 1.0
        bright = 6 + int(8 * prog_t)
        put(bowed(note_hz(ch), e8 * 1.05, amp * acc, bright=bright, seed=i), tt, -0.25 + 0.5 * ((i % 2) == 0), 0.55)
        if tt > T_SPIRAL:   # octava grave doblando, desde la espiral
            put(bowed(note_hz(ch) / 2, e8 * 1.05, amp * 0.5, bright=bright, seed=i + 7), tt, 0.0, 0.5)
        tt += e8; i += 1

    # 4 · pads que cambian cada dos compases
    tt = T_STAIRS; j = 0
    while tt < T_OSTI:
        d_ = min(2 * bar + 1.6, T_OSTI - tt + 0.6)
        g = 0.1 + 0.22 * min(1, (tt - T_STAIRS) / (T_OSTI - T_STAIRS))
        put(pad([note_hz(n) for n in chords[(j + 0) % 4]], d_, g, attack=1.4, rel=1.2), tt, 0.0, 1.0)
        tt += 2 * bar; j += 1

    # 5 · golpes graves en los cortes fuertes
    for t0, g in ev['booms']:
        put(boom(3.0, g), t0, 0, 1.0)

    # 6 · reja de HUECO: campanitas frias que se deshacen
    for k_, n in enumerate(['A5', 'E6', 'D6', 'A6', 'F6', 'E6']):
        put(bowed(note_hz(n), 1.6, 0.05, bright=3, attack=0.004, decay=0.6, sus=0.0, rel=0.6), T_REJA + k_ * 0.27, 0.4 - k_ * 0.15, 1.0)

    # 7 · tic-tac del mecanismo (dial) en corcheas
    t_a, t_b = ev['ticks']
    tt = t_a
    while tt < t_b:
        put(tick(0.12 if int((tt - t_a) / e8) % 2 == 0 else 0.07), tt, 0.3, 1.0); tt += e8
    if T_OSTI < T_TITLE - 0.5:      # farol apagado: solo un brillo agudo mientras aparecen los tallados
        n_ = int((T_FLASH - T_OSTI + 1.0) * SR); tt_ = np.arange(n_) / SR
        g_ = (np.sin(2 * np.pi * note_hz('A5') * tt_) + 0.5 * np.sin(2 * np.pi * note_hz('E6') * tt_)) * (0.6 + 0.4 * np.sin(2 * np.pi * 4.3 * tt_))
        g_ *= np.clip(tt_ / 1.2, 0, 1) * np.clip((n_ / SR - tt_) / 0.8, 0, 1)
        put(g_.astype(np.float32), T_OSTI + 0.2, -0.2, 0.05)

    # 8 · subida al anillo de diamante y golpe del titulo
    put(riser(T_TITLE - T_FLASH + 1.8, 0.16), T_FLASH - 1.8, 0, 1.0)
    put(boom(6.0, 0.9, f0=90, f1=28), T_TITLE, 0, 1.0)
    put(pad([note_hz(n) for n in ('D2', 'A2', 'D3', 'F3', 'A3', 'D4')], END - T_TITLE + 1.5, 0.42, attack=0.05, rel=3.0), T_TITLE, 0, 1.0)
    put(bowed(note_hz('D5'), END - T_TITLE, 0.06, bright=4, attack=0.6, decay=0.5, sus=0.7, rel=2.5), T_TITLE + 0.4, 0, 1.0)

    L2 = reverb(L, seed=11); R2 = reverb(R, seed=12)
    st = np.stack([L2, R2], 1)
    fade = np.clip((END + 1.2 - np.arange(N) / SR) / 2.6, 0, 1)[:, None]
    st = st * fade
    st = np.tanh(st / (np.abs(st).max() + 1e-9) * 1.6) / np.tanh(1.6) * 0.89
    from scipy.io import wavfile
    wavfile.write(out_wav, SR, (st * 32767).astype(np.int16))
    print('musica', out_wav, round(N / SR, 2), 's')


if __name__ == '__main__':
    a = sys.argv[1:]
    if a[0] == 'music':
        music(a[1])
    elif a[0] == 'video':
        wav = a[a.index('--wav') + 1] if '--wav' in a else None
        prev = [int(x) for x in a[a.index('--preview') + 1].split(',')] if '--preview' in a else None
        video(a[1], a[2], wav, prev)
    elif a[0] == 'edit':
        for e in EDIT: print(e)
        print('total', TOTAL, TOTAL / FPS)
