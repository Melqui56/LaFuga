# La Fuga — nivel jugable en Unity

Nivel de escape en primera persona para el ACA I de **Diseño y construcción de
ambientes interactivos en motores de videojuegos** (CUN, 26ES5, Bloque I).

**Autores:** Melquisedec Casallas Escobar y Manuel Alejandro Domínguez Guerrero
**Unity:** 6000.5.6f1 (si no abre en esta versión, no se instala otra)

## Qué es el nivel

Un túnel de servicio se inunda. El jugador entra por la boca del túnel, sella
fugas con la pistola de sellador, salta el pozo de drenaje y gira dos válvulas
para abrir la compuerta. El nivel termina cuando gira la rueda de cierre y el
agua empieza a bajar.

Cinco zonas: **A** Boca del túnel (entrada y tutorial), **B** Galería de bombas
(válvula 1 y fugas), **C** Pozo de drenaje (salto mortal), **D** Compuerta
(válvula 2 y el gate) y **E** Cámara de la compuerta (salida).

Se puede perder de dos maneras: el agua alcanza al jugador o cae al pozo. La
dificultad crece porque el agua sube más rápido en cada tramo y cada fuga sin
sellar la acelera.

## Cómo abrirlo

1. Unity Hub → **Add** → seleccionar esta carpeta → abrir con **6000.5.6f1**.
2. En el Project, abrir `Assets/Scenes/LaFuga.unity`.
3. Presionar **Play**.

Controles: **WASD** para moverse, **Shift** para correr, **Espacio** para
saltar, **clic izquierdo** para sellar una fuga y **E** para usar válvulas,
puerta y rueda. **Esc** libera el cursor.

## Cómo está construido

El nivel no se armó a mano en el editor: se genera por código desde un solo
archivo de datos, `Assets/nivel.json`. Ese archivo define las zonas, los muros,
las plataformas, los interactivos, las fugas, las luces, la ruta y el ritmo del
agua. El menú **La Fuga → Construir nivel** reconstruye la escena completa y
siempre produce el mismo resultado.

La ventaja es que el plano del documento y el nivel construido no pueden
diferir: los dos salen del mismo archivo.

## Estructura

```
Assets/
├── nivel.json              fuente de verdad del nivel
├── Audio/                  seis efectos sintetizados (válvula, puerta, rueda,
│                           fuga, sello y agua de fondo)
├── Editor/
│   ├── ConstructorNivel.cs genera la escena desde nivel.json
│   ├── Grabador.cs         graba un recorrido y guarda un PNG por cuadro
│   └── PruebaLogica.cs     comprueba valvulas, puerta y rueda sin render
├── Scenes/LaFuga.unity     la escena del nivel
└── Scripts/
    ├── Jugador.cs          movimiento, cámara y disparo con cooldown
    ├── Interactivo.cs      válvulas, puerta de compuerta y rueda
    ├── Fuga.cs             fuga que se sella con el disparo
    ├── GestorNivel.cs      agua que sube, derrota, victoria y reinicio
    └── BotGrabacion.cs     recorrido automático para el video de evidencia
```

## Interactivos y retroalimentación

| Elemento | Función | Animación | Sonido |
|---|---|---|---|
| Válvula 1 y 2 | Habilitan la compuerta | Giro de 90° | Golpe metálico y agua |
| Puerta de compuerta | Gate: solo abre con las dos válvulas | Sube 2,6 m | Motor y chirrido |
| Rueda de cierre | Detiene el agua y cierra el nivel | Giro continuo | Agua que cesa y campana |
| Fugas | Si no se sellan, el agua sube más rápido | El chorro se corta | Chorro y siseo |

## Notas

- El proyecto no tiene errores de compilación y no depende de rutas locales.
- `Library/`, `Temp/`, `Logs/` y `UserSettings/` están fuera del repositorio.
- Los diagramas y el documento se generan desde `nivel.json` con los scripts
  de la carpeta `fuente/` del entregable.
