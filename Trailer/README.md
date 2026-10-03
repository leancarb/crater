# CRATER — tráiler

Animación en JavaScript (canvas 2D + WebAudio, sin dependencias) que presenta la estética y las mecánicas del juego en ~1:45.

Abrir `index.html` en un navegador y tocar **Ver tráiler** (el sonido necesita ese clic).
Controles: `Espacio` pausa · `R` reinicia · `M` silencia.

Sigue el GDD v0.5 y toma la estructura del tráiler de *Silo*: planos lentos, placas de texto sobre negro
con un golpe grave, un montaje que se acelera, el título recién al final y un cierre corto después.

Secuencia: la capilla de día → el eclipse (se callan los pájaros, sube el cráter, destella la puerta) →
el óculo con el eclipse congelado → se enciende la linterna → CUERPO (anclas y puente de luz) →
HUECO (la reja cede) → la Cresta (la pared espejo; al apagar, aparecen los tallados y se abre la puerta) →
montaje → anillo de diamante → título → PRÓXIMAMENTE.

Las frases de las placas están en `CUES` dentro de `trailer.js`. Colores, conos, alcances y tiempos
salen de `Assets/Filtro_*.asset`, `Assets/Scripts` y el GDD.
