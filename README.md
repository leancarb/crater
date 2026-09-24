# CRÁTER — vertical slice

Exploración en primera persona dentro de un cráter. Desde una capilla, un
eclipse revela un cráter en el valle que sólo existe mientras dura la totalidad.
Adentro, la linterna es la única herramienta: con luz blanca abre el Umbral, con
el filtro **CUERPO** enciende anclas de basalto que tienden puentes de luz, y con
**HUECO** disuelve rejas. Al final hay que apagarla y dejar que los ojos se
acostumbren a la oscuridad: así aparece la puerta de salida.

Unity 6000.3 · URP (Forward+) · Input System.

## Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Linterna | F | RB |
| Filtro CUERPO / HUECO | 1 / 2 | X / Y |
| Luz blanca | Q | B |
| Pausa (R reinicia, X sale) | Esc | Start |
| Saltar la cinemática (después de verla una vez) | Espacio | — |

## Recorrido

| Sala | Qué enseña |
|---|---|
| 00 Capilla (prólogo) | De día. Al salir, el eclipse revela el cráter en el valle; su puerta lleva a la Explanada |
| 01 Explanada | Borde del cráter, bajo la totalidad; rampa hacia abajo |
| 02 Umbral | Recoger la linterna; sostener la luz sobre un ancla abre la compuerta |
| 03 Campo | CUERPO: tres puentes (enseñar, barrer con retención, mirar hacia arriba) |
| 04 Hondonada | HUECO: rejas y zigzag; al final, combinar los dos filtros desde una repisa |
| 05 Cresta | La pared del fondo sólo refleja el propio foco. Apagar la linterna y adaptarse: aparecen tallados ocultos y la puerta del eclipse |
| 06 Capilla (epílogo) | Cruzar la puerta: anillo de diamante, blanco y la capilla de día. El cráter ya no está; quedarse donde estaba trae los créditos |

## Cómo se trabaja

La escena, los prefabs, los materiales, el audio y el post-procesado **se generan
desde código**. No se editan a mano: se cambia el código y se reconstruye.

- **Crater › Reconstruir todo** — regenera todo y valida. Es idempotente.
- **Crater › Validar proyecto** — chequea que cada puzzle esté conectado y al alcance del haz.
- **Crater › Renderizar vistas previas** — guarda capturas en `Assets/Art/Previews/Unity`.
- **Crater › Construir demo Windows** — valida y compila en `Builds/Windows`.

Dónde tocar:

- `Assets/Editor/ConstructorCrater.Nivel.cs` — el nivel: geometría, puzzles, luces y arte, en metros.
- `Assets/Editor/ConstructorCrater.cs` — materiales, prefabs, render y post-procesado.
- `Assets/Editor/GeneradorAudioCrater.cs` — sonidos sintetizados (reemplazables por WAV grabados con el mismo nombre).
- `Assets/Data/Filtros/` — los dos filtros (color, cono, alcance, tiempo de carga).
- `Assets/Scripts/` — el juego: `Jugador`, `Linterna`, `Mecanicas`, `Flujo`, `Interfaz`.

## Tests

Desde **Window › General › Test Runner**:

- **EditMode** — reglas de las mecánicas (carga, retención, puentes, rejas, compuerta, respawn).
- **PlayMode** — recorre la escena real de punta a punta caminando y apuntando la linterna.
  Si una pieza del nivel se mueve y un puzzle deja de poder resolverse, falla.

## Arte

El kit modular está en `Assets/Art/Blender` (ver su README) y se exporta a
`Assets/Models/CraterKit`. El constructor usa esos FBX directamente.
