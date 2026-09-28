"""Prepara un mapa de layout hecho a mano para el proyecto CRÁTER.

Toma el .blend de origen (no lo modifica), lo ordena con las convenciones del
kit modular y guarda:

- Assets/Art/Blender/CRATER_Mapa_01.blend
- Assets/Models/CraterMapa/SM_Mapa01_*.fbx   (una por colección MAPA_*)
- Assets/Art/Previews/CRATER_Mapa_01.png

Uso:
    blender -b "ruta/al/Crater Mapa 01.blend" --python Tools/Blender/preparar_mapa_crater.py
"""
import bpy
import bmesh
import os
from mathutils import Matrix, Vector


ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(ROOT, "Assets", "Art", "Blender", "CRATER_Mapa_01.blend")
PREVIEW_PATH = os.path.join(ROOT, "Assets", "Art", "Previews", "CRATER_Mapa_01.png")
EXPORT_DIR = os.path.join(ROOT, "Assets", "Models", "CraterMapa")

# Espesor que se da a las piezas de una sola cara para que tengan dorso y
# colisión confiable en Unity.
ESPESOR_PISO = 0.3
ESPESOR_MURO = 0.4
ESPESOR_TUNEL = 0.3


def material(name, color, roughness=0.65):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Metallic"].default_value = 0.0
        bsdf.inputs["Roughness"].default_value = roughness
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


def set_material(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def select_only(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


def apply_transforms(obj):
    """Aplica rotación y escala; deja la ubicación como pivote de la pieza.

    Se hace sobre la malla y no con el operador porque éste ignora objetos
    de colecciones ocultas en la capa de vista (el Templo lo estaba).
    """
    if obj.data.users > 1:
        obj.data = obj.data.copy()
    loc = obj.matrix_basis.to_translation()
    obj.data.transform(Matrix.Translation(-loc) @ obj.matrix_basis)
    obj.matrix_basis = Matrix.Translation(loc)


def origin_to_base(obj):
    """Pivote en el centro de la base de la pieza, como en el kit."""
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    base = Vector((
        (min(p.x for p in pts) + max(p.x for p in pts)) / 2,
        (min(p.y for p in pts) + max(p.y for p in pts)) / 2,
        min(p.z for p in pts),
    ))
    offset = obj.matrix_world.inverted() @ base
    obj.data.transform(Matrix.Translation(-offset))
    obj.location = base


def solidify(obj, thickness, offset):
    mod = obj.modifiers.new("Espesor", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = offset
    mod.use_even_offset = True
    select_only([obj])
    bpy.ops.object.modifier_apply(modifier=mod.name)


def recalc_normals(obj, inside=False):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if inside:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()


def remove_floor_faces(obj, z=0.0, tol=0.01):
    """Quita caras horizontales a la altura z (se superponen con otro piso)."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    mw = obj.matrix_world
    dead = [f for f in bm.faces
            if abs((mw.to_3x3() @ f.normal).normalized().z) > 0.99
            and all(abs((mw @ v.co).z - z) < tol for v in f.verts)]
    bmesh.ops.delete(bm, geom=dead, context='FACES')
    bm.to_mesh(obj.data)
    bm.free()
    return len(dead)


def join(objs, name):
    select_only(objs)
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name
    obj.data.name = name
    return obj


def rename(old, new):
    obj = bpy.data.objects[old]
    obj.name = new
    obj.data.name = new
    return obj


def export_collection(col, filename):
    mesh_objects = [o for o in col.objects if o.type == 'MESH']
    if not mesh_objects:
        return
    select_only(mesh_objects)
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


# --- Limpieza -------------------------------------------------------------

# Pruebas de material y su luz: no forman parte del mapa.
for obj in list(bpy.data.collections["prueba materiales"].objects):
    bpy.data.objects.remove(obj, do_unlink=True)
for obj in [o for o in bpy.data.objects if o.type != 'MESH']:
    bpy.data.objects.remove(obj, do_unlink=True)

# Textura externa (../texturas) que no viaja con el proyecto.
for img in list(bpy.data.images):
    bpy.data.images.remove(img)

# --- Materiales (misma paleta que CRATER_Kit_Modular) ---------------------

basalt_dark = material("M_Basalt_Dark", (0.018, 0.022, 0.030), roughness=0.78)
basalt_mid = material("M_Basalt_Mid", (0.055, 0.065, 0.085), roughness=0.72)
stone = material("M_Stone_Edge", (0.16, 0.15, 0.14), roughness=0.88)
ash = material("M_Terreno_Ceniza", (0.045, 0.043, 0.042), roughness=0.95)

# --- Nombres, geometría y materiales --------------------------------------

terreno = rename("exterior", "Terreno_Crater")
set_material(terreno, ash)

# Recorrido principal, en el orden en que se camina.
escalones = [bpy.data.objects[f"Cube.{i:03d}"] for i in range(1, 46)]
for o in escalones:
    apply_transforms(o)
escalera = join(escalones, "Escalera_Descenso")
set_material(escalera, basalt_mid)

plaza_piso = rename("Plane", "Plaza_Piso")
plaza_muros = rename("Plane.001", "Plaza_Muros")
remove_floor_faces(plaza_muros, z=0.0)  # duplicaba Plaza_Piso (z-fighting)

tunel = rename("Circle.003", "Tunel_Plaza_Anillo")
anillo_piso = rename("Circle", "Anillo_Piso")
columnas = ["Cylinder.003", "Cylinder.006", "Cylinder.002", "Cylinder.005", "Cylinder.004", "Cylinder.007"]
columnas = [rename(old, f"Anillo_Columna_{i + 1:02d}") for i, old in enumerate(columnas)]
obelisco = rename("Cube.050", "Anillo_Obelisco")

terraza_a = rename("Plane.002", "Terraza_A_Piso")
terraza_b = rename("Plane.003", "Terraza_B_Piso")
# Cube.046/047 son el escalón; Cube.048/049 son sus contrahuellas de una cara.
for n in ("Cube.046", "Cube.047", "Cube.048", "Cube.049"):
    apply_transforms(bpy.data.objects[n])
for n in ("Cube.048", "Cube.049"):
    solidify(bpy.data.objects[n], ESPESOR_PISO, 0.0)
escalon_ab = join([bpy.data.objects[n] for n in ("Cube.046", "Cube.047", "Cube.048", "Cube.049")],
                  "Terraza_AB_Escalon")

tunel_norte = rename("Circle.001", "Tunel_Norte")

templo = rename("Cube", "Templo_Cuerpo")
templo_pilares = [rename("Cylinder", "Templo_Pilar_A"), rename("Cylinder.001", "Templo_Pilar_B")]

for obj in [plaza_piso, plaza_muros, tunel, anillo_piso, *columnas, obelisco, terraza_a, terraza_b,
            escalon_ab, tunel_norte, templo, *templo_pilares]:
    apply_transforms(obj)

# Piezas de una sola cara: se les da espesor.
for piso in (plaza_piso, anillo_piso, terraza_a, terraza_b):
    solidify(piso, ESPESOR_PISO, -1.0)  # normal hacia arriba: crece hacia abajo
solidify(plaza_muros, ESPESOR_MURO, 0.0)
for t in (tunel, tunel_norte):
    recalc_normals(t)
    solidify(t, ESPESOR_TUNEL, 0.0)

for obj in (plaza_piso, anillo_piso, terraza_a, terraza_b, escalon_ab):
    set_material(obj, basalt_mid)
for obj in (plaza_muros, tunel, tunel_norte, *columnas):
    set_material(obj, basalt_dark)
for obj in (obelisco, templo, *templo_pilares):
    set_material(obj, stone)

for obj in bpy.data.objects:
    if obj.type == 'MESH' and obj is not terreno:
        origin_to_base(obj)

# --- Colecciones ----------------------------------------------------------

old_collections = list(bpy.data.collections)
col_terreno = collection("MAPA_Terreno")
col_recorrido = collection("MAPA_Recorrido")
col_templo = collection("MAPA_Templo")

move_to_collection(terreno, col_terreno)
for obj in (escalera, plaza_piso, plaza_muros, tunel, anillo_piso, *columnas, obelisco,
            terraza_a, terraza_b, escalon_ab, tunel_norte):
    move_to_collection(obj, col_recorrido)
for obj in (templo, *templo_pilares):
    move_to_collection(obj, col_templo)
for col in old_collections:
    bpy.data.collections.remove(col)

bpy.ops.outliner.orphans_purge(do_recursive=True)

# --- Guardar y exportar ---------------------------------------------------

os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
os.makedirs(os.path.dirname(PREVIEW_PATH), exist_ok=True)
os.makedirs(EXPORT_DIR, exist_ok=True)

bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH, compress=True)

export_collection(col_terreno, "SM_Mapa01_Terreno.fbx")
export_collection(col_recorrido, "SM_Mapa01_Recorrido.fbx")
export_collection(col_templo, "SM_Mapa01_Templo.fbx")

# --- Vista previa (no se guarda en el .blend) -----------------------------

scene = bpy.context.scene
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
cam.location = (185, -70, 70)
cam.rotation_euler = (Vector((70, 15, 0)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam
terreno.hide_render = True  # hoy tapa la plaza; se ve el recorrido solo
scene.render.filepath = PREVIEW_PATH
bpy.ops.render.render(write_still=True)
