# Cámara de joystick y segundo puente del Cruce

El stick derecho tenía escala 32: con sensibilidad base 2,2 y opción 1, el máximo era 70,4 grados por segundo. El barrido entre las anclas del techo consumía demasiado tiempo del filtro sostenido de 3 segundos.

`EntradaCrater.cs` usa escala 72 y curva radial `magnitud^1,5`. El máximo pasa a 158,4 grados por segundo. A una magnitud procesada de 0,2, la velocidad queda prácticamente igual: 14,08 frente a 14,17 grados por segundo. Se conserva la zona muerta del Input System, el mouse y el multiplicador de sensibilidad de las opciones.

No se modifica la retención de las anclas (1,9 segundos), el filtro (3 segundos), la disposición de las anclas ni las velocidades de caminar/correr (4/5,6 m/s).

Las pruebas usan GamepadState y los métodos reales de cámara y movimiento del jugador: apuntan desde la orilla, sostienen RB para cargar ambas anclas y mueven el stick izquierdo mientras bajan la mirada; RT permite completar el cruce. Se comprueba por separado que caminar no alcanza. Las opciones temporales y dispositivos se restauran al terminar. La prueba automatizada no sustituye la evaluación de comodidad con un control físico.

Verificación conjunta con el encuadre: EditMode 36/36 y PlayMode afectado 12/12; validador 0 errores. Resultados en `Docs/Testing/Revision20261105/JoystickEncuadre-EditMode.json` y `JoystickEncuadre-Play.json`.
