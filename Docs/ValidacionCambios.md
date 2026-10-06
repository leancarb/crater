# CRÁTER — cambios y validación de esta copia

2026-10-05 · Unity 6000.3.23f1 · proyecto independiente.

## Implementación

- Inicio exterior en SpawnValle, lomas con ocultamiento y reaparición de la capilla, vegetación y apachetas; límites del valle y horizonte fuera del camino.
- Eclipse durante la caminata, avance automático en 80 s y totalidad en el mirador. La apertura bloquea caminar y conserva el control de la mirada.
- Valle girado 180°, boca horizontal con cordones bajos, escalera sin paredes altas sobre el terreno y dos pares de mojones con velas. Sin senda flotante.
- Plaza hundida 1,2 m, filtros dentro de las primeras salas, óculos y haces sobre los sellos; luz bajo la puerta. Pisos de vuelta conectados al anillo y luces con sombras.
- Puerta SOL permanente de una ancla; puentes temporales de dos anclas. Rejas más transparentes con LUNA y sello este como placa circular.
- Cuatro grutas de recuperación a -3 m, murales, escaleras y losas que se abren desde abajo. Sin respawn.
- Luz blanca 2000 / 24 m / 36°; filtros 520 / 15 m / 28°.
- Umbral de la Cresta con luz pálida y línea en el piso; reflejos recortados en las cuatro paredes, sin reflejo en el vano vacío. Fondo de basalto sin brillo pulido.
- Créditos al permanecer en la huella del cráter, sin cierre anticipado al salir del atrio.
- Ideas descartadas del resumen excluidas.

## Evidencia

- Reconstrucción y validación de referencias: sin errores (Logs/final.log).
- EditMode: 17/17 aprobadas (Logs/editmode.xml).
- PlayMode: cuatro pruebas aprobadas en sus últimas ejecuciones. El recorrido completo y el eclipse están en Logs/playmode-recorrido.xml; reflejos y las cuatro recuperaciones se verificaron después en Logs/playmode-verificaciones.xml.
- El último ajuste tras el recorrido fue excluir rocas de horizonte sin collider del valle; no cambia el camino ni las mecánicas.
- Capturas reales de Unity en Docs/Previews: capilla visible al inicio, oculta en la bajada, visible en la siguiente loma; mirador de totalidad, luz de la Cresta, gruta y plaza.
- Los logs anteriores conservan los fallos de pruebas que permitieron corregir el pebetero sobre la escalera de O3 y el cierre anticipado del epílogo.

## Para probar

Abrir Assets/Scenes/CraterVerticalSlice.unity y presionar Play. La escena ya está reconstruida; si se modifica el constructor, usar Crater > Reconstruir todo.

La lectura visual se verificó mediante capturas fijas y la jugabilidad mediante tests; queda el playtest subjetivo de orientación, ritmo y luz en la máquina del jugador. Unity informa reducción automática de resolución del atlas de sombras en algunas vistas y avisos del detector de tallados basado en bounds; la validación de referencias y la compilación no reportaron errores.
