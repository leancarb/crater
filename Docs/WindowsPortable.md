# CRATER en un único EXE para Windows

El archivo para compartir es **`Builds/CRATER_Windows.exe`**. La otra persona hace doble clic y se abre el juego. No necesita Unity, instalar el proyecto ni descomprimir un ZIP manualmente.

La primera ejecución extrae los archivos de Unity en `%LOCALAPPDATA%/CRATER/Portable/<versión>`. Las siguientes reutilizan esa copia. Cada build se identifica por el hash de su contenido, de modo que otra versión no pisa una que se está jugando. Si falta algún archivo, el lanzador lo vuelve a extraer. Se conservan las ubicaciones de guardado y preferencias normales del juego.

## Generarlo desde Unity

- **Crater → Construir demo Windows**: valida, construye el jugador, genera el ZIP y el EXE portable. El empaquetado portable se ejecuta desde un Editor en Windows.
- **Crater → Empaquetar Windows en un solo EXE**: vuelve a empaquetar el build de `Builds/Windows` existente, sin recompilar el juego.

El constructor usa el compilador de .NET Framework incluido en Windows; no descarga herramientas ni requiere un instalador externo. El código del lanzador está en `Tools/WindowsPortable/LanzadorCrater.cs`. Es un paquete para Windows de 64 bits. El flujo de macOS sigue generando su ZIP.

## Verificación del 6/11/2026

- Unity 6000.3.23f1: build de Windows completado con resultado **Success**.
- EXE portable con ícono: **79.029.760 bytes**, aproximadamente 79 MB.
- SHA-256: `7535b53493ac417d324d5166ad3f17f87432fc09dd68d27184503a7a0d3d83b5`.
- Se copió el EXE solo a una carpeta con espacios y caracteres Unicode y se ejecutó la preparación desde allí.
- Los **178 archivos** extraídos coinciden por SHA-256 con el build empaquetado.
- Una segunda apertura reutilizó el cache; quitar un archivo generado y volver a abrirlo reparó la extracción.
- El propio lanzador abrió el jugador Unity desde la carpeta extraída. El arranque se verificó en modo batch, sin gráficos; el proceso de prueba se cerró al terminar.

Esta comprobación cubre el empaquetado y el arranque en esta PC. No incluye un recorrido completo del juego ni una prueba en otra computadora. Los registros de compilación y arranque quedan en `Logs/WindowsPortable20261106`, fuera de Git. El resultado resumido está en `Docs/Testing/WindowsPortable20261106.json`.

Se recompiló posteriormente con el ícono de `images/icon.jpg`; se verificó también en una ventana real del juego. Ver `Docs/IconoWindows20261106.md` y los registros locales de `Logs/IconoWindows20261106`.
