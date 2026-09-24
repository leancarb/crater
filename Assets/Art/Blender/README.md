# CRÁTER — kit modular inicial

`CRATER_Kit_Modular.blend` es la primera pasada de modelado basada en las nueve
referencias visuales compartidas. Mantiene una escala de trabajo de 1 unidad =
1 metro y separa las piezas por colecciones para poder iterarlas sin afectar el
recorrido jugable.

## Colecciones

- `KIT_Anchor_Basalto`: ancla facetada con núcleo y anillos emisivos.
- `KIT_Linterna`: linterna low-poly con cuerpo, lente, interruptor y aro trasero.
- `KIT_Reja_Hueco`: marco, entramado irregular y plano azul de disolución.
- `KIT_Puente_Cuerpo`: seis módulos transitables con diagonales y bordes emisivos.
- `KIT_Arquitectura`: muro, pilares facetados y óculo modular.
- `KIT_Motivos_Tallados`: espiral, serpiente escalonada, chakana y rombo.

Los FBX exportados están en `Assets/Models/CraterKit/`. La colección `PREVIEW`
es sólo de presentación y no forma parte de esos FBX.

## Estado

Esta pasada define silueta, proporciones, pivotes, nombres y separación de
materiales. La siguiente iteración debería concentrarse en desgaste, variantes
de pilares y muros, UVs definitivas y adaptación de cada pieza al graybox tras
un playtest del recorrido.

El archivo puede regenerarse ejecutando `Tools/Blender/crear_kit_crater.py` con
Blender 5.2.
