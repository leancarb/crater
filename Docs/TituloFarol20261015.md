# Título, prueba de HUD y farol — 15/10/2026

Copia confirmada por el usuario: `C:/Users/carbo/Desktop/crater`. Base restaurada; no se reaplicaron los cambios anteriores de iluminación, columnas o rejas.

## Cambios

- `Assets/Resources/Fonts/CCElephantmenGreatest Bold.ttf`: copia de la fuente aportada en `font/`, disponible en builds. Conserva Á y los símbolos de los controles.
- `Assets/Scripts/Interfaz/InterfazCrater.cs:28`: `usarFuenteNuevaEnHUD = true`. Prueba de la fuente en estado, filtros, indicaciones, interacción, pausa y subtítulo. Factor de tamaño 0,73 para compensar su ancho. Desmarcar antes de Play devuelve el HUD anterior; título independiente. Los logos y textos institucionales conservan su estilo.
- `Assets/Scripts/Interfaz/InterfazCrater.cs:31`: fondo crema `#D9CF97`, tinta oliva `#4B4016`.
- `Assets/Scripts/Interfaz/InterfazCrater.cs:199`: título CRÁTER a 110, sin sombra, con espaciado y desgaste. Se usa en apertura y cierre; el fondo crema acompaña al título en créditos. Fundidos existentes conservados.
- `Assets/Scripts/Interfaz/TipografiaTitulo.cs`: separa las letras desde los vértices, conservando la palabra y el acento.
- `Assets/Resources/UI/TituloDesgastado.shader`: poros estáticos de desgaste únicamente en el título. Material creado una vez y liberado al destruir la interfaz.
- `Assets/Editor/ConstructorCrater.cs:706` y `:893`: el generador usa el farol nuevo en mano y recogible, de modo que se conserva al reconstruir.
- `Assets/Editor/ConstructorCrater.Farol.cs:11`: genera depósito oscuro, difusor opalino, jaula de ocho nervios y asa arqueada; cuatro mallas persistentes en `Assets/Models/Farol`, tres materiales propios. No agrega luces ni colliders. El difusor cambia de color mediante LinternaVisual y los filtros existentes.
- `Assets/Editor/ConstructorCrater.Farol.cs:117`: menú `Crater > Actualizar título y farol`; actualiza los dos prefabs sin reconstruir el nivel. Ya ejecutado.
- `Assets/Prefabs/Jugador.prefab` y `Recogible_Linterna.prefab`: modelos y referencias visuales actualizados. Pose en mano `(0,24; -0,22; 0,60)`, rotación `(0; -18; -8)`; recogible apoyado por su base. La escena hereda los cambios de prefab.

## Validación

- Unity 6000.3.23f1: compilación sin errores.
- Validador en EditMode: 0 problemas.
- FarolTituloTests (EditMode): 3/3.
- RitmoCraterTests (PlayMode): 3/3; anclas, desplazamiento y filtros de dos segundos. Resultado en `Logs/FarolTitulo-PlayMode.json`.
- TipografiaCraterTests (PlayMode): 3/3; fuente y símbolos del HUD, reversión independiente del título, colores y shader. Resultado en `Logs/Tipografia-PlayMode.json`.
- Revisión visual: render del farol, captura en primera persona, título y HUD/pausa a 1280×720. Las vistas de HUD son muestras de composición con textos de prueba y fondo neutro.
- No se ejecutó la suite completa ni se generaron builds/ZIP nuevos. No se modificaron WAV ni Assets/Audio/Grabado. No hubo commit, push ni check-in en Unity Version Control.

## Reversión de la prueba de HUD

Desmarcar `Usar Fuente Nueva En HUD` en InterfazCrater antes de entrar en Play. La copia previa del script (con el título tipográfico y antes de esta referencia visual) está en `Backups/TipografiaAntesReferencia20261015/InterfazCrater.cs`; es un respaldo, no una fuente alternativa del proyecto.

## Ajuste posterior: farola baja y placa original
La pose en mano pasa a (0,26; -0,35; 0,66), escala 0,74: 13 cm más abajo y un 26 % menor que la pose anterior. El recogible conserva su tamaño. ConstructorCrater.cs y ConstructorCrater.Farol.cs mantienen el ajuste al regenerar; LinternaVisual tiene el mismo valor por defecto.
El título usa ahora la imagen original aportada por el usuario en Assets/Resources/Art/TituloCrater.png (1672×941), sin compresión, mipmaps ni cambio de proporción. SHA-256 del PNG copiado idéntico al original. InterfazCrater presenta la placa con RawImage y AspectRatioFitter, conserva los fundidos y la oculta para los créditos en texto. La fuente del HUD sigue siendo la prueba anterior, independiente.
Validación de este ajuste: compilación sin errores, validador 0, FarolTituloTests 3/3 EditMode y TipografiaCraterTests 4/4 PlayMode. Resultados en Logs/FarolaBajaPlaca y vistas en Docs/Previews/FarolaBajaPlaca. Sin check-in, builds ni cambios de audio.

Decisión posterior del usuario: se desactiva la fuente nueva en el HUD. usarFuenteNuevaEnHUD queda false tanto por defecto como en la escena guardada mediante Unity; recupera fuente y tamaños anteriores. La placa del título se conserva.

## Decisión vigente 16/10/2026: título tipográfico blanco y negro
Se retira la placa de la interfaz y de Resources. La imagen y el script anterior están respaldados en Backups/TituloImagenArchivado_20261016. CRÁTER vuelve a presentarse con CCElephantmenGreatest Bold: blanco sobre negro al inicio y negro sobre blanco en los créditos y el cierre. Se conserva el espaciado y los fundidos; se elimina el material de desgaste del título activo. HUD anterior y farola baja sin cambios.

Ajuste posterior: el título visible se escribe CRATER, sin tilde, tanto al inicio como al final. Se conservan fuente, espaciado y colores blanco/negro.
