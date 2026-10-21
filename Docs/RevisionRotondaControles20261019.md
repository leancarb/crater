# Rotonda, carteles, controles y escalera — 19/10/2026

Proyecto aplicado: `C:/Users/carbo/Desktop/crater`. Respaldo previo: `Backups/RotondaControlesAntes20261019/`.

## Cambios

- Teclado y teclado numérico: **1 = luz normal, 2 = SOL, 3 = LUNA**. F/RB sigue encendiendo y apagando la linterna. Los botones del joystick conservan su asignación. Fuente: `Assets/Scripts/Nucleo/EntradaCrater.cs:65`. HUD y pausa actualizados en `Assets/Scripts/Interfaz/InterfazCrater.cs:311` y `:452`. “Cruceta” cambia a “flechitas” en la indicación del slider (`:352`).
- Retirados los dos paneles de retorno a luz blanca de las alas. Los paneles de SOL/LUNA ahora dibujan 2 y 3, incluida la nueva figura del número 3. Fuente: `Assets/Editor/ConstructorCrater.Murales.cs`.
- Cartel físico WASD/mouse a 6 m hacia adelante y 2,8 m a la derecha del punto inicial del exterior, apoyado a la altura del terreno, con dos soportes y tinta Unlit para conservar contraste a contraluz; retirado su ejemplar del Umbral. Fuente: `Assets/Editor/ConstructorCrater.RotondaRevision.cs:63` y llamada en ConstruirCapilla.
- Retirados todos los diez módulos decorativos de la segunda captura (doble aro, paño y columnas laterales) del Umbral y del Cruce. La fuente ya no los instancia: `Assets/Editor/ConstructorCrater.Nivel.cs:534`.
- Obelisco: fuste ahusado, remate piramidal de cuatro caras, altura total 6,83 m desde la base hundida (cima en y=5,75). SOL/LUNA y los dos eclipses quedan dentro de cavidades reales, detrás de la superficie exterior; no son placas flotantes. Se conservan sus estados y funciones. Fuente: `Assets/Editor/ConstructorCrater.RotondaRevision.cs:13`, `ConstructorCrater.Alas.cs`, MapaYObelisco.
- Seis columnas a radio 8,8 m, fuera de la escalinata cuyo borde exterior está a 8,125 m; base en y=0, techo en y=7. Mantienen simetría y eje de paso libre. Se retiran los pequeños soles/lunas de sus caras; el progreso enciende aros circulares al ras del piso y focos ascendentes, uno por columna, con sus notas y pausa originales. Fuente: `Assets/Editor/ConstructorCrater.Alas.cs`, `ConstructorCrater.RotondaRevision.cs:9` y AroDelPiso. Actualizado el radio del validador y del test de circulación.
- Apertura del cráter: sin encuadre automático ni desplazamiento de cámara. Cuatro focos integrados en las piedras del borde y un relleno ambiental más claro durante la totalidad iluminan la abertura; aparecen gradualmente al abrirse y se apagan antes de la revelación. Fuente: Assets/Editor/ConstructorCrater.LuzApertura.cs y PrologoCapilla.AplicarRevelado.
- Escalera: sus ocho dibujos tienen materiales exclusivos con emisión moderada (máximo 0,45) y focos suaves hacia los peldaños (20 de intensidad, 4,7 m de alcance, cono 110°, interior 45°, sin sombras). La emisión visible y la luz sobre el piso se ajustan por separado. Retirado el foco suelto `Luz_Vela_Entrada (1)` del centro de la bajada y la generación de `Luz_Plaza`. Se conservan las cuatro velas reales de la entrada. Fuente: `Assets/Editor/ConstructorCrater.LuzEscalera.cs:8`, llamadas en `ConstructorCrater.Tallados.cs:200`.

## Aplicación y mantenimiento

Se actualizaron las fuentes y las piezas de la escena mediante Unity, sin editar YAML. Los menús `Crater/Actualizar rotonda y carteles` y `Crater/Actualizar luz de escalera` permiten repetir la actualización localizada. La reconstrucción completa conserva estas decisiones. La actualización localizada conserva el resto de la escena restaurada; también limpia símbolos antiguos que habían quedado separados de sus padres al ampliar el nivel.

No se modificaron WAV ni grabaciones, no se generaron builds y no se hizo check-in en Unity Version Control. Para distribuir estos cambios hay que volver a construir la demo.

## Validación

Resultados JSON y capturas en `Logs/Rotonda20261019/`. Pruebas nuevas en `Assets/Tests/PlayMode/RotondaCartelesTests.cs`: teclas 1/2/3 y numérico, carteles/geometría/ausencia de símbolos antiguos, iluminación del borde y cámara libre durante la apertura, luminarias de escalera. También se ejecutaron las tres pruebas de secuencia de sellos y la de circulación/simetría de columnas.

Resultado: 9/9 pruebas PlayMode seleccionadas aprobadas (4 de esta revisión, 3 de secuencia de sellos, 1 de circulación de columnas, 1 de audio de apertura). EditMode completo: 30/31; permanece el fallo previo LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar, que busca Luz_Pasaje ausente. Validador: 0 problemas. 43 archivos protegidos de audio sin cambios SHA-256. Capturas revisadas de rotonda encendida, descenso y dibujo de escalera cercano; en la vista cercana sin linterna se conserva el color del símbolo, sin blanco recortado. No se ejecutó toda la suite PlayMode ni se exportó un build.
Decisión final: sin desplazamientos de cámara añadidos. El cartel lateral se actualiza con Crater/Actualizar cartel inicial y las luces del borde con Crater/Actualizar iluminación de apertura. Los JSON TestsRevision.json y TestsAudioApertura.json corresponden a esta decisión final; la cámara se mantiene libre. Las capturas de encuadres elevados en Logs son exploraciones descartadas; la apertura vigente se documenta en Apertura_Luz_Camara_Libre.png.

## Corrección de apertura sin encoger (19/10)

El efecto circular provenía de reducir X/Z de Tapa_Pozo hasta cero. PrologoCapilla ahora guarda la posición cerrada y desplaza la tapa, conservando su escala, bajo el terreno hacia el fondo opuesto a la boca. Baja suavemente hasta 1 m durante el primer tramo para ocultarse bajo el suelo existente, sin arrastrar un disco visible por la superficie. Conserva los tiempos, borde, luces, cámara libre y audio. Al volver a cero recupera posición y collider cerrado para el epílogo. No requiere reconstruir la escena: se aplica desde el script.

Respaldo: Backups/AperturaSinEncoger20261019/PrologoCapilla.cs. Regresión: LaTapaSeRetiraSinAchicarseYRecuperaSuCierre falló antes por escala y pasó después; se verifican tamaño constante durante apertura, retirada, ocultación y restauración del cierre. Capturas Tapa_*.png en Logs/Rotonda20261019. No se modificaron WAV ni se hizo check-in.
Validación final de esta corrección: 5/5 RotondaCartelesTests aprobados (19 s), compilación sin errores; resultados en Logs/Rotonda20261019/TestsTapaRetirada.json. Capturas intermedias revisadas.

## Reja final del Cruce más ancha (19/10)

ConstructorCrater.Alas.cs: el paso Piel_N_Salida cambia de -2..2 a -4..4 y Reja_N_Salida de escala X 4/6 a 8/6. Ancho nominal 4→8 m de diseño, 5→10 m con EspacioCrater.Escala=1,25. Se ajustaron en Unity la instancia y las tres piezas del tabique (laterales y dintel); oclusión recalculada. Anclas, posiciones, puentes y retenciones conservados. Respaldo de fuente y escena en Backups/RejaCruceAntes20261019. Sin build ni check-in.
Validación de reja: compilación sin errores, validador0; PlayMode CadaParejaExigeApuntarPorSeparadoYPermiteCruzarConUnPulsoSolar aprobado (23,75 s). Captura desde isla segura revisada: ambas anclas visibles entre los barrotes. Evidencia TestsCruceRejaAncha.json y Cruce_Reja_Ancha.png.
