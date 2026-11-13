# Traspaso del proyecto Pokimun

Este archivo resume el estado del repositorio Unity, los avances de diseño sonoro y las pruebas pendientes para continuar el proyecto desde otro chat de Codex.

## Punto de partida en Git

- La última revisión de `main` que se comprobó es `b8c676f`, merge de la PR #27 de mejoras de sonido de Lautaro. La PR #26, merge `91855ed`, incorporó las correcciones de Android y el menú animado.
- Al preparar este informe, la migración a Wwise, los sonidos y la animación de botones, las correcciones del Lobby, los efectos de investigación, la bienvenida y los bancos regenerados eran **cambios locales sin commit**. Para que aparezcan en otra computadora, hay que compartir la rama que los contenga o transferir esos archivos junto con el repositorio.
- El equipo usa GitHub Desktop y ramas con prefijos como `feature/`, `fix/` y `chore/`. Para trabajo nuevo, partir de un `main` actualizado.

## Cambios que hicimos

- **Menú principal.** El video de fondo `Assets/Resources/FondoDuelo.webm` muestra una ronda grabada desde la cámara del combate a 1080p y 60 FPS. Hydros se defiende y ataca, Ignis completa sus dos acciones y el turno vuelve a Hydros. Dura unos 25 segundos y tiene un fundido breve en el empalme del bucle. Unity reproduce el video y Wwise reproduce la música.
- **Android y voz.** La pantalla permanece encendida durante el combate en Android. Los controles táctiles de ataque, defensa, cura y pausa se muestran durante la pelea y se ocultan al pausar o salir de ella. En el menú y el combate, las órdenes de navegación por voz se filtran según el panel activo y el foco de la aplicación. Se conservó la entrada por teclado. El pulido de reconocimiento de voz en Lobby y Búsqueda queda por revisar aparte de las voces grabadas de los personajes.
- **Combate y multijugador.** Límite de cinco minutos en PC y Android, en solitario y en línea, medido en tiempo real incluso durante la pausa. Gana quien conserva más vida y hay empate si ambos tienen la misma. Con Relay, el anfitrión comunica el resultado por tiempo y los equipos necesitan la misma versión del protocolo de combate (v2). Si un turno remoto llega durante la animación local, se procesa después. Cada equipo ve a Hydros como su personaje e Ignis como rival. Falta probar una partida cruzada entre dispositivos.
- **Iluminación de los portales del Lobby.** La primera prueba redujo las luces puntuales y activó la emisión de los materiales. Como el apagado persistió, ambas superficies pasaron a URP Unlit, con color naranja para combate y azul para búsqueda. La superficie mantiene su color sin depender de luces dinámicas. Las luces puntuales siguen iluminando el entorno con intensidad 2,5 y alcance 7 m. El generador `Assets/Editor/AcabadoEntorno.cs` reproduce esta configuración. Queda probar ambos portales desde distintos ángulos en Play Mode.
- **Pausa del Lobby.** Se quitó «Volver al lobby» y se cerró el espacio entre botones. La Búsqueda de Ignis conserva esa opción.
- **Efectos visuales de portales.** Ambos usan el shader sin iluminación dinámica `Assets/Exploracion/Mejoras/PortalSantuario.shader`. El portal naranja de combate tiene brasas ascendentes y el portal azul de búsqueda tiene corrientes ondulantes y partículas que se desplazan de lado. Los dos tienen un borde luminoso. Queda revisar los efectos en Play Mode y en Android.
- **Migración a Wwise.** Se migraron ocho audios nativos del menú y del combate. Son `Menu_Music.wav`, `Escenario.wav`, `BasicoAgua.wav`, `CriticoAgua.wav`, `BasicoFuego.wav`, `CriticoFuego.wav`, `victoria.wav` y `derrota.wav`. Se conservaron los retrasos de los ataques y la música de combate sube de tono cuando alguno tiene 30 % de vida o menos. Los controles del menú y de pausa envían los ajustes a Wwise mediante `Volumen_Musica` y `Volumen_Efectos`. La captura del micrófono para los comandos sigue a cargo de Unity.
- **Sonidos del menú principal.** Se rediseñaron a partir de la música de fondo y de la textura de madera de los botones. Al pasar el mouse suena un roce de madera derivado del golpe de confirmación, una octava más arriba y con un ataque más suave. Su resonancia en Fa4 coincide con una nota presente en la música del menú. Al presionar o tocar se escucha un golpe amortiguado, editado a partir del sonido de la caja de madera del juego, con una resonancia en Fa3. Los [MIDI editables](Audio/) guardan las notas. Los WAV mono de 48 kHz y 24 bits duran 0,084 s y 0,168 s y tienen picos de −20 y −13 dBFS, respectivamente. Los eventos 2D `Play_SFX_Menu_Hover` y `Play_SFX_Menu_Click` siguen pasando por el bus SFX. Se regeneraron los bancos de Windows, Linux y Android.
- **Interfaz de combate.** Se restauró `FeedbackAtaqueUI.ActualizarEstado` tras el error CS1061. El método actualiza la ronda y las acciones en pantalla. Se comprobó que estén presentes los métodos que llama `GestorNivel`.

Los efectos de botones se aplican también en Opciones, Tutorial y Créditos, incluidos los botones para volver al menú principal.

Al cerrar un panel, `BotonAnimado` restablece la escala original y el estado del puntero. Queda comprobar en Play Mode que el botón no siga agrandado al volver de Opciones o Tutorial.

El roce de madera usa el subbus `UI_Hover` de Wwise. Al reproducirse atenúa el bus de música 2 dB en 25 ms y, al terminar, el nivel se recupera en 150 ms. La pulsación usa el bus SFX directamente.

## Avances de sonido reportados por el compañero

La base de esta sección son los tres informes compartidos, `Informe_SFX_Pokimun.docx`, `Pokimun — Desarrollo del diseño sonoro.docx` y `Pokimun — Informe de avance del TP de sonido.pdf`. Se pueden adjuntar al chat nuevo si hace falta revisar la consigna, las capturas o el proceso de producción.

- **Lobby Santuario.** El equipo reportó que el estanque de Hydros quedó como emisor 3D con alcance de 12 m y la lava, con 20 m. Los portales comparten un ambiente en bucle y tienen un efecto 2D independiente al cruzarlos. La música interactiva de **Santuario** usa cuatro capas sincronizadas a 72 BPM. A la base se suman capas según la cercanía a la estatua de Ignis, a la de Hydros o al portal de búsqueda.
- **Búsqueda de Ignis.** Hay un ambiente de pájaros en bucle y emisores de abejas cerca de flores. Se reportaron una puerta doble, dos cofres interactivos, pisadas por superficie y los roces de arbusto V3 y flores V2. En la escucha anterior, el charco del cobertizo y los pasos sobre pasto estaban bien. La música de **Búsqueda** cambia entre Exploración, Tensión y Hallazgo al siguiente compás.
- **Voces grabadas.** La bienvenida de Lautaro figura al iniciar la búsqueda. Las voces de Ignis se emiten en 3D desde su escondite y las de Hydros se reproducen en 2D. Los informes atribuyen «¿Qué fue eso?» a Ignis. Walter propuso conservar solo la risa y la decisión sigue abierta.
- **Bienvenida propia del Lobby Santuario.** Se editó en REAPER con un filtro paso alto a 100 Hz, ReaVerbate con tamaño de sala 60, señal procesada a −7,9 dB y retraso inicial de 25 ms, y 2,52 dB de ganancia en la pista. El WAV estéreo de 48 kHz y 24 bits dura 6,02 s, incluida la cola, y su pico es de −5,6 dBFS. Wwise lo reproduce en 2D por el bus Voces con el evento `Play_VO_Bienvenida_Lobby`, 3,5 s después de la primera entrada al Lobby de cada sesión. La sesión `ReaperLocal/BienvenidaLobby.rpp` y sus tomas están excluidas de Git. El WAV que usa el juego está en `Pokimun_WwiseProject/Originals/SFX/TP_SFX/VO_Bienvenida_Lobby.wav`. La locución corresponde al apartado 2.e de la consigna.
- **Producción y mezcla.** Los efectos se editaron en REAPER con grabaciones propias y material de banco. Se exportaron WAV a 48 kHz y 24 bits, principalmente en mono. Wwise organiza la mezcla en buses de Música, Ambiente, SFX y Voces. El bus de Voces baja la música al hablar. El inventario del TP reportó 15 SFX frente a los 14 solicitados. El informe de SFX documenta cuatro efectos enlazados con cuatro capas cada uno frente a cinco requeridos por la consigna.

## Qué verificar al retomar

- **Capas nuevas de música.** Se guardaron los MIDI definitivos en [Docs/Audio](Audio/). Se renderizaron las pistas adicionales con síntesis y se integraron como cuatro WAV separados en el proyecto Wwise. Santuario suma una percusión grave de fondo en su segmento de ocho compases a 72 BPM. Búsqueda añade percusión suave en Exploración, un fondo grave sostenido en Tensión y un sostén grave en Hallazgo, a 96 BPM. Se conservaron los scripts, los parámetros de proximidad y los cambios de estado de las escenas. Los bancos generados para Windows, Linux y Android incluyen las cuatro pistas. Para el informe final, capturar el segmento de Santuario con sus cinco pistas y los tres segmentos de Búsqueda con sus capas adicionales. Queda escuchar el balance y comprobar los empalmes en Unity.

- **Cierre de los cofres.** La grabación del compañero `Cofre_Cerrando.wav` se incorporó al proyecto Wwise como `Play_SFX_Cofre_Cerrando`. Suena en 3D desde cada cofre al empezar el cierre automático. La parte más fuerte de la toma coincide aproximadamente con el final del movimiento de la tapa. Queda probar la sincronización en ambos cofres y reunir la sesión o captura de las cuatro capas si se incluye como quinto SFX de la consigna.

- **Vegetación.** Se bajaron los roces en Wwise 10 dB para arbustos y 13 dB para flores. Comprobar la diferencia al pasar junto a ambos en el mismo recorrido.
- **Pasos en el jardín trasero del Lobby.** La franja «Paso de tierra trasero» usa `Play_SFX_Paso_Tierra_Lobby`. El WAV mono de 48 kHz y 24 bits se editó a partir del tramo 8,75–9,11 s de la preescucha pública de `soil_14a_lightshoes_walk.wav`, de sturmankin, CC0. Al salir de la franja, el piso vuelve a usar los pasos de piedra. [Fuente y licencia](https://freesound.org/people/sturmankin/sounds/278914/)
- **Volumen al investigar.** Se subieron 4 dB en Wwise el toque de madera del tronco y el tocón, y el impacto de concreto de las ruinas.
- **Investigación del tronco, el tocón y las ruinas.** El tronco caído y el tocón usan `Play_SFX_Investigar_Tronco`, un toque seco de madera. Las ruinas usan `Play_SFX_Investigar_Ruinas`, un impacto de concreto roto. Ambos son eventos 3D con material CC0 y se disparan al investigar. El usuario confirmó que los efectos sonaban bien antes de pedir la subida de 4 dB. Queda escuchar el balance con ese último ajuste. Los MIDI de la propuesta sintetizada siguen en `Docs/Audio`.

1. Abrir la rama que incluya los cambios locales y confirmar que Unity termine de compilar. En la última validación por lotes, Unity se cerró antes de compilar. Se generaron los bancos de Windows, Linux y Android y se verificaron los eventos y sus medios en disco. La escucha en juego sigue pendiente.
2. En Play Mode, escuchar el video en bucle con su música, recorrer los botones del menú con el mouse y probar la confirmación táctil en Android. Comprobar música, ataques y resultados del combate. Mover los controles de música y efectos en menú y pausa y comprobar que el nivel persista al cambiar de escena.
3. Recorrer el Lobby y la Búsqueda. Escuchar la bienvenida del Lobby y el cambio de piedra a tierra al cruzar la franja trasera. Comprobar el alcance del estanque, la presencia de la lava, los portales y las capas musicales. Revisar los cofres, los roces de plantas y las pisadas sobre tierra expuesta en Búsqueda. Para esa zona de Búsqueda, el PDF enumera las pisadas como implementadas y también deja pendiente verificar el archivo y la zona.
4. Probar comandos de voz, teclado y controles táctiles. Verificar el fin por tiempo y una partida Relay entre Android y PC con el mismo protocolo. Probar Linux y Windows según disponibilidad. La preparación del proyecto y la generación de ejecutables son pasos separados.
5. Para la entrega de sonido, seleccionar los 14 SFX de la tabla final (7 en objetos, 5 por acciones y 2 extradiegéticos). Si el cierre del cofre va a ser el quinto SFX de cuatro capas, combinar la grabación propia con otras tres capas y documentarlas. Revisar si se exige primera persona también en el Lobby y el alcance del ambiente general en bucle. Completar el PDF con los nombres completos, créditos y capturas de REAPER, Wwise y Unity. Reunir el ejecutable y los WAV en `TPFinal_grupo1` como ZIP o RAR. Si pesa más de 50 MB, entregar un TXT con el enlace. Revisar con la cátedra el criterio de autoría y uso de IA que señala el PDF.

## Fuentes de biblioteca para tronco y ruinas

El apartado 2.d de la consigna permite sonidos descargados en las capas posteriores a la primera. Al menos cinco de los 14 SFX deben tener cuatro capas como mínimo y empezar con Foley grabado por integrantes del grupo. El enunciado pide diseño propio para cada efecto y capturas del proceso por capas en el PDF. Los candidatos de biblioteca pueden editarse y combinarse con otras tomas para el tronco y las ruinas. Sus datos de origen quedan registrados abajo.

- **Tronco caído.** `knock.wav`, de UberBosser, CC0. Es un solo toque de madera. [Ficha y licencia en Freesound](https://freesound.org/people/UberBosser/sounds/421585/)
- **Ruinas.** `Concrete Breaks Several Denoised`, de loganzsound, CC0. Es una grabación de trozos de concreto al golpear el suelo. [Ficha y licencia en Freesound](https://freesound.org/people/loganzsound/sounds/843339/)

Wwise usa versiones editadas de las preescuchas públicas MP3 de alta calidad. Se exportaron en WAV mono de 48 kHz y 24 bits con filtros, fundidos y picos de −14 dBFS. El toque del tronco dura 0,164 s. Las ruinas usan el recorte de 0,090 a 0,550 s. Estos efectos de biblioteca se documentan como SFX por acción.

## Rutas para trabajar en Codex

| Qué revisar | Ruta |
|---|---|
| Escenas principales | `Assets/Scenes/Menus.unity`, `Lobby.unity`, `BusquedaIgnis.unity` y `Escenario.unity` |
| Menú, botones y pausa de exploración | `Assets/Prefabs/Menus/Scripts/MenuManager.cs`, `Assets/Scripts/BotonAnimado.cs`, `Assets/Scripts/Exploracion/InterfazExploracion.cs` |
| Audio y volumen de Wwise | `Assets/Scripts/Audio/SFX/AudioGlobal.cs` y `AudioTP.cs` |
| Combate y Relay | `Assets/Scripts/GestorNivel.cs`, `GestorRedRelay.cs`, `ControladorAnimaciones.cs` y `FeedbackAtaqueUI.cs` |
| Reconocimiento de voz | `Assets/Scripts/GestorVozMultiplataforma.cs`, `ControladorVozMenu.cs` y `DetectorCommandControl.cs` |
| Proyecto, eventos y bancos de Wwise | `Pokimun_WwiseProject/Pokimun_WwiseProject.wproj`, `Events/SFX_TP.wwu`, `Containers/SFX_TP.wwu` y `GeneratedSoundBanks/` dentro del proyecto Wwise |
| Fuentes de audio del menú y del combate | `Docs/Audio/` para los MIDI y `Pokimun_WwiseProject/Originals/SFX/MenuCombate/` para los WAV |

Los ocho WAV migrados todavía pueden estar duplicados en `Assets/Audios`. Se comprobó que las escenas y los scripts ya no referencian esas copias. Si se limpian después de probar el audio, conservar los WAV de `Originals` que usa Wwise para regenerar bancos.

Para iniciar el otro chat, abrir el repositorio en Codex, adjuntar este informe y pedir que verifique primero la rama y el estado local. Una vez confirmada la versión, puede empezar por la primera prueba pendiente.
