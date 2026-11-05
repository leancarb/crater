# Seguimiento del exterior y reflejo — 5/11/2026

- Inicio: Z local pasa de 193 a 197 (X local 20). Mundo `(-20; altura; -200,5)`, cuatro metros atrás, con el mismo cierre izquierdo y carteles. Se prolonga el sendero hasta la nueva posición. Constructor: `ConstructorCrater.InicioElevado.cs`.
- Montañas: `VisibilidadExterior.cs` deja de ocultar el paisaje por altura solamente. Ahora exige `PrologoCapilla.EnElCrater` y cámara por debajo de 7,5 m. Caminar por terrenos bajos del exterior no dispara el ocultamiento.
- Superficie sobre la rotonda: el constructor genera `Cubierta_Exterior_Rotonda`, de tierra, con colisión y sin oclusión estática horneada. `CubiertaExteriorRotonda.cs` conserva la cubierta mientras se está afuera y retira renderizador y collider al entrar. Se mantiene la abertura vista desde dentro.
- Reflejo: paredes, jambas, dintel y compuerta comparten el componente circular y una única malla de reflejo. Cada vértice se proyecta sobre el collider real, con 10 cm de separación; así el haz alcanza varias superficies a la vez. El reflector plano independiente de la compuerta queda deshabilitado. Se conservan intensidad 4, fundido de 0,25 s y apagado al apagar el farol.

Ambiente exterior: se recupera el WAV original `exteriorCapilla`, sin reemplazo por aire procedural. Volumen 0,20, frente al 0,28 que tenía ese sonido anteriormente (29 % menos). El ambiente interior mantiene su mezcla actual. No se modifica ningún WAV.

Los cambios de objetos y materiales se generan desde los constructores. Sin cambios manuales de escena/materiales, WAV o ilustraciones originales. La publicación en GitHub fue autorizada por el usuario al completar esta iteración; sin check-in de Unity Version Control.

Resultados de esta iteración: `Docs/Testing/Revision20261105/Seguimiento-EditMode.json` y `Seguimiento-Play.json`. Los informes sin prefijo corresponden a la iteración anterior del mismo día.

EditMode: 36/36. PlayMode afectado: 9/9. Validador: 0 errores. Se comprueban suelo continuo, bloqueo lateral y posibilidad de retroceder, visibilidad del paisaje exterior en terreno bajo, retirada de cubierta al entrar, alineación lateral y reflejo compartido de paredes y compuerta. No se ejecutó toda la suite histórica de PlayMode en este seguimiento.
