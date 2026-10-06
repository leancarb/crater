# CRÁTER — alas y Cruce reconstruidos

> Documento histórico. Para el diseño vigente del 6/10/2026, consultar [EstadoActualCrater.md](EstadoActualCrater.md) y [RevisionContemplativa.md](RevisionContemplativa.md). Las capturas de esta etapa pueden representar cambios posteriormente revertidos.


Documento histórico: las alas de esta propuesta se revirtieron. Sólo permanece el Cruce. El diseño vigente se describe en [EstadoActualCrater.md](EstadoActualCrater.md).

## Decisión de diseño

Se reemplazaron los tres recorridos del proyecto anterior desde los generadores, conservando las reglas de los filtros y el arte original. Las alas se pueden completar en cualquier orden. No se exige salto; las entradas y los atajos mantienen su conexión con la rotonda. El obelisco vuelve al centro de su plaza.

## SOL: patio, terraza y balcón

El primer patio gira el recorrido hacia el norte. Dos anclas juntas, visibles sobre la otra orilla, materializan un puente de tres metros. Hay seis segundos de retención para experimentar sin apuro. El abismo se extiende de pared a pared, por lo que no hay un borde sólido que permita evitar el problema.

Una rampa de cuatro metros de ancho sube a una terraza de 1,2 metros. Desde allí se reconoce el ancla lateral y la puerta de salida. Esta ancla abre permanentemente una puerta; el descenso lleva al siguiente patio. Las anclas de puentes y de puertas mantienen su diferencia visual original.

El balcón final separa las dos anclas sobre la otra orilla. Se debe barrer el encuadre, recordar la retención y cruzar el segundo puente. El sello se ve al llegar; activa el atajo y la cinemática de regreso. Se retiró la búsqueda del sello detrás del tabique anterior.

## LUNA: revelar, cambiar de encuadre y ascender

Una única reja estrecha presenta el filtro sobre suelo firme. Después aparece una galería de dos barreras desfasadas: abrir el frente permite entrar y reconocer un paso lateral; avanzar cambia la vista sobre la segunda piel. Los muros cierran los rodeos y los dinteles conservan la lectura de puerta.

El último paso sube por una rampa hacia un observatorio de 1,2 metros. Una reja situada en el ascenso se observa desde abajo y se disuelve antes de pasar. La placa lunar del sello queda arriba, frente al jugador. Un descenso lateral lleva directamente al atajo. Se reemplazaron la trampilla, la galería inferior y la escotilla del proyecto anterior.

## Cruce: combinación con un lugar para observar

La primera orilla presenta un ancla visible y otra en un nicho cubierto por una reja. Desde suelo firme, LUNA despeja el nicho y SOL activa el par. El puente está descentrado, por lo que el recorrido cambia de dirección antes de llegar al segundo patio.

El siguiente abismo tiene dos tramos separados por una isla sólida de 3,6 × 2 metros. El primer tramo responde a anclas en la orilla de partida. En la isla se puede esperar a que desaparezca ese puente: el piso sigue siendo seguro. Desde allí, LUNA revela las anclas del tramo de salida; SOL materializa el segundo puente y LUNA abre el paso final. Los tiempos de retención dejan margen para observar, cambiar de filtro y caminar.

La última antesala es tranquila y vuelve a encuadrar la entrada de la Cresta. El final conserva su regla de adaptación a la oscuridad.

## Recuperación y comunicación

Hay cuatro grutas con retorno por escalera, adaptadas a los nuevos abismos. Las losas se abren desde abajo y permanecen abiertas. Caer no borra sellos ni obliga a reiniciar. La isla del Cruce es una pausa segura, no un tramo de puente disfrazado.

Los murales usan el lenguaje gráfico original, se colocan junto a las reglas pertinentes y se conservan separados en las salas de los filtros. Las luces cálidas o frías encuadran destinos y los cambios de dirección revelan el siguiente problema. Se mantienen los hilos de progreso, los faros de regreso y el obelisco central.

## Fuente y validación

Fuente: `Assets/Editor/ConstructorCrater.Alas.cs`. Las vistas del Editor se ajustaron a las salas nuevas en `VistaPreviaCrater.cs`. Las pruebas de recorrido se actualizaron para caminar por las rampas, cruzar cada barrera y esperar en la isla con el puente apagado.

La reconstrucción validó referencias y alcance, y la compilación terminó sin errores. Las vistas del patio SOL, la galería LUNA, la isla y el obelisco fueron inspeccionadas. La comodidad y el interés del recorrido requieren la devolución del jugador: no se presentan como medidos por pruebas automáticas.

No se generó un ejecutable para esta actualización, siguiendo la preferencia del usuario. La demo previamente compilada corresponde al trazado anterior; la escena del Editor contiene el diseño nuevo.

## Resultado de validación

PlayMode: el recorrido completo hasta los créditos aprobó en 152,75 segundos. La ejecución del conjunto pasó nueve pruebas y detectó una pared del nicho que bloqueaba la escalera N2. Se movió la escalera y su abertura al lado opuesto; la prueba de las cuatro caídas aprobó después en 15,7 segundos. Las diez pruebas de juego quedan verificadas entre ambas ejecuciones (`Logs/alas-cruce-playmode-1.json` y `Logs/alas-cruce-recuperacion.json`). El ajuste de recuperación queda fuera del camino principal ya recorrido.

La reconstrucción final comprobó que el centro horizontal del obelisco coincide con (0,12), con tolerancia de un centímetro (`Logs/alas-cruce-reconstruccion.json`). Las pruebas técnicas se guardan en `Logs/alas-cruce-editmode.json`, y el estado final de compilación en `Logs/alas-cruce-consola-final.json`.
