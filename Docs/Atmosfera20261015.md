# Atmósfera y señales de luz — 15/10/2026

Decisión vigente posterior a Ritmo20261015 y Playtest20261015: conservar la oscuridad con lectura del espacio, retirando los rellenos puntuales sin una fuente visible. Se mantiene URP/Lit, la paleta y la niebla. Todos los cambios de escena, materiales y prefabs se producen desde ConstructorCrater.

- Ambiente interior en gradiente HDR: cielo (1,3; 1,35; 1,45), horizonte (1,05; 1,05; 1,1), suelo (0,6; 0,58; 0,58). Compensa el albedo bajo del basalto sin colocar puntos de luz en el aire. Las capturas iniciales con valores menores dejaban las alas casi negras y se descartaron.
- Exposición interior +0,65 EV; exterior 0 EV. Transición continua al bajar/subir la escalera según altura de los ojos (y=7 a y=3). El ajuste de brillo del jugador se suma y el perfil compartido no cambia durante el juego. La adaptación final conserva 3,2 EV y sus tiempos.
- Bloom: threshold 1, intensidad 0,22. Contraste 3, viñeta 0,16, grano 0,035. Saturación -6. Se conserva el SSAO existente.
- Se eliminan los rellenos enumerados en ConstructorCrater.Atmosfera.cs. Se conservan las luces de velas, óculos, objetos interactivos y respuestas de progreso. Las guías de piso frente a la Cresta y la puerta de sellos siguen brillando.
- Lámpara superior visible sobre Ancla_Umbral, Ancla_O_Puerta y Sello_Oeste. Las anclas de puentes no reciben esta lámpara. La lámpara del Umbral conserva su forma y emisión; las otras dos usan el mismo diseño.
- Linterna blanca: apertura 54°, núcleo 12% de la apertura, intensidad 1100. SOL/LUNA: apertura visual 44°, intensidad 360; cono efectivo para activar receptores 28°. Se mantienen los 2 segundos efectivos de filtro y las parejas separadas. El reflejo usa la apertura visual para coincidir con el haz.
- Los seis pilares centrales se iluminan desde sus bases con luminarias visibles y haces ascendentes, ámbar en SOL y azul en LUNA. Se retiran los pequeños glifos de sus caras y los dos rellenos elevados del conjunto. Se conservan el orden, las notas y las transiciones de las dos secuencias.
- El sol y la luna del obelisco quedan al ras de la piedra, con una separación técnica de 3 mm de diseño para evitar parpadeos. Conservan su respuesta al progreso y la apertura del Cruce.

La revisión se comprueba con el validador, pruebas de exposición interior/exterior, lámparas de puertas, encendido independiente de bases y distancia de los símbolos a la piedra. Las pruebas de recorrido conservan ambos órdenes de alas, secuencias de columnas, los cinco puentes con una sola activación de SOL y el final.

Evidencia visual local: Logs/Atmosfera20261015/Antes y Despues. Se compararon el Umbral (apagada y con haz), Rotonda, SOL, LUNA, Cruce, Cresta y pilares encendidos. No se regeneran ejecutables ni ZIP hasta volver a construir demo. Los WAV y Assets/Audio/Grabado se conservan.

## Fuentes de los cambios

| Archivo y línea | Cambio |
|---|---|
| Assets/Editor/ConstructorCrater.Atmosfera.cs:7 | Lista explícita de rellenos sin fuente y retirada al regenerar; suavizado de haces de lámparas y óculos. |
| Assets/Editor/ConstructorCrater.Nivel.cs:98 | Aplicación de la atmósfera antes del presupuesto de sombras y la oclusión. |
| Assets/Editor/ConstructorCrater.Nivel.cs:151 | Ambiente interior en gradiente HDR. |
| Assets/Editor/ConstructorCrater.Nivel.cs:597 | Observador y exposición interior en el sistema existente de adaptación. |
| Assets/Editor/ConstructorCrater.Nivel.cs:728 | Lámparas superiores para las anclas de puertas; helper común en línea 749. |
| Assets/Editor/ConstructorCrater.Alas.cs:288 | Luminarias de base en las seis columnas y retirada de glifos pequeños/rellenos elevados. |
| Assets/Editor/ConstructorCrater.Alas.cs:336 | Sol y luna al ras de las caras reales del obelisco; intersección de triángulos a la altura del símbolo en línea 194, sin usar los límites del pedestal. |
| Assets/Editor/ConstructorCrater.cs:181 | Intensidad de filtros 360. |
| Assets/Editor/ConstructorCrater.cs:579 | Bloom, exposición, contraste, viñeta y grano. |
| Assets/Editor/ConstructorCrater.cs:693 | Configuración generada de la linterna blanca y sus bordes. |
| Assets/Scripts/Flujo/AdaptacionOscuridad.cs:24 | Exposición base según altura, conservando brillo de opciones y adaptación final. |
| Assets/Scripts/Linterna/LinternaController.cs:44 | Apertura visual independiente del cono efectivo de receptores. |
| Assets/Scripts/Flujo/ParedEspejo.cs:92 | Reflejo ajustado a la apertura visual del haz. |
| Assets/Editor/ValidarProyectoCrater.cs:76 | Validación de cono efectivo, apertura suave, bases y lámparas de puertas. |
| Assets/Tests/PlayMode/AtmosferaCraterTests.cs:11 | Pruebas de bases, lámparas y exposición. |
| Assets/Tests/PlayMode/RitmoCraterTests.cs:54 | El cono efectivo sigue en 28° con haz visual de 44°. |
| Assets/Tests/EditMode/CraterRevisionTests.cs:12 | Todos los vértices de SOL/LUNA se apoyan en la malla real antes del static batching. |
| Assets/Tests/EditMode/CraterRevisionTests.cs:139 | Murales sin rellenos puntuales flotantes. |
| Assets/Editor/VistaPreviaCrater.cs:97 | Vistas previas con el mismo ambiente, linterna y exposición por altura que el juego. |
| Assets/Tests/PlayMode/Crater.Tests.PlayMode.asmdef:9 | Referencias a URP/Core para verificar el perfil de exposición. |

Los assets generados correspondientes son Assets/Scenes/CraterVerticalSlice.unity y su oclusión, Assets/Models/Facetado/MallasFacetadas.asset, Assets/Settings/CraterVolumeProfile.asset, los dos filtros y los prefabs Jugador, Recogible_Filtro y Reja. Las capturas de las pruebas se actualizan en Docs/Previews/Exploracion y Docs/Previews/Recorrido. README, GDD y Docs/AI/UnityProjectContext.md registran la decisión vigente.

## Validación de la revisión

Unity 6000.3.23f1: compilación sin errores y validador sin problemas. EditMode final: 29/29, incluido el apoyo de cada vértice de los símbolos sobre la malla real.

Se ejecutó toda la assembly PlayMode (44 tests, 914,16 s): 42 pasaron y 2 pruebas de entrada dieron valores nulos con Game View sin foco. Ambas pasaron individualmente al repetirlas con Unity al frente, sin cambiar controles de producción. Tras corregir el encastre sobre las caras reales se repitieron las tres secuencias de Rotonda (3/3), las pruebas de atmósfera (3/3) y el paso físico alrededor de las columnas (1/1). Los últimos resultados por test son 44 aprobados, sin omisiones. Esto combina la corrida completa y las repeticiones dirigidas; no es una segunda corrida completa de 44.

Los informes originales, las repeticiones y validacion-agregada.json se guardan localmente en Logs/Atmosfera20261015. Los 43 archivos de audio protegidos conservan su SHA-256. No se generaron nuevos ejecutables ni ZIP.
