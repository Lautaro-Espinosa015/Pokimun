# Ajustes de voz, cierre de conexión e Hydros

Revisión del 23 de octubre de 2026, posterior al commit `bcd215d` de integración de Vosk.

## Sensibilidad del crítico

El controlador de combate ahora permite ajustar dos umbrales RMS independientes:

| Ejecución | Campo del Inspector | Valor inicial |
| --- | --- | --- |
| Windows / editor Windows | Umbral Grito Rms | 0,12 |
| Linux / editor Linux | Umbral Grito Rms Linux | 0,24 |

Linux requiere inicialmente el doble de nivel RMS que antes para activar el crítico. Windows conserva su umbral. La detección de la palabra y la decisión del crítico son pasos distintos: se modifica la decisión del crítico, utilizando la captura existente.

El log de cada orden aceptada de combate identifica proveedor, RMS, umbral, crítico y aceptación. El valor Linux necesita comprobación con voz normal y fuerte en el micrófono utilizado; la diferencia de ganancia entre sistemas no se puede calibrar de forma fiable sin esas mediciones.

## Cierre local y desconexión remota

Antes, `PauseManager.SalirJuego` solicitaba destruir el gestor y salía de Play Mode sin cerrar Relay de forma síncrona. Netcode podía emitir el callback de desconexión antes de que Unity ejecutara el `Destroy`, produciendo el aviso local de que había salido el otro jugador.

`GestorNivel.PrepararSalidaDelJuego` cierra la selección y las corrutinas, retira su escucha de pérdida de conexión y cancela Relay antes de destruir objetos o finalizar Play Mode. Esta preparación se utiliza tanto al salir desde el gestor como desde la pausa. Si falta un gestor de nivel, la pausa también busca y cierra Relay directamente.

`GestorRedRelay` cancela su conexión al cerrar la aplicación. En el editor, un registro anterior a los `Awake` de Netcode identifica `ExitingPlayMode`, de modo que el callback local del cierre del editor no se presenta como un error remoto. La marca se reinicia al comenzar otra sesión, incluyendo el caso de recarga de dominio desactivada.

El equipo que permanece conectado sigue recibiendo el aviso y el evento de pérdida de conexión. No se rebajó ni eliminó globalmente el `LogWarning` de desconexión remota.

## Giro de Hydros al volver a reposo

Se reprodujo el movimiento en una escena de previsualización usando el modelo, el avatar y el Animator Controller reales de Hydros, con root motion desactivado como en la escena de combate. El idle conservaba la orientación original (`keepOriginalOrientation = true`), mientras que básico, crítico y defensa usaban la orientación calculada del cuerpo (`false`). La raíz del personaje permanecía fija, pero la referencia de orientación corporal cambiaba al mezclar esos clips con idle.

El primer ajuste puso `keepOriginalOrientation = true` en las acciones para igualarlas al idle. Aunque redujo el salto de orientación medido, la prueba visual del usuario mostró que dirigía los ataques y el escudo hacia la cámara. Se descartó ese ajuste.

El segundo ajuste igualó el idle a las acciones, pero la prueba visual también mostró que desviaba la postura de reposo. Se restauró toda la configuración de importación original: idle con `keepOriginalOrientation = true` y las acciones con `false`. La continuidad numérica por sí sola no valida la dirección visual del personaje en la cámara del juego.

La revisión actual conserva esas orientaciones originales y suaviza el retorno a reposo de Hydros aumentando únicamente sus tres transiciones de salida de 0,25 a 0,45 segundos. `ControladorAnimaciones.FinalizarAccion` respeta una transición que ya vuelve a idle, conserva un idle en curso y permite que la defensa salga mediante su transición por booleano, evitando sobrescribir la mezcla del Animator con otro CrossFade.

Medición del cambio máximo de yaw corporal por frame, simulando 60 FPS durante el retorno:

| Acción | Antes | Después |
| --- | --- | --- |
| Ataque básico → reposo | 3,779° | 2,093° |
| Ataque crítico → reposo | 3,743° | 2,080° |
| Defensa → reposo | 3,332° | 2,106° |

Estas son medidas de orientación del cuerpo humanoide, no una evaluación visual completa de la partida. La raíz permaneció fija durante la simulación. La transición entre las posturas originales sigue existiendo; el ajuste reduce su brusquedad sin redirigir las animaciones. Conviene confirmar visualmente el resultado en la cámara del escenario, especialmente en ataques consecutivos y defensa.

Se añadió el menú `Pokimun > Animaciones > Medir retorno de Hydros a reposo` para repetir la medición fuera de Play Mode. Genera un resumen y una traza CSV en `Temp/DiagnosticoHydros/despues.txt` y `despues.csv`, sin guardar cambios en la escena abierta. La referencia anterior se conservó en `Backups/AjustesVozRedHydros-20261023/`.

## Validación y pruebas restantes

- **21 pruebas EditMode aprobadas:** las 13 de voz previas, cinco de integración de umbral/cierre local/desconexión remota/reinicio de sesión y tres que preservan la configuración de orientación original de las acciones de Hydros y de su idle, validada por las pruebas visuales del usuario.
- Los tests de cierre invocan los callbacks de los componentes reales en objetos aislados; no crean una conexión Relay por Internet ni sustituyen la prueba entre dos equipos.
- Scripts del player compilados para **Windows x86_64** y **Linux x86_64** después de los ajustes de voz y red.
- Integridad de los 21 recursos oficiales Vosk verificada.
- Comparación de orientación antes/después usando las animaciones reales importadas en Unity.

Prueba manual recomendada:

1. En Linux, hablar normalmente y luego elevar la voz para un ataque. Comparar RMS y crítico, ajustando `Umbral Grito Rms Linux` al dispositivo si hace falta.
2. Con dos equipos conectados, salir por el menú de pausa y por voz. El equipo que sale no debe mostrar el aviso remoto; el otro debe indicar que se perdió la conexión.
3. Repetir con host e invitado intercambiados y detener Play Mode directamente en el editor. Volver a iniciar para comprobar que las desconexiones reales siguen notificándose.
4. Observar varios ataques básicos/críticos y defensas de Hydros, comprobando el retorno a idle desde la cámara habitual.
