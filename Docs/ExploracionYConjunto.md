# CRÁTER — exploración opcional y conjunto de piedras

6 de octubre de 2026. Este informe complementa `EstadoActualCrater.md` y el GDD vigente. `RevisionContemplativa.md` documenta la revisión anterior: sus capturas y resultados son históricos donde cambiaron los valores. No se produjo un EXE.

## Criterio y conservación

Se conservan arte restaurado, estética facetada, paleta, obelisco, dos alas originales, Cruce e isla segura, cuatro recuperaciones, anclas pulidas, respuesta de oscuridad, cinemáticas de sellos y relato del eclipse. Las adiciones son opcionales: tienen una insinuación y una respuesta sensible, sin nuevos controles obligatorios, soluciones ni atribuciones culturales inventadas. Las referencias orientan decisiones de intimidad, observación y respuesta; no se trasladan literalmente elementos ajenos al mundo.

Se prototipó primero una piedra, su representación y una cavidad resonante. Se comprobó la recogida mediante teclado virtual, no duplicación y reinicio; se inspeccionaron antes/después y el fragmento visible. La inspección detectó y corrigió una oclusión del fragmento por su fondo antes de distribuir los otros cinco.

## Correcciones solicitadas

- Apertura: vuelve el sonido de piedra existente, sin recuperar el destello descartado. Su fuente pertenece a sistemas activos y está situada en la puerta; no queda dentro del objeto oculto durante el comienzo de la animación. Volumen 0,8, radio mínimo 14 m, pitch 0,8 y grave previo desvanecido para que llegue al mirador.
- Velas: cuatro luces de entrada reservan sombras de 128px. Se conservan las cuatro reservas de LUNA de 256px y las tres luces locales seleccionadas. Ya no dependen de acercarse para habilitar sombras.
- Óculo: techo anular de 64 sectores, abertura circular de 16 m. El mismo eclipse usa dos posiciones por recorrido: 14° en la aproximación y 75° al bajar al subsuelo, para verlo por la abertura física. Volver por la escalera o salir al epílogo restaura el horizonte inicial, sin una imagen falsa. El mirador conserva observación libre y totalidad retenida al mirar fuera.
- Q: ambos murales de luz blanca se trasladan a paredes separadas del ornamento que los tapaba.
- Elección lunar: rebotes discretos en los extremos de las pantallas dejan leer los giros; mantienen oculta la solución del otro lado.
- Luz blanca: conserva potencia al acercarse a paredes. El ajuste de iluminancia de los filtros permanece.
- Puentes: SOL inicial 4s y colgantes 5,5s; Cruce descentrado 8s, hasta la isla 6s, salida 7s. Son retenciones tras perder el haz. Rejas comunes 6,25s y mecanismos permanentes se conservan.

## Experiencias distribuidas

| Experiencia | Insinuación desde el recorrido | Recompensa y ubicación |
|---|---|---|
| Reflejo y origen | Rebote pequeño junto al banco sur de la rotonda | Agua en el cuenco refleja una fuente física elevada, localizable al cambiar la mirada. Una sonda URP captura el entorno real; no se dibuja un brillo fingido sobre el agua. |
| Motivo por perspectiva | Sectores de un halo sobre superficies a distintas profundidades | Cerca del acceso sur se reúnen en un encuadre amplio. Margen de 1,6m y resonancia suave; sin progreso obligatorio. |
| Cavidad para escuchar | Luz cálida y abertura lateral al oeste de la rotonda | El viento y su cuerpo grave entran/salen suavemente a lo largo de 8m. La pieza descansa dentro; entrada caminable hacia el este. |
| Veta angular | Relieve oblicuo plateado al oeste de la segunda sala SOL | Su brillo cambia con la incidencia del haz, sin carga, activación ni eventos de mecanismo. |
| Actividad interrumpida | Asiento y cuenco ya presentes en O1 | Unión de reparación y tres piedras ordenadas, pequeños signos de cuidado sin inventar tradición cultural. |
| Descubrimiento al regresar | Una losa lateral junto al conjunto | Tras el primer sello un rebote gradual revela su tallado. Usa eventos existentes de los sellos y no agrega una cinemática. |
| Movimiento lento | Polvo en la iluminación del óculo | Un sistema, máximo 20 partículas, 0,8/s, lento y sólo emitiendo con la fuente encendida. |
| Vista opcional | Desvío y pirca baja del exterior | Plataforma lateral plana con tres bordes y retorno abierto, vista del valle/eclipse y pieza. Se puede regresar por la escalera del cráter antes de Cresta. |

## Piezas y reglas

| Índice / identificador estable | Ubicación | Señal y acceso |
|---|---|---|
| 0 · `exterior_mirador` | Desvío lateral del valle, coordenada local (-12;0,25;65) de `06_Capilla` | Pirca baja y apoyo de piedra. Se entra desde el sendero por el lado hacia su eje; vuelve por la misma entrada. |
| 1 · `sol_veta` | Segunda sala SOL, junto al relieve del lado oeste | Motivo claro pequeño sobre apoyo bajo, al observar la pared después de abrir la puerta permanente. |
| 2 · `luna_reparo` | Ramal sur sin salida de la elección inicial LUNA | Junto a la composición de reparación; se encuentra al mirar alrededor y admite regreso por la misma reja. |
| 3 · `gruta_o1` | Extremo del asiento de la gruta O1 | Se descubre al acercarse al conjunto de descanso. Escalera existente abre su losa desde abajo; la colección no se pierde al caer. |
| 4 · `rotonda_cavidad` | Cavidad cálida en el lateral oeste de la rotonda | Pieza pequeña a ras de un suelo seguro, con viento localizado y aproximación desde el este. |
| 5 · `cruce_antesala` | Lateral oeste de la entrada del Cruce, antes del primer abismo | Apoyo bajo sobre suelo sólido. Permite volver al conjunto por la puerta ya abierta antes del cierre de Cresta. |

Cada pieza es una piedra hexagonal pequeña y plana con un sector tallado; no tiene los aros de anclas ni el lenguaje de filtros y sellos. Se interactúa al mirar a menos de 2,5m y sin obstáculos, mediante Espacio/Enter o A (×). El texto contextual sigue el dispositivo usado. Recoger produce una luz corta y una nota delicada, sin mover la cámara. Un HashSet de IDs registra una sola vez cada pieza; desaparece su instancia y aparece el fragmento correspondiente en el conjunto. La pausa muestra `Piedras talladas · x/6`.

El estado pertenece a la escena y permanece durante caídas y recuperaciones. Reiniciar mediante el flujo existente carga otra escena y limpia la colección. No se usan PlayerPrefs, archivos ni infraestructura de guardado adicional. No se recoge durante pausa, prólogo, cinemáticas de sellos, Cresta, epílogo o créditos. El exterior conserva retorno por la escalera abierta; la oportunidad de completar termina al entrar en Cresta. El final principal no depende de ninguna piedra.

## Acontecimiento y reescucha

El conjunto está al noroeste de la plaza, fuera del mapa central y de los indicadores SOL/LUNA. Forma seis sectores de un halo. Completar la colección lo deja preparado: espera una mirada a menos de 9m y con línea de visión. Una nota muy baja, localizada y espaciada puede invitar a volver. La secuencia dura 5 s y no congela el movimiento ni gira la mirada.

Tallados y luz progresan, seis resonancias, con fuentes independientes para no retocar el tono de notas que continúan sonando, forman una composición y una proyección estilizada del mismo halo aparece sobre la piedra vecina. No es una simulación física de proyector. Pausa detiene tiempo y audio; si una cinemática de sello toma el control, la secuencia espera y continúa al devolverlo. Al terminar quedan brillo y proyección visibles. Mirar a menos de 3 m y usar la misma acción reproduce otra vez la composición, manteniendo encendido el lugar.

## Fuentes y presupuesto

`ConstructorCrater.Exploracion.cs` se ejecuta después de ampliar/facetar el subsuelo para conservar proporciones. Genera seis IDs fijos, referencias explícitas y materiales/mallas con rutas estables. Faceta exclusivamente el nuevo grupo con el tratamiento existente, sin volver a modificar la geometría anterior. Las piedras y las losas se apoyan en superficies físicas; los fragmentos por perspectiva tienen soportes. La reconstrucción limpia y vuelve a crear la escena; no agrega instancias sobre las anteriores. No se agregan paquetes ni assets externos. Runtime: ColeccionPiedras/PiezaTallada/ConjuntoTallado, AguaEnCuenco, CavidadResonante, VetaAngular, MotivoPorPerspectiva, RevelacionAlRegresar y PolvoEnLuz; UI mínima en InterfazCrater.

Sonda: un cubemap de 64 px, seis caras repartidas, excluye jugador y agua, sólo al acercarse y después de cambios de iluminación de sellos. Se habilitó `realtimeReflectionProbes` en los perfiles del proyecto; antes estaba desactivado. No hay captura continua ni cámara adicional. Un ParticleSystem con 20 partículas máximo. Emisión de tallados usa MaterialPropertyBlock; no clona materiales por fotograma. La búsqueda de piedras recorre un array fijo de seis y los sonidos emplean las fuentes existentes.

## Validación actual

Revisión del 6 de octubre de 2026: compilación correcta, validador de referencias/alcance del haz con 0 problemas y escena reconstruida guardada en `Assets/Scenes/CraterVerticalSlice.unity`. Sin EXE. La consola final después de las pruebas no registra errores ni advertencias. Al reconstruir se emiten avisos de los logos FADU/Campos ausentes, que usan el reemplazo textual existente en créditos (`Logs/exploracion_advertencias_final.json`). Ya no aparecen avisos de tallados dentro de las losas del Cruce. Las suites finales se ejecutaron por separado y sus resultados se reúnen en `Logs/exploracion_playmode_final.json`.

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Recorridos y mecánicas PlayMode | 18/18 | `Logs/exploracion_recorridos_final.json` |
| Exploración, estados, entrada contextual, pausa y reset | 5/5 | `Logs/exploracion_interacciones_final.json` |
| Mando/Input System existente | 2/2 | `Logs/exploracion_mando_final.json` |
| EditMode, incluidos IDs/índices y audio personalizado | 23/23 | `Logs/exploracion_editmode_final.json` |
| Reconstrucción repetida | Mismas cantidades, IDs y referencias; 0 problemas | `Logs/exploracion_idempotencia_antes.json`, `Logs/exploracion_idempotencia_despues.json` |
| WAV y huellas protegidos | 43 WAV y 43 huellas anteriores idénticos | `Logs/exploracion_audio_preservado.json` |

Cuatro recorridos hasta créditos comprobaron: SOL→LUNA sin colección, LUNA→SOL sin colección, SOL→LUNA con colección parcial y LUNA→SOL con colección completa. Incluyen las cuatro recuperaciones con progreso conservado, desvío de LUNA, retorno al exterior desde el Umbral, recogidas accesibles y regreso al conjunto antes de Cresta. Las pruebas de entrada recogen realmente con Espacio y A virtuales, rechazan duplicados y reinician a cero; la ruta completa registra piezas mediante la API después de caminar hasta ellas y verificar alcance/visibilidad. Se conserva la pausa, salto y devolución de control de cinemáticas; la secuencia opcional espera cuando otro sistema toma el control.

La revisión detectó y corrigió la fuente inactiva de apertura, la opción que deshabilitaba sondas realtime, una pieza dentro de un pebetero y un conjunto que estorbaba el paso entre alas. También retiró del generador los dos paneles explicativos antiguos del Cruce: reaparecían y ocultaban parte de su mural, en contradicción con la decisión documentada. Permanecen los murales decorativos. Tras esa limpieza se repitieron la suite de exploración (5/5), un recorrido SOL→LUNA completo hasta créditos (1/1, `Logs/exploracion_recorrido_limpieza_final.json`) y la reconstrucción. Una comprobación visual adicional separó los carteles Q de los menhires y repitió el ensayo de capturas/reflejo (1/1, `Logs/exploracion_capturas_revision_final.json`); sus imágenes finales usan su luz ambiental, con la linterna apagada para leer el pictograma. La blanca conserva sus 2.000 de potencia y puede sobreexponer la pintura en el centro del haz a corta distancia; su calibración perceptiva sigue pendiente junto con la comodidad de la iluminación.

Las capturas usan la cámara real del jugador. Los recorridos completos caminan con CharacterController, colisiones y haz; las pruebas aisladas colocan al jugador en puntos reproducibles para comparar estados. Dispositivos virtuales comprueban rutas del Input System; no prueban un mando físico.

Las fuentes se reproducen durante PlayMode. Además, un observador pasivo tomó 1.136 bloques de salida real de AudioListener, canal 0, durante 6,005 s del acontecimiento en el ambiente nocturno: pico máximo 0,1461 y RMS máximo 0,0649. Ninguna muestra observada alcanzó la saturación; esto no acredita toda la mezcla ni ambos canales. Son bloques cortos de niveles, no una grabación cronológica ni una escucha humana. Evidencia: `Logs/exploracion_audio_salida.csv` y `Logs/exploracion_audio_niveles.json`. La mezcla perceptiva, los auriculares y altavoces quedan pendientes. No se presentan comprobaciones de AudioSource como prueba de disfrute sonoro.

Las pruebas técnicas verifican funcionamiento, referencias y colisiones. La curiosidad, claridad sin guía, comodidad con las nuevas retenciones, carácter acogedor de la oscuridad y placer de observar deben evaluarse con jugadores. El muestreo del Editor no sustituye rendimiento GPU en el equipo objetivo.


### Medición de efectos en el Editor

En la escena corregida se muestrearon 120 fotogramas por estado, desde una vista fija de la cámara del jugador, con el grupo opcional activo y desactivado. Main Thread: 6,17 ms frente a 6,31 ms; draw calls: 1374,26 frente a 1335,50. Los contadores estaban disponibles. Es una muestra del Editor, con ruido y costes del runner; no permite atribuir con precisión toda la diferencia ni estimar FPS o coste GPU de una build. Que Main Thread resulte menor con efectos en este ensayo evidencia el ruido del muestreo, no una mejora de rendimiento. GC medio: 50,95 KB frente a 51,01 KB; no se interpreta esa variación como una mejora de asignaciones.

La textura realtime de la sonda ocupó 263.160 bytes según el Profiler. La prueba la desactivó/reactivó y pidió otra captura para comprobar su recurso: contó dos capturas durante ese ensayo. El flujo normal captura al acercarse y tras cambios de sellos, no de forma continua. El máximo de polvo es de 20 partículas. Evidencia: `Logs/exploracion_coste.txt`. Queda pendiente medir el pico de captura, GPU y calidad de sombras en el equipo objetivo.

### Referencias técnicas

- [ReflectionProbe, documentación Unity](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/reflectionprobe).
- [AudioListener.GetOutputData, documentación Unity](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiolistener/getoutputdata): permite muestrear niveles de la salida; no sirve para reconstrucción cronológica crítica ni sustituye la escucha de la mezcla.


### Capturas de la escena actual

- `Docs/Previews/Exploracion/01_Prototipo_Pieza.png`, `02_Prototipo_Recogida.png`, `03_Prototipo_Representacion.png`: antes/después y aparición del fragmento.
- `04_Conjunto_Durante.png`, `05_Conjunto_Pausa.png`, `06_Conjunto_Completo.png`: secuencia, contador y transformación persistente, en la misma carpeta.
- `07_Velas_Lejos.png`, `08_Velas_Cerca.png`, `09_Cuenco_Reflejo.png`, `10_Veta_Frontal.png`, `11_Veta_Oblicua.png`: sombras y respuestas visuales.
- `12_Motivo_Alineado.png`, `13_Motivo_Desplazado.png`, `14_Pieza_Contexto_Mando.png`: perspectiva y acción contextual.
- `15_Q_SOL.png`, `16_Q_LUNA.png`, `17_LUNA_Giro_Sin_Linterna.png`, `18_Cruce_Mural_Despejado.png`: cartel separado, giro sin revelar la solución y arte conservado.
- `Docs/Previews/Recorrido/23_Rotonda_Eclipse_Real.png`, `Coleccion_Completa_Recorrido.png` y `Coleccion_Vista_Exterior.png`: cámara real durante pruebas de recorrido; corona exterior a través del óculo, conjunto completo y vista lateral.


Corrección posterior de eclipse y audio: `CorreccionEclipseAudio.md` prevalece para estas partes. El cielo celeste ya no recibe niebla interior y el cerro que tapaba la abertura está fuera del límite caminable. El bucle global de ambiente se retiró. Vistas actuales: `Docs/Previews/Exploracion/19_Eclipse_Horizonte_Inicial.png` y `20_Eclipse_Plaza_0.png`/`1.png`/`2.png`; las capturas anteriores del óculo pertenecen al estado previo.

Revisión posterior: `LegibilidadYEpilogo.md` prevalece sobre el regreso con huella/guiño y sol en su posición inicial. El epílogo cierra todas las aberturas sin rastro, cambia el sol a atardecer en otra dirección y mantiene créditos. También se revisó la legibilidad de los detalles opcionales.
