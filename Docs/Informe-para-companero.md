# Pokimun · Cambios para el compañero

Observaciones de las pruebas del Lobby y del nivel de búsqueda de Ignis, junto con los cambios implementados en el menú y el combate.

## Lobby

- **Estanque de agua del Lobby.** Todavía se escucha desde demasiado lejos. Reducir su alcance y comprobar que se aprecie al acercarse.
- **Lava.** Verificar si el sonido está conectado y se reproduce. Si ya funciona, darle más presencia cerca del estanque.
- **Música de las estatuas.** Se distingue una capa musical cerca de la estatua de Ignis y otra cerca de la de Hydros. Ese comportamiento está bien. Al ajustar los ambientes, comprobar que las dos capas sigan distinguiéndose al recorrer el Lobby.
- **Portales.** Implementar un sonido propio para los portales, según lo acordado en la llamada de Discord. Queda por confirmar el clip y cuándo debe dispararse.
- **Introducción del Lobby Santuario.** Queda pendiente grabar e incorporar su propia locución de bienvenida.

## Búsqueda de Ignis

- **Charco de agua.** Su sonido se escuchó bien en la última prueba y queda aprobado por ahora.

- **Locución de Lautaro.** Mover el saludo que hoy se escucha al iniciar el Lobby para que se reproduzca al iniciar el nivel de búsqueda.
- **Pisadas en tierra.** Incorporar el sonido de los pasos al caminar sobre suelo de tierra expuesta. Las pisadas sobre el pasto del nivel de búsqueda se escuchan bien y quedan aprobadas.
- **Cofres del cobertizo.** Aplicar una apariencia de cofre al bloque de madera que está al costado. Darle la misma apariencia y el mismo sonido al cubo ubicado enfrente, en la posición simétrica. Comprobar que ambos se vean como cofres y reproduzcan el mismo efecto.
- **Arbustos.** El efecto actual de la vegetación suena mal durante el recorrido. Reemplazarlo por un SFX gratuito o por uno que produzca el equipo. Revisar el inventario de efectos antes de elegir para saber cómo queda el recuento de SFX del TP.
- **Flores.** Agregar un sonido breve al pasar entre ellas. Tomar como referencia el roce de arbustos mojados y darle menos intensidad. Probar ambos efectos en el mismo recorrido para comprobar que se distingan.
- **«¿Qué fue eso?» o «¿Qué es eso?».** Observación de Walter para conversar con Lautaro. Confirmar primero qué personaje dice la frase. Si es Ignis, a Walter le resulta redundante y propone quedarse con la risa que agregó Lautaro. Si es Hydros, revisar qué aporta la frase y la continuidad de su voz entre las tomas, porque se percibe un cambio entre la voz de Brian y la otra voz. La decisión queda abierta.

En la última escucha, los demás sonidos se percibieron bien implementados. La prioridad de esta revisión es corregir los arbustos, sumar el roce de las flores y conversar la observación sobre la frase.

## Cambios del equipo para comunicar

- **Portales del Lobby.** Se redujeron la intensidad y el alcance de las luces azul y roja. En Play Mode se verificó que los dos portales se distinguen y que se puede entrar al de combate.
- **Menú principal.** El fondo muestra una ronda real en solitario, grabada desde la cámara original del combate a 1080p y 60 FPS. Hydros se defiende y luego ataca. Después Ignis completa sus dos acciones y el turno vuelve a Hydros. El video dura unos 25 segundos y se reproduce en bucle detrás de la interfaz. Se añadió un fundido breve entre el final y el inicio. Se compararon los fotogramas del archivo actualizado y queda pendiente ver la transición en el menú de Unity. La música sigue saliendo del menú.
- **Audio de Unity y Wwise.** Se desactivó la creación automática del receptor de Wwise para que el menú y el combate conserven su receptor de Unity. El Lobby y la búsqueda ya tienen sus receptores de Wwise asignados.
- **Voz en el menú y el combate.** Los comandos de navegación se filtran según el panel activo y se descartan si la aplicación pierde el foco o si la frase empezó antes del cambio de panel. En el combate, las órdenes de pausa, reanudación, regreso y cancelación de conexión se habilitan según el estado de la partida. Se conservan las entradas por teclado.
- **Controles de combate en Android.** La pantalla permanece encendida mientras se juega. Los botones táctiles de ataque, defensa, cura y pausa aparecen durante el combate y se ocultan al pausar o salir de la pelea. Las acciones se habilitan cuando el jugador puede actuar.
- **Límite de combate.** El máximo de cinco minutos se aplica en Android y PC, en solitario y en línea. Se mide en tiempo real, incluso durante la pausa. Al agotarse, gana quien tenga más vida. Si ambos tienen la misma, hay empate.
- **Multijugador.** El anfitrión comunica el resultado por tiempo y el invitado sincroniza los puntos de vida con ese resultado. Si el rival envía su turno mientras todavía se anima el turno local, se guarda para procesarlo al terminar la animación. Cada equipo ve a Hydros como su personaje y a Ignis como rival. Para jugar juntos, ambos equipos necesitan una versión que use el protocolo 2.

## Pruebas pendientes

- Ver el empalme del video en el menú de Unity y comprobar su reproducción en Android.
- Probar los comandos de voz y los controles táctiles en un teléfono. Verificar el fin por tiempo en solitario y en una partida en línea entre Android y PC con la misma versión.
- Completar las tareas de sonido del Lobby y de la búsqueda enumeradas arriba. Los ajustes de voz descritos en este informe corresponden al menú principal y al combate.
