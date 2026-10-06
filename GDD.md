# CRÁTER — Game Design Document

> Versión 0.7 · Vertical slice · Unity 6 (6000.3) + URP (Forward+) · PC, teclado/mouse y joystick
>
> Documento vivo de esta copia independiente. El GDD se ajusta a las decisiones de diseño que se prueban en el juego; no es una especificación congelada. Estado vigente del 6/10/2026: `Docs/EstadoActualCrater.md` y `Docs/RevisionContemplativa.md`. Los documentos de revisiones anteriores son históricos. Los detalles de implementación están en el `README.md`.

---

## 1. Concepto

**En una frase:** durante un eclipse, un cráter que sólo existe bajo esa luz se abre frente a una estación de observación; adentro, una linterna con filtros le enseña al jugador a ver, hasta que descubre que para salir tiene que apagarla.

**Género:** puzzle ambiental en primera persona, contemplativo.
**Tono:** contemplativo, de asombro y descubrimiento. Sin combate ni amenazas. Caerse en un abismo lleva a una gruta cálida, desde donde se vuelve caminando por una escalera.
**Duración:** vertical slice (≈ 15–25 min).

### Pilares

1. **La luz es el verbo.** Toda interacción pasa por el haz: iluminar es actuar.
2. **Ver es un acto, no un dato.** Cada filtro es una forma de mirar la materia; el final obliga a mirar *sin* herramienta.
3. **Ritmo contenido.** Sin salto. La caminata marca el ritmo; Shift permite correr de forma auxiliar. La tensión viene de entender, no de reaccionar.
4. **Interfaz mínima.** El estado de la linterna arriba a la derecha y la mira con la barra de carga. Las indicaciones breves son una ayuda opcional (H en pausa), desactivada por defecto. El resto lo dicen el color, el sonido y el mundo. La estación de observación no tiene indicaciones.

---

## 2. Narrativa

### Premisa

El protagonista empieza en una **loma exterior**, de día, con la estación de observación como referencia visible. El relieve la oculta y la vuelve a mostrar durante la aproximación. Empieza un **eclipse total de sol** y su luz revela un **cráter** en el valle que no estaba ahí: sólo existe mientras dura la totalidad. En su borde aparece la **boca de una escalera**: la entrada está en el suelo.

El eclipse dura exactamente lo que el jugador permanezca adentro. El cráter es un espacio metafórico/onírico: no se explica, se atraviesa.

### Arco

| Momento | Qué pasa | Qué siente el jugador |
|---|---|---|
| **Prólogo** | Loma exterior de mañana, con la estación de observación como objetivo visible. Al bajar entre las lomas se pierde y reaparece. El eclipse avanza caminando hacia el mirador (o solo en 80 s). En el paso entre cerros llega la totalidad: se abre el cráter adelante mientras se bloquea caminar, conservando la mirada. Cordones bajos delimitan la abertura en el suelo; no hay destello; se escucha tierra/piedra abrirse. | Curiosidad, extrañeza. |
| **Descenso** | Sin cortes: se cruza la boca del cráter y se baja por una escalera hasta la Explanada, en el fondo del pozo. Un pasillo sigue al Umbral, donde está la linterna. | Descubrimiento. |
| **Aprendizaje** | La Rotonda abre dos alas, en cualquier orden: la oeste entrega el filtro sol, la este el filtro luna. Cada una termina en un sello. Con los dos, el Cruce pide usar los filtros juntos. | Dominio, asombro. |
| **La oscuridad** | Al entrar a la Cresta, una losa cierra el corredor a la espalda: no hay vuelta. Paredes, piso y techo devuelven el reflejo del propio foco. Al apagar la linterna y esperar, aparecen tallados ocultos. | Revelación, encierro. |
| **Final** | Adaptado a la oscuridad, el techo se abre y entra la luz blanca del fin del eclipse hasta inundarlo todo. Silencio, **anillo de diamante**, y el jugador aparece en la estación de observación, de día. | Cierre sereno. |
| **Epílogo** | El cráter desaparece sin rastro: terreno continuo, sin huella ni abertura. El sol cambió de posición y el exterior tiene luz de atardecer. Cruzar el antiguo lugar del cráter trae los créditos; tras 45 segundos libres también se ofrecen. | Pérdida suave, confirmación. |

### El eclipse

- **Afuera** se ve en el cielo: el sol y la corona en la totalidad. No es noche cerrada sino un crepúsculo profundo: cielo azul oscuro, el horizonte cálido alrededor y el paisaje todavía visible. La luna nueva no se ve de día: sólo su silueta cuando pasa por delante del sol. En el epílogo ya pasó y quedó del otro lado.
- **Adentro** la totalidad está congelada. La apertura física de la Rotonda permite observar el mismo eclipse exterior, con su corona, desde el sector sur. Los óculos decorativos de otros sectores conservan su arte.
- **Termina con el anillo de diamante**, el destello real con el que cierra la totalidad: la luz vuelve de golpe, como al prender la luz después de estar a oscuras.

### Tema

La herramienta que nos permite ver también define lo que vemos. La linterna enseña dos maneras de mirar la materia, pero la última puerta sólo aparece cuando el jugador deja de iluminar.

---

## 3. Mecánicas

### 3.1 Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Correr (mantener) | Shift | apretar el stick izq. |
| Linterna | F | RB |
| Filtro sol / luna | 1 / 2 | X / Y |
| Luz blanca | Q | B |
| Pausa (R reinicia, X sale; H alterna ayudas escritas) | Esc | Start |
| Saltar animaciones | Espacio / Enter | A / cruz |

### 3.2 Linterna

- Se encuentra en el Umbral. Luz blanca: abre la compuerta del Umbral.
- La luz blanca alumbra bastante más y más lejos que con cualquier filtro: con un filtro puesto se ve menos, así que dan ganas de volver a la luz blanca (Q).
- **Un filtro a la vez.** Cambiar tarda 0,6 s y durante el cambio el haz casi se apaga.
- Ilumina todos los receptores dentro del cono con línea de vista; cargarlos lleva 0,4 s.

### 3.3 Filtros

| # | Filtro | Color | Receptor | Qué hace |
|---|---|---|---|---|
| 1 | **Filtro sol** | Ámbar | Ancla de basalto → Puente de luz | Enciende anclas, sólo de frente (por la cara de los aros). Con todas las anclas de un puente encendidas, el puente se materializa. Las anclas retienen unos segundos al perder el haz. |
| 2 | **Filtro luna** | Azul de luna | Reja (barras de vidrio azul translúcido) | Disuelve la materia hueca mientras se la ilumina: se vuelve atravesable. Una reja sólida tapa la luz: lo que está detrás no se enciende. Si se apunta a un ancla tapada con el filtro correcto, el ancla titila apagada y el sello azul de la reja late: "primero yo". La trampilla inicial tapa una rampa descendente; la última escotilla es una reja vertical en la salida de otra rampa. Sus pretiles evitan caídas laterales. |

**Sellos:** al final de cada ala. Una vez encendidos quedan así para siempre; abren un atajo a la Rotonda y, los dos juntos, la puerta del norte.

**Puertas:** en este recorrido la activación queda resuelta de forma permanente. Las anclas que abren puertas tienen una base distinta; la retención temporal se reserva a los puentes.

### 3.4 Adaptación a la oscuridad

Sólo en la Cresta. Al apagar hay una primera respuesta de exposición y viento en menos de un segundo; tras 0,4 s empieza una adaptación gradual de 11 s. Encender la linterna la pierde suavemente en 1,2 s. El viento del óculo acompaña la adaptación con un máximo reducido. Al 90 % se abre el techo.

### 3.5 La pared espejo, el encierro y el techo

- Las cuatro paredes, el piso y el techo de la Cresta reflejan el foco, recortándolo en su propio borde. **Con la linterna prendida** devuelve un reflejo encandilante del propio foco (del color del filtro), como una ventana de noche.
- **Con la linterna apagada**, el reflejo desaparece y aparecen los tallados latentes.
- Al entrar a la Cresta, una losa sube del piso y cierra el corredor.
- Adaptado, el techo se abre y entra la luz blanca del fin del eclipse hasta inundarlo todo.

---

## 4. Recorrido

```
                                          ┌─ ALA OESTE (SOL) ────┐
VALLE (estación de observación visible) ─► EXPLANADA ─► UMBRAL ─► ROTONDA ┤                      ├─ sellos ─► CRUCE ─► CRESTA ─► (blanco) ─► ESTACIÓN ─► CRÉDITOS
(prólogo)               linterna          └─ ALA ESTE (LUNA) ────┘            (los dos)  oscuridad              (epílogo)
```

Las alas anteriores vuelven a seguir **Enseñar → Probar → Torcer**. En LUNA, las dos rejas iniciales son una decisión: el ramal sur no tiene salida y obliga a volver; el norte continúa. Se hacen en cualquier orden; el sello del final abre un atajo de vuelta a la Rotonda.

| Sala | Contenido |
|---|---|
| **00 Estación de observación (prólogo)** | Libre, sin indicaciones, desde SpawnValle en una loma (z local 150). El camino hacia la estación de observación encuadra el eclipse y desemboca en el mirador. El valle está girado 180° alrededor del pozo: la estación de observación queda detrás y se llega de frente a la escalera. La entrada es una abertura en el piso, sin pilares ni dintel. |
| **01 Explanada** | El fondo del pozo, bajo la totalidad. Se llega por la escalera de la puerta, entre dos paredes altas talladas: a la izquierda, en azul, la luna pasa por delante del sol; a la derecha, en naranja, el sol por delante de la luna. Bajando, los dos eclipses avanzan hasta la totalidad. Un pasillo sigue al Umbral; en sus dos paredes, losas pintadas cuentan el lore sin palabras. A la izquierda: el sol y la luna separados, el eclipse sobre la estación de observación, el cráter que se abre, alguien con la luz que los revela. A la derecha, cómo sigue: las anclas que tienden un puente, la puerta con el sol y la luna a los lados, el eclipse que la abre, la vuelta a la estación de observación al atardecer. |
| **02 Umbral** | Los controles se enseñan sin texto y en el momento en que se usan: cada losa muestra la tecla dibujada como tecla y el ícono de lo que hace. Al entrar, mover (WASD + mouse); junto a la linterna, prenderla (F). Sostener la luz blanca sobre un ancla abre la compuerta. Los filtros se enseñan en la primera sala de cada ala, junto al filtro y al problema: sol (1, el puente aparece) o luna (2, la reja se abre), y una losa cercana muestra Q para volver a la luz blanca. |
| **03 Rotonda** | Sala circular de 37,5 m, con plaza hundida circular de 1,2 m, escalinata anular continua y vista elevada al entrar; con óculo y, debajo, el conjunto original del kit Blender, con seis columnas y su obelisco nuevamente centrado en la plaza. Las alas se entran por la mitad de cada costado, lejos de la entrada (en el playtest, con la sala cuadrada, estaban demasiado cerca); los atajos vuelven por dos pasillos que entran por el noroeste y el noreste; al norte, la puerta de los sellos. Hasta ella llegan dos hilos de tallados: soles desde el ala oeste (filtro sol) y lunas desde el ala este (filtro luna); al volver a la rotonda con un sello encendido, su hilo se prende de a uno con tonos que suenan desde los tallados, con una cinemática que orienta la cámara hacia las luces y devuelve el control al terminar; con el segundo sello se enciende además el eclipse sobre la puerta y la puerta se abre a la vista. Sobre cada atajo, un faro (tallado con luz) se prende cuando su sello se enciende. |
| **03a Ala oeste** | Filtro sol. Arte del sol: frisos de soles y halos en las paredes, las fases del sol comido en la sala de la puerta y pebeteros de piedra con un sol encima que se prenden con el sello. Enseñar: un abismo y dos anclas juntas. Probar: una puerta permanente con una ancla en el rincón del fondo. Las puertas tienen una ancla y los puentes dos, con duración limitada. Torcer: las anclas cuelgan del techo sobre el otro lado de un pozo. Sello detrás de un tabique. |
| **03b Ala este** | Filtro luna. Arte de la luna: las fases de la luna, un cielo de estrellas, una luna llena grande y menhires con una luna y una estrella talladas que se encienden con el sello. Enseñar: un muro de rejas. Probar: la sala no tiene salida; el camino es una trampilla en el piso que da a una galería de abajo, con otra reja. Torcer: una rampa con pretiles que termina en una reja vertical permanente. Sello: una placa de materia hueca que queda disuelta. |
| **04 Cruce** | LUNA revela un ancla en un nicho y SOL forma un puente descentrado. Después, un puente lleva a una isla de piedra: allí se puede detener y dejar apagar el primer tramo. LUNA revela las anclas de salida, SOL forma el segundo tramo y LUNA abre el paso final. Una antesala tranquila lleva a la Cresta. |
| **05 Cresta** | Se cierra a la espalda. Apagar la linterna: tallados latentes y la pared espejo. El techo se abre y entra la luz blanca. |
| **06 Estación de observación (epílogo)** | Al atardecer, sin linterna: el sol cambió de posición (18° de elevación, azimut 30°), el cielo se vuelve cálido y las sombras cambian de dirección. El cráter desaparece sin huella ni nuevo tallado sobre la puerta; techo y terreno vuelven a cerrarse. Cruzar su antiguo lugar, o esperar 45 s de exploración libre, trae los créditos. |

---

## 5. Dirección de arte

- **Kit modular low-poly** (Blender): anclas facetadas, rejas, puente, linterna, arquitectura con pilares y motivos tallados (espiral, serpiente escalonada, chakana, rombo).
- **Paleta:** basalto casi negro; ámbar para el filtro sol y los tallados vivos; azul de luna para el filtro luna y lo latente; plateado frío para todo lo que es eclipse (corona, puerta, óculos).
- **Lo que brilla, se usa.** Sólo brilla lo funcional o lo que responde al jugador: anclas, sellos, hilos, faros, pebeteros y menhires (apenas, hasta que se enciende su sello), los tallados latentes. El arte decorativo (frisos, murales, las paredes de la escalera, los paneles) es pintura mate sobre piedra: no brilla, así no se confunde con algo para iluminar.
- **Ornamento, no acertijo.** Los frisos repiten un solo motivo (todos soles, todos halos, todas lunas crecientes): no hay secuencias ni órdenes que invitan a buscar un patrón. Las únicas secuencias son los hilos de la rotonda, que se encienden solos y muestran el progreso.
- **Sin instrucciones escritas por defecto.** La ayuda opcional de H en pausa puede mostrar indicaciones de progreso. El HUD no explica los controles: los controles se enseñan con murales en el momento en que se usan y la historia con los del pasaje.
- **Saltear animaciones:** Espacio, Enter o A completan las secuencias del prólogo, sellos, final y créditos, sin aviso en pantalla. La cinemática de los sellos orienta la mirada y devuelve el control al terminar; puede pausarse y saltarse.
- **Pausa:** muestra los controles, en gris apagado, y H para alternar las ayudas escritas.
- **Umbral:** el ancla es de luz blanca: piedra clara, aros blancos y una lámpara colgada encima, distinta de las anclas del filtro sol. Las paredes llevan un friso de serpiente escalonada (sol al oeste, luna al este) y un mural del ancla que baja la losa.
- **Pasillos de vuelta:** al abrirse cada atajo se ve un sol (o una luna) grande y, por el pasillo, otros dos; se encienden con el sello y alumbran el camino a la rotonda.
- **Cruce:** se conservan los murales decorativos del valle y la chakana. Se retiraron los paneles explicativos que saturaban la composición. La isla segura contiene un apoyo de piedra y un cuenco, fuera del paso.
- **Cresta:** un díptico al entrar: alguien encandilado por su propio destello y, enfrente, la misma persona con la luz baja y los tallados apareciendo alrededor. El reflejo de la linterna sigue al haz en las cuatro paredes; arriba, tallados claros cuentan sin palabras qué hacer (un ojo encandilado, el ojo cerrado y las estrellas; alguien con la linterna colgando entre estrellas) y se iluminan junto con la luz que entra por el techo. En el destello del reflejo se ve la silueta de un ojo cerrado. La sala no lleva módulos de arquitectura (cortaban el reflejo).
- **Centro de la rotonda:** es el mapa del progreso. Las columnas del oeste llevan soles y las del este lunas, que se encienden con el sello de su ala. El obelisco es el reloj del eclipse: en su cara oeste un sol y en la este una luna, que se encienden con el sello de su ala. Con los dos, se enciende el eclipse de su cara sur y la cinemática se concentra en el hilo y la puerta que se abre, sin un giro adicional hacia el obelisco. Sobre el obelisco hay una apertura real al exterior; la vista del eclipse se encuentra desde el sector sur. Entra una iluminación tenue, sin una imagen de cielo falsa sobre la piedra.
- **Anclas que abren puertas:** se paran sobre una base de piedra clara de dos escalones; las de los puentes no.
- **Final:** cruzar el antiguo lugar del cráter cierra la demo; esperar 45 segundos libres también permite terminar.
- **Murales de las alas:** se conserva el arte restaurado, con el panel LUNA y luz blanca separados. En el desvío lunar, un panel de reparación muestra una persona, una junta del piso y escalones: aporta historia e insinúa mirar abajo, sin dibujar la solución ni una ruta.
- **Estación de observación:** refugio secular de adobe y cal, techo bajo inclinado, alero, instrumental, mesas, cajas y patio con pirca. La silueta asimétrica de su sensor sirve de referencia durante la aproximación. No hay cruz, campanario, altar ni bancos religiosos. Los pictogramas narrativos representan este edificio.
- **Gruta O1:** asiento de piedra y cuenco bajo tres sectores tallados que responden suavemente al haz con diferentes resonancias. Están al costado de la escalera, separados del mural de regreso. Sugieren descanso y cuidado cotidiano; no se les atribuyen significados culturales auténticos. El gesto es opcional y no modifica el progreso.
- **Anclas:** reposo visible, luz y resonancia desde el primer contacto, aros con vibración mínima, tono de activación limitado y pérdida de brillo gradual. Los pares SOL iniciales retienen 4 s y los colgantes 5,5 s tras perder el haz; no se añaden focos sobre las anclas colgadas.
- **Post-procesado:** ACES, bloom, viñeta y grano fino. La adaptación usa la exposición.

## 6. Audio

Todo el audio se sintetiza en `GeneradorAudioCrater` y se puede reemplazar por WAV grabados con el mismo nombre.

| Momento | Sonido |
|---|---|
| Estación de observación | Pájaros y viento exterior; sin campana |
| Eclipse | Los pájaros ceden al ambiente de totalidad. El cráter se abre sin destello, con una respuesta de tierra/piedra |
| Cráter | Dron ambiente, viento del óculo que sube con la adaptación |
| Anclas | Resonancia tenue en carga y tono de activación; salida suave. Los pares forman intervalos del mismo acorde |
| Techo de la Cresta | Retumbo al abrirse; después, silencio |
| Final | Silencio → tono agudo del anillo de diamante → pájaros y viento exterior |
| Créditos | Acorde lento en La mayor, el mismo de las anclas |

---

## 7. Estado

### Hecho
- [x] Recorrido completo, arte modular, interfaz, pausa, joystick
- [x] Filtros sol y luna con sus puzzles
- [x] Prólogo caminable desde las lomas: cielo con eclipse, mirador y abertura del cráter
- [x] Apertura de la rotonda hacia el eclipse exterior congelado
- [x] Pared espejo, encierro y techo que se abre en la Cresta
- [x] Entrada continua: el cráter es un pozo en el valle
- [x] Anillo de diamante, epílogo sin cráter y créditos
- [x] Validador y tests (EditMode y PlayMode del recorrido completo, prólogo incluido)

### Pendiente
- [x] Arte de la estación de observación y del valle (low-poly generado por el constructor)
- [ ] Créditos definitivos (`SISTEMAS` → `EclipseFinalController` → `Creditos`)
- [ ] Playtest: encuadres de las lomas, claridad de totalidad, grutas y recorte del reflejo
- [ ] Evaluación perceptiva de mezcla y transiciones con auriculares y altavoces; grabaciones personalizadas existentes preservadas

### Descartado respecto del GDD anterior
- Filtro RASTRO y sus salas (Bifurcación, El Paso).
- La Lente.
- La pista forzada de la Galería: la Cresta ya indica apagar la linterna.

## Acuerdos vigentes — 6/10/2026

Prevalece `Docs/EstadoActualCrater.md`. Las alas anteriores se conservan, con dos entradas iniciales a ciegas en LUNA, ramal sur sin salida y regreso seguro. El Cruce nuevo permanece. La ampliación horizontal del 25% se conserva, con alturas, escala y velocidad del jugador originales. Los modelos, anclas, rejas, murales y motivos mantienen sus proporciones: el generador amplía arquitectura y conexiones, no estira estos objetos.

La revisión usa vistas y cambios de dirección, contraste contenido, gestos sonoros opcionales y objetos de uso cotidiano. `Docs/RevisionContemplativa.md` registra sus referencias y las decisiones propias del juego. No reincorpora mirillas, portales, focos sobre anclas colgantes, surcos, miguitas, ventanas de retorno, zigzag previo a Cresta ni el arte de cobre descartado.

Los documentos `RevisionIntegral.md`, `RedisenioV2.md` y `AlasYCruceNuevo.md` registran etapas históricas. Sus diseños y capturas no sustituyen el generador vigente ni este estado. Los nombres técnicos que conservan Capilla siguen por compatibilidad de referencias.


## Exploración y colección opcional — ampliación vigente

Seis piedras pequeñas componen un halo por sectores. Sus motivos proceden del lenguaje gráfico del juego, sin atribución cultural inventada. Se recogen mirando y usando Espacio/Enter o A (×), con aviso contextual breve independiente de las ayudas de progreso. La pausa muestra x/6; cada pieza aparece en un conjunto lateral de la rotonda. No son anclas, filtros ni sellos.

Las ubicaciones y reglas están en `Docs/ExploracionYConjunto.md`. Todas se alcanzan caminando; la exterior admite regreso por la escalera abierta. El cierre de Cresta termina la oportunidad de explorar. Caídas conservan la colección, reiniciar la escena la borra, y no hay guardado adicional entre partidas. El recorrido principal no exige ninguna pieza.

Completar prepara un acontecimiento de cinco segundos, que espera una mirada cercana. El conjunto enciende sus tallados, proyecta una versión estilizada del mismo halo y emite una breve composición localizada. El jugador conserva movimiento y mirada; pausa y sellos suspenden la secuencia. Queda una transformación permanente y la misma acción permite escuchar otra vez.

Los detalles opcionales invitan por contraste tenue, perspectiva y sonido: cuenco con agua y reflejo real, halo que se reúne desde un encuadre tolerante, cavidad resonante, veta angular, reparaciones y orden junto a un descanso, un detalle revelado al volver con un sello, polvo discreto y una vista lateral del exterior. No cambian las soluciones de las alas ni la narrativa del eclipse. El óculo es circular. El sol está bajo (14°) durante la aproximación, sube a 75° sólo al entrar al subsuelo y vuelve a su horizonte original al regresar opcionalmente por la escalera; el epílogo final lo mueve a otra dirección y elevación para mostrar paso del tiempo. Se conserva el mismo eclipse y su estado. El ambiente global continuo de oleaje se retiró; la apertura de piedra se escucha desde el mirador y los detalles sonoros localizados permanecen.

Revisión de legibilidad y cierre sin rastro: `Docs/LegibilidadYEpilogo.md`. Los detalles opcionales se hicieron más perceptibles tras la devolución del jugador; su comprensión sigue requiriendo una partida sin guía.
