# Respuestas del playtest y luminosidad para el aula

Fecha de revisión: 18/10/2026. Fuente: `Cráter Testing.csv.zip`, cinco respuestas del 16–17/10. Muestra pequeña y cualitativa; no permite generalizar a todos los jugadores.

## Qué muestran las respuestas

Todos jugaron en Windows con teclado y mouse; tres con parlantes y dos con auriculares.

| Señal | Respuestas |
|---|---:|
| Desorientación en la primera puerta con linterna | 3 de 5 |
| Desorientación en la sala circular/obelisco | 2 de 5 |
| Confusión al combinar filtros | 2 de 5 |
| Confusión en la sala final a oscuras | 2 de 5 |
| No sabían que existían pausa y ayudas | 2 de 5 |
| Se detuvieron tras recoger la linterna, sin filtros | 2 de 5 |

Dos marcaron que todo estaba muy oscuro y otra persona describió la combinación de oscuridad excesiva con luz blanca demasiado intensa. Dos describieron luz quemada; uno señaló que así no podía leer los murales. Otra persona reportó mareo, aunque consideraba adecuada la iluminación. Son problemas diferentes: subir exposición sin equilibrar la linterna puede empeorar las altas luces.

Una persona usó las ayudas; otra dijo que no consiguió activarlas y una señaló que la pausa no se entendía. Cuatro describieron la velocidad como normal y una pidió correr con teclado. La solicitud de salto es una expectativa de una persona, no evidencia suficiente para cambiar el diseño.

Se valoraron la premisa de los filtros, los puentes y la presentación. Las valoraciones de experiencia fueron 3, 4, 2, 3 y 3 (media 3); el CSV no contiene los extremos de la escala, por lo que no se interpreta como nota sobre cinco.

## Cambios realizados ahora

- Pausa (`Esc` / `Start`): slider de **Luminosidad**, manipulable con mouse, flechas izquierda/derecha y navegación del joystick. El porcentaje expresa la posición del control, no un porcentaje físico de luz.
- Inicio en **50 %**, equivalente a +0,75 EV. Rango de exposición −1 a +2,5 EV. Se ve aplicado durante la pausa y se guarda al seguir, reiniciar o salir. Los ajustes antiguos distintos de cero se conservan; el antiguo cero migra al valor inicial más claro.
- Relleno ambiental interior más alto para leer piso y paredes: cielo RGB (1,30; 1,35; 1,45), horizonte (1,05; 1,05; 1,10), suelo (0,60; 0,58; 0,58). Valores HDR de Unity.
- Luz blanca de la linterna: intensidad 2000 → 650, ángulo 36° → 52°, cono interior al 12 % del exterior. Se abre y suaviza el haz para reducir el contraste abrupto y la sobreexposición cercana.
- Pausa con título, controles más legibles y explicación del slider; conserva la fuente habitual del HUD.
- La adaptación final suma el ajuste del jugador incluso al reiniciarse; conserva sus tiempos y condiciones de apertura. Los filtros SOL/LUNA conservan su intensidad, alcance funcional y duración.
- Aplicado en scripts y en escena/prefab mediante Unity. No se reconstruyó el nivel completo ni se generaron WAV.

No se implementaron tutoriales, salto, sprint por teclado ni nuevas pistas de recorrido. Son decisiones pendientes de diseño. Prioridad sugerida para la próxima iteración: primera puerta y descubrimiento de ayudas; después orientación de la rotonda y claridad del final. Revisar mareo por separado si vuelve a aparecer.

## Para probar en clase

Abrir pausa y ajustar Luminosidad según la pantalla y la luz del aula. Volver al juego para evaluar la imagen sin el panel oscurecido. Probar primero al 50 %, subir si hace falta y revisar un mural cercano con luz blanca además de una zona sin linterna. Los ajustes quedan guardados en esa computadora.

Los ejecutables y ZIP anteriores no cambian al editar el proyecto: hay que volver a usar **Construir demo** antes de llevar la nueva versión al aula.

Respaldo previo: `Backups/LuminosidadAntesAula20261018/`. Capturas de comparación y resultados de pruebas: `Logs/LuminosidadAula20261018/`.

## Validación realizada

- Compilación de Unity sin errores; validador del proyecto: 0 problemas.
- PlayMode: 11/11 pruebas seleccionadas pasan (3 de luminosidad, 3 de ritmo/filtros, 1 de adaptación y 4 de tipografía). Ver JSON en Logs/LuminosidadAula20261018. No se ejecutó todo el recorrido automático completo.
- EditMode completo: 30/31 pasan. Fallo previo en CraterRevisionTests.LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar, línea 101: busca Luz_Pasaje, ausente tanto en el respaldo anterior como en la escena actual. No se alteraron los murales para tapar ese fallo.
- Capturas de Umbral, Rotonda, Cresta y pausa revisadas. Ver *_Final.png. No equivalen a una prueba física con el proyector o monitor del aula.
- La escena sólo cambia sus tres colores ambientales; el prefab Jugador sólo cambia parámetros de luz blanca. Geometría y colliders sin cambios. Los 43 archivos protegidos de audio conservan sus SHA-256 del registro existente.
- No se generaron ejecutables ni ZIP de distribución y no se hizo check-in.

## Archivos principales

- Assets/Scripts/Nucleo/OpcionesCrater.cs:25 y :87 — valor inicial, límites, migración y almacenamiento del brillo.
- Assets/Scripts/Interfaz/InterfazCrater.cs:296 y :317 — slider, selección al pausar, entrada de mouse/joystick y etiqueta.
- Assets/Scripts/Interfaz/PausaCrater.cs:84 — persistencia al salir de la pausa y acciones de reinicio/salida.
- Assets/Scripts/Flujo/AdaptacionOscuridad.cs:126 — conservar el ajuste al restablecer la exposición.
- Assets/Editor/ConstructorCrater.Nivel.cs:152 — relleno interior regenerable.
- Assets/Editor/ConstructorCrater.cs:693 — luz blanca del prefab regenerable.
- Assets/Scripts/Linterna/LinternaController.cs:44 y :206 — valores iniciales y suavidad exclusiva de luz blanca.
- Assets/Tests/PlayMode/LuminosidadCraterTests.cs — migración, límites, guardado, raycast/arrastre real, exposición con tiempo detenido y navegación.
- Assets/Tests/PlayMode/Crater.Tests.PlayMode.asmdef — referencias de URP necesarias para comprobar exposición.
