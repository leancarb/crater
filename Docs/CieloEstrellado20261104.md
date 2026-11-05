# Cielo estrellado — 4/11/2026

Se agrega un campo de 1300 estrellas al constructor del cielo, con semilla fija 41673. Usa una sola malla de 5200 vértices y un material sin iluminación; no agrega luces, GameObjects por estrella ni imágenes externas. Los puntos varían en tamaño, brillo y tono cálido/frío, con contorno suave y menor presencia en el horizonte.

Antes del eclipse son casi imperceptibles (multiplicador 0,18). Su brillo aumenta suavemente entre el 8 % y el 95 % de progreso hasta 2,2 durante la totalidad. En el epílogo baja a 0,035. Los puntos siguen la posición de la cámara, conservando sus direcciones en el paisaje. No se agrega una animación de parpadeo.

El shader coloca las estrellas al fondo de la profundidad y antes del sol, la luna y la corona. Las paredes, los techos y las montañas las ocultan. Se conservan al mirar el cielo desde una abertura real del subsuelo; no se incluyen entre los renderizadores de paisaje que se apagan al bajar. Mostrar(false) las oculta junto al cielo.

Archivos: `Assets/Editor/ConstructorCrater.Estrellas.cs` (nuevo), `ConstructorCrater.Nivel.cs`, `Assets/Art/Shaders/EstrellasCielo.shader` (nuevo), `Assets/Scripts/Flujo/CieloEclipse.cs`, `Assets/Tests/PlayMode/CartelesYCieloTests.cs`. Los materiales, mallas y la escena se regeneran desde código.

La prueba de color lunar aísla las estrellas para comparar el disco contra el fondo liso: la luna debe ocultar las estrellas que pasan detrás. Dos pruebas nuevas verifican aumento de puntos visibles, estados del cielo y ocultación completa por una pared opaca.

Revisión visual: [antes del eclipse](Previews/Estrellas20261104/01_Antes.png) y [totalidad](Previews/Estrellas20261104/02_Eclipse.png). Capturas de la cámara del juego, sin editar imágenes. No hay commit, push ni check-in de Unity Version Control.


Validación: 33/33 EditMode, 8/8 PlayMode de cielo/carteles; validador 0 incidencias, shader sin errores de compilación y consola sin errores. La reconstrucción conserva avisos previos de logos de créditos ausentes y aros tallados sobre columnas. Los 169 WAV/PNG protegidos permanecen idénticos. Resultados en `Docs/Testing/Estrellas20261104`. No se generó un build.
