# Instrucción y narrativa — 21/10/2026

Base de trabajo: main actualizado, commit 8425bdc. Proyecto conectado: C:/Users/carbo/Desktop/crater. Los cambios se implementan en constructores y se generan mediante Unity; no se editan YAML de escena ni materiales a mano.

## Instrucción

Un solo constructor `PlacaInstruccion` arma el fondo y el soporte, las cuatro esquinas en L, los pictogramas extruidos y su spot. Conserva los nombres de las instrucciones para no romper búsquedas existentes.

- Fondo URP/Lit Placa_Pizarra, #2E3540, smoothness0.1, metallic0, sin texturas ni emisión.
- Trazo_Instruccion, #F4F1EA, smoothness0.05, metallic0, sin emisión.
- Losa mínima1.2×0.8m, espesor0.06m; centro1.6m sobre el piso real. Exterior: altura calculada sobre el relieve local.
- Cuatro esquinas de dos brazos:0.15m por brazo, trazo0.03m, relieve0.01m. No se añade marco perimetral continuo.
- Pictogramas: relieve0.01m, trazos mínimos0.04m, incluyendo dibujos escalados. Se conserva el contenido de cada tutorial. Se eligió la variante monocroma; el acento de filtro era opcional.
- Spot propio de color #F4F1EA, intensidad14, ángulo35°, rango3m, sin sombras; situado arriba y delante.
- Seis placas de apagado de1.6×0.8m: personaje con farol prendido → power → ojo cerrado y estrellas. El power se comparte con el panel de la linterna.
- Las lecciones del primer puente y del Cruce se desplazan al costado para que, al bajar su altura, no intercepten los haces hacia las anclas.

## Narrativa

24 conjuntos quedan como pintura mate directa sobre la pared: sin losa, marco añadido ni esquinas en L. Conservan los dibujos procedurales y los colores narrativos de Sol y Luna. Los ocho PNG del pasaje conservan sus archivos fuente; el constructor extrae su tinta en geometría y descarta fondo y marco horneados, con PinturaHueso #948C7A sin emisión. No se modificaron los PNG originales.

Las indicaciones de apagado salen del grupo de arte que el final podía hacer brillar. Se conserva la lógica de apertura, movimiento y progresión: no se modificó ningún script de gameplay.

## Placas migradas

Coordenadas mundiales (X,Y,Z), en metros:

| Placa | Grupo de ubicación | Centro mundial |
|---|---|---|
| `Apagar_Der_84` | MotivosLatentes_Cresta | (14.82, 1.60, 110.13) |
| `Apagar_Der_91` | MotivosLatentes_Cresta | (14.82, 1.60, 118.88) |
| `Apagar_Fondo` | MotivosLatentes_Cresta | (0.00, 1.60, 129.32) |
| `Apagar_Izq_84` | MotivosLatentes_Cresta | (-14.82, 1.60, 110.13) |
| `Apagar_Izq_91` | MotivosLatentes_Cresta | (-14.82, 1.60, 118.88) |
| `Control_FiltroLuna` | Murales_Docentes | (35.20, 1.60, 14.31) |
| `Control_FiltroSol` | Murales_Docentes | (-34.57, 1.60, 14.31) |
| `Control_Linterna` | Murales_Docentes | (7.31, 1.60, -7.38) |
| `Control_Mover` | Cartel_Controles_Inicio | (2.74, 17.60, -147.64) |
| `Ensenanza_Cresta_Espera` | Correccion_Docente | (1.86, 1.60, 98.21) |
| `Ensenanza_Cresta_Reflejo` | Correccion_Docente | (-1.86, 1.60, 96.33) |
| `Ensenanza_Cruce_Combinacion` | Correccion_Docente | (-7.23, 1.60, 46.38) |
| `Ensenanza_Dos_Anclas` | Correccion_Docente | (-36.35, 1.60, 16.75) |
| `Mural_Ancla_Puerta` | Murales_Docentes | (7.31, 1.60, -16.13) |
| `Mural_Anclas_Del_Techo` | Murales_Docentes | (-25.00, 1.60, 46.19) |

## Archivos de código

- Assets/Editor/ConstructorCrater.Placas.cs: constructor único, materiales, relieve, secuencia de apagado, pintura PNG, migración localizada y limpieza de mallas generadas sin referencias.
- Assets/Editor/ConstructorCrater.Murales.cs: enrutar tutoriales y narrativa a sus constructores respectivos; sustituir los glifos flotantes; mantener grosor al escalar; power compartido.
- Assets/Editor/ConstructorCrater.CorreccionDocente.cs: clasificación de instrucciones, ubicación lateral de lecciones y conservación del anclaje de pared.
- Assets/Editor/ConstructorCrater.Espacio.cs: conservar dimensiones de placas y pinturas durante la ampliación del nivel.
- Assets/Editor/ConstructorCrater.Nivel.cs: integrar la migración en reconstrucción completa y quitar emisión programada de los antiguos glifos.
- Assets/Editor/ConstructorCrater.RotondaRevision.cs: retirar el fondo especial del WASD y reconocer los nuevos soportes al actualizar.
- Assets/Editor/ConstructorCrater.cs: color exacto de PinturaHueso en el generador de materiales.
- Assets/Editor/ValidarProyectoCrater.cs: reglas de placa, narrativa y pasaje.
- Assets/Tests/EditMode/PlacasInstruccionTests.cs: materiales, medidas, esquinas, relieve, orientación de caras, spot y narrativa.
- Assets/Tests/EditMode/CraterRevisionTests.cs: sustituir la expectativa antigua de quad con losa por pintura narrativa directa.
- Assets/Tests/PlayMode/PlacasLecturaPlayTests.cs: seis secuencias de apagado y ausencia de emisión al activarse el final.
- Assets/Tests/PlayMode/ExploracionCraterTests.cs: validar tinta narrativa, referencia del dibujo original y dimensiones visibles sin exigir el antiguo rectángulo de fondo.
- Assets/Tests/PlayMode/CorreccionDocenteCompletaTests.cs: lectura a1.6m y líneas de vista hacia las anclas.

Generados por Unity: Assets/Materials/Placa_Pizarra.mat y Trazo_Instruccion.mat, actualización de PinturaHueso.mat, contenedor de mallas, escena CraterVerticalSlice y oclusión. Las referencias existentes se mantienen cuando corresponde.

## Validación

- EditMode completo:33/33 aprobados.
- PlayMode completo: 60/66 aprobados en la primera ejecución. Incluye los cuatro recorridos completos, aprobados. Se actualizó la expectativa antigua de textura/fondo del test de murales; retest final: 1/1 aprobado.
- Los cinco fallos restantes están fuera de la migración de placas:
  - `CorreccionEclipseAudioTests.ElSolMantieneElHorizonteExteriorYSeVeEnLaPlaza`: espera 14°; el sol actual está a 12°.
  - `LegibilidadEpilogoTests.DetallesTienenRespuestaYSeVenDesdeSueloSeguro`: sistema de polvo sin partículas en la comprobación.
  - `RecorridoCraterTests.ElExteriorTrasLaEstacionTieneSueloContinuo`: la ruta de prueba choca con Roca_Cierre_Valle.
  - `RecorridoCraterTests.PisoYTechoReflejanYElOculoMuestraElCieloReal`: el raycast hacia el eclipse intercepta geometría.
  - `RecorridoCraterTests.TecladoMouseMandoYPausaConservanControlDuranteLosSellos`: pulsa 2 esperando Sol; los controles actuales usan Sol=1 y Luna=2.
- Retests de la versión final: placas 1/1, murales 1/1 y corrección docente (incluida línea de vista a anclas) 4/4; todos aprobados. Balance por test tras retests: 61/66, con los cinco fallos enumerados.
- Validador completo: cero errores.
- No se afirma que la suite completa esté verde. No se cambiaron esos sistemas para ocultar los fallos.
- Capturas revisadas de WASD, linterna, filtro Sol, combinación del Cruce, apagado y narrativa.
- Respaldo previo: Backups/PlacasInstruccion20261021. Evidencia y listado TSV: Logs/PlacasInstruccion.
- Sin check-in Unity Version Control, sin commit/push adicional, sin builds.

Los cuatro murales de las grutas (valle y eclipse) se incluyen como narrativa, sin soporte ni L. Total:15 placas y24 conjuntos narrativos.
