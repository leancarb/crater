# CRÁTER — vertical slice

Exploración en primera persona dentro de un cráter. Desde una capilla, un
eclipse revela un cráter en el valle que sólo existe mientras dura la totalidad.
Adentro, la linterna es la única herramienta: con luz blanca abre el Umbral, con
el filtro **SOL** enciende anclas de basalto que tienden puentes de luz, y con
el filtro **LUNA** disuelve rejas. Al final hay que apagarla y dejar que los ojos se
acostumbren a la oscuridad: así se abre el techo y entra la luz del fin del eclipse.

Unity 6000.3 · URP (Forward+) · Input System.

## Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Correr (mantener) | Shift | apretar el stick izq. |
| Linterna | F | RB |
| Filtro SOL / LUNA | 1 / 2 | X / Y |
| Luz blanca | Q | B |
| Pausa y opciones (W / S elegir, A / D cambiar; R reinicia, X sale) | Esc | Start |
| Saltar la cinemática (después de verla una vez) | Espacio | — |

## Recorrido

| Sala | Qué enseña |
|---|---|
| 00 Capilla (prólogo) | De día. Al salir del atrio, el eclipse abre el cráter en el valle: un pozo con una puerta en el borde |
| 01 Explanada | El fondo del pozo: se baja por la escalera de la puerta, sin cortes; un pasillo con murales del lore sigue al Umbral |
| 02 Umbral | Murales que enseñan cada control sin texto, en el momento en que se usa; sostener la luz sobre un ancla abre la compuerta |
| 03 Rotonda | Sala circular; las alas se entran por la mitad de cada costado, en cualquier orden. Oeste (filtro SOL): puente, puerta de dos anclas, anclas en el techo. Este (filtro LUNA): muro de rejas, trampilla al piso de abajo, escotilla en el techo. Cada una termina en un sello que abre un atajo |
| 04 Cruce | Con los dos sellos se abre el norte: un ancla detrás de una reja, y un puente que termina contra una reja |
| 05 Cresta | Se cierra a la espalda. La pared del fondo sólo refleja el propio foco. Apagar la linterna y adaptarse: aparecen tallados ocultos y se abre el techo |
| 06 Capilla (epílogo) | La luz del techo lo inunda todo: anillo de diamante, blanco y la capilla de día. El cráter ya no está; quedarse donde estaba trae los créditos |

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
- `Assets/Editor/ConstructorCrater.Facetado.cs` — la estética low-poly: caras planas, superficies irregulares y un tono por cara. El tamaño de las caras y el relieve de cada zona están arriba de todo, en los `Perfil`.
- `Assets/Editor/ConstructorCrater.Pozo.cs` — el cráter como pozo en el valle (la entrada sin cortes).
- `Assets/Editor/ConstructorCrater.Alas.cs` — la Rotonda, las dos alas y el Cruce: el corazón del recorrido.
- `Assets/Editor/ConstructorCrater.Tallados.cs` — los tallados de sol y luna, los hilos de los sellos y el arte de las alas.
- `Assets/Editor/ConstructorCrater.Murales.cs` — los murales sin texto: el lore del pasaje y los controles del Umbral.
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
