# CRÁTER — vertical slice

Exploración en primera persona dentro de un cráter. La linterna es la única
herramienta: con luz blanca abre el Umbral, con el filtro **CUERPO** enciende
anclas de basalto que tienden puentes de luz, y con **HUECO** disuelve rejas.
Al final hay que apagarla y dejar que los ojos se acostumbren a la oscuridad.

Unity 6000.3 · URP (Forward+) · Input System.

## Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Linterna | F | RB |
| Filtro CUERPO / HUECO | 1 / 2 | X / Y |
| Luz blanca | Q | B |
| Pausa (R reinicia, X sale) | Esc | Start |

## Recorrido

| Sala | Qué enseña |
|---|---|
| 01 Explanada | Inicio al aire libre; rampa hacia abajo |
| 02 Umbral | Recoger la linterna; sostener la luz sobre un ancla abre la compuerta |
| 03 Campo | CUERPO: tres puentes (enseñar, barrer con retención, mirar hacia arriba) |
| 04 Hondonada | HUECO: rejas y zigzag; al final, combinar los dos filtros desde una repisa |
| 05 Cresta | Apagar la linterna y adaptarse: aparecen tallados ocultos |
| 06 Capilla | Epílogo de día |

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
