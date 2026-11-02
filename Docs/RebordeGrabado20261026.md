# Reborde y murales tallados — 26/10/2026

Proyecto aplicado: `C:/Users/carbo/Desktop/crater`. Fuente en los constructores; no se editaron materiales o escena a mano.

- `ConstructorCrater.InicioElevado.cs`: reborde interior de radio 10,8 → 10,25 m, con faldón de tierra de 0,8 m que cubre las juntas del pozo. Conserva la boca de la escalera mediante una transición de altura a los lados de la entrada.
- `ConstructorCrater.Pozo.cs` y `ConstructorCrater.Grabado.cs`: remates de paredes del pozo a y 8,82 m, por debajo de la superficie exterior. La actualización localizada ajusta las paredes existentes sin cambiar el piso o la rampa de la escalera.
- `ConstructorCrater.LuzApertura.cs`: elimina los cuatro focos exteriores `Luz_Borde_Apertura` y sus soportes. El prólogo queda sin referencias a esos focos. Conserva las luminarias de los dibujos interiores y las velas de la entrada.
- `ConstructorCrater.Tallados.cs` y `ConstructorCrater.Grabado.cs`: dibujos de la escalera 25 cm más bajos; los primeros se limitan a y 8,05 m para no superar su pared.
- `ConstructorCrater.CaminoMurales.cs` y `ConstructorCrater.Grabado.cs`: nueve murales con mapas normales de incisión, altura/paralaje 0,008 y oclusión suave 0,3 en URP/Lit. Profundidad usada para calcular las normales: 1,5 cm. Color piedra, smoothness 0,04, metallic 0 y sin emisión. Es un relieve visual: no agrega geometría ni procesamiento de CPU durante el juego.
- Mapas derivados en `Assets/Art/Murals/Runicos`, generados por el constructor. Los PNG originales permanecen intactos. Los murales siguen a pared completa; LUNA conserva sus estrellas en la otra pared.

Respaldo previo en `crater-chatgpt/Logs/RebordeGrabado20261026/Antes`. Los 51 PNG/WAV protegidos conservan su hash. Sin commit, push ni check-in.

Validación: `FiltrosSostenidosPlayTests` PlayMode 6/6 aprobados, incluidos los límites de altura de los ocho dibujos tras abrir y la ausencia de focos exteriores. Validador: 0 problemas. Revisión visual en Play del reborde y del grabado; capturas en `Docs/Previews/RebordeGrabado20261026`. No se ejecutó la suite completa. El tono final del grabado se toma del material de cada pared para integrarlo con la piedra.
Ajuste posterior solicitado: los nueve tallados pasan a blanco mate. Tinta derivada RGB blanco y BaseColor blanco; conserva normales, paralaje y oclusión, sin emisión. Constructor y escena actualizados; comprobación de los nueve materiales correcta. No se repitieron tests por este ajuste visual.
Corrección posterior: conservar aspect ratio original. Los nueve murales se encajan completos al máximo tamaño dentro del paño, centrados, sin recortar ni estirar. Normal/parallax calculados con sus medidas finales. Importadores de tinta, normal y altura usan NPOT None para impedir redimensionado desproporcionado. Comprobación directa de proporciones9/9 y validador0; test actualizado para exigir proporción. Sin repetir suite de tests por este ajuste.
