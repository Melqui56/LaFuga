# La Fuga — nivel jugable en Unity

Nivel de escape en primera persona para el ACA 2 de **Diseño y construcción de
ambientes interactivos en motores de videojuegos** (CUN, 26ES5, Bloque I).

**Autores:** Melquisedec Casallas Escobar y Manuel Alejandro Domínguez Guerrero
**Unity:** 6000.5.6f1

## El nivel

Un operario queda atrapado en un túnel de drenaje que se inunda. Tiene que
sellar las fugas con la pistola de sellador, saltar el pozo, girar dos válvulas
para abrir la compuerta y girar la rueda que detiene el agua. La rueda levanta
la compuerta del **Colector 3**, vigilado por guardias, que hay que cruzar
hasta la **escotilla verde**.

Zonas: **A** Boca del túnel, **B** Galería de bombas, **C** Pozo de drenaje,
**D** Compuerta, **E** Cámara de la compuerta, **F** Colector 3 y **G** Salida.

Se pierde si el agua cubre al jugador, si cae al pozo o si los guardias le
quitan toda la salud. La dificultad crece por tramo: el agua sube más rápido y
los guardias son más rápidos y ven más lejos. Al entrar al colector hay un
punto de control.

## Jugar

- **Build:** descomprimir `LaFuga_Windows.zip` y ejecutar `LaFuga.exe`, o
  `LaFuga_Linux.zip` y ejecutar `LaFuga.x86_64`.
- **Proyecto:** Unity Hub → Add → esta carpeta → abrir con 6000.5.6f1 →
  `Assets/Scenes/LaFuga.unity` → Play.

Controles: **WASD** moverse, **Shift** correr, **Espacio** saltar,
**clic izquierdo** sellar fugas, **E** usar válvulas, puerta y rueda,
**Esc** liberar el cursor.

## Scripts del juego

| Script | Qué hace |
|---|---|
| `Jugador.cs` | Movimiento, cámara, salto y disparo del sellador |
| `Fuga.cs` | Fuga que acelera el agua hasta que se sella |
| `Interactivo.cs` | Válvulas, puerta de la compuerta y rueda |
| `GestorNivel.cs` | Agua, objetivos, punto de control, derrota y victoria |
| `EnemigoIA.cs` | Guardia con NavMesh: patrulla, persecución y ataque |
| `ArmaEnemigo.cs`, `EventosArma.cs` | Daño del golpe sincronizado con la animación |
| `SaludJugador.cs` | Salud del jugador |
| `HUD.cs` | Salud, riesgo del agua, válvulas, fugas y objetivo actual |
| `SalidaNivel.cs` | Escotilla de salida |

El guardia usa `Assets/ACA2/Guardia.controller`, con un Blend Tree 1D
(*Locomocion*) controlado por el parámetro `Velocidad`.
