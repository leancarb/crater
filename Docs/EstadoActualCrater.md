# CRÁTER — estado vigente

6 de octubre de 2026. Este documento y `ExploracionYConjunto.md` prevalecen sobre documentos y capturas de diseños posteriormente revertidos. La escena se genera desde código; el Editor contiene la revisión actual. No se produjo un EXE en esta iteración.

## Recorrido y escala

Se conserva el arte restaurado: modelos originales de linterna, anclas, rejas y conjunto central, paleta, pintura mate, pictogramas y superficies facetadas. Las alas anteriores permanecen; el Cruce nuevo conserva nicho, puente descentrado, dos tramos e isla segura. No vuelven mirillas, portales, focos sobre anclas colgadas, surcos, miguitas, ventanas de retorno, zigzag ni acentos de cobre.

La ampliación horizontal del subsuelo sigue siendo del 25% por eje, con alturas, escala y velocidad del jugador originales. La arquitectura se ensancha; los modelos, anclas, murales y figuras se recolocan conservando proporciones y rotaciones. Las rejas se amplían uniformemente para cubrir los pasos. La rotonda tiene 37,5 m de diámetro, plaza hundida facetada, escalinata anular continua y obelisco centrado.

## Comunicación y pausas

Las caras SOL/LUNA del obelisco y las columnas corresponden a los destinos de las alas. Dos marcas discretas junto a la puerta muestran qué sellos requiere y se encienden con ellos. Cada regreso conserva la cinemática del hilo de luces; con ambos sellos se abre la puerta. Se quitó el giro adicional hacia el obelisco. Pausa, salto y devolución segura del control se conservan.

En SOL, las anclas colgantes bajan 40 cm para entrar en el encuadre del cambio de dirección, junto al mural existente. El tabique sigue ocultando el sello hasta rodearlo; un rebote tenue aparece del lado oculto. No se añade iluminación sobre esas anclas.

En LUNA, las dos rejas iniciales siguen siendo una elección a ciegas: pantallas de piedra ocultan el fondo y exigen doblar. El ramal sur termina en una cámara de reparación con una persona, la junta del piso y escalones pintados. Es una pista ambiental, sin mapa ni solución completa. La abertura de regreso conserva contraste y ambas rejas funcionan desde ambos lados.

La trampilla tapa una rampa de descenso a la galería; no hay que dejarse caer. La salida de la tercera sala es una reja vertical al final de la rampa, con pretiles en ambos bordes. Esa reja y la de la trinchera permanecen abiertas una vez resueltas. Las luces de LUNA tienen rangos menores y sombras preservadas para evitar fugas por las paredes. Los paneles explicativos superpuestos del Cruce se retiraron; se conserva su arte decorativo.

## Primer ancla, gruta y detalles

El primer par SOL establece el criterio de interacción: brillo progresivo desde el primer contacto, resonancia tenue en carga, vibración mínima de sus aros, tono de activación limitado y pérdida de brillo gradual. El sonido de carga sale suavemente; mantener el haz no repite el tono. El par inicial retiene 4 s y los colgantes 5,5 s tras perder la luz; la retención temporal de rejas comunes pasa a 6,25 s. El Cruce retiene 8 s en el puente descentrado, 6 s hasta la isla y 7 s hacia la salida.

La gruta O1 reúne un asiento de piedra, un cuenco y tres sectores tallados con diferentes resonancias suaves al apuntar. El mural de regreso queda separado; el conjunto está fuera de la escalera. Es opcional y no abre puertas ni modifica sellos. Las cuatro grutas permiten regresar sin respawn ni pérdida de progreso. Un banco y cuenco en la rotonda y un apoyo con cuenco al costado de la isla segura sugieren uso cotidiano con moderación. No se atribuyen significados culturales auténticos a estos objetos.

## Eclipse, exterior y final

El eclipse conserva 7° de diámetro aparente. En el exterior está a 14°; sólo al bajar al subsuelo sube a 75° para alinearse con la abertura circular. Al regresar opcionalmente por la escalera recupera el horizonte inicial. El epílogo usa otra posición solar (18° de elevación y azimut 30°), con tonos cálidos para mostrar el paso del tiempo. La totalidad espera cuatro segundos de observación desde el mirador: mirar fuera del encuadre pausa el contacto, con mirada libre y movimiento detenido. El cráter se abre sin destello ni contorno luminoso. Reproduce el audio de piedra existente desde una fuente activa e independiente de la puerta oculta, con volumen 0,8 y distancia mínima 14 m para cubrir el mirador. Su pitch 0,8 extiende los 3,2 s a los 4 s de apertura. El grave previo se desvanece en el primer segundo y se detiene para no taparlo.

Sobre el obelisco hay una apertura circular física de 16 m de diámetro al exterior y luz tenue. La elevación del sol se ajustó para que el disco exterior sea visible desde suelo seguro de la plaza. Los discos y la corona usan un shader celeste sin niebla: antes la niebla interior borraba la vista aun con una abertura despejada. La profundidad mantiene su ocultación por techos y paredes. Desde el sector sur se observa el mismo eclipse de la aproximación, sin una imagen falsa de cielo pegada al techo. El cierre del terreno y los límites exteriores dejan libre esa vista; las paredes de la escalera y sus juntas siguen cerradas, con tallados apoyados a 2 cm.

La Cresta refleja el foco en paredes, piso y techo, con recorte en cada superficie. Al apagar se percibe una primera respuesta de exposición y viento en menos de un segundo. La adaptación empieza tras 0,4 s y continúa durante 11 s; encender la linterna la pierde en 1,2 s. Se conservan pictogramas, tallados latentes, apertura del techo, anillo de diamante y regreso al exterior.

El antiguo edificio religioso es una estación de observación secular: adobe, cal, alero, instrumental, mesas y cajas. Los pictogramas narrativos usan su silueta secular. El terreno caminable cubre también la parte posterior y sus límites impiden caer al llano decorativo. Cruzar el antiguo lugar del cráter, con terreno cerrado y sin marca visible, dispara los créditos; 45 s de exploración libre ofrecen una alternativa. Los identificadores técnicos con `Capilla` permanecen por compatibilidad.

## Audio y validación

Se retiró la reproducción continua del ambiente global `ambiente_crater.wav`, percibido como oleaje. El WAV y su huella se conservan. Entrar directo, mezclar al cráter y salir ya no reinician ese bucle; los sonidos localizados y la adaptación de Cresta permanecen.

Los pasos bajaron de volumen 0,45 a 0,20 y el material del collider selecciona piedra, tierra, madera o metal. La cadencia aumenta con la velocidad real al correr. Se conservaron los WAV anteriores y las huellas previas; se añadieron bancos de madera/metal y una resonancia procedural suave.

La ampliación actual pasó 25/25 PlayMode en tres suites (18 de recorrido/mecánicas, 5 de exploración y 2 de mando) y 23/23 EditMode. Se verificaron ambos órdenes, final sin piezas/parcial/completo, cuatro recuperaciones, reinicio, pausa y coexistencia con cinemáticas. Reconstrucciones repetidas conservaron instancias/IDs y el validador devolvió 0 problemas de referencias/alcance del haz. Los 43 WAV y 43 huellas previos siguen idénticos. Resultados, capturas, medición de salida sonora y coste del Editor: `ExploracionYConjunto.md`. La revisión anterior queda como historial en `RevisionContemplativa.md`. Las pruebas técnicas no prueban curiosidad, comprensión, comodidad ni disfrute; requieren jugadores. También quedan pendientes escucha de mezcla, mando físico y rendimiento GPU en el equipo objetivo.

## Exploración opcional vigente

Se conserva la revisión anterior y se añaden seis piedras talladas, independientes de filtros y sellos. Espacio/Enter o A (×) recoge al mirar a menos de 2,5 m, con línea de visión y aviso contextual. La pausa muestra x/6. Las caídas no pierden piezas; reiniciar la escena limpia el conjunto. No hay persistencia entre partidas.

Distribución: mirador lateral exterior, veta de SOL, desvío sur de LUNA, asiento de O1, cavidad lateral de la rotonda y antesala del Cruce. Se puede volver al exterior por la escalera abierta; el conjunto se completa antes del cierre de la Cresta. El final funciona sin piezas.

El conjunto está al noroeste de la plaza, fuera del mapa central. Al completar espera cercanía y mirada, enciende gradualmente sus tallados y su proyección estilizada, y reproduce seis resonancias sin girar ni bloquear al jugador. Respeta pausa y cinemáticas de sellos. Después queda iluminado y permite reescucha con la misma acción.

Un cuenco refleja una fuente física mediante una sonda local de 64 px, capturada al acercarse y tras cambios de sellos; no cada fotograma. Un halo fragmentado se reúne por perspectiva, una cavidad modifica suavemente el viento, una veta responde al ángulo del haz, O1 incorpora reparación y piedras ordenadas, un detalle aparece con el primer sello, polvo limitado a 20 partículas acompaña la abertura y un mirador lateral ofrece vista y regreso seguro.

Los murales de Q están separados del ornamento. Los giros tras las pantallas iniciales de LUNA reciben un rebote tenue sin descubrir los destinos. La luz blanca deja de reducir su potencia por proximidad a paredes. Las cuatro velas de entrada conservan sombras a distancia además de las cuatro luces reservadas de LUNA.

Corrección posterior de eclipse/audio: `CorreccionEclipseAudio.md`. El cerro E se apartó más allá del límite caminable porque tapaba una vista real por el óculo; no se eliminó ni se abrió el terreno jugable.

### Diagnóstico posterior de cortes de audio (6/10/2026)
El usuario informa interferencia generalizada. Se reinicializó el motor de salida sin cambiar configuración ni WAV. Capturas continuas del exterior, tono de control, apertura y filtros no reprodujeron cortes internos ni saturación. La causa y la escucha física siguen pendientes; no se considera resuelto. Ver `Docs/DiagnosticoCortesAudio.md`. La sonda temporal se retiró de Assets y se archivó en Logs/Audio.

### Legibilidad y regreso sin rastro (6/10/2026)
`LegibilidadYEpilogo.md` prevalece para los detalles ambientales y el regreso. Cuenco/reflejo, encuadre, veta, reparación O1, revelación de losa, polvo y mirador se revisan desde vistas del jugador. El traslado final cierra Cresta, óculo y ranura exterior; restaura terreno facetado sin patrón radial, oculta huella, tapa circular y guiño de la casa. El disparador invisible de créditos y la colección opcional conservan sus reglas. El atardecer tiene otra dirección; volver por la escalera durante la exploración conserva el eclipse inicial.

Corrección posterior: el Banco_Trabajo_Exterior tenía sólo el tablero a 0,8 m y flotaba en el patio. ConstructorCrater.Capilla.cs genera ahora cuatro patas, encastradas entre patio y tablero. Sin cambios de mecánicas o audio. El Cuenco_Isla del Cruce es ambiental sin interacción; el agua reflectante pertenece sólo al Cuenco_Rotonda. La colección completa se aprecia regresando al panel lateral de la rotonda y mirándolo antes de entrar a la Cresta; después se puede volver a escuchar con Espacio.

Decisión posterior del usuario: conservar el cuenco y su agua, retirar la columna rectangular con el sol y su foco asociado (Apoyo_Origen_Reflejo, Origen_Reflejo y Luz_Origen_Reflejo). El agua refleja el entorno existente. Las capturas anteriores con esa columna son históricas.
