# CRATER · Apertura en Blender (referencia: intro de *Silo*)

Secuencia de apertura de unos 58 s, renderizada en Blender 5.2 (Cycles) con los assets del proyecto. Sigue la
estructura de la intro de *Silo*: cielo, picada al pozo, escalera con figuras de arena, paleta teal, espiral dorada,
timelapse, mecanismo en luz roja, corte del recorrido y el título metálico, con créditos chicos en las esquinas.

| Plano | Escena base | Qué pasa |
|---|---|---|
| `s1` | `CRATER_Kit_Exterior_1` · `Scene` | Cielo ocre con nubes; la luna tapa el sol detrás de la capilla, sale la corona y la cámara sube hasta quedar cenital sobre el cráter |
| `s2` | `CRATER_Kit_Modular_Exterior` · `01 · Montaje exterior` | Picada cenital girando dentro del pozo; el forro de piedra se deshace en polvo dorado |
| `s3` | `CRATER_Kit_Exterior_1` · `DEMO_Entrada_Rectangular` | Una figura de arena baja la escalera con la linterna y se deshace; la cámara entra al túnel de franjas cian y la reja de HUECO (`KIT_Reja_Hueco`) se disuelve en polvo azul |
| `s5` | `CRATER_Kit_Modular` · `KIT_Motivos_Tallados` | La espiral tallada, cenital, girando y soltando trazos de oro |
| `s6` | `CRATER_Kit_Modular_Exterior` | Timelapse del valle: los cardones brotan mientras el sol cruza el cielo |
| `s7` | `CRATER_Kit_Modular_Exterior` | El dial de la estación de observación bajo una luz roja que gira |
| `s8` | `CRATER_Mapa_01` | El recorrido completo brillando dentro del terreno en alambre, como una radiografía |
| `s9` | `CRATER_Kit_Exterior_1` · `KIT_Ext_Eclipse` | La corona gira como un rotor, el anillo de diamante crece hasta llenar el cuadro y aparece el título |

## Requisitos

- Python 3.13 con `bpy` 5.2 (`pip install bpy`), además de `numpy`, `scipy`, `pillow` e `imageio-ffmpeg`.
- `Assets/Art/Blender/CRATER_Kit_Modular.blend` y `CRATER_Mapa_01.blend` (ya están en el repo).
- `CRATER_Kit_Exterior_1.blend` y `CRATER_Kit_Modular_Exterior.blend`: no están versionados. Ponelos en la misma
  carpeta o indicá dónde están con `CRATER_EXTERIOR_DIR`.
- Fuentes (OFL) en `Trailer/blender/fonts/` o en `CRATER_FONTS`: `Cinzel-600.ttf`, `IBMPlexSans-300.ttf`,
  `IBMPlexSans-500.ttf`, `Italiana-400.ttf`.

## Uso

```bash
cd Trailer/blender
# cuadros de prueba de un plano (50 % de resolución, 8 muestras)
python -I crater_intro.py s3 prueba --frames 1,150,270 --pct 50 --samples 8
# todos los planos a 960x540 (unas 2 h en 4 núcleos de CPU); se puede cortar y reanudar
./render_all.sh render s1 s2 s3 s5 s6 s7 s8 s9
# música sintetizada y montaje final a 1080p
python -I post.py music musica.wav
python -I post.py video render crater_apertura.mp4 --wav musica.wav
```

`post.py` une los planos con fundidos, agrega bloom, grano y viñeta, escala a 1920×1080, superpone los créditos
de esquina (salas y filtros del GDD) y el «PRÓXIMAMENTE» final, y codifica en H.264 + AAC. La música está
sintetizada con numpy: un drone en Re, un ostinato de cuerdas que crece (i–VI–III–VII), golpes graves en los
cortes, el tic-tac del dial y una subida al anillo de diamante que termina en el título.

## Segunda apertura: solo con los cuatro kits

`crater_v2.py` arma otra versión (unos 56 s) usando **solo** las piezas de `CRATER_Kit_Crater`, `CRATER_Kit_Exterior`,
`CRATER_Kit_Interior` y `CRATER_Kit_Terrenos_y_Encuentros`. El cielo, el eclipse, las luces y el polvo son procedurales.
Esos cuatro `.blend` tampoco están versionados: ponelos en `Assets/Art/Blender/` o indicá la carpeta con `CRATER_KITS_DIR`.

| Plano | Piezas | Qué pasa |
|---|---|---|
| `v1` | Terreno y lomas, estación E01–E10, utilería, rocas y cardones | La estación en el valle; la veleta gira y la luna tapa el sol |
| `v2` | Taludes y bermas, fondo y monolitos del cráter | Picada cenital al embudo; los taludes se deshacen en polvo dorado |
| `v3` | Cuadros `Historia_02`–`05` | Travelling lateral por los cuadros de historia, con la luz del farol pasando |
| `v4` | Rotonda, pedestal, farol, columnas SOL/LUNA | La Rotonda a oscuras con el farol; se encienden los aros y los sellos |
| `v5` | Anclas de basalto, puente de cuerpo, paños SOL | El haz ámbar enciende las anclas y el puente se arma panel por panel |
| `v6` | Reja LUNA, paños LUNA, arco | La placa azul se disuelve y la cámara la atraviesa |
| `v7` | Paños de la Cresta, aro de piso, hojas de piedra, farol | El farol se apaga, brillan los tallados y el óculo se abre en luz blanca |
| `v8` | Estación y valle | La estación al atardecer; título y «PRÓXIMAMENTE» |

```bash
./render_v2.sh render2 v1 v2 v3 v4 v5 v6 v7 v8
python -I post_v2.py music musica_v2.wav
python -I post_v2.py video render2 crater_apertura_kits.mp4 --wav musica_v2.wav
```
