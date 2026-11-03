# CRATER — teaser

Teaser de ~44 s, en JavaScript puro con canvas 2D y WebAudio, sin dependencias.
Abrir `index.html` y tocar **Ver tráiler**: el sonido necesita ese clic.

Estética low-poly de facetas grandes y sombreado plano:
- piedra clara contra negro;
- el haz como una cuña translúcida, con su mancha nítida en el piso;
- anclas de roca facetada con anillos;
- puente de paneles triangulados.

Sigue el GDD v0.5:

1. El eclipse: sube el cráter y destella la puerta.
2. Placa sobre negro.
3. El Umbral: sala circular con el óculo estrellado y la linterna en el piso, que se enciende.
4. La sala de los monolitos.
5. CUERPO: el haz enciende un ancla y aparece el puente entre dos anclas.
6. Placa sobre negro.
7. La Cresta: la pared espejo, "Apagala." y la puerta plateada.
8. Anillo de diamante.
9. Título.
10. PRÓXIMAMENTE.

Sin montaje de repaso al final.

`drawFrame(t)` depende solo de `t`. Los tiempos por escena están en `SC` y las placas en `CUES`.
Las anclas cargan en 0,35 s, como en el juego.
