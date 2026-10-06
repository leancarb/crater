# Legibilidad de detalles y regreso sin rastro — 6/10/2026

El jugador informó que no encontró o no entendió siete detalles. Esta revisión corrige presentación y respuesta; las comprobaciones automáticas no se toman como evidencia de comprensión.

- Cuenco: escala mayor, agua dentro del borde, origen luminoso con soporte físico y posición alineada con una mirada hacia el agua. Reflejo del entorno capturado al acercarse; sin captura continua.
- Perspectiva: encuadre desde la aproximación sur, al lado opuesto del cuenco para no superponer ambos motivos. Los seis sectores usan un material propio con emisión habilitada y reciben un brillo conjunto al alinearse, con zona tolerante de 2 m. No modifica el progreso.
- Veta: relieve más ancho, respuesta angular más marcada y rápida; material independiente de la iluminación directa para que la blanca no sobreexponga todas las variaciones. Mantiene alcance/cono/oclusión del haz.
- O1: cuenco ampliado, reparación en la cara visible y piedras sobre el asiento real, colocadas desde su transformación preservada después de ampliar el subsuelo.
- Losa: el motivo queda oculto hasta el primer sello; después aparece gradualmente con más contraste. Es una transformación ambiental, no una puerta ni un objetivo obligatorio.
- Polvo: textura radial suave, transparencia corregida y altura más próxima al encuadre desde el suelo. Sigue limitado a 20 partículas, con emisión baja y pausa normal.
- Mirador: desplazado de 12 a 7,5 m respecto del eje del sendero. Ubicado a 78 m longitudinales del origen exterior, antes de las lomas: en el punto anterior un cerro bloqueaba la vista. El borde abierto mira al sendero; tres lados protegidos y regreso por la misma entrada. Al volver desde abajo se restablecen luz y niebla del exterior; antes quedaba el ambiente del subsuelo. La primera prueba encontró una pared bloqueando la entrada y se corrigió.

## Epílogo

Durante el traslado bajo la pantalla blanca se cierran las hojas de la Cresta, se tapa el óculo circular y se restaura el terreno exterior completo con collider. La ranura de paisaje que permitía ver el eclipse desde abajo también se cierra. El terreno final utiliza triangulación facetada sin un centro radial que marque el antiguo pozo. No se muestran la tapa circular, la huella ni el guiño sobre la puerta de la casa.

La zona de créditos permanece invisible y activa en el antiguo lugar del cráter; se conserva también la alternativa de 45 segundos libres. El final principal continúa independiente de la colección.

El sol final usa elevación 18° y azimut 30°, con colores de atardecer, distinto de la aproximación (14° y 180°). El retorno opcional por la escalera antes del final sigue restaurando el eclipse de la aproximación; no activa el atardecer.

## Validación

Las dos pruebas nuevas de legibilidad/epílogo pasan; la vista del mirador se comprueba caminando desde su entrada con CharacterController y mediante oclusión visual, y se inspeccionó la captura real. El cierre se comprueba con rayos de colisión sobre el antiguo pozo y capturas del terreno y del sol final.

La ejecución inicial de PlayMode dio 28/30: pasaron los dos órdenes principales sin piezas, el final con colección parcial, las cuatro recuperaciones y las pruebas de mecanismos/colección. Dos fallos eran precondiciones de las pruebas: el recorrido completo iluminaba las anclas de entrada y luego intentaba bajar por el puente todavía sólido; ahora apaga y espera su disolución antes de descender, sin modificar la retención del juego. La prueba de mouse inyectaba el delta durante los tres frames iniciales descartados para estabilizar el cursor; espera esos frames y vuelve a pasar con pausa y salto de sellos. No se modificó el control de producción.

La repetición del recorrido con las seis piezas y final pasa (259 s). EditMode: 23/23. La cobertura de PlayMode queda con sus 30 casos aprobados entre la ejecución general y las repeticiones específicas, sin afirmar que la ejecución inicial fue 30/30. Logs de repeticiones: `Logs/legibilidad_epilogo_mirador_final.json`, `Logs/legibilidad_controles_final.json`, `Logs/legibilidad_coleccion_completa_final.json` y `Logs/legibilidad_editmode_final.json`. Capturas actuales: Docs/Previews/Exploracion, números 21 a 32. Se verificaron los 43 WAV contra sus SHA-256 y el catálogo contra la copia anterior: cero cambios. No se generó EXE.

La comprensión espontánea, descubrimiento, comodidad y disfrute requieren una sesión con jugadores; estas pruebas sólo verifican funcionamiento y presentación desde posiciones conocidas. El diagnóstico anterior de cortes de audio permanece sin causa confirmada; estos cambios no lo declaran resuelto.

Validación final después de reconstruir nuevamente: cero problemas de referencias, compilación sin errores, escena guardada y fuera de Play. Conteos idénticos antes/después: 2623 objetos, 109 luces, 6 piezas y 3 cierres; no se duplicaron en la reconstrucción. Registro: `Logs/legibilidad_validacion_final.json`.

Decisión posterior del usuario: conservar el cuenco y su agua, retirar la columna rectangular con el sol y su foco asociado (Apoyo_Origen_Reflejo, Origen_Reflejo y Luz_Origen_Reflejo). El agua refleja el entorno existente. Las capturas anteriores con esa columna son históricas.
