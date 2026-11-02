# Iteración de CRATER — 2/11/2026

Base: `e9ab941` en `main`. Unity 6000.3.23f1, URP Forward+. Implementación en constructores y scripts; escena, prefabs, mallas, materiales y oclusión reconstruidos con `ConstructorCrater.ReconstruirTodo()`.

## Cambios por punto

| Punto | Implementación y valores | Fuente principal |
|---|---|---|
| 1. Encuadre exterior | Se eleva progresivamente el banco lateral del sendero final (refuerzo adicional de hasta 1,1 m). Conserva un centro bajo y el eclipse visible; no orienta la cámara. | `Assets/Editor/ConstructorCrater.InicioElevado.cs`, función `AlturaInicioElevado` |
| 2. Bajada | Pretiles de piedra de 1,3 m de alto, 0,35 m de espesor, a ambos lados; tramos de aproximadamente 1,5 m y encuentros cerrados en los cambios de dirección. | `Assets/Editor/ConstructorCrater.Iteracion.cs:13` |
| 3. Spawn | Coordenada local X = 4 m, Z = 184 m; altura tomada del terreno; yaw 155° frente a los 180° del eje anterior. | `Assets/Editor/ConstructorCrater.Iteracion.cs:37` |
| 4. Eclipse | Diámetro angular 7° → 9,5°, compartido por exterior e interior; corona sigue la proporción del disco. | `Assets/Editor/ConstructorCrater.Iteracion.cs:41` |
| 5. Lobby oscuro | Luces ambientales, de lectura y accesos apagadas al iniciar; conserva el brillo propio del farol y el aporte tenue del óculo (18 → 6 de intensidad). También incluye las luces opcionales situadas dentro de la Rotonda. | `Assets/Scripts/Flujo/LlegadaRotonda.cs`, `ConstructorCrater.Iteracion.cs:54` |
| 6. Recoger farol | Cierra una losa de 15 × 4 × 0,45 m en la entrada, recorrido 4,5 m y duración 1,2 s. Usa el sonido de piedra existente, volumen 0,7. Iluminación gradual en 2,5 s; incluye pasillos SOL/LUNA. La piedra exterior mantiene su identificador y pasa al borde interior de la Rotonda, coordenada de diseño (-4; 0,3; 0), del lado accesible después del cierre. Atajo inicial desactivado. | `LlegadaRotonda.cs`, `Compuerta.cs`, `ConstructorCrater.Iteracion.cs` |
| 7. Exterior oculto | Bajo Y = 7,5 m se ocultan renderizadores del exterior, incluido el cierre rocoso y los elementos opcionales. Se restaura el estado original al subir; conserva GameObjects, colisiones y eclipse. | `Assets/Scripts/Flujo/VisibilidadExterior.cs`, `ConstructorCrater.Iteracion.cs:76` |
| 8. Tutorial | Iconos reducidos a la acción principal, etiqueta de teclado/mando según último dispositivo usado. Se añade un cartel separado para mirar. | `Assets/Editor/ConstructorCrater.Placas.cs:113`, `Assets/Scripts/Interfaz/EtiquetaTutorial.cs` |
| 9. Tutorial / historia | Tutorial: placa clara #D1CEC1, trazo oscuro #222831, esquinas en L, foco y soporte. Historia: pintura mate sin losa ni marco. Lore_1…8 usa tinta extraída de sus PNG originales; las figuras procedurales tienen cabeza cuadrada, cuerpo trapezoidal y extremidades sólidas. La conversión completa ahora forma parte de Reconstruir todo. | `ConstructorCrater.Placas.cs`, `ConstructorCrater.Murales.cs:906`, `ConstructorCrater.Nivel.cs:100` |
| 10. Filtros | Se verificó que ambos ya tenían el mismo cono (28°) y alcance (18,75 m); se conservan. Esta iteración no cambia posiciones, canales ni tiempos de retención de receptores. | Datos de filtros y constructor |
| 11. Grutas | Luces rojas RGB (0,55; 0,015; 0,01). Principal: intensidad 32 → 10, rango 8 → 3 m; pie: 18 → 6 y 4 → 2,5 m. Rincón O1: pasa a rojo, rango 3 → 2,5 m, sombras activadas. Valores de diseño antes de la compensación espacial ×1,25 en distancias / ×1,5625 en intensidad. | `ConstructorCrater.Alas.cs:665`, `ConstructorCrater.Detalles.cs:31` |
| 12. Perspectiva | Al alinear el motivo aparece un rayo fino hacia el farol; desaparece al perder la alineación o recogerlo. | `Assets/Scripts/Flujo/MotivoPorPerspectiva.cs`, `ConstructorCrater.Iteracion.cs` |
| 13. Salto | 0,4 m, gravedad -18 m/s²; Espacio / A (×). Velocidad horizontal limitada a 2 m/s durante el salto, stepOffset = 0 hasta aterrizar; corta ascenso al golpear el techo. Caminar/correr siguen en 4 / 5,6 m/s. | `Assets/Scripts/Jugador/JugadorFPS.cs:25` |
| 14. Interacción | E / B (○) recoge y omite animaciones. Espacio / A queda sólo para salto. Pausa y ayudas actualizadas. | `Assets/Scripts/Nucleo/EntradaCrater.cs:77`, `InterfazCrater.cs:312` |

La propuesta del documento de usar L1 partía de un esquema anterior. Se conserva el esquema vigente: F / X (□) para el farol; mantener clic izquierdo / R1 para SOL y derecho / L1 para LUNA; soltar devuelve blanco en 0,35 s. B (○) estaba libre durante el juego; en pausa conserva la acción Seguir. No se restaura el temporizador de filtros ni las teclas 1/2.

## Capturas de Play

Tomadas del juego en el Editor, 1280 × 720, sin superponer anotaciones ni alterar las imágenes.

- [Pasillo de lomadas y eclipse](Previews/Iteracion20261102/01_EncuadreEclipse.png).
- [Lobby oscuro y farol](Previews/Iteracion20261102/02_LobbyOscuro.png).
- [Lobby encendido después de recoger](Previews/Iteracion20261102/03_LobbyEncendido.png).
- [Cartel de tutorial](Previews/Iteracion20261102/04_CartelTutorial.png).
- [Narrativa directamente en pared](Previews/Iteracion20261102/05_HistoriaPared.png).
- [Gruta: aporte rojo tenue al piso](Previews/Iteracion20261102/06_GrutaRoja.png).
- [Óculo con eclipse y sin montañas](Previews/Iteracion20261102/07_OculoEclipse.png).

## Rendimiento

Comparación controlada en Play del Editor: misma cámara y luces en la Rotonda, RenderTexture 1280 × 720, tres renders de calentamiento y veinte llamadas `Camera.Render()` cronometradas por estado. Se compara la misma escena activando/desactivando exclusivamente la lista exterior.

| Estado | Renderizadores exteriores activos | CPU por Camera.Render |
|---|---:|---:|
| Exterior visible | 527 | 1,767080 ms |
| Exterior oculto | 0 | 1,708575 ms |

La diferencia de aproximadamente 0,059 ms es orientativa y puede variar entre muestras. No representa FPS, tiempo GPU ni una promesa de mejora del build. La ganancia comprobada es que el paisaje deja de enviarse al render de la Rotonda. Falta perfilado de GPU y un build en el equipo objetivo.

## Validación

- Reconstrucción completa desde código y validador: **0 problemas**.
- Suite EditMode completa: **33/33 aprobados**.
- Suite PlayMode completa: **63/86 aprobados, 23 fallidos, 0 omitidos**. No se considera validado el recorrido completo.
- Los **5 tests nuevos** de esta iteración pasaron en esa suite: encendido/cierre, altura de salto, no superar pretil, no cruzar hueco y restauración del exterior.
- Pasaron los seis tests de filtros sostenidos (mouse y mando, retorno animado, puerta SOL visible, murales y tallados), los controles de mando y las pruebas de recogida con E/B.
- Tras revisar los fallos se adaptó la lectura del cartel al soporte a 1,6 m y se aseguró restaurar su CharacterController aunque falle una aserción. También se agregó al test de cierre la accesibilidad de la piedra trasladada. Resultado de estas comprobaciones finales: **5/5 tests nuevos y 4/4 de carteles/control aprobados**. El intento intermedio bloqueado por el diálogo de la escena temporal se canceló; después se reconstruyó correctamente y EditMode volvió a pasar 33/33.

El 63/86 corresponde a la corrida completa; las comprobaciones posteriores se reportan por separado. Los dos fallos de `CorreccionDocenteCompletaTests` se verificaron resueltos en su repetición 4/4. No se volvió a ejecutar la suite completa después de esos ajustes.

Los fallos no se ocultaron ni se marcaron como omitidos. Hay expectativas desactualizadas verificables en el código base: filtro automático de 3 s frente al control sostenido; gatillo 0,35 / sensibilidad 40 frente a los valores base 0,5 / 32; inicio antiguo que camina recto al mirador, busca Ancla_Umbral y recoge el farol antes de la Rotonda. También aparecen fallos de feedback anterior de compuertas/piedras, reflejos, entradas de mouse, movimiento y oclusión. No se atribuyen todos a esta iteración sin reproducirlos aisladamente. Queda pendiente adaptar y diagnosticar estos escenarios hasta lograr una suite completa verde.

Evidencia: [EditMode](Testing/Iteracion20261102/EditMode.json), [PlayMode completo](Testing/Iteracion20261102/PlayModeCompleto.json), [tests nuevos](Testing/Iteracion20261102/TestsNuevos.json), [carteles](Testing/Iteracion20261102/Carteles.json).

### Fallos de la corrida completa

- `CompuertaFeedbackPlayTests.AbrirConfirmaConTemblorCortoSinCambiarLaMirada`: Expected: in range (0,0001,0,012)   But was:  0.0f
- `ConfirmacionPiedraPlayTests.RecogerConfirmaSinAyudasYUnDuplicadoNoRepiteLaConfirmacion`: System.InvalidOperationException : Sequence contains no matching element
- `CorreccionDocenteCompletaTests.LasLeccionesSeLeenDesdeSueloSeguroYNoTapanLasAnclas`: Ensenanza_Dos_Anclas   Expected: in range (0,05,0,95)   But was:  -0.0814325809f
- `CorreccionDocenteCompletaTests.PerderFocoNoPausaPeroLaPausaManualSigueFuncionando`: SetUp : Unhandled log message: '[Error] CharacterController.Move called on inactive controller'. Use UnityEngine.TestTools.LogAssert.Expect
- `CorreccionEclipseAudioTests.ElSolMantieneElHorizonteExteriorYSeVeEnLaPlaza`: el sol exterior quedó alto   Expected: 14 +/- 0.100000001f   But was:  9.00000095f
- `ExploracionCraterTests.ConjuntoEsperaLaVueltaRespetaPausaYAdmiteReescuchaConMando`: Expected: True   But was:  False
- `ExploracionCraterTests.ReflejoSeCapturaSoloAlAcercarseYElRelieveRespondeAlAngulo`: System.InvalidOperationException : Sequence contains no matching element
- `FiltroHUDPlayTests.ElRelojRespetaCambioTresSegundosPausaYRetornoAutomatico`: System.InvalidOperationException : Sequence contains no matching element
- `GatilloCamaraPlayTests.GatilloAnalogicoYStickRespondenSinActivarCarreraConL3`: Expected: True   But was:  False
- `RecorridoCraterTests.ElAtajoOcultoSoloAcortaLaAproximacion`: System.NullReferenceException : Object reference not set to an instance of an object
- `RecorridoCraterTests.ElEclipseAvanzaQuietoYElMiradorConservaLaMirada`: Expected: True   But was:  False
- `RecorridoCraterTests.ElExteriorTrasLaEstacionTieneSueloContinuo`: no se pudo llegar a (-38.00, 9.00, 17.50): el jugador quedó en (-32.97, 8.94, 11.40). Colliders cerca: Roca_Cierre_Valle@(-44.81, 24.93, 15.47), Terreno@(0.00, 8.90, -31.50), Jugador@(-32.97, 9.84, 11.40)
- `RecorridoCraterTests.ElFinalFuncionaConColeccionCompleta`: llegar al mirador no abrió el cráter   Expected: True   But was:  False
- `RecorridoCraterTests.ElFinalFuncionaConColeccionParcial`: llegar al mirador no abrió el cráter   Expected: True   But was:  False
- `RecorridoCraterTests.ElRecorridoCompletoSePuedeJugar`: llegar al mirador no abrió el cráter   Expected: True   But was:  False
- `RecorridoCraterTests.ElRecorridoConLunaPrimeroSePuedeJugar`: llegar al mirador no abrió el cráter   Expected: True   But was:  False
- `RecorridoCraterTests.LaRotondaMuestraAmbosPendientesYAbrirNoExigeQuedarse`: UnityEngine.UnassignedReferenceException : The variable eclipseRegreso of ObeliscoDelEclipse has not been assigned. You probably need to assign the eclipseRegreso variable of the ObeliscoDelEclipse script in the inspector.
- `RecorridoCraterTests.LaRotondaMuestraLunaPrimeroSinTomarLaMirada`: System.IndexOutOfRangeException : Index was outside the bounds of the array.
- `RecorridoCraterTests.LaRotondaMuestraSolPrimeroSinTomarLaMirada`: System.IndexOutOfRangeException : Index was outside the bounds of the array.
- `RecorridoCraterTests.PisoYTechoReflejanYElOculoMuestraElCieloReal`: el techo o terreno tapa el eclipse real   Expected: False   But was:  True
- `RecorridoCraterTests.TecladoMouseMandoYPausaConservanControlDuranteLosSellos`: el mouse no controla la mirada   Expected: greater than 0.100000001f   But was:  0.0f
- `RitmoCraterTests.ElJugadorCaminaACuatroYCorreConShiftOR2SinSprintEnL3`: Expected: 4.0f +/- 0.150000006f   But was:  0.0f
- `RitmoCraterTests.FiltroSostenidoRespetaPausaYSueltaAlReanudar`: Expected: same as <Filtro_Cuerpo (FiltroDefinicion)>   But was:  null

Se verificaron 95 huellas WAV/PNG al inicio y al terminar, tanto en `crater-chatgpt` como en el proyecto abierto `crater`: 0 diferencias. Reconstruir todo ahora carga los clips existentes en lugar de regenerar los WAV. Ningún original protegido fue reemplazado.

## Reproducción y límites

La actualización local del inicio también carga los clips existentes y reemplaza sus sistemas; no agrega copias de la puerta, el encendido, el control de visibilidad ni el cartel de mirar.

- Abrir `Assets/Scenes/CraterVerticalSlice.unity`; los recursos generados están incluidos. Reconstruir todo vuelve a generar esta iteración.
- Los pretiles tienen colisión; el salto se probó contra un obstáculo de 1 m (menor que los pretiles reales de 1,3 m) y un hueco de 2,5 m, sin permitir cruzarlos. No equivale a una auditoría exhaustiva de todas las rutas alternativas del mapa.
- La gruta mantiene el arte mate visible; el rojo es tenue y puede necesitar ajuste artístico en el playtest. No se usa un volumen rojo global que tiña las salas superiores.
- La captura del eclipse usa la totalidad como estado de observación; el jugador conserva el control durante el avance y la apertura.
- No se generó un build de distribución, no se hizo push y no se hizo check-in de Unity Version Control.

## Archivos de la iteración

Los `.mat`, prefabs, mallas, escena y oclusión de esta lista son salidas del constructor; no se editaron a mano.

- `Assets/Editor/ConstructorCrater.Alas.cs`
- `Assets/Editor/ConstructorCrater.CaminoMurales.cs`
- `Assets/Editor/ConstructorCrater.Detalles.cs`
- `Assets/Editor/ConstructorCrater.InicioElevado.cs`
- `Assets/Editor/ConstructorCrater.Iteracion.cs`
- `Assets/Editor/ConstructorCrater.Murales.cs`
- `Assets/Editor/ConstructorCrater.Nivel.cs`
- `Assets/Editor/ConstructorCrater.Placas.cs`
- `Assets/Editor/ConstructorCrater.cs`
- `Assets/Editor/GeneradorAudioCrater.cs`
- `Assets/Editor/ValidarProyectoCrater.cs`
- `Assets/Materials/Agua_Cuenco.mat`
- `Assets/Materials/CoronaEclipse.mat`
- `Assets/Materials/DiscoLuna.mat`
- `Assets/Materials/DiscoSol.mat`
- `Assets/Materials/Escalera_Luna_Luminaria.mat`
- `Assets/Materials/Escalera_Sol_Luminaria.mat`
- `Assets/Materials/Exterior_CieloBrumoso.mat`
- `Assets/Materials/Farol_Cuerpo.mat`
- `Assets/Materials/Farol_Jaula.mat`
- `Assets/Materials/Guia_Farol_Perspectiva.mat`
- `Assets/Materials/PinturaHueso.mat`
- `Assets/Materials/Placa_Pizarra.mat`
- `Assets/Materials/Polvo_Suave.mat`
- `Assets/Materials/Trazo_Instruccion.mat`
- `Assets/Models/Facetado/MallasFacetadas.asset`
- `Assets/Models/Generado/Relieve_Inicio_Elevado.asset`
- `Assets/Models/Generado/Sendero_Tierra_Clara.asset`
- `Assets/Prefabs/Jugador.prefab`
- `Assets/Prefabs/Recogible_Filtro.prefab`
- `Assets/Prefabs/Reja.prefab`
- `Assets/Scenes/CraterVerticalSlice.unity`
- `Assets/Scenes/CraterVerticalSlice/OcclusionCullingData.asset`
- `Assets/Scripts/Flujo/LlegadaRotonda.cs`
- `Assets/Scripts/Flujo/MotivoPorPerspectiva.cs`
- `Assets/Scripts/Flujo/PrologoCapilla.cs`
- `Assets/Scripts/Flujo/VisibilidadExterior.cs`
- `Assets/Scripts/Interfaz/EtiquetaTutorial.cs`
- `Assets/Scripts/Interfaz/InterfazCrater.cs`
- `Assets/Scripts/Jugador/JugadorFPS.cs`
- `Assets/Scripts/Mecanicas/Compuerta.cs`
- `Assets/Scripts/Nucleo/EntradaCrater.cs`
- `Assets/Tests/EditMode/CraterRevisionTests.cs`
- `Assets/Tests/EditMode/PlacasInstruccionTests.cs`
- `Assets/Tests/PlayMode/CorreccionDocenteCompletaTests.cs`
- `Assets/Tests/PlayMode/CorreccionProfesorTests.cs`
- `Assets/Tests/PlayMode/EntradaJoystickTests.cs`
- `Assets/Tests/PlayMode/ExploracionCraterTests.cs`
- `Assets/Tests/PlayMode/IteracionNoviembreTests.cs`
- `Assets/Tests/PlayMode/PlacasLecturaPlayTests.cs`
- `Assets/Tests/PlayMode/RecorridoCraterTests.cs`
- `Assets/Tests/PlayMode/RotondaCartelesTests.cs`
- `Docs/EstadoActualCrater.md`
- `Docs/Iteracion20261102.md`
- `Docs/Previews/Iteracion20261102/01_EncuadreEclipse.png`
- `Docs/Previews/Iteracion20261102/02_LobbyOscuro.png`
- `Docs/Previews/Iteracion20261102/03_LobbyEncendido.png`
- `Docs/Previews/Iteracion20261102/04_CartelTutorial.png`
- `Docs/Previews/Iteracion20261102/05_HistoriaPared.png`
- `Docs/Previews/Iteracion20261102/06_GrutaRoja.png`
- `Docs/Previews/Iteracion20261102/07_OculoEclipse.png`
- `Docs/Testing/Iteracion20261102/Carteles.json`
- `Docs/Testing/Iteracion20261102/EditMode.json`
- `Docs/Testing/Iteracion20261102/PlayModeCompleto.json`
- `Docs/Testing/Iteracion20261102/TestsNuevos.json`
- `GDD.md`
- `README.md`
