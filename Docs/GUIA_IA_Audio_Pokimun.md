# Guía para una IA: implementar SFX en Wwise y llevarlos a Unity (proyecto Pokimun)

Instrucciones para un asistente de IA que continúa el diseño sonoro del juego **Pokimun** (TP final de Diseño de Sonido). Seguilas al pie de la letra: así trabaja el equipo y así se evita romper el proyecto de los demás.

---

## 1. Contexto

| Qué | Dónde / versión |
| --- | --- |
| Unity | 6000.3.23f1 (Unity 6.3 LTS), URP |
| Wwise | 2025.1.12 + integración de Wwise para Unity |
| Proyecto Unity (repo git) | `Pokimun/` |
| Proyecto Wwise | `Pokimun/Pokimun_WwiseProject/` |
| Audio original (WAV) de los SFX | `Pokimun_WwiseProject/Originals/SFX/TP_SFX/` |
| Scripts de audio | `Assets/Scripts/Audio/` (música) y `Assets/Scripts/Audio/SFX/` (SFX y voces) |
| Material del grupo (fuera del repo) | `TP Final/SFX_Listos/` (WAV terminados), `TP Final/SFX_Listos/Voces/` |
| Escenas sonorizadas | `Assets/Scenes/Lobby.unity` y `Assets/Scenes/BusquedaIgnis.unity` |

La consigna pide: 14 SFX (7 espacializados en objetos, 5 espacializados por acciones, 2 extradiegéticos), al menos 5 SFX hechos con 4 capas (capa 1 = Foley propio), voz propia, ambiente general en loop, y 2 piezas de música (remezcla vertical y resecuenciación horizontal) implementadas con Wwise.

---

## 2. Reglas de oro (no negociables)

1. **Antes de tocar nada: `git pull` y revisar qué cambió.** Otros integrantes trabajan en el mismo repo. Nunca sobrescribas un archivo sin haberlo leído en su versión actual. Si un archivo cambió desde que lo leíste, volvé a leerlo y combiná los cambios.
2. **Wwise tiene que estar CERRADO cuando se editan los `.wwu` a mano.** Si Wwise está abierto, al guardar pisa tus cambios (o vos pisás los suyos).
3. **Respetar los finales de línea de cada archivo.** Los `.cs` y algunos `.wwu` usan CRLF; otros `.wwu` usan LF. Detectalo y conservá el mismo.
4. **No modificar las escenas de Unity (`.unity`) si se puede evitar.** Generan conflictos de git. Todo el audio se configura **por código buscando objetos por nombre** (ver sección 6). Solo se agrega a la escena un componente cuando no queda otra.
5. **Un evento por sonido, con nombre `Play_<NombreDelSound>`.** Unity llama a los eventos por su nombre en texto: si renombrás algo en Wwise, actualizá Unity.
6. **Nunca saturar.** Todos los WAV tienen margen (picos entre -6 y -19 dBFS). El volumen final se decide en Wwise (volumen del Sound + bus), no subiendo el WAV.
7. **Después de cada cambio en Wwise:** abrir Wwise, comprobar 0 errores, guardar (Ctrl+S) y **Generate All** de SoundBanks (Windows y Android). Después, Play en Unity y comprobar 0 errores en la consola.
8. **Anotar lo hecho en la bitácora del proyecto** (`Bitacora_desarrollo_TP.md`): qué se agregó, dónde suena, con qué volumen y atenuación.
9. **No inventar contenido sonoro.** Usá los WAV que entrega el equipo. Si falta un archivo, pedilo; no lo generes.

---

## 3. Convenciones de nombres

| Tipo | Prefijo | Ejemplo |
| --- | --- | --- |
| Efecto | `SFX_` | `SFX_Arbusto`, `SFX_Amb_Lava` (ambientes: `SFX_Amb_`) |
| Voz | `VO_<Personaje>_` | `VO_Ignis_Risa`, `VO_Hydros_TeEncontre` |
| Música | `MUS_` | `MUS_Santuario_Capa1_Base` |
| Evento | `Play_` + nombre del Sound | `Play_SFX_Arbusto` |
| Atenuación (ShareSet) | `Att_` | `Att_Abejas` |
| Game Parameter | Palabra en castellano | `Cercania_Ignis`, `Volumen_Musica` |

Nombres de archivo WAV: claros y versionados (`SFX_Entrar_Arbusto_V3.wav`). WAV 48 kHz, 24 bit, mono (estéreo solo si hace falta).

---

## 4. Estructura actual en Wwise

**Work Units** (pestaña Audio → Containers):

- `Default Work Unit`: la música (`Santuario` = Music Playlist con 4 capas; `Busqueda_Musica` = Music Switch Container ligado al State Group `Busqueda`).
- `SFX_TP` (`Containers/SFX_TP.wwu`, ID `{E4E3DA59-B30C-4796-B6B6-C5F3613FCA31}`): todos los SFX y voces, en carpetas:

| Carpeta | ID | Contenido | Bus |
| --- | --- | --- | --- |
| Ambientes_3D | `{D4A2DE6A-7350-4D5E-982D-2117801E2EF8}` | Loops 3D: lava, estanque, portal, abejas | Ambiente |
| Ambiente_2D | `{F1C36BEC-84FA-4746-884E-24B63AA717A6}` | Pájaros de fondo | Ambiente |
| Pasos | `{8085A7CA-F92A-49D6-8B89-DCE704E77E99}` | Piedra, tierra, madera, charco | SFX |
| Interacciones_3D | `{315138D0-C2FA-4F18-8C0D-B21C77C332AF}` | Cofre, puerta, arbusto, flores, hallazgo | SFX |
| Extradiegeticos_2D | `{2C7C14A9-FA7A-4ADD-969D-FEF1A759F612}` | Cruzar portal, transición, locución | SFX / Voces |
| Voces_Ignis_3D | `{A17535ED-709D-4BA6-91BE-E0FC1294C92F}` | Voces de Ignis | Voces |
| Voces_Hydros_2D | `{104FC1CB-67F1-4E5B-9C23-5215A5C976EB}` | Voces de Hydros | Voces |

**Eventos:** `Events/SFX_TP.wwu` (ID `{EB355461-C4B6-4C94-8A49-EF85E041E91E}`).

**Buses** (`Busses/Default Work Unit.wwu`, Work Unit `{EFEFD7F3-2A11-4683-A889-30488491F87E}`), todos hijos de `Main Audio Bus`:

| Bus | ID | Nivel | Notas |
| --- | --- | --- | --- |
| Ambiente | `{04900290-D162-4533-8865-0149FDB569D1}` | -3 dB | RTPC `Volumen_Efectos` |
| SFX | `{F8A4C978-29E7-4592-9592-CBB0860C1160}` | 0 dB | RTPC `Volumen_Efectos` |
| Voces | `{8A389AC0-8F01-41B2-87A7-3EF71BF226D2}` | 0 dB | Auto-ducking: baja Musica -10 dB |
| Musica | (ver el .wwu) | -8 dB | RTPC `Volumen_Musica` |

Main Audio Bus tiene un Wwise Peak Limiter (preset `Brick_Wall_Minus_1dB_Peak_Fast_Release`).

**Conversion por defecto:** `Default Conversion Settings` `{6D1B890C-9826-4384-BF07-C15223E9FB56}`, Work Unit `{8130EB71-3B95-43B6-9522-00A92E2B9C5B}`.

**Atenuaciones** (`Attenuations/TP_Atenuaciones.wwu`, Work Unit `{003062FD-3D98-405A-A87B-DDB746589A77}`):

| Nombre | ID | Alcance | Uso |
| --- | --- | --- | --- |
| Att_Abejas | `{A8C07801-1EFB-4547-B217-E4722B68A1E8}` | 9 m | Fuentes chicas |
| Att_Estanque | `{215BDE31-DF19-4036-B794-6B82F0E57E0C}` | 12 m | Agua del Lobby |
| Att_Pasos | `{BBE84A5C-81B7-44E1-B8E1-33BDA769DFB6}` | 15 m | Pasos |
| Att_Lava | `{A34E45F0-35A6-455D-9C92-21C821775267}` | 20 m | Lava |
| Att_Ambiente_Loop | `{B65EB0E9-6D5A-4D0D-A0F9-62A6406AC8DC}` | 25 m | Portales y loops grandes |
| Att_Interaccion | `{9A3C34CD-A94A-46E7-9F62-BC42CFB7976C}` | 30 m | Interacciones |
| Att_Voz_Ignis | `{5BF2AB3E-93C4-4D65-B392-E112AFCE8C7F}` | 35 m | Voces 3D |

**Game Syncs:** Game Parameters `Cercania_Ignis`, `Cercania_Hydros`, `Cercania_Portal` (música del Lobby), `Volumen_Musica`, `Volumen_Efectos` (sliders del menú de pausa); State Group `Busqueda` (`Explorando`, `Tension`, `Hallazgo`).

---

## 5. Receta: agregar un SFX nuevo en Wwise

### 5.1 Preparar el WAV

1. Recibir el WAV terminado del equipo (en `TP Final/SFX_Listos/`).
2. Medir pico y RMS (por ejemplo con Python + scipy). Si el pico supera -3 dBFS, avisar al equipo antes de seguir.
3. Copiarlo a `Pokimun_WwiseProject/Originals/SFX/TP_SFX/` con el mismo nombre.

### 5.2 Editar `Containers/SFX_TP.wwu` (con Wwise cerrado)

Agregar un `<Sound>` dentro del `<ChildrenList>` de la carpeta que corresponda. Copiá un Sound vecino del mismo tipo y cambiá:

- `Name` y un `ID` nuevo (GUID en MAYÚSCULAS entre llaves) y un `ShortID` nuevo (entero aleatorio de 32 bits).
- El `AudioFileSource`: `Name` = nombre del archivo sin extensión, `ID` nuevo, `<AudioFile>TP_SFX\Archivo.wav</AudioFile>`. **No pongas `<MediaIDList>`**: Wwise lo calcula solo.
- El `ActiveSource` con el mismo nombre e ID que el `AudioFileSource`.

**Plantilla de un Sound 3D (en loop, con atenuación):**

```xml
<Sound Name="SFX_Nuevo" ID="{GUID-NUEVO-1}" ShortID="123456789">
	<PropertyList>
		<Property Name="3DSpatialization" Type="int16" Value="1"/>
		<Property Name="IsLoopingEnabled" Type="bool" Value="True"/>
		<Property Name="OverridePositioning" Type="bool" Value="True"/>
		<Property Name="Volume" Type="Real64">
			<ValueList>
				<Value>-2</Value>
			</ValueList>
		</Property>
	</PropertyList>
	<ReferenceList>
		<Reference Name="Attenuation">
			<ObjectRef Name="Att_Interaccion" ID="{9A3C34CD-A94A-46E7-9F62-BC42CFB7976C}" WorkUnitID="{003062FD-3D98-405A-A87B-DDB746589A77}"/>
		</Reference>
		<Reference Name="Conversion">
			<ObjectRef Name="Default Conversion Settings" ID="{6D1B890C-9826-4384-BF07-C15223E9FB56}" WorkUnitID="{8130EB71-3B95-43B6-9522-00A92E2B9C5B}"/>
		</Reference>
		<Reference Name="OutputBus">
			<ObjectRef Name="SFX" ID="{F8A4C978-29E7-4592-9592-CBB0860C1160}" WorkUnitID="{EFEFD7F3-2A11-4683-A889-30488491F87E}"/>
		</Reference>
	</ReferenceList>
	<ChildrenList>
		<AudioFileSource Name="SFX_Nuevo_V1" ID="{GUID-NUEVO-2}">
			<Language>SFX</Language>
			<AudioFile>TP_SFX\SFX_Nuevo_V1.wav</AudioFile>
		</AudioFileSource>
	</ChildrenList>
	<ActiveSourceList>
		<ActiveSource Name="SFX_Nuevo_V1" ID="{GUID-NUEVO-2}" Platform="Linked"/>
	</ActiveSourceList>
</Sound>
```

Variantes:

- **Un solo disparo (no loop):** quitar `IsLoopingEnabled`.
- **2D (voz en off, extradiegético, ambiente de fondo):** quitar `3DSpatialization` y la `Reference Name="Attenuation"`; dejar `OverridePositioning`.
- **Voz:** bus `Voces`. **Ambiente:** bus `Ambiente`.
- **Reemplazar el audio de un SFX existente:** solo cambiar `Name` del `AudioFileSource`/`ActiveSource` y la ruta del `<AudioFile>`, y borrar su `<MediaIDList>`. Quitar los ajustes provisorios (pitch, filtros) si el archivo nuevo ya viene terminado.

### 5.3 Editar `Events/SFX_TP.wwu`

Agregar al final del `<ChildrenList>` del Work Unit:

```xml
<Event Name="Play_SFX_Nuevo" ID="{GUID-NUEVO-3}">
	<ChildrenList>
		<Action Name="" ID="{GUID-NUEVO-4}" ShortID="987654321">
			<ReferenceList>
				<Reference Name="Target">
					<ObjectRef Name="SFX_Nuevo" ID="{GUID-NUEVO-1}" WorkUnitID="{E4E3DA59-B30C-4796-B6B6-C5F3613FCA31}"/>
				</Reference>
			</ReferenceList>
		</Action>
	</ChildrenList>
</Event>
```

No hace falta un evento Stop: Unity detiene los loops con `AudioTP.Detener` (Stop con fundido sobre el mismo evento).

### 5.4 Atenuación nueva (si ninguna sirve)

Copiar una `<Attenuation>` de `TP_Atenuaciones.wwu`, darle `Name`, `ID`, `ShortID` y un `ID` nuevo a su `<Curve Name="VolumeDry">`, ajustar `RadiusMax` y los `<Point>` (XPos = metros, YPos = dB; el primero con `Flags` 5, el último con -200 dB y `Flags` 37).

### 5.5 Validar en Wwise

1. Validar que cada `.wwu` editado sea XML válido.
2. Abrir el proyecto en Wwise **desde el Wwise Launcher** (Projects → Open in Wwise). Si aparece el cartel "already opened", hay dos instancias: cancelar y cerrar la sobrante.
3. Comprobar el contador de errores y advertencias (arriba a la derecha): tiene que ser 0 y 0.
4. Ctrl+S → F7 (layout SoundBank) → plataformas Windows y Android tildadas → **Generate All** → el log tiene que terminar en `0 error(s)`.
5. Cerrar Wwise si después vas a volver a editar archivos a mano.

---

## 6. Receta: disparar el SFX desde Unity

### 6.1 Piezas que ya existen (`Assets/Scripts/Audio/SFX/`, namespace `Pokimun.Audio`)

| Clase | Para qué |
| --- | --- |
| `AudioTP` (estática) | `Post(evento, gameObject)` carga el banco del evento y lo dispara (agrega `AkGameObj` si falta). `Detener(evento, go, fundidoMs)` lo para con fundido. `Voz(evento, go, duracion, forzar)` dispara una voz solo si no hay otra sonando. |
| `EmisorSonidoLoop` | Componente: loop 3D que arranca en `Start` y se detiene con fundido de 600 ms al desactivarse (cambio de escena). |
| `RoceVegetacion` | Roce al pasar cerca de objetos por **comienzo de nombre** (grupos con evento, radio e intervalo mínimo). `RoceVegetacion.EmisorEn(nombre, posicion, evento, padre)` crea un emisor en loop en una posición. |
| `PasosJugador` + `ZonaSuperficie` | Pasos según la distancia recorrida; la superficie cambia el evento (por collider o radio, con prioridad). |
| `PuertaCobertizo`, `AparienciaCofre` | Visual + sonido de la puerta y los cofres del cobertizo. |
| `AudioLobby` | Todo el audio del Lobby (en el objeto `AudioSFX` de la escena). |
| `AudioBusqueda` | Todo el audio de BusquedaIgnis (en el objeto `AudioSFX` de la escena). |
| `AudioGlobal` | Persistente: sonido al cruzar portales, transición de escena y sincronización de los sliders de volumen con Wwise. |

Música (`Assets/Scripts/Audio/`): `MusicaDeEscena` (base: fundidos entre escenas), `MusicaSantuario` (RTPC de cercanía), `MusicaBusqueda` (State `Busqueda`).

### 6.2 Cómo se agrega un sonido

1. Elegir el script de la escena (`AudioLobby` o `AudioBusqueda`) y agregar un **campo público** con el nombre del evento (`public string eventoNuevo = "Play_SFX_Nuevo";`) con `[Tooltip]` en castellano.
2. Buscar el objeto **por nombre** en `Start` (`GameObject.Find`, o recorrer `FindObjectsByType<Transform>` con `StartsWith`). No arrastrar referencias en el Inspector.
3. Según el tipo:
   - **Loop en un objeto:** `go.AddComponent<EmisorSonidoLoop>().evento = eventoNuevo;`
   - **Loop en una posición sin objeto:** `RoceVegetacion.EmisorEn("Nombre", posicion, eventoNuevo, transform);`
   - **Al pasar cerca de objetos:** agregar un grupo a `RoceVegetacion`.
   - **Al presionar una tecla:** dentro de `Update`, con `Keyboard.current` (Input System) y comprobando distancia y ángulo de la cámara del jugador, como el cofre.
   - **Por un evento del juego:** suscribirse a los eventos de `GestorExploracion` (`Hallazgo`, `PistaDisponible`, `CambioEscondite`) y desuscribirse en `OnDestroy`.
   - **Voces:** siempre con `AudioTP.Voz(...)` para que no se pisen.
4. Los valores nuevos de campos serializados se toman del código solo si la escena todavía no los guardó. Si cambiás un valor por defecto de un campo que ya está en la escena, la escena manda: avisalo o cambialo en el Inspector.
5. Guardar con el mismo final de línea (CRLF en los `.cs`).

### 6.3 Probar

1. Volver a Unity: esperar que compile. La consola tiene que estar sin errores.
2. Play en la escena (Lobby o BusquedaIgnis). Moverse hasta el sonido. Para salir de Play con el cursor capturado por el juego: **Ctrl+P**.
3. Revisar la consola: 0 errores y 0 advertencias. Revisar en la jerarquía que se crearon los emisores.
4. Para escuchar valores en vivo: Profiler de Wwise conectado al juego (Connect mientras Unity está en Play).

---

## 7. Mezcla: criterios del equipo

- Las voces y las interacciones mandan; la música acompaña; el ambiente rellena.
- La música está a -8 dB en su bus y baja -10 dB cuando suena una voz (ducking en el bus Voces).
- Un sonido chico (abejas, flores) se apaga cerca (9 m o menos); uno que guía al jugador (voz de Ignis) llega lejos (35 m).
- Los ambientes de fondo 2D (pájaros) van bajitos: -8 dB.
- Nada de volumen positivo grande para "arreglar" un WAV bajo: si un WAV está muy bajo, pedirle al equipo otro render.

---

## 8. Checklist antes de terminar

- [ ] `git pull` hecho al empezar; ningún cambio ajeno pisado.
- [ ] WAV copiados a `Originals/SFX/TP_SFX/`.
- [ ] `.wwu` válidos; Wwise abre con 0 errores y 0 advertencias.
- [ ] SoundBanks generados (Generate All) con 0 errores.
- [ ] Unity compila; Play en las dos escenas sin errores ni advertencias.
- [ ] Bitácora actualizada (qué, dónde, volumen, atenuación, evento).
- [ ] Commit con los WAV nuevos, los `.wwu`, los `.cs` (y sus `.meta`) y un mensaje claro.

---

## 9. Problemas conocidos y soluciones

| Problema | Solución |
| --- | --- |
| Wwise abre una segunda instancia ("already opened") | Abrir siempre desde el Wwise Launcher; si pasa, Cancel y cerrar la sobrante. |
| Un loop sigue sonando al cambiar de escena | Usar `EmisorSonidoLoop` (para con fundido en `OnDisable`) o `AudioTP.Detener`. |
| Los pasos suenan solos al terminar la partida | `PasosJugador` usa el desplazamiento real y `JugadorExploracion.Bloqueado`; no usar `CharacterController.velocity`. |
| Un slider del menú no cambia nada | Los sliders guardan PlayerPrefs (`VolumenMusica`, `VolumenEfectos`); `AudioGlobal` los envía como RTPC a Wwise. Revisar que los buses tengan el RTPC. |
| Un sonido 3D se oye desde toda la escena | Falta atenuación o el alcance es muy grande: asignar o crear un `Att_`. |
| Después de recompilar aparecen `MissingReferenceException` del Inspector | Son del editor de Unity; desaparecen solas. |
