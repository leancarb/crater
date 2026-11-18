"""
CRATER — segunda apertura (referencia: intro de Silo) armada SOLO con las piezas de cuatro kits:
CRATER_Kit_Crater, CRATER_Kit_Exterior, CRATER_Kit_Interior y CRATER_Kit_Terrenos_y_Encuentros.
(El cielo, las luces, el polvo y el eclipse son procedurales: no salen de otros .blend.)

  python -I crater_v2.py <plano> <carpeta_salida> [--frames 1,48] [--pct 50] [--samples 8]

Reutiliza las herramientas de crater_intro.py (camara, polvo, disolucion, materiales).
"""
import bpy, sys, os, math, random, importlib.util
from mathutils import Vector, Matrix, Quaternion

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location('crater_intro', os.path.join(HERE, 'crater_intro.py'))
ci = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(ci)
for _k, _v in vars(ci).items():
    if not _k.startswith('__') and _k not in ('main', 'SHOTS'):
        globals()[_k] = _v

KDIR = os.environ.get('CRATER_KITS_DIR', os.path.join(os.path.abspath(os.path.join(HERE, '..', '..')), 'Assets/Art/Blender'))
K_CRATER = os.path.join(KDIR, 'CRATER_Kit_Crater.blend')
K_EXT = os.path.join(KDIR, 'CRATER_Kit_Exterior.blend')
K_INT = os.path.join(KDIR, 'CRATER_Kit_Interior.blend')
K_TER = os.path.join(KDIR, 'CRATER_Kit_Terrenos_y_Encuentros.blend')

SOLC = (1.0, 0.45, 0.08)
LUNAC = (0.2, 0.5, 1.0)


# ------------------------------------------------------------------ piezas de los kits
_cache = {}


def kit_col(path, name, fresh=True):
    key = (path, name)
    if not fresh and key in _cache:
        return _cache[key]
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.collections = [name]
    c = dst.collections[0]
    _cache[key] = c
    return c


def bbox_of(objs):
    pts = [o.matrix_world @ Vector(k) for o in objs if o.type == 'MESH' for k in o.bound_box]
    mn = Vector([min(p[i] for p in pts) for i in range(3)]); mx = Vector([max(p[i] for p in pts) for i in range(3)])
    return mn, mx


def piece(path, name, at=(0, 0, 0), rot=(0, 0, 0), scale=1.0, pivot='base'):
    """copia real de una pieza: el punto `pivot` de su caja ('base' = centro de la base, 'center') queda en `at`"""
    c = kit_col(path, name)
    bpy.context.scene.collection.children.link(c)
    bpy.context.view_layer.update()
    objs = list(c.all_objects)
    mn, mx = bbox_of(objs)
    base = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z if pivot == 'base' else (mn.z + mx.z) / 2))
    piv = link(bpy.data.objects.new(name + '_piv', None))
    piv.location = at
    piv.rotation_euler = rot if isinstance(rot, (tuple, list)) else (0, 0, rot)
    piv.scale = (scale,) * 3 if not isinstance(scale, (tuple, list)) else scale
    for o in objs:
        if o.parent is None or o.parent not in objs:
            o.parent = piv
            o.matrix_parent_inverse = Matrix.Translation(-base)
    bpy.context.view_layer.update()
    return piv, {o.name.split('.')[0]: o for o in objs}


def inst(path, name, at, rot=0.0, scale=1.0):
    """instancia liviana de una pieza (para repetir cardones, rocas...)"""
    c = kit_col(path, name, fresh=False)
    if 'base_ok' not in c:
        mn, mx = bbox_of(list(c.all_objects))
        c.instance_offset = ((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z); c['base_ok'] = 1
    e = bpy.data.objects.new(name + '_i', None); e.instance_type = 'COLLECTION'; e.instance_collection = c
    e.location = at; e.rotation_euler = (0, 0, rot); e.scale = (scale,) * 3
    return link(e)


def kit_mat(path, name):
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.materials = [name]
    return dst.materials[0]


def glow(m, strength, col=None):
    p = principled(m)
    if p is None: return m
    c = col or tuple(p.inputs['Base Color'].default_value[:3])
    p.inputs['Emission Color'].default_value = (*c, 1); p.inputs['Emission Strength'].default_value = strength
    return p


def own_mats(o):
    """materiales propios para poder animarlos sin tocar otras piezas"""
    for i, s_ in enumerate(o.material_slots):
        if s_.material: o.material_slots[i].material = s_.material.copy()
    return [s_.material for s_ in o.material_slots if s_.material]


def mats_named(objs, prefix):
    out = []
    for o in objs:
        for s_ in o.material_slots:
            if s_.material and s_.material.name.split('.')[0] == prefix and s_.material not in out:
                out.append(s_.material)
    return out


def ground_z(objs, x, y, top=80.0):
    dg = bpy.context.evaluated_depsgraph_get()
    best = None
    for o in objs:
        if o.type != 'MESH': continue
        inv = o.matrix_world.inverted()
        ok, loc, n, i = o.evaluated_get(dg).ray_cast(inv @ Vector((x, y, top)), (inv.to_3x3() @ Vector((0, 0, -1))).normalized())
        if ok:
            z = (o.matrix_world @ loc).z
            best = z if best is None else max(best, z)
    return best if best is not None else 0.0


def eclipse(center, radius, f_total, f0=1, offset=1.6):
    """eclipse procedural: sol, luna que lo tapa y corona que aparece en la totalidad"""
    away = Vector(center).normalized()
    right = away.cross(Vector((0, 0, 1))).normalized(); up = right.cross(away).normalized()
    sol = sphere('Sol', Vector(center) + away * radius * 1.4, radius * 0.97, emit_mat('m_sol', (1.0, 0.8, 0.4), 30))
    luna = disc('Luna', Vector(center), radius, mat('m_luna', (0, 0, 0), 1.0), rot=away.to_track_quat('-Z', 'Y').to_euler())
    p0 = Vector(center) + (right * 1.25 + up * 0.45) * radius * offset
    key(luna, 'location', f0, p0); key(luna, 'location', f_total, Vector(center))
    # corona: anillo con caida radial, transparente por fuera
    m = bpy.data.materials.new('m_corona'); nt = m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    tc = nt.nodes.new('ShaderNodeTexCoord'); ln = nt.nodes.new('ShaderNodeVectorMath'); ln.operation = 'LENGTH'
    fr = nt.nodes.new('ShaderNodeMapRange'); fr.inputs['From Min'].default_value = 1.0; fr.inputs['From Max'].default_value = 3.0
    fr.inputs['To Min'].default_value = 1.0; fr.inputs['To Max'].default_value = 0.0
    pw = nt.nodes.new('ShaderNodeMath'); pw.operation = 'POWER'; pw.inputs[1].default_value = 3.0
    sep = nt.nodes.new('ShaderNodeSeparateXYZ'); at_ = nt.nodes.new('ShaderNodeMath'); at_.operation = 'ARCTAN2'
    sc_ = nt.nodes.new('ShaderNodeMath'); sc_.operation = 'MULTIPLY'; sc_.inputs[1].default_value = 7.0
    sn = nt.nodes.new('ShaderNodeMath'); sn.operation = 'SINE'
    rr = nt.nodes.new('ShaderNodeMapRange'); rr.inputs['From Min'].default_value = -1; rr.inputs['To Min'].default_value = 0.55
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    st = nt.nodes.new('ShaderNodeMath'); st.operation = 'MULTIPLY'; st.name = 'fuerza'
    st.inputs[1].default_value = 0.0
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value = (*SILVER, 1)
    tr = nt.nodes.new('ShaderNodeBsdfTransparent'); add = nt.nodes.new('ShaderNodeAddShader'); out = nt.nodes.new('ShaderNodeOutputMaterial')
    L = nt.links
    L.new(tc.outputs['Object'], ln.inputs[0]); L.new(ln.outputs['Value'], fr.inputs['Value']); L.new(fr.outputs[0], pw.inputs[0])
    L.new(tc.outputs['Object'], sep.inputs[0]); L.new(sep.outputs['Y'], at_.inputs[0]); L.new(sep.outputs['X'], at_.inputs[1])
    L.new(at_.outputs[0], sc_.inputs[0]); L.new(sc_.outputs[0], sn.inputs[0]); L.new(sn.outputs[0], rr.inputs['Value'])
    L.new(pw.outputs[0], mul.inputs[0]); L.new(rr.outputs[0], mul.inputs[1]); L.new(mul.outputs[0], st.inputs[0])
    L.new(st.outputs[0], em.inputs['Strength']); L.new(tr.outputs[0], add.inputs[0]); L.new(em.outputs[0], add.inputs[1]); L.new(add.outputs[0], out.inputs[0])
    corona = disc('Corona', Vector(center) + away * radius * 0.6, radius * 3.0, m, rot=away.to_track_quat('-Z', 'Y').to_euler())
    corona.scale = (1, 1, 1)
    # el disco tiene radio 3R: las coordenadas de objeto van de 0 a 3R; normalizamos con la escala del radio
    corona.data.transform(Matrix.Scale(1.0, 4)); corona.scale = (radius, radius, radius)
    corona.data.transform(Matrix.Scale(1.0 / radius, 4))
    key_input(st.inputs[1], 1, 0.0); key_input(st.inputs[1], f_total - 10, 0.0); key_input(st.inputs[1], f_total + 12, 5.0)
    return sol, luna, corona


def station(at=(0, 0, 0), rot=0.0):
    """estacion de observacion armada con los modulos E01-E10 del kit exterior"""
    root = link(bpy.data.objects.new('Estacion', None)); root.location = at; root.rotation_euler = (0, 0, rot)
    parts = []

    def put(name, loc, r=0.0):
        p, objs = piece(K_EXT, name, loc, r)
        p.parent = root; parts.append(p); return p, objs
    # frente (mira a -Y): ciego, ventana, vano, ventana
    put('E01_Pano_ciego_de_adobe_2_4_m', (-3.6, 0, 0))
    put('E02_Pano_con_ventana_2_4_m', (-1.2, 0, 0))
    put('E04_Vano_de_acceso', (1.35, 0, 0))
    put('E02_Pano_con_ventana_2_4_m', (3.9, 0, 0))
    for x in (-3.6, -1.2, 1.2, 3.6, 4.6):
        put('E01_Pano_ciego_de_adobe_2_4_m', (x, 4.8, 0), math.pi)
    for y in (1.45, 3.85):
        put('E01_Pano_ciego_de_adobe_2_4_m', (-5.05, y, 0), math.pi / 2)
        put('E01_Pano_ciego_de_adobe_2_4_m', (5.35, y, 0), -math.pi / 2)
    for y in (1.2, 3.6):
        put('E08_Cubierta_inclinada_9_5_2_4', (0.15, y, 4.15))
    put('E09_Alero_4_2_2_8', (1.35, -1.55, 3.85))
    put('E10_Poste_de_alero', (-0.55, -2.75, 0)); put('E10_Poste_de_alero', (3.25, -2.75, 0))
    put('E06_Hoja_de_madera', (0.6, -0.15, 0), -0.5)
    vel, vo = put('H05_Veleta', (-3.2, 3.0, 5.05))
    dial, do = put('H04_Dial_de_observacion', (-7.2, -3.4, 0))
    put('H01_Mesa_de_instrumental', (4.0, -3.2, 0), 0.2)
    put('H06_Luz_de_patio', (6.6, -1.6, 0))
    put('H02_Baul_de_estacion', (-4.2, -1.3, 0), 0.1)
    return root, vel, dial


def scatter_valle(seed, area, avoid, n_cardon=26, n_roca=14, n_mata=60, ground=None):
    rnd = random.Random(seed)
    cardones = [f'V0{i}_Cardon_0{i}' for i in range(1, 10)]
    rocas = ['R01_Roca_de_valle_01', 'R02_Roca_de_valle_02', 'R03_Roca_de_valle_03', 'R06_Roca_de_valle_06', 'R07_Roca_de_valle_07', 'R09_Roca_de_valle_09', 'R05_Roca_de_valle_05']
    matas = ['V10_Mata_de_puna', 'V11_Paja_de_borde', 'V12_Mata_rala']
    out = []
    (x0, y0), (x1, y1) = area

    def free(x, y):
        return all((x - ax) ** 2 + (y - ay) ** 2 > r * r for ax, ay, r in avoid)
    for names, n, smin, smax in [(cardones, n_cardon, 0.8, 1.35), (rocas, n_roca, 0.6, 1.6), (matas, n_mata, 0.8, 1.6)]:
        k = 0; tries = 0
        while k < n and tries < n * 30:
            tries += 1
            x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
            if not free(x, y): continue
            z = ground_z(ground, x, y) if ground else 0.0
            out.append(inst(K_EXT, rnd.choice(names), (x, y, z - 0.05), rnd.uniform(0, 6.28), rnd.uniform(smin, smax))); k += 1
    return out


def valley_ground():
    """suelo del valle (Exterior_Terreno) y lomas de cierre (Exterior_Lomas_Valle) del kit de terrenos"""
    g, go = piece(K_TER, 'Exterior_Terreno', (0, 0, 0))
    l1, lo = piece(K_TER, 'Exterior_Lomas_Valle', (0, 95, -0.3))
    l2, lo2 = piece(K_TER, 'Exterior_Lomas_Valle', (-70, -10, -0.3), math.pi / 2)
    l3, lo3 = piece(K_TER, 'Exterior_Lomas_Valle', (72, 10, -0.3), -math.pi / 2)
    return g, [o for o in go.values() if o.type == 'MESH']


# ------------------------------------------------------------------ planos
def v1_valle(sc):
    """La estacion de observacion en el valle; la luna tapa el sol y cae la penumbra del eclipse."""
    sc.frame_start, sc.frame_end = 1, 192
    bg = world_gradient([(0.0, (0.05, 0.035, 0.02)), (0.5, (0.36, 0.22, 0.1)), (0.57, (0.18, 0.12, 0.06)), (0.8, (0.05, 0.04, 0.03)), (1.0, (0.012, 0.012, 0.016))], 1.0)
    key_input(bg.inputs['Strength'], 1, 1.0); key_input(bg.inputs['Strength'], 120, 0.25)
    g, gobjs = valley_ground()
    root, vel, dial = station((0, 6, 0))
    key(vel, 'rotation_euler', 1, (0, 0, 0), 'LINEAR'); key(vel, 'rotation_euler', 192, (0, 0, math.radians(260)), 'LINEAR')
    scatter_valle(4, ((-40, -30), (40, 45)), [(0, 8, 10), (-1, -14, 9)], ground=gobjs)
    E = Vector((10, 330, 150))
    eclipse(E, 13, 120)
    sol = light('SUN', 'Sol', (0, 0, 0), 3.2, (1.0, 0.66, 0.35), 1.5, rot=sun_rot(-E.normalized() * Vector((1, 1, 1))))
    sol.data.keyframe_insert('energy', frame=1); sol.data.energy = 0.15; sol.data.keyframe_insert('energy', frame=120)
    pl = light('SUN', 'Plata', (0, 0, 0), 0.0, SILVER, 6, rot=sun_rot(-E.normalized()))
    pl.data.keyframe_insert('energy', frame=110); pl.data.energy = 0.5; pl.data.keyframe_insert('energy', frame=140)
    for i, h in enumerate([90, 105]):
        cl = plane(f'Nube{i}', (0, 0, h), 2400, cloud_mat(f'm_nube{i}', (0.42 - i * 0.08, 0.32 - i * 0.06, 0.13), 0.5, 0.012, 0.55 + 0.03 * i, i + 3))
        e = cl.data.materials[0].node_tree.nodes.get('Emission')
        key_input(e.inputs['Strength'], 1, 0.5 - i * 0.1); key_input(e.inputs['Strength'], 120, 0.12)
    cam = camera(30)
    cam_path(cam, [
        (1,   (9.5, -16, 1.4), (2, 40, 14), 0, 30),
        (110, (6.5, -11.5, 1.9), (4, 60, 26), 0, 32),
        (192, (4.5, -9.0, 2.6), (E.x, E.y, E.z - 30), 0, 34)])
    sc.view_settings.exposure = 0.2


def v2_crater(sc):
    """Picada cenital al embudo del crater; los taludes se deshacen en polvo dorado y la camara cae por el agujero."""
    sc.frame_start, sc.frame_end = 1, 168
    world_gradient([(0.0, (0.02, 0.016, 0.012)), (0.5, (0.1, 0.06, 0.03)), (1.0, (0.01, 0.01, 0.014))], 0.4)
    g, go = piece(K_TER, 'Exterior_Terreno', (0, 0, 0))
    gm = [o for o in go.values() if o.type == 'MESH'][0]
    import bmesh      # abrir el hueco del crater en el suelo: se borran las caras cercanas al centro
    bpy.context.view_layer.update()
    bm = bmesh.new(); bm.from_mesh(gm.data); mw = gm.matrix_world
    dead = [f for f in bm.faces if ((mw @ f.calc_center_median()).xy).length < 13.2]
    bmesh.ops.delete(bm, geom=dead, context='FACES'); bm.to_mesh(gm.data); bm.free()
    ring, ro = piece(K_CRATER, 'Crater_Taludes_y_Bermas_Adaptados', (0, 0, -10.45))
    fondo, fo = piece(K_CRATER, 'Crater_Fondo', (0, 0, -9.25), 0, 0.52)
    for nm, x, c in [('Crater_Monolito_Sol', 4.2, SOLC), ('Crater_Monolito_Luna', -4.2, LUNAC)]:
        p, po = piece(K_CRATER, nm, (x, -19.5, 0), math.pi)
        for o in po.values():
            for m in mats_named([o], 'V1_M_Amber_Emission') + mats_named([o], 'V1_M_Luna_Emission'):
                glow(m, 6.0)
    for m in mats_named(list(fo.values()), 'V1_M_Eclipse_Plata'):
        glow(m, 2.5, SILVER)
    scatter_valle(9, ((-45, -45), (45, 45)), [(0, 0, 19.5)], n_cardon=18, n_roca=10, n_mata=40)
    taludes = [o for n, o in ro.items() if o.type == 'MESH' and n.startswith('Crater_Talud')]
    w = merged(taludes, 'TaludesPolvo')
    for o in taludes: o.hide_render = True
    dissolve(w, 40, 150, 0.85, (1.0, 0.5, 0.12), 3, nscale=0.4, edge_w=0.025)
    dust(w, 'PolvoTalud', 6.0, 0.03, GOLD, 12, 40, 150, direction=(0, 0, 1), dist=14, swirl=5, spread=0.85, appear=True, seed=5,
         nscale=0.4, inward=((0.0, 0.0), 1.5))
    light('SUN', 'Penumbra', (0, 0, 0), 0.6, (1.0, 0.6, 0.32), 2, rot=sun_rot((0.3, 0.5, -0.8)))
    light('POINT', 'Abajo', (0, 0, -7.0), 220, GOLD, 2)
    cam = camera(28)
    cam_path(cam, [
        (1,   (0, 0, 46), None, 0, 28),
        (70,  (0, 0.4, 26), None, 45, 26),
        (130, (0, 0.6, 6), None, 110, 22),
        (168, (0, 0.6, -6.5), None, 160, 20)])
    sc.view_settings.exposure = 0.0


def v3_murales(sc):
    """Travelling lateral por los cuadros de historia; la luz del farol pasa y el polvo dorado flota."""
    sc.frame_start, sc.frame_end = 1, 168
    world_color((0.004, 0.003, 0.003), 1)
    wallm = kit_mat(K_INT, 'BasaltoMedio')
    plane('Muro', (9, 0.06, 2), 60, wallm, rot=(math.radians(90), 0, 0))
    plane('PisoM', (9, -6, -0.1), 60, kit_mat(K_INT, 'Suelo'))
    xs = []
    for i, h in enumerate(['Historia_02', 'Historia_03', 'Historia_04', 'Historia_05']):
        p, po = piece(K_INT, h, (i * 6.0, 0, 0.9), 0, 1.0, pivot='base')
        xs.append(i * 6.0)
    farol = light('POINT', 'Farol', (0, -1.6, 2.3), 230, (1.0, 0.62, 0.3), 0.15)
    key(farol, 'location', 1, (-0.5, -1.7, 2.2)); key(farol, 'location', 168, (21.5, -1.7, 2.2))
    light('AREA', 'Relleno', (9, -9, 4), 40, (1.0, 0.5, 0.2), 14, rot=(math.radians(70), 0, 0))
    import bmesh
    me = bpy.data.meshes.new('aire')
    me.from_pydata([(-13, 0, -1.7), (13, 0, -1.7), (13, 0, 1.7), (-13, 0, 1.7)], [], [(0, 1, 2, 3)]); me.update()
    aire = link(bpy.data.objects.new('Aire', me)); aire.location = (9, -1.4, 2.2); aire.hide_render = True
    dust(aire, 'Motas', 9.0, 0.009, GOLD, 4, 1, 400, direction=(0.3, -0.4, 1), dist=0.8, swirl=1.6, spread=0.0, seed=2)
    cam = camera(35)
    cam_path(cam, [
        (1,   (-1.0, -4.4, 1.9), (0.6, 0, 1.9), 0, 35),
        (168, (19.0, -4.0, 2.0), (20.2, 0, 1.9), 0, 35)])
    sc.view_settings.exposure = 0.6


def v4_rotonda(sc):
    """La Rotonda a oscuras: solo el farol sobre el pedestal; al acercarnos se encienden las columnas."""
    sc.frame_start, sc.frame_end = 1, 192
    world_color((0.003, 0.003, 0.005), 1)
    rot_, ro = piece(K_INT, 'Rotonda_Arquitectura', (0, 0, 0))
    objs = [o for o in ro.values() if o.type == 'MESH']
    zf = ground_z(objs, 0.0, 0.0, 6.0)
    ped, po = piece(K_INT, 'Pedestal_Lobby', (0, 0, zf))
    top = zf + 1.4
    far, fo = piece(K_INT, 'Farol_CRATER', (0, 0, top), 0, 1.8)
    for o in fo.values(): o.visible_shadow = False
    for m in mats_named(list(fo.values()), 'Farol_Difusor'): glow(m, 18, (1.0, 0.78, 0.45))
    fl = light('POINT', 'LuzFarol', (0, 0, top + 0.5), 260, (1.0, 0.74, 0.42), 0.12)
    cols = []
    for k, (nm, ring_nm, x, y, c) in enumerate([('Columna_Sol_Lobby', 'Aro_Base_Columna_Ambar', -8.0, -3.0, SOLC), ('Columna_Sol_Lobby', 'Aro_Base_Columna_Ambar', -8.0, 3.5, SOLC),
                                                 ('Columna_Luna_Lobby', 'Aro_Base_Columna_TalladoLuna', 8.0, -3.0, LUNAC), ('Columna_Luna_Lobby', 'Aro_Base_Columna_TalladoLuna', 8.0, 3.5, LUNAC)]):
        zc = ground_z(objs, x, y, 6.0)
        piece(K_INT, nm, (x, y, zc))
        rp, rpo = piece(K_INT, ring_nm, (x, y, zc + 0.02), 0, 1.25)
        f = 96 + k * 14
        for o in rpo.values():
            if o.type != 'MESH': continue
            for m in own_mats(o):
                p = principled(m); p.inputs['Emission Color'].default_value = (*c, 1)
                key_input(p.inputs['Emission Strength'], 1, 0.0); key_input(p.inputs['Emission Strength'], f, 0.0); key_input(p.inputs['Emission Strength'], f + 8, 9.0)
        lc = light('POINT', f'Col{k}', (x, y, zc + 0.6), 0, c, 0.6)
        lc.data.keyframe_insert('energy', frame=f); lc.data.energy = 260; lc.data.keyframe_insert('energy', frame=f + 10)
    # sellos de los arcos: se encienden al final
    for m in mats_named(objs, 'Sol') + mats_named(objs, 'Luna'):
        p = principled(m); p.inputs['Emission Color'].default_value = p.inputs['Base Color'].default_value
        key_input(p.inputs['Emission Strength'], 1, 0.0); key_input(p.inputs['Emission Strength'], 150, 0.0); key_input(p.inputs['Emission Strength'], 180, 3.0)
    ocu = light('AREA', 'Oculo', (0, 0, 14), 900, SILVER, 8, rot=(0, 0, 0))
    cam = camera(30)
    cam_path(cam, [
        (1,   (0.4, -15.5, top + 0.6), (0, 0, top + 0.3), 0, 30),
        (130, (0.2, -4.6, top + 0.4), (0, 0, top + 0.35), 0, 42),
        (192, (2.8, -9.5, top + 2.2), (0, 0, top + 0.3), 0, 32)])
    sc.view_settings.exposure = 0.8


def v5_puente(sc):
    """SOL: el haz ambar enciende las dos anclas y el puente de luz se arma panel por panel sobre el abismo."""
    sc.frame_start, sc.frame_end = 1, 192
    world_color((0.004, 0.003, 0.003), 1)
    gap = 8.0
    for y, r in [(-gap / 2 - 3.0, 0), (gap / 2 + 3.0, math.pi)]:
        piece(K_INT, 'Pared_Sol_Sol', (0, y, -0.3), (math.radians(90), 0, r), pivot='center')
    for x in (-5.5,):
        for y in (-7.3, 0.0, 7.3):
            piece(K_INT, 'Pared_Sol_0', (x, y, -14), (0, 0, math.pi / 2 if x < 0 else -math.pi / 2), (1.0, 1.0, 3.0))
    pa, ao = piece(K_INT, 'Ancla_Basalto', (-2.6, -gap / 2 - 1.2, 0), 0)
    pb, bo = piece(K_INT, 'Ancla_Basalto', (2.6, gap / 2 + 1.2, 0), math.pi)

    def ignite(objs, f):
        rings = [o for n, o in objs.items() if o.type == 'MESH' and ('Ring' in n or 'Core' in n)]
        for j, o in enumerate(rings):
            for m in own_mats(o):
                p = principled(m)
                key_input(p.inputs['Emission Strength'], 1, 0.0); key_input(p.inputs['Emission Strength'], f + j * 5, 0.0); key_input(p.inputs['Emission Strength'], f + j * 5 + 8, 7.0)
            s = o.scale.copy(); key(o, 'scale', 1, s * 0.3); key(o, 'scale', f + j * 5, s * 0.3); key(o, 'scale', f + j * 5 + 10, s)
    ignite(ao, 34); ignite(bo, 74)
    for (px, py), f in [((-2.6, -gap / 2 - 0.4), 34), ((2.6, gap / 2 + 0.4), 74)]:
        la = light('POINT', 'LA', (px, py, 1.5), 0, SOLC, 0.3); la.data.keyframe_insert('energy', frame=f); la.data.energy = 120; la.data.keyframe_insert('energy', frame=f + 14)
    pp, po = piece(K_INT, 'Puente_Cuerpo', (0, 0, 0.0), math.pi / 2 * 0, (1.0, gap / 8.0, 1.0), pivot='base')
    panels = sorted([o for n, o in po.items() if n.startswith('BridgePanel')], key=lambda o: o.name)
    facets = sorted([o for n, o in po.items() if n.startswith('BridgeFacet')], key=lambda o: o.name)
    order = list(zip(panels, facets))
    order.sort(key=lambda pf: bbox_center(pf[0]).y)
    for i, (pn, fc) in enumerate(order):
        f = 96 + i * 8
        for o in (pn, fc):
            s = o.scale.copy(); key(o, 'scale', 1, (s.x, 0.001, s.z)); key(o, 'scale', f, (s.x, 0.001, s.z)); key(o, 'scale', f + 8, s)
    for n, o in po.items():
        if n.startswith('Bridge_Edge'):
            s = o.scale.copy(); key(o, 'scale', 1, (s.x, 0.001, s.z)); key(o, 'scale', 96, (s.x, 0.001, s.z)); key(o, 'scale', 146, s)
    for m in mats_named(list(po.values()), 'PuenteVidrio') + mats_named(list(po.values()), 'PuenteBorde'):
        p = principled(m); key_input(p.inputs['Emission Strength'], 90, 0.0); key_input(p.inputs['Emission Strength'], 110, 0.7)
    lb = light('AREA', 'Puente', (0, 0, 2.5), 0, SOLC, 3, rot=(0, 0, 0))
    lb.data.keyframe_insert('energy', frame=96); lb.data.energy = 45; lb.data.keyframe_insert('energy', frame=150)
    H = (9.0, -7.5, 2.6)
    haz = light('SPOT', 'Haz', H, 2200, (1.0, 0.52, 0.12), 0.05, spot=9)
    key(haz, 'rotation_euler', 1, aim(H, (-4, -gap / 2 - 2.5, 1.0)).to_euler())
    key(haz, 'rotation_euler', 36, aim(H, (-2.6, -gap / 2 - 0.6, 1.4)).to_euler())
    key(haz, 'rotation_euler', 60, aim(H, (-2.6, -gap / 2 - 0.6, 1.4)).to_euler())
    key(haz, 'rotation_euler', 78, aim(H, (2.6, gap / 2 + 0.6, 1.4)).to_euler())
    key(haz, 'rotation_euler', 192, aim(H, (2.6, gap / 2 + 0.6, 1.4)).to_euler())
    light('POINT', 'Gruta', (0, 0, -18), 1400, (1.0, 0.1, 0.03), 4)
    light('AREA', 'Ambiente', (2, 0, 9), 60, (1.0, 0.6, 0.35), 12, rot=(0, 0, 0))
    cam = camera(24)
    cam_path(cam, [
        (1,   (9.5, -4.5, 1.4), (-0.5, -1.5, 0.4), 0, 26),
        (100, (9.0, -1.0, 1.9), (-0.5, 0, 0.2), 0, 24),
        (192, (7.8, 2.5, 3.6), (-0.5, 0.5, -0.6), 0, 22)])
    sc.view_settings.exposure = 0.6


def v6_reja(sc):
    """LUNA: el haz azul disuelve la placa de la reja; la camara la atraviesa."""
    sc.frame_start, sc.frame_end = 1, 144
    world_color((0.002, 0.004, 0.008), 1)
    rj, ro = piece(K_INT, 'Reja_Luna', (0, 0, 0))
    placa = [o for n, o in ro.items() if n.startswith('Reja_Luna_Placa')][0]
    for x in (-7.2, 7.2):
        piece(K_INT, 'Pared_Luna_Luna', (x, 0.3, 0))
    for x in (-3.9, 3.9):
        for y in (-3.8, 4.4):
            piece(K_INT, 'Pared_Luna_129', (x, y, 0), (0, 0, math.pi / 2), (1, 1, 1.15))
    piece(K_INT, 'Arco', (0, 9.0, 0))
    floor = plane('PisoR', (0, 0, 0), 60, kit_mat(K_INT, 'Suelo'))
    w = merged([placa], 'PlacaPolvo'); placa.hide_render = True
    m = w.material_slots[0].material; glow(m, 0.35, (0.09, 0.23, 0.88))
    dissolve(w, 40, 110, 0.85, (0.35, 0.65, 1.0), 6, nscale=0.9, edge_w=0.03)
    dust(w, 'PolvoLuna', 260, 0.012, (0.3, 0.6, 1.0), 6, 40, 110, direction=(0, 1, 0.4), dist=3.5, swirl=2.6, spread=0.85, appear=True, seed=7, nscale=0.9, shrink=0.9)
    haz = light('SPOT', 'HazLuna', (0.6, -9, 2.0), 420, LUNAC, 0.05, spot=24, rot=aim((0.6, -9, 2.0), (0, 0, 2.6)).to_euler())
    light('POINT', 'Mas_alla', (0, 7.0, 2.5), 400, (0.5, 0.75, 1.0), 1.0)
    light('AREA', 'Teal', (0, -3, 7), 160, TEAL, 6, rot=(0, 0, 0))
    cam = camera(28)
    cam_path(cam, [
        (1,   (1.6, -9.5, 1.7), (0, 0, 2.8), 0, 28),
        (100, (0.5, -4.2, 1.9), (0, 2, 2.6), 0, 28),
        (144, (0.0, 3.5, 2.0), (0, 12, 2.4), 0, 26)])
    sc.view_settings.exposure = 0.6


def v7_cresta(sc):
    """La Cresta: sala circular de panos SOL/LUNA. El farol se apaga, se encienden los tallados y el oculo se abre en luz blanca."""
    sc.frame_start, sc.frame_end = 1, 168
    world_color((0.0, 0.0, 0.0), 0)
    R = 8.9; N = 16
    pano_objs = []
    for i in range(N):
        a = 2 * math.pi * i / N
        nm = 'Cresta_Pano_Sol' if i % 2 == 0 else 'Cresta_Pano_Luna'
        p, po = piece(K_INT, nm, (R * math.cos(a), R * math.sin(a), 0), a + math.pi / 2)
        pano_objs += [o for o in po.values() if o.type == 'MESH']
    piso, pso = piece(K_INT, 'Aro_Piso', (0, 0, 0.01), 0, 0.66)
    plane('PisoC', (0, 0, 0), 30, kit_mat(K_INT, 'Suelo'))
    techo, to = piece(K_INT, 'Aro_Piso', (0, 0, 8.0), 0, 0.72)
    for o in to.values():
        if o.type == 'MESH': o.modifiers.new('sol', 'SOLIDIFY').thickness = 0.3
    hojas = []
    for s_ in (-1, 1):
        h, ho = piece(K_INT, 'Hoja_Piedra_Modular', (s_ * 1.7, 0, 8.0), (math.radians(90), 0, 0), (1.22, 1.0, 1.9), pivot='center')
        hojas.append(h)
        key(h, 'location', 1, (s_ * 1.7, 0, 8.0)); key(h, 'location', 112, (s_ * 1.7, 0, 8.0)); key(h, 'location', 150, (s_ * 6.8, 0, 8.0))
    # los tallados latentes sobre los panos: brillan cuando se apaga el farol
    for m in mats_named(pano_objs, 'Sol') + mats_named(pano_objs, 'Luna'):
        p = principled(m); p.inputs['Emission Color'].default_value = p.inputs['Base Color'].default_value
        key_input(p.inputs['Emission Strength'], 1, 0.0); key_input(p.inputs['Emission Strength'], 62, 0.0); key_input(p.inputs['Emission Strength'], 104, 6.0)
    far, fo = piece(K_INT, 'Farol_CRATER', (0, 0, 0.05), 0, 1.8)
    for o in fo.values(): o.visible_shadow = False
    dif = mats_named(list(fo.values()), 'Farol_Difusor')
    for m in dif:
        p = glow(m, 18, (1.0, 0.78, 0.45)); key_input(p.inputs['Emission Strength'], 50, 18); key_input(p.inputs['Emission Strength'], 60, 0.0)
    fl = light('POINT', 'Farol', (0, 0, 0.5), 600, (1.0, 0.8, 0.55), 0.1)
    fl.data.keyframe_insert('energy', frame=50); fl.data.energy = 0; fl.data.keyframe_insert('energy', frame=60)
    luz = light('AREA', 'FinDelEclipse', (0, 0, 9.5), 0, (1.0, 0.97, 0.92), 3.5, rot=(0, 0, 0))
    luz.data.keyframe_insert('energy', frame=112); luz.data.energy = 9000; luz.data.keyframe_insert('energy', frame=156)
    blanco = disc('Blanco', (0, 0, 12), 5, emit_mat('m_blanco', (1.0, 0.97, 0.92), 0.0))
    be = blanco.data.materials[0].node_tree.nodes.get('Emission')
    key_input(be.inputs['Strength'], 112, 0.0); key_input(be.inputs['Strength'], 150, 40.0)
    cam = camera(20)
    cam_path(cam, [
        (1,   (0.0, -4.2, 1.5), (0, 6, 2.8), 0, 20),
        (100, (0.0, -3.0, 1.4), (-3, 6, 3.6), 0, 20),
        (168, (0.0, -0.8, 1.3), (0, 0.4, 12), 0, 18)])
    sc.view_settings.exposure = 0.6


def v8_epilogo(sc):
    """Epilogo: la estacion al atardecer, sin crater. Queda el plano limpio para el titulo."""
    sc.frame_start, sc.frame_end = 1, 180
    world_gradient([(0.0, (0.06, 0.03, 0.02)), (0.5, (0.85, 0.42, 0.16)), (0.56, (0.45, 0.22, 0.12)), (0.75, (0.12, 0.08, 0.1)), (1.0, (0.03, 0.03, 0.06))], 0.9)
    g, gobjs = valley_ground()
    root, vel, dial = station((0, 6, 0), math.radians(-12))
    key(vel, 'rotation_euler', 1, (0, 0, 0), 'LINEAR'); key(vel, 'rotation_euler', 180, (0, 0, math.radians(-60)), 'LINEAR')
    scatter_valle(21, ((-40, -30), (40, 45)), [(0, 8, 10)], ground=gobjs)
    sun_d = Vector((-0.55, 0.8, -0.08))
    light('SUN', 'Ocaso', (0, 0, 0), 3.6, (1.0, 0.5, 0.22), 1.0, rot=sun_rot(sun_d))
    sphere('SolBajo', -sun_d.normalized() * 900 + Vector((0, 0, 20)), 26, emit_mat('m_ocaso', (1.0, 0.55, 0.2), 12))
    light('SUN', 'Cielo', (0, 0, 0), 0.4, (0.5, 0.55, 0.8), 6, rot=sun_rot((0.3, -0.4, -0.85)))
    cam = camera(32)
    cam_path(cam, [
        (1,   (-16, -24, 3.0), (-2, 8, 4.5), 0, 32),
        (180, (-12.5, -19.5, 3.4), (-2, 8, 5.5), 0, 33)])
    sc.view_settings.exposure = 0.0


SHOTS2 = {'v1': v1_valle, 'v2': v2_crater, 'v3': v3_murales, 'v4': v4_rotonda,
          'v5': v5_puente, 'v6': v6_reja, 'v7': v7_cresta, 'v8': v8_epilogo}


def main2():
    a = sys.argv[1:]
    shot, out = a[0], a[1]
    frames = None; pct = 100; samples = 8
    if '--frames' in a: frames = [int(x) for x in a[a.index('--frames') + 1].split(',')]
    if '--pct' in a: pct = int(a[a.index('--pct') + 1])
    if '--samples' in a: samples = int(a[a.index('--samples') + 1])
    sc = reset(samples, pct)
    SHOTS2[shot](sc)
    d = os.path.join(out, shot); os.makedirs(d, exist_ok=True)
    if frames:
        for f in frames:
            sc.frame_set(f); sc.render.filepath = os.path.join(d, f'{f:04d}.png')
            bpy.ops.render.render(write_still=True, scene=sc.name)
    else:
        sc.render.filepath = os.path.join(d, '####')
        sc.render.use_overwrite = False; sc.render.use_placeholder = True
        bpy.ops.render.render(animation=True, scene=sc.name)


if __name__ == '__main__':
    main2()
