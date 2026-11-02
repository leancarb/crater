# Camino, farol y murales — 26/10/2026

Proyecto aplicado: `C:/Users/carbo/Desktop/crater`. Cambios desde constructores y scripts; escena y materiales regenerados con la actualización localizada, sin reconstruir todo el proyecto.

## Cambios

- `ConstructorCrater.InicioElevado.cs`: tierra del camino con franja clara de 3,6 m; bordes laterales más próximos (5,5 → 4,6 m desde el eje), rocas más bajas y bancos del tramo inferior de 0,9 → 1,25 m.
- `ConstructorCrater.CaminoMurales.cs`: reúne la actualización del sendero, los nuevos murales y los ajustes localizados. Se ejecuta desde `AplicarInicioElevado`, también al reconstruir.
- `ConstructorCrater.Nivel.cs`: sol exterior de 12° → 9°; elimina la generación de los bloques decorativos `Borde_*` del cráter.
- `ConstructorCrater.Pozo.cs`: zona de entrada de centro (0; 4,8; −37,5), tamaño 16×9,6×16 m, a centro (0; 3; −35,5), tamaño 16×6×8 m. El límite inicial pasa de z −45,5 a −39,5, más abajo en la escalera. `Zona_Mirador` permanece intacta.
- `ConstructorCrater.LuzApertura.cs`: conserva cuatro focos de apertura, ahora al ras del reborde de tierra, sin las cajas exteriores.
- `ConstructorCrater.PuertaSol.cs`: ancla de diseño (−22,4; 0; 15,2) → (−22; 0; 10,9), en la esquina cercana a la entrada. Conserva orientación hacia el suelo seguro, conexión con la puerta y señal inferior.
- `LinternaController.cs`: cambio de filtro 0,6 → 0,35 s; soltar inicia la transición a blanco en vez de cancelar la animación. Mezcla gradual del color del haz; conserva filtros sostenidos.
- `LinternaVisual.cs`: movimiento más rápido, suavizado exponencial de 16/s; la lente acompaña el color del haz durante la transición.

## Murales

Se usan los nueve PNG nuevos de `images/murals_final`, según confirmación del usuario. Los originales permanecen intactos. El constructor genera texturas con transparencia en `Assets/Art/Murals/Runicos`: tinta mate sobre la pared, sin losa, marco ni emisión, con proporciones originales, mipmaps y compresión de alta calidad.

| Imagen | Ubicación |
|---|---|
| Entrada y descenso al crater | Pared izquierda del fondo del pozo, `Masa_Norte_Izq` |
| Umbral descubrimiento del farol | Pared izquierda del Umbral |
| Rotonda dos caminos | Dintel de la entrada de la Rotonda, cara interior |
| Ala SOL construir un paso | Pared norte de O2 |
| Ala LUNA atravesar lo sólido | Pared norte de E2 |
| Cruce alternar las dos luces | Pared derecha del Cruce |
| Cresta descubrir en la oscuridad | Pared derecha del corredor de entrada a Cresta, fuera de los espejos |
| Regreso el ciclo transformado | Pared izquierda del mismo corredor |
| Mystical Celestial Portal Schematic | Dintel de la puerta de los sellos, hacia la Rotonda |

No se modifican controles, tiempos de retención, velocidades, puzzles ni la arquitectura del Umbral. Los 51 PNG/WAV protegidos conservan su hash. Sin commit, push ni check-in.

Capturas de revisión en `Docs/Previews/CaminoMurales20261026`. Respaldo previo en `crater-chatgpt/Logs/RevisionCaminoMurales20261026/Antes`.

## Validación final

- `CraterGameplayTests`: EditMode 17/17 aprobados.
- `FiltrosSostenidosPlayTests`: PlayMode 5/5 aprobados (mouse, mando, apertura con puerta visible, transición a blanco y montaje de murales/borde).
- El montaje de pruebas aísla dispositivos reales y la captura de cursor del Editor para evitar reinicios del mouse simulado; el juego conserva sus controles.
- Validador del proyecto: 0 problemas. Revisión visual en Play del inicio, sendero y mural SOL.
- No se ejecutó la suite completa. El último ajuste visual eleva los dos murales del corredor de Cresta a 4,1 m para despejar los paneles de instrucción; se regeneró y volvió a validar la escena.
