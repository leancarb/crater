"""
CRATER — secuencia de apertura al estilo de la intro de Silo, armada en Blender 5.2
con las escenas de exterior (CRATER_Kit_Exterior_1, CRATER_Kit_Modular_Exterior),
el kit modular y el mapa del proyecto (Assets/Art/Blender).

Uso:
  python -I crater_intro.py <plano> <carpeta_salida> [--frames 1,48,96] [--pct 50] [--samples 16]

Cada plano construye su propia escena desde cero, anima la camara y renderiza
una secuencia PNG en <carpeta_salida>/<plano>/####.png (Cycles en CPU).
"""
import bpy, sys, os, math
from mathutils import Vector, Euler, Quaternion, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
# kit y mapa del repo; los dos .blend de exterior se buscan en CRATER_EXTERIOR_DIR (por defecto, la misma carpeta)
ART = os.environ.get('CRATER_BLEND_DIR', os.path.join(REPO, 'Assets/Art/Blender'))
EXT_DIR = os.environ.get('CRATER_EXTERIOR_DIR', ART)
KIT = os.path.join(ART, 'CRATER_Kit_Modular.blend')
MAPA = os.path.join(ART, 'CRATER_Mapa_01.blend')
EXT1 = os.path.join(EXT_DIR, 'CRATER_Kit_Exterior_1.blend')
EXTM = os.path.join(EXT_DIR, 'CRATER_Kit_Modular_Exterior.blend')
FONTS = os.environ.get('CRATER_FONTS', os.path.join(HERE, 'fonts'))
FPS = 24

AMBER = (1.0, 0.42, 0.06)
GOLD = (1.0, 0.62, 0.22)
SILVER = (0.78, 0.84, 1.0)
TEAL = (0.12, 0.5, 0.62)
HUECO = (0.08, 0.22, 1.0)


# ------------------------------------------------------------------ escena base
def reset(samples, pct):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    return render_settings(sc, samples, pct, new_world=True)


def render_settings(sc, samples, pct, new_world=False):
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    try: sc.cycles.denoiser = 'OPENIMAGEDENOISE'
    except Exception: pass
    sc.cycles.max_bounces = 4
    sc.cycles.transparent_max_bounces = 16
    sc.cycles.use_adaptive_sampling = True
    sc.render.use_persistent_data = True
    sc.render.resolution_x, sc.render.resolution_y = 960, 540
    sc.render.resolution_percentage = pct
    sc.render.fps = FPS
    sc.render.image_settings.file_format = 'PNG'
    sc.view_settings.view_transform = 'AgX'
    try: sc.view_settings.look = 'AgX - Medium High Contrast'
    except Exception: pass
    if new_world or not sc.world:
        sc.world = bpy.data.worlds.new('W')
    world_color((0.0, 0.0, 0.0), 0)
    return sc


def open_base(path, scene_name, samples, pct):
    """abre un .blend del proyecto y deja solo la escena pedida como base del plano"""
    bpy.ops.wm.open_mainfile(filepath=path, load_ui=False, use_scripts=False)
    sc = bpy.data.scenes[scene_name]
    if bpy.context.window: bpy.context.window.scene = sc
    for o in list(bpy.data.scenes):
        if o != sc: bpy.data.scenes.remove(o)
    render_settings(sc, samples, pct)
    return sc


def world_color(col, strength):
    w = bpy.context.scene.world
    try: w.use_nodes = True
    except Exception: pass
    nt = w.node_tree if w.node_tree else None
    if nt is None:
        w.use_nodes = True; nt = w.node_tree
    bg = nt.nodes.get('Background') or nt.nodes.new('ShaderNodeBackground')
    out = nt.nodes.get('World Output') or nt.nodes.new('ShaderNodeOutputWorld')
    bg.inputs['Color'].default_value = (*col, 1); bg.inputs['Strength'].default_value = strength
    nt.links.new(bg.outputs[0], out.inputs[0])
    return bg


def world_gradient(stops, strength=1.0):
    """cielo por altura: stops = [(pos 0..1 de abajo a arriba, color)]"""
    w = bpy.context.scene.world; nt = w.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = -1; mr.inputs['From Max'].default_value = 1
    ramp = nt.nodes.new('ShaderNodeValToRGB'); bg = nt.nodes.new('ShaderNodeBackground'); out = nt.nodes.new('ShaderNodeOutputWorld')
    els = ramp.color_ramp.elements
    els[0].position, els[0].color = stops[0][0], (*stops[0][1], 1)
    els[1].position, els[1].color = stops[-1][0], (*stops[-1][1], 1)
    for p, c in stops[1:-1]:
        e = els.new(p); e.color = (*c, 1)
    bg.inputs['Strength'].default_value = strength
    L = nt.links
    L.new(tc.outputs['Generated'], sep.inputs[0]); L.new(sep.outputs['Z'], mr.inputs['Value'])
    L.new(mr.outputs[0], ramp.inputs[0]); L.new(ramp.outputs[0], bg.inputs[0]); L.new(bg.outputs[0], out.inputs[0])
    return bg


# ------------------------------------------------------------------ materiales
def mat(name, base=(0.1, 0.1, 0.1), rough=0.8, metal=0.0, emit=None, strength=0.0, alpha=1.0):
    m = bpy.data.materials.new(name)
    try: m.use_nodes = True
    except Exception: pass
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*base, 1)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metal
    if emit:
        p.inputs['Emission Color'].default_value = (*emit, 1)
        p.inputs['Emission Strength'].default_value = strength
    p.inputs['Alpha'].default_value = alpha
    return m


def emit_mat(name, col, strength):
    m = bpy.data.materials.new(name)
    try: m.use_nodes = True
    except Exception: pass
    nt = m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    e = nt.nodes.new('ShaderNodeEmission'); o = nt.nodes.new('ShaderNodeOutputMaterial')
    e.inputs['Color'].default_value = (*col, 1); e.inputs['Strength'].default_value = strength
    nt.links.new(e.outputs[0], o.inputs[0])
    return m


def principled(m):
    return m.node_tree.nodes.get('Principled BSDF')


def key_input(sock, frame, value):
    sock.default_value = value
    sock.keyframe_insert('default_value', frame=frame)


def cloud_mat(name, col, strength, scale, cover, seed):
    """capa de nubes: ruido -> mascara de alfa, emision tenida"""
    m = bpy.data.materials.new(name)
    try: m.use_nodes = True
    except Exception: pass
    nt = m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    tc = nt.nodes.new('ShaderNodeTexCoord'); mp = nt.nodes.new('ShaderNodeMapping')
    mp.inputs['Location'].default_value = (seed * 13.1, seed * 7.7, seed * 3.3)
    nz = nt.nodes.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = scale; nz.inputs['Detail'].default_value = 8; nz.inputs['Roughness'].default_value = 0.62
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = cover; ramp.color_ramp.elements[0].color = (0, 0, 0, 1)
    ramp.color_ramp.elements[1].position = min(1, cover + 0.22); ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value = (*col, 1); em.inputs['Strength'].default_value = strength
    tr = nt.nodes.new('ShaderNodeBsdfTransparent'); mix = nt.nodes.new('ShaderNodeMixShader'); o = nt.nodes.new('ShaderNodeOutputMaterial')
    L = nt.links
    L.new(tc.outputs['Object'], mp.inputs['Vector']); L.new(mp.outputs[0], nz.inputs['Vector'])
    L.new(nz.outputs['Fac'], ramp.inputs[0]); L.new(ramp.outputs[0], mix.inputs[0])
    L.new(tr.outputs[0], mix.inputs[1]); L.new(em.outputs[0], mix.inputs[2]); L.new(mix.outputs[0], o.inputs[0])
    return m


# ------------------------------------------------------------------ objetos
def link(obj, sc=None):
    (sc or bpy.context.scene).collection.objects.link(obj); return obj


def append_collection(path, name):
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        dst.collections = [name]
    col = dst.collections[0]
    bpy.context.scene.collection.children.link(col)
    return col


def kit(name, offset=(0, 0, 0), rot_z=0.0, scale=1.0):
    """agrega una coleccion del kit como piezas propias, transformadas en bloque"""
    col = append_collection(KIT, name)
    root = bpy.data.objects.new(name + '_root', None); link(root)
    for o in list(col.objects):
        o.parent = root
    root.location = offset; root.rotation_euler = (0, 0, rot_z); root.scale = (scale,) * 3
    return root, {o.name.split('.')[0]: o for o in col.objects}


def box(name, loc, size, m):
    me = bpy.data.meshes.new(name)
    sx, sy, sz = [s / 2 for s in size]
    v = [(-sx, -sy, -sz), (sx, -sy, -sz), (sx, sy, -sz), (-sx, sy, -sz), (-sx, -sy, sz), (sx, -sy, sz), (sx, sy, sz), (-sx, sy, sz)]
    f = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    me.from_pydata(v, [], f); me.update()
    o = bpy.data.objects.new(name, me); o.location = loc; o.data.materials.append(m)
    return link(o)


def plane(name, loc, size, m, rot=(0, 0, 0)):
    me = bpy.data.meshes.new(name); s = size / 2
    me.from_pydata([(-s, -s, 0), (s, -s, 0), (s, s, 0), (-s, s, 0)], [], [(0, 1, 2, 3)]); me.update()
    o = bpy.data.objects.new(name, me); o.location = loc; o.rotation_euler = rot; o.data.materials.append(m)
    return link(o)


def sphere(name, loc, r, m, segs=48):
    import bmesh
    me = bpy.data.meshes.new(name); bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=segs // 2, radius=r)
    bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    o = bpy.data.objects.new(name, me); o.location = loc; o.data.materials.append(m)
    return link(o)


def disc(name, loc, r, m, rot=(0, 0, 0), segs=96):
    me = bpy.data.meshes.new(name)
    v = [(0, 0, 0)] + [(r * math.cos(i / segs * 2 * math.pi), r * math.sin(i / segs * 2 * math.pi), 0) for i in range(segs)]
    f = [(0, 1 + i, 1 + (i + 1) % segs) for i in range(segs)]
    me.from_pydata(v, [], f); me.update()
    o = bpy.data.objects.new(name, me); o.location = loc; o.rotation_euler = rot; o.data.materials.append(m)
    return link(o)


def light(kind, name, loc, energy, col=(1, 1, 1), size=1.0, rot=(0, 0, 0), spot=None):
    d = bpy.data.lights.new(name, kind); d.energy = energy; d.color = col
    if kind == 'AREA': d.size = size
    if kind in ('POINT', 'SPOT'): d.shadow_soft_size = size
    if kind == 'SUN': d.angle = math.radians(size)
    if kind == 'SPOT' and spot: d.spot_size = math.radians(spot); d.spot_blend = 0.35
    o = bpy.data.objects.new(name, d); o.location = loc; o.rotation_euler = rot
    return link(o)


def key(obj, path, frame, value, interp=None):
    setattr(obj, path, value)
    obj.keyframe_insert(path, frame=frame)
    if interp:
        ad = obj.animation_data
        for fc in getattr(ad.action, 'fcurves', []):
            for kp in fc.keyframe_points:
                if int(kp.co.x) == frame: kp.interpolation = interp


def hide_between(obj, f_show, f_hide=None):
    """visible en render solo entre f_show y f_hide"""
    obj.hide_render = True; obj.keyframe_insert('hide_render', frame=1)
    obj.hide_render = False; obj.keyframe_insert('hide_render', frame=f_show)
    if f_hide:
        obj.hide_render = True; obj.keyframe_insert('hide_render', frame=f_hide)


# ------------------------------------------------------------------ camara
def camera(lens=35):
    d = bpy.data.cameras.new('Cam'); d.lens = lens; d.clip_start = 0.05; d.clip_end = 20000
    o = link(bpy.data.objects.new('Cam', d)); bpy.context.scene.camera = o
    return o


def aim(loc, target, roll=0.0):
    d = Vector(target) - Vector(loc)
    q = d.to_track_quat('-Z', 'Y')
    if roll: q = q @ Quaternion((0, 0, 1), math.radians(roll))
    return q


def cam_path(cam, keys):
    """keys: [(frame, loc, target, roll_deg, lens)]; target None => mira recto hacia abajo"""
    prev = None
    for fr, loc, tgt, roll, lens in keys:
        cam.location = loc; cam.keyframe_insert('location', frame=fr)
        if tgt is None:
            q = Quaternion((0, 0, 1), math.radians(roll))
        else:
            q = aim(loc, tgt, roll)
        e = q.to_euler('XYZ', prev) if prev else q.to_euler('XYZ')
        prev = e
        cam.rotation_euler = e; cam.keyframe_insert('rotation_euler', frame=fr)
        if lens:
            cam.data.lens = lens; cam.data.keyframe_insert('lens', frame=fr)


# ------------------------------------------------------------------ polvo (Geometry Nodes)
def dust(src, name, density, radius, col, strength, f0, f1, direction=(0, 0, 1), dist=3.0, swirl=2.0,
         spread=0.6, invert=False, shrink=0.7, seed=0, appear=False, follow=False, nscale=3.0, inward=None):
    """
    Granos emisivos repartidos sobre las caras de `src`. Entre f0 y f1 se desprenden
    (escalonados por un ruido de la posicion) hacia `direction` con remolino.
    invert=True: los granos llegan desde lejos y se asientan sobre la superficie.
    """
    o = bpy.data.objects.new(name, src.data.copy())
    if follow:      # hereda toda la animacion de la pieza (p.ej. extremidades que se balancean)
        o.parent = src
    else:
        o.matrix_world = src.matrix_world.copy()
        if src.parent: o.parent = src.parent; o.matrix_parent_inverse = src.matrix_parent_inverse.copy()
    link(o)
    ng = bpy.data.node_groups.new(name + '_GN', 'GeometryNodeTree')
    ng.interface.new_socket('Geometry', in_out='INPUT', socket_type='NodeSocketGeometry')
    ng.interface.new_socket('Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
    N, L = ng.nodes, ng.links
    gin = N.new('NodeGroupInput'); gout = N.new('NodeGroupOutput')
    dp = N.new('GeometryNodeDistributePointsOnFaces'); dp.inputs['Density'].default_value = density; dp.inputs['Seed'].default_value = seed
    pos = N.new('GeometryNodeInputPosition'); tm = N.new('GeometryNodeInputSceneTime')
    # escalonado: ruido de la posicion en 0..1
    nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = nscale
    nr = N.new('ShaderNodeMapRange'); nr.inputs['From Min'].default_value = 0.3; nr.inputs['From Max'].default_value = 0.7
    # t = (frame - f0) / (f1 - f0) - r * spread
    sub = N.new('ShaderNodeMath'); sub.operation = 'SUBTRACT'; sub.inputs[1].default_value = f0
    div = N.new('ShaderNodeMath'); div.operation = 'DIVIDE'; div.inputs[1].default_value = max(1, f1 - f0) * (1 - spread)
    rs = N.new('ShaderNodeMath'); rs.operation = 'MULTIPLY'; rs.inputs[1].default_value = spread / (1 - spread)
    tt = N.new('ShaderNodeMath'); tt.operation = 'SUBTRACT'
    k = N.new('ShaderNodeMapRange'); k.interpolation_type = 'SMOOTHSTEP'; k.clamp = True
    if invert:
        k.inputs['To Min'].default_value = 1; k.inputs['To Max'].default_value = 0
    L.new(pos.outputs[0], nz.inputs['Vector']); L.new(nz.outputs['Fac'], nr.inputs['Value'])
    L.new(tm.outputs['Frame'], sub.inputs[0]); L.new(sub.outputs[0], div.inputs[0])
    L.new(nr.outputs[0], rs.inputs[0]); L.new(div.outputs[0], tt.inputs[0]); L.new(rs.outputs[0], tt.inputs[1])
    L.new(tt.outputs[0], k.inputs['Value'])
    # desplazamiento: direccion * dist * k + remolino * k^2
    nz2 = N.new('ShaderNodeTexNoise'); nz2.inputs['Scale'].default_value = 0.6; nz2.inputs['Detail'].default_value = 2
    tsc = N.new('ShaderNodeVectorMath'); tsc.operation = 'ADD'
    tvec = N.new('ShaderNodeCombineXYZ')
    tsec = N.new('ShaderNodeMath'); tsec.operation = 'MULTIPLY'; tsec.inputs[1].default_value = 0.15
    L.new(tm.outputs['Seconds'], tsec.inputs[0]); L.new(tsec.outputs[0], tvec.inputs['Z'])
    L.new(pos.outputs[0], tsc.inputs[0]); L.new(tvec.outputs[0], tsc.inputs[1]); L.new(tsc.outputs[0], nz2.inputs['Vector'])
    cen = N.new('ShaderNodeVectorMath'); cen.operation = 'SUBTRACT'; cen.inputs[1].default_value = (0.5, 0.5, 0.5)
    L.new(nz2.outputs['Color'], cen.inputs[0])
    k2 = N.new('ShaderNodeMath'); k2.operation = 'MULTIPLY'
    L.new(k.outputs[0], k2.inputs[0]); L.new(k.outputs[0], k2.inputs[1])
    sw = N.new('ShaderNodeVectorMath'); sw.operation = 'SCALE'
    L.new(cen.outputs[0], sw.inputs[0])
    swk = N.new('ShaderNodeMath'); swk.operation = 'MULTIPLY'; swk.inputs[1].default_value = swirl * 2
    L.new(k2.outputs[0], swk.inputs[0]); L.new(swk.outputs[0], sw.inputs['Scale'])
    dv = N.new('ShaderNodeVectorMath'); dv.operation = 'SCALE'; dv.inputs[0].default_value = Vector(direction).normalized() * dist
    L.new(k.outputs[0], dv.inputs['Scale'])
    off = N.new('ShaderNodeVectorMath'); off.operation = 'ADD'
    L.new(dv.outputs[0], off.inputs[0]); L.new(sw.outputs[0], off.inputs[1])
    if inward:      # (centro, distancia): los granos tambien se cierran hacia el eje vertical del centro
        ctr, din = inward
        sxyz = N.new('ShaderNodeSeparateXYZ'); L.new(pos.outputs[0], sxyz.inputs[0])
        cxy = N.new('ShaderNodeCombineXYZ'); cxy.inputs['X'].default_value = ctr[0]; cxy.inputs['Y'].default_value = ctr[1]
        L.new(sxyz.outputs['Z'], cxy.inputs['Z'])
        tow = N.new('ShaderNodeVectorMath'); tow.operation = 'SUBTRACT'; L.new(cxy.outputs[0], tow.inputs[0]); L.new(pos.outputs[0], tow.inputs[1])
        nrm = N.new('ShaderNodeVectorMath'); nrm.operation = 'NORMALIZE'; L.new(tow.outputs[0], nrm.inputs[0])
        kin = N.new('ShaderNodeMath'); kin.operation = 'MULTIPLY'; kin.inputs[1].default_value = din; L.new(k.outputs[0], kin.inputs[0])
        sin_ = N.new('ShaderNodeVectorMath'); sin_.operation = 'SCALE'; L.new(nrm.outputs[0], sin_.inputs[0]); L.new(kin.outputs[0], sin_.inputs['Scale'])
        off2 = N.new('ShaderNodeVectorMath'); off2.operation = 'ADD'; L.new(off.outputs[0], off2.inputs[0]); L.new(sin_.outputs[0], off2.inputs[1])
        off = off2
    sp = N.new('GeometryNodeSetPosition')
    L.new(gin.outputs[0], dp.inputs['Mesh']); L.new(dp.outputs['Points'], sp.inputs['Geometry']); L.new(off.outputs[0], sp.inputs['Offset'])
    # grano: se achica al desprenderse
    ico = N.new('GeometryNodeMeshIcoSphere'); ico.inputs['Radius'].default_value = radius; ico.inputs['Subdivisions'].default_value = 1
    sc_ = N.new('ShaderNodeMath'); sc_.operation = 'MULTIPLY_ADD'; sc_.inputs[1].default_value = -shrink; sc_.inputs[2].default_value = 1.0
    L.new(k.outputs[0], sc_.inputs[0])
    scl = sc_.outputs[0]
    if appear:      # el grano no existe hasta que se desprende de la superficie
        gt = N.new('ShaderNodeMath'); gt.operation = 'GREATER_THAN'; gt.inputs[1].default_value = 0.0
        L.new(tt.outputs[0], gt.inputs[0])
        ms = N.new('ShaderNodeMath'); ms.operation = 'MULTIPLY'
        L.new(sc_.outputs[0], ms.inputs[0]); L.new(gt.outputs[0], ms.inputs[1]); scl = ms.outputs[0]
    ip = N.new('GeometryNodeInstanceOnPoints')
    L.new(sp.outputs[0], ip.inputs['Points']); L.new(ico.outputs['Mesh'], ip.inputs['Instance']); L.new(scl, ip.inputs['Scale'])
    sm = N.new('GeometryNodeSetMaterial'); sm.inputs['Material'].default_value = emit_mat(name + '_m', col, strength)
    L.new(ip.outputs[0], sm.inputs['Geometry']); L.new(sm.outputs[0], gout.inputs[0])
    o.modifiers.new('dust', 'NODES').node_group = ng
    o.data.materials.clear()
    return o


# ------------------------------------------------------------------ utilidades de montaje
def merged(objs, name):
    """une varias piezas (con modificadores aplicados) en un solo objeto en coordenadas de mundo"""
    import bmesh
    dg = bpy.context.evaluated_depsgraph_get()
    bm = bmesh.new(); gm = []
    for o in objs:
        tmp = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
        tmp.transform(o.matrix_world)
        n0 = len(bm.faces)
        bm.from_mesh(tmp)
        bm.faces.ensure_lookup_table()
        idx = []
        for s_ in o.material_slots:
            if s_.material not in gm: gm.append(s_.material)
            idx.append(gm.index(s_.material))
        if idx:
            for i in range(n0, len(bm.faces)):
                f = bm.faces[i]; f.material_index = idx[min(f.material_index, len(idx) - 1)]
        bpy.data.meshes.remove(tmp)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in gm: me.materials.append(m)
    return link(bpy.data.objects.new(name, me))


def dissolve(obj, f0, f1, spread, edge_col, edge_strength=6.0, nscale=3.0, edge_w=0.02):
    """
    La superficie se borra con el mismo ruido que usa dust(): cada punto desaparece
    cuando (frame - f0) / (f1 - f0) supera ruido * spread, con un borde encendido.
    """
    for i, s_ in enumerate(obj.material_slots):
        m = s_.material.copy(); obj.material_slots[i].material = m
        nt = m.node_tree; N, L = nt.nodes, nt.links
        out = next(n for n in N if n.bl_idname == 'ShaderNodeOutputMaterial' and n.is_active_output)
        src = out.inputs['Surface'].links[0].from_socket
        tc = N.new('ShaderNodeTexCoord'); nz = N.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = nscale
        nr = N.new('ShaderNodeMapRange'); nr.inputs['From Min'].default_value = 0.3; nr.inputs['From Max'].default_value = 0.7
        thr = N.new('ShaderNodeMath'); thr.operation = 'MULTIPLY'; thr.inputs[1].default_value = spread
        v = N.new('ShaderNodeValue'); v.name = 'avance'
        v.outputs[0].default_value = 0.0; v.outputs[0].keyframe_insert('default_value', frame=f0)
        v.outputs[0].default_value = 1.0; v.outputs[0].keyframe_insert('default_value', frame=f1)
        alive = N.new('ShaderNodeMath'); alive.operation = 'LESS_THAN'
        edge0 = N.new('ShaderNodeMath'); edge0.operation = 'SUBTRACT'; edge0.inputs[1].default_value = edge_w
        edge = N.new('ShaderNodeMath'); edge.operation = 'GREATER_THAN'
        em = N.new('ShaderNodeEmission'); em.inputs['Color'].default_value = (*edge_col, 1)
        es = N.new('ShaderNodeMath'); es.operation = 'MULTIPLY'; es.inputs[1].default_value = edge_strength
        add = N.new('ShaderNodeAddShader'); tr = N.new('ShaderNodeBsdfTransparent'); mix = N.new('ShaderNodeMixShader')
        L.new(tc.outputs['Object'], nz.inputs['Vector']); L.new(nz.outputs['Fac'], nr.inputs['Value'])
        L.new(nr.outputs[0], thr.inputs[0])
        L.new(v.outputs[0], alive.inputs[0]); L.new(thr.outputs[0], alive.inputs[1])
        L.new(thr.outputs[0], edge0.inputs[0]); L.new(v.outputs[0], edge.inputs[0]); L.new(edge0.outputs[0], edge.inputs[1])
        L.new(edge.outputs[0], es.inputs[0]); L.new(es.outputs[0], em.inputs['Strength'])
        L.new(src, add.inputs[0]); L.new(em.outputs[0], add.inputs[1])
        L.new(alive.outputs[0], mix.inputs[0]); L.new(tr.outputs[0], mix.inputs[1]); L.new(add.outputs[0], mix.inputs[2])
        L.new(mix.outputs[0], out.inputs['Surface'])
        ad = nt.animation_data
        if ad and ad.action:
            for fc in getattr(ad.action, 'fcurves', []):
                for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'


def sun_rot(direction):
    """rotacion para una luz SUN cuya luz viaja en `direction`"""
    return Vector(direction).normalized().to_track_quat('-Z', 'Y').to_euler()


def bbox_center(o):
    pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
    return sum(pts, Vector()) / 8


def principled_of(o):
    return principled(o.material_slots[0].material)


# ------------------------------------------------------------------ planos
def s1_cielo(sc):
    """PLANO 1-2 · El cielo del eclipse sobre la capilla; la luna tapa el sol, la camara sube y cae a cenital sobre el crater."""
    sc.frame_start, sc.frame_end = 1, 192
    bpy.data.collections['KIT_Exterior_Nuevo'].hide_render = True
    bg = world_gradient([(0.0, (0.05, 0.035, 0.02)), (0.5, (0.32, 0.2, 0.08)), (0.56, (0.16, 0.11, 0.05)), (0.75, (0.05, 0.04, 0.025)), (1.0, (0.01, 0.01, 0.012))], 1.0)
    key_input(bg.inputs['Strength'], 1, 1.0); key_input(bg.inputs['Strength'], 84, 0.22)
    # eclipse del valle: el sol detras de la luna; la luna avanza hasta la totalidad
    corona = bpy.data.objects['Eclipse_Corona_demo']; luna = bpy.data.objects['Eclipse_Luna_demo']
    lc = bbox_center(luna); away = lc.normalized()
    right = away.cross(Vector((0, 0, 1))).normalized(); up = right.cross(away).normalized()
    sphere('Sol', lc + away * 22, 17.0, emit_mat('m_sol', (1.0, 0.78, 0.35), 28))
    l0 = luna.location.copy()
    key(luna, 'location', 1, l0 + right * 24 + up * 9); key(luna, 'location', 84, l0)
    cp = principled_of(corona)
    cp.inputs['Base Color'].default_value = (0, 0, 0, 1)
    key_input(cp.inputs['Alpha'], 1, 0.0); key_input(cp.inputs['Alpha'], 70, 0.0); key_input(cp.inputs['Alpha'], 94, 1.0)
    key_input(cp.inputs['Emission Strength'], 1, 0.0); key_input(cp.inputs['Emission Strength'], 74, 0.0); key_input(cp.inputs['Emission Strength'], 98, 2.6)
    # nubes ocres atravesadas por la mirada
    cols = [(0.42, 0.33, 0.13), (0.34, 0.28, 0.12), (0.26, 0.22, 0.1)]
    for i, h in enumerate([150, 168, 186]):
        cl = plane(f'Nube{i}', (0, 0, h), 3000, cloud_mat(f'm_nube{i}', cols[i], 0.55 - i * 0.1, 0.012, 0.52 + 0.03 * i, i))
        e = cl.data.materials[0].node_tree.nodes.get('Emission')
        key_input(e.inputs['Strength'], 1, 0.55 - i * 0.1); key_input(e.inputs['Strength'], 84, 0.18)
    sol = bpy.data.objects['DEMO_Sol']; sol.data.color = (1.0, 0.62, 0.3)
    sol.data.energy = 2.6; sol.data.keyframe_insert('energy', frame=1); sol.data.energy = 0.12; sol.data.keyframe_insert('energy', frame=84)
    plata = light('SUN', 'Corona', (0, 0, 0), 0.0, SILVER, 4, rot=sun_rot(-away))
    plata.data.keyframe_insert('energy', frame=80); plata.data.energy = 0.9; plata.data.keyframe_insert('energy', frame=110)
    cam = camera(32)
    E = lc
    cam_path(cam, [
        (1,   (10, -112, 1.6), (E.x, E.y, E.z - 80), 0, 32),
        (96,  (8, -106, 5.0), (E.x, E.y, E.z - 40), 0, 34),
        (150, (3, -62, 58), (0, -6, 0), 0, 30),
        (192, (0, -1, 112), None, 0, 30)])
    sc.view_settings.exposure = 0.3


def s2_pozo(sc):
    """PLANO 3 · Picada cenital dentro del pozo del crater; el forro de ladrillo se deshace en polvo dorado."""
    sc.frame_start, sc.frame_end = 1, 156
    C = Vector((10.1, 2.4, 0.0))
    bg = world_gradient([(0.0, (0.03, 0.02, 0.012)), (0.5, (0.12, 0.07, 0.035)), (1.0, (0.01, 0.01, 0.014))], 0.35)
    s = bpy.data.objects['Sol bajo']; s.data.energy = 0.7; s.data.color = (1.0, 0.6, 0.3)
    r = bpy.data.objects['Rebote frío']; r.data.energy = 0.45; r.data.color = SILVER
    col = bpy.data.collections['Cráter · montaje abierto']
    forro = [o for o in col.all_objects if o.name.startswith('C08')]
    w = merged(forro, 'Forro')
    for o in forro: o.hide_render = True
    dissolve(w, 34, 170, 0.85, (1.0, 0.5, 0.12), 3, nscale=0.45, edge_w=0.025)
    dust(w, 'PolvoForro', 9.0, 0.045, GOLD, 12, 34, 170, direction=(0, 0, 1), dist=16, swirl=5, spread=0.85, appear=True, seed=3,
         nscale=0.45, inward=((C.x, C.y), 6.0))
    light('POINT', 'Fondo', C + Vector((0, 3, -7.5)), 500, GOLD, 3)
    light('POINT', 'Medio', C + Vector((0, 3, -2)), 150, AMBER, 4)
    cam = camera(28)
    cam_path(cam, [
        (1,   C + Vector((0, 0, 42)), None, 0, 28),
        (70,  C + Vector((0, 1.2, 22)), None, 40, 26),
        (156, C + Vector((0, 2.6, 8)), None, 100, 21)])
    sc.view_settings.exposure = 0.0


def ray_z(obj, x, y, top=3.0):
    """altura de la superficie de `obj` bajo (x, y), rayo hacia abajo en espacio del objeto"""
    inv = obj.matrix_world.inverted()
    o = inv @ Vector((x, y, top)); d = (inv.to_3x3() @ Vector((0, 0, -1))).normalized()
    ok, loc, n, i = obj.ray_cast(o, d)
    return (obj.matrix_world @ loc).z if ok else None


def s3_escalera(sc):
    """PLANOS 4-5 · La entrada rectangular: una figura de arena baja con la linterna y se deshace;
    la camara sigue al tunel de franjas cian y la reja de HUECO se disuelve en polvo azul."""
    sc.frame_start, sc.frame_end = 1, 318
    for o in sc.objects:
        if o.name.startswith('Col_'): o.hide_render = True
    world_gradient([(0.0, (0.02, 0.014, 0.01)), (0.5, (0.1, 0.06, 0.03)), (1.0, (0.012, 0.012, 0.016))], 0.7)
    light('SUN', 'SolRasante', (0, 0, 0), 5.0, (1.0, 0.58, 0.26), 1.0, rot=sun_rot((-0.9, 0.35, -0.32)))
    esc = bpy.data.objects['EntradaRecta_Escalera']
    # llanura alrededor, con el hueco del encuentro de la entrada (no tapa la escalera)
    tierra = bpy.data.materials['ER_Tierra_Demo']
    for i, (x0, x1, y0, y1) in enumerate([(-300, 300, -300, -0.2), (-300, 300, 21.4, 300), (-300, -10.8, -0.2, 21.4), (10.8, 300, -0.2, 21.4)]):
        me = bpy.data.meshes.new(f'llanura{i}'); z = -0.03
        me.from_pydata([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], [], [(0, 1, 2, 3)]); me.update()
        o = link(bpy.data.objects.new(f'Llanura{i}', me)); o.data.materials.append(tierra)
    # figura de arena (camina hacia +Y bajando la escalera)
    root, parts = figura(sc, 'Fig')
    root.rotation_euler = (0, 0, math.radians(90))
    path = [(1, 0.9), (60, 4.6), (120, 8.3), (150, 10.0)]
    for fr, y in path:
        z = ray_z(esc, 0.0, y) or 0.0
        key(root, 'location', fr, (0.0, y, z + 0.03))
    for fr in range(1, 151, 12):
        sg = 1 if (fr // 12) % 2 == 0 else -1
        for pn, a in [('piernaI', 22), ('piernaD', -22), ('brazoI', -16)]:
            key(parts[pn], 'rotation_euler', fr, (0, math.radians(a * sg), 0))
    key(parts['brazoD'], 'rotation_euler', 1, (0, math.radians(-38), 0))
    for o in parts.values(): o.hide_render = True
    for pn, o in parts.items():
        dust(o, 'Arena_' + pn, 1100, 0.011, (1.0, 0.66, 0.3), 1.6, 146, 214, direction=(1, 0, 0.35), dist=5, swirl=3.2, spread=0.55,
             seed=sum(map(ord, pn)) % 97, follow=True)
    lroot, lparts = kit('KIT_Linterna', (0, 0, 0), 0, 0.1)
    lroot.parent = parts['brazoD']; lroot.location = (0.06, 0, -0.34); lroot.rotation_euler = (0, 0, math.radians(180))
    for o in lparts.values(): hide_between(o, 1, 170)
    haz = light('SPOT', 'Haz', (0, 0, 0), 600, AMBER, 0.04, spot=38)
    haz.parent = parts['brazoD']; haz.location = (0.3, 0, -0.32); haz.rotation_euler = (0, math.radians(-20), 0)
    haz.data.keyframe_insert('energy', frame=150); haz.data.energy = 0; haz.data.keyframe_insert('energy', frame=176)
    # tunel: paleta teal; la reja HUECO al fondo
    rr, rp = kit('KIT_Reja_Hueco', (0, 18.3, -9.12), 0, 0.68)
    bars = [o for n, o in rp.items() if n.startswith('Gate_Vertical') or n.startswith('Gate_Horizontal')]
    bpy.context.view_layer.update()
    rej = merged(bars, 'Barrotes')
    for o in bars: o.hide_render = True
    dissolve(rej, 250, 300, 0.8, (0.25, 0.55, 1.0), 18)
    dust(rej, 'PolvoAzul', 450, 0.012, (0.25, 0.55, 1.0), 5, 250, 300, direction=(0, 1, 0.3), dist=3, swirl=3.0, spread=0.8, appear=True, seed=11, shrink=0.9)
    box('Vacio', (0, 19.9, -7.5), (4.6, 0.4, 4.6), mat('m_vacio', (0.0, 0.0, 0.0), 1.0))
    seal = rp['Gate_BlueSeal']; p = principled_of(seal)
    key_input(p.inputs['Emission Strength'], 200, 0.6); key_input(p.inputs['Emission Strength'], 245, 4.0); key_input(p.inputs['Emission Strength'], 300, 0.0)
    light('AREA', 'Teal', (0, 15.0, -6.1), 260, TEAL, 2.5, rot=(0, 0, 0))
    light('POINT', 'Azul', (0, 19.5, -7.6), 300, HUECO, 1.0)
    cam = camera(30)
    cam_path(cam, [
        (1,   (0.3, -5.5, 4.2), (0, 4.5, -3.0), 0, 30),
        (150, (0.45, 5.4, -2.4), (0, 11.4, -7.8), 0, 32),
        (205, (0.25, 8.9, -5.0), (0, 14.8, -8.0), 0, 30),
        (250, (0.0, 12.6, -7.45), (0, 18.4, -7.5), 0, 28),
        (300, (0.0, 15.7, -7.5), (0, 19.0, -7.5), 0, 28),
        (336, (0.0, 18.9, -7.5), (0, 23.0, -7.5), 0, 28)])
    sc.view_settings.exposure = 0.4


def mapa(materials_bright=1.0):
    for c in ['MAPA_Terreno', 'MAPA_Recorrido', 'MAPA_Templo']:
        append_collection(MAPA, c)
    for m in bpy.data.materials:
        p = principled(m) if m.node_tree else None
        if p and materials_bright != 1.0:
            b = p.inputs['Base Color'].default_value
            p.inputs['Base Color'].default_value = (min(1, b[0] * materials_bright), min(1, b[1] * materials_bright), min(1, b[2] * materials_bright), 1)


def figura(sc, name):
    """silueta humana simple hecha con cajas, para sembrar granos de arena"""
    root = link(bpy.data.objects.new(name, None))
    m = mat(name + '_m', (0.5, 0.5, 0.5))
    parts = {}
    for pn, loc, size in [('torso', (0, 0, 1.22), (0.42, 0.26, 0.62)), ('cabeza', (0, 0, 1.68), (0.22, 0.24, 0.26)),
                          ('cadera', (0, 0, 0.88), (0.36, 0.24, 0.16)),
                          ('brazoI', (0, 0.27, 1.2), (0.12, 0.12, 0.6)), ('brazoD', (0, -0.27, 1.2), (0.12, 0.12, 0.6)),
                          ('piernaI', (0, 0.11, 0.42), (0.15, 0.15, 0.84)), ('piernaD', (0, -0.11, 0.42), (0.15, 0.15, 0.84))]:
        o = box(f'{name}_{pn}', loc, size, m); o.parent = root; parts[pn] = o
    return root, parts


def s5_espiral(sc):
    """La espiral tallada en cenital: gira y la camara cae hacia su centro."""
    sc.frame_start, sc.frame_end = 1, 168
    world_color((0.0, 0.0, 0.0), 0)
    root, parts = kit('KIT_Motivos_Tallados', (0, 0, 0))
    root.rotation_euler = (math.radians(-90), 0, 0)   # de frente a tendido: lo vemos desde arriba
    sp = parts['Motif_Spiral']
    c = sp.matrix_world @ Vector((0, 0, 0))
    bpy.context.view_layer.update()
    ws = [sp.matrix_world @ Vector(v) for v in sp.bound_box]
    center = sum(ws, Vector()) / 8
    # el resto de los motivos, apagados, en un muro de basalto
    wall = plane('Muro', (center.x + 1.5, center.y, center.z - 0.2), 14, mat('m_muro', (0.03, 0.028, 0.03), 0.9))
    gold = emit_mat('m_oro', GOLD, 7)
    sp.data.materials.clear(); sp.data.materials.append(gold)
    for n, o in parts.items():
        if n != 'Motif_Spiral' and o.type == 'MESH':
            o.data.materials.clear(); o.data.materials.append(mat('m_apag_' + n, (0.05, 0.04, 0.03), 0.7, emit=AMBER, strength=0.25))
    piv = link(bpy.data.objects.new('Giro', None)); piv.location = center
    mw = sp.matrix_world.copy()
    sp.parent = None; sp.matrix_basis = mw
    sp.parent = piv; sp.matrix_parent_inverse = Matrix.Translation(center).inverted()
    key(piv, 'rotation_euler', 1, (0, 0, 0)); key(piv, 'rotation_euler', 168, (0, 0, math.radians(-160)))
    g = dust(sp, 'TrazosOro', 3200, 0.0035, (1.0, 0.72, 0.36), 7, 40, 168, direction=(0, 0, 1), dist=0.35, swirl=0.12, spread=0.7)
    light('POINT', 'Brillo', (center.x, center.y, center.z + 1.2), 60, GOLD, 0.5)
    cam = camera(50)
    cam_path(cam, [(1, (center.x, center.y, center.z + 7), None, 0, 45),
                   (110, (center.x, center.y, center.z + 2.4), None, 40, 50),
                   (168, (center.x + 0.05, center.y - 0.1, center.z + 1.5), None, 90, 55)])
    sc.view_settings.exposure = 0.4


def s6_valle(sc):
    """PLANO 6 · Timelapse del valle: los cardones y las matas brotan mientras las sombras giran (amanecer a ocaso)."""
    sc.frame_start, sc.frame_end = 1, 168
    bg = world_color((0.5, 0.36, 0.22), 0.5)
    for fr, c, st in [(1, (0.35, 0.2, 0.12), 0.25), (60, (0.42, 0.46, 0.55), 0.55), (120, (0.6, 0.38, 0.2), 0.45), (168, (0.22, 0.07, 0.04), 0.15)]:
        key_input(bg.inputs['Color'], fr, (*c, 1)); key_input(bg.inputs['Strength'], fr, st)
    sol = bpy.data.objects['Sol bajo']
    for fr, el, az, en, c in [(1, 4, -70, 1.2, (1.0, 0.45, 0.2)), (60, 55, 10, 3.6, (1.0, 0.86, 0.7)), (120, 28, 110, 2.8, (1.0, 0.62, 0.35)), (168, 2, 175, 0.6, (1.0, 0.3, 0.1))]:
        d = Vector((math.cos(math.radians(el)) * math.sin(math.radians(az)), math.cos(math.radians(el)) * math.cos(math.radians(az)), -math.sin(math.radians(el))))
        key(sol, 'rotation_euler', fr, sun_rot(d)); sol.data.energy = en; sol.data.keyframe_insert('energy', frame=fr)
        sol.data.color = c; sol.data.keyframe_insert('color', frame=fr)
    cam_pos = Vector((47.0, -9.0, 4.2))
    plants = [o for o in bpy.data.collections['Valle · afloramientos y cardones'].all_objects
              if o.type == 'MESH' and (o.name.startswith('V'))]
    import random
    rnd = random.Random(7)
    for o in plants:
        dist = (o.matrix_world.translation - cam_pos).length
        f = int(min(130, 8 + dist * 1.6 + rnd.uniform(-10, 10)))
        s = o.scale.copy()
        key(o, 'scale', 1, s * 0.001); key(o, 'scale', f, s * 0.001); key(o, 'scale', f + 26, s)
    cam = camera(26)
    cam_path(cam, [
        (1,   cam_pos, (28, 3, 2.0), 0, 26),
        (168, Vector((36.0, -7.5, 2.9)), (10, 4, 0.0), 0, 24)])
    sc.view_settings.exposure = -0.2


def s7_estacion(sc):
    """PLANO 7 · La estacion de observacion: el dial bajo una luz roja que gira como un mecanismo; la veleta sobre el techo."""
    sc.frame_start, sc.frame_end = 1, 144
    world_gradient([(0.0, (0.02, 0.004, 0.002)), (0.5, (0.16, 0.03, 0.01)), (1.0, (0.01, 0.003, 0.003))], 0.5)
    for n in ['Sol bajo', 'Rebote frío']:
        bpy.data.objects[n].data.energy *= 0.12
    dial = bpy.data.objects['H04 · Dial de observación.002']
    D = bbox_center(dial); top = D.z + 0.9
    piv = link(bpy.data.objects.new('GiroLuz', None)); piv.location = (D.x, D.y, top)
    roja = light('POINT', 'Roja', (2.4, 0, 1.4), 220, (1.0, 0.16, 0.04), 0.08); roja.parent = piv
    key(piv, 'rotation_euler', 1, (0, 0, 0), 'LINEAR'); key(piv, 'rotation_euler', 144, (0, 0, math.radians(300)), 'LINEAR')
    light('POINT', 'Brasa', (D.x - 1.2, D.y - 2.4, 0.9), 60, (1.0, 0.35, 0.08), 0.6)
    vel = bpy.data.objects['H05 · Veleta.001']
    key(vel, 'rotation_euler', 1, (0, 0, 0), 'LINEAR'); key(vel, 'rotation_euler', 144, (0, 0, math.radians(-540)), 'LINEAR')
    light('SPOT', 'Veleta', (vel.location.x + 3, vel.location.y - 4, vel.location.z + 3), 400, (1.0, 0.25, 0.06), 0.1,
          rot=aim((vel.location.x + 3, vel.location.y - 4, vel.location.z + 3), vel.location).to_euler(), spot=30)
    cam = camera(45)
    keys = []
    for i, fr in enumerate([1, 48, 96, 144]):
        a = math.radians(-40 + i * 38); r = 3.0 - i * 0.35
        keys.append((fr, (D.x + r * math.cos(a), D.y + r * math.sin(a), top + 0.9 - i * 0.15), (D.x, D.y, top - 0.15), 0, 42 + i * 3))
    cam_path(cam, keys)
    sc.view_settings.exposure = 0.3


def s8_radiografia(sc):
    """PLANO 8 · El recorrido completo del Mapa_01 brillando dentro del crater, como una radiografia."""
    sc.frame_start, sc.frame_end = 1, 120
    mapa(1.0)
    world_color((0.0, 0.0, 0.0), 0)
    ter = bpy.data.objects['Terreno_Crater']
    dec = ter.modifiers.new('dec', 'DECIMATE'); dec.ratio = 0.12
    wf = ter.modifiers.new('wf', 'WIREFRAME'); wf.thickness = 0.35; wf.use_replace = True
    ter.data.materials.clear(); ter.data.materials.append(emit_mat('m_rayos', (0.35, 0.45, 0.6), 0.9))
    cols = {'Templo': SILVER, 'Tunel': HUECO, 'Anillo': AMBER, 'Terraza': AMBER, 'Plaza': (1.0, 0.8, 0.55), 'Escalera': (1.0, 0.8, 0.55)}
    i = 0
    for o in bpy.data.objects:
        if o.type != 'MESH' or o is ter: continue
        c = next((v for k, v in cols.items() if o.name.startswith(k)), GOLD)
        m = emit_mat('m_rec_' + o.name, c, 0.0); o.data.materials.clear(); o.data.materials.append(m)
        e = m.node_tree.nodes.get('Emission')
        f = 6 + (i % 12) * 4; i += 1
        key_input(e.inputs['Strength'], 1, 0.0); key_input(e.inputs['Strength'], f, 0.0); key_input(e.inputs['Strength'], f + 10, 2.2)
    cam = camera(30)
    cx, cy = -8, 20
    cam_path(cam, [
        (1,   (62, -120, 92), (48, 20, -10), 0, 28),
        (60,  (40, -170, 140), (40, 20, -10), 0, 28),
        (120, (-8, -330, 215), (-8, 20, -20), 0, 30)])
    sc.view_settings.exposure = 0.3


def s9_eclipse(sc):
    """La corona gira como un rotor; el anillo de diamante crece hasta llenar el cuadro; titulo metalico."""
    sc.frame_start, sc.frame_end = 1, 180
    world_color((0.0, 0.0, 0.0), 0)
    # corona: disco emisivo con rayos radiales (coordenadas polares)
    m = bpy.data.materials.new('m_corona')
    try: m.use_nodes = True
    except Exception: pass
    nt = m.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    at = nt.nodes.new('ShaderNodeMath'); at.operation = 'ARCTAN2'
    ln = nt.nodes.new('ShaderNodeVectorMath'); ln.operation = 'LENGTH'
    cxyz = nt.nodes.new('ShaderNodeCombineXYZ')
    sca = nt.nodes.new('ShaderNodeMath'); sca.operation = 'MULTIPLY'; sca.inputs[1].default_value = 12.0
    nz = nt.nodes.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 3.0; nz.inputs['Detail'].default_value = 6
    fall = nt.nodes.new('ShaderNodeMapRange'); fall.inputs['From Min'].default_value = 1.0; fall.inputs['From Max'].default_value = 3.4; fall.inputs['To Min'].default_value = 1; fall.inputs['To Max'].default_value = 0
    pw = nt.nodes.new('ShaderNodeMath'); pw.operation = 'POWER'; pw.inputs[1].default_value = 2.2
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    ray = nt.nodes.new('ShaderNodeMapRange'); ray.inputs['From Min'].default_value = 0.42; ray.inputs['From Max'].default_value = 0.75
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value = (*SILVER, 1)
    st = nt.nodes.new('ShaderNodeMath'); st.operation = 'MULTIPLY'; st.inputs[1].default_value = 1.6
    tr = nt.nodes.new('ShaderNodeBsdfTransparent'); mx = nt.nodes.new('ShaderNodeAddShader'); o = nt.nodes.new('ShaderNodeOutputMaterial')
    L = nt.links
    L.new(tc.outputs['Object'], sep.inputs[0]); L.new(sep.outputs['Y'], at.inputs[0]); L.new(sep.outputs['X'], at.inputs[1])
    L.new(tc.outputs['Object'], ln.inputs[0]); L.new(at.outputs[0], sca.inputs[0]); L.new(sca.outputs[0], cxyz.inputs['X'])
    L.new(ln.outputs['Value'], cxyz.inputs['Y']); L.new(cxyz.outputs[0], nz.inputs['Vector'])
    L.new(nz.outputs['Fac'], ray.inputs['Value']); L.new(ln.outputs['Value'], fall.inputs['Value']); L.new(fall.outputs[0], pw.inputs[0])
    L.new(pw.outputs[0], mul.inputs[0]); L.new(ray.outputs[0], mul.inputs[1]); L.new(mul.outputs[0], st.inputs[0])
    L.new(st.outputs[0], em.inputs['Strength']); L.new(tr.outputs[0], mx.inputs[0]); L.new(em.outputs[0], mx.inputs[1]); L.new(mx.outputs[0], o.inputs[0])
    corona = disc('Corona', (0, 0, 0), 3.6, m, rot=(math.radians(90), 0, 0))
    key(corona, 'rotation_euler', 1, (math.radians(90), 0, 0), 'LINEAR'); key(corona, 'rotation_euler', 120, (math.radians(90), math.radians(60), 0), 'LINEAR')
    corona.location = (0, 0.9, 0)
    # piezas del kit de eclipse (CRATER_Kit_Exterior_1): corona, luna y anillo de diamante, centradas en el eje
    append_collection(EXT1, 'KIT_Ext_Eclipse')
    bpy.context.view_layer.update()
    kp = {o.name.split('.')[0]: o for o in bpy.data.collections['KIT_Ext_Eclipse'].objects}
    k = 1.0 / 1.03
    for n, y in [('Eclipse_Corona', 0.5), ('Eclipse_Luna', 0.0), ('Eclipse_Anillo_Diamante', -0.04), ('Eclipse_Sol', 0.3)]:
        o = kp[n]; c = bbox_center(o)
        o.location = o.location - c; bpy.context.view_layer.update()
        piv = link(bpy.data.objects.new('P_' + n, None)); piv.location = (0, y, 0)
        o.parent = piv; piv.scale = (k, k, k)
        kp[n] = piv
    luna = kp['Eclipse_Luna']
    kc = kp['Eclipse_Corona']
    key(kc, 'rotation_euler', 1, (0, 0, 0), 'LINEAR'); key(kc, 'rotation_euler', 118, (0, math.radians(-95), 0), 'LINEAR')
    ring = kp['Eclipse_Anillo_Diamante']; hide_between(ring, 70, 118)
    key(ring, 'scale', 70, (k * 0.9,) * 3); key(ring, 'scale', 100, (k,) * 3)
    kp['Eclipse_Sol'].hide_render = True
    for o in bpy.data.collections['KIT_Ext_Eclipse'].objects:
        if o.name.startswith('Eclipse_Sol'): o.hide_render = True
    # anillo de diamante: la cuenta de luz en el borde crece hasta cubrir todo
    bead = sphere('Diamante', (math.cos(math.radians(35)) * 1.0, -0.2, math.sin(math.radians(35)) * 1.0), 0.06, emit_mat('m_diam', (1.0, 0.95, 0.85), 30))
    key(bead, 'scale', 1, (0.01,) * 3); key(bead, 'scale', 60, (0.3,) * 3); key(bead, 'scale', 92, (1.4,) * 3); key(bead, 'scale', 112, (90,) * 3)
    for ob in [corona, bead] + [o for o in bpy.data.collections['KIT_Ext_Eclipse'].objects if not o.name.startswith('Eclipse_Sol')]: hide_between(ob, 1, 118)
    # titulo metalico sobre resplandor ocre
    fondo = plane('Ocre', (0, 6, 0), 40, emit_mat('m_ocre', (0.85, 0.4, 0.1), 2.2), rot=(math.radians(90), 0, 0))
    fe = fondo.data.materials[0].node_tree.nodes.get('Emission')
    key_input(fe.inputs['Strength'], 118, 2.0); key_input(fe.inputs['Strength'], 134, 0.25); key_input(fe.inputs['Strength'], 180, 0.12)
    cu = bpy.data.curves.new('Titulo', 'FONT')
    cu.body = 'CRÁTER'
    try: cu.font = bpy.data.fonts.load(os.path.join(FONTS, 'Cinzel-600.ttf'))
    except Exception as e: print('fuente', e)
    cu.align_x = 'CENTER'; cu.align_y = 'CENTER'; cu.size = 1.0; cu.space_character = 1.45
    cu.extrude = 0.08; cu.bevel_depth = 0.012; cu.bevel_resolution = 2
    tit = link(bpy.data.objects.new('Titulo', cu)); tit.location = (0, 0.4, 0); tit.rotation_euler = (math.radians(90), 0, 0)
    metal = mat('m_metal', (0.42, 0.36, 0.3), 0.38, metal=1.0)
    nt2 = metal.node_tree; nzz = nt2.nodes.new('ShaderNodeTexNoise'); nzz.inputs['Scale'].default_value = 22; nzz.inputs['Detail'].default_value = 10
    rr = nt2.nodes.new('ShaderNodeMapRange'); rr.inputs['To Min'].default_value = 0.22; rr.inputs['To Max'].default_value = 0.7
    nt2.links.new(nzz.outputs['Fac'], rr.inputs['Value']); nt2.links.new(rr.outputs[0], principled(metal).inputs['Roughness'])
    cu.materials.append(metal)
    hide_between(tit, 118); hide_between(fondo, 112)
    light('AREA', 'Clave', (-3, -5, 3), 260, (1.0, 0.8, 0.6), 4, rot=(math.radians(60), 0, math.radians(-30)))
    light('AREA', 'Rim', (3, 3, 2), 600, (1.0, 0.55, 0.2), 3, rot=(math.radians(-60), 0, math.radians(150)))
    cam = camera(50)
    cam_path(cam, [(1, (0, -9.0, 0), (0, 0, 0), 0, 50), (110, (0, -6.6, 0), (0, 0, 0), 0, 50),
                   (118, (0, -8.2, 0), (0, 0, 0), 0, 50), (180, (0, -6.8, 0), (0, 0, 0), 0, 50)])
    sc.view_settings.exposure = 0.4


SHOTS = {'s1': (EXT1, 'Scene', s1_cielo), 's2': (EXTM, '01 · Montaje exterior', s2_pozo),
         's3': (EXT1, 'DEMO_Entrada_Rectangular', s3_escalera), 's5': (None, None, s5_espiral),
         's6': (EXTM, '01 · Montaje exterior', s6_valle), 's7': (EXTM, '01 · Montaje exterior', s7_estacion),
         's8': (None, None, s8_radiografia), 's9': (None, None, s9_eclipse)}


def main():
    a = sys.argv[1:]
    shot, out = a[0], a[1]
    frames = None; pct = 100; samples = 16
    if '--frames' in a: frames = [int(x) for x in a[a.index('--frames') + 1].split(',')]
    if '--pct' in a: pct = int(a[a.index('--pct') + 1])
    if '--samples' in a: samples = int(a[a.index('--samples') + 1])
    path, scene, fn = SHOTS[shot]
    sc = open_base(path, scene, samples, pct) if path else reset(samples, pct)
    fn(sc)
    if '--fs' in a:     # recorte de rango: --fs 1,96
        f0, f1 = [int(x) for x in a[a.index('--fs') + 1].split(',')]
        sc.frame_start, sc.frame_end = f0, f1
    d = os.path.join(out, shot); os.makedirs(d, exist_ok=True)
    if frames:
        for f in frames:
            sc.frame_set(f); sc.render.filepath = os.path.join(d, f'{f:04d}.png')
            bpy.ops.render.render(write_still=True, scene=sc.name)
    else:
        sc.render.filepath = os.path.join(d, '####')
        sc.render.use_overwrite = False; sc.render.use_placeholder = True   # se puede reanudar
        bpy.ops.render.render(animation=True, scene=sc.name)


if __name__ == '__main__':
    main()
