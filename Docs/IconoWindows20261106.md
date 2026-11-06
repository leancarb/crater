# Ícono de la aplicación — 6/11/2026

Se usa `images/icon.jpg` completo, sin recortar ni modificar el original. El constructor lo importa como `Assets/Art/Iconos/CRATER.jpg` y configura los tamaños de ícono de Standalone mediante la API de PlayerSettings.

El jugador de Unity lleva el ícono en sus recursos nativos de Windows. El empaquetador también genera un ICO multirresolución (16, 24, 32, 48, 64, 128 y 256 píxeles) y lo incrusta en `CRATER_Windows.exe`; su ventana de preparación toma el ícono del propio ejecutable. El ICO separado no hace falta para compartir el juego.

Verificación:

- Build de Windows y empaquetado portable completados correctamente.
- Ícono del archivo portable y del jugador extraídos y comprobados: ambos contienen el eclipse naranja/azul.
- Se abrió el juego desde el portable y se consultó el ícono de su ventana real `UnityWndClass`, título `CRÁTER`. Coincide exactamente con el ícono extraído del jugador (mismo SHA-256 del PNG de comprobación).
- Los 178 archivos extraídos del portable coinciden con su ZIP. Se verificó reutilización del cache y reparación de un archivo faltante.
- El proceso de juego creado para la prueba se cerró al terminar. No se modificaron escenas, geometría, controles ni audio.

El archivo para compartir sigue siendo `Builds/CRATER_Windows.exe`. Las próximas construcciones vuelven a tomar el ícono desde el constructor.
