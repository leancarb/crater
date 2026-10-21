# Corrección docente — primera implementación (19/10/2026)

Fuente: transcripción de la corrección del 19/10 y devolución del formulario. Proyecto activo: `C:/Users/carbo/Desktop/crater`.

## Esta tanda

- Linterna, filtros y piedras se recogen mirando de cerca y confirmando con Espacio / botón sur del mando (A Xbox, × PlayStation). Pasar por encima de la linterna o de un filtro ya no lo incorpora sin advertirlo. No se cambia el botón SOL (oeste) para evitar conflictos.
- ColeccionPiedras arbitra las interacciones principales y opcionales; se conserva su comprobación de distancia, mirada, obstáculos, pausa y secuencias. La recogida de un objeto tiene prioridad sobre una piedra próxima.
- La indicación contextual usa el último dispositivo utilizado, no sólo la presencia de un mando conectado. Tras recoger el farol indica F / RB (R1) para encenderlo, hasta el primer encendido. HUD de filtros también cambia entre 1/2/3 y los botones del mando.
- Las anclas temporales tienen un arco junto al receptor. Permanece completo al iluminar y se vacía con la retención real; desaparece al desactivarse. Las anclas permanentes no muestran un temporizador engañoso.
- Durante el último 35 % del tiempo de un puente, un pulso sonoro breve acompaña el parpadeo existente y se acelera al agotarse. Es espacial, se detiene al deshabilitar el puente y respeta la pausa. El clip se sintetiza en memoria; no genera ni modifica WAV.
- Se conservan filtros de dos segundos, carga, retención, solidez, recorridos, luz ambiental, arte y cámara libre. Los componentes visuales/sonoros nacen desde los scripts al entrar en Play; reconstruir no pierde los cambios.

## Archivos

- Assets/Scripts/Nucleo/EntradaCrater.cs: acción compartida y dispositivo utilizado.
- Assets/Scripts/Mecanicas/Recogible.cs: elimina la recogida automática por trigger.
- Assets/Scripts/Mecanicas/ColeccionPiedras.cs: selección y recogida explícita; primera indicación de encendido.
- Assets/Scripts/Interfaz/InterfazCrater.cs: etiquetas de filtros según dispositivo.
- Assets/Scripts/Mecanicas/Ancla.cs y CuentaAtrasAncla.cs: arco de retención.
- Assets/Scripts/Mecanicas/PuenteLuz.cs: aviso sonoro temporal.
- Assets/Tests/PlayMode/CorreccionProfesorTests.cs: recogida real por teclado/mando, pausa y temporizadores.
- Assets/Tests/PlayMode/RecorridoCraterTests.cs: el recorrido existente confirma las recogidas en lugar de asumir adquisición al caminar.

Respaldo de los archivos existentes: Backups/CorreccionProfesorInicio20261019. Sin check-in, exportación ni modificación de archivos de audio.

## Siguientes tandas de la corrección

1. Enseñanza ambiental antes del primer obstáculo, con lectura de dos anclas y resultado visible desde un lugar seguro.
2. Primer uso combinado de SOL y LUNA en el Cruce: composición, orientación de piezas y confirmación del resultado.
3. Diferenciar murales narrativos de instrucciones y hacer legibles los elementos interactivos del final.
4. Encuadre ambiental del eclipse y presentación de coleccionables.

Estas tandas aún no se implementaron. Aumentar los dos segundos, cambiar el orden de las alas y exigir sprint son propuestas para evaluar; no se alteraron las reglas sin comprobar primero su comunicación.

## Validación de esta tanda

- Unity compila sin errores. Pruebas nuevas PlayMode: 3/3 aprobadas.
- Recorrido completo SOL→LUNA→Cruce→Cresta→epílogo→créditos: aprobado, 142,92 s. Se actualizaron las recogidas explícitas del test y su expectativa obsoleta de título CRÁTER a CRATER, conforme a la decisión anterior de quitar la tilde.
- EditMode completo: 30/31; se mantiene el fallo previo LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar:101 por Luz_Pasaje ausente. No se ocultó ni se deshabilitó ese test.
- Capturas revisadas de recogida, indicación F para encender y arco a media retención. Evidencia en Logs/CorreccionProfesor20261019. La prueba comprueba reproducción del aviso; queda pendiente escuchar la mezcla en el equipo de clase.
- 43 archivos de audio protegidos sin cambios SHA-256. Sin build ni check-in.
Actualización posterior: las tandas de recorrido, combinación, final, marcos y exterior ya están implementadas. Ver Docs/CorreccionDocenteCompleta20261019.md, que también amplía filtros a3s y añade luces del techo de columnas.
