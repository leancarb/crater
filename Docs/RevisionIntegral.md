# CRÁTER — revisión integral

> Documento histórico. Para el diseño vigente del 6/10/2026, consultar [EstadoActualCrater.md](EstadoActualCrater.md) y [RevisionContemplativa.md](RevisionContemplativa.md). Las capturas de esta etapa pueden representar cambios posteriormente revertidos.


Fecha: 5 de octubre de 2026. Proyecto independiente `crater-chatgpt`, Unity 6000.3.23f1.

## Criterio de reconstrucción

El GDD es un documento vivo. Se contrastaron sus versiones con la escena, los generadores, los scripts, las notas de continuidad y la transcripción aportada de [Spatial Communication in Level Design, Peter Field](https://www.youtube.com/watch?v=AKeUZVikPV8). Se conservó la identidad contemplativa andina, la exploración sin salto, los filtros SOL/LUNA, las rutas que pueden hacerse en cualquier orden y el regreso final al cráter desaparecido.

No se restauraron propuestas expresamente descartadas en las notas: mirilla, portal que encuadra el obelisco, luces sobre anclas colgantes, surcos de orientación, ventanas del retorno, mojones alrededor del cráter y zigzag previo a Cresta.

Antes de modificar el proyecto se guardó `Backups/CRATER_antes_revision_integral.zip`. Las capturas de referencia anteriores están en `Docs/Previews/AntesRevision`.

## Game design y comunicación espacial

- La capilla sigue siendo un destino visible desde el valle. El relieve alterna ocultamiento y revelación; el eclipse progresa durante la caminata. Esto aplica las ideas del video sobre objetivo visible (2:25–4:16), vistas que aportan información (7:22–8:21), reaparición del destino (31:42–33:06) y topografía que orienta la mirada (35:18–36:55).
- Los sellos revelan sus hilos, el obelisco y la salida mediante una secuencia dentro del mundo. El jugador conserva movimiento, mirada, linterna y pausa. Desactivar el componente durante la secuencia completa los cambios pendientes para evitar un bloqueo.
- La ayuda escrita durante la partida está desactivada por defecto. H en pausa permite habilitarla y guardar la preferencia. Los controles se consultan en la pausa.
- Se mantienen los murales y los pictogramas como enseñanza contextual, los mecanismos de ancla permanente y las cuatro grutas de recuperación mediante escaleras.

## Level design y arte

- Las estelas SOL/LUNA se colocaron junto al primer encuentro con cada filtro y se orientaron hacia la entrada. El recorrido automatizado detectó que una base invadía el acceso oriental; se trasladaron ambas estelas fuera de ese paso.
- Las escaleras de recuperación reciben luz cálida en su pie para hacer legible la salida sin una flecha escrita.
- Se redujeron bloom, viñeta, grano y contraste para favorecer la lectura del terreno y los relieves. Se conservó la paleta existente y el arte facetado.
- La comprobación de tallados ahora sincroniza la física y usa la forma real de los colliders en vez de sus cajas envolventes. Se eliminaron avisos falsos de pinturas incrustadas en paredes rotadas.
- La captura de previews prepara el cielo mediante su API pública; ya no ejecuta mensajes de ciclo de vida sobre componentes inactivos.

Las imágenes están generadas por cámaras de Unity, no son renders conceptuales. `Assets/Art/Previews/Unity` contiene vistas fijas; `Docs/Previews/Recorrido` registra estados del recorrido de prueba. Las vistas fijas no representan por sí solas el estado de activación durante una partida.

## Audio

- Se separaron pasos de piedra y tierra. Los colliders de tierra identifican su superficie y el jugador selecciona el banco correspondiente.
- Se suavizó la síntesis de pasos y se ajustaron los drones a ciclos continuos. El análisis de 34 WAV registró 115,7 segundos de audio mono, un pico máximo de 0,7 y ausencia de saturación digital (`Logs/revision-audio.json`). Esto no sustituye una evaluación auditiva humana.
- La campana pertenece espacialmente a la capilla. Los tonos de los hilos se emiten desde sus tallados.
- El generador protege reemplazos manuales mediante huellas SHA-256. También prioriza grabaciones de `Assets/Audio/Grabado` con el mismo nombre. La reconstrucción ya no sobrescribe automáticamente una grabación personalizada.

El material sonoro continúa siendo síntesis procedural. No se incorporaron grabaciones ni una nueva banda sonora de terceros.

## Técnica y reconstrucción

- `PresupuestoSombras` limita a tres las luces locales elegibles que proyectan sombras cerca del jugador, con estabilidad entre actualizaciones. La luz direccional y la linterna conservan su comportamiento. El atlas mantiene 2048 y las luces locales usan resolución 256.
- El perfil de postprocesado se actualiza conservando su GUID y sus componentes existentes.
- Se instaló el puente local `com.unity.pipeline 0.8.0-exp.1` con autorización del usuario y se comprobó la conexión con el Editor abierto.

No se afirma una mejora medida de FPS o consumo de audio: falta un perfil comparativo de rendimiento en el hardware objetivo.

## Validación

La reconstrucción incluye validación de referencias y escenas. La primera revisión detectó un bloqueo real del recorrido oriental, corregido moviendo las estelas. Se conservan los resultados de esa ejecución como evidencia.

- EditMode: 19 pruebas aprobadas, incluidas selección de sombras y protección de audio personalizado. Resultado: `Logs/revision-editmode.json`.
- PlayMode: 5 de 5 aprobadas en 168,62 segundos, incluido el recorrido completo hasta los créditos, eclipse, reflejos de Cresta, recuperación de las cuatro caídas y libertad de cámara durante los sellos. Resultado final: `Logs/revision-playmode-final.json`. La ejecución anterior con el bloqueo queda en `Logs/revision-playmode-antes-correccion.json`.
- Windows x64: compilación release aprobada, backend Mono, versión 0.3.0, escena `CraterVerticalSlice`, tamaño informado 116 MB. Ejecutable: `Builds/Windows/CRATER.exe`. El primer build emitió un aviso de cambios sin compilar; después de sincronizar la importación se repitió el build y ese aviso desapareció. Evidencia final: `Logs/revision-build-final-consola.json`.
- Arranque independiente: el ejecutable inicializó motor, Input System y PhysX sin excepciones registradas en modo sin gráficos (`Logs/revision-player-windows.log`). También arrancó con Direct3D 11 sobre NVIDIA RTX 5070 sin excepciones ni fallos de shader registrados (`Logs/revision-player-grafico.log`). Se detuvieron exclusivamente los procesos de prueba al terminar; no se interpreta ese cierre como un código de salida normal. La comprobación del recorrido y las imágenes corresponde a PlayMode en el Editor; estas pruebas de arranque no sustituyen jugar el ejecutable completo.

El único aviso del build final corresponde a la ausencia de configuración de Pipeline runtime: confirma que el puente de automatización queda desactivado en el ejecutable. El puente del Editor continúa disponible.

Quedan dos avisos conocidos por ausencia de los logos oficiales FADU y Campos; se utiliza el texto de respaldo. No se inventaron esos logos. Las pruebas automatizadas verifican funcionamiento y transitabilidad; la comprensión de un jugador nuevo requiere una sesión de juego humana.
