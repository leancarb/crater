# Revisión del inicio, filtros y puerta SOL — 26/10/2026

Proyecto aplicado: C:/Users/carbo/Desktop/crater (Unity abierto, 6000.3.23f1). Se conservaron las diferencias anteriores respecto de crater-chatgpt. Sin commit, push ni check-in.

## Inicio y paisaje

- El jugador aparece en la meseta elevada mirando hacia el cráter. Un borde bajo de roca impide tomar la bajada de frente y deja libre la vista.
- El sendero sale a la derecha de ese encuadre: puntos locales (0,184), (-12,184), (-19,177), (-19,165), (-12,154), (0,150). Baja los cinco metros adicionales hasta el punto anterior; ya no es una rampa frontal de 34 m. Su longitud aproximada es 60 m.
- Las lomadas laterales del terreno en el tramo z80–150 sugieren el recorrido; el camino nuevo se integra en una malla continua. El cartel WASD acompaña la salida lateral.
- La elevación del cráter es circular y ocupa toda su superficie: cúpula suave de 0,8 m centrales hasta cero en el radio exterior de 19,5 m. El sector central pertenece a Tapa_Pozo y se retira con su animación; el anillo exterior permanece. No queda una cubierta flotante ni se vuelve a encoger el círculo.
- Siguen el farol central y el Umbral conservado sin su ancla ni compuerta.

## Controles acordados

| Acción | Mouse/teclado | Xbox | PlayStation |
|---|---|---|---|
| Encender/apagar farol | F | X | □ |
| SOL | Mantener izquierdo | Mantener RB | Mantener R1 |
| LUNA | Mantener derecho | Mantener LB | Mantener L1 |
| Luz blanca | Soltar el filtro | Soltar el filtro | Soltar el filtro |

No hay vencimiento de 3 s ni teclas 1/2 para equipar. Si se sostienen ambos botones, prevalece el último presionado; al soltarlo se recupera el otro. Los filtros se siguen recogiendo y desbloqueando. El farol debe estar encendido; los botones de filtro no lo vuelven a encender si se apagó. Soltar cancela también un cambio de filtro en curso. En pausa se ignora la entrada de juego; al reanudar se consulta el estado real del botón.

Se conserva la demora de cambio de color de 0,6 s, los alcances de los haces y las cargas/retenciones de anclas. El campo antiguo duracionFiltro se conserva serializado a cero por compatibilidad, pero ya no existe el reloj de agotamiento. HUD, pausa y pictogramas de filtros se actualizaron; se oculta la barra de tiempo antigua.

## Puerta SOL

Ancla_O_Puerta: de diseño (-31,0,19,8) a (-22,4,0,15,2), antes de CorrimientoAlaOeste y la ampliación. Mira hacia un punto de activación (-28,0,14), desde donde la puerta y el ancla entran en el mismo encuadre. Se mueve su pedestal y se conserva la condición permanente y el vínculo a Puerta_O_Ancla.

Franja ámbar sin collider debajo de la puerta: 3,875 m de largo, 5,5 cm de alto, 3,5 cm de espesor; material funcional Ambar existente, sin editarlo. Resplandor ascendente integrado al piso: spot de intensidad45, rango5m, ángulo100° e interior55°, sin sombras; apunta a la hoja para que su movimiento sea visible. La compuerta conserva su duración, desplazamiento y sonido.

## Archivos principales

- Assets/Editor/ConstructorCrater.InicioElevado.cs: meseta, bajada lateral, lomadas guía y cubierta circular.
- Assets/Editor/ConstructorCrater.PuertaSol.cs: encuadre del ancla, señal de la puerta y actualización de pictogramas.
- Assets/Editor/ConstructorCrater.Murales.cs: botón izquierdo/derecho representado en cada filtro.
- Assets/Scripts/Nucleo/EntradaCrater.cs: botones sostenidos y X/□ para el farol.
- Assets/Scripts/Linterna/LinternaController.cs: selección por estado de botones, sin vencimiento.
- Assets/Scripts/Interfaz/InterfazCrater.cs: leyendas actualizadas y eliminación del contador visual.
- Assets/Editor/ConstructorCrater.cs y ValidarProyectoCrater.cs: compatibilidad del campo de duración a cero.
- Tests: FiltrosSostenidosPlayTests (nuevo), InicioElevadoPlayTests, RitmoCraterTests, RotondaCartelesTests, CorreccionDocenteCompletaTests y FarolTituloTests adaptados a las decisiones actuales.
- En Unity se regeneraron la escena, oclusión, mallas de inicio/cubierta, dibujos de controles y el ajuste del prefab Jugador. Las mallas de arte usan el contenedor ya existente. No se reemplazó la escena diferente de la copia Git.

## Validación

Resultados y capturas: Logs/RevisionInicioFiltros20261026. Respaldo previo en su subcarpeta Antes. El menú localizado sigue siendo Crater > Actualizar inicio elevado y farol; la reconstrucción completa llama la misma implementación.

EditMode completo: 30/33. Fallos de expectativas anteriores del arte, fuera de esta tanda: CraterRevisionTests.LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar; PlacasInstruccionTests.LaNarrativaEstaPintadaSinLosaNiEsquinasEnL; PlacasInstruccionTests.TodasLasPlacasUsanPizarraYTrazoMateConCuatroEsquinasYSpotPropio. La escena restaurada no contiene los 24 conjuntos ni las 16 placas de esa otra versión. No se restauró ese arte para hacer pasar los tests.

PlayMode focalizado: 13/13 aprobados (FiltrosSostenidos3, InicioElevado2, Ritmo3, RotondaCarteles5). Incluye botones de mouse/mando, prioridad y liberación, pausa, movimiento, cruce de los cinco puentes, apertura del Cruce, bajada lateral, cartel y animación de la cubierta. No se ejecutó la suite completa de PlayMode.

Se abrieron hacia los costados los cerros A/B que tapaban la silueta circular del cráter y se extendieron los cierres laterales a la longitud del mirador nuevo. Son cambios de terreno generados por código, no modificaciones manuales de la escena.


Cierre: compilación sin errores y validador0. Los 51 PNG/WAV originales protegidos conservan sus hashes. La revisión visual final incluye vista inicial, camino lateral, lomada completa y ancla/puerta SOL iluminadas en el mismo encuadre. Unity queda fuera de Play. Capturas persistentes en Docs/Previews/RevisionInicioFiltros20261026.
