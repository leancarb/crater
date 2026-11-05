# CRÁTER — Game Design Document

> Version 0.9 · Vertical slice hecho en Unity 6 con URP · PC, teclado y mouse o joystick
>
> Este documento lo vamos actualizando a medida que probamos el juego y tomamos desiciones. Describe como esta el juego hoy.

---

## 1. Concepto

**En una frase:** durante un eclipse total se abre en un valle andino un cráter que solo existe bajo esa luz. Adentro, un farol con dos filtros le enseña al jugador a ver, hasta que descubre que para salir tiene que apagarlo.

**Género:** puzzle ambiental en primera persona, contemplativo.

**Tono:** queremos que el jugador sienta asombro y ganas de descubrir. No hay combate ni enemigos. Si te caes en un abismo terminas en una gruta con luz roja y volves caminando por una escalera, no hay muerte ni respawn.

**Duración:** es un vertical slice, se juega en una sola sentada corta.

### Pilares

1. **La luz es el verbo.** Todo lo que haces en el juego pasa por el haz del farol: iluminar es actuar.
2. **Ver es un acto.** Cada filtro es una forma distinta de mirar las cosas, y el final te obliga a mirar sin ninguna herramienta.
3. **Ritmo tranquilo.** Se camina, se puede correr un poco y hay un salto chiquito que no sirve para saltearse los puzzles. La tensión viene de entender, no de reaccionar rápido.
4. **Interfaz mínima.** No hay mira ni barras. Solo se ve que filtro tenés puesto. Los controles se enseñan con carteles dentro del mundo y la historia con dibujos en las paredes. Las ayudas escritas son opcionales y vienen apagadas.

---

## 2. Narrativa

### Premisa

El jugador arranca en una terraza en lo alto, de dia, mirando el valle. Bajando por un sendero entre lomas, la luna empieza a tapar el sol. Cuando llega la totalidad la tierra se abre adelante: aparece un cráter que antes no estaba, con una escalera que baja. Abajo hay un templo olvidado, lleno de soles y lunas tallados, y en el centro hay un farol.

El eclipse dura lo mismo que el jugador se quede adentro. El cráter es un lugar medio de sueño: no se explica, se atraviesa.

### Arco

| Momento | Que pasa | Que siente el jugador |
|---|---|---|
| **Prólogo** | Arrancas en la terraza y bajas por un sendero entre lomas que te hacen mirar al cielo. El eclipse avanza mientras caminas. En el mirador llega la totalidad y la tierra se abre adelante, sin cortes y sin que pierdas el control. | Curiosidad, extrañeza. |
| **Descenso** | Bajas por la boca del cráter hasta el fondo. En el pasaje las paredes cuentan la historia con dibujos, y se llega a la Rotonda. | Descubrimiento. |
| **El farol** | La Rotonda esta a oscuras y lo unico que brilla es el farol en el centro. Cuando lo agarras se cierra la entrada atras tuyo y se prende la sala, junto con los caminos a las alas del sol y de la luna. | Hallazgo, compromiso. |
| **Aprendizaje** | Las dos alas se hacen en el orden que quieras: SOL construye puentes de luz y LUNA atraviesa materia. Cada una termina en un sello. Con los dos sellos se abre el Cruce, donde hay que combinar los filtros. | Dominio, asombro. |
| **La oscuridad** | Al entrar a la última sala, la Cresta, se cierra el camino atras tuyo. Las paredes te devuelven el reflejo de tu propio farol. Si lo apagas y esperas, aparecen tallados escondidos. | Revelación, encierro. |
| **Final** | Cuando tus ojos se acostumbran a la oscuridad se abre el techo y entra la luz del final del eclipse hasta llenar todo de blanco. Silencio, el anillo de diamante, y aparecés afuera junto a la estación de observación, al atardecer. | Cierre tranquilo. |
| **Epílogo** | La estación aparece por primera vez y el cráter desaparecio sin dejar rastro. El sol cambio de lugar y la luz es calida. Caminando hasta donde estaba el cráter, o explorando un rato, aparecen los créditos. | Pérdida suave, confirmación. |

### El eclipse

- **Afuera** se ve en el cielo. No es de noche sino un crepúsculo profundo, y de a poco aparecen estrellas. Las montañas y las paredes tapan el cielo como corresponde.
- **Adentro** el eclipse queda congelado. Desde la Rotonda se ve el mismo eclipse real por una abertura en el techo.
- **Termina con el anillo de diamante**, el destello real con el que termina un eclipse: la luz vuelve de golpe, como cuando prendés la luz despues de estar a oscuras.

### Tema

La herramienta que nos deja ver tambien decide que vemos. El farol te enseña dos maneras de mirar, pero la última puerta solo aparece cuando dejás de iluminar.

---

## 3. Mecánicas

### 3.1 Controles

| Acción | Teclado y mouse | Joystick (Xbox) |
|---|---|---|
| Moverse / mirar | WASD y mouse | stick izquierdo y stick derecho |
| Correr | **Shift** | **RT** |
| Salto corto | **Espacio** | **A** |
| Agarrar / interactuar / saltear animaciones | **E** | **B** |
| Prender y apagar el farol | **F** | **X** |
| Filtro SOL (mantener) | **Clic izquierdo** | **RB** |
| Filtro LUNA (mantener) | **Clic derecho** | **LB** |
| Volver a la luz blanca | Soltar el filtro | Soltar el filtro |
| Pausa | Esc | Start |

Los carteles y la pausa muestran las teclas o los botones del joystick según con que estes jugando.

### 3.2 El farol

- Esta en el centro de la Rotonda, que esta a oscuras. Es lo primero que ves al llegar.
- Cuando lo agarras se cierra la entrada y se va prendiendo la sala y los caminos a las alas.
- La luz blanca alumbra mas lejos que los filtros.
- El haz activa todo lo que alcanza a iluminar.

### 3.3 Filtros

Los filtros funcionan mientras mantenés apretado el botón, pero duran poco. Cuando soltás, o cuando se termina el tiempo, el farol vuelve solo a la luz blanca. En pantalla se ve que filtro tenés puesto y cuanto le queda. Los dos filtros tienen el mismo tamaño de haz.

| Filtro | Color | Sobre que actúa | Qué hace |
|---|---|---|---|
| **SOL** | Ámbar | Anclas de piedra → puentes de luz | Prende las anclas si las iluminás de frente. Cuando todas las anclas de un puente estan prendidas, el puente aparece. Las anclas aguantan un ratito prendidas despues de que dejás de iluminarlas. |
| **LUNA** | Azul | Rejas de vidrio azul | Mientras las iluminás se disuelven y se pueden atravesar. Una reja sólida tapa la luz, asi que lo que hay atras no se prende. Algunas rejas y trampillas quedan abiertas una vez que las resolvés. |

- **Sellos:** estan al final de cada ala. Cuando los prendés quedan prendidos para siempre, abren un atajo de vuelta a la Rotonda y, con los dos, se abre el Cruce.
- **Puertas y puentes:** las puertas tienen un solo ancla y quedan abiertas; los puentes tienen dos anclas y duran un rato. Las anclas de las puertas estan sobre una base de piedra clara para diferenciarlas.
- **Salto:** es chiquito a proposito, para que no se puedan saltear los puzzles.

### 3.4 Redes de seguridad

Debajo de cada abismo hay una gruta con luz roja tenue, con un dibujo en la pared y una escalera que te devuelve a antes del desafío. Caerse no te hace perder nada.

### 3.5 Adaptación a la oscuridad

Solo pasa en la Cresta. Cuando apagás el farol, la imagen y el sonido del viento cambian de a poco, como si tus ojos se acostumbraran. Si lo volvés a prender se pierde. Cuando estas adaptado se abre el techo.

### 3.6 La Cresta

- Es una sala circular con una abertura en el techo tapada.
- Con el farol prendido, las paredes, el piso y el techo te devuelven un reflejo que encandila, del color del filtro que tengas. Con el farol apagado el reflejo desaparece y aparecen tallados escondidos.
- Cuando entrás se cierra el camino atras tuyo.
- Cuando te adaptás a la oscuridad se abre el techo y entra la luz del final del eclipse.

### 3.7 Exploración opcional

Hay seis piedras talladas escondidas que juntas forman un halo. Se agarran mirándolas y apretando E o B, y en la pausa se ve cuantas tenés. No hacen falta para terminar el juego. Cuando juntás todas, el conjunto se prende y suena una pequeña melodía. Tambien hay detalles opcionales para descubrir: un cuenco con agua que refleja, un halo que se arma mirando desde el lugar justo, una cavidad que suena distinto y una gruta con un asiento y tallados que responden a la luz.

---

## 4. Recorrido

1. **Exterior:** terraza → sendero y lomas → mirador (totalidad) → boca del cráter
2. **Descenso:** fondo del cráter → pasaje → Umbral → Rotonda (farol)
3. **Alas, en cualquier orden:** Ala oeste (SOL) y Ala este (LUNA) → sellos
4. **Final:** Cruce → Cresta → (blanco) → estación de observación (epílogo) → créditos

Cada ala sigue la idea de enseñar, probar y torcer, y se puede hacer en el orden que quieras. El sello al final de cada una abre un atajo de vuelta a la Rotonda.

| Sala | Qué hay |
|---|---|
| **Prólogo exterior** | Arrancás en una terraza alta a un costado, mirando el paisaje, con roca alrededor para que no te caigas. Al lado hay carteles para moverse y saltar. Un sendero baja entre lomas que te hacen mirar el eclipse. La estación de observación todavia no se ve. La entrada al cráter es una abertura en el piso. |
| **Fondo del cráter y pasaje** | Se baja por una escalera con lunas y soles tallados. En el pasaje, dibujos en las paredes cuentan la historia sin palabras. |
| **Umbral** | Una sala de paso hacia la Rotonda, con una guarda escalonada naranja de un lado y azul del otro. |
| **Rotonda (lobby)** | Una sala circular grande con una plaza hundida y una abertura al cielo donde se ve el eclipse. Al llegar esta a oscuras, solo brilla el farol y unas velas en la entrada. Desde un costado, unos tallados se alinean por perspectiva y enmarcan el farol. Cuando lo agarrás se cierra la entrada y se prende todo. Cuando volvés con un sello se prenden sus columnas una por una con notas musicales, y con los dos sellos se abre la puerta del Cruce. |
| **Ala oeste (SOL)** | Todo el arte es del sol. Primero un abismo con dos anclas separadas que tenés que iluminar de a una. Despues una puerta con un ancla en el rincón. Al final, anclas colgadas del techo del otro lado de un pozo. El sello esta escondido atras de un tabique. |
| **Ala este (LUNA)** | Todo el arte es de la luna. Primero un muro de rejas: hay dos caminos y uno termina en una sala de reparación sin salida. Despues una sala sin salida donde el camino es una trampilla en el piso que baja a una galería. Al final, una rampa que termina en una reja. El sello es una placa redonda que queda disuelta. |
| **Cruce** | Acá se combinan los dos filtros: LUNA para descubrir anclas escondidas y SOL para armar los puentes, con una isla en el medio para descansar. El último tramo hay que correrlo. Despues hay una antesala que lleva a la Cresta. |
| **Cresta** | La sala final. Se cierra atras tuyo, el reflejo te encandila, y al apagar el farol aparecen los tallados y se abre el techo. |
| **Estación de observación (epílogo)** | Al atardecer, sin farol. La estación aparece por primera vez y el cráter ya no esta. Caminando hasta donde estaba, o explorando un rato, terminan los créditos. |

---

## 5. Dirección de arte

- **Estilo low poly** hecho con un kit modular en Blender y geometría facetada: anclas, rejas, puentes, el farol, arquitectura y tallados.
- **Paleta:** basalto casi negro, ámbar para el sol, azul para la luna y plateado frio para todo lo que es eclipse.
- **Lo que brilla se usa.** Solo brilla lo que tiene una función o le responde al jugador: el farol, las anclas, los sellos, las columnas, las velas y los tallados escondidos. El arte decorativo es mate, asi nadie piensa que tiene que iluminarlo.
- **Luz con fuente.** Sacamos las luces que no tenian de donde salir. Ahora la luz viene del farol, de velas, de las anclas o de aberturas al cielo.

### 5.1 Tutorial e historia

Usamos dos lenguajes visuales separados para que no se mezclen:

- **TUTORIAL = carteles.** Son carteles físicos sobre un soporte, claros y con marco, con una sola acción cada uno y un dibujo simple. En el recuadro de la tecla el juego muestra la tecla o el botón del joystick según con que estes jugando. Hay carteles para moverse, saltar, agarrar piedras, los dos filtros, volver a la luz blanca, las dos anclas, correr, combinar y la sala final.
- **HISTORIA = dibujos en las paredes.** Pictogramas mate de estilo rupestre andino, sin teclas ni flechas. El viajero tiene sombrero y poncho con guarda. Estan en el pasaje, en la Rotonda, en la puerta de los sellos y en el camino a la Cresta.
- **Guarda escalonada:** una franja naranja de un lado y azul del otro en el Umbral y la Rotonda.

### 5.2 Otros elementos

- **Estación de observación:** un refugio de adobe y cal con instrumentos y un patio con pirca, sin nada religioso. Solo aparece en el epílogo, y los dibujos de la historia muestran este edificio.
- **Cresta:** sala circular con tallados escondidos (un ojo encandilado, un ojo cerrado y estrellas) que aparecen al apagar el farol.
- **Grutas:** luz roja tenue, dibujos de vida cotidiana y una escalera para volver.
- **Post-procesado:** tonos cinematográficos, un poco de brillo, viñeta y grano fino.

---

## 6. Audio

Los sonidos se generan desde el proyecto y se pueden reemplazar por grabaciones propias con el mismo nombre.

| Momento | Sonido |
|---|---|
| Exterior | Viento suave, sin pájaros ni campana |
| Eclipse | Un sonido grave que crece y la tierra y la piedra abriendose |
| Abajo | Un ambiente de aire bajito; el viento de afuera se apaga al bajar |
| Al agarrar el farol | La piedra que cierra la entrada |
| Anclas | Una resonancia cuando las cargás y un tono cuando se prenden; los pares forman un mismo acorde |
| Columnas | Notas que suben cuando volvés con cada sello |
| Techo de la Cresta | Un retumbo cuando se abre y despues silencio |
| Final | Silencio, el tono agudo del anillo de diamante y otra vez el viento de afuera |
| Créditos | Un acorde lento, el mismo de las anclas |

---

## 7. Estado

### Hecho
- [x] Prólogo caminable con la terraza, el sendero, las lomas, el eclipse con estrellas y el cráter que se abre sin cortes
- [x] Rotonda a oscuras con el farol como foco, que se cierra y se prende cuando lo agarrás
- [x] Filtros que se mantienen apretados, salto corto y los controles nuevos
- [x] Carteles de tutorial con teclas y botones de joystick, y dibujos de historia en las paredes
- [x] Grutas rojas, la Cresta circular con el reflejo, el anillo de diamante y el epílogo
- [x] Optimización: el paisaje de afuera no se dibuja cuando estas abajo

### Pendiente
- [ ] **Arte:** poner en el juego las imagenes nuevas que ya hicimos (carteles nuevos y dibujos de las alas, el Cruce, las grutas y la caida).
- [ ] **Arte:** hacer las que faltan: el cartel del sol con una sola ancla y los primeros dibujos de la historia, donde el viajero todavia no tiene el farol.
- [ ] **Tests:** actualizar las pruebas automáticas que todavia esperan el recorrido viejo.
- [ ] **Playtest:** ver si se entiende el prólogo, el farol y la sala final, si se leen las grutas rojas y si son comodos el salto y los filtros.
- [ ] **Audio:** probar el sonido en los parlantes del aula y con auriculares.
- [ ] **Build:** sacar un ejecutable y probar el rendimiento en la compu de la expo.
- [ ] **Instalación en Magma:** joystick con botones personalizados (sol, luna, farol) que coincidan con los iconos del juego.
- [ ] **Créditos** definitivos.

### Lo que descartamos
- El filtro RASTRO, la Lente y la pista forzada de la Galería.
- Mirillas, portales, focos sobre las anclas colgadas, surcos, miguitas, ventanas para mirar atras, el pasillo en zigzag y el arte de cobre.
- El ancla de luz blanca y la puerta del Umbral, y el obelisco en el centro de la Rotonda (ahora ahi esta el farol).
- Las teclas 1, 2 y Q para los filtros.
- Los carteles con stickman y los murales de mecánica pintados en la pared (ahora son carteles físicos).
