# Corrección de bordes, inicio, luces, encuadre y reflejo — 4/11/2026

Esta revisión prevalece sobre `Revision20261104.md` para los puntos siguientes. Trabajo en `Desktop/crater`, con código y escena regenerada copiados a `Desktop/crater-chatgpt`. Sin commit, push ni check-in de Unity Version Control.

## Inicio y bordes

El exterior está girado 180°: el anterior X local -6 dejaba al jugador en X mundial +6, hacia la derecha de la vista. El inicio pasa a X local +14 (X mundial -14), conservando Z local 187 y orientación hacia el paisaje. La terraza se ensancha hasta X local 25, con cierre lateral visible y cierre posterior. El sendero comienza en la nueva posición y conecta con la bajada anterior.

Las crestas naturales pasan de dos superficies abiertas a sólidos triangulares cerrados: laterales, base y tapas. La orientación se conserva entre secciones y se verifica sobre el volumen completo; no se usa un material de doble cara para disimular huecos. Las juntas tienen continuidad geométrica y los colliders usan la misma malla cerrada. Se conserva la estética de tierra/roca facetada. No se modifican la velocidad, carrera, gravedad ni salto del jugador.

## Luces

Se elimina la creación del spot `Luz_Cartel` en todos los carteles. No se agrega emisión para reemplazarlo. También se retiran los puntos ambientales sin soporte en pasillos, salas y murales: `Luz_Umbral`, `Luz_Compuerta`, `Luz_Pasillo_*`, `Luz_Vuelta_*`, `Luz_Mural`, luces generales O1/O2/O3/E1/E2, desvíos, galerías, grutas, Cruce y Cresta. Se retiran los dos rellenos genéricos de las secuencias de columnas; el validador exige en cambio las fuentes funcionales de piso y techo de cada columna.

Se conservan farol, velas con llama, lámparas de anclas, haces desde aberturas visibles, aros funcionales de columnas y obelisco, señal de puerta, respuesta de recogida/conjunto y rebote del propio farol. La luz ambiental del proyecto se conserva; no se agregan nuevos puntos flotantes.

## Encuadre

El punto de observación pasa del eje de entrada `(0; 1,62; 5,6)` al lateral `(-5; 1,62; 8)`. Las seis piezas se separan en profundidad en pasos de 0,65 m, con escala corregida por perspectiva. Desde el acceso central dejan de formar el círculo completo. Cada pieza tiene apoyo y brazo de piedra; la visual al difusor queda libre desde el punto correcto.

La alineación exige cercanía horizontal inferior a 0,45 m, diferencia vertical menor a 0,35 m y dirección de mirada dentro de 4°. La luz cenital tenue responde suavemente al encontrar esa posición. Sin línea blanca, giro forzado ni bloqueo del jugador.

## Reflejo de la última sala

El plano que se recolocaba según la normal de cada tramo de pared se sustituye por una malla de reflejo conformada al cilindro de la sala, de radio 14,8 m. El halo usa 20 × 8 celdas y reutiliza sus buffers. Se recorta al piso/techo y a los sectores donde hay muro; no atraviesa el vano de entrada. El diámetro visible se limita a 6 m. Conserva intensidad 4 y fundido de 0,25 s. Las superficies existentes de piso y techo mantienen su comportamiento.

## Validación

Resultados definitivos y capturas se registran al terminar. Las pruebas usan el movimiento real con presión sostenida contra el borde y posterior retroceso; revisan la posición izquierda y el suelo, el cierre topológico de las mallas, las luces retiradas, la búsqueda lateral y la continuidad del reflejo. Para dispositivos simulados se ignoran cambios de foco durante cada prueba y se restaura después la configuración original. No se modifica la configuración de entrada del juego.

El Editor se cerró durante el trabajo; la recuperación se hizo con la misma versión 6000.3.23f1, en una sesión de validación sin interfaz. El crash registrado estaba en el render de UI del Editor; no se atribuye a una causa de gameplay. Los resultados finales corresponden a la sesión recuperada.


## Resultado final

| Comprobación | Resultado |
|---|---|
| Validador tras regenerar | 0 incidencias |
| EditMode completo | 33/33 |
| PlayMode BordesYEncuadreTests | 6/6 |
| PlayMode RevisionNoviembreTests | 9/9 |
| PlayMode CartelesYCieloTests | 6/6 |
| Consola en la revisión visual | 0 errores, 0 warnings |
| WAV y PNG protegidos del proyecto de trabajo | 169/169 idénticos |

Se ejecutaron las 21 pruebas PlayMode afectadas; no se afirma que toda la suite histórica esté verde. El atasco se comprobó presionando hacia adelante cinco segundos y retrocediendo dos, tanto en el borde de X local 14 como en el anterior de X local -6. El cierre se verificó contando exactamente dos caras por arista soldada y orientación positiva del volumen. Se corrigió además el enrutamiento de teclado/mouse simulados al Game View durante los tests sin ventana; las preferencias se restauran al terminar.

Capturas reales sin editar: [inicio izquierdo](Previews/CorreccionBordes20261104/01_Inicio_Izquierda.png), [extremo cerrado](Previews/CorreccionBordes20261104/02_Borde_Cerrado.png), [lobby sin focos](Previews/CorreccionBordes20261104/03_Lobby_Sin_Focos.png), [encuadre desde la entrada](Previews/CorreccionBordes20261104/04_Encuadre_Desde_Entrada.png), [encuadre encontrado](Previews/CorreccionBordes20261104/05_Encuadre_Encontrado.png), [reflejo curvo](Previews/CorreccionBordes20261104/06_Reflejo_Curvo.png). Los frames avanzaron entre capturas y el reflejo se revisó tras completar el fundido.

Evidencia de pruebas: [Testing/CorreccionBordes20261104](Testing/CorreccionBordes20261104/). La sesión temporal sin interfaz se cerró y liberó el proyecto después de guardar. No se generó un build. Queda pendiente la valoración visual y de comodidad con jugadores; no se atribuye una mejora de rendimiento medida.

## Archivos principales y líneas

| Cambio | Referencia |
|---|---|
| Sólidos de borde con tapas y orientación consistente | `Assets/Editor/ConstructorCrater.Revision20261104.cs:10` |
| Límites de terraza y bordes de bajada | `Assets/Editor/ConstructorCrater.Revision20261104.cs:32` |
| Terraza ampliada y sendero desde la nueva posición | `Assets/Editor/ConstructorCrater.InicioElevado.cs` |
| Inicio desplazado al lado izquierdo mundial | `Assets/Editor/ConstructorCrater.Iteracion.cs:15` |
| Carteles sin creación de focos | `Assets/Editor/ConstructorCrater.CartelesHistoria.cs:109` |
| Encuadre con profundidad y apoyos separados | `Assets/Editor/ConstructorCrater.Revision20261104.cs:127` |
| Retiro de puntos sin soporte | `Assets/Editor/ConstructorCrater.Revision20261104.cs:158` |
| Alineación lateral con tolerancia acotada | `Assets/Scripts/Flujo/MotivoPorPerspectiva.cs:21` |
| Reflejo conformado al cilindro y recortado | `Assets/Scripts/Flujo/ParedEspejo.Circular.cs:14` (nuevo) |
| Delegación del reflejo circular; piso/techo conservados | `Assets/Scripts/Flujo/ParedEspejo.cs` |
| Validación de luces funcionales de cada columna | `Assets/Editor/ValidarProyectoCrater.cs:201` |

Pruebas: `Assets/Tests/PlayMode/BordesYEncuadreTests.cs` (nuevo), `RevisionNoviembreTests.cs` y `CartelesYCieloTests.cs`. Se incluyen metadatos, escena, mallas y oclusión producidos por el constructor. Las imágenes originales, audio grabado y cambios anteriores se conservan.
