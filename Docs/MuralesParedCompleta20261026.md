# Murales a pared completa y esquina SOL

Aplicado en `Desktop/crater`, desde `ConstructorCrater.CaminoMurales.cs` y `ConstructorCrater.PuertaSol.cs`.

- Los nueve murales usan el ancho y alto del paño visible de su pared, con un margen de 8 cm. La imagen completa se adapta a ambas dimensiones, sin recortar el dibujo. Conservan tinta mate, sin fondo negro ni emisión.
- El constructor retira dibujos, frisos y paneles anteriores superpuestos a esos paños. En SOL se eliminaron los cuatro `Friso_O2_*` de la captura. No retira paredes, puertas ni receptores funcionales.
- LUNA pasa de `Muro_E2_Norte` a `Muro_E2_Oeste`, mirando al interior. Conserva las seis `Estrellas_E2_*` de la pared norte y `Luna_E2` en la pared este.
- El ancla de la puerta SOL pasa a la esquina opuesta: diseño (−22; 0; 19), más el corrimiento del ala y la ampliación del subsuelo. Conserva su conexión con la puerta y la señal inferior.
- La actualización localizada y la reconstrucción completa vuelven a aplicar esta disposición.

PlayMode dirigido: `FiltrosSostenidosPlayTests`, 5/5 aprobados, incluido el encuadre de puerta/ancla desde el suelo seguro, tamaño del mural SOL y conservación de las estrellas. Revisión visual del mural SOL en Play. No se ejecutó la suite completa.

Respaldo previo: `crater-chatgpt/Logs/MuralesParedCompleta20261026/Antes`. Sin push ni check-in. Esta disposición reemplaza los tamaños y posiciones de murales/ancla descritos en `CaminoMurales20261026.md`.
