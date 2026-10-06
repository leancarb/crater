# CRÁTER — revisión contemplativa

> Registro de la revisión anterior. La ampliación posterior y los nuevos valores de puentes, eclipse y audio están en `ExploracionYConjunto.md` y `EstadoActualCrater.md`. Sus resultados no validan automáticamente esos cambios posteriores.

6/10/2026. Estado generado, con arte restaurado y decisiones del usuario preservadas. Documentación principal: `EstadoActualCrater.md`, `../GDD.md` y `../README.md`.

## Referencias y decisiones propias

- [Spatial Communication in Level Design, Peter Field](https://www.youtube.com/watch?v=AKeUZVikPV8): vistas útiles al llegar y doblar, destinos reconocibles, contraste contenido y objetivos que reaparecen. Se usa la transcripción aportada en el proyecto; esta revisión no afirma haber reproducido el video.
- [Il Filo Conduttore y Neighbor — ficha oficial de Triennale](https://triennale.org/en/events/game-collection-1): se toma el gesto sencillo como motivo para observar una respuesta cuidada, y la intimidad de un espacio con detalles de presencia humana. Son criterios de diseño propios, no reproducción de sus escenas.

Aplicación: ancla que empieza a resonar con el contacto del haz; tres sectores opcionales para explorar con un mismo gesto; asiento y cuenco dentro de una recuperación; reparación abandonada en el desvío lunar; marcas de estado discretas en la puerta de los sellos. Los objetos sugieren descanso, trabajo y cuidado. No se inventan leyendas, ritos ni símbolos culturales supuestamente auténticos.

## Criterio y ritmo

Primero se trabajaron el ancla SOL inicial y la gruta O1. La respuesta pasa de reposo a carga luminosa y resonante, activación y retención con pérdida gradual. La vibración afecta sólo a los aros visuales, nunca a la cámara ni a las colisiones. El tono no se repite al mantener el haz, y la resonancia de carga tiene salida suave.

El espacio amplio de la rotonda y el Cruce se conserva. La pista del desvío LUNA se coloca en su destino, no en una caminata agregada. El asiento O1, banco de la rotonda y apoyo de la isla quedan fuera del paso. El banco se coloca en el sector sur, desde el que se observa el eclipse real; el detalle lunar se coloca en el tabique del fondo, separado del ornamento lateral restaurado. No se compactan las salas mediante escala del jugador ni se aumenta su velocidad.

| Paso | Distancia física aproximada | Margen mecánico |
|---|---:|---|
| Puente inicial SOL, desde el apoyo hasta la otra orilla | 6 m / 2,3 s a velocidad 2,6 | Retención 8 s después del haz |
| Puente colgante SOL, desde el apoyo hasta la otra orilla | 5,9 m / 2,3 s | Retención 9 s |
| Rejas temporales comunes | Según posición y aproximación | Retención 6,25 s |
| Cruce | Dos tramos independientes | Isla segura permite detenerse entre ellos; retenciones particulares conservadas |

Son medidas de geometría y tiempos, no evidencia de comodidad. La lectura de vistas y la elección a ciegas deben observarse en jugadores que no conozcan el mapa.

## Implementación

- `ConstructorCrater.Espacio.cs`: ampliación de arquitectura con recolocación y proporciones protegidas de modelos, anclas, recogibles, rejas, murales y objetos pequeños; rampas reconstruidas desde sus extremos físicos sin sesgo de colisión; planos reflectantes con dimensiones físicas.
- `ConstructorCrater.Alas.cs`, `.Detalles.cs`, `.Murales.cs`, `.Tallados.cs`: LUNA a ciegas con reparación, rampas con pretiles, rejas finales permanentes; puerta con dos marcas de sellos; anclas colgantes mejor encuadradas; detalles de uso cotidiano y sector resonante O1; retirada de paneles explicativos del Cruce.
- `Ancla.cs`, `TalladoResonante.cs`, `GeneradorAudioCrater.cs`: respuesta gradual, resonancia de carga, vibración visual y gesto opcional sin progreso. Nuevas fuentes locales de bajo volumen.
- `ConstructorCrater.Capilla.cs`, `.Nivel.cs`, `CieloEclipse.cs`, `PrologoCapilla.cs`, `EclipseFinalController.cs`: estación secular, piso posterior y límites seguros, apertura real al eclipse, eliminación del destello/sonido del cráter y créditos al cruzar la huella.
- `SuperficieDePasos.cs`, `JugadorFPS.cs`: pasos por cuatro superficies y cadencia por velocidad real.
- `AdaptacionOscuridad.cs`, `ConstructorCrater.Nivel.cs`: primera devolución de exposición/viento rápida, adaptación gradual, reflejos de piso y techo.

La apertura del cielo requiere un paso elongado hacia el norte por la baja elevación solar de 14°. Se amplió el vacío del techo y el recorte del llano exterior fuera de la zona caminable. El límite exterior se mantiene sobre el suelo del valle: extenderlo bajo tierra bloqueaba el haz y la vista del eclipse, error detectado y corregido durante las pruebas.

## Validación de esta revisión

Reconstrucción final completada con `ConstructorCrater.ReconstruirDesdeLineaDeComandos`, `VistaPreviaCrater.Renderizar` y `RenderizarCambios`. La escena guardada quedó abierta en el Editor, fuera de Play. No se creó un EXE.

| Comprobación final | Resultado | Evidencia |
|---|---|---|
| Compilación y consola actual | Sin errores ni advertencias; compilación terminada | [Consola](../Logs/revision_contemplativa_consola_final.json) |
| Referencias, scripts faltantes y alcance del haz | 0 problemas; escena guardada | [Validador](../Logs/revision_contemplativa_validacion_final.json) |
| PlayMode | 18/18, 449,10 s; sin pruebas omitidas | [Resultados completos](../Logs/revision_contemplativa_playmode_final.json) |
| EditMode | 19/19, 0,46 s; sin pruebas omitidas | [Resultados completos](../Logs/revision_contemplativa_editmode_final.json) |
| Audio anterior | 42 WAV comparados sin cambios; 34 huellas anteriores intactas | [Comparación SHA-256](../Logs/audio_revision_preservacion_final.json) |

PlayMode recorre SOL→LUNA y LUNA→SOL desde el exterior hasta el título visible de los créditos (185,36 y 184,74 s respectivamente). Usa la escena, el CharacterController, las colisiones y el haz reales, con tiempo acelerado ×2. Ambos recorridos incluyen el desvío lunar y regreso, descenso a la galería, escotilla, cinemáticas de los sellos, isla segura con la linterna apagada, Cresta, apertura del techo y epílogo. Las cuatro recuperaciones tienen una prueba adicional de caída, apertura de retorno, subida y conservación del progreso.

También se verificaron carga/activación/retención/descarga del ancla, respuesta opcional sin cambio de etapa, reproducción y salida de las resonancias, adaptación y pérdida al encender, seis superficies reflectantes y recortes, eclipse real visible por la apertura, proporciones de las anclas, pasos en cuatro materiales y cadencia al correr, cierre de los laterales de la escalera y suelo posterior de la estación. Las rampas reconstruidas permitieron completar los dos órdenes sin los escalones invisibles que producía su escala anterior.

Teclado, mouse y mando se comprobaron a través de dispositivos virtuales del Input System: movimiento, mirada, linterna/filtros, pausa/reanudación, salto de ambas cinemáticas y control recuperado. Esto verifica las rutas de entrada, no sustituye probar un mando físico ni juzgar sensibilidad o comodidad.

### Capturas actuales inspeccionadas

Son capturas de la cámara del jugador en la escena real, a 1280×720; las pruebas de interacción colocan al jugador en posiciones transitables para examinar estados reproducibles. Se revisaron, entre otras:

- Ancla: [reposo](Previews/Recorrido/13_Ancla_Reposo.png), [carga](Previews/Recorrido/14_Ancla_Carga.png), [activación](Previews/Recorrido/15_Ancla_Activa.png), [retención](Previews/Recorrido/16_Ancla_Retencion.png) y [descarga](Previews/Recorrido/17_Ancla_Descargada.png).
- Gruta: [conjunto y cuenco](Previews/Recorrido/18b_Gruta_Conjunto.png), [tallados en reposo](Previews/Recorrido/18_Gruta_Reposo.png) y [respuesta al haz](Previews/Recorrido/19_Gruta_Resonancia.png).
- LUNA: [detalle del desvío](Previews/Recorrido/05b_Luna_Desvio.png), separado del ornamento restaurado.
- Cresta: [linterna encendida](Previews/Recorrido/20_Cresta_Con_Linterna.png), [primera respuesta al apagar](Previews/Recorrido/21_Cresta_Primera_Respuesta.png) y [adaptación parcial](Previews/Recorrido/22_Cresta_Adaptacion_Parcial.png).
- Rotonda y reflejos: [eclipse exterior sobre el obelisco](Previews/Recorrido/23_Rotonda_Eclipse_Real.png), [piso](Previews/Recorrido/24_Reflejo_Piso.png) y [techo](Previews/Recorrido/25_Reflejo_Techo.png).
- Créditos visibles: [SOL→LUNA](Previews/Recorrido/12_Final_SOL_Luna.png) y [LUNA→SOL](Previews/Recorrido/12_Final_Luna_SOL.png).

Las vistas fijas complementarias incluyen la aproximación diurna a la estación. [VistasGeneradasVigentes](Previews/VistasGeneradasVigentes.md) identifica los 18 archivos del generador actual; otros PNG antiguos permanecen como registro histórico.

### Audio y límites de la validación

Las fuentes se reprodujeron en Unity durante PlayMode y se comprobaron estados de reproducción, ganancias y transiciones. Los nuevos WAV procedurales se analizaron en pico/RMS y no tienen recorte digital; los archivos personalizados y las entradas previas de huellas no se sobrescribieron. Las herramientas usadas no permitieron escuchar o capturar la mezcla completa: queda pendiente una evaluación perceptiva real con auriculares y altavoces.

Durante renders se registraron mensajes informativos de reducción de resolución del atlas de sombras adicionales. No son errores de compilación ni advertencias de consola, pero la calidad de sombras y el rendimiento deben medirse en el equipo objetivo. No se realizó una prueba de rendimiento ni una sesión humana completa. Los resultados automáticos y las capturas prueban funcionamiento y estados; no prueban comprensión, comodidad o disfrute.

## Comprobación con jugadores pendiente

- Descubrir sin guía las anclas colgantes, el sello tras el tabique y el uso de LUNA sobre el piso.
- Entender el regreso del desvío y el vínculo entre sellos, obelisco y puerta.
- Percibir y disfrutar la respuesta del ancla/tallado y elegir detenerse en gruta e isla.
- Juzgar la oscuridad acogedora y la transición al final en diferentes pantallas.
- Escuchar la mezcla real con auriculares y altavoces, y medir rendimiento con las sombras ampliadas. Las comprobaciones de AudioSource y WAV no sustituyen esa evaluación perceptiva.
