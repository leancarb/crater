"""Asienta MAPA_Recorrido sobre Terreno_Crater en CRATER_Mapa_01.blend.

1. Baja o sube el recorrido entero (sin romper sus uniones) hasta que el piso
   del anillo quede a la altura del terreno en su centro.
2. Adapta el terreno: debajo de cada piso transitable queda justo por debajo
   de la losa, y alrededor se funde con el terreno original con un talud
   proporcional al desnivel. El túnel queda siempre enterrado y se abren
   las bocas.
3. Vuelve a exportar los FBX y la vista previa.

Se corre una sola vez sobre el .blend ya preparado:
    blender -b Assets/Art/Blender/CRATER_Mapa_01.blend --python Tools/Blender/asentar_recorrido.py
"""
import bpy
import bmesh
import os
from mathutils import Vector
from mathutils.kdtree import KDTree


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PREVIEW_PATH = os.path.join(ROOT, "Assets", "Art", "Previews", "CRATER_Mapa_01.png")
EXPORT_DIR = os.path.join(ROOT, "Assets", "Models", "CraterMapa")

PISOS = ["Plaza_Piso", "Anillo_Piso", "Terraza_A_Piso", "Terraza_B_Piso", "Terraza_AB_Escalon", "Escalera_Descenso"]
TUNEL = "Tunel_Plaza_Anillo"
FUERA_DEL_RECORRIDO = {"Tunel_Norte"}  # no conecta con nada todavía: queda donde está

BAJO_LOSA = 0.1        # el terreno queda apenas por debajo de la cara superior de cada piso
PASO_MUESTREO = 0.5    # grilla con la que se muestrean los pisos
DENTRO = 0.5           # a esta distancia de un piso, el vértice se considera debajo de él
TALUD = 1.4            # ancho del talud por metro de desnivel (≈ 35°)
TALUD_MIN, TALUD_MAX = 6.0, 40.0
TAPADA_TUNEL = 0.6     # tierra mínima sobre el túnel
CORTES_TERRENO = 2     # subdivisión del terreno cerca del recorrido (3.9 m → 1.3 m)
SUAVIZADO = 12         # pasadas de suavizado de los taludes


def smoothstep(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def world_bounds(obj):
    pts = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    return (Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))),
            Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts))))


def terrain_height(ter, x, y):
    mi = ter.matrix_world.inverted()
    ok, loc, _, _ = ter.ray_cast(mi @ Vector((x, y, 10000)), mi.to_3x3() @ Vector((0, 0, -1)))
    return (ter.matrix_world @ loc).z if ok else None


def sample_tops(obj):
    """Puntos de las caras superiores de un piso, en coordenadas de mundo."""
    lo, hi = world_bounds(obj)
    mi = obj.matrix_world.inverted()
    down = (mi.to_3x3() @ Vector((0, 0, -1))).normalized()
    pts = []
    x = lo.x
    while x <= hi.x + 1e-6:
        y = lo.y
        while y <= hi.y + 1e-6:
            ok, loc, normal, _ = obj.ray_cast(mi @ Vector((x, y, hi.z + 5)), down)
            if ok and (obj.matrix_world.to_3x3() @ normal).normalized().z > 0.7:
                pts.append(Vector((x, y, (obj.matrix_world @ loc).z)))
            y += PASO_MUESTREO
        x += PASO_MUESTREO
    return pts


scene = bpy.context.scene
if scene.get("recorrido_asentado"):
    raise SystemExit("El recorrido ya está asentado; no se vuelve a mover.")

ter = bpy.data.objects["Terreno_Crater"]
recorrido = [o for o in bpy.data.collections["MAPA_Recorrido"].objects if o.name not in FUERA_DEL_RECORRIDO]

# --- 1. Mover el recorrido ------------------------------------------------

anillo = bpy.data.objects["Anillo_Piso"]
lo, hi = world_bounds(anillo)
centro = (lo + hi) / 2
dz = round(terrain_height(ter, centro.x, centro.y) - hi.z, 2)
for obj in recorrido:
    obj.location.z += dz
bpy.context.view_layer.update()
print(f"Recorrido desplazado {dz:+.2f} m")

# --- 2. Adaptar el terreno ------------------------------------------------

puntos = []
for name in PISOS:
    puntos += sample_tops(bpy.data.objects[name])
kd = KDTree(len(puntos))
for i, p in enumerate(puntos):
    kd.insert(Vector((p.x, p.y, 0)), i)
kd.balance()

tunel = bpy.data.objects[TUNEL]
t_lo, t_hi = world_bounds(tunel)
t_eje_y = (t_lo.y + t_hi.y) / 2
t_radio = (t_hi.y - t_lo.y) / 2

zona_lo = Vector((min(p.x for p in puntos) - TALUD_MAX, min(p.y for p in puntos) - TALUD_MAX))
zona_hi = Vector((max(p.x for p in puntos) + TALUD_MAX, max(p.y for p in puntos) + TALUD_MAX))


def en_zona(v):
    return zona_lo.x <= v.x <= zona_hi.x and zona_lo.y <= v.y <= zona_hi.y


mw, mi = ter.matrix_world, ter.matrix_world.inverted()
bm = bmesh.new()
bm.from_mesh(ter.data)

edges = [e for e in bm.edges if all(en_zona(mw @ v.co) for v in e.verts)]
bmesh.ops.subdivide_edges(bm, edges=edges, cuts=CORTES_TERRENO, use_grid_fill=True)


def cortar(co, no, cerca_de):
    """Agrega una arista del terreno sobre el plano (co, no), sólo cerca de las bocas."""
    geom = [f for f in bm.faces if cerca_de(mw @ f.calc_center_median())]
    geom += list({e for f in geom for e in f.edges}) + list({v for f in geom for v in f.verts})
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=mi @ co, plane_no=(mi.to_3x3() @ no).normalized())


# El contorno del túnel queda marcado en el terreno, así el agujero de cada
# boca coincide con el túnel y no con la grilla.
for x in (t_lo.x, t_hi.x):
    cortar(Vector((x, 0, 0)), Vector((1, 0, 0)),
           lambda c, x=x: abs(c.x - x) < 5 and t_lo.y - 8 < c.y < t_hi.y + 8)
for y in (t_lo.y, t_hi.y):
    cortar(Vector((0, y, 0)), Vector((0, 1, 0)),
           lambda c, y=y: abs(c.y - y) < 5 and (abs(c.x - t_lo.x) < 5 or abs(c.x - t_hi.x) < 5))

# Sobre el plano de cada boca el terreno se separa en dos: del lado del cerro
# queda a la altura del techo del túnel, del lado de afuera baja al piso, y
# entre los dos se levanta el frente de la boca.
EPS = 0.01
ALA = 10.0  # el frente se extiende a los costados hasta que la tapada llega al piso
bocas = (t_lo.x, t_hi.x)


def en_frente(w):
    return any(abs(w.x - x) < 1e-3 for x in bocas) and t_lo.y - ALA <= w.y <= t_hi.y + ALA


bmesh.ops.split_edges(bm, edges=[e for e in bm.edges if all(en_frente(mw @ v.co) for v in e.verts)])


def lado_cerro(v):
    """True si el vértice del frente pertenece a las caras del lado del cerro."""
    w = mw @ v.co
    if not v.link_faces:
        return False
    dx = sum((mw @ f.calc_center_median()).x for f in v.link_faces) / len(v.link_faces) - w.x
    return (dx > 0) == (abs(w.x - t_lo.x) < 1e-3)


frente = {v: lado_cerro(v) for v in bm.verts if en_frente(mw @ v.co)}

fijo, tapada, altura = {}, {}, {}
for v in bm.verts:
    w = mw @ v.co
    if not en_zona(w):
        continue
    orig = w.z
    cerro = frente.get(v, False)
    cerca = None if cerro else kd.find_range(Vector((w.x, w.y, 0)), DENTRO)
    if cerro:
        nuevo = -1e9  # lo define la tapada
        fijo[v] = True
    elif cerca:
        nuevo = min(puntos[i].z for _, i, _ in cerca) - BAJO_LOSA
        fijo[v] = True
    else:
        _, i, d = kd.find(Vector((w.x, w.y, 0)))
        base = puntos[i].z - BAJO_LOSA
        ancho = max(TALUD_MIN, min(TALUD_MAX, abs(orig - base) * TALUD))
        nuevo = base + (orig - base) * smoothstep((d - DENTRO) / ancho)

    # El túnel sigue enterrado: tapada encima y talud de 45° a los lados. En
    # las bocas la tierra queda al ras del techo para que la tape el túnel.
    if t_lo.x - EPS <= w.x <= t_hi.x + EPS and (cerro or v not in frente):
        lateral = max(0.0, abs(w.y - t_eje_y) - t_radio)
        boca = min(w.x - t_lo.x, t_hi.x - w.x) < 1.0
        tapada[v] = t_hi.z + (0.0 if boca else TAPADA_TUNEL) - lateral
        nuevo = max(nuevo, tapada[v])
    altura[v] = nuevo

# Suavizado de los taludes (los pisos quedan fijos): saca los picos que deja
# el cambio de un piso a otro.
libres = [v for v in altura if v not in fijo]
vecinos = {v: [e.other_vert(v) for e in v.link_edges if e.other_vert(v) in altura] for v in libres}
for _ in range(SUAVIZADO):
    siguiente = {}
    for v in libres:
        if vecinos[v]:
            media = sum(altura[n] for n in vecinos[v]) / len(vecinos[v])
            siguiente[v] = altura[v] + 0.5 * (media - altura[v])
    for v, h in siguiente.items():
        altura[v] = max(h, tapada.get(v, h))

for v, h in altura.items():
    w = mw @ v.co
    w.z = h
    v.co = mi @ w

# Frente de cada boca: une los dos lados del corte, salvo donde está el túnel.
def subir(v, z):
    w = mw @ v.co
    w.z = max(w.z, z)
    v.co = mi @ w


def altura_arco(x, y):
    """Altura del techo exterior del túnel en (x, y), justo adentro de la boca."""
    tmi = tunel.matrix_world.inverted()
    adentro = 0.02 if x == t_lo.x else -0.02
    ok, loc, _, _ = tunel.ray_cast(tmi @ Vector((x + adentro, y, t_hi.z + 5)),
                                   (tmi.to_3x3() @ Vector((0, 0, -1))).normalized())
    return (tunel.matrix_world @ loc).z if ok else t_hi.z


creadas = 0
for x in bocas:
    pares = {}
    for v, cerro in frente.items():
        if abs((mw @ v.co).x - x) < 1e-3:
            pares.setdefault(round((mw @ v.co).y, 3), [None, None])[0 if cerro else 1] = v
    ys = sorted(y for y, (arriba, abajo) in pares.items() if arriba and abajo)
    afuera = -1.0 if x == t_lo.x else 1.0
    for y0, y1 in zip(ys, ys[1:]):
        (a0, b0), (a1, b1) = pares[y0], pares[y1]
        subir(a0, (mw @ b0.co).z)  # el lado del cerro nunca queda por debajo del de afuera
        subir(a1, (mw @ b1.co).z)
        if t_lo.y - EPS < (y0 + y1) / 2 < t_hi.y + EPS:
            # Sobre el túnel el frente baja sólo hasta el arco.
            b0, b1 = (bm.verts.new(mi @ Vector((x, y, altura_arco(x, y)))) for y in (y0, y1))
        cara = bm.faces.new((a0, a1, b1, b0))
        cara.normal_update()
        if (mw.to_3x3() @ cara.normal).x * afuera < 0:
            cara.normal_flip()
        creadas += 1

bm.to_mesh(ter.data)
bm.free()
ter.data.update()
print(f"Terreno adaptado; {creadas} caras en el frente de las bocas")

scene["recorrido_asentado"] = dz
bpy.ops.wm.save_mainfile(compress=True)

# --- 3. Exportar y vista previa -------------------------------------------


def export_collection(col, filename):
    mesh_objects = [o for o in col.objects if o.type == 'MESH']
    bpy.ops.object.select_all(action='DESELECT')
    for o in mesh_objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = mesh_objects[0]
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(EXPORT_DIR, filename),
        use_selection=True,
        object_types={'MESH'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',
        axis_up='Y',
        add_leaf_bones=False,
        bake_anim=False,
    )


export_collection(bpy.data.collections["MAPA_Terreno"], "SM_Mapa01_Terreno.fbx")
export_collection(bpy.data.collections["MAPA_Recorrido"], "SM_Mapa01_Recorrido.fbx")

scene.render.engine = 'BLENDER_WORKBENCH'
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_cavity = True
scene.render.resolution_x = 1600
scene.render.resolution_y = 1000
cam_data = bpy.data.cameras.new("PREVIEW_Camara")
cam_data.clip_end = 5000
cam = bpy.data.objects.new("PREVIEW_Camara", cam_data)
scene.collection.objects.link(cam)
cam.location = (190, -85, 75 + dz)
cam.rotation_euler = (Vector((70, 15, dz)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam
scene.render.filepath = PREVIEW_PATH
bpy.ops.render.render(write_still=True)
