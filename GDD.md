# CRÁTER — Game Design Document

> Versión 0.5 · Vertical slice · Unity 6 (6000.3) + URP (Forward+) · PC, teclado/mouse y joystick
>
> Esta versión del GDD describe el vertical slice con arte modular (rama `crater/linterna-y-final-adaptacion`) más el arco narrativo del eclipse. Los detalles de implementación están en el `README.md`.

---

## 1. Concepto

**En una frase:** durante un eclipse, un cráter que sólo existe bajo esa luz se abre frente a una capilla; adentro, una linterna con filtros le enseña al jugador a ver, hasta que descubre que para salir tiene que apagarla.

**Género:** puzzle ambiental en primera persona, contemplativo.
**Tono:** contemplativo, de asombro y descubrimiento. Sin combate ni amenazas. Caerse en una fosa sólo devuelve al último suelo seguro.
**Duración:** vertical slice (≈ 15–25 min).

### Pilares

1. **La luz es el verbo.** Toda interacción pasa por el haz: iluminar es actuar.
2. **Ver es un acto, no un dato.** Cada filtro es una forma de mirar la materia; el final obliga a mirar *sin* herramienta.
3. **Ritmo contenido.** Sin salto ni carrera. La tensión viene de entender, no de reaccionar.
4. **Interfaz mínima.** Una indicación breve abajo, el estado de la linterna arriba a la derecha y la mira con la barra de carga. El resto lo dicen el color, el sonido y el mundo. La capilla no tiene indicaciones.

---

## 2. Narrativa

### Premisa

El protagonista está en una **capilla**, de día. Empieza un **eclipse total de sol** y su luz revela un **cráter** en el valle que no estaba ahí: sólo existe mientras dura la totalidad. En su borde destella el contorno de una **puerta**.

El eclipse dura exactamente lo que el jugador permanezca adentro. El cráter es un espacio metafórico/onírico: no se explica, se atraviesa.

### Arco

| Momento | Qué pasa | Qué siente el jugador |
|---|---|---|
| **Prólogo** | Capilla de día. Al salir, empieza el eclipse (cinemática sin control): se callan los pájaros, todo queda en un crepúsculo profundo, la tierra se abre en un pozo, sube el borde del cráter y destella la puerta. | Curiosidad, extrañeza. |
| **Descenso** | Sin cortes: se cruza la puerta y se baja por una escalera hasta la Explanada, en el fondo del pozo. Un pasillo sigue al Umbral, donde está la linterna. | Descubrimiento. |
| **Aprendizaje** | La Rotonda abre dos alas, en cualquier orden: la oeste entrega CUERPO, la este HUECO. Cada una termina en un sello. Con los dos, el Cruce pide usar los filtros juntos. | Dominio, asombro. |
| **La oscuridad** | Al entrar a la Cresta, una losa cierra el corredor a la espalda: no hay vuelta. La pared del fondo sólo devuelve el reflejo del propio foco. Al apagar la linterna y esperar, aparecen tallados ocultos. | Revelación, encierro. |
| **Final** | Adaptado a la oscuridad, el techo se abre y entra la luz blanca del fin del eclipse hasta inundarlo todo. Silencio, **anillo de diamante**, y el jugador aparece en la capilla, de día. | Cierre sereno. |
| **Epílogo** | El cráter ya no está: sólo queda pasto aplastado. Quedarse ahí (o esperar un rato) trae los créditos. | Pérdida suave, confirmación. |

### El eclipse

- **Afuera** se ve en el cielo: el sol y la corona en la totalidad. No es noche cerrada sino un crepúsculo profundo: cielo azul oscuro, el horizonte cálido alrededor y el paisaje todavía visible. La luna nueva no se ve de día: sólo su silueta cuando pasa por delante del sol. En el epílogo ya pasó y quedó del otro lado.
- **Adentro** la totalidad está congelada. Se ve por los **óculos** (Rotonda y Cruce): sol negro, corona plateada, cielo de noche.
- **Termina con el anillo de diamante**, el destello real con el que cierra la totalidad: la luz vuelve de golpe, como al prender la luz después de estar a oscuras.

### Tema

La herramienta que nos permite ver también define lo que vemos. La linterna enseña dos maneras de mirar la materia, pero la última puerta sólo aparece cuando el jugador deja de iluminar.

---

## 3. Mecánicas

### 3.1 Controles

| Acción | Teclado y mouse | Joystick |
|---|---|---|
| Moverse / mirar | WASD · mouse | stick izq. · stick der. |
| Linterna | F | RB |
| Filtro CUERPO / HUECO | 1 / 2 | X / Y |
| Luz blanca | Q | B |
| Pausa (R reinicia, X sale) | Esc | Start |
| Saltar la cinemática (desde la 2.ª vez) | Espacio | — |

### 3.2 Linterna

- Se encuentra en el Umbral. Luz blanca: abre la compuerta del Umbral.
- **Un filtro a la vez.** Cambiar tarda 0,6 s y durante el cambio el haz casi se apaga.
- Ilumina todos los receptores dentro del cono con línea de vista; cargarlos lleva 0,4 s.

### 3.3 Filtros

| # | Filtro | Color | Receptor | Qué hace |
|---|---|---|---|---|
| 1 | **CUERPO** | Ámbar | Ancla de basalto → Puente de luz | Enciende anclas. Con todas las anclas de un puente encendidas, el puente se materializa. Las anclas retienen unos segundos al perder el haz. |
| 2 | **HUECO** | Azul | Reja | Disuelve la materia hueca mientras se la ilumina: se vuelve atravesable. Una reja sólida tapa la luz: lo que está detrás no se enciende. También hay rejas acostadas (trampillas en el piso, escotillas en el techo). |

**Sellos:** al final de cada ala. Una vez encendidos quedan así para siempre; abren un atajo a la Rotonda y, los dos juntos, la puerta del norte.

**Puertas sostenidas:** se abren sólo mientras sus anclas estén encendidas; la retención da unos segundos para pasar. Nunca se cierran encima del jugador.

### 3.4 Adaptación a la oscuridad

Sólo en la Cresta. Con la linterna apagada, tras 3 s la exposición sube durante 10 s; encenderla la pierde en 1,2 s. El viento del óculo sube con la adaptación. Al 90 % se abre el techo.

### 3.5 La pared espejo, el encierro y el techo

- La pared del fondo de la Cresta es basalto pulido. **Con la linterna prendida** devuelve un reflejo encandilante del propio foco (del color del filtro), como una ventana de noche.
- **Con la linterna apagada**, el reflejo desaparece y aparecen los tallados latentes.
- Al entrar a la Cresta, una losa sube del piso y cierra el corredor.
- Adaptado, el techo se abre y entra la luz blanca del fin del eclipse hasta inundarlo todo.

---

## 4. Recorrido

```
                                          ┌─ ALA OESTE (CUERPO) ─┐
CAPILLA ─► EXPLANADA ─► UMBRAL ─► ROTONDA ┤                      ├─ sellos ─► CRUCE ─► CRESTA ─► (blanco) ─► CAPILLA ─► CRÉDITOS
(prólogo)               linterna          └─ ALA ESTE (HUECO) ───┘            (los dos)  oscuridad              (epílogo)
```

Cada ala sigue el patrón **Enseñar → Probar → Torcer**. Se hacen en cualquier orden; el sello del final abre un atajo de vuelta a la Rotonda.

| Sala | Contenido |
|---|---|
| **00 Capilla (prólogo)** | Libre, sin indicaciones. Al cruzar el umbral: cinemática del eclipse (~18 s). El cráter se abre en el valle, frente a la capilla: es un pozo y el resto del nivel está bajo tierra. La puerta está en el borde, del lado de la capilla. |
| **01 Explanada** | El fondo del pozo, bajo la totalidad. Se llega por la escalera de la puerta; un pasillo sigue al Umbral. |
| **02 Umbral** | La linterna. Sostener la luz blanca sobre un ancla abre la compuerta. |
| **03 Rotonda** | Sala central con óculo. Dos alas a los lados; al norte, la puerta de los sellos con dos tallados que se encienden con cada sello. |
| **03a Ala oeste** | CUERPO. Enseñar: un abismo y dos anclas juntas. Probar: una puerta que se sostiene con dos anclas lejanas (encender una, barrer a la otra y pasar con la retención). Torcer: las anclas cuelgan del techo sobre el otro lado de un pozo. Sello detrás de un tabique. |
| **03b Ala este** | HUECO. Enseñar: un muro de rejas. Probar: la sala no tiene salida; el camino es una trampilla en el piso que da a una galería de abajo, con otra reja. Torcer: una rampa que termina contra una escotilla de reja en el techo. Sello: una placa de materia hueca que queda disuelta. |
| **04 Cruce** | Los dos filtros. Un ancla del puente está detrás de una reja: disolverla, cambiar a CUERPO y encenderla antes de que se cierre. Después, un puente sostenido por anclas que quedan a la espalda termina contra una reja: parado arriba, cambiar a HUECO y pasar antes de que se apague. |
| **05 Cresta** | Se cierra a la espalda. Apagar la linterna: tallados latentes y la pared espejo. El techo se abre y entra la luz blanca. |
| **06 Capilla (epílogo)** | De día, sin linterna. El cráter no está; en su lugar, pasto aplastado. Quedarse 5 s ahí, o esperar 2 min, trae los créditos. |

---

## 5. Dirección de arte

- **Kit modular low-poly** (Blender): anclas facetadas, rejas, puente, linterna, arquitectura con pilares y motivos tallados (espiral, serpiente escalonada, chakana, rombo).
- **Paleta:** basalto casi negro; ámbar para CUERPO y los tallados vivos; azul para HUECO y lo latente; plateado frío para todo lo que es eclipse (corona, puerta, óculos).
- **Capilla:** capilla andina de adobe encalado. Zócalo de piedra, contrafuertes, techo de paja a dos aguas, espadaña con campana y cruz, óculo sobre la puerta. Adentro: vigas, bancos, retablo y velas. Adelante, un atrio con pirca, arco de ingreso y cruz atrial. Alrededor: cardones, paja brava y cerros facetados. Es el único lugar con luz de día.
- **Post-procesado:** ACES, bloom, viñeta y grano fino. La adaptación usa la exposición.

## 6. Audio

Todo el audio se sintetiza en `GeneradorAudioCrater` y se puede reemplazar por WAV grabados con el mismo nombre.

| Momento | Sonido |
|---|---|
| Capilla | Pájaros y viento exterior; la campana de la espadaña suena al empezar |
| Eclipse | Los pájaros se callan de golpe; entra un grave; retumbo mientras sube el cráter; brillo agudo cuando destella la puerta |
| Cráter | Dron ambiente, viento del óculo que sube con la adaptación |
| Anclas | Cada una un tono; los pares forman intervalos del mismo acorde |
| Techo de la Cresta | Retumbo al abrirse; después, silencio |
| Final | Silencio → tono agudo del anillo de diamante → pájaros y la campana |
| Créditos | Acorde lento en La mayor, el mismo de las anclas |

---

## 7. Estado

### Hecho
- [x] Recorrido completo, arte modular, interfaz, pausa, joystick
- [x] Filtros CUERPO y HUECO con sus puzzles
- [x] Prólogo en la capilla: cielo con el eclipse, cinemática, cráter del valle
- [x] Óculos con el eclipse congelado
- [x] Pared espejo, encierro y techo que se abre en la Cresta
- [x] Entrada continua: el cráter es un pozo en el valle
- [x] Anillo de diamante, epílogo sin cráter y créditos
- [x] Validador y tests (EditMode y PlayMode del recorrido completo, prólogo incluido)

### Pendiente
- [x] Arte de la capilla y del valle (low-poly generado por el constructor)
- [ ] Créditos definitivos (`SISTEMAS` → `EclipseFinalController` → `Creditos`)
- [ ] Playtest: duración de la cinemática, intensidades del cielo y del reflejo
- [ ] Audio grabado

### Descartado respecto del GDD anterior
- Filtro RASTRO y sus salas (Bifurcación, El Paso).
- La Lente.
- La pista forzada de la Galería: la Cresta ya indica apagar la linterna.
