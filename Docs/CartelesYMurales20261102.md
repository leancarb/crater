# Carteles, historia y correcciones — 2/11/2026

Base: `d6a2fb4`, rama `main`, Unity 6000.3.23f1 / URP Forward+. Se trabajó en el proyecto abierto `Desktop/crater` y se sincronizó con el repositorio `Desktop/crater-chatgpt`. Todos los objetos, luces, materiales y recursos importados se generan desde los constructores. No se editó a mano la escena.

## Errores corregidos

- **Farol invisible:** la selección del exterior por altura incluía Deposito, Lens_Difusor, Tapa y JaulaYAsa del jugador. Se excluye toda la jerarquía de `JugadorFPS`; el modelo funciona y se ve abajo.
- **Entrada al cráter perdida:** el trigger de una sola vez podía llamar a `EntrarAlCrater` durante la apertura y la condición `Reproduciendo` descartaba la solicitud. Ahora se conserva hasta terminar; además hay una comprobación de altura al bajar. El cielo cambia a 75° al comenzar a entrar, sin esperar a que termine el fundido.
- **Apertura tardía con movimiento libre:** Zona_Mirador se adelanta de Z local 55,5 a 80; mantiene 88 × 4 × 2 m. Totalidad 4 → 1,2 s y apertura 4 → 2,4 s; duración total 8 → 3,6 s. No se bloquea movimiento ni cámara.
- **Luna delante de montañas:** los astros estaban físicamente a unos 150 m y podían quedar delante de terreno más lejano. Se dibujan a profundidad de fondo, sin escribir profundidad y en orden cielo/corona/sol/luna. Las paredes y montañas los ocultan; la luna sigue tapando al sol. Diámetro aparente 9,5° → **7°**.
- **Texto a través de paredes:** los glifos usan `Crater/TextoMundo`, con prueba de profundidad. Se retiraron los textos físicos anteriores que usaban el material de fuente por defecto.
- **Carteles quemados por luz blanca cercana:** se limita la iluminancia visual al apuntar a una placa cercana. No cambia ángulo, alcance ni recepción de anclas/rejas.
- **Exterior sonando adentro:** la entrada ya no se pierde y la mezcla se completa en 1,2 s. El exterior se detiene al bajar; el interior usa el viento grave `viento_oculo`, en lugar del dron, a volumen 0,10. No se reemplazó ningún WAV.

| Sonido | Antes | Ahora |
|---|---:|---:|
| Ambiente exterior | 0,90 | 0,28 |
| Pájaros | 0,85 | 0,24 |
| Grave del eclipse | 0,50 | 0,18 |
| Apertura de tierra | 0,80 | 0,35 |
| Cierre de entrada | 0,70 | 0,40 |
| Ambiente interior | dron apagado (0) | viento de óculo, 0,10 |

## Arte y controles

Las carpetas reales son `images/tutorial` (10 PNG 1448 × 1086) e `images/historia` (12 PNG 1774 × 887). Se preservaron todos los originales. El usuario confirmó reutilizar la ilustración de construir el puente para SOL y dos anclas.

Los carteles conservan 4:3, placa de 1,8 × 1,35 m, marco, soporte y foco tenue de corto alcance. Se mide el recuadro vacío de cada PNG, en lugar de asumir una posición idéntica: su centro X varía aproximadamente de 14,8 % a 17,8 %. El glifo se centra y ajusta a ese espacio.

Se adoptó la convención **Xbox**, con iconos geométricos generados en Unity. A/B/X se superponen como botones redondos con letras verde/roja/azul; RB/LB/RT usan una silueta de botón con texto blanco. Los iconos desaparecen al usar teclado. Los PNG no llevan botones horneados. Las leyendas del HUD y pausa también usan nombres Xbox.

| Acción | Archivo de tutorial | Teclado / Xbox | Ubicación |
|---|---|---|---|
| 01 Mover | Directional Figure Icon Sign | WASD / stick izq. | Inicio, posición del antiguo Control_Mover |
| 02 Saltar | Jump Over the Obstacle Icon | Espacio / A | Borde lateral del primer tramo de bajada |
| 03 Recoger | Ancient Artifact Pickup Icon | E / B | Junto al farol y a la piedra trasladada al interior |
| 04 Farol | Lantern Bearer with Light Beam | F / X | Centro de la Rotonda |
| 05 SOL | Lantern Light Builds a Bridge | clic izq. sostenido / RB | Entrada del ala SOL |
| 06 LUNA | Lantern Beam Breaking Through Barriers | clic der. sostenido / LB | Entrada del ala LUNA |
| 07 Blanca | Lantern Beam Keycap Journey | soltar / soltar | Junto al cartel LUNA |
| 08 Dos anclas | Lantern Light Builds a Bridge | clic izq. / RB | Lugar de Ensenanza_Dos_Anclas |
| 09 Correr | Lantern Runner Approaches the Bridge | Shift / RT | Antes del segundo puente del Cruce |
| 10 Combinar | Lantern Puzzle Bridge Diagram | derecha → izquierda / LB → RB | Lugar de Ensenanza_Cruce_Combinacion |
| 11 Final | Lantern Off, Eye Watching | F / X | Entrada de la Cresta; reemplaza las dos placas anteriores |

Son 11 acciones y **12 soportes**, por la copia de recoger. Las placas 03/04 se ubican a X ±1,7 m y Z centro−0,6 m, dentro de la plaza hundida: dejan libre el eje al farol y la vuelta de radio 3,5 m. Se conservan los controles reales sostenidos: no se agregan 1/2/Q ni un botón para luz blanca. Caminar/correr siguen en 4 / 5,6 m/s; salto 0,4 m, sin alterar puzzles.

## Historia

Importación a `Assets/Art/Murals/Historia/01..12.png`. El constructor deriva un alfa suave que separa la tinta cálida del fondo aproximadamente #1C1A1D, conservando color, dibujo, proporción 2:1 y originales. Material URP/Lit mate, sin emisión, mipmaps y compresión de alta calidad, sRGB convencional. No hay losa, marco físico, teclas ni flechas añadidas.

| Nº | Fuente en images/historia | Montaje |
|---|---|---|
| 01 | Andean Traveler at the Sun Station | Pasaje, izquierda 1 |
| 02 | Andean Observatory Under Sun and Moon | Pasaje, izquierda 2 |
| 03 | Solar Eclipse Observation Station | Pasaje, izquierda 3 |
| 04 | Lantern Light Reveals Ancient Crater Glyphs | Pasaje, izquierda 4 |
| 05 | Eclipse Gateway and Lantern Traveler | Pasaje, derecha 1 |
| 06 | Ancient Eclipse Doorway with Lantern Bearer | Pasaje, derecha 2 |
| 07 | Andean Canyon Bridge Traveler | Pasaje, derecha 3 |
| 08 | Andean Eclipse Observation Station | Pasaje, derecha 4 |
| 09 | Andean Temple Lantern and Eclipse | Paño de la Rotonda visible al llegar, lateral al foco del farol |
| 10 | Sun and Moon Stepped Emblem | Dintel interior de entrada a Rotonda |
| 11 | Ancient Portal Beneath Sun and Moon | Dintel de la puerta de sellos |
| 12 | Celestial Cycle Over Observation Station | Pared izquierda del corredor de Cresta |

Se retiran las nueve instancias anteriores de Murales_Runicos, incluidos los cuatro paneles de mecánica y los duplicados de entrada/Umbral. Los archivos PNG antiguos siguen intactos. Se conservan las funciones Lore y los objetos Lore_1…8 para el nuevo montaje. Los cinco tallados latentes Apagar_* de la Cresta vuelven a ser geometría de narrativa mate: no se convierten en carteles ni se eliminan.

## Capturas y validación

- Reconstrucción completa desde `ConstructorCrater.ReconstruirTodo()` y validador: **0 errores**.
- EditMode completo: **33/33**, 0 omitidos.
- PlayMode completo final: **74/91**, **17 fallos**, 0 omitidos, 523,41 s. Las cinco regresiones nuevas pasan también en esta ejecución completa.
- Comparación con la ejecución previa (63/86, 23 fallos): todos los 17 fallos restantes ya figuraban allí; ningún nombre nuevo fallando. La primera ejecución de esta integración detectó una obstrucción de las placas; se corrigió en el constructor y el recorrido circular de la plaza pasó en la ejecución final.
- Huellas: **146/146 WAV y PNG originales idénticos**, tanto en el proyecto abierto como en el repositorio. Ningún archivo de `Assets/Audio` modificado.
- Evidencia y mensajes exactos: [EditMode](Testing/CartelesYMurales20261102/EditMode.json), [PlayMode final](Testing/CartelesYMurales20261102/PlayMode.json), [cinco regresiones](Testing/CartelesYMurales20261102/regresiones-final.json), [validador](Testing/CartelesYMurales20261102/validador-final.json), [huellas](Testing/CartelesYMurales20261102/huellas-despues.json).

La suite completa **no está verde**. Pendientes heredados:

| Prueba | Resultado |
|---|---|
| `CompuertaFeedbackPlayTests.AbrirConfirmaConTemblorCortoSinCambiarLaMirada` | Expected: in range (0,0001,0,012);   But was:  0.0f |
| `ConfirmacionPiedraPlayTests.RecogerConfirmaSinAyudasYUnDuplicadoNoRepiteLaConfirmacion` | System.InvalidOperationException : Sequence contains no matching element |
| `CorreccionEclipseAudioTests.ElSolMantieneElHorizonteExteriorYSeVeEnLaPlaza` | el sol exterior quedó alto;   Expected: 14 +/- 0.100000001f;   But was:  9.00000095f |
| `FiltroHUDPlayTests.ElRelojRespetaCambioTresSegundosPausaYRetornoAutomatico` | System.InvalidOperationException : Sequence contains no matching element |
| `GatilloCamaraPlayTests.GatilloAnalogicoYStickRespondenSinActivarCarreraConL3` | Expected: True;   But was:  False |
| `RecorridoCraterTests.ElAtajoOcultoSoloAcortaLaAproximacion` | System.NullReferenceException : Object reference not set to an instance of an object |
| `RecorridoCraterTests.ElEclipseAvanzaQuietoYElMiradorConservaLaMirada` | Expected: True;   But was:  False |
| `RecorridoCraterTests.ElExteriorTrasLaEstacionTieneSueloContinuo` | no se pudo llegar a (-38.00, 9.00, 17.50): el jugador quedó en (-33.01, 8.94, 11.38). Colliders cerca: Roca_Cierre_Valle@(-44.81, 24.93, 15.47), Terreno@(0.00, 8.90, -31.50), Jugador@(-33.01, 9.84, 11.38) |
| `RecorridoCraterTests.ElFinalFuncionaConColeccionCompleta` | llegar al mirador no abrió el cráter;   Expected: True;   But was:  False |
| `RecorridoCraterTests.ElFinalFuncionaConColeccionParcial` | llegar al mirador no abrió el cráter;   Expected: True;   But was:  False |
| `RecorridoCraterTests.ElRecorridoCompletoSePuedeJugar` | llegar al mirador no abrió el cráter;   Expected: True;   But was:  False |
| `RecorridoCraterTests.ElRecorridoConLunaPrimeroSePuedeJugar` | llegar al mirador no abrió el cráter;   Expected: True;   But was:  False |
| `RecorridoCraterTests.LaRotondaMuestraAmbosPendientesYAbrirNoExigeQuedarse` | UnityEngine.UnassignedReferenceException : The variable eclipseRegreso of ObeliscoDelEclipse has not been assigned.; You probably need to assign the eclipseRegreso variable of the ObeliscoDelEclipse script in the inspector. |
| `RecorridoCraterTests.LaRotondaMuestraLunaPrimeroSinTomarLaMirada` | System.IndexOutOfRangeException : Index was outside the bounds of the array. |
| `RecorridoCraterTests.LaRotondaMuestraSolPrimeroSinTomarLaMirada` | System.IndexOutOfRangeException : Index was outside the bounds of the array. |
| `RecorridoCraterTests.PisoYTechoReflejanYElOculoMuestraElCieloReal` | el techo o terreno tapa el eclipse real;   Expected: False;   But was:  True |
| `RecorridoCraterTests.TecladoMouseMandoYPausaConservanControlDuranteLosSellos` | Expected: same as <Filtro_Cuerpo (FiltroDefinicion)>;   But was:  null |

Las pruebas anteriores siguen accediendo a elementos retirados (obelisco, atajo, coleccionable exterior), rutas anteriores y contratos de filtro automático/gatillo/cámara. No se deshabilitaron ni se tocaron esas mecánicas para obtener un verde. Su actualización o investigación queda pendiente. El fallo de visibilidad desde otras posiciones de la plaza continúa registrado; la vista del eclipse desde el centro se verificó en Play y en captura.

Capturas de la escena final, sin alterar los PNG de origen:

- [Cartel con teclado](Previews/CartelesYMurales20261102/01_CartelTeclado.png), [Xbox RB](Previews/CartelesYMurales20261102/02_CartelMando.png), [Xbox B](Previews/CartelesYMurales20261102/02b_Boton_B_Xbox.png), [farol blanco cercano](Previews/CartelesYMurales20261102/01b_Cartel_FarolBlanco.png).
- [Pasaje, historia 01–08](Previews/CartelesYMurales20261102/03_Pasaje_Historia_01_08.png).
- [Lobby tras recoger el farol: carteles 03/04 e historia 09/11](Previews/CartelesYMurales20261102/04_Lobby_Carteles_Historia_Farol.png).
- [Entrada de Cresta: cartel 11, historia 12 y tallados latentes](Previews/CartelesYMurales20261102/05_Entrada_Cresta.png).
- [Eclipse interior](Previews/CartelesYMurales20261102/06_Eclipse_Interior.png), [horizonte exterior](Previews/CartelesYMurales20261102/07_Exterior_Profundidad_Luna.png).

Todas las imágenes conservan proporción. El mural 09 queda más pequeño (**1,46 × 0,73 m**) para caber en un paño plano de la pared facetada y no competir con el farol; su lectura a distancia es el punto a revisar en el próximo playtest. El cartel SOL queda parcialmente detrás de su pedestal desde una mirada frontal, pero la zona del botón y el pictograma quedan visibles. Quedan sin probar mando físico, escucha humana de la mezcla y un build de jugador; Xbox se ejercitó mediante dispositivos simulados del Input System. Se salió de Play y se restauraron los dispositivos habilitados antes de las capturas.


## Ubicaciones de código

| Cambio | Archivo y línea |
|---|---|
| Importación y alfa | `Assets/Editor/ConstructorCrater.CartelesHistoria.cs:47` |
| Soporte e iconos Xbox | `Assets/Editor/ConstructorCrater.CartelesHistoria.cs:91` |
| Montaje y retiro de paneles anteriores | `Assets/Editor/ConstructorCrater.CartelesHistoria.cs:149` |
| Cambio de dispositivo y ajuste de glifo | `Assets/Scripts/Interfaz/CartelTutorial.cs:32` |
| Nombres Xbox | `Assets/Scripts/Nucleo/EntradaCrater.cs:73` |
| Entrada pendiente | `Assets/Scripts/Flujo/PrologoCapilla.cs:270` |
| Brillo blanco sobre cartel | `Assets/Scripts/Linterna/LinternaController.cs:140` |
| Tamaño, tiempos y exclusión del jugador | `Assets/Editor/ConstructorCrater.Iteracion.cs:41` |
| Ambiente nuevo | `Assets/Editor/ConstructorCrater.Nivel.cs:598` |
| Orden de astros | `Assets/Editor/ConstructorCrater.cs:278` |

## Archivos principales

- `Assets/Editor/ConstructorCrater.CartelesHistoria.cs`: catálogo, importación reproducible, medición de recuadros, placas, iconos y doce montajes de historia.
- `Assets/Scripts/Interfaz/CartelTutorial.cs`, `EntradaCrater.cs`, `InterfazCrater.cs`: dispositivo activo, leyendas e iconos Xbox; conserva bindings.
- `Assets/Scripts/Flujo/PrologoCapilla.cs`, `ConstructorCrater.Iteracion.cs`: apertura adelantada, entrada diferida, visibilidad del jugador, mezcla.
- `Assets/Art/Shaders/CieloSinNiebla.shader`, `TextoMundo.shader`: profundidad correcta de astros y glifos.
- `Assets/Scripts/Linterna/LinternaController.cs`: legibilidad de carteles con luz blanca cercana.
- `ConstructorCrater.Nivel.cs`, `.CaminoMurales.cs`, `.Murales.cs`, `.Placas.cs`, `ConstructorCrater.cs`: integración, audio y conservación de tallados.
- `ValidarProyectoCrater.cs`, tests EditMode/PlayMode: nuevos contratos de montaje y regresiones de entrada/render.

Salidas generadas: texturas, materiales, mallas, prefabs, escena y oclusión. Sin push ni check-in de Unity Version Control.

Inventario completo de fuentes, recursos generados, arte y documentación: [ArchivosTocados.tsv](Testing/CartelesYMurales20261102/ArchivosTocados.tsv).
