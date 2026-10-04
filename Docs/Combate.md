# Flujo de combate

## Entrada y elección de modo

La escena inicial de la compilación es `Menus`. Jugar carga `Escenario`, donde se elige **solitario**, **crear sala** o **unirse**. No se inicia la batalla mientras se está eligiendo modo o esperando conexión. En solitario no se inicializan los servicios de Relay y se oculta el selector.

## Reglas

- Cada ronda admite exactamente dos acciones por participante: dos ataques, o un ataque y un escudo, en cualquier orden.
- El plan queda cerrado al elegir la segunda acción. Una orden tardía o un segundo escudo se rechazan.
- Se compara acción 1 con acción 1 y luego acción 2 con acción 2. Piedra gana a tijera; tijera a papel; papel a piedra. Empate: cero daño.
- Ataque contra escudo: el ataque hace daño reducido. Dos escudos: cero daño. El escudo solo protege su propia pareja de acciones.
- El crítico multiplica el daño del ataque que acierta; no cambia quién gana piedra/papel/tijera.
- Cada ataque tiene una presentación de al menos 4 segundos. Después de las dos presentaciones de una pareja se muestra su resultado y se actualizan los PS una sola vez.
- Al llegar a cero PS se termina la partida y se cancela la segunda pareja, si estaba pendiente.
- Al agotar las rondas gana quien tenga más PS restantes; iguales PS producen empate. La escena conserva el límite de 10 rondas que tenía configurado.

## Controles

| Acción | Voz | Teclado |
| --- | --- | --- |
| Piedra | roca, piedra | 1 |
| Papel | hoja, papel | 2 |
| Tijera | tijera, tijeras, hidro pulso | 3 |
| Defensa | escudo, defensa, bloqueo | D o 0 |
| Ataque potente | voz por encima del umbral RMS | Mayús + 1/2/3 |
| Gesto preparado por el controlador/cámara | — | A; Mayús+A para potenciar |

También funcionan las teclas numéricas del teclado numérico. Espacio y H ya no cambian la vida de los personajes. El reconocimiento de voz usa Windows Speech; el umbral RMS se ajusta en `DetectorCommandControl`. Se descartan frases empezadas antes de abrir la selección o de reanudarla.

## Responsabilidades

- `ReglasCombate`: validación de planes y resolución determinista; no depende de Unity.
- `GestorNivel`: estado, selección, CPU, orden de presentación, actualización de vida, final y coordinación de red.
- `DetectorCommandControl`: voz/teclado; solo envía órdenes al gestor.
- `ControladorAnimaciones`: animaciones y VFX; nunca aplica daño. Retorna a Reposo y limpia los efectos entre acciones.
- `Vida` / `BarraVida`: PS exactos, límites y presentación numérica. Las referencias asignadas a Hydros e Ignis se conservan.
- `FeedbackAtaqueUI`: ronda, acciones, carteles, selector de modo y resultado final.
- `GestorRedRelay`: conexión, mensajes fiables y cierre de la sesión.

`RecibirDano`, `Victoria` y `Derrota` son animaciones opcionales: solo se activan si el Animator tiene esos parámetros. Los controladores actuales usan Atacar, AtacarCritico y Defendiendo (bool). No se inventan clips que aún no existen.

## Multijugador

El creador controla Hydros y el invitado controla Ignis. El host valida ambos planes y envía el intercambio cerrado; los dos equipos lo resuelven con las mismas reglas y parámetros. La siguiente ronda espera la confirmación de presentación de ambos. Se verifican remitente, sesión, ronda, acciones y vida para evitar duplicados o aplicar mensajes viejos. Una desconexión muestra partida interrumpida.

Ambos equipos deben usar esta misma versión. Relay necesita la configuración de Unity Services del proyecto y conexión a Internet. No se genera una CPU en una partida multijugador. La pausa de un menú local bloquea las órdenes del jugador; solo en solitario detiene el tiempo de la batalla.

## Supervisión y ajustes

`logsDetallados` en GestorNivel activa mensajes con ID de partida, ronda y estado. Se registran acciones aceptadas/rechazadas, planes cerrados, ganador de cada pareja, daño base, daño anterior/posterior al escudo, daño efectivamente aplicado, exceso sobre los PS restantes y vida antes/después. El cierre incluye resultado y motivo. Los mensajes de voz incluyen RMS, umbral y aceptación.

El daño base, reducción del escudo, multiplicador crítico, probabilidades de CPU y duraciones se ajustan en el Inspector. El redondeo de daño es al entero más cercano, con mitades hacia arriba: por ejemplo, 25 × 1,5 = 38; con escudo del 50 %, 19. El daño aplicado se limita a la vida restante.

Los parámetros de escena y las referencias existentes se preservan; los carteles y el selector se crean al ejecutar la escena. El panel de instrucciones forma parte de `Assets/Scenes/Canvas.prefab`.
