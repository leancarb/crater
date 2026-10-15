# Optimización de rendimiento — 8/10/2026

Primera pasada medida en el Editor, solicitada después de observar 150–200 FPS.
Se conserva el arte restaurado, el facetado, la iluminación, la resolución, los
controles, las distancias y los tiempos de las mecánicas. No se generó un EXE.

## Cambios vigentes

- `ConstructorCrater.Rendimiento.cs` prepara oclusión estática: paredes y pisos
  opacos, fijos y con collider permiten descartar objetos tapados. Puertas,
  rejas, puentes y hojas móviles del techo no son oclusores. Umbral de agujeros
  0,25 m, oclusores 4 m y backface threshold 100. La reconstrucción guarda la
  escena, recalcula `Assets/Scenes/CraterVerticalSlice/OcclusionCullingData.asset`
  y vuelve a guardarla. Los datos no deben reutilizarse después de modificar
  geometría sin reconstruir.
- Los tres discos del cielo que siguen a la cámara quedan excluidos de la
  oclusión dinámica horneada: de lo contrario, la corona desaparecía por el
  óculo aunque hubiera línea de visión. Conservan el test de profundidad,
  por lo que paredes y techo siguen tapándolos físicamente. Se comprobó con
  capturas de la misma posición y la prueba de eclipse desde tres posiciones.
- Las barras de cada reja comparten una malla: conservan sus vértices, UV,
  normales, material, collider, marco y punto de impacto. Se aplican la misma
  carga y disolución a todo el conjunto. Las piezas de cada mata de paja y cada
  cardón también se agrupan por planta. **Se facetan antes de agrupar** para
  conservar las formas y tonos originales. Los colliders del cardón permanecen
  en sus objetos originales. No se une todo el paisaje en una única malla;
  cada planta sigue teniendo un volumen de descarte pequeño.
- Anclas, rejas, puentes, testigos de sellos, conjunto opcional y veta evitan
  reenviar propiedades visuales cuando su valor no cambió. Carga, retención,
  pulsos, disolución, vibración, colliders y sonido siguen avanzando. Las anclas
  tampoco reasignan la escala de los aros cuando terminó la vibración.
- El HUD recompone las etiquetas de filtros al desbloquear/equipar y al
  reactivarse, mediante los eventos existentes. La mira y la barra de carga
  siguen respondiendo cada fotograma.

Se mantienen HDR, bloom, MSAA 4, sombras PC 4096, cuatro cascadas y distancia
220 m. No se redujeron las sombras reservadas de velas/LUNA. No se cambiaron
WAV, huellas de audio ni paquetes. GPU Resident Drawer y su oclusión se probaron
y **se descartaron**: empeoraban la CPU en esta escena. Sus opciones y las
variantes BRG quedaron como antes.

## Mediciones

Unity 6000.3.23f1, URP 17.3, Editor Play, Game View 2560×1248, Direct3D12,
RTX 5070 y Ryzen 9 5900X. vSync 0 y targetFrameRate -1. Vistas fijas desde
posiciones del jugador; prólogo detenido, cámara/movimiento detenidos y misma
calidad. En rotonda, linterna blanca encendida. Se descartan 120 fotogramas
iniciales: baseline 360 muestras y versión final 1.200. Se informan medianas.

| Vista / contador | Antes | Después |
|---|---:|---:|
| Exterior: hilo principal | 6,408 ms | 5,314 ms |
| Exterior: PlayerLoop | 3,783 ms | 2,884 ms |
| Exterior: hilo de render | 2,843 ms | 2,123 ms |
| Exterior: GPU | 0,995 ms | 0,961 ms |
| Exterior: llamadas de dibujo | 2.798 | 1.761 |
| Exterior: cambios SetPass | 210 | 105 |
| Rotonda: hilo principal | 5,486 ms | 5,315 ms |
| Rotonda: PlayerLoop | 3,060 ms | 2,936 ms |
| Rotonda: GPU | 1,361 ms | 1,368 ms |
| Rotonda: llamadas de dibujo | 1.668 | 1.306 |

El exterior reduce aproximadamente 17 % la mediana del hilo principal y 37 %
las llamadas de dibujo. La rotonda reduce 22 % las llamadas, pero sólo cerca
de 3 % el tiempo principal: esa diferencia pequeña debe considerarse junto
a la variación del Editor. La GPU de rotonda quedó prácticamente igual.

Después de corregir el descarte del cielo, una repetición de 1.200 muestras
por vista dio 4,940 ms / 1.751 llamadas en exterior y 5,191 ms / 1.302 llamadas
en rotonda (`Logs/rendimiento_confirmacion_*.txt`). Confirma la reducción de
llamadas y muestra la variación del Editor entre sesiones: 17–23 % menos
tiempo principal en exterior, 3–5 % en rotonda respecto de ese baseline.
No se atribuye la diferencia entre repeticiones a la corrección del cielo.

Una comparación de oclusión activada/desactivada dentro de la misma sesión,
1.200 muestras por estado, dio 1.329/1.669 llamadas en rotonda, pero tiempos
principales casi iguales (5,657/5,682 ms). Menos llamadas no implica por sí
solo un aumento proporcional de FPS.

Archivos: `Logs/rendimiento_antes_*.txt`, `rendimiento_final_*.txt`,
`rendimiento_occlusion_AB_*.txt` y `rendimiento_resumen.json`. Los contadores
incluyen trabajo del Editor. `EditorLoop` dio cero y no se usa como prueba de
coste nulo. El intento de medir asignaciones mediante
`GC.GetAllocatedBytesForCurrentThread` dio cero y no se usa como evidencia.
El contador GC por fotograma sigue cerca de 50 KB e incluye Editor/puente;
esta pasada **no demuestra** haber eliminado el GC de todo el juego.

## Validación

Compilación, reconstrucción, referencias y alcance del haz verificados con el
flujo existente. La batería EditMode completa pasó sus 23 pruebas. En Play se
ejecutaron las suites de exploración opcional (5), ambos órdenes completos de
las alas y final sin piezas (2), las cuatro recuperaciones (1), controles (1),
columnas y convivencia de secuencias (3), respuesta de ancla/tallado (1),
rejas sin siseo repetido (1) y eclipse/audio de apertura (3): **17 pruebas
aprobadas, sin fallos en la versión corregida**. Los resultados
quedan en `Logs/rendimiento_*.json`. Los recorridos completos se ejecutaron
antes de la última corrección, que sólo excluye los discos del cielo del
descarte dinámico; después se repitió la suite de eclipse/audio.

La primera ejecución de esa suite detectó la corona ausente. Se conserva el
fallo en `Logs/rendimiento_eclipse_antes_correccion.json`, junto con capturas
de oclusión activada/desactivada y corregida. Esta comparación no se usa
como evidencia de una mejora de rendimiento: sirve para detectar y corregir
una regresión visual.

Los 43 WAV y el archivo de huellas conservan sus SHA-256 iniciales (44
archivos comprobados). Los ajustes de render y ProjectSettings coinciden
con el respaldo previo. Registro: `Logs/rendimiento_audio_preservado.json`.

Dos reconstrucciones consecutivas dejan los mismos 2.827 objetos, 1.411
renderers, 114 luces, 612 colliders, 13 anclas y 9 rejas, con cero errores
de validación. Los tres renderers del cielo conservan su excepción de
oclusión. Registros: `Logs/rendimiento_estructura_primera.json` y
`rendimiento_estructura_segunda.json`.

El HUD sigue los desbloqueos, selección SOL, regreso a blanca y
desactivar/reactivar: `Logs/rendimiento_hud_validacion.txt`.
Capturas en `Docs/Previews/Rendimiento`: exterior, rotonda y reja LUNA en
reposo, carga y disolución. La comparación de oclusión en rotonda y reja fue
idéntica; en exterior hubo diferencias máximas de 2/255 en pocos píxeles.
La comparación del exterior antes/después de agrupar vegetación no mostró
píxeles con diferencia superior a 24/255. Son controles de esas vistas;
no prueban ausencia de errores desde todo encuadre imaginable.

## Repetir y pendientes

En Play, mantener el mismo encuadre y usar **Crater > Medir rendimiento de la
vista actual (Play)**. La sonda termina sola y guarda un TXT con cámara,
resolución, medianas y p90. No se incluye en el ejecutable.

Queda medir un build Windows normal a resolución fija, con Unity cerrado,
en varios sectores y durante el recorrido, incluidas oscilaciones y percentiles.
Estas cifras no son FPS del EXE ni prometen una mejora igual en otra máquina.
No se hizo una auditoría completa de memoria ni un perfil del reproductor.
La escucha física de la interferencia de audio informada previamente sigue
pendiente; esta optimización no se presenta como su solución. La comodidad,
comprensión y disfrute requieren sesiones con jugadores.
