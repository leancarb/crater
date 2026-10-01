"""Genera la planta del mapa de CRÁTER (SVG) leyendo las medidas del constructor.

Lee las llamadas Caja(...), anclas, rejas, trampillas, puentes y compuertas de
Assets/Editor/ConstructorCrater*.cs, así la planta queda igual al nivel real.
La capilla está arriba del cráter (en la superficie): se dibuja al sur, punteada.

Uso:  python3 Tools/Planta/generar_planta.py   →  Docs/Planta_CRATER.svg
(Para un PNG: abrir el SVG en un navegador y exportarlo, o usar Chrome headless.)
"""
import math, os, re

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
EDITOR = os.path.join(RAIZ, "Assets", "Editor")
SALIDA = os.path.join(RAIZ, "Docs", "Planta_CRATER.svg")

# variables que usan algunas medidas del pozo
VARS = {"piso": 0.0, "suelo": 8.9, "borde": 9.2, "masa": 8.75, "InicioEscalera": -46.3, "FinEscalera": -34.6,
        "MitadEscalera": 1.6, "RadioPozo": 10.5, "CentroPozo.z": -37.5}

SOL, LUNA, TINTA, GRIS = "#E08A2C", "#5E82E6", "#1E1E1E", "#8A8A8A"
X0, X1, Z0, Z1, S = -44.0, 66.0, -84.0, 104.0, 6.0
ANCHO, ALTO = (X1 - X0) * S, (Z1 - Z0) * S + 70


def num(expr):
    e = expr.strip().replace("f", "")
    for k, v in sorted(VARS.items(), key=lambda kv: -len(kv[0])):
        e = e.replace(k, repr(v))
    return float(eval(e, {"__builtins__": {}}))


def leer():
    texto = ""
    for f in sorted(os.listdir(EDITOR)):
        if f.startswith("ConstructorCrater") and f.endswith(".cs") and "Capilla" not in f:
            texto += open(os.path.join(EDITOR, f), encoding="utf-8").read()
    return texto


def P(x, z):
    return ((x - X0) * S, (Z1 - z) * S + 40)


def main():
    src = leer()
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{ANCHO:.0f}" height="{ALTO:.0f}" viewBox="0 0 {ANCHO:.0f} {ALTO:.0f}" '
           'font-family="Helvetica, Arial, sans-serif">', f'<rect width="100%" height="100%" fill="white"/>']

    def rect(x0, z0, x1, z1, **a):
        (ax, ay), (bx, by) = P(min(x0, x1), max(z0, z1)), P(max(x0, x1), min(z0, z1))
        attrs = " ".join(f'{k.replace("_", "-")}="{v}"' for k, v in a.items())
        out.append(f'<rect x="{ax:.1f}" y="{ay:.1f}" width="{bx - ax:.1f}" height="{by - ay:.1f}" {attrs}/>')

    def texto(x, z, t, size=13, color=TINTA, peso="600", ancla="middle"):
        px, py = P(x, z)
        out.append(f'<text x="{px:.1f}" y="{py:.1f}" font-size="{size}" font-weight="{peso}" fill="{color}" text-anchor="{ancla}" letter-spacing="1">{t}</text>')


    # cajas del nivel
    cajas = []
    for m in re.finditer(r'Caja\(\w+, \$?"([^"]+)", ([^;]*?), k\.(\w+)\)', src):
        partes = [p for p in m.group(2).split(",")]
        if len(partes) != 6:
            continue
        try:
            v = [num(p) for p in partes]
        except Exception:
            continue
        cajas.append((m.group(1), v))
    for n, (x0, y0, z0, x1, y1, z1) in cajas:   # pisos
        if n == "Piso_Pozo":
            continue   # el fondo del pozo es redondo: se dibuja abajo
        if n.startswith(("Piso", "Descanso", "Repisa")) or (abs(y1) < 0.05 and y0 < 0):
            rect(x0, z0, x1, z1, fill="#EDEDED" if y1 > -1 else "#D6D6D6")
    # el fondo y la pared del pozo (la roca del norte se dibuja encima)
    cx, cy = P(0, -37.5)
    out.append(f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{10.5 * S:.1f}" fill="#EDEDED"/>')
    a, b = P(-1.9, -48.3), P(1.9, -48.3)
    out.append(f'<path d="M{a[0]:.1f} {a[1]:.1f} A {11.2 * S:.1f} {11.2 * S:.1f} 0 1 1 {b[0]:.1f} {b[1]:.1f}" fill="none" stroke="{TINTA}" stroke-width="{1.4 * S:.1f}"/>')
    rect(-1.75, -34.6, 1.75, -23, fill="#EDEDED")   # el pasaje sigue abierto por encima del arco
    # escalera del pozo
    for i in range(14):
        z = -46.3 + i * (11.7 / 14)
        a, b = P(-1.6, z), P(1.6, z)
        out.append(f'<line x1="{a[0]:.1f}" y1="{a[1]:.1f}" x2="{b[0]:.1f}" y2="{b[1]:.1f}" stroke="{GRIS}" stroke-width="0.8"/>')
    rect(-1.6, -46.3, 1.6, -34.6, fill="none", stroke=GRIS, stroke_width="0.8")
    # las paredes de la rotonda se arman en un bucle: van a mano
    for lado in (-1, 1):
        xa = -12.3 if lado < 0 else 12.0
        for za, zb in [(-3.3, -1), (3, 23), (26.5, 27.3)]:
            cajas.append(("Muro_Rotonda", [xa, -0.3, za, xa + 0.3, 7, zb]))
    for n, (x0, y0, z0, x1, y1, z1) in cajas:   # muros y roca
        if n.startswith(("Techo", "Dintel", "Pared_Puerta")):
            continue
        if n.startswith("Masa"):
            if y0 < 1:
                rect(x0, z0, x1, z1, fill="#3A3A3A")
        elif y0 < 1.5 and y1 > 1.5:
            rect(x0, z0, x1, z1, fill=TINTA)
    # puentes
    for m in re.finditer(r'Puente\(k, g, "([^"]+)", new Vector3\(([^)]*)\), new Vector3\(([^)]*)\), ([\d.]+)f', src):
        a = [num(p) for p in m.group(2).split(",")]
        b = [num(p) for p in m.group(3).split(",")]
        w = float(m.group(4)) / 2
        if abs(b[0] - a[0]) > abs(b[2] - a[2]):
            rect(a[0], a[2] - w, b[0], a[2] + w, fill=SOL, fill_opacity="0.35", stroke=SOL, stroke_width="1")
        else:
            rect(a[0] - w, a[2], a[0] + w, b[2], fill=SOL, fill_opacity="0.35", stroke=SOL, stroke_width="1")

    # rejas paradas (6 m por la escala) y trampillas
    for m in re.finditer(r'Reja\(k, g, "([^"]+)", new Vector3\(([^)]*)\)(?:, ([-\d.]+)f?)?(?:, new Vector3\(([^)]*)\))?\)', src):
        p = [num(v) for v in m.group(2).split(",")]
        rot = float(m.group(3) or 0)
        esc = num(m.group(4).split(",")[0]) if m.group(4) else 1.0
        w = 3 * esc
        if m.group(1).startswith("Sello"):
            continue
        if abs(rot) % 180 == 90:
            rect(p[0] - 0.35, p[2] - w, p[0] + 0.35, p[2] + w, fill=LUNA)
        else:
            rect(p[0] - w, p[2] - 0.35, p[0] + w, p[2] + 0.35, fill=LUNA)
    for m in re.finditer(r'Trampilla\(k, g, "([^"]+)", new Vector3\(([^)]*)\)\)', src):
        p = [num(v) for v in m.group(2).split(",")]
        rect(p[0] - 3, p[2], p[0] + 3, p[2] + 4.6, fill=LUNA, fill_opacity="0.25", stroke=LUNA, stroke_width="1.5", stroke_dasharray="4 3")

    # compuertas y puertas
    for m in re.finditer(r'CompuertaLosa\(k, g, "([^"]+)", new Vector3\(([^)]*)\), new Vector3\(([^)]*)\)', src):
        c = [num(v) for v in m.group(2).split(",")]
        t = [num(v) for v in m.group(3).split(",")]
        rect(c[0] - t[0] / 2, c[2] - t[2] / 2, c[0] + t[0] / 2, c[2] + t[2] / 2, fill="white", stroke=TINTA, stroke_width="1.2", stroke_dasharray="3 2")
    for z0, z1 in [(-3.3, -2.9), (74.7, 75.0)]:   # compuerta del Umbral y cierre de la Cresta
        rect(-2 if z0 > 0 else -6, z0, 2 if z0 > 0 else 6, z1, fill="white", stroke=TINTA, stroke_width="1.2", stroke_dasharray="3 2")

    # anclas (las del techo, huecas) y sellos
    for m in re.finditer(r'CrearAnclaEnEscena\(k, g, "([^"]+)", new Vector3\(([^)]*)\)', src):
        x, y, z = [num(v) for v in m.group(2).split(",")]
        px, py = P(x, z)
        if m.group(1).startswith("Sello"):
            out.append(estrella(px, py, SOL))
        elif y > 1:
            out.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="4.5" fill="white" stroke="{SOL}" stroke-width="2"/>')
        else:
            out.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="4.5" fill="{SOL}"/>')
    px, py = P(14.6, 26.4)
    out.append(estrella(px, py, LUNA))
    px, py = P(-5.1, -10)
    out.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="4.5" fill="white" stroke="{GRIS}" stroke-width="2"/>')

    # anillo de columnas y obelisco de la rotonda
    for i in range(6):
        a = math.radians(-90 + 30 + i * 60 - 30)
        px, py = P(5.2 * math.cos(a), 12 + 5.2 * math.sin(a))
        out.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="3" fill="{GRIS}"/>')
    rect(-0.8, 16.4, 0.8, 18.0, fill=GRIS)

    # la capilla, arriba en la superficie
    rect(-4.2, -78.6, 4.2, -64.4, fill="none", stroke=GRIS, stroke_width="1.5", stroke_dasharray="5 3")
    rect(-1.0, -64.4, 1.0, -48.3, fill="none", stroke=GRIS, stroke_width="1", stroke_dasharray="2 3")
    texto(0, -72.5, "CAPILLA", 12, GRIS)
    texto(3, -58, "en la superficie, 9 m más arriba", 9.5, GRIS, "400", "start")

    # nombres
    texto(-5.5, -40.5, "POZO", 12)
    texto(-4.5, -43.3, "escalera", 9.5, GRIS, "400")
    texto(-2.8, -29, "pasaje", 9.5, "white", "400", "end")
    texto(0, -15, "UMBRAL", 12)
    texto(0, 4, "ROTONDA", 13)
    texto(-24, -10.5, "ALA SOL", 13, SOL)
    texto(24, -10.5, "ALA LUNA", 13, LUNA)
    texto(9, 47, "CRUCE", 13, TINTA, "600", "start")
    texto(0, 85, "CRESTA", 13)
    texto(0, 101.5, "pared espejo", 9.5, GRIS, "400")

    # título, escala y norte
    out.append(f'<text x="24" y="28" font-size="16" font-weight="700" fill="{TINTA}" letter-spacing="2">CRÁTER · PLANTA</text>')
    ex, ey = P(40, -80)
    out.append(f'<line x1="{ex:.1f}" y1="{ey:.1f}" x2="{ex + 10 * S:.1f}" y2="{ey:.1f}" stroke="{TINTA}" stroke-width="2"/>'
               f'<text x="{ex + 5 * S:.1f}" y="{ey - 6:.1f}" font-size="10" fill="{TINTA}" text-anchor="middle">10 m</text>')
    nx, ny = P(58, 98)
    out.append(f'<path d="M{nx} {ny - 22} L{nx - 7} {ny} L{nx} {ny - 5} L{nx + 7} {ny} Z" fill="{TINTA}"/>'
               f'<text x="{nx}" y="{ny + 14}" font-size="11" font-weight="700" text-anchor="middle" fill="{TINTA}">N</text>')

    # referencias
    lx, ly = P(40, 70)
    filas = [
        (f'<circle cx="{lx + 6}" cy="{{y}}" r="4.5" fill="{SOL}"/>', "ancla (filtro sol)"),
        (f'<circle cx="{lx + 6}" cy="{{y}}" r="4.5" fill="white" stroke="{SOL}" stroke-width="2"/>', "ancla en el techo"),
        (f'<rect x="{lx}" y="{{y0}}" width="12" height="5" fill="{LUNA}"/>', "reja (filtro luna)"),
        (f'<rect x="{lx}" y="{{y0}}" width="12" height="9" fill="{LUNA}" fill-opacity="0.25" stroke="{LUNA}" stroke-dasharray="4 3"/>', "trampilla / escotilla"),
        (f'<rect x="{lx}" y="{{y0}}" width="12" height="9" fill="{SOL}" fill-opacity="0.35" stroke="{SOL}"/>', "puente de luz"),
        (f'<rect x="{lx}" y="{{y0}}" width="12" height="5" fill="white" stroke="{TINTA}" stroke-dasharray="3 2"/>', "puerta / compuerta"),
        ("STAR", "sello"),
        (f'<rect x="{lx}" y="{{y0}}" width="12" height="9" fill="white" stroke="{GRIS}" stroke-width="0.6"/>', "abismo (sin piso)"),
        (f'<rect x="{lx}" y="{{y0}}" width="12" height="9" fill="#D6D6D6"/>', "piso más bajo"),
    ]
    out.append(f'<text x="{lx}" y="{ly - 14}" font-size="11" font-weight="700" fill="{TINTA}" letter-spacing="1">REFERENCIAS</text>')
    for i, (forma, nombre) in enumerate(filas):
        y = ly + i * 20
        out.append(estrella(lx + 6, y, SOL) if forma == "STAR" else forma.format(y=y, y0=y - 4))
        out.append(f'<text x="{lx + 20}" y="{y + 4}" font-size="10.5" fill="{TINTA}">{nombre}</text>')

    out.append("</svg>")
    os.makedirs(os.path.dirname(SALIDA), exist_ok=True)
    open(SALIDA, "w", encoding="utf-8").write("\n".join(out))
    print("Planta guardada en", SALIDA)


def estrella(cx, cy, color, r=7):
    pts = []
    for i in range(8):
        rr = r if i % 2 == 0 else r * 0.4
        a = math.radians(-90 + i * 45)
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{color}"/>'


if __name__ == "__main__":
    main()
