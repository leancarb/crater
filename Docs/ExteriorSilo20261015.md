# Exterior inspirado en Silo — 15/10/2026

Copia activa: `C:/Users/carbo/Desktop/crater`. Referencias: las cuatro capturas de Silo aportadas por el usuario. Se tradujeron paleta, luz, bruma, grava y vegetación seca al lenguaje facetado de CRÁTER.

## Implementación

- `Assets/Editor/ConstructorCrater.ExteriorSilo.cs:13`: menú `Crater > Actualizar exterior referencia Silo`, ya ejecutado. Genera y guarda sólo el cambio exterior desde Unity.
- `Assets/Editor/ConstructorCrater.Nivel.cs:99`: la reconstrucción completa vuelve a aplicar el exterior después del facetado y antes de hornear oclusión.
- `Assets/Editor/ConstructorCrater.ExteriorSilo.cs:29`: tierra gris ocre `(0,61; 0,58; 0,47)`; adobe, cal, piedra, cubierta y vegetación con menor saturación. Materiales compartidos del subsuelo preservados.
- `Assets/Shaders/SueloExterior.shader`: detalle mate en coordenadas de mundo, manchas de polvo y grano irregular; conserva normales facetadas, sombras del sol y niebla URP. Sin emisión. No depende de los UV por faceta del suelo anterior.
- `Assets/Editor/ConstructorCrater.ExteriorSilo.cs:39`: material propio de hormigón mate para borde, cordones y umbral exteriores. No modifica los materiales de basalto interiores.
- `Assets/Editor/ConstructorCrater.ExteriorSilo.cs:51`: luz diurna 2; sol `(0,98; 0,96; 0,83)`, cielo `(0,87; 0,90; 0,79)`, ambiente superior `(1; 1; 0,94)`, horizonte `(0,85; 0,83; 0,70)` y suelo `(0,50; 0,49; 0,40)`. Niebla exterior 0,005. Dirección solar inicial 14°, dirección del epílogo, progreso y tiempos del eclipse conservados.
- `Assets/Shaders/CieloExteriorBrumoso.shader`: fondo de nubes suaves, estáticas y de bajo contraste. Esfera sin collider ni sombras; acompaña a la cámara y se oculta en el subsuelo.
- `Assets/Shaders/CieloExteriorComun.hlsl` y `Assets/Art/Shaders/CieloSinNiebla.shader`: fondo y silueta lunar comparten la misma función de cielo. Evita ver un disco lunar artificial sobre las nubes antes del eclipse. El modo interior conserva su color y profundidad anteriores; sol y corona no usan el modo de imitación.
- `Assets/Scripts/Flujo/CieloEclipse.cs:32`: referencia al fondo, visibilidad en las transiciones, posición y color. Es un cambio de presentación; se conserva la lógica del eclipse.
- `Assets/Editor/ConstructorCrater.ExteriorSilo.cs:101`: grava dispersa con semilla fija 151026, en dos mallas/materiales. Se apoya sobre las mallas visibles del terreno y las lomas; excluye la estación, el hueco del cráter y reduce densidad en el eje caminable. Sin nuevos colliders.
- Cardones: se oculta su representación verde y se genera una silueta de arbusto seco en cada ubicación. Las colisiones existentes se conservan. Se reduce la representación de paja a un tercio.
- `Assets/Models/ExteriorSilo/`: tres mallas persistentes (grava oscura, grava clara y arbustos), 98.484 triángulos en conjunto, tres renderers. El grano fino restante se resuelve en el shader. No se midió rendimiento de build.
- `Assets/Scenes/CraterVerticalSlice.unity`: salida del menú generador, guardada mediante Unity; no se editó YAML.
- `Assets/Tests/EditMode/ExteriorSiloTests.cs`: comprobación de shaders y regeneración determinista sin cambios de colliders.
- `Assets/Tests/PlayMode/CorreccionEclipseAudioTests.cs:71` y `LegibilidadEpilogoTests.cs:82`: expectativa de niebla exterior actualizada de 0,003 a 0,005; niebla interior permanece 0,02.

## Preservación y validación

- Compilación de C# y shaders sin errores; validador: 0 problemas.
- 612 colliders antes y después; todos conservan exactamente su serialización. Dos regeneraciones producen los mismos vértices.
- ExteriorSiloTests: 2/2 EditMode aprobados (`Logs/ExteriorSilo/EditMode.json`).
- Los 43 WAV conservan sus SHA-256 (`Logs/ExteriorSilo-AudioAntes.json` y `ExteriorSilo-AudioDespues.json`). No se modificó `Assets/Audio/Grabado`.
- Se conservan el recorrido, las alturas caminables, las lomas, los disparadores y los accesos. No se reintrodujeron los cambios descartados de pilares, rejas o iluminación interior.
- Respaldo previo de escena y fuentes relevantes: `Backups/ExteriorAntesSilo20261015`.
- Sin commit, push, check-in en Unity Version Control ni builds/ZIP nuevos.

Resultados PlayMode y vistas finales se registran al terminar la validación.

## Estado actual: variante archivada el 16/10/2026
Por decisión del usuario volvió el exterior anterior: tierra rojiza, cardones y paja originales, cielo azul y niebla exterior 0,003. Se restauraron 353 renderers a partir de la escena previa, las referencias de materiales y los parámetros diurnos. Los 612 colliders conservaron su serialización exacta. La farola baja (escala 0,74), la placa de título y el HUD con la fuente anterior se conservaron.
La variante Silo se guardó antes de la reversión en Backups/ExteriorSilo_Archivado_20261016 y en el ZIP del mismo nombre: 76 archivos con manifiesto SHA-256 y prueba de integridad del ZIP. El generador optativo permanece disponible desde Crater > Actualizar exterior referencia Silo, pero ya no se ejecuta al reconstruir el nivel. Sus tests específicos están en el respaldo, fuera de la suite activa. Las expectativas de niebla del recorrido volvieron a 0,003. No hubo check-in, commits, builds ni cambios de audio.
