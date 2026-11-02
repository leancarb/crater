# Inicio elevado y farol central — 26/10/2026

Aplicado en el proyecto abierto **C:/Users/carbo/Desktop/crater**, Unity 6000.3.23f1. La versión abierta tenía cambios respecto de crater-chatgpt; los scripts compartidos se modificaron puntualmente en ambas copias, sin reemplazar sus otras diferencias ni restaurar contenido descartado.

## Resultado

- Inicio nuevo a z local 184, cinco metros por encima del inicio anterior. Bajada de 34 m, con centro suavemente curvo, que empalma a z150 con el relieve anterior. Cámara y movimiento siguen bajo control del jugador.
- El terreno anterior a ese empalme conserva su perfil. Se extendió el terreno caminable, se corrió el cierre posterior y se agregaron 23 rocas de encuadre con una sola malla y material compartidos. Son piezas provisionales para sustituir por el kit.
- Reborde bajo del impacto: unos 0,8 m máximos de elevación, ancho radial de 8,7 m y contorno irregular. El frente de acceso queda bajo y despejado; no se modifica la tapa ni su animación de apertura.
- Farol recogible en el centro de la Rotonda, donde estaba el obelisco, sobre un apoyo cilíndrico bajo. Se retiró la geometría visible y la colisión del obelisco. Su controlador técnico conserva las secuencias de las seis columnas, aros y puerta del Cruce.
- **Corrección final del usuario:** se conserva todo el Umbral, su pasillo, piso, paredes, techo, luces y murales. Se retiran sólo el ancla de ese mecanismo, su base/lámpara asociadas y la compuerta. No se acortó ni eliminó la sala.
- FlujoJuegoCrater pasa a Explorar al encender el farol si no existe la compuerta del Umbral; conserva compatibilidad con escenas anteriores que sí la tienen.

## Archivos de esta tanda

- Assets/Editor/ConstructorCrater.InicioElevado.cs: terreno, encuadre, reborde, traslado y actualización localizada.
- Assets/Editor/ConstructorCrater.Nivel.cs: integra AplicarInicioElevado en la reconstrucción, antes de sombras/oclusión.
- Assets/Scripts/Flujo/FlujoJuegoCrater.cs: continuidad sin la primera compuerta.
- Assets/Editor/ValidarProyectoCrater.cs: acepta la variante nueva y comprueba Umbral conservado, ausencia del mecanismo, farol central y terreno.
- Assets/Tests/PlayMode/InicioElevadoPlayTests.cs: dos pruebas de integración.
- Assets/Models/Generado/Relieve_Inicio_Elevado.asset y Reborde_Crater_Antiguo.asset, con sus metadatos: mallas persistentes reutilizadas al volver a actualizar.
- En el proyecto abierto también se guardaron Assets/Scenes/CraterVerticalSlice.unity, datos de oclusión y el contenedor de mallas usado por la oclusión. No se sobrescribió la escena de la copia Git, que contenía diferencias previas.

## Cómo seguir con el kit

El grupo `NIVEL/06_Capilla/Inicio_Elevado_Kit` contiene las rocas provisionales y el reborde. `NIVEL/06_Capilla/Paisaje/Lomas_Valle` contiene la malla caminable.

Las medidas de arranque están al principio de ConstructorCrater.InicioElevado.cs: InicioElevadoZ=184, FinBajadaInicialZ=150, DesnivelInicio=5. El futuro modelo de roca se conecta donde ahora se elige la malla compartida de Cerro_Inicio; las colocaciones tienen nombres Roca_Bajada_O/E y Roca_Fondo. El terreno y el reborde son mallas continuas independientes del kit de objetos.

La actualización ya está aplicada. Menú para repetir sólo esta parte: **Crater > Actualizar inicio elevado y farol**. Los reemplazos definitivos deben incorporarse al constructor para conservarlos al regenerar.

## Validación y límites

- Compilación de Unity sin errores; validador **0 problemas**, igual que antes.
- PlayMode focalizado **2/2**: bajada con CharacterController y recogida/flujo sin compuerta; seis columnas y apertura del Cruce sin el obelisco visible.
- Revisión visual en Play del inicio, bajada y farol central; no se guardaron cámaras temporales ni cambios de iluminación de las capturas.
- Los 51 WAV/PNG originales protegidos conservan sus hashes frente al registro anterior.
- No se ejecutó la suite completa ni se midió rendimiento. Esto es una base de nivel para iterar el kit, no una optimización medida.
- Sin commit, push ni check-in. Respaldo de la escena, mallas y scripts anteriores en crater-chatgpt/Logs/InicioElevado20261026/Antes; resultados de pruebas en esa misma carpeta de Logs.
