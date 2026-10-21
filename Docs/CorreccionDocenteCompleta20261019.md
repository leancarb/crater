# Corrección docente: recorrido, combinación y luz de columnas — 19/10/2026

Proyecto activo: C:/Users/carbo/Desktop/crater. Respaldo antes de esta tanda: Backups/CorreccionDocenteCompleta20261019 (fuentes y escena).

## Cambios

1. Primer puente SOL: panel de tres pasos antes del vacío (iluminar cada ancla por separado y después cruzar), reloj dibujado, apoyos colgados del techo y marcas discretas de lectura en suelo seguro. Las dos anclas miran hacia el lugar de lectura. La enseñanza no activa el puente ni cambia sus requisitos.
2. Cruce: panel que muestra LUNA → reja → dos anclas SOL → puente y tiempo. Se presenta antes del primer puente de combinación. Las anclas iniciales y las de salida se orientan hacia sus lugares de observación; se conserva la última reja ancha y las posiciones de las anclas.
3. Cresta/final: el díptico de reflejo y espera pasa de las paredes lejanas al corredor de entrada, separado de las paredes para verse completo. Unas marcas mates del suelo conducen al punto de observación. Se mantienen adaptación, reflejos y apertura; no aparece un botón nuevo ni se resuelve automáticamente.
4. Exterior: siete pares de piedras sobre el terreno delimitan el recorrido. Un pórtico con un hueco circular encuadra el sol desde una marca voluntaria en el suelo. No se mueve la cámara ni se aplica el exterior Silo archivado.
5. Lenguaje gráfico: las instrucciones tienen marco completo y dobles esquinas. Los murales narrativos procedurales usan un friso inferior sin el mismo marco. Los ocho PNG de lore se conservan. Los controles de pared de linterna/SOL/LUNA usan pictogramas, sin teclas exclusivas de teclado; los botones reales siguen en la indicación contextual/HUD.
6. Filtros SOL y LUNA: 3 segundos efectivos en lugar de 2. Se actualizan runtime, constructor, prefab Jugador, instancia, pausa, validador y pruebas. Se conserva el retorno automático a luz normal y el comportamiento durante pausa.
7. Seis columnas: cada una suma aro en el techo y foco descendente, sincronizados con su aro y luz ascendente del piso mediante el mismo TestigoDeSello. Foco superior a y=6,88, intensidad máxima45; piso conserva65. Se mantiene la secuencia de notas y encendidos al regresar con cada sello.

## Persistencia

Assets/Editor/ConstructorCrater.CorreccionDocente.cs reúne la construcción localizada. El constructor completo llama AplicarCorreccionDocente después de ampliar/facetar el nivel; MapaYObelisco también incorpora el foco superior. Crater/Actualizar corrección docente aplica la revisión sin reconstruir todo. La actualización reemplaza sólo su grupo de piezas y las losas afectadas; se recalcula oclusión y se guarda con Unity. Sin edición manual de YAML, WAV, scripts de audio grabado, build ni check-in.

## Archivos principales

- Assets/Editor/ConstructorCrater.CorreccionDocente.cs: lecciones, señales de piso, pórtico exterior, orientación de anclas, luces superiores y actualización localizada.
- Assets/Editor/ConstructorCrater.Murales.cs: distinción narrativa/instrucción y pictogramas de controles.
- Assets/Editor/ConstructorCrater.Nivel.cs y ConstructorCrater.Alas.cs: integración con regeneración.
- Assets/Scripts/Mecanicas/TestigoDeSello.cs: segundo foco sincronizado con el mismo estado.
- Assets/Scripts/Linterna/LinternaController.cs, Assets/Editor/ConstructorCrater.cs, Assets/Scripts/Interfaz/InterfazCrater.cs y Assets/Editor/ValidarProyectoCrater.cs: duración3s y etiquetas.
- Assets/Tests/PlayMode/CorreccionDocenteCompletaTests.cs: luz superior/piso, lecciones visibles sin bloquear haces y encuadre del sol.
- Assets/Tests/PlayMode/RitmoCraterTests.cs, RotondaCartelesTests.cs y Assets/Tests/EditMode/FarolTituloTests.cs: expectativas nuevas.

Las capturas de diagnóstico y resultados JSON se guardan en Logs/CorreccionDocenteCompleta20261019. La legibilidad del arte se revisa en capturas; la comprensión por un jugador nuevo requiere un playtest sin explicación oral.

## Validación final

- Compilación de Unity sin errores; validador:0 problemas.
- 12/12 PlayMode seleccionados aprobados: 3 de lecciones/techo/exterior, 3 de ritmo/filtros/controles, 5 de rotonda/carteles/escalera/apertura y 1 recorrido completo hasta créditos (143,18 s).
- EditMode completo:30/31. Se mantiene el fallo previo LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar:101, por Luz_Pasaje ausente; sin fallos nuevos.
- Capturas finales revisadas: pórtico alineado desde el punto voluntario, primer puente y Cruce con el dibujo completo en el encuadre, díptico final completo y luces de piso/techo. Los primeros encuadres demasiado altos fueron corregidos y las capturas sustituidas.
- Actualización localizada ejecutada dos veces sin duplicar paneles ni aros de techo. Los requisitos y el trayecto de los puzzles se verificaron con el recorrido completo.
- Audio protegido43 archivos sin cambios; no build ni check-in.

Referencias de código: ConstructorCrater.CorreccionDocente.cs:37 (aplicación), :15 (luz superior), :161 (exterior); ConstructorCrater.Murales.cs:180 (lenguaje de marcos); ConstructorCrater.Nivel.cs:98 (regeneración); ConstructorCrater.Alas.cs:255 (rotonda); TestigoDeSello.cs:26 y :60 (foco sincronizado); LinternaController.cs:51 y ConstructorCrater.cs:686 (duración3s).


## Ajuste posterior: claridad, techo y capturas (19/10/2026)

Este ajuste reemplaza las marcas de observación y el pórtico descritos arriba.
- Retiradas las pisadas Lectura_Primer_Puente, Lectura_Cruce, Lectura_Cresta y las diez franjas Guia_Piso_Cresta. Permanecen los dibujos de enseñanza y las luces funcionales del recorrido.
- Aros superiores a y=6,835 m, carcasa opaca entre 6,845 y 6,955 m, bajo el techo de 7 m; focos a 6,72 m. Siguen sincronizados con el piso.
- Pórtico y encuadre solar retirados; guía exterior con 14 rocas irregulares bajas y dos afloramientos laterales. Paso central libre, sin nuevo marco ni señal pintada.
- Pausa por pérdida de foco desactivada en fuente, constructor y escena; ejecución en segundo plano activada. Esc/Start conservan la pausa manual.
- Cambios: ConstructorCrater.CorreccionDocente.cs:15 y :147; ConstructorCrater.Nivel.cs:603; PausaCrater.cs:21 y :30; CorreccionDocenteCompletaTests.cs:18 y :82.
- PlayMode seleccionados: 9/9 (4 ajustes + 5 regresión rotonda). Compilación sin errores. Capturas revisadas de exterior, interior y vista superior. Audio protegido: 43 archivos sin cambios.
- Copia previa en Backups/AjustesCapturasTechoExterior20261019. Evidencia en Logs/AjustesCapturasTechoExterior20261019. Sin build ni check-in.
