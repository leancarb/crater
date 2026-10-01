# CRÁTER — demo de mecánicas (versión para clase)

Rama `claude/demo-clase`: sólo el juego, sin interfaz. Empieza en la Explanada y
termina al cruzar la puerta del eclipse; después vuelve a empezar solo.

Sin HUD: no hay textos, mira, barra de carga ni título. Todo se comunica con luz,
color y el sonido de los puzzles. También es más austera: los filtros no suenan ni
zumban, los objetos para recoger están quietos, la linterna en la mano no se balancea
y el jugador no cabecea ni hace ruido de pasos. El estado de la progresión se puede ver en el Inspector, en el
objeto `SISTEMAS` → `FlujoJuegoCrater` → *Etapa Visible*.

Unity 6000.3 · URP (Forward+) · Input System.

## Cómo abrirlo

1. Abrir el proyecto y esperar a que compile.
2. **Crater › Reconstruir todo** (genera la escena de esta versión).
3. Play.

## Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Linterna | F | RB |
| Filtro CUERPO / HUECO | 1 / 2 | X / Y |
| Luz blanca | Q | B |
| Pausa (la pantalla se oscurece; R reinicia, X sale) | Esc | Start |

## Guión para mostrar en clase

| Sala | Qué se muestra | Script |
|---|---|---|
| 01 Explanada | Controlador en primera persona: caminar y mirar, sin salto ni carrera | `JugadorFPS` |
| 02 Umbral | Recoger la linterna. Luz blanca sostenida sobre el ancla: la **carga** sube y la compuerta se hunde | `Recogible`, `LinternaController`, `Ancla`, `Compuerta` |
| 03 Campo | Filtro **CUERPO**. Enseñar: dos anclas juntas tienden un puente. Probar: anclas separadas, hay que barrer y cruzar con la **retención** (el ancla late cada vez más rápido y el puente parpadea antes de irse). Torcer: anclas en el techo | `ReceptorDeLuz`, `Ancla`, `PuenteLuz` |
| 03 Campo (arriba) | Óculo: el eclipse congelado en el cielo | `ConstructorCrater` (textura generada) |
| 04 Hondonada | Filtro **HUECO**. Rejas que se disuelven mientras se las ilumina; zigzag; al final, puente con CUERPO y reja con HUECO desde una repisa | `MateriaHueca` |
| (cualquier fosa) | Caerse devuelve al último suelo firme | `RespawnPorCaida` |
| 05 Cresta | La pared del fondo sólo devuelve el **reflejo** del propio foco. Apagar la linterna: la exposición sube de a poco (**adaptación a la oscuridad**), aparecen tallados ocultos y la puerta | `ParedEspejo`, `AdaptacionOscuridad`, `PuertaEclipse` |
| Final | Cruzar la puerta: silencio, **anillo de diamante**, blanco y vuelve a empezar | `EclipseFinalController`, `Fundidos` |

Ideas para mostrar:

- Iluminar un ancla con el filtro equivocado: no pasa nada (cada receptor tiene un canal).
- Encender un puente y soltar el haz: contar los segundos de retención.
- Pararse dentro de una reja disuelta y soltar el haz: no se cierra encima del jugador.
- En la Cresta, prender la linterna a mitad de la adaptación: se pierde en un segundo.

## Cómo está hecho

La escena, los prefabs, los materiales, el audio y el post-procesado **se generan
desde código**. No se editan a mano: se cambia el código y se reconstruye.

- **Crater › Reconstruir todo** — regenera todo y valida. Es idempotente.
- **Crater › Validar proyecto** — chequea que cada puzzle esté conectado y al alcance del haz.
- **Crater › Renderizar vistas previas** — guarda capturas en `Assets/Art/Previews/Unity`.
- **Crater › Construir demo Windows** — valida y compila en `Builds/Windows`.
- **Crater › Construir demo Mac** — valida y compila `Builds/Mac/CRATER.app` (requiere el módulo *Mac Build Support* de Unity Hub; se puede hacer desde Windows).

Dónde tocar:

- `Assets/Editor/ConstructorCrater.Nivel.cs` — el nivel: geometría, puzzles, luces y arte, en metros.
- `Assets/Editor/ConstructorCrater.cs` — materiales, prefabs, render y post-procesado.
- `Assets/Editor/ConstructorCrater.Facetado.cs` — la estética low-poly: caras planas, superficies irregulares y un tono por cara. El tamaño de las caras y el relieve de cada zona están arriba de todo, en los `Perfil`.
- `Assets/Editor/GeneradorAudioCrater.cs` — sonidos sintetizados (reemplazables por WAV grabados con el mismo nombre).
- `Assets/Data/Filtros/` — los dos filtros (color, cono, alcance, tiempo de carga).
- `Assets/Scripts/` — el juego: `Jugador`, `Linterna`, `Mecanicas`, `Flujo`, `Interfaz` (sólo fundidos).

Cada script tiene una sección **CÓMO FUNCIONA** en el encabezado.

## Tests

Desde **Window › General › Test Runner**:

- **EditMode** — reglas de las mecánicas (carga, retención, puentes, rejas, compuerta, respawn).
- **PlayMode** — recorre la escena real de punta a punta caminando y apuntando la linterna.
  Si una pieza del nivel se mueve y un puzzle deja de poder resolverse, falla.

## Arte

El kit modular está en `Assets/Art/Blender` (ver su README) y se exporta a
`Assets/Models/CraterKit`. El constructor usa esos FBX directamente.

## La versión completa

La rama `claude/stoic-rubin-qn1pj9` tiene el juego completo: interfaz, prólogo en la
capilla con la cinemática del eclipse, epílogo y créditos (ver `GDD.md`).
