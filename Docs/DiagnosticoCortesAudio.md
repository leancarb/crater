# Cortes e interferencia de audio — 6/10/2026

El usuario informa que todos los sonidos se oyen cortados y con interferencia. Esto reabre el diagnóstico: las pruebas anteriores de RMS/isPlaying no acreditan una reproducción sin cortes ni una mezcla agradable.

## Lo observado

- Editor conectado: Unity 6000.3.23f1, proyecto CRÁTER, una instancia del editor. No se detectó un proceso del EXE en el momento de la inspección.
- Una sola AudioListener habilitada; 86 fuentes configuradas, tres en reproducción durante el muestreo exterior. Ningún filtro de audio global ni OnAudioFilterRead de producción.
- Motor: estéreo, 48.000 Hz, buffer de 1.024 muestras y cuatro buffers; volumen maestro 1, pausa desactivada. No se cambiaron preferencias del jugador ni del dispositivo de Windows.
- Se instrumentó temporalmente el receptor en una prueba de PlayMode: copia continua de la mezcla, sin modificar muestras y sin escribir archivos en el hilo de audio. Se capturó el exterior y un tono independiente de control, 220 Hz.
- Antes del reinicio: exterior con pico 0,321, sin recorte ni muestras no finitas; no se encontraron bloques de 10 ms de silencio. Tono de control con pico 0,0354, salto máximo entre muestras 0,00104 y silencio máximo de una sola muestra. Ningún corte reproducido en la señal interna.
- CPU de audio máxima medida 1,095 %; intervalo máximo observado entre callbacks 40,4 ms. DSP avanza por bloques de 21,33 ms. Estas mediciones corresponden a la sesión de pruebas; no garantizan el comportamiento de todas las partidas ni del controlador físico.

## Acción aplicada

Se ejecutó una sola reinicialización del motor de salida mediante AudioSettings.Reset, fuera de Play, usando exactamente la configuración previa. Unity confirmó éxito y mantuvo todos los valores. No se agregó un reinicio automático al juego, no se cambió el buffer y no se reemplazó el audio.

## Alcance y pendiente

Las capturas son internas a Unity, anteriores a la salida física del dispositivo. No permiten descartar interferencia en la salida de Windows, auriculares, parlantes o su controlador. Tampoco constituyen escucha humana. Se pidió confirmar si ocurre en Play o en el EXE. La causa permanece sin confirmar y el problema no se declara resuelto: falta comprobar si el reinicio eliminó el síntoma al jugar.

La instrumentación es temporal y se archiva en Logs/Audio fuera de Assets. Las capturas y métricas se guardan en esa carpeta. No se generó EXE. Se conservan los cambios de eclipse y el contenido contemplativo.

Tras la reinicialización se capturaron además diez segundos de apertura (pico 0,334) y diez segundos de cambios SOL/LUNA/blanco (pico 0,145): sin recorte, muestras inválidas ni saltos en el avance DSP. La prueba de instrumentación completó las cuatro capturas; esto no resuelve por sí solo el síntoma informado.
