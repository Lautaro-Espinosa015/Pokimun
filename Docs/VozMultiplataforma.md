# Voz en Windows y Linux

## Integración

`GestorVozMultiplataforma` es el proveedor que utilizan `ControladorVozMenu` y `DetectorCommandControl`. Los consumidores lo añaden a su propio GameObject si no existe, lo configuran en `OnEnable` y lo detienen en `OnDisable`. No hace falta arrastrar componentes nuevos en las escenas.

- Windows y editor Windows: `KeywordRecognizer`, con el vocabulario existente.
- Linux x86_64 y editor Linux: Vosk offline, con el modelo español pequeño.
- El sistema del editor decide el proveedor mientras se trabaja en Unity. Seleccionar Linux como destino desde Windows no intenta cargar `libvosk.so` dentro del editor Windows.

Las frases llegan al hilo principal y se comparan con órdenes completas. Se normalizan mayúsculas, tildes, puntuación y espacios; por ejemplo, «NÚMERO UNO» y «numero uno» llegan al mismo comando. No se ejecuta «cura» por aparecer dentro de otra palabra ni se interpretan órdenes parciales.

El combate conserva `IntentarRegistrarAccion` y las comprobaciones existentes de turno, pausa, foco y antigüedad de la frase. El proveedor nuevo no modifica las reglas del combate ni el reflejo local de los personajes en multijugador.

## Archivos instalados y procedencia

| Recurso | Ubicación | Versión |
| --- | --- | --- |
| Binding oficial C# | `Assets/ThirdParty/Vosk/` | Commit `6cc1d5a2a2837e570e32eec4f0ada383a5e94d04` de `alphacep/vosk-unity-asr` |
| Biblioteca nativa | `Assets/Plugins/Linux/x86_64/libvosk.so` | Vosk 0.3.45, distribución Linux x86_64 |
| Modelo español | `Assets/StreamingAssets/vosk-model-small-es/` | `vosk-model-small-es-0.42` |
| Licencia del binding | `Assets/ThirdParty/Vosk/LICENSE.txt` | Apache 2.0 |

Fuentes: [binding oficial para Unity](https://github.com/alphacep/vosk-unity-asr/tree/6cc1d5a2a2837e570e32eec4f0ada383a5e94d04), [distribución nativa oficial](https://github.com/alphacep/vosk-api/releases/tag/v0.3.45), [catálogo y licencia del modelo español](https://alphacephei.com/vosk/models).

Se incorporaron únicamente los cinco archivos C# del binding, su licencia, la biblioteca Linux y el modelo español. No se importaron escenas de demostración, el modelo ruso ni bibliotecas de otras plataformas. El repositorio de Unity de Vosk no se añadió como dependencia UPM porque es un proyecto de ejemplo y no contiene un paquete UPM en su raíz.

`Docs/Vosk-Dependencias.json` registra procedencia, versión y SHA256 de 21 archivos instalados, además de los hashes Git originales de los archivos del binding y los hashes de los ZIP descargados. Los hashes de los ZIP se calcularon al descargar; no son una firma publicada por el proveedor.

Los recursos instalados y sus `.meta` deben acompañar al proyecto al compartirlo. El modelo se copia al build mediante StreamingAssets. El importador de `libvosk.so` está limitado al player Linux64 y al editor Linux x86_64; no debe activarse para Windows.

## Audio, potencia y ciclo de vida

Hay una captura continua por proveedor activo, a **16000 Hz**, con un clip circular de diez segundos y bloques de 100 ms. Si Unity entrega varios canales, se promedian por frame antes de convertir a PCM16 mono. El medidor RMS usa la misma captura; el controlador de combate ya no abre un segundo micrófono.

La carga del modelo y el reconocimiento Vosk se realizan en un trabajador. La lectura de Unity y la ejecución de acciones quedan en el hilo principal. La cola de audio está acotada: si el equipo no alcanza a procesarla o el clip pierde audio, se detiene la voz y se registra el motivo, en lugar de ejecutar órdenes atrasadas.

Vosk devuelve el inicio y final de las palabras. Se usan esos tiempos para rechazar órdenes de una ventana anterior de combate y medir la potencia en el momento del audio. `umbralGritoRms` y `ventanaNivelVoz` siguen siendo ajustables en `DetectorCommandControl`. La frecuencia se fija a 16000 Hz; el antiguo campo de frecuencia del controlador ya no se utiliza.

Al desactivar el consumidor se detiene el micrófono y se cancela el trabajador. El trabajador libera primero `VoskRecognizer` y luego `Model`. Reactivar el consumidor vuelve a llamar a `Iniciar`. En Windows, si falla únicamente la captura para RMS, Windows Speech puede seguir funcionando con potencia cero; en Linux la captura es necesaria para reconocer.

## Comprobaciones automáticas

Con Unity abierto, fuera de Play Mode:

- `Pokimun > Voz > Ejecutar pruebas de reconocimiento` ejecuta las pruebas EditMode.
- `Pokimun > Voz > Comprobar scripts Linux x86_64` comprueba recursos e importación y compila los scripts para Linux.
- `Pokimun > Voz > Comprobar scripts Windows x86_64` compila los scripts para Windows.

También se puede ejecutar desde PowerShell, en la raíz del proyecto:

```powershell
./Tools/VerificarVoz.ps1
./Tools/VerificarVoz.ps1 -Accion pruebas
./Tools/VerificarVoz.ps1 -Accion linux
./Tools/VerificarVoz.ps1 -Accion windows
```

La primera orden comprueba integridad sin requerir Unity. Las otras solicitan una validación al editor mediante `Temp/ValidacionVoz/solicitud.txt`. El editor solo la procesa fuera de Play Mode, cuando termina de importar y compilar. Ejecutar una solicitud a la vez y esperar su resultado antes de enviar otra. Los resultados `.txt` y `.xml` quedan en `Temp/ValidacionVoz`; Unity puede borrar Temp al cerrar.

Compilar scripts comprueba ambas ramas de plataforma, pero no genera un ejecutable completo ni demuestra funcionamiento del plugin nativo o del micrófono en Linux.

### Resultado de la implementación del 23 de octubre de 2026

- **13 pruebas EditMode aprobadas**, sin fallos ni omisiones, ejecutadas en Unity 6000.3.23f1.
- **Scripts de player Linux x86_64 compilados** por Unity; el assembly generado contiene la implementación Vosk y las comprobaciones finales de micrófono.
- **Scripts de player Windows x86_64 compilados** por Unity; el editor Windows también compiló el proveedor compartido.
- **21 recursos oficiales comprobados** mediante SHA256; los cinco archivos C# coinciden además con sus blobs Git originales.
- Biblioteca comprobada como **ELF de 64 bits para x86_64** y modelo español instalado con sus 14 archivos originales.
- Los ajustes de proyecto y la fuente que ya tenían modificaciones coinciden con las copias locales conservadas; no se cambiaron las escenas ni los scripts de reglas, gestor de nivel o Relay.

La prueba de voz hablada en un equipo Linux y la partida entre dos sistemas siguen pendientes. Los resultados automáticos se guardaron en `Temp/ValidacionVoz/pruebas.xml`, `pruebas.txt`, `linux.txt` y `windows.txt`.

## Prueba manual pendiente en cada sistema

1. Abrir `Menus` y entrar en Play Mode. En Linux esperar el mensaje `[Voz] Vosk escuchando español offline a 16000 Hz` después de la carga del modelo.
2. Decir «opciones», «música cincuenta», «volver» y «jugar». Comprobar que las acciones corresponden a los menús visibles.
3. En el selector de partida, dictar «letra alfa», esperar una breve pausa, dictar otra letra y probar «borrar». Los comandos del código solo deben escribir cuando el campo está disponible.
4. Elegir «solitario». Probar «tijera» y «cura» durante el turno propio; probar «escudo» desde una ronda posterior a la primera. Una orden pronunciada antes de habilitar el turno no debe colarse en el siguiente.
5. Comparar un ataque hablado normal con otro por encima del umbral RMS. Revisar en consola el RMS, el umbral y si fue crítico. Puede hacer falta ajustar el umbral al micrófono del equipo.
6. Probar pausa y reanudación, volver al menú y entrar de nuevo, y desactivar/reactivar el consumidor para comprobar la liberación y reinicio de la voz.
7. Generar un build Linux x86_64 y ejecutarlo en Linux con acceso al dispositivo de entrada. Confirmar el modelo y `libvosk.so` en la distribución y revisar el Player.log si no inicia.
8. Probar una partida Windows/Linux con Relay: dictar el código de sala y comprobar órdenes, vidas reflejadas y resultados. Relay necesita Internet; el reconocimiento de voz no depende de un servicio remoto.

En Vosk usar preferentemente los comandos españoles y una orden por frase, con pausa entre órdenes. Las variantes inglesas permanecen en el vocabulario, pero el modelo instalado es español. No se simularon pruebas de reconocimiento con una persona hablando ni una partida entre dos sistemas durante esta implementación.

## Referencia local para recuperación

`Backups/AntesVoz-20261023/` contiene el ZIP del estado confirmado en Git de scripts/configuración/escenas, su commit base, el parche de los cambios previos en ajustes y fuente, y copias de esos dos archivos modificados. Se excluye de Git mediante `.gitignore`. Esa referencia corresponde a archivos rastreados y no equivale a un commit de todos los archivos que estaban sin seguimiento.
