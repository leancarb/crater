# CRÁTER — arte restaurado y mejoras conservadas

> Documento histórico. Para el diseño vigente del 6/10/2026, consultar [EstadoActualCrater.md](EstadoActualCrater.md) y [RevisionContemplativa.md](RevisionContemplativa.md). Las capturas de esta etapa pueden representar cambios posteriormente revertidos.


5 de octubre de 2026. El usuario pidió revertir el arte nuevo conservando las demás mejoras.

## Arte vigente

Se restauran los modelos originales de linterna, ancla, reja y conjunto central, la paleta anterior, los pictogramas SOL/LUNA/luz blanca y los marcos cerrados. Se retiran las costillas y los acentos de cobre añadidos. La linterna vuelve a su escala y posición originales.

Se conservan la plaza hundida circular con seis escalones anulares continuos, los murales separados en ambas alas y la limitación de intensidad del haz blanco ante superficies cercanas. El resto de las correcciones de la revisión integral permanece. El respaldo `Backups/ArteV2_revertido.zip` conserva el generador del arte descartado; las capturas en `Docs/Previews/RedisenioV2` son históricas y no representan el arte vigente.

## Joystick

Compatible con mandos reconocidos como Gamepad por Unity Input System; los nombres Xbox tienen equivalencia PlayStation por posición.

| Acción | Mando |
|---|---|
| Movimiento / mirada | Stick izquierdo / derecho |
| Correr | Mantener L3 |
| Linterna | RB / R1 |
| SOL | X / cuadrado |
| LUNA | Y / triángulo |
| Luz blanca | B / círculo |
| Saltar secuencia | A / cruz |
| Pausar / seguir | Start / Options |
| Seguir desde pausa | B / círculo |
| Reiniciar desde pausa | Y / triángulo |
| Salir desde pausa | X / cuadrado |
| Alternar ayuda escrita desde pausa | Cruceta arriba |

Teclado y mouse mantienen sus controles. Las acciones del menú se leen sólo durante la pausa. Se añadieron controles de mando al texto de pausa.

## Cinemáticas y sonido

Al volver a la rotonda con cada sello, la cámara gira hacia su hilo y muestra el encendido progresivo. Con ambos sellos muestra el eclipse central, el de la puerta y la apertura. Al terminar devuelve el control y conserva la dirección final. Puede pausarse y saltarse con el mando; una interrupción del componente restaura el control y completa el progreso pendiente.

El siseo de las rejas se repetía mientras `Activo` permanecía verdadero, incluso indefinidamente en sellos permanentes. Ahora cada activación produce una transición de 0,7 segundos con salida suave. La fuente es más baja y su alcance baja de 28 a 9 metros. Mantener la luz sobre una reja ya disuelta no reinicia el sonido. También se corrigió la reinicialización del MaterialPropertyBlock de rejas y linterna después de una recarga de scripts.

## Validación tras restaurar el arte

La reconstrucción y la compilación de scripts finalizaron sin errores. Las capturas actuales están en `Assets/Art/Previews/Unity`; se verificaron la rotonda y las rejas originales.

EditMode: 19/19 aprobadas (`Logs/arte-restaurado-editmode.json`). PlayMode: 10/10 aprobadas en 177,19 segundos (`Logs/arte-restaurado-playmode.json`). Incluyen recorrido completo hasta los créditos, controles de mando y pausa, devolución del control después de las cinemáticas, siseo breve y piso circular en todas las direcciones.

Los controles de mando se validan con dispositivos simulados del Input System.

La demo Windows se recompiló correctamente y arrancó con Direct3D 11 sin errores ni excepciones en el log de comprobación (`Logs/arte-restaurado-player.log`). Ejecutable actualizado: `Builds/Windows/CRATER.exe`. El recorrido completo se probó en el Editor.

Corrección posterior: el piso circular y los escalones reciben el acabado low poly del nivel (normales planas, textura de facetas y relieve leve en las superficies amplias). La malla de colisión conserva alturas regulares para caminar.
Validación del piso facetado: reconstrucción y captura revisadas; prueba PlayMode del piso circular aprobada (`Logs/rotonda-facetada-playmode.json`). Demo Windows recompilada (`Logs/rotonda-facetada-build.json`).

Los trazados de ambas alas y del Cruce fueron posteriormente reemplazados; `Docs/AlasYCruceNuevo.md` y el GDD contienen su especificación vigente. El obelisco vuelve al centro, conservando su modelo original.
