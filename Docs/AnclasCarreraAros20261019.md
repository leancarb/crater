# Anclas, carrera, aros y cierre del valle — 19/10/2026

Solicitud: restaurar Sol=1 y Luna=2, Shift para correr, orientar parejas solares a ±45°, trasladar al techo las anclas del segundo puente del Cruce y exigir carrera; aros de progreso completos en piso/techo del obelisco; ocultar bordes exteriores con rocas.

Implementación:
- EntradaCrater.cs: teclas 1/2 y teclado numérico; Shift izquierdo/derecho además de R2/RT. B conserva retorno manual en mando; teclado vuelve a blanca al agotarse los tres segundos. InterfazCrater.cs actualiza HUD y pausa.
- ConstructorCrater.AnclasCarrera.cs: cada pareja de los cinco puentes tiene orientaciones horizontales ±45° respecto de su aproximación. Las del segundo Cruce cuelgan a 5,95 m y miran hacia abajo; retención 1,9 s. Las del segundo puente oeste conservan ubicación colgante y adoptan el mismo criterio de orientación.
- ObeliscoDelEclipse.cs: arosCompletos sólo se activa cuando las seis columnas alcanzaron al menos 0,99 de su transición. Un TestigoDeSello comparte fundido de piso y techo.
- Aro inferior sobre el sexto escalón a y=-1,055 m; radio 4,75 m y ancho 0,12 m. Aro superior a 6,835 m con carcasa anular opaca a 6,90 m; mantiene libre el óculo y no sobresale del techo.
- Cierre_Rocoso_Valle: 42 masas facetadas con MeshCollider, material existente CapillaTierra, semilla fija; laterales y extremos superpuestos. El cierre posterior se aleja del óculo (z local=-48).
- ConstructorCrater.CorreccionDocente.cs integra los cambios tanto en actualización localizada como en reconstrucción completa. Validador y pruebas actualizados.

Respaldo previo: Backups/AnclasCarreraAros20261019. Evidencia: Logs/AnclasCarreraAros20261019.

Validación inicial: nuevas pruebas 3/3 (incluye caminar falla / Shift cruza usando el input real), Ritmo 3/3, Rotonda 5/5. Recorrido completo 1/1 (142,93 s), Corrección Docente 4/4: 16/16 PlayMode seleccionados. EditMode completo 30/31, mismo fallo previo LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar por Luz_Pasaje ausente. Capturas revisadas de frente, laterales y atrás del inicio, anclas colgantes y aros completos. Altura del aro inferior corregida para superar Anillo_Piso y comprobada con una prueba adicional.

No se hizo check-in, no se generaron builds y no se modificó audio.

## Aro en el borde de la abertura

Ajuste posterior solicitado: abertura del techo reducida de radio 6,4 a 3,8 m de diseño (radio final 4,75 m; diámetro 9,5 m). El borde interior del aro superior coincide con el borde físico de la abertura. Canto opaco continuo entre y=6,835 y 7 m y carcasa superior anular; emisión bajo el techo. Se conserva el aro inferior y el encendido al completar seis columnas. Cambios en ConstructorCrater.Exploracion.cs y ConstructorCrater.AnclasCarrera.cs; prueba PlayMode de coincidencia geométrica y encendido aprobada 1/1, captura 07_Aro_Borde_Abertura revisada. Respaldo: Backups/BordeAberturaObelisco20261019.
