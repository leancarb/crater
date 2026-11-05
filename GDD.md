# CRÁTER — Game Design Document

> Versión 0.9 · 5/11/2026 · Vertical slice · Unity 6 (6000.3.23f1) + URP (Forward+) · PC, teclado/mouse y joystick (convención Xbox)
>
> Documento vivo: describe el juego tal como está en `main` (5/11/2026). Los detalles técnicos y la evidencia de cada iteración están en `Docs/` (ver §8). Cuando un documento de iteración y este GDD no coinciden, prevalece el documento más reciente de `Docs/`.

---

## 1. Concepto

**En una frase:** durante un eclipse total, un cráter que sólo existe bajo esa luz se abre en un valle andino; adentro, un farol con dos filtros le enseña al jugador a ver, hasta que descubre que para salir tiene que apagarlo.

**Género:** puzzle ambiental en primera persona, contemplativo.
**Tono:** asombro y descubrimiento. Sin combate ni amenazas. Caerse en un abismo lleva a una gruta en penumbra roja, desde donde se vuelve caminando por una escalera: no hay muerte ni reaparición.
**Duración:** vertical slice (≈ 15–25 min).

### Pilares

1. **La luz es el verbo.** Toda interacción pasa por el haz del farol: iluminar es actuar.
2. **Ver es un acto, no un dato.** Cada filtro es una forma de mirar la materia; el final obliga a mirar *sin* herramienta.
3. **Ritmo contenido.** Caminar a 4 m/s, correr a 5,6 m/s y un salto corto de 0,4 m que no permite saltear puzzles. La tensión viene de entender, no de reaccionar.
4. **Interfaz mínima.** Sin mira ni barra de carga. El HUD sólo muestra el filtro activo y sus segundos restantes. Los controles se enseñan con carteles físicos en el mundo; la historia, con dibujos en las paredes. Las ayudas escritas son opcionales (H en pausa) y están apagadas por defecto.

---

## 2. Narrativa

### Premisa

El jugador empieza en una **terraza elevada**, de día, mirando el paisaje del valle. Al bajar por un sendero entre lomas, el sol empieza a ser cubierto por la luna. Cuando llega la totalidad, la tierra se abre adelante: un **cráter** que no estaba ahí, con la **boca de una escalera** en el suelo. Adentro hay un templo olvidado, tallado con soles y lunas, y en su centro, un farol.

El eclipse dura exactamente lo que el jugador permanezca adentro. El cráter es un espacio metafórico/onírico: no se explica, se atraviesa.

### Arco

| Momento | Qué pasa | Qué siente el jugador |
|---|---|---|
| **Prólogo** | Terraza elevada con cierres naturales de roca. Un sendero de tierra baja entre lomas que encuadran el cielo; el eclipse empieza a oscurecer desde el 12 % del recorrido y avanza mientras se camina (o solo, con el tiempo). En el mirador llega la totalidad (1,2 s) y el cráter se abre (2,4 s) sin destello, con sonido de tierra y piedra. El jugador conserva siempre cámara y movimiento. | Curiosidad, extrañeza. |
| **Descenso** | Sin cortes: se cruza la boca y se baja por una escalera hasta la Explanada. El pasaje cuenta la historia en las paredes y lleva, por el Umbral, a la Rotonda. | Descubrimiento. |
| **El farol** | La Rotonda está a oscuras: sólo brilla el farol, en el centro. Al recogerlo, una losa cierra la entrada a la espalda y la sala se enciende gradualmente, junto con los accesos a las alas SOL y LUNA. | Hallazgo, compromiso. |
| **Aprendizaje** | Las dos alas se hacen en cualquier orden: SOL construye puentes de luz y LUNA atraviesa materia hueca. Cada una termina en un sello. Con los dos, se abre el Cruce, donde los filtros se combinan. | Dominio, asombro. |
| **La oscuridad** | Al entrar a la Cresta, una losa cierra el corredor. Las paredes de la sala circular devuelven el reflejo del propio farol. Al apagarlo y esperar, aparecen tallados ocultos. | Revelación, encierro. |
| **Final** | Adaptado a la oscuridad, el techo se abre y entra la luz blanca del fin del eclipse hasta inundarlo todo. Silencio, **anillo de diamante**, y el jugador aparece junto a la estación de observación, al atardecer. | Cierre sereno. |
| **Epílogo** | La estación de observación aparece por primera vez. El cráter desapareció sin rastro. El sol está en otra posición (18° de elevación, azimut 14°) y la luz es cálida. Cruzar el antiguo lugar del cráter, o esperar 45 s de exploración libre, trae los créditos. | Pérdida suave, confirmación. |

### El eclipse

- **Afuera** se ve en el cielo con un diámetro aparente de 7°. No es noche cerrada sino un crepúsculo profundo, con 1300 estrellas que aparecen gradualmente hacia la totalidad y desaparecen en el epílogo. Los astros se dibujan al fondo: las montañas y paredes los tapan, y la luna tapa al sol.
- **Adentro** la totalidad está congelada. La abertura circular de la Rotonda deja ver el mismo eclipse real, sin cielo pintado.
- **Termina con el anillo de diamante**: la luz vuelve de golpe, como al prender la luz después de estar a oscuras.

### Tema

La herramienta que nos permite ver también define lo que vemos. El farol enseña dos maneras de mirar la materia, pero la última puerta sólo aparece cuando el jugador deja de iluminar.

---

## 3. Mecánicas

### 3.1 Controles

| Acción | Teclado y mouse | Joystick (Xbox) |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Caminar / correr | 4 m/s · **Shift** 5,6 m/s | 4 m/s · **RT** 5,6 m/s |
| Salto corto (0,4 m) | **Espacio** | **A** |
| Recoger / interactuar / omitir animaciones | **E** | **B** |
| Prender y apagar el farol | **F** | **X** |
| Filtro SOL (mantener) | **Clic izquierdo** | **RB** |
| Filtro LUNA (mantener) | **Clic derecho** | **LB** |
| Volver a luz blanca | Soltar el filtro | Soltar el filtro |
| Pausa (R reinicia, X sale, H alterna ayudas) | Esc | Start |

Los carteles y la pausa muestran las teclas o los botones Xbox según el último dispositivo usado.

### 3.2 El farol

- Está en el centro oscuro de la Rotonda, sobre un apoyo bajo. Es lo único iluminado al llegar.
- Recogerlo (E / B) cierra la entrada con una losa de piedra y enciende la sala en 2,5 s, incluidos los pasillos de SOL y LUNA.
- La luz blanca alumbra más y más lejos que cualquier filtro.
- Ilumina todos los receptores dentro del cono con línea de vista.

### 3.3 Filtros

SOL y LUNA se activan **mientras se mantiene** su botón, durante un máximo de **3 segundos**. Soltar, o agotar el tiempo, vuelve a luz blanca con una transición de 0,18 s. Tras agotarlo hay que soltar y volver a presionar. El HUD muestra el nombre del filtro y sus segundos restantes. La pausa congela el tiempo. Ambos filtros usan el mismo cono (28°) y alcance (18,75 m).

| Filtro | Color | Receptor | Qué hace |
|---|---|---|---|
| **SOL** | Ámbar | Ancla de basalto → puente de luz | Enciende anclas, sólo de frente. Con todas las anclas de un puente encendidas, el puente se materializa. Las anclas retienen unos segundos al perder el haz (4 s los pares iniciales, 5,5 s las colgadas). |
| **LUNA** | Azul de luna | Reja (barras de vidrio azul translúcido) | Disuelve la materia hueca mientras se la ilumina: se vuelve atravesable. Una reja sólida tapa la luz: lo que está detrás no se enciende. Algunas rejas y trampillas quedan abiertas una vez resueltas. |

- **Sellos:** al final de cada ala. Una vez encendidos quedan así para siempre; abren un atajo a la Rotonda y, los dos juntos, la puerta del Cruce.
- **Puertas vs. puentes:** las puertas tienen un ancla y quedan abiertas; los puentes tienen dos y duran un tiempo. Las anclas que abren puertas se apoyan en una base de piedra clara.
- **Salto:** limita la velocidad horizontal en el aire a 2 m/s; probado para no superar pretiles ni cruzar los huecos de los puzzles.

### 3.4 Redes de seguridad

Debajo de cada abismo hay una gruta 3 m más abajo, en penumbra roja tenue (sin teñir las salas de arriba), con un dibujo propio y una escalera de piedra que devuelve al jugador antes del desafío. Caer no hace perder progreso ni piezas coleccionables.

### 3.5 Adaptación a la oscuridad

Sólo en la Cresta. Al apagar el farol hay una respuesta de exposición y viento en menos de un segundo; tras 0,4 s empieza una adaptación gradual de 11 s. Prender el farol la pierde en 1,2 s. Al 90 % se abre el techo.

### 3.6 La Cresta: reflejo, encierro y techo

- **Sala circular** de 15 m de radio, con óculo central cubierto por dos hojas.
- **Reflejo:** con el farol prendido, paredes, jambas, dintel, compuerta, piso y techo devuelven un reflejo encandilante del propio haz (del color del filtro). Es una sola malla que se proyecta sobre las superficies reales y se recorta en sus bordes. Con el farol apagado, el reflejo desaparece y aparecen cinco tallados latentes.
- **Encierro:** al entrar, una losa sube del piso y cierra el corredor.
- **Techo:** adaptado, el techo se abre y entra la luz blanca del fin del eclipse.

### 3.7 Exploración opcional

Seis piedras talladas componen un halo por sectores. Se recogen mirándolas y usando E / B; la pausa muestra x/6. No son anclas, filtros ni sellos, y el recorrido principal no exige ninguna. La pieza que antes estaba en el exterior se encuentra ahora en la Rotonda, sobre un banco. Al completar el conjunto, sus tallados se encienden y suena una breve composición, sin quitarle el control al jugador. Detalles opcionales: el cuenco con agua y reflejo, el halo que se reúne por perspectiva, la cavidad resonante, la veta angular y la gruta O1 con asiento, cuenco y tallados que responden al haz.

---

## 4. Recorrido

```
TERRAZA ─► SENDERO Y LOMAS ─► MIRADOR (totalidad) ─► BOCA ─► EXPLANADA ─► UMBRAL ─► ROTONDA (farol)
                                                                                     ├─ ALA OESTE (SOL) ─┐
                                                                                     └─ ALA ESTE (LUNA) ─┴─ sellos ─► CRUCE ─► CRESTA ─► (blanco) ─► ESTACIÓN (epílogo) ─► CRÉDITOS
```

Las alas siguen **Enseñar → Probar → Torcer** y se hacen en cualquier orden. El sello del final de cada una abre un atajo de vuelta a la Rotonda.

| Sala | Contenido |
|---|---|
| **00 Prólogo exterior** | Inicio en una terraza elevada a la izquierda del eje, mirando el paisaje, con cierres laterales y posterior de roca facetada. Carteles de mover y saltar junto al inicio. Un sendero de tierra baja entre bancos de roca continuos (sin pretiles repetidos) y lomas que encuadran el eclipse. La estación de observación está oculta durante el prólogo. La entrada al cráter es una abertura en el piso. |
| **01 Explanada y pasaje** | El fondo del pozo, bajo la totalidad. Se baja por una escalera entre paredes talladas con lunas y soles. En el pasaje, los dibujos de historia 01–08 cuentan, sin palabras, qué pasó y cómo sigue. |
| **02 Umbral** | Sala de paso hacia la Rotonda, con su guarda escalonada (naranja a la izquierda, azul a la derecha). Ya no tiene ancla ni compuerta: el farol se encuentra en la Rotonda. |
| **03 Rotonda (lobby)** | Sala circular de 37,5 m con plaza hundida, escalinata anular y abertura al cielo con el eclipse. Al llegar está a oscuras: sólo el farol, en el centro, y cuatro velas con llama en la entrada. Desde un punto lateral, fragmentos tallados se alinean por perspectiva y encuadran el farol, y una luz cenital tenue lo realza al encontrar el punto. Al recogerlo se cierra la entrada y se encienden la sala y los accesos a las alas. Al volver con cada sello, sus columnas se encienden una por una con notas ascendentes; con ambos sellos se completa el eclipse central y se abre la puerta del Cruce. La guarda escalonada recorre las paredes. |
| **03a Ala oeste (SOL)** | Arte del sol. Enseñar: un abismo y dos anclas separadas, a las que hay que apuntar por turno. Probar: una puerta permanente con un ancla en el rincón del fondo. Torcer: anclas que cuelgan del techo sobre el otro lado de un pozo. Sello detrás de un tabique. |
| **03b Ala este (LUNA)** | Arte de la luna. Enseñar: un muro de rejas; las dos rejas iniciales son una elección a ciegas (el ramal sur termina en una cámara de reparación, el norte continúa). Probar: la sala no tiene salida; el camino es una trampilla que da a una rampa descendente y a una galería. Torcer: una rampa con pretiles que termina en una reja vertical. Sello: una placa redonda de materia hueca que queda disuelta. |
| **04 Cruce** | LUNA revela un ancla en un nicho y SOL forma un puente descentrado. Un segundo tramo lleva a una isla segura donde se puede parar. LUNA revela las anclas de salida, SOL forma el último tramo (hay que correr) y LUNA abre el paso final. Una antesala lleva a la Cresta. |
| **05 Cresta** | Sala circular. Se cierra a la espalda. Reflejo del farol en todas las superficies; al apagarlo, tallados latentes y adaptación; el techo se abre y entra la luz blanca. |
| **06 Estación de observación (epílogo)** | Al atardecer, sin farol. La estación aparece por primera vez, con el sol en otra posición. El cráter desapareció. Cruzar su antiguo lugar, o esperar 45 s, trae los créditos. |

---

## 5. Dirección de arte

- **Kit modular low-poly** (Blender) y geometría facetada generada por código: anclas, rejas, puentes, farol, arquitectura y motivos tallados.
- **Paleta:** basalto casi negro; ámbar para SOL; azul de luna para LUNA; plateado frío para el eclipse.
- **Lo que brilla, se usa.** Sólo brilla lo funcional o lo que responde al jugador: farol, anclas, sellos, columnas, velas, tallados latentes. El arte decorativo es mate y no brilla, así no se confunde con algo para iluminar.
- **Iluminación con fuente visible.** Se retiraron las luces flotantes de salas, pasillos, murales y carteles. Se conservan el farol, las velas con llama, las lámparas de anclas, los haces que entran por aberturas reales, los aros de las columnas y el rebote del propio farol.

### 5.1 Tutorial vs. historia

Dos lenguajes visuales separados:

- **TUTORIAL = carteles físicos.** Placa clara con marco sobre un soporte, una sola acción por cartel, pictograma con silueta lisa y un recuadro donde el juego dibuja la tecla o el botón Xbox según el dispositivo. Hay 10 carteles: mover, saltar, recoger piedra, SOL, LUNA, volver a blanco, dos anclas, correr, combinar y sala final. Recoger y prender el farol se indican con avisos contextuales.
- **HISTORIA = dibujos en las paredes.** Pictogramas mates de estilo rupestre andino, con un marco fino de piedra mate y sin teclas ni flechas. El viajero lleva sombrero y poncho con guarda. Hay 12 dibujos integrados: 01–08 en el pasaje, 09 (el farol, ampliado) en la Rotonda, 10 (dos caminos) en la entrada de la Rotonda, 11 en la puerta de los sellos y 12 (el ciclo) en el corredor de la Cresta.
- **Guarda escalonada:** franja continua naranja a la izquierda y azul a la derecha en el Umbral y la Rotonda.

### 5.2 Otros elementos

- **Estación de observación:** refugio secular de adobe y cal, con instrumental y patio con pirca; sin elementos religiosos. Sólo aparece en el epílogo. Los dibujos de historia representan este edificio.
- **Cresta:** sala circular de facetas continuas; cinco tallados latentes (ojo encandilado, ojo cerrado y estrellas) que aparecen al apagar el farol.
- **Grutas:** penumbra roja tenue, con dibujos de vida cotidiana y escalera de regreso.
- **Post-procesado:** ACES, bloom, viñeta y grano fino. La adaptación a la oscuridad usa la exposición.

---

## 6. Audio

Los sonidos se sintetizan en `GeneradorAudioCrater` y pueden reemplazarse por un WAV grabado con el mismo nombre en `Assets/Audio/Grabado/`. Reconstruir nunca reescribe un WAV existente.

| Momento | Sonido |
|---|---|
| Exterior | Viento exterior original (`exterior_capilla`, volumen 0,20); sin pájaros ni campana |
| Eclipse | Grave del eclipse (0,18) que se desvanece al abrirse el cráter; apertura de tierra y piedra (0,35) |
| Subsuelo | Ambiente interior de aire filtrado, bajo; el exterior se detiene al bajar |
| Cierre de la entrada | Piedra (0,40) al recoger el farol |
| Anclas | Resonancia tenue en carga y tono de activación; los pares forman intervalos del mismo acorde |
| Columnas | Notas ascendentes localizadas al volver con cada sello |
| Techo de la Cresta | Retumbo al abrirse; después, silencio |
| Final | Silencio → tono agudo del anillo de diamante → exterior |
| Créditos | Acorde lento en La mayor, el mismo de las anclas |

---

## 7. Estado

### Hecho
- [x] Prólogo caminable: terraza, sendero, lomas, eclipse con estrellas y apertura del cráter sin bloquear al jugador
- [x] Rotonda oscura con el farol como foco, cierre de la entrada y encendido al recogerlo
- [x] Filtros sostenidos de 3 s con HUD de segundos, salto corto y nuevo esquema de controles
- [x] Carteles físicos de tutorial con glifos de teclado/Xbox y dibujos de historia en las paredes
- [x] Grutas rojas de seguridad, Cresta circular con reflejo continuo, anillo de diamante y epílogo
- [x] Ocultamiento del paisaje exterior al estar en el subsuelo (optimización)
- [x] Validador sin errores; EditMode 36/36; pruebas PlayMode de los sistemas afectados aprobadas

### Pendiente
- [ ] **Arte:** integrar las imágenes nuevas ya identificadas en `images/carteles` y `images/murales_historia` (carteles 12–15; murales 13 SOL, 14 LUNA, 15 Cruce, 16/16b/16c grutas, 17 caída).
- [ ] **Arte:** generar las imágenes que faltan: cartel 05 (SOL con una sola ancla) y murales 01 y 02 sin farol, 03 (el cráter se abre) y 04 (la bajada). Hoy SOL y dos anclas comparten imagen.
- [ ] **Tests:** la suite PlayMode histórica completa no está verde (17 fallos heredados, varios por expectativas desactualizadas del recorrido anterior). Hay que adaptarla.
- [ ] **Playtest:** encuadre del prólogo, comprensión del farol y la sala final, legibilidad de las grutas rojas, comodidad del salto y de los filtros sostenidos.
- [ ] **Audio:** escucha en los parlantes del aula y con auriculares.
- [ ] **Build:** generar y medir rendimiento en el equipo objetivo.
- [ ] **Instalación (Magma):** joystick con botones personalizados (sol, luna, farol) y glifos del juego que coincidan.
- [ ] **Créditos** definitivos.

### Descartado
- Filtro RASTRO, la Lente y la pista forzada de la Galería.
- Mirillas, portales, focos sobre anclas colgadas, surcos, miguitas, ventanas de retorno, zigzag antes de la Cresta y el arte de cobre.
- Ancla de luz blanca y compuerta del Umbral; obelisco visible en el centro de la Rotonda (reemplazado por el farol).
- Teclas 1/2/Q para los filtros y el temporizador automático de filtros.
- Carteles de stickman y murales de mecánica pintados en la pared (reemplazados por carteles físicos).

---

## 8. Documentos de referencia

| Tema | Documento |
|---|---|
| Iteración más reciente: inicio, encuadre, guarda, Cresta | `Docs/Revision20261105.md`, `Docs/Seguimiento20261105.md` |
| Recorrido, arte, luces, filtros sostenidos, audio, Cresta circular | `Docs/Revision20261104.md`, `Docs/CorreccionBordesEncuadre20261104.md` |
| Cielo estrellado | `Docs/CieloEstrellado20261104.md` |
| Carteles, historia y correcciones de entrada/cielo/farol | `Docs/CartelesYMurales20261102.md` |
| Iteración de diseño, arte y jugabilidad (salto, E/B, lobby) | `Docs/Iteracion20261102.md` |
| Identificación de imágenes nuevas | `Docs/Arte/Identificacion20261103/Informe.md` |
| Estado general y exploración | `Docs/EstadoActualCrater.md`, `Docs/ExploracionYConjunto.md` |
| Sellos y columnas | `Docs/SellosEnObelisco.md` |
| Rendimiento | `Docs/OptimizacionRendimiento.md` |

Los nombres técnicos que conservan `Capilla` u `Obelisco` siguen por compatibilidad de referencias.
