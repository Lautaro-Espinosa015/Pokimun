# Análisis de Pokimun — 23 de octubre de 2026

> Este informe describe el estado previo a la integración de voz solicitada después de la revisión. La implementación posterior de Windows/Vosk, sus recursos y la guía de pruebas se documentan en `Docs/VozMultiplataforma.md`; los hallazgos de voz de este informe son históricos.

## Alcance y límites

Revisión del código propio de juego, menús, herramientas de editor, configuración de Unity, paquetes, escenas de compilación y referencias serializadas relevantes. Se leyeron `Documentacion_Arquitectura.md` y `Documentacion_Proyecto_Completo.md` de Downloads, además de `Docs/Combate.md`. Esos documentos se trataron como contexto histórico, no como instrucciones para modificar el proyecto.

Se inspeccionaron las integraciones de MediaPipe y sus dependencias relevantes; esto no constituye una auditoría exhaustiva de cada archivo de terceros, modelo 3D, textura, animación o shader importado. No se ejecutó el juego, el editor de Unity, una compilación completa ni una sesión real de Relay. Los problemas señalados proceden de lectura de código y, donde se indica, ejecución aislada de las reglas C#.

No se modificaron scripts, escenas, paquetes ni ajustes. Este informe es el único archivo añadido durante esta revisión.

## Estado real del proyecto

Unity **6000.3.23f1**, con Universal Render Pipeline **17.3.0**, Input System **1.20.0**, Netcode for GameObjects **1.8.1**, Relay **1.0.5**, Authentication **3.3.1** y Core **1.12.5**, según los archivos locales. MediaPipe está incorporado como paquete local, versión **0.16.0**.

Las escenas habilitadas para compilar son `Assets/Scenes/Menus.unity` y `Assets/Scenes/Escenario.unity`, en ese orden. El flujo de entrada es menú → escenario → selección de solitario, crear sala o unirse.

En la escena Escenario, el gestor está configurado con **10 rondas**, **25 de daño**, multiplicador crítico **1,5** y reducción de defensa **0,3**. Las dos referencias de Vida utilizadas por el gestor tienen **150 PS** máximos. Existen otros valores de Vida en la escena, pero no son los componentes referenciados por este gestor.

El combate actual es **alternado**, con **dos acciones por turno**. Defensa crea dos bloqueos que caducan cuando su propietario vuelve a actuar; Curar recupera hasta 7 PS; Atacar produce daño base o crítico. El campo `JugadaRPS` permanece en el modelo de datos, pero no decide un enfrentamiento de piedra/papel/tijera en `ResolverTurno`.

## Mapa de responsabilidades

| Área | Archivos principales | Situación |
| --- | --- | --- |
| Reglas | `Assets/Scripts/ReglasCombate.cs` | C# independiente de Unity; valida planes y calcula un turno. |
| Coordinación | `Assets/Scripts/GestorNivel.cs` | Estados, CPU, entradas, vidas, presentación, red y final de partida. |
| Transporte | `Assets/Scripts/GestorRedRelay.cs` | Autenticación, Relay DTLS, handshake, mensajes JSON y cierre. |
| Entrada de combate | `Assets/Scripts/DetectorCommandControl.cs` | Voz Windows, teclado, potencia RMS y comandos de sala/pausa. |
| Entrada del menú | `Assets/Scripts/ControladorVozMenu.cs` | Reconocimiento Windows y navegación/opciones. |
| Voz nueva | `Assets/Scripts/GestorVozMultiplataforma.cs` | Prototipo separado, sin acciones reales conectadas. |
| Gestos | `ControladorAtaqueManos.cs`, `HandLandmarkerRunner.cs` | Reconocimiento de manos implementado, sin conectar al flujo principal actual de la escena. |
| Vida | `Vida.cs`, `BarraVida.cs` | Estado de PS, límites, eventos y presentación. |
| Presentación | `FeedbackAtaqueUI.cs`, `ControladorAnimaciones.cs`, `EfectoVisualPersonaje.cs`, `AtaqueVisualAgua.cs` | UI dinámica, animaciones, audio y efectos. |
| Menús y ajustes | `Assets/Prefabs/Menus/Scripts/` | Navegación, pausa, preferencias y FPS. `PointsManager` conserva lógica comentada. |
| Editor | `Assets/Editor/` | Generación de opciones, pausa y diferentes versiones de VFX. |

Hay una separación útil entre cálculo y presentación: `ReglasCombate` no depende de Unity y los controladores visuales no calculan daño. Sin embargo, `GestorNivel` concentra muchas responsabilidades y mezcla referencias de personajes fijos con conceptos de jugador local/remoto.

## Hallazgos prioritarios

### 1. Media: etiquetas del invitado incompatibles con la perspectiva local prevista

**Evidencia:** `GestorNivel.cs:176`, `:257`, `:263`, `:298`, `:410`, `:428`.

**Aclaración del usuario posterior a la revisión:** cada jugador se ve como Hydros y representa al oponente como Ignis. Los personajes son representaciones locales; no identifican de manera global al host y al invitado.

Con ese diseño, resolver las acciones propias sobre `vidaJugador` y las remotas sobre `vidaEnemigo`, invirtiendo las vidas al comprobar el mensaje, es coherente. Se retira el diagnóstico inicial de un error general en la asignación de daño, escudos o victoria por esa inversión: dependía de una interpretación incorrecta del diseño.

La discrepancia restante está en las etiquetas: el invitado se prepara con `participanteLocal = 1`, lo que escribe Hydros rival e Ignis propio en las barras y muestra Ignis como nombre local en el HUD, aunque controla al Hydros de su pantalla.

**Corrección propuesta:** conservar el reflejo local de personajes y hacer que las barras y el HUD indiquen Hydros propio e Ignis rival en ambos equipos. Mantener host/invitado como rol de red independiente de la representación visual. Confirmar el comportamiento con dos instancias antes de atribuir otros errores a esta perspectiva.

### 2. Riesgo pendiente de reproducción: turnos sin confirmación de presentación ni recepción diferida

**Evidencia:** `GestorNivel.cs:332-347`, `:415-419`.

Cada equipo avanza al terminar su propia corrutina. No existe un mensaje que confirme que ambos terminaron la presentación. Los tiempos de ataque, defensa y resultado se transmiten al invitado al iniciar, por lo que ambos usan las mismas duraciones. En condiciones normales, el emisor comienza primero su presentación y el receptor después de recibir el paquete: esto explica que la alternancia funcione en las pruebas del usuario.

Si, por ejemplo, el host sufre una pausa prolongada del proceso o una caída de rendimiento durante su propia presentación, el cliente podría terminar antes, elegir sus acciones y enviarlas cuando el host todavía tiene `esMiTurno = true`. En ese caso la condición de recepción descarta el mensaje sin conservarlo. Es un escenario deducido del código, no un fallo reproducido con Relay durante esta revisión. La ausencia de confirmación, por sí sola, no demuestra que las partidas normales estén fallando.

En otras combinaciones de tiempos, la comprobación de vida puede comparar contra PS que todavía no se han actualizado visualmente. Además, la recepción de turnos no exige `estadoActual == TurnoRival`: durante la presentación de un turno remoto, otro mensaje de la misma ronda podría iniciar una segunda resolución.

**Corrección propuesta:** identificar cada turno y su participante, aceptar una sola vez cada plan y coordinar el avance mediante confirmaciones o un estado autoritativo. Separar el estado lógico ya resuelto de la animación en curso.

### 3. Alta: la voz de Linux aún no está integrada

**Evidencia:** `ControladorVozMenu.cs:5`, `DetectorCommandControl.cs:227`, `GestorVozMultiplataforma.cs:291`, `:329`.

No se encontraron el binding de Vosk, `libvosk.so` ni `Assets/StreamingAssets/vosk-model-small-es` en este checkout. Tampoco aparece Vosk como dependencia en `Packages/manifest.json`.

El gestor nuevo contiene reconocimiento Linux, pero su ejecución de comandos solo registra mensajes y tiene acciones comentadas. No se encontró su GUID en las escenas o prefabs inspeccionados mediante búsqueda en Assets. El menú continúa dependiendo directamente de `UnityEngine.Windows.Speech`, sin protección de plataforma; la voz de combate solo se inicia en Windows. Hay que resolver ambas rutas para considerar terminado el soporte Linux.

Problemas adicionales del prototipo:

- Inicializa en `Start`, pero libera en `OnDisable`: al reactivar el componente no se reinicia el reconocedor.
- Un fallo después de crear el modelo/reconocedor no libera inmediatamente lo ya creado.
- La lectura de audio no contempla la conversión a mono si el clip tiene varios canales.
- Cada actualización asigna arrays nuevos y realiza el reconocimiento en el hilo principal; el impacto debe medirse antes de decidir si moverlo a un trabajador.
- La coincidencia por `Contains` admite subcadenas, por ejemplo «cura» dentro de otras palabras, y solo ejecuta la primera coincidencia.
- Debe evitarse que el prototipo y el medidor RMS abran y cierren el mismo micrófono de forma independiente.
- Los símbolos de plataforma deben considerar el sistema del editor frente al destino de compilación; `Update` usa una condición Linux diferente del orden de selección de `Start`.

**Corrección propuesta:** proveedor común de reconocimiento que entregue frases a los diccionarios existentes, con captura de audio y limpieza bien definidas. Así se conserva la validación de `IntentarRegistrarAccion` y el dictado de códigos.

### 4. Media: la última ronda puede terminar antes de la respuesta del segundo participante

**Evidencia:** `ReglasCombate.cs:132`, `GestorNivel.cs:332`.

`ResolverTurno` finaliza cuando `ronda >= maxRondas`, después del turno de un solo participante. Como el número de ronda se comparte hasta completar ambos turnos, el primer participante de la última ronda puede cerrar la partida sin que actúe el segundo. Esto también afecta al modo CPU.

**Comprobación aislada:** una llamada en ronda 10 de 10 con ambos personajes a 150 PS y dos curaciones devuelve `Empate` inmediatamente. El gestor termina la partida antes de alternar al rival.

**Corrección propuesta:** resolver KO tras cualquier acción, pero evaluar el límite de rondas al completar los turnos de ambos participantes, si esa es la regla deseada.

### 5. Media: comandos de menú pueden reiniciar una partida solitaria en curso

**Evidencia:** `DetectorCommandControl.cs:46`, `:61`, `:258`; `GestorNivel.cs:146`.

Los comandos «solitario», «revancha» o «reintentar» se ejecutan antes de aplicar las restricciones de la ventana de combate. `IniciarPartida` impide reiniciar una batalla online, pero permite reiniciar una batalla solitaria en curso. Una frase reconocida puede borrar vida, plan y corrutinas sin estar en la pantalla correspondiente.

**Corrección propuesta:** habilitar cada familia de comandos según el estado y la pantalla visible. Revisar también las órdenes de navegación y salida.

### 6. Media: la música no vuelve a arrancar al repetir una partida

**Evidencia:** `FeedbackAtaqueUI.cs:56`, `:232`, `:262`; `GestorNivel.cs:146`, `:155`.

La música se inicia en `Start` del componente y se detiene en `MostrarFin`. El botón de volver a jugar reutiliza el mismo componente; `Limpiar` oculta paneles pero no reproduce de nuevo la música ni restablece su pitch. La siguiente partida queda sin música de batalla.

**Corrección propuesta:** método explícito de inicio/reinicio de presentación que restaure música, pitch y estado de audio.

### 7. Baja: la curación informada no coincide con la efectiva

**Evidencia:** `ReglasCombate.cs:116`, `FeedbackAtaqueUI.cs:130`.

`curaAplicada` siempre vale 7 antes de limitar la vida al máximo. Con 150/150 PS, el resultado informa +7 aunque el cambio real sea 0; con 148/150 informa +7 aunque recupere 2. Se reprodujo el primer caso ejecutando el C# de reglas fuera de Unity.

Además, son 7 PS fijos: con los 150 PS configurados en escena no representan el 7 % mencionado en el comentario.

**Corrección propuesta:** calcular `vidaDespues - vidaAntes` y aclarar si la regla requiere cantidad fija o porcentaje.

### 8. Media: la cámara no llega al control de combate principal

**Evidencia:** `ControladorAtaqueManos.cs:43`, `DetectorCommandControl.cs:287`, `Assets/Scenes/Escenario.unity:8757`.

La escena apunta al controlador base `ControladorAtaqueJugador`, no al derivado de manos. La tecla A, descrita como gesto preparado, registra directamente un ataque Tijera sin consultar `ObtenerJugadaAtaque`. Aunque la clasificación de manos actualice su propiedad, esa ruta de entrada no la utiliza. Tampoco el cálculo actual usa `jugada` para diferenciar daño o resultado.

No se encontraron archivos `.bytes` o `.task` en Assets, aunque el runner solicita `hand_landmarker.bytes`; la ubicación o suministro del modelo debe verificarse al integrar la cámara. El runner depende además del arranque y del proveedor de imagen de MediaPipe.

La clasificación conserva el último gesto cuando desaparece la mano y usa comparaciones verticales de dedos, sensibles a la orientación de la mano. Antes de habilitarla deben definirse caducidad, estabilidad y confirmación de la entrada.

### 9. Baja: validación del modelo de acciones incompleta

**Evidencia:** `ReglasCombate.cs:54`.

`EsValida` valida únicamente `AccionCombate`, no `JugadaRPS`. Una acción de ataque con `jugada = 999` se acepta; se comprobó en ejecución aislada. Hoy el cálculo ignora la jugada, pero UI y futuras reglas pueden recibir datos incoherentes.

El transporte comprueba remitente, versión, sesión y tamaño del mensaje, y el gestor comprueba ronda, plan y vidas. Eso aporta protección útil, pero no convierte el host en autoridad de todos los resultados: ambos equipos resuelven localmente y no existe un intercambio autoritativo cerrado como describe `Docs/Combate.md`.

## Diferencias con la documentación recibida

| Afirmación documental | Código actual |
| --- | --- |
| Cinco acciones por ronda | Dos, mediante `ReglasCombate.AccionesPorRonda`. |
| Planificación simultánea/comparación de piedra, papel y tijera | Turnos alternados; la jugada no decide el daño. |
| GestorNivel es Singleton | No declara una instancia estática ni aplica ese patrón. |
| Host valida ambos planes y envía resolución cerrada | Se envían planes de turno; cada equipo calcula su copia. |
| Siguiente ronda espera presentación de ambos | No hay confirmación de presentación. |
| Música crítica a pitch 1,15 | `FeedbackAtaqueUI` usa 1,07. |
| Volumen con conversión logarítmica a decibelios | Se escriben volúmenes lineales de AudioSource y PlayerPrefs. |
| Voz multiplataforma ya instalada | El código Vosk es un prototipo separado y faltan sus recursos en el checkout. |

También hay explicaciones exageradas sobre garantías de red en los documentos. La entrega fiable no evita desconexiones, timeouts o el descarte de mensajes por el estado del juego. No se midió consumo de red ni se puede afirmar estabilidad en cualquier conexión a partir de esta revisión.

## Verificaciones realizadas

- Lectura de los dos documentos suministrados y de la documentación de combate del repositorio.
- Inspección de los 17 scripts de `Assets/Scripts`, los cuatro scripts de menús y las herramientas de editor; revisión de dependencias y código de soporte pertinente de MediaPipe.
- Inspección de escenas habilitadas, referencias del gestor, parámetros efectivos y uso de componentes mediante GUID.
- Búsqueda de Vosk, modelo español, recursos de modelos de manos y uso del gestor nuevo.
- Compilación aislada de `ReglasCombate.cs` con `Add-Type` y ejecución de casos: dos ataques normales dejan al defensor en 100 PS partiendo de 150; curar a vida llena informa 7 y cambia 0; ronda límite devuelve resultado antes de la alternancia; una jugada numérica desconocida se acepta.
- Inspección de estado Git: ya existían cambios en `ProjectSettings/ProjectSettings.asset`, la fuente fallback de LiberationSans y archivos nuevos del gestor de voz. No se alteraron.

Las pruebas aisladas no equivalen a compilar Unity ni verifican funcionamiento del micrófono, permisos, plugins nativos, animaciones o Relay. Los GUID no resueltos únicamente contra Assets/Packages pueden pertenecer a paquetes en caché; no se clasificaron como scripts faltantes sin evidencia adicional.

## Orden recomendado para continuar

1. Acordar las reglas objetivo usando esta discrepancia como punto de decisión: combate alternado actual o enfrentamiento simultáneo descrito en documentación. No reescribir reglas por asumir que el texto histórico es la especificación vigente.
2. Corregir etiquetas del invitado conservando la perspectiva local confirmada y revisar sincronización de turnos; verificar dos instancias con acciones diferentes y vidas desiguales.
3. Corregir cierre de ronda, restricciones de comandos por pantalla y reinicio audiovisual.
4. Integrar un proveedor de voz Windows/Linux en los consumidores existentes e incorporar binding, biblioteca nativa y modelo con configuración de importación y distribución comprobada.
5. Conectar y probar la cámara si continúa formando parte del diseño de controles.
6. Añadir pruebas útiles del núcleo de reglas y del protocolo: última ronda completa, KO temprano, cura limitada, escudos, perspectiva local del invitado, turnos duplicados y latencia/presentaciones con distinta duración. Las pruebas incluidas en MediaPipe no cubren estas reglas propias.
7. Actualizar la documentación para que exista una descripción vigente y coherente con el comportamiento elegido.

Para validar la siguiente implementación serán necesarias sesiones reales en Windows y Linux, dos equipos o instancias para Relay, pruebas de cancelación/reconexión y comprobación visual de escenas y builds.

## Revisión específica de la perspectiva multijugador tras la aclaración del usuario

Se volvió a seguir la selección local, el envío de vidas, `VidaCoincide`, la resolución con `soyAtacante` y la aplicación de resultados. Se ejecutó el código real de `ReglasCombate.cs` en memoria, con un adaptador de prueba que reproduce la asignación local/remota de vidas y escudos del gestor. El adaptador no ejecuta el MonoBehaviour, las corrutinas ni el transporte.

Se comprobaron vidas y bloqueos reflejados después de cada caso y resultados de victoria opuestos en el KO:

| Caso | PS propios/rivales del emisor | PS propios/rivales del receptor | Resultado |
| --- | --- | --- | --- |
| Host hace dos ataques | 150 / 100 | 100 / 150 | Coherente |
| Invitado se cura y ataca | 107 / 125 | 125 / 107 | Coherente |
| Host defiende y ataca | 125 / 82 | 82 / 125 | Coherente |
| Invitado hace ataque normal y crítico contra escudo | 82 / 81 | 81 / 82 | Coherente |
| KO independiente, empezando con 100 / 20 | 100 / 0 | 0 / 100 | GanaJugador / GanaRival |

Esto respalda el diseño descrito por el usuario: cada pantalla representa al propietario como Hydros y al otro jugador como Ignis. No se encontró un error de cálculo causado por ese reflejo con los máximos de vida actuales, iguales para ambos personajes. No corresponde cambiar esa arquitectura por la interpretación inicial del informe.

La inconsistencia de textos sigue siendo verificable: `participanteLocal` afecta las etiquetas y el HUD del invitado, mientras las acciones y resultados usan la perspectiva local. La sincronización temporal queda como riesgo pendiente de reproducción, separado de la identidad visual. No se modificó código de juego para esta revisión.
