# CRATER — teaser

Versión corta del tráiler (~58 s), en JavaScript puro con canvas 2D y WebAudio, sin dependencias.
Abrir `index.html` y tocar **Ver tráiler**: el sonido necesita ese clic.

Sigue el GDD v0.5 con la estructura del tráiler de *Silo*:

1. La capilla de día.
2. El eclipse: sube el cráter y destella la puerta.
3. Placa sobre negro.
4. El túnel: se enciende la linterna.
5. CUERPO: las anclas y el puente de luz.
6. HUECO: la reja.
7. Placa sobre negro.
8. La Cresta: la pared espejo, "Apagala." y la puerta plateada.
9. Montaje.
10. Anillo de diamante.
11. Título.
12. PRÓXIMAMENTE.

Reutiliza las escenas de `../trailer.js` con un mapa de tiempo por escena (`SC` en `teaser.js`).
Así se acorta sin cambiar la carga real de los receptores (0,35 s) ni la demora de cambio de filtro (0,8 s).
Las placas están en `CUES`.
