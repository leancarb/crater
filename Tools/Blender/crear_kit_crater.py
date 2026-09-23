import bpy
import math
import os
from mathutils import Vector, Matrix, Euler


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(ROOT, "Assets", "Art", "Blender", "CRATER_Kit_Modular.blend")
PREVIEW_PATH = os.path.join(ROOT, "Assets", "Art", "Previews", "CRATER_Kit_Modular.png")
EXPORT_DIR = os.path.join(ROOT, "Assets", "Models", "CraterKit")


def reset_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)


def material(name, color, metallic=0.0, roughness=0.65, emission=None, strength=0.0, alpha=1.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, alpha)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Alpha"].default_value = alpha
        if emission:
            socket = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
            if socket:
                socket.default_value = (*emission, 1.0)
            if bsdf.inputs.get("Emission Strength"):
                bsdf.inputs["Emission Strength"].default_value = strength
    if alpha < 1.0 and hasattr(mat, "surface_render_method"):
        mat.surface_render_method = 'DITHERED'
    return mat


def collection(name):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def move_to_collection(obj, col):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def assign(obj, mat):
    if hasattr(obj.data, "materials"):
        obj.data.materials.append(mat)
    return obj


def bevel(obj, amount=0.06, segments=2):
    mod = obj.modifiers.new("Bisel", 'BEVEL')
    mod.width = amount
    mod.segments = segments
    return obj


def cube(name, location, scale, mat, col, rotation=(0, 0, 0), bevel_amount=0.04):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel_amount:
        bevel(obj, bevel_amount, 2)
    assign(obj, mat)
    return move_to_collection(obj, col)


def cylinder(name, location, radius, depth, mat, col, rotation=(0, 0, 0), vertices=10, bevel_amount=0.03):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    if bevel_amount:
        bevel(obj, bevel_amount, 2)
    assign(obj, mat)
    return move_to_collection(obj, col)


def ico(name, location, scale, mat, col, subdivisions=1, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0,
                                         location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    return move_to_collection(obj, col)


def torus(name, location, major, minor, mat, col, rotation=(math.pi / 2, 0, 0), major_segments=16):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor,
                                    major_segments=major_segments, minor_segments=6,
                                    location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    return move_to_collection(obj, col)


def curve_tube(name, points, bevel_depth, mat, col, cyclic=False):
    curve = bpy.data.curves.new(name + "_Curve", 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 1
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = 0
    spline = curve.splines.new('POLY')
    spline.points.add(len(points) - 1)
    for index, point in enumerate(points):
        spline.points[index].co = (*point, 1.0)
    spline.use_cyclic_u = cyclic
    obj = bpy.data.objects.new(name, curve)
    col.objects.link(obj)
    assign(obj, mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    obj.select_set(False)
    return obj


def create_anchor(col, basalt, basalt_light, amber):
    ico("Anchor_Rock_Back", (0, 0.15, 1.35), (1.05, 0.42, 1.32), basalt, col, 2,
        rotation=(0.08, 0.05, -0.08))
    ico("Anchor_Rock_Front", (0, -0.18, 1.35), (0.78, 0.18, 0.92), basalt_light, col, 1,
        rotation=(0.0, 0.15, 0.1))
    for radius, thickness in ((0.46, 0.055), (0.31, 0.045)):
        torus("Anchor_Ring", (0, -0.39, 1.35), radius, thickness, amber, col)
    cylinder("Anchor_Core", (0, -0.40, 1.35), 0.16, 0.09, amber, col,
             rotation=(math.pi / 2, 0, 0), vertices=16, bevel_amount=0.015)
    ico("Anchor_Base", (0, 0.08, 0.28), (0.8, 0.55, 0.35), basalt, col, 1)


def create_flashlight(col, metal, worn, amber):
    axis = (0, math.pi / 2, 0)
    cylinder("Flashlight_Body", (0, 0, 0.45), 0.34, 2.8, metal, col, axis, 10, 0.05)
    cylinder("Flashlight_Head", (-1.55, 0, 0.45), 0.68, 0.72, metal, col, axis, 10, 0.06)
    cylinder("Flashlight_Bevel", (-1.93, 0, 0.45), 0.78, 0.18, worn, col, axis, 10, 0.04)
    cylinder("Flashlight_Lens", (-2.04, 0, 0.45), 0.57, 0.06, amber, col, axis, 10, 0.01)
    cylinder("Flashlight_Rear", (1.50, 0, 0.45), 0.42, 0.28, worn, col, axis, 10, 0.04)
    cube("Flashlight_SwitchBase", (-0.25, 0, 0.84), (0.36, 0.20, 0.07), worn, col, bevel_amount=0.035)
    cube("Flashlight_Switch", (-0.30, 0, 0.96), (0.18, 0.14, 0.07), metal, col, bevel_amount=0.025)
    torus("Flashlight_RearLoop", (1.82, 0, 0.45), 0.32, 0.055, worn, col,
          rotation=(0, math.pi / 2, 0), major_segments=12)


def create_gate(col, basalt, basalt_light, blue):
    cube("Gate_LeftPost", (-2.25, 0, 2.15), (0.45, 0.55, 2.15), basalt, col, rotation=(0, 0.04, -0.02))
    cube("Gate_RightPost", (2.25, 0, 2.15), (0.45, 0.55, 2.15), basalt, col, rotation=(0, -0.04, 0.02))
    cube("Gate_Top", (0, 0, 4.15), (2.7, 0.55, 0.38), basalt, col)
    for i, x in enumerate((-1.55, -0.78, 0, 0.78, 1.55)):
        cube(f"Gate_Vertical_{i+1}", (x, -0.12, 2.05), (0.15, 0.20, 1.8),
             basalt_light, col, rotation=(0, 0, (i - 2) * 0.018), bevel_amount=0.07)
    for i, z in enumerate((0.75, 1.55, 2.35, 3.15)):
        cube(f"Gate_Horizontal_{i+1}", (0, -0.14, z), (1.95, 0.18, 0.13),
             basalt_light, col, rotation=(0, 0, (-1) ** i * 0.018), bevel_amount=0.065)
    cube("Gate_BlueSeal", (0, -0.38, 2.05), (1.95, 0.025, 1.75), blue, col, bevel_amount=0.0)


def create_bridge(col, amber_glass, amber):
    width = 2.2
    length = 8.0
    panel_length = length / 6.0
    for i in range(6):
        y0 = i * panel_length
        verts = [
            (-width / 2, y0, -0.08), (width / 2, y0, -0.08),
            (-width / 2, y0 + panel_length, -0.08), (width / 2, y0 + panel_length, -0.08),
            (-width / 2, y0, 0.08), (width / 2, y0, 0.08),
            (-width / 2, y0 + panel_length, 0.08), (width / 2, y0 + panel_length, 0.08),
        ]
        faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1),
                 (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
        mesh = bpy.data.meshes.new(f"BridgePanel_{i+1}_Mesh")
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        obj = bpy.data.objects.new(f"BridgePanel_{i+1}", mesh)
        col.objects.link(obj)
        assign(obj, amber_glass)
        diag = 0.72 if i % 2 == 0 else -0.72
        cube(f"BridgeFacet_{i+1}", (0, y0 + panel_length / 2, 0.095),
             (width * 0.52, 0.025, 0.018), amber, col,
             rotation=(0, 0, diag), bevel_amount=0.0)
    cube("Bridge_Edge_L", (-width / 2, length / 2, 0.10), (0.025, length / 2, 0.025), amber, col, bevel_amount=0.0)
    cube("Bridge_Edge_R", (width / 2, length / 2, 0.10), (0.025, length / 2, 0.025), amber, col, bevel_amount=0.0)


def create_architecture(col, basalt, stone):
    cube("Architecture_Wall", (0, 0.25, 1.75), (2.8, 0.25, 1.75), basalt, col, bevel_amount=0.08)
    for x in (-2.45, 2.45):
        cube("Architecture_Pillar", (x, 0, 2.8), (0.38, 0.42, 2.8), stone, col,
             rotation=(0, 0, -0.035 if x < 0 else 0.035), bevel_amount=0.1)
        ico("Architecture_PillarCap", (x, 0, 5.72), (0.58, 0.55, 0.38), stone, col, 1)
    torus("Architecture_Oculus", (0, -0.28, 2.3), 1.05, 0.12, stone, col, major_segments=20)
    torus("Architecture_OculusInner", (0, -0.29, 2.3), 0.72, 0.055, stone, col, major_segments=20)


def create_motifs(col, amber):
    # Espiral ritual.
    spiral = []
    for i in range(36):
        t = i / 35 * math.tau * 2.25
        radius = 0.05 + i / 35 * 0.72
        spiral.append((-2.1 + math.cos(t) * radius, -0.04, 1.4 + math.sin(t) * radius))
    curve_tube("Motif_Spiral", spiral, 0.055, amber, col)

    # Serpiente escalonada, inspirada en el motivo compartido.
    snake = [(-0.95, -0.04, 1.85), (-0.65, -0.04, 1.85), (-0.65, -0.04, 1.55),
             (-0.35, -0.04, 1.55), (-0.35, -0.04, 1.25), (-0.05, -0.04, 1.25),
             (0.25, -0.04, 1.25), (0.25, -0.04, 1.55), (0.55, -0.04, 1.55),
             (0.55, -0.04, 1.85), (0.85, -0.04, 1.85)]
    curve_tube("Motif_Snake", snake, 0.075, amber, col)
    ico("Motif_SnakeHead", (1.0, -0.04, 1.85), (0.28, 0.07, 0.20), amber, col, 1)
    cylinder("Motif_SnakeEye", (1.04, -0.12, 1.92), 0.035, 0.04, basalt_dark, col,
             rotation=(math.pi / 2, 0, 0), vertices=8, bevel_amount=0.0)

    # Chakana geométrica y rombo, útiles como placas modulares.
    blocks = [(2.0, 1.4), (2.0, 1.75), (2.0, 1.05), (1.65, 1.4), (2.35, 1.4)]
    for i, (x, z) in enumerate(blocks):
        cube(f"Motif_Chakana_{i+1}", (x, -0.04, z), (0.18, 0.055, 0.18), amber, col, bevel_amount=0.015)
    cube("Motif_Diamond", (3.05, -0.04, 1.4), (0.18, 0.055, 0.18), amber, col,
         rotation=(0, math.pi / 4, 0), bevel_amount=0.015)


def duplicate_for_preview(source_col, preview_col, offset, rotation=(0, 0, 0), scale=1.0):
    transform = (Matrix.Translation(Vector(offset))
                 @ Euler(rotation, 'XYZ').to_matrix().to_4x4()
                 @ Matrix.Scale(scale, 4))
    for source in source_col.objects:
        copy = source.copy()
        if source.data:
            copy.data = source.data
        preview_col.objects.link(copy)
        copy.matrix_world = transform @ source.matrix_world


def look_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()


def export_collection(col, filename):
    bpy.ops.object.select_all(action='DESELECT')
    mesh_objects = [obj for obj in col.objects if obj.type == 'MESH']
    for obj in mesh_objects:
        obj.select_set(True)
    if not mesh_objects:
        return
    bpy.context.view_layer.objects.active = mesh_objects[0]
    filepath = os.path.join(EXPORT_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={'MESH'},
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',
        axis_up='Y',
        add_leaf_bones=False,
        bake_anim=False,
    )


reset_scene()
os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
os.makedirs(os.path.dirname(PREVIEW_PATH), exist_ok=True)
os.makedirs(EXPORT_DIR, exist_ok=True)

basalt_dark = material("M_Basalt_Dark", (0.018, 0.022, 0.030), roughness=0.78)
basalt_mid = material("M_Basalt_Mid", (0.055, 0.065, 0.085), roughness=0.72)
stone = material("M_Stone_Edge", (0.16, 0.15, 0.14), roughness=0.88)
metal = material("M_Flashlight_BlackMetal", (0.035, 0.040, 0.048), metallic=0.72, roughness=0.34)
worn = material("M_Flashlight_Worn", (0.12, 0.11, 0.10), metallic=0.48, roughness=0.58)
amber = material("M_Amber_Emission", (0.78, 0.31, 0.035), roughness=0.28,
                 emission=(1.0, 0.23, 0.018), strength=5.0)
amber_glass = material("M_Amber_Glass", (0.86, 0.30, 0.025), roughness=0.22,
                       emission=(1.0, 0.18, 0.01), strength=2.6, alpha=0.42)
blue = material("M_Hueco_Blue", (0.025, 0.075, 0.38), roughness=0.30,
                emission=(0.025, 0.10, 1.0), strength=3.2, alpha=0.28)

anchor_col = collection("KIT_Anchor_Basalto")
flashlight_col = collection("KIT_Linterna")
gate_col = collection("KIT_Reja_Hueco")
bridge_col = collection("KIT_Puente_Cuerpo")
architecture_col = collection("KIT_Arquitectura")
motifs_col = collection("KIT_Motivos_Tallados")

create_anchor(anchor_col, basalt_dark, basalt_mid, amber)
create_flashlight(flashlight_col, metal, worn, amber)
create_gate(gate_col, basalt_dark, basalt_mid, blue)
create_bridge(bridge_col, amber_glass, amber)
create_architecture(architecture_col, basalt_dark, stone)
create_motifs(motifs_col, amber)

for col in (anchor_col, flashlight_col, gate_col, bridge_col, architecture_col, motifs_col):
    for obj in col.objects:
        obj["crater_asset"] = col.name
        obj["unity_scale_meters"] = 1.0

export_collection(anchor_col, "SM_Anchor_Basalto.fbx")
export_collection(flashlight_col, "SM_Linterna.fbx")
export_collection(gate_col, "SM_Reja_Hueco.fbx")
export_collection(bridge_col, "SM_Puente_Cuerpo.fbx")
export_collection(architecture_col, "SM_Arquitectura_Modular.fbx")
export_collection(motifs_col, "SM_Motivos_Tallados.fbx")

preview_col = collection("PREVIEW")
duplicate_for_preview(anchor_col, preview_col, (-4.5, -0.2, 0.0), scale=1.05)
duplicate_for_preview(gate_col, preview_col, (0.0, 2.9, 0.0), scale=0.82)
duplicate_for_preview(flashlight_col, preview_col, (4.2, -0.4, 0.15), rotation=(0, 0, -0.10), scale=0.90)
duplicate_for_preview(architecture_col, preview_col, (0.0, 6.3, 0.0), scale=0.76)
duplicate_for_preview(motifs_col, preview_col, (0.0, 6.0, 0.50), scale=0.70)
duplicate_for_preview(bridge_col, preview_col, (0.0, -5.3, 0.04), scale=0.72)

for source_col in (anchor_col, flashlight_col, gate_col, bridge_col, architecture_col, motifs_col):
    for source_obj in source_col.objects:
        source_obj.hide_render = True

ground = cube("Preview_Ground", (0, 2.8, -0.18), (8.5, 7.0, 0.18), basalt_dark, preview_col, bevel_amount=0.0)

bpy.ops.object.light_add(type='AREA', location=(-4.5, -3.5, 7.0))
key = bpy.context.object
key.name = "Preview_AmberKey"
key.data.energy = 1150
key.data.color = (1.0, 0.25, 0.045)
key.data.shape = 'DISK'
key.data.size = 5.0
look_at(key, (0, 2.5, 1.5))
move_to_collection(key, preview_col)

bpy.ops.object.light_add(type='AREA', location=(5.5, 1.0, 6.0))
fill = bpy.context.object
fill.name = "Preview_BlueFill"
fill.data.energy = 1350
fill.data.color = (0.035, 0.10, 1.0)
fill.data.size = 4.0
look_at(fill, (0, 3.2, 1.6))
move_to_collection(fill, preview_col)

bpy.ops.object.light_add(type='AREA', location=(0, 8.5, 8.0))
rim = bpy.context.object
rim.name = "Preview_Rim"
rim.data.energy = 800
rim.data.color = (0.55, 0.62, 1.0)
rim.data.size = 3.0
look_at(rim, (0, 3.0, 1.8))
move_to_collection(rim, preview_col)

bpy.ops.object.camera_add(location=(12.8, -15.8, 9.3))
camera = bpy.context.object
camera.name = "Preview_Camera"
camera.data.lens = 51
look_at(camera, (0, 3.1, 1.8))
move_to_collection(camera, preview_col)
bpy.context.scene.camera = camera

world = bpy.context.scene.world or bpy.data.worlds.new("CRATER_World")
bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.001, 0.0015, 0.003, 1.0)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.035

scene = bpy.context.scene
scene["reference_direction"] = "Low-poly basaltico, ritual andino abstracto, luz ambar CUERPO y azul HUECO"
scene["reference_images"] = "ancla de basalto apagada.png; campo abierto.png; la cresta.png; idea de sala.png; linterna.png; motivo tallado para muros.png; poster.png; puente de luz.png; reja tapa del filtro.png"
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = PREVIEW_PATH
scene.render.film_transparent = False
scene.render.image_settings.color_mode = 'RGBA'
scene.view_settings.look = 'AgX - Medium High Contrast'

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.render.render(write_still=True)
print(f"CRATER_BLEND={BLEND_PATH}")
print(f"CRATER_PREVIEW={PREVIEW_PATH}")
print(f"CRATER_EXPORTS={EXPORT_DIR}")
