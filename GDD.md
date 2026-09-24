# CRÁTER — Game Design Document

> Versión 0.4 · Vertical slice · Unity 6 (6000.3) + URP (Forward+) · PC, teclado/mouse y joystick
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

El protagonista está en una **capilla**, de día. Empieza un **eclipse total de sol** y su luz revela un **cráter** en el valle que no estaba ahí: sólo existe mientras dura la totalidad. En su centro destella el contorno de una **puerta**.

El eclipse dura exactamente lo que el jugador permanezca adentro. El cráter es un espacio metafórico/onírico: no se explica, se atraviesa.

### Arco

| Momento | Qué pasa | Qué siente el jugador |
|---|---|---|
| **Prólogo** | Capilla de día. Al salir, empieza el eclipse (cinemática sin control): se callan los pájaros, sube el cráter, destella la puerta. | Curiosidad, extrañeza. |
| **Descenso** | La puerta del cráter lleva a la Explanada, en su borde. Una rampa baja al Umbral, donde está la linterna. | Descubrimiento. |
| **Aprendizaje** | El Campo entrega CUERPO; la Hondonada, HUECO. | Dominio, asombro. |
| **La oscuridad** | En la Cresta, la pared del fondo sólo devuelve el reflejo del propio foco. Al apagar la linterna y esperar, aparecen tallados ocultos y la puerta. | Revelación. |
| **Final** | Al cruzar la puerta, silencio y el **anillo de diamante**: todo se vuelve blanco. El jugador aparece en la capilla, de día. | Cierre sereno. |
| **Epílogo** | El cráter ya no está: sólo queda pasto aplastado. Quedarse ahí (o esperar un rato) trae los créditos. | Pérdida suave, confirmación. |

### El eclipse

- **Afuera** se ve en el cielo: el sol, la luna que lo tapa y la corona en la totalidad.
- **Adentro** la totalidad está congelada. Se ve por los **óculos** (Campo y Cresta): sol negro, corona plateada, cielo de noche.
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
| 2 | **HUECO** | Azul | Reja | Disuelve la materia hueca mientras se la ilumina: se vuelve atravesable. |

### 3.4 Adaptación a la oscuridad

Sólo en la Cresta. Con la linterna apagada, tras 3 s la exposición sube durante 10 s; encenderla la pierde en 1,2 s. El viento del óculo sube con la adaptación. Al 90 % se abre la puerta del eclipse.

### 3.5 La pared espejo y la puerta

- La pared del fondo de la Cresta es basalto pulido. **Con la linterna prendida** devuelve un reflejo encandilante del propio foco (del color del filtro), como una ventana de noche.
- **Con la linterna apagada**, el reflejo desaparece y se insinúa el contorno de la puerta, que crece con la adaptación.
- Adaptado, la hoja desaparece y detrás se ve la luz plateada del eclipse. Cruzarla es el final.

---

## 4. Recorrido

```
CAPILLA ─► EXPLANADA ─► UMBRAL ─► CAMPO ─► HONDONADA ─► CRESTA ─► (blanco) ─► CAPILLA ─► CRÉDITOS
(prólogo)               linterna  CUERPO   HUECO        oscuridad              (epílogo)
```

Cada filtro sigue el patrón **Enseñar → Probar → Torcer**.

| Sala | Contenido |
|---|---|
| **00 Capilla (prólogo)** | Libre, sin indicaciones. Al cruzar el umbral: cinemática del eclipse (~18 s). El cráter aparece en el valle, frente a la capilla; se entra por un hueco del borde hasta la puerta del centro. |
| **01 Explanada** | Borde del cráter, bajo la totalidad. Rampa hacia abajo. |
| **02 Umbral** | La linterna. Sostener la luz blanca sobre un ancla abre la compuerta. |
| **03 Campo** | CUERPO. Enseñar: dos anclas juntas. Probar: anclas separadas, barrer y cruzar con la retención. Torcer: anclas en el techo, hay que levantar la mirada. Óculo con el eclipse. |
| **04 Hondonada** | HUECO. Enseñar: una reja. Probar: zigzag. Torcer: puente con CUERPO hasta una repisa y reja con HUECO desde ahí. |
| **05 Cresta** | Apagar la linterna. Tallados latentes y la puerta del eclipse en la pared espejo. Óculo grande con el eclipse. |
| **06 Capilla (epílogo)** | De día, sin linterna. El cráter no está; en su lugar, pasto aplastado. Quedarse 5 s ahí, o esperar 2 min, trae los créditos. |

---

## 5. Dirección de arte

- **Kit modular low-poly** (Blender): anclas facetadas, rejas, puente, linterna, arquitectura con pilares y motivos tallados (espiral, serpiente escalonada, chakana, rombo).
- **Paleta:** basalto casi negro; ámbar para CUERPO y los tallados vivos; azul para HUECO y lo latente; plateado frío para todo lo que es eclipse (corona, puerta, óculos).
- **Capilla:** adobe, techo de paja y piedra, en un valle de tierra entre cerros. Es el único lugar con luz de día.
- **Post-procesado:** ACES, bloom, viñeta y grano fino. La adaptación usa la exposición.

## 6. Audio

Todo el audio se sintetiza en `GeneradorAudioCrater` y se puede reemplazar por WAV grabados con el mismo nombre.

| Momento | Sonido |
|---|---|
| Capilla | Pájaros y viento exterior |
| Eclipse | Los pájaros se callan de golpe; entra un grave |
| Cráter | Dron ambiente, viento del óculo que sube con la adaptación |
| Anclas | Cada una un tono; los pares forman intervalos del mismo acorde |
| Puerta del eclipse | El viento sale de la puerta; retumbo al abrirse |
| Final | Silencio → tono agudo del anillo de diamante → pájaros |

---

## 7. Estado

### Hecho
- [x] Recorrido completo, arte modular, interfaz, pausa, joystick
- [x] Filtros CUERPO y HUECO con sus puzzles
- [x] Prólogo en la capilla: cielo con el eclipse, cinemática, cráter del valle
- [x] Óculos con el eclipse congelado
- [x] Pared espejo y puerta del eclipse en la Cresta
- [x] Anillo de diamante, epílogo sin cráter y créditos
- [x] Validador y tests (EditMode y PlayMode del recorrido completo, prólogo incluido)

### Pendiente
- [ ] Arte de la capilla (hoy es blockout con los materiales del kit)
- [ ] Créditos definitivos (`SISTEMAS` → `EclipseFinalController` → `Creditos`)
- [ ] Playtest: duración de la cinemática, intensidades del cielo y del reflejo
- [ ] Audio grabado

### Descartado respecto del GDD anterior
- Filtro RASTRO y sus salas (Bifurcación, El Paso).
- La Lente.
- La pista forzada de la Galería: la Cresta ya indica apagar la linterna.
