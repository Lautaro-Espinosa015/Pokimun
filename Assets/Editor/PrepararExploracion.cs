using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Pokimun.Exploracion;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Construye escenas iniciales editables sin modificar Escenario ni los modelos originales.</summary>
[InitializeOnLoad]
public static class PrepararExploracion
{
    private const string Recursos = "Assets/Exploracion";
    private const string Bosque = "Assets/NIVELES/recursosNiveles/Bosque/Handpainted_Forest_pack/Models/";
    private static Material piedra, musgo, madera, agua, fuego;
    private static double proximaConsulta;
    static PrepararExploracion() { EditorApplication.update += RevisarSolicitud; EditorApplication.update += DiagnosticoJuego; }
    private static void DiagnosticoJuego()
    {
        const string solicitud = "Temp/Exploracion/juego.txt";
        if (!EditorApplication.isPlaying || !File.Exists(solicitud)) return;
        string accion;
        try { accion = File.ReadAllText(solicitud).Trim(); File.Delete(solicitud); }
        catch (IOException) { return; }
        var g = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        if (g == null) return;
        if (accion.StartsWith("ir:"))
        {
            string nombre = accion.Substring(3);
            if (nombre == "ignis") g.jugador.IrA(g.ignis.position + Vector3.right * 1.8f);
            else g.jugador.IrA(g.destinos.First(d => d.nombreVoz == nombre).PuntoLlegada);
        }
        else if (accion == "interactuar") g.Interactuar();
        else if (accion == "busqueda") g.Cargar("BusquedaIgnis");
        else if (accion == "lobby") g.Cargar("Lobby");
        else if (accion == "mirar-ignis")
        {
            float angulo = Quaternion.LookRotation(g.ignis.position - g.jugador.transform.position).eulerAngles.y;
            g.jugador.Girar(Mathf.DeltaAngle(g.jugador.transform.eulerAngles.y, angulo));
        }
        var lineas = new List<string> { "Escena=" + SceneManager.GetActiveScene().name, "Jugador=" + g.jugador.transform.position,
            "Camara=" + g.jugador.camara.transform.position + " tercera=" + g.jugador.terceraPersona, "Navegando=" + g.jugador.Navegando,
            "Aviso=" + g.jugador.Aviso, "Tiempo=" + (g.Partida?.Tiempo ?? 0), "Pausa=" + g.Pausado + " escalaTiempo=" + Time.timeScale };
        foreach (var r in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            lineas.Add(r.name + " bounds=" + r.bounds + " escala=" + r.transform.lossyScale + " root=" + r.transform.root.name);
        File.WriteAllLines("Temp/Exploracion/juego-resultado.txt", lineas);
    }
    private static void RevisarSolicitud()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.timeSinceStartup < proximaConsulta) return;
        proximaConsulta = EditorApplication.timeSinceStartup + 2;
        const string archivo = "Temp/Exploracion/solicitud.txt";
        if (!File.Exists(archivo)) return;
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) return;
        string accion;
        try { accion = File.ReadAllText(archivo).Trim(); File.Delete(archivo); }
        catch (IOException) { return; }
        try
        {
            if (accion == "inspeccionar") Inspeccionar();
            else if (accion == "crear") Crear();
            else if (accion == "validar") ValidarEscenas();
            else if (accion == "actualizar-base") ActualizarBase();
            else if (accion == "pruebas") EjecutarPruebas();
        }
        catch (Exception ex)
        {
            File.WriteAllText("Temp/Exploracion/resultado.txt", "ERROR: " + ex); Debug.LogException(ex);
        }
    }

    private static void ActualizarBase()
    {
        // Herramienta de desarrollo con copia previa; nunca se llama automáticamente al abrir Unity.
        string copia = ".local-codex/Backups/Exploracion-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(copia);
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" })
            File.Copy("Assets/Scenes/" + nombre + ".unity", copia + "/" + nombre + ".unity");
        Construir();
    }

    private static UnityEditor.TestTools.TestRunner.Api.TestRunnerApi pruebas;
    [MenuItem("Pokimun/Exploración/Ejecutar pruebas de reglas y regresión")]
    public static void EjecutarPruebas()
    {
        pruebas = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
        pruebas.RegisterCallbacks(new ResultadosPruebas());
        pruebas.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter {
            testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
            assemblyNames = new[] { "Pokimun.Exploracion.Tests", "Pokimun.Voz.Tests" }
        }));
    }
    private sealed class ResultadosPruebas : UnityEditor.TestTools.TestRunner.Api.ICallbacks
    {
        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor t) { }
        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor t) { }
        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor r) { }
        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor r)
        {
            Directory.CreateDirectory("Temp/Exploracion");
            UnityEditor.TestTools.TestRunner.Api.TestRunnerApi.SaveResultToFile(r, Path.GetFullPath("Temp/Exploracion/pruebas.xml"));
            File.WriteAllText("Temp/Exploracion/pruebas.txt", $"{r.TestStatus}: correctas={r.PassCount}, fallidas={r.FailCount}, omitidas={r.SkipCount}");
            EditorApplication.delayCall += () => { if (pruebas != null) UnityEngine.Object.DestroyImmediate(pruebas); };
        }
    }

    [MenuItem("Pokimun/Exploración/Validar escenas y destinos")]
    public static void ValidarEscenas()
    {
        var mensajes = new List<string>();
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" })
        {
            var escena = EditorSceneManager.OpenScene("Assets/Scenes/" + nombre + ".unity", OpenSceneMode.Single);
            var gestor = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
            if (gestor == null || gestor.jugador == null || gestor.jugador.camara == null) throw new Exception(nombre + ": jugador o cámara faltante.");
            if (UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length != 1) throw new Exception(nombre + ": cantidad incorrecta de cámaras.");
            if (UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length != 0) throw new Exception(nombre + ": no debe contener audio todavía.");
            if (gestor.busqueda && (gestor.escondites.Length != 9 || gestor.ignis == null)) throw new Exception("Configuración de escondites incompleta.");
            foreach (var d in gestor.destinos)
            {
                if (!NavMesh.SamplePosition(gestor.jugador.transform.position, out var inicio, 2, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(d.PuntoLlegada, out var llegada, 1, NavMesh.AllAreas)) throw new Exception(nombre + ": destino fuera de navegación: " + d.nombreVoz);
                var ruta = new NavMeshPath();
                if (!NavMesh.CalculatePath(inicio.position, llegada.position, NavMesh.AllAreas, ruta) || ruta.status != NavMeshPathStatus.PathComplete)
                    throw new Exception(nombre + ": destino inaccesible: " + d.nombreVoz);
                mensajes.Add(nombre + ": ruta a " + d.nombreVoz + " OK");
            }
            if (gestor.busqueda)
                foreach (var p in gestor.escondites)
                    if (!NavMesh.SamplePosition(p.position, out _, 1, NavMesh.AllAreas)) throw new Exception("Escondite fuera del suelo: " + p.name);
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gestor.gameObject) > 0) throw new Exception("Scripts faltantes.");
            mensajes.Add(nombre + ": configuración OK");
        }
        Directory.CreateDirectory("Temp/Exploracion"); File.WriteAllLines("Temp/Exploracion/validacion.txt", mensajes);
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
    }

    public static void Inspeccionar()
    {
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/Escenario.unity", OpenSceneMode.Single);
        var lineas = new List<string>();
        foreach (var raiz in escena.GetRootGameObjects())
        {
            lineas.Add("ROOT " + raiz.name + " " + raiz.transform.position);
            if (raiz.name != "Escenario") continue;
            foreach (var t in raiz.GetComponentsInChildren<Transform>())
            {
                var r = t.GetComponent<Renderer>();
                if (r != null) lineas.Add(t.name + " center=" + r.bounds.center + " size=" + r.bounds.size + " parent=" + t.parent.name);
            }
        }
        foreach (string personaje in new[] { "Hydros", "Ignis" })
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModelosDePokimun/" + personaje + "/Ender_" + personaje + "@Fight Idle.fbx");
            var obj = UnityEngine.Object.Instantiate(modelo);
            foreach (var r in obj.GetComponentsInChildren<Renderer>()) lineas.Add(personaje + " " + r.name + " " + r.bounds);
            UnityEngine.Object.DestroyImmediate(obj);
        }
        Directory.CreateDirectory("Temp/Exploracion"); File.WriteAllLines("Temp/Exploracion/inspeccion.txt", lineas);
    }

    [MenuItem("Pokimun/Exploración/Crear escenas iniciales (solo si no existen)")]
    public static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode antes de crear escenas.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Guardá las escenas abiertas antes de crear las nuevas.");
        if (File.Exists("Assets/Scenes/Lobby.unity") || File.Exists("Assets/Scenes/BusquedaIgnis.unity"))
            throw new InvalidOperationException("Las escenas ya existen. No se sobrescriben cambios de diseño.");
        Construir();
    }
    private static void Construir()
    {
        Directory.CreateDirectory(Recursos); Directory.CreateDirectory(Recursos + "/Materiales");
        AssetDatabase.Refresh();
        piedra = Material("Piedra musgosa", new Color(.24f, .29f, .24f));
        musgo = Material("Musgo", new Color(.19f, .29f, .10f));
        madera = Material("Madera cobertizo", new Color(.30f, .19f, .10f));
        agua = Material("Portal agua", new Color(.1f, .68f, .77f));
        fuego = Material("Portal fuego", new Color(.95f, .35f, .09f));
        CrearLobby(); CrearBusqueda();
        var escenas = EditorBuildSettings.scenes.ToList();
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" })
        {
            string ruta = "Assets/Scenes/" + nombre + ".unity";
            if (!escenas.Any(e => e.path == ruta)) escenas.Add(new EditorBuildSettingsScene(ruta, true));
        }
        EditorBuildSettings.scenes = escenas.ToArray(); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion"); File.WriteAllText("Temp/Exploracion/resultado.txt", "OK: Lobby y BusquedaIgnis creadas y navegación horneada.");
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
    }
    private static void CrearLobby()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Iluminar();
        var arquitectura = new GameObject("Santuario de piedra musgosa");
        Cubo("Suelo", new Vector3(0, -.25f, 0), new Vector3(26, .5f, 30), piedra, arquitectura.transform);
        Cubo("Muro oeste", new Vector3(-13, 3, 0), new Vector3(.7f, 6, 30), piedra, arquitectura.transform);
        Cubo("Muro este", new Vector3(13, 3, 0), new Vector3(.7f, 6, 30), piedra, arquitectura.transform);
        Cubo("Muro fondo", new Vector3(0, 3, 15), new Vector3(26, 6, .7f), piedra, arquitectura.transform);
        Cubo("Muro entrada", new Vector3(0, 3, -15), new Vector3(26, 6, .7f), piedra, arquitectura.transform);
        Cubo("Techo", new Vector3(0, 6.3f, 0), new Vector3(26, .6f, 30), piedra, arquitectura.transform);
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 p = new Vector3(x * 10, 0, z * 10);
            Cubo("Basamento", p + Vector3.up * .2f, new Vector3(2, .4f, 2), musgo, arquitectura.transform);
            Cubo("Columna", p + Vector3.up * 3, new Vector3(1.2f, 6, 1.2f), piedra, arquitectura.transform);
        }
        var destinos = new List<DestinoExploracion>();
        destinos.Add(Portal("combate", "Escenario", new Vector3(-6, 0, 13.6f), fuego));
        destinos.Add(Portal("busqueda", "BusquedaIgnis", new Vector3(6, 0, 13.6f), agua));
        foreach (string personaje in new[] { "Hydros", "Ignis" })
        {
            float x = personaje == "Hydros" ? -6 : 6;
            var pedestal = Cubo("Pedestal " + personaje, new Vector3(x, .5f, -2), new Vector3(2.6f, 1, 2.6f), piedra);
            var estatua = Personaje(personaje, new Vector3(x, 1, -2)); estatua.name = "Estatua " + personaje;
            estatua.transform.rotation = Quaternion.Euler(0, 180, 0);
            var destino = pedestal.AddComponent<DestinoExploracion>(); destino.nombreVoz = personaje.ToLowerInvariant();
            destino.descripcion = "Estatua de " + personaje;
            destino.llegada = Punto("Llegada " + personaje, new Vector3(x, 0, -5), destino.transform);
            destinos.Add(destino); Cartel(personaje.ToUpperInvariant(), new Vector3(x, 3.4f, -2));
        }
        // Objetos editables que reservan las futuras interacciones sonoras, sin componentes de audio.
        destinos.Add(Objeto("cristal", new Vector3(-10, 1, 5), agua, "Cristal del santuario"));
        destinos.Add(Objeto("mecanismo", new Vector3(10, 1, 5), musgo, "Mecanismo del santuario"));
        var gestor = CrearGestor(false, new Vector3(0, .05f, -10), destinos.ToArray());
        Hornear(gestor.gameObject);
        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Lobby.unity");
    }
    private static void CrearBusqueda()
    {
        // Se clona exclusivamente la raíz de geometría del nivel actual, no sus gestores, UI, personajes ni audio.
        var origen = EditorSceneManager.OpenScene("Assets/Scenes/Escenario.unity", OpenSceneMode.Single);
        var entorno = origen.GetRootGameObjects().First(o => o.name == "Escenario");
        var copia = UnityEngine.Object.Instantiate(entorno); copia.name = "Bosque del nivel 1";
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(copia, escena); EditorSceneManager.CloseScene(origen, true);
        SceneManager.SetActiveScene(escena); Iluminar();
        // Las posiciones finales se ajustan a la geometría existente mediante el informe de inspección.
        PrepararBosque(copia);
        var destinos = new List<DestinoExploracion>();
        Vector3 cobertizo = new Vector3(0, 0, -12);
        Cubo("Suelo cobertizo", cobertizo + new Vector3(0, -.09f, 0), new Vector3(8, .3f, 7), madera);
        Cubo("Pared oeste cobertizo", cobertizo + new Vector3(-4, 1.8f, 0), new Vector3(.3f, 3.6f, 7), madera);
        Cubo("Pared este cobertizo", cobertizo + new Vector3(4, 1.8f, 0), new Vector3(.3f, 3.6f, 7), madera);
        Cubo("Pared fondo cobertizo", cobertizo + new Vector3(0, 1.8f, -3.5f), new Vector3(8, 3.6f, .3f), madera);
        Cubo("Frente izquierdo", cobertizo + new Vector3(-2.6f, 1.8f, 3.5f), new Vector3(2.8f, 3.6f, .3f), madera);
        Cubo("Frente derecho", cobertizo + new Vector3(2.6f, 1.8f, 3.5f), new Vector3(2.8f, 3.6f, .3f), madera);
        Cubo("Dintel cobertizo", cobertizo + new Vector3(0, 3.2f, 3.5f), new Vector3(2.4f, .8f, .3f), madera);
        Cubo("Techo cobertizo", cobertizo + new Vector3(0, 3.8f, 0), new Vector3(8.6f, .35f, 7.6f), madera);
        var volumen = new GameObject("Interior cobertizo").AddComponent<BoxCollider>();
        volumen.center = cobertizo + Vector3.up * 1.8f; volumen.size = new Vector3(8, 4, 7); volumen.isTrigger = true;
        var vuelta = Objeto("lobby", cobertizo + new Vector3(0, 1, -3.2f), agua, "Volver al lobby");
        vuelta.escenaDestino = "Lobby"; destinos.Add(vuelta);
        var regresoCobertizo = new GameObject("Destino cobertizo").AddComponent<DestinoExploracion>();
        regresoCobertizo.nombreVoz = "cobertizo"; regresoCobertizo.descripcion = "Cobertizo";
        regresoCobertizo.llegada = Punto("Llegada cobertizo", cobertizo + new Vector3(0, .06f, 1)); destinos.Add(regresoCobertizo);
        var escondites = new List<Transform>();
        Vector3[] posiciones = {
            new Vector3(-10,0,-3), new Vector3(9,0,-3), new Vector3(-12,0,5),
            new Vector3(11,0,6), new Vector3(-6,0,11), new Vector3(5,0,12),
            new Vector3(-12,0,14), new Vector3(12,0,15), new Vector3(0,0,18)
        };
        for (int i = 0; i < posiciones.Length; i++)
        {
            Vector3 p = posiciones[i];
            escondites.Add(Punto("Escondite " + (i + 1), p));
            var obstaculo = Cubo("Roca refugio " + (i + 1), p + new Vector3(0, .85f, -1.8f), new Vector3(2.6f, 1.7f, 1.6f), piedra);
            obstaculo.transform.rotation = Quaternion.Euler(0, i * 23, 0);
            var modeloRoca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NIVELES/recursosNiveles/Bosque/BrokenVector/LowPolyRockPack/Prefabs/Rock Type2 01.prefab");
            if (modeloRoca != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(modeloRoca);
                var limites = visual.GetComponentInChildren<Renderer>().bounds;
                visual.transform.localScale = new Vector3(2.6f / limites.size.x, 1.7f / limites.size.y, 1.6f / limites.size.z);
                visual.transform.rotation = obstaculo.transform.rotation;
                visual.transform.position += obstaculo.transform.position - visual.GetComponentInChildren<Renderer>().bounds.center;
                foreach (var c in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                visual.transform.SetParent(obstaculo.transform, true); obstaculo.GetComponent<Renderer>().enabled = false;
            }
            string nombre = new[] { "rocas", "pino", "arbusto", "tronco", "ruinas", "claro", "helechos", "mirador", "arbol grande" }[i];
            var d = obstaculo.AddComponent<DestinoExploracion>(); d.nombreVoz = nombre; d.descripcion = "Investigar " + nombre;
            d.llegada = Punto("Llegada " + nombre, p + new Vector3(2.3f, 0, -1), d.transform);
            destinos.Add(d); Cartel(nombre.ToUpperInvariant(), p + new Vector3(0, 2.8f, -1.8f));
        }
        Cartel("COBERTIZO", cobertizo + new Vector3(0, 3.5f, 3.7f));
        var gestor = CrearGestor(true, cobertizo + new Vector3(0, .05f, -1), destinos.ToArray());
        gestor.interiorCobertizo = volumen; gestor.escondites = escondites.ToArray();
        gestor.ignis = Personaje("Ignis", posiciones[0]).transform;
        var detector = gestor.ignis.gameObject.AddComponent<CapsuleCollider>(); detector.center = Vector3.up; detector.height = 2; detector.radius = .55f;
        Hornear(gestor.gameObject);
        EditorSceneManager.SaveScene(escena, "Assets/Scenes/BusquedaIgnis.unity");
    }
    private static void PrepararBosque(GameObject bosque)
    {
        bosque.transform.position += new Vector3(-13, 0, 3);
        foreach (var c in bosque.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach (var a in bosque.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(a);
        foreach (var plataforma in bosque.GetComponentsInChildren<Transform>().Where(t => t.name == "Plataforma").ToArray())
            UnityEngine.Object.DestroyImmediate(plataforma.gameObject);
        foreach (var t in bosque.GetComponentsInChildren<Transform>().Where(t => t.name.ToLowerInvariant().StartsWith("montallas")).ToArray())
            t.position += Vector3.forward * 14;
        foreach (var r in bosque.GetComponentsInChildren<Renderer>())
        {
            if (r.name.StartsWith("Fir_") || r.name.StartsWith("Pine_"))
            {
                var tronco = new GameObject("Colisión tronco"); tronco.transform.position = new Vector3(r.bounds.center.x, 1.5f, r.bounds.center.z);
                tronco.transform.SetParent(r.transform, true); var c = tronco.AddComponent<CapsuleCollider>(); c.height = 3; c.radius = .32f;
            }
            else if (r.name.StartsWith("Rock Type"))
            {
                var roca = new GameObject("Colisión roca"); roca.transform.position = r.bounds.center; roca.transform.SetParent(r.transform, true);
                roca.AddComponent<BoxCollider>().size = r.bounds.size * .8f;
            }
        }
        // Suelo de recorrido continuo y plano: se revisa visualmente antes de sustituir las plataformas originales.
        Cubo("Suelo de exploración", new Vector3(0, -.3f, 2), new Vector3(34, .6f, 40), musgo);
        var piso = AssetDatabase.LoadAssetAtPath<GameObject>(Bosque + "Floor_Grass_v1_2.prefab");
        if (piso != null)
        {
            var anteriores = bosque.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Floor_Grass")).Select(r => r.gameObject).ToArray();
            foreach (var anterior in anteriores) UnityEngine.Object.DestroyImmediate(anterior);
            for (int x = 0; x < 5; x++)
            for (int z = 0; z < 6; z++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(piso);
                var r = tile.GetComponentInChildren<Renderer>();
                float factor = 6.85f / r.bounds.size.x; tile.transform.localScale *= factor;
                Vector3 centro = r.bounds.center;
                tile.transform.position += new Vector3(-13.65f + x * 6.82f, .015f, -14.9f + z * 6.82f) - centro;
                tile.transform.SetParent(bosque.transform, true);
            }
        }
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Barrera(new Vector3(lado * 17, 3, 2), new Vector3(.5f, 6, 40));
            Barrera(new Vector3(0, 3, 2 + lado * 20), new Vector3(34, 6, .5f));
        }
        var arbol = AssetDatabase.LoadAssetAtPath<GameObject>(Bosque + "Fir_v1_1.prefab");
        if (arbol != null)
        for (int i = 0; i < 12; i++)
        {
            foreach (Vector3 p in new[] { new Vector3(-17, 0, -18 + i * 3.5f), new Vector3(17, 0, -18 + i * 3.5f),
                new Vector3(-17 + i * 3.1f, 0, -18), new Vector3(-17 + i * 3.1f, 0, 22) })
            {
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(arbol); obj.name = "Árbol límite";
                obj.transform.position = p; obj.transform.SetParent(bosque.transform, true);
                obj.transform.localScale *= 1.3f;
            }
        }
    }
    private static GestorExploracion CrearGestor(bool busqueda, Vector3 inicio, DestinoExploracion[] destinos)
    {
        var go = new GameObject("Gestor exploración"); var gestor = go.AddComponent<GestorExploracion>();
        gestor.busqueda = busqueda; gestor.destinos = destinos; gestor.interfaz = go.AddComponent<InterfazExploracion>();
        var camara = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camara.tag = "MainCamera";
        var jugador = new GameObject("Hydros explorador"); jugador.transform.position = inicio; jugador.layer = 2;
        var cc = jugador.AddComponent<CharacterController>(); cc.center = new Vector3(0, .95f, 0); cc.height = 1.9f; cc.radius = .35f; cc.stepOffset = .3f;
        var entradas = jugador.AddComponent<StarterAssets.StarterAssetsInputs>(); entradas.cursorLocked = false; entradas.cursorInputForLook = false; entradas.analogMovement = true;
        var oficial = jugador.AddComponent<StarterAssets.FirstPersonController>(); oficial.MoveSpeed = 3.5f; oficial.GroundLayers = 1 << 0;
        oficial.GroundedRadius = .28f; oficial.CinemachineCameraTarget = Punto("Objetivo cámara", inicio + Vector3.up * 1.65f, jugador.transform).gameObject;
        // Nuestro adaptador unifica teclado, voz y táctil. Evita que PlayerInput duplique las entradas.
        jugador.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = false;
        var control = jugador.AddComponent<JugadorExploracion>(); gestor.jugador = control; control.terceraPersona = !busqueda;
        var cuerpo = Personaje("Hydros", inicio); cuerpo.transform.SetParent(jugador.transform, true); control.cuerpo = cuerpo.transform;
        ConfigurarMarcha(cuerpo.GetComponentInChildren<Animator>());
        foreach (var t in cuerpo.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
        control.camara = camara.GetComponent<Camera>(); control.camara.nearClipPlane = .06f; control.camara.farClipPlane = 180;
        return gestor;
    }
    private static GameObject Personaje(string nombre, Vector3 posicion)
    {
        string ruta = "Assets/ModelosDePokimun/" + nombre + "/Ender_" + nombre + "@Fight Idle.fbx";
        var envoltura = new GameObject(nombre); envoltura.transform.position = posicion;
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
        obj.transform.SetParent(envoltura.transform, false);
        var animator = obj.GetComponent<Animator>(); animator.applyRootMotion = false;
        string controlador = Recursos + "/Idle" + nombre + ".controller";
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(controlador);
        if (ac == null)
        {
            ac = AnimatorController.CreateAnimatorControllerAtPath(controlador);
            var clip = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            // Copia editable y loop propio: no modifica la importación de animaciones del combate.
            var copia = UnityEngine.Object.Instantiate(clip); copia.name = "Reposo " + nombre;
            var ajustes = AnimationUtility.GetAnimationClipSettings(copia); ajustes.loopTime = true; AnimationUtility.SetAnimationClipSettings(copia, ajustes);
            AssetDatabase.CreateAsset(copia, Recursos + "/Reposo" + nombre + ".anim");
            ac.AddMotion(copia);
        }
        if (!ac.parameters.Any(p => p.name == "Velocidad")) ac.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        animator.runtimeAnimatorController = ac;
        // Los bounds importados ya incluyen la conversión de unidades FBX.
        // BakeMesh aplicaría otra vez la escala de los huesos de estos modelos.
        var renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds limites = renderers[0].bounds;
        foreach (var r in renderers) limites.Encapsulate(r.bounds);
        float escala = 1.9f / Mathf.Max(.01f, limites.size.y);
        obj.transform.localScale *= escala;
        obj.transform.localPosition = new Vector3(-(limites.center.x - envoltura.transform.position.x) * escala,
            -(limites.min.y - envoltura.transform.position.y) * escala, -(limites.center.z - envoltura.transform.position.z) * escala);
        return envoltura;
    }
    private static void ConfigurarMarcha(Animator animator)
    {
        string ruta = Recursos + "/MovimientoHydros.controller";
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
        if (ac == null)
        {
            ac = AnimatorController.CreateAnimatorControllerAtPath(ruta);
            ac.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            var arbol = new BlendTree { name = "Reposo y marcha", blendParameter = "Velocidad", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(arbol, ac);
            arbol.AddChild(AssetDatabase.LoadAssetAtPath<AnimationClip>(Recursos + "/ReposoHydros.anim"), 0);
            var fuente = AssetDatabase.LoadAllAssetsAtPath("Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/Locomotion--Walk_N.anim.fbx")
                .OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            var marcha = UnityEngine.Object.Instantiate(fuente); marcha.name = "Marcha Hydros";
            AnimationUtility.SetAnimationEvents(marcha, Array.Empty<AnimationEvent>()); // Los pasos se sonorizarán después.
            AssetDatabase.CreateAsset(marcha, Recursos + "/MarchaHydros.anim");
            arbol.AddChild(marcha, 3.5f);
            var estado = ac.layers[0].stateMachine.AddState("Explorar"); estado.motion = arbol;
            ac.layers[0].stateMachine.defaultState = estado;
        }
        animator.runtimeAnimatorController = ac;
    }
    private static DestinoExploracion Portal(string nombre, string escena, Vector3 posicion, Material mat)
    {
        Cubo("Jamba izquierda " + nombre, posicion + new Vector3(-1.5f, 1.8f, 0), new Vector3(.6f, 3.6f, .8f), piedra);
        Cubo("Jamba derecha " + nombre, posicion + new Vector3(1.5f, 1.8f, 0), new Vector3(.6f, 3.6f, .8f), piedra);
        Cubo("Dintel " + nombre, posicion + new Vector3(0, 3.7f, 0), new Vector3(3.6f, .6f, .8f), piedra);
        var obj = Cubo("Portal " + nombre, posicion + Vector3.up * 1.6f, new Vector3(2.4f, 3.2f, .25f), mat);
        var d = obj.AddComponent<DestinoExploracion>(); d.nombreVoz = nombre; d.escenaDestino = escena; d.descripcion = "Entrar a " + nombre;
        d.llegada = Punto("Llegada " + nombre, posicion + Vector3.back * 2, obj.transform);
        Cartel(nombre.ToUpperInvariant(), posicion + new Vector3(0, 4.4f, -.1f)); return d;
    }
    private static DestinoExploracion Objeto(string nombre, Vector3 posicion, Material mat, string descripcion)
    {
        var obj = Cubo(nombre, posicion, Vector3.one, mat);
        var d = obj.AddComponent<DestinoExploracion>(); d.nombreVoz = nombre; d.descripcion = descripcion;
        d.llegada = Punto("Llegada " + nombre, posicion + new Vector3(0, -posicion.y, -2), obj.transform);
        Cartel(nombre.ToUpperInvariant(), posicion + Vector3.up * 1.1f); return d;
    }
    private static void Hornear(GameObject gestor)
    {
        // Capa 2 del jugador excluida; solo colliders sencillos del entorno.
        var superficie = gestor.AddComponent<NavMeshSurface>(); superficie.collectObjects = CollectObjects.All;
        superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders; superficie.layerMask = 1 << 0;
        // Ignis no bloquea el horneado: cambia de ubicación durante la partida.
        var partida = gestor.GetComponent<GestorExploracion>();
        if (partida.ignis != null)
            foreach (var c in partida.ignis.GetComponentsInChildren<Collider>()) c.enabled = false;
        superficie.BuildNavMesh();
        string ruta = Recursos + (partida.busqueda ? "/NavegacionBusqueda.asset" : "/NavegacionLobby.asset");
        var existente = AssetDatabase.LoadAssetAtPath<NavMeshData>(ruta);
        superficie.navMeshData.name = Path.GetFileNameWithoutExtension(ruta);
        if (existente == null) AssetDatabase.CreateAsset(superficie.navMeshData, ruta);
        else
        {
            superficie.RemoveData(); EditorUtility.CopySerialized(superficie.navMeshData, existente);
            superficie.navMeshData = existente; superficie.AddData(); EditorUtility.SetDirty(existente);
        }
        if (partida.ignis != null)
        {
            AjustarEscondites(partida);
            partida.ignis.position = partida.escondites[0].position;
            foreach (var c in partida.ignis.GetComponentsInChildren<Collider>()) c.enabled = true;
        }
    }
    private static void AjustarEscondites(GestorExploracion partida)
    {
        if (!NavMesh.SamplePosition(partida.jugador.transform.position, out var inicio, 1, NavMesh.AllAreas))
            throw new Exception("El inicio de la búsqueda no está en el suelo navegable.");
        var usados = new List<Vector3>();
        foreach (var escondite in partida.escondites)
        {
            Vector3 origen = escondite.position; bool encontrado = false;
            for (int anillo = 0; anillo <= 8 && !encontrado; anillo++)
            for (int angulo = 0; angulo < 16 && !encontrado; angulo++)
            {
                Vector3 candidato = origen + Quaternion.Euler(0, angulo * 22.5f, 0) * Vector3.right * (anillo * .5f);
                if (!NavMesh.SamplePosition(candidato, out var hit, .35f, NavMesh.AllAreas) || hit.position.y > .35f ||
                    usados.Any(p => Vector3.Distance(p, hit.position) < 1.5f)) continue;
                var ruta = new NavMeshPath();
                if (!NavMesh.CalculatePath(inicio.position, hit.position, NavMesh.AllAreas, ruta) || ruta.status != NavMeshPathStatus.PathComplete) continue;
                escondite.position = hit.position; usados.Add(hit.position); encontrado = true;
            }
            if (!encontrado) throw new Exception("No hay un lugar accesible cerca de " + escondite.name);
        }
    }
    private static void Barrera(Vector3 posicion, Vector3 tamano)
    {
        var obj = new GameObject("Límite físico"); obj.transform.position = posicion; obj.AddComponent<BoxCollider>().size = tamano;
    }
    private static GameObject Cubo(string nombre, Vector3 posicion, Vector3 tamano, Material mat, Transform padre = null)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = nombre; obj.transform.position = posicion;
        obj.transform.localScale = tamano; obj.transform.SetParent(padre, true); obj.GetComponent<Renderer>().sharedMaterial = mat; return obj;
    }
    private static Transform Punto(string nombre, Vector3 posicion, Transform padre = null)
    {
        var obj = new GameObject(nombre); obj.transform.position = posicion; obj.transform.SetParent(padre, true); return obj.transform;
    }
    private static Material Material(string nombre, Color color)
    {
        string ruta = Recursos + "/Materiales/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; AssetDatabase.CreateAsset(mat, ruta); }
        if (nombre == "Piedra musgosa" && mat.mainTexture == null)
        {
            var textura = new Texture2D(128, 128); textura.name = "Piedra y musgo";
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float ruido = Mathf.PerlinNoise(x * .075f, y * .075f);
                bool junta = y % 32 < 2 || (x + (y / 32 % 2) * 16) % 32 < 2;
                Color baseColor = junta ? new Color(.13f, .16f, .12f) : Color.Lerp(new Color(.39f, .42f, .36f), new Color(.22f, .31f, .13f), Mathf.SmoothStep(.4f, .75f, ruido));
                textura.SetPixel(x, y, baseColor);
            }
            textura.Apply(); textura.wrapMode = TextureWrapMode.Repeat;
            AssetDatabase.CreateAsset(textura, Recursos + "/Materiales/PiedraMusgo.asset");
            mat.mainTexture = textura; mat.color = Color.white; mat.mainTextureScale = new Vector2(3, 3); EditorUtility.SetDirty(mat);
        }
        return mat;
    }
    private static void Cartel(string texto, Vector3 posicion)
    {
        var obj = new GameObject("Cartel " + texto); obj.transform.position = posicion;
        var t = obj.AddComponent<TMPro.TextMeshPro>(); t.text = texto; t.fontSize = 5;
        t.alignment = TMPro.TextAlignmentOptions.Center; t.rectTransform.sizeDelta = new Vector2(5, 1);
        t.color = new Color(.92f, .92f, .72f);
    }
    private static void Iluminar()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.52f, .57f, .56f);
        var sol = new GameObject("Luz principal").AddComponent<Light>(); sol.type = LightType.Directional;
        sol.intensity = 1.1f; sol.transform.rotation = Quaternion.Euler(45, -35, 0);
    }
}
