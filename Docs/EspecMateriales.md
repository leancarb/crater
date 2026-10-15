# CRÁTER — Especificación de materiales y emisión (v2, 14/10/2026)

Fuente: GDD §5 «Lo que brilla, se usa» + valores reales leídos de Assets/Materials el 14/10/2026.
IMPORTANTE: los materiales los crea `Assets/Editor/ConstructorCrater.cs` (y parciales). Cualquier
cambio se hace en el constructor, no en los .mat, o se pisa en la próxima generación.
Colores en sRGB (como los muestra el Inspector).

## 1. Lo que ya cumple (no tocar)
| Material | Valor actual | Nota |
|---|---|---|
| BasaltoOscuro | #0F1114 | coincide con basalto base |
| BasaltoMedio | #1F2025 | coincide con basalto medio |
| PisoBasalto | #1A1A1C | ok |
| PinturaHueso / PinturaSol / PinturaLuna | #948C7A / #A8541C / #6180C2, sin emisión | mate, cumple la regla |
| Ancla, puente (Ambar, PuenteBorde, PuenteVidrio) | ámbar #E66B1A, emisión ~1.6 | funcional, ok |

## 2. Cambios propuestos
| # | Dónde | Hoy | Propuesta | Por qué |
|---|---|---|---|---|
| 1 | Luz_Mural (Murales.cs ~l.121) | intensidad 6×ancho | 2×ancho, rango igual | la pintura mate se lee como emisiva por la luz dedicada |
| 2 | Luz_Murales_Oeste / Este (Murales.cs l.87-88) | 22 / 26 | 10 / 12 | ídem |
| 3 | Pintura_Encuadre (Exploracion.cs l.197) | _EMISSION activo, ×0.01 | sin Emisivo(...) | decorativo: no debe tener emisión, aunque sea mínima |
| 4 | RejaBasalto | base #99BDFF α0.55, emisión (0.36,0.44,0.6) | base #4E8CFF α0.55, emisión #2A4FA8 ×1.5 | hoy lee lila/lavanda con ACES |
| 5 | TalladoLuna | base (0.36,0.44,0.6), emisión ×1.8 | base #2A4FA8, emisión #4E8CFF ×1.5 | mismo corrimiento a lila |
| 6 | PinturaSol | #A8541C | #9A5A2A (opcional) | menos saturado, no vira a rojo bajo luz cálida |

## 3. Decisiones pendientes (no implementar sin confirmar)
- Tallados del Umbral con TalladoEclipse (Nivel.cs l.225, emisión ×2): hoy brillan a propósito. Mantener o pasar a PinturaHueso.
- Murales del pasaje: DECIDIDO 14/10/2026, se reemplazan por PNG (ver sección 5).

## 4. Verificación
1. Correr el constructor y confirmar que los .mat generados tienen los valores de la sección 2.
2. Bloom Threshold 1.0: sólo deben hacer bloom anclas, sellos, puente, rejas, obelisco, faros, pebeteros, menhires y tallados latentes.
3. Capturas sin menú de pausa del pasaje de la Explanada, el Umbral y una reja, para la auditoría.

## 5. Reemplazo de los murales del pasaje por PNG (decidido 14/10/2026)
Archivos: `Assets/Art/Murals/mural_explanada_*.png`, 1728×1152 (3:2), 6 colores exactos de la paleta, sin emisión.

Mapeo en `ConstructorCrater.Murales.cs`, `ConstruirMurales` (l.44-54):
| Losa | Escena procedural actual | PNG |
|---|---|---|
| Lore_1 (oeste) | LoreSolYLuna | mural_explanada_izq_01 |
| Lore_2 (oeste) | LoreEclipse | mural_explanada_izq_02 |
| Lore_3 (oeste) | LoreCrater | mural_explanada_izq_03 |
| Lore_4 (oeste) | LoreLuz | mural_explanada_izq_04 |
| Lore_5 (este) | LoreAnclas | mural_explanada_der_01 |
| Lore_6 (este) | LoreSellos | mural_explanada_der_02 |
| Lore_7 (este) | LorePuerta | mural_explanada_der_03 |
| Lore_8 (este) | LoreVuelta | mural_explanada_der_04 |

Reglas:
- Se conserva el bloque de piedra de cada losa, su posición y su orientación. Sólo cambia la cara pintada: un quad con un material mate nuevo `PinturaMural` (URP/Lit, Base Map = PNG, Smoothness 0.05, Metallic 0, sin emisión).
- Proporción: la losa hoy mide 2 × 1.4 m (1.43:1); el PNG es 3:2. Ancho de la losa 2.1 m, alto 1.4 m. No estirar la textura.
- Import de los PNG: sRGB activado, Mip Maps activados, Wrap Clamp, Filter Bilinear, Max Size 2048, Compression High Quality.
- No borrar las funciones Lore*: dejarlas sin usar (o detrás de un flag) para poder volver atrás.
- Luz_Pasaje (l.54, intensidad 34): bajar a 14 y revisar en captura; la textura ya trae el contraste.
- Validador y tests: actualizar lo que cuente mallas `Pintura_*` en Lore_*; correr EditMode y PlayMode.
