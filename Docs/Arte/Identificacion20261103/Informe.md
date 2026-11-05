# Identificación de imágenes — 3/11/2026

Se revisaron visualmente las 33 imágenes de las carpetas que contienen las incorporaciones recientes:

- Tutorial: `C:/Users/carbo/Desktop/crater/images/tutorial` (14 PNG).
- Historia: `C:/Users/carbo/Desktop/crater/images/historia` (19 PNG).

Las rutas del pedido eran marcadores sin completar; se utilizaron estas carpetas del proyecto de trabajo conocido. La selección se hizo por contenido, sin deducir el número a partir del nombre inglés ni del orden de archivos. Se copiaron **14 tutoriales y 15 imágenes de historia** con los nombres pedidos. Los originales se conservan, sin modificar píxeles, proporciones, colores, alfa ni compresión. Las copias son idénticas byte por byte.

Este paso sólo organiza el arte. No cambia constructores, materiales, escena, importadores ni asignaciones que usa actualmente el juego. No se ejecutaron pruebas de Unity ni se hizo commit, push o check-in de Unity Version Control en este paso.

## Casos pendientes: no se asignó un nombre por aproximación

| Destino solicitado | Estado | Evidencia |
|---|---|---|
| Tutorial `05_filtro_sol.png` | Falta una coincidencia | No hay un dibujo de UNA sola ancla y puente naranja. `Lantern Light Builds a Bridge.png` muestra DOS anclas juntas y se asigna únicamente a `08_dos_anclas`. `Lantern Activates the Orange Bridge.png` tiene dos anclas en el techo y corresponde a `15_anclas_techo`. |
| Historia `01_sol_y_luna.png` | Candidata con diferencia explícita | `Andean Observatory Under Sun and Moon.png` muestra sol y luna separados sobre la estación, pero el viajero lleva un farol; el pedido dice sin farol. No se copió bajo ese nombre. |
| Historia `02_eclipse_estacion.png` | Sin coincidencia exacta; dos candidatas aproximadas | `Andean Eclipse Observation Station.png` y `Solar Eclipse Observation Station.png` muestran eclipse y estación, pero ambos viajeros llevan farol. La segunda además tiene un círculo en el piso. No se eligió una. |
| Historia `03_crater_se_abre.png` | Ambigua | `Solar Eclipse Observation Station.png` muestra un círculo/aro con rayos en el suelo frente a la estación. `Lantern Light Reveals Ancient Crater Glyphs.png` muestra un pozo circular abierto delante de la estación. Ambas pueden corresponder al tema; no se eligió una. |
| Historia `04_bajada.png` | Falta una coincidencia | Ninguna muestra claramente al viajero bajando una escalera con sol y luna en sus paredes. En `Lantern Light Reveals Ancient Crater Glyphs.png` está parado junto al borde iluminando un pozo con esos símbolos; no se ve un descenso por escalera. |

Los siguientes cuatro originales de historia quedaron **sin asignar** por esos motivos:

- `Andean Observatory Under Sun and Moon.png`.
- `Andean Eclipse Observation Station.png`.
- `Solar Eclipse Observation Station.png`.
- `Lantern Light Reveals Ancient Crater Glyphs.png`.

No se crearon archivos vacíos ni sustitutos para los cinco destinos pendientes. No hay dos candidatas entre las asignaciones aceptadas ni se reutilizó un mismo original para dos nombres.

## Tutorial: copias confirmadas

| Nombre nuevo | Original identificado |
|---|---|
| `01_moverse.png` | `Directional Figure Icon Sign.png` |
| `02_saltar.png` | `Jump Over the Obstacle Icon.png` |
| `03_recoger.png` | `Ancient Artifact Pickup Icon.png` |
| `04_farol.png` | `Lantern Bearer with Light Beam.png` |
| `06_filtro_luna.png` | `Lantern Beam Breaking Through Barriers.png` |
| `07_luz_blanca.png` | `Lantern Beam Keycap Journey.png` |
| `08_dos_anclas.png` | `Lantern Light Builds a Bridge.png` |
| `09_correr.png` | `Lantern Runner Approaches the Bridge.png` |
| `10_combinar.png` | `Lantern Puzzle Bridge Diagram.png` |
| `11_sala_final.png` | `Lantern Off, Eye Watching.png` |
| `12_dos_anclas_separadas.png` | `Lantern Guide Across the Chasm.png` |
| `13_ancla_puerta.png` | `Lantern Beam Activates Lowering Gate.png` |
| `14_mirar_alrededor.png` | `Head-Turning Pathway Icon.png` |
| `15_anclas_techo.png` | `Lantern Activates the Orange Bridge.png` |

## Historia: copias confirmadas

| Nombre nuevo | Original identificado |
|---|---|
| `05_puente.png` | `Andean Canyon Bridge Traveler.png` |
| `06_puerta.png` | `Ancient Portal Beneath Sun and Moon.png` |
| `07_puerta_abierta.png` | `Eclipse Gateway and Lantern Traveler.png` |
| `08_regreso.png` | `Andean Traveler at the Sun Station.png` |
| `09_farol.png` | `Andean Temple Lantern and Eclipse.png` |
| `10_dos_caminos.png` | `Sun and Moon Stepped Emblem.png` |
| `11_puerta_sellos.png` | `Ancient Eclipse Doorway with Lantern Bearer.png` |
| `12_ciclo.png` | `Celestial Cycle Over Observation Station.png` |
| `13_ala_sol.png` | `Ancient Observatory Solar Study.png` |
| `14_ala_luna.png` | `Moonlit Observatory Watchers.png` |
| `15_cruce.png` | `Andean Trail Under Sun and Moon.png` |
| `16_grutas.png` | `Ancient Cave Feast Pictogram.png` |
| `16b_grutas_reparacion.png` | `Andean Stoneworker Repairing Cave Floor.png` |
| `16c_grutas_descanso.png` | `Andean Cave Counting Scene.png` |
| `17_caida.png` | `Cavefall, Lantern, and Ascent.png` |

## Comprobación y referencias

Los tutoriales son 1448 × 1086 (4:3) y las imágenes de historia 1774 × 887 (2:1). Se verifican dimensiones y SHA-256 en el [inventario de correspondencias](Correspondencias.tsv). El [catálogo de originales](Originales.json) registra las 33 huellas previas, y el [manifiesto de copias](Copias.json) contiene los 29 destinos.

Vistas de revisión con cada archivo identificado: [tutorial](Tutorial.jpg) e [historia](Historia.jpg). Son láminas de consulta; no son reemplazos de los archivos fuente.
