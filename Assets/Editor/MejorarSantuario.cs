using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Pokimun.Exploracion;
using StarterAssets;

/// <summary>Mejora las escenas existentes y conserva sus objetos, GUID y referencias.</summary>
public static class MejorarSantuario
{
    private const string Recursos = "Assets/Exploracion/Mejoras";
    private const string Animaciones = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/";
    private static Material suelo, muro, columna, borde, tierra, madera, agua, lava;

    [MenuItem("Pokimun/Exploración/Mejorar santuario y locomoción")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Salí de Play Mode antes de actualizar las escenas.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guardá las escenas abiertas antes de actualizar.");
        string copia = ".local-codex/Backups/Santuario-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(copia); Directory.CreateDirectory(Recursos); AssetDatabase.Refresh();
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" }) File.Copy("Assets/Scenes/" + nombre + ".unity", copia + "/" + nombre + ".unity");
        CrearMateriales();
        var animacion = CrearAnimacion();
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" })
        {
            var escena = EditorSceneManager.OpenScene("Assets/Scenes/" + nombre + ".unity");
            var gestor = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
            ConfigurarJugador(gestor.jugador, animacion);
            if (nombre == "Lobby") { DecorarLobby(gestor); AgregarPasoTierra(gestor); }
            Hornear(gestor);
            EditorSceneManager.SaveScene(escena);
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/mejoras.txt", "OK: santuario con jardines, estanques y locomoción del asset en ambas escenas.");
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
    }
    private static void AgregarPasoTierra(GestorExploracion gestor)
    {
        if (GameObject.Find("Paso de tierra trasero") != null) return;
        var raiz = GameObject.Find("Jardines y estanques").transform;
        var paso = Cubo("Paso de tierra trasero", new Vector3(0, .015f, -12.6f), new Vector3(18, .03f, 2.6f), tierra, raiz);
        // Franja al ras: no agrega escalones ni bloquea las conexiones con el piso de piedra.
        var destino = paso.AddComponent<DestinoExploracion>(); destino.nombreVoz = "tierra";
        destino.descripcion = "Zona de tierra";
        destino.llegada = new GameObject("Llegada tierra").transform; destino.llegada.SetParent(raiz, false);
        destino.llegada.position = new Vector3(0, .03f, -12.6f);
        var destinos = gestor.destinos.ToList(); destinos.Add(destino); gestor.destinos = destinos.ToArray();
        EditorUtility.SetDirty(gestor);
    }
    private static RuntimeAnimatorController CrearAnimacion()
    {
        string ruta = Recursos + "/LocomocionHydros.overrideController";
        var existente = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
        if (existente != null) return existente;
        var original = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Animaciones + "StarterAssetsThirdPerson.controller");
        var controlador = new AnimatorOverrideController(original) { name = "LocomocionHydros" };
        var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>(); controlador.GetOverrides(pares);
        for (int i = 0; i < pares.Count; i++)
        {
            var clip = UnityEngine.Object.Instantiate(pares[i].Key); clip.name = pares[i].Key.name;
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            // El asset aporta reposo, caminar, correr, salto y aterrizaje; el avatar sigue siendo el de Hydros.
            AssetDatabase.CreateAsset(clip, Recursos + "/" + clip.name.Replace("/", "_") + ".anim");
            pares[i] = new KeyValuePair<AnimationClip, AnimationClip>(pares[i].Key, clip);
        }
        controlador.ApplyOverrides(pares); AssetDatabase.CreateAsset(controlador, ruta); return controlador;
    }
    private static void ConfigurarJugador(JugadorExploracion jugador, RuntimeAnimatorController animacion)
    {
        jugador.cuerpo.GetComponentInChildren<Animator>().runtimeAnimatorController = animacion;
        var primera = jugador.GetComponent<FirstPersonController>();
        var tercera = jugador.GetComponent<ThirdPersonController>();
        if (jugador.terceraPersona)
        {
            GameObject objetivo = primera != null ? primera.CinemachineCameraTarget : tercera.CinemachineCameraTarget;
            if (primera != null) UnityEngine.Object.DestroyImmediate(primera);
            if (tercera == null) tercera = jugador.gameObject.AddComponent<ThirdPersonController>();
            tercera.CinemachineCameraTarget = objetivo; tercera.MoveSpeed = 2; tercera.SprintSpeed = 5.335f;
            tercera.SpeedChangeRate = 10; tercera.RotationSmoothTime = .12f;
            tercera.GroundLayers = 1 << 0; tercera.GroundedRadius = .28f; tercera.JumpHeight = 0;
            tercera.LockCameraPosition = true; tercera.FootstepAudioClips = Array.Empty<AudioClip>(); tercera.LandingAudioClip = null;
        }
        else
        {
            primera.MoveSpeed = 2; primera.SprintSpeed = 5.335f; primera.SpeedChangeRate = 10; primera.JumpHeight = 0;
        }
        var entrada = jugador.GetComponent<StarterAssetsInputs>(); entrada.analogMovement = true;
        jugador.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = false;
        EditorUtility.SetDirty(jugador);
    }
    private static void DecorarLobby(GestorExploracion gestor)
    {
        var arquitectura = GameObject.Find("Santuario de piedra musgosa");
        foreach (var r in arquitectura.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterial = r.name == "Suelo" ? suelo : r.name == "Techo" ? madera : r.name == "Columna" ? columna : r.name == "Basamento" ? borde : muro;
        }
        foreach (var d in gestor.destinos)
        {
            if (d.nombreVoz == "hydros" || d.nombreVoz == "ignis") d.GetComponent<Renderer>().sharedMaterial = borde;
            if (d.nombreVoz == "cristal" || d.nombreVoz == "mecanismo")
            {
                float x = d.nombreVoz == "cristal" ? -5 : 5;
                Vector3 desplazamiento = new Vector3(x, 1, 8) - d.transform.position;
                d.transform.position += desplazamiento;
                var cartel = GameObject.Find("Cartel " + d.nombreVoz.ToUpperInvariant());
                if (cartel != null) cartel.transform.position += desplazamiento;
            }
        }
        if (GameObject.Find("Jardines y estanques") != null) return;
        var raiz = new GameObject("Jardines y estanques").transform;
        // Los jardines quedan contra los muros y dejan corredores por ambos lados de los estanques.
        for (int lado = -1; lado <= 1; lado += 2)
        {
            for (int z = -9; z <= 9; z += 6) Jardin(new Vector3(lado * 11.8f, 0, z), new Vector3(1.25f, .5f, 3.2f), raiz);
            Cubo("Zócalo lateral", new Vector3(lado * 12.55f, .35f, 0), new Vector3(.15f, .7f, 29), columna, raiz);
        }
        Jardin(new Vector3(0, 0, 11.2f), new Vector3(3.2f, .5f, 1.4f), raiz);
        Jardin(new Vector3(-10, 0, 12), new Vector3(2.3f, .5f, 1.5f), raiz);
        Jardin(new Vector3(10, 0, 12), new Vector3(2.3f, .5f, 1.5f), raiz);
        var destinos = gestor.destinos.ToList();
        destinos.Add(Estanque("estanque de agua", new Vector3(-7.3f, 0, 3.3f), false, raiz));
        destinos.Add(Estanque("estanque de lava", new Vector3(7.3f, 0, 3.3f), true, raiz));
        destinos.Add(Destino("jardin izquierdo", new Vector3(-10.4f, 0, 0), raiz));
        destinos.Add(Destino("jardin derecho", new Vector3(10.4f, 0, 0), raiz));
        destinos.Add(Destino("jardin del fondo", new Vector3(0, 0, 9), raiz));
        gestor.destinos = destinos.ToArray();
        foreach (int lado in new[] { -1, 1 })
        {
            Cubo("Banco de piedra", new Vector3(lado * 8, .45f, -8), new Vector3(3, .25f, .8f), borde, raiz);
            Cubo("Soporte banco", new Vector3(lado * 8 - 1, .2f, -8), new Vector3(.3f, .4f, .7f), columna, raiz);
            Cubo("Soporte banco", new Vector3(lado * 8 + 1, .2f, -8), new Vector3(.3f, .4f, .7f), columna, raiz);
        }
        EditorUtility.SetDirty(gestor);
    }
    private static void Jardin(Vector3 centro, Vector3 tamano, Transform padre)
    {
        Cubo("Jardinera de piedra", centro + Vector3.up * .2f, tamano, borde, padre);
        Cubo("Tierra", centro + Vector3.up * .47f, new Vector3(tamano.x * .86f, .06f, tamano.z * .9f), tierra, padre);
        string fuente = "Assets/NIVELES/recursosNiveles/Bosque/Handpainted_Forest_pack/Models/";
        for (int i = 0; i < 5; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fuente + (i % 2 == 0 ? "Grass_v1_3.prefab" : "Grass_v2_1.prefab"));
            var planta = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            planta.name = "Planta del santuario"; planta.transform.SetParent(padre, false);
            planta.transform.position = centro + new Vector3(Mathf.Sin(i * 2.4f) * tamano.x * .28f, .5f, Mathf.Cos(i * 2.4f) * tamano.z * .3f);
            planta.transform.rotation = Quaternion.Euler(0, i * 73, 0); planta.transform.localScale = Vector3.one * (.8f + (i % 3) * .22f);
            foreach (var c in planta.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        }
    }
    private static DestinoExploracion Estanque(string nombre, Vector3 posicion, bool esLava, Transform padre)
    {
        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); baseObj.name = nombre;
        baseObj.transform.SetParent(padre, false); baseObj.transform.position = posicion + Vector3.up * .12f;
        baseObj.transform.localScale = new Vector3(3.8f, .12f, 3.8f); baseObj.GetComponent<Renderer>().sharedMaterial = esLava ? lava : agua;
        UnityEngine.Object.DestroyImmediate(baseObj.GetComponent<Collider>());
        // Volumen simple impide entrar al estanque y conserva la navegación por su contorno.
        var colision = new GameObject("Volumen estanque"); colision.transform.SetParent(padre, false);
        colision.transform.position = posicion + Vector3.up * .65f;
        colision.AddComponent<BoxCollider>().size = new Vector3(4, 1.8f, 4);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2 / 16;
            var piedra = Cubo("Borde del estanque", posicion + new Vector3(Mathf.Sin(a) * 2.05f, .2f, Mathf.Cos(a) * 2.05f), new Vector3(.75f, .4f, .45f), esLava ? columna : borde, padre);
            piedra.transform.rotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
        }
        baseObj.AddComponent<SuperficieEstanque>().lava = esLava;
        if (esLava)
        {
            var luz = new GameObject("Resplandor lava").AddComponent<Light>(); luz.transform.SetParent(padre, false);
            luz.transform.position = posicion + Vector3.up; luz.type = LightType.Point; luz.range = 5; luz.intensity = 1.2f; luz.color = new Color(1, .3f, .08f);
            luz.shadows = LightShadows.None;
        }
        var destino = baseObj.AddComponent<DestinoExploracion>(); destino.nombreVoz = nombre; destino.descripcion = nombre;
        destino.llegada = new GameObject("Llegada " + nombre).transform; destino.llegada.SetParent(padre, false);
        destino.llegada.position = posicion + Vector3.back * 2.8f;
        return destino;
    }
    private static DestinoExploracion Destino(string nombre, Vector3 punto, Transform padre)
    {
        var obj = new GameObject(nombre); obj.transform.SetParent(padre, false); obj.transform.position = punto;
        var d = obj.AddComponent<DestinoExploracion>(); d.nombreVoz = nombre; d.descripcion = nombre; return d;
    }
    private static void Hornear(GestorExploracion gestor)
    {
        var surface = gestor.GetComponent<NavMeshSurface>(); surface.RemoveData();
        var existente = surface.navMeshData;
        var colisiones = gestor.ignis != null ? gestor.ignis.GetComponentsInChildren<Collider>() : Array.Empty<Collider>();
        foreach (var c in colisiones) c.enabled = false;
        surface.BuildNavMesh();
        if (existente != null)
        {
            surface.RemoveData(); string nombre = existente.name;
            EditorUtility.CopySerialized(surface.navMeshData, existente); existente.name = nombre;
            surface.navMeshData = existente; surface.AddData(); EditorUtility.SetDirty(existente);
        }
        foreach (var c in colisiones) c.enabled = true;
    }
    private static GameObject Cubo(string nombre, Vector3 posicion, Vector3 tamano, Material material, Transform padre)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = nombre; obj.transform.SetParent(padre, false);
        obj.transform.position = posicion; obj.transform.localScale = tamano; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
    }
    private static void CrearMateriales()
    {
        suelo = Material("Losas de suelo", new Color(.42f, .43f, .36f), 0, new Vector2(9, 10));
        muro = Material("Caliza envejecida", new Color(.49f, .48f, .36f), 1, new Vector2(5, 2));
        columna = Material("Basalto", new Color(.16f, .20f, .18f), 2, new Vector2(2, 4));
        borde = Material("Piedra tallada", new Color(.39f, .42f, .33f), 2, Vector2.one * 2);
        tierra = Material("Tierra húmeda", new Color(.13f, .095f, .045f), 3, Vector2.one * 3);
        madera = Material("Vigas oscuras", new Color(.19f, .13f, .075f), 4, new Vector2(6, 3));
        agua = Material("Agua del santuario", new Color(.07f, .46f, .52f), 5, Vector2.one * 2);
        lava = Material("Lava del santuario", new Color(.8f, .19f, .025f), 6, Vector2.one * 2);
    }
    private static Material Material(string nombre, Color color, int tipo, Vector2 tiling)
    {
        string ruta = Recursos + "/" + nombre + ".mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta); if (existente != null) return existente;
        var textura = new Texture2D(128, 128) { name = nombre, wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 128; y++)
        for (int x = 0; x < 128; x++)
        {
            float n = Mathf.PerlinNoise(x * .09f + tipo * 10, y * .09f + tipo * 10);
            float fino = Mathf.PerlinNoise(x * .5f, y * .5f);
            Color c = color * (.75f + n * .35f + fino * .12f);
            if (tipo < 2)
            {
                int alto = tipo == 0 ? 64 : 32;
                bool junta = y % alto < 2 || (x + (y / alto % 2) * 32) % 64 < 2;
                if (junta) c *= .48f;
                if (tipo == 1 && n > .57f) c = Color.Lerp(c, new Color(.22f, .29f, .12f), .4f);
            }
            if (tipo == 4) c *= .7f + Mathf.Sin(x * .5f + n * 5) * .18f;
            if (tipo == 5) c = Color.Lerp(new Color(.04f, .24f, .29f), new Color(.3f, .68f, .67f), n);
            if (tipo == 6) c = Color.Lerp(new Color(.08f, .045f, .025f), new Color(1, .47f, .035f), Mathf.SmoothStep(.35f, .65f, n));
            c.a = 1; textura.SetPixel(x, y, c);
        }
        textura.Apply(); AssetDatabase.CreateAsset(textura, Recursos + "/" + nombre + ".asset");
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre, mainTexture = textura, color = Color.white };
        mat.mainTextureScale = tiling; mat.SetFloat("_Smoothness", tipo == 5 ? .5f : .04f); mat.SetFloat("_Metallic", 0);
        if (tipo == 6) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(1, .22f, .025f) * 1.2f); }
        AssetDatabase.CreateAsset(mat, ruta); return mat;
    }
}
