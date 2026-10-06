# CRÁTER — vertical slice

Exploración en primera persona dentro de un cráter. Desde una estación de observación, un
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
| Pausa / seguir | Esc | Start / Options; B / círculo sigue |
| Reiniciar / salir (en pausa) | R / X | Y / triángulo; X / cuadrado |
| Ayudas escritas (en pausa) | H | Cruceta arriba |
| Saltar animaciones | Espacio | A / cruz |

## Recorrido

| Sala | Qué enseña |
|---|---|
| 00 Estación (prólogo) | Arranque en una loma exterior con vista a la estación. El eclipse avanza al caminar (o en 80 s quieto). En el mirador se abre un pozo con entrada en el suelo; la mirada sigue libre |
| 01 Explanada | El fondo del pozo: se baja por la escalera de la boca del cráter, sin cortes; un pasillo con murales del lore sigue al Umbral |
| 02 Umbral | Murales que enseñan cada control sin texto, en el momento en que se usa; sostener la luz sobre un ancla abre la compuerta |
| 03 Rotonda | Sala circular ampliada a 37,5 m con plaza hundida redonda de 1,2 m y una escalinata anular continua; las alas se entran por la mitad de cada costado, en cualquier orden. Oeste (SOL): puente, puerta permanente y anclas del techo. Este (LUNA): elección entre dos rejas, trampilla y escotilla. Cada una termina en un sello que abre un atajo |
| 04 Cruce | Nicho con ancla cubierta, puente descentrado e isla segura entre dos tramos para observar y combinar filtros |
| 05 Cresta | Se cierra a la espalda. Paredes, piso y techo reflejan el foco, recortado en cada superficie. Apagar la linterna y adaptarse: aparecen tallados ocultos y se abre el techo |
| 06 Estación (epílogo) | La luz del techo lo inunda todo: anillo de diamante, blanco y la estación de día. El cráter ya no está; cruzar su huella trae los créditos |

## Revisión integral

El GDD es un documento vivo. El estado vigente está en **[Docs/EstadoActualCrater.md](Docs/EstadoActualCrater.md)** y la revisión contemplativa del 6/10/2026 en **[Docs/RevisionContemplativa.md](Docs/RevisionContemplativa.md)**. Los documentos de rediseños anteriores y sus capturas son históricos; varias de sus propuestas se revirtieron.

Se conservan los modelos y la paleta restaurados, las alas anteriores y el Cruce nuevo. La primera ancla SOL y la gruta O1 establecen una respuesta suave al haz y un lugar de descanso con tres tallados resonantes opcionales. LUNA conserva dos entradas a ciegas: el desvío cuenta una pequeña historia de reparación; la ruta principal desciende y asciende por rampas protegidas. La rotonda tiene una apertura física desde la que se observa el eclipse exterior. Al regresar de las alas, las cinemáticas muestran sus hilos de luz y la apertura de la puerta; mantienen pausa, salto y devolución segura del control.

El edificio exterior es una estación de observación secular. Los nombres de algunos archivos, grupos y referencias serializadas mantienen `Capilla` por compatibilidad.

## Cómo se trabaja

La escena, los prefabs, los materiales, el audio y el post-procesado **se generan
desde código**. No se editan a mano: se cambia el código y se reconstruye.

- **Crater › Reconstruir todo** — regenera todo y valida. Es idempotente.
- **Crater › Validar proyecto** — chequea que cada puzzle esté conectado y al alcance del haz.
- **Crater › Renderizar cambios del valle** — guarda las vistas de las lomas, mirador, plaza, gruta y umbral de la Cresta en `Docs/Previews`.
- **Crater › Renderizar vistas previas** — guarda capturas en `Assets/Art/Previews/Unity`.
- **Crater › Construir demo Windows** — valida y compila en `Builds/Windows`.

Dónde tocar:

- `Assets/Editor/ConstructorCrater.Nivel.cs` — el nivel: geometría, puzzles, luces y arte, en metros.
- `Assets/Editor/ConstructorCrater.cs` — materiales, prefabs, render y post-procesado.
- `Assets/Editor/ConstructorCrater.ArteNuevo.cs` — geometría de la plaza circular y su escalinata continua.
- `Assets/Editor/ConstructorCrater.Facetado.cs` — la estética low-poly: caras planas, superficies irregulares y un tono por cara. El tamaño de las caras y el relieve de cada zona están arriba de todo, en los `Perfil`.
- `Assets/Editor/ConstructorCrater.Pozo.cs` — el cráter como pozo en el valle (la entrada sin cortes).
- `Assets/Editor/ConstructorCrater.Alas.cs` — la Rotonda, las dos alas y el Cruce: el corazón del recorrido.
- `Assets/Editor/ConstructorCrater.Tallados.cs` — los tallados de sol y luna, los hilos de los sellos y el arte de las alas.
- `Assets/Editor/ConstructorCrater.Murales.cs` — los murales sin texto: el lore del pasaje y los controles del Umbral.
- `Assets/Editor/ConstructorCrater.Exploracion.cs` — óculo circular, seis piezas opcionales, conjunto, cuenco reflectante y detalles de exploración.
- `Assets/Editor/ConstructorCrater.Detalles.cs` — gruta O1, tallados resonantes, desvío lunar y objetos de uso cotidiano.
- `Assets/Editor/ConstructorCrater.Espacio.cs` — ampliación horizontal y protección de las proporciones de objetos y murales.
- `Assets/Editor/GeneradorAudioCrater.cs` — sonidos sintetizados, pasos de piedra, tierra, madera y metal, y bucles. Los WAV personalizados se preservan usando `Assets/Audio/Generado/huellas.json`; las grabaciones de `Assets/Audio/Grabado/` con el mismo nombre tienen prioridad.
- `Assets/Data/Filtros/` — los dos filtros (color, cono, alcance, tiempo de carga).
- `Assets/Scripts/` — el juego: `Jugador`, `Linterna`, `Mecanicas`, `Flujo`, `Interfaz`.

## Tests

Desde **Window › General › Test Runner**:

- **EditMode** — reglas de las mecánicas (carga, retención, puentes, rejas y compuerta).
- **PlayMode** — recorre la escena real de punta a punta caminando y apuntando la linterna.
  Si una pieza del nivel se mueve y un puzzle deja de poder resolverse, falla. Verifica ambos órdenes de las alas, el desvío de LUNA, cuatro recuperaciones, el final, alcance del haz, superficies reflectantes y teclado/mouse/mando con pausa y salto de sellos. Estas pruebas no miden comprensión ni disfrute.

## Arte

El kit modular está en `Assets/Art/Blender` (ver su README) y se exporta a
`Assets/Models/CraterKit`. El constructor usa esos FBX directamente.

## Recuperación al caer

Los cuatro abismos de O1, O3, N1 y N2 terminan en grutas 3 m abajo. Cada gruta tiene luz cálida, un mural propio y una escalera hacia la orilla de partida. La losa al ras del piso tapa la salida desde arriba y se corre al llegar desde abajo; queda abierta. No hay respawn por caída.

Esta copia se trabaja de manera independiente, sin sincronizar con GitHub.

## Editor conectado

Esta copia tiene el puente local `com.unity.pipeline` instalado con autorización. `unity status` comprueba el editor; `unity command --tag tests` expone el runner. Reconstruir desde `Crater > Reconstruir todo`; el perfil visual conserva su GUID entre reconstrucciones.

Antes de la revisión se guardó `Backups/CRATER_antes_revision_integral.zip`, con Assets, Packages, ProjectSettings y documentación de esa versión.

La escena del Editor sigue `Docs/EstadoActualCrater.md`. `Docs/AlasYCruceNuevo.md` conserva una etapa histórica de diseños luego parcialmente revertidos. No se recompiló la demo Windows en esta actualización.

La especificación vigente está en `Docs/EstadoActualCrater.md`: alas anteriores, Cruce nuevo, subsuelo ampliado, eclipse visible y bajada corregida.


La ampliación actual está en `Docs/ExploracionYConjunto.md`: seis piedras opcionales, progreso en pausa y acontecimiento ambiental en la rotonda. Espacio/Enter o A (×) recoge y permite reescuchar. Reiniciar la escena borra la colección; las recuperaciones la conservan. No se añade guardado entre partidas ni dependencia externa.

Revisión posterior: `Docs/LegibilidadYEpilogo.md` prevalece sobre el regreso con huella/guiño y sol en su posición inicial. El epílogo cierra todas las aberturas sin rastro, cambia el sol a atardecer en otra dirección y mantiene créditos. También se revisó la legibilidad de los detalles opcionales.
