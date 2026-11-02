# Ayudas de lectura — 21/10/2026

Implementación de la solicitud adjunta, sin commits ni push. HEAD conservado: 155ff8a (main local; no se reseteó a 8425bdc). Proyecto abierto y validado: C:/Users/carbo/Desktop/crater. Copia Git: C:/Users/carbo/Desktop/crater-chatgpt.

## Cambios de esta solicitud

- **InterfazCrater.cs:** canales HUD_CRATER/Orientacion y Momento con cola compartida. Orientación: itálica hueso cálido, centro ligeramente a la izquierda, entrada/salida de 0,35 s y permanencia de 4 s; respeta H salvo paraTodos. Momento: fuente CCElephantmen, espaciado ancho, friso escalonado fino, entrada/salida de 1,5 s y permanencia de 3 s; siempre visible, una vez por texto y partida. Prompt conserva los controles. Pausa: «Fragmentos de la corona · n/6».
- **FlujoJuegoCrater.cs:** siete indicaciones migradas a Orientación, constantes comentadas. En Cruce, Momento «El sol y la luna, juntos.» precede a «Ahora las dos luces juntas.» mediante la cola. EntrarCresta inicia GuiaCresta.
- **GuiaCresta.cs** (nuevo): contador mientras el farol está encendido, reinicio al apagarlo, umbrales 30/60/90 s. Dos refuerzos de 0 a 6 en 2 s, alcance 4 m, sin sombras; aviso final forzado con EntradaCrater.BotonLinterna. Deshabilitar apaga los refuerzos. **EclipseFinalController.cs** lo desactiva al abrir el techo.
- **MateriaHueca.cs:** orientación «Primero la luna. Después el sol.» ante el ancla solar bloqueada del Cruce, una vez por partida; no cambia la lógica de las rejas.
- **ConstructorCrater.Nivel.cs:** crea y conecta guía, luces, flujo, eclipse e interfaz de las rejas del Cruce.
- **ConstructorCrater.Murales.cs:** PanelEnLaOscuridad usa farol colgante a -70°, sin cono, y pequeño símbolo power compartido. PanelEncandilado conserva su dibujo.
- **ConstructorCrater.Placas.cs:** conserva la composición del panel Ensenanza_Cresta_Espera; agrega soporte de la nueva pista E2, doble esquina y altura 1,8 m. La placa nueva no tiene collider.
- **ConstructorCrater.Detalles.cs:** Pista_Trampilla_E2, orientada al oeste sobre la pared interior este, 2,4 × 1,6 m. Ajuste tras ampliación del nivel para conservar las medidas de los cuatro adornos y el alcance de los spots de placa; reubica refuerzos frente a los tallados actuales Apagar_Izq_84 y Apagar_Der_91. Conserva los colliders anteriores en coordenadas de mundo.
- **ConstructorCrater.CorreccionDocente.cs:** clasifica la nueva pista como instrucción.
- **ColeccionPiedras.cs:** interacción «Guardar fragmento», con EntradaCrater.BotonInteractuar. Sin cambios de recolección.
- **ConstructorCrater.cs / GeneradorAudioCrater.cs:** ReconstruirTodo carga los 43 clips existentes mediante CargarExistentes, con prioridad a Grabado y error si falta alguno. No sintetiza ni guarda WAV. Esta ruta permite reconstruir respetando la prohibición de regenerar audio.
- **ValidarProyectoCrater.cs:** verifica guía, referencias del flujo y eclipse, dos luces y 16 placas de instrucción.
- **CanalesLecturaPlayTests.cs** (nuevo): aviso forzado con H apagado; reinicio al apagar el farol a los 10 s; orden Momento → Orientación y ausencia de solapamiento. Los tres verifican movimiento libre, sin bloqueo ni cambio de timeScale por las ayudas.
- **PlacasInstruccionTests.cs / PlacasLecturaPlayTests.cs:** cuenta y altura de la nueva placa; conserva cinco secuencias de tres pasos y el pictograma específico de espera.
- **CraterRevisionTests.cs:** validación de clips existentes sin crear WAV temporales ni sintetizar audio.
- Escena, prefabs y mallas sincronizados desde la reconstrucción por código. Los materiales y demás archivos del estado Git incluyen también el trabajo previo de separación entre instrucción y narrativa; no son todos cambios nuevos de esta solicitud.

## Medidas verificadas en la escena reconstruida

Trampilla_E: centro (33,750; -0,450; 41,571), tamaño (7,504; 1,402; 5,683) m.
Cuatro tiras: ancho 0,05 m, altura 0,02 m, margen 0,06 m; dos de 5,853 m y dos de 7,674 m. Sin colliders, sin tapar la rampa.
Pista_Trampilla_E2: centro (48,550; 1,800; 41,625) m.
Comparación de 637 colliders (ruta, tipo, estado, trigger y bounds de mundo a cuatro decimales): cero diferencias.
Hashes SHA-256 de los 51 archivos originales protegidos (8 PNG y 43 WAV): cero diferencias.

## Validación

- ReconstruirTodo ejecutado mediante la ruta sin generación de audio.
- Compilación final: sin errores. Validador: **0 problemas**.
- EditMode: **33/33 aprobados**.
- PlayMode focalizado: **8/8 aprobados**: CanalesLecturaPlayTests 3, PlacasLecturaPlayTests 1 y CorreccionDocenteCompletaTests 4.
- No se volvió a ejecutar la suite completa de PlayMode en esta solicitud. Los fallos ajenos de la revisión anterior siguen documentados en PlacasInstruccion20261021.md; estos resultados focalizados no implican que toda esa suite esté aprobada.
- Revisión visual de ambos canales, friso conectado, pictograma de apagado, pista y contorno de trampilla. Capturas y JSON de pruebas en Logs/AyudasLectura (carpeta ignorada por Git).
- Sin cambios intencionales de controles, filtros, velocidades, retenciones, colliders, orden de flujo o cámaras. Sin check-in de Unity Version Control.

## Estado de archivos

El archivo `AyudasLectura20261021-git-status.txt` contiene la salida completa de `git status --short`, incluidos los cambios previos que ya estaban en el working tree. No se hizo stage, commit ni push.
