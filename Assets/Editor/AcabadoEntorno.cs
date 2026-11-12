using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
using Pokimun.Exploracion;

/// <summary>Acabado del prototipo: materiales CC0, fuentes de luz locales y cierre visual del bosque.</summary>
public static class AcabadoEntorno
{
    private const string Carpeta = "Assets/Exploracion/Mejoras";
    private const string Texturas = "Assets/ThirdParty/PolyHaven/";
    private const string Bosque = "Assets/NIVELES/recursosNiveles/Bosque/Handpainted_Forest_pack/Models/";
    private const string Rocas = "Assets/NIVELES/recursosNiveles/Bosque/BrokenVector/LowPolyRockPack/Prefabs/";

    [MenuItem("Pokimun/Exploración/Pulir suelo del lobby y nubes")]
    public static void PulirSueloYCielo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salí de Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Guardá las escenas abiertas.");
        string copia = ".local-codex/Backups/SueloCielo-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(copia);
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" }) File.Copy("Assets/Scenes/" + nombre + ".unity", copia + "/" + nombre + ".unity");
        var lobby = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var g = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        var controlador = g.jugador.GetComponent<CharacterController>();
        controlador.height = 1.8f; controlador.center = Vector3.up * .9f; controlador.radius = .3f;
        controlador.skinWidth = .03f; controlador.stepOffset = .08f; controlador.slopeLimit = 45; controlador.minMoveDistance = 0;
        var animador = g.jugador.cuerpo.GetComponentInChildren<Animator>();
        animador.applyRootMotion = false; animador.Rebind(); animador.Update(0);
        var izquierdo = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
        var derecho = animador.GetBoneTransform(HumanBodyBones.RightFoot);
        float antes = Mathf.Min(izquierdo.position.y, derecho.position.y) - g.jugador.transform.position.y;
        // El hueso del pie está ligeramente por encima de la suela; los bounds incluyen otros elementos del modelo.
        g.jugador.cuerpo.position += Vector3.up * (.08f - antes);
        var paso = GameObject.Find("Paso de tierra trasero");
        paso.transform.position = new Vector3(0, -.012f, -12.6f);
        var colPaso = paso.GetComponent<Collider>(); if (colPaso != null) UnityEngine.Object.DestroyImmediate(colPaso);
        // La franja visual comparte el plano físico continuo del suelo de piedra.
        foreach (string nombre in new[] { "Hydros", "Ignis" })
        {
            var pedestal = GameObject.Find("Pedestal " + nombre);
            var caja = pedestal.GetComponent<BoxCollider>(); if (caja == null) caja = pedestal.AddComponent<BoxCollider>();
            caja.enabled = true; caja.isTrigger = false; caja.center = Vector3.zero; caja.size = Vector3.one;
            // Sin colliders en la estatua animada que puedan elevar o enganchar al jugador.
            var estatua = GameObject.Find("Estatua " + nombre);
            foreach (var c in estatua.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        }
        Rehornear(); EditorSceneManager.SaveScene(lobby);
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");
        var raiz = GameObject.Find("Nubes de atardecer");
        if (raiz == null)
        {
            raiz = new GameObject("Nubes de atardecer");
            string ruta = Carpeta + "/Nube estilizada.asset";
            var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (malla == null) { malla = CrearNube(); AssetDatabase.CreateAsset(malla, ruta); }
            string rutaMaterial = Carpeta + "/Nubes rosadas.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = new Color(.91f, .8f, .75f);
                material.SetFloat("_Smoothness", 0); material.enableInstancing = true; AssetDatabase.CreateAsset(material, rutaMaterial);
            }
            var azar = new System.Random(11242026);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                var o = new GameObject("Nube", typeof(MeshFilter), typeof(MeshRenderer)); o.transform.SetParent(raiz.transform, false);
                o.transform.position = new Vector3(Mathf.Cos(a) * (58 + i % 3 * 9), 25 + i % 4 * 4, Mathf.Sin(a) * 66);
                o.transform.rotation = Quaternion.Euler(0, (float)azar.NextDouble() * 360, 0);
                float escala = 3.3f + (float)azar.NextDouble() * 1.8f; o.transform.localScale = new Vector3(escala * 1.5f, escala * .6f, escala);
                o.GetComponent<MeshFilter>().sharedMesh = malla; var r = o.GetComponent<MeshRenderer>(); r.sharedMaterial = material;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }
        EditorSceneManager.SaveScene(escena); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/suelo-cielo.txt", "OK: offset anterior de pies=" + antes.ToString("F3") + "; suelas ajustadas, suelo físico continuo, escalones limitados a 8 cm, pedestales con BoxCollider; nubes=" + raiz.transform.childCount);
    }

    private static Mesh CrearNube()
    {
        var v = new List<Vector3>(); var t = new List<int>();
        Vector3[] centros = { new Vector3(-1.25f, 0, 0), new Vector3(0, .45f, 0), new Vector3(1.3f, .08f, 0), new Vector3(.1f, 0, .65f) };
        foreach (var centro in centros)
        for (int a = 0; a < 5; a++)
        for (int s = 0; s < 10; s++)
        {
            Func<int, int, Vector3> p = (anillo, segmento) => {
                float lat = Mathf.PI * anillo / 5, lon = Mathf.PI * 2 * segmento / 10;
                return centro + new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat) * .8f, Mathf.Sin(lat) * Mathf.Sin(lon));
            };
            foreach (var punto in new[] { p(a, s), p(a, s + 1), p(a + 1, s + 1), p(a, s), p(a + 1, s + 1), p(a + 1, s) }) { t.Add(v.Count); v.Add(punto); }
        }
        var malla = new Mesh { name = "Nube estilizada" }; malla.SetVertices(v); malla.SetTriangles(t, 0); malla.RecalculateNormals(); malla.RecalculateBounds(); return malla;
    }

    [MenuItem("Pokimun/Exploración/Atardecer, arbustos y simplificar lobby")]
    public static void AtardecerYArbustos()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salí de Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Guardá las escenas abiertas.");
        string copia = ".local-codex/Backups/Atardecer-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(copia);
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" }) File.Copy("Assets/Scenes/" + nombre + ".unity", copia + "/" + nombre + ".unity");
        var lobby = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var g = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        var quitar = g.destinos.Where(d => d.nombreVoz == "cristal" || d.nombreVoz == "mecanismo").ToArray();
        g.destinos = g.destinos.Except(quitar).ToArray();
        foreach (var d in quitar)
        {
            var cartel = GameObject.Find("Cartel " + d.nombreVoz.ToUpperInvariant());
            if (cartel != null) UnityEngine.Object.DestroyImmediate(cartel);
            UnityEngine.Object.DestroyImmediate(d.gameObject);
        }
        Rehornear(); EditorSceneManager.SaveScene(lobby);
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");
        g = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        var altura = Limites(g.ignis.gameObject).size.y;
        g.ignis.localScale *= 1.15f / Mathf.Max(.01f, altura);
        // El collider existente se escala junto al personaje y conserva la interacción.
        var sol = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l => l.type == LightType.Directional);
        sol.transform.rotation = Quaternion.Euler(17, -65, 0); sol.color = new Color(1, .63f, .36f); sol.intensity = 1.25f;
        RenderSettings.sun = sol;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.36f, .31f, .44f);
        RenderSettings.ambientEquatorColor = new Color(.48f, .31f, .22f);
        RenderSettings.ambientGroundColor = new Color(.16f, .18f, .14f);
        RenderSettings.ambientIntensity = 1; RenderSettings.reflectionIntensity = .25f;
        string rutaCielo = Carpeta + "/Cielo atardecer.mat";
        var cielo = AssetDatabase.LoadAssetAtPath<Material>(rutaCielo);
        if (cielo == null) { cielo = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(cielo, rutaCielo); }
        cielo.SetColor("_SkyTint", new Color(.56f, .38f, .49f)); cielo.SetColor("_GroundColor", new Color(.3f, .22f, .17f));
        cielo.SetFloat("_AtmosphereThickness", 1.35f); cielo.SetFloat("_Exposure", .9f);
        RenderSettings.skybox = cielo; EditorUtility.SetDirty(cielo);
        Camera.main.clearFlags = CameraClearFlags.Skybox;
        var raiz = GameObject.Find("Arbustos grandes");
        if (raiz == null)
        {
            raiz = new GameObject("Arbustos grandes");
            string ruta = Carpeta + "/Arbusto frondoso.asset";
            var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (malla == null) { malla = CrearMallaArbusto(); AssetDatabase.CreateAsset(malla, ruta); }
            string rutaMat = Carpeta + "/Hojas arbusto.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.color = new Color(.16f, .3f, .065f);
                material.SetFloat("_Smoothness", .02f); material.SetFloat("_Metallic", 0); material.enableInstancing = true;
                AssetDatabase.CreateAsset(material, rutaMat);
            }
            var azar = new System.Random(110126);
            foreach (var punto in g.escondites)
            for (int i = 0; i < 3; i++)
            {
                float angulo = (85 + i * 85) * Mathf.Deg2Rad;
                var o = new GameObject("Arbusto " + punto.name, typeof(MeshFilter), typeof(MeshRenderer));
                o.transform.SetParent(raiz.transform, false);
                o.transform.position = punto.position + new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo)) * 1.6f;
                o.transform.rotation = Quaternion.Euler(0, (float)azar.NextDouble() * 360, 0);
                o.transform.localScale = Vector3.one * (.85f + (float)azar.NextDouble() * .25f);
                o.GetComponent<MeshFilter>().sharedMesh = malla;
                o.GetComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, malla.subMeshCount).ToArray();
                // Vegetación atravesable, sin nuevos obstáculos de navegación ni MeshColliders.
            }
        }
        EditorSceneManager.SaveScene(escena); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/atardecer.txt", "OK: cristal y mecanismo retirados; Ignis de " + Limites(g.ignis.gameObject).size.y.ToString("F2") + " m; arbustos=" + raiz.transform.childCount + "; luz de atardecer solo en búsqueda.");
    }

    private static Mesh CrearMallaArbusto()
    {
        var vertices = new List<Vector3>();
        var caras = new[] { new List<int>(), new List<int>(), new List<int>() };
        var azar = new System.Random(1132026);
        Action<Vector3, Vector3, Vector3, int> triangulo = (a, b, c, tono) => {
            foreach (var p in new[] { a, b, c }) { caras[tono].Add(vertices.Count); vertices.Add(p); }
        };
        const int anillos = 8, segmentos = 15;
        var puntos = new Vector3[anillos + 1, segmentos];
        for (int a = 0; a <= anillos; a++)
        for (int s = 0; s < segmentos; s++)
        {
            float lat = Mathf.PI * a / anillos, lon = 2 * Mathf.PI * s / segmentos;
            float irregular = .91f + (float)azar.NextDouble() * .18f;
            puntos[a, s] = new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon) * 1.05f * irregular,
                .88f + Mathf.Cos(lat) * .84f, Mathf.Sin(lat) * Mathf.Sin(lon) * .92f * irregular);
        }
        for (int a = 0; a < anillos; a++)
        for (int s = 0; s < segmentos; s++)
        {
            int t = (s + 1) % segmentos;
            if (a > 0) triangulo(puntos[a, s], puntos[a, t], puntos[a + 1, t], azar.Next(3));
            if (a < anillos - 1) triangulo(puntos[a, s], puntos[a + 1, t], puntos[a + 1, s], azar.Next(3));
        }
        // Hojas tridimensionales con punta y nervio: rompen el contorno redondo del volumen interior.
        for (int i = 0; i < 105; i++)
        {
            float y = -.65f + (float)azar.NextDouble() * 1.6f, lon = (float)azar.NextDouble() * Mathf.PI * 2;
            var normal = new Vector3(Mathf.Sqrt(1 - y * y) * Mathf.Cos(lon), y, Mathf.Sqrt(1 - y * y) * Mathf.Sin(lon));
            var centro = new Vector3(normal.x * 1.02f, .88f + normal.y * .81f, normal.z * .9f);
            var lateral = Vector3.Cross(normal, Vector3.up).normalized;
            var eje = Vector3.Cross(lateral, normal).normalized;
            float ancho = .12f + (float)azar.NextDouble() * .1f, largo = .22f + (float)azar.NextDouble() * .12f;
            var punta = centro + eje * largo + normal * .28f;
            var baseHoja = centro - eje * largo;
            var izquierda = centro - lateral * ancho;
            var derecha = centro + lateral * ancho;
            var nervio = centro + normal * .17f;
            triangulo(baseHoja, izquierda, nervio, 0); triangulo(izquierda, punta, nervio, 1);
            triangulo(punta, derecha, nervio, 2); triangulo(derecha, baseHoja, nervio, 0);
        }
        var malla = new Mesh { name = "Arbusto de hojas angulares" }; malla.SetVertices(vertices); malla.subMeshCount = 3;
        for (int i = 0; i < 3; i++) malla.SetTriangles(caras[i], i);
        malla.RecalculateNormals(); malla.RecalculateBounds(); return malla;
    }

    [MenuItem("Pokimun/Exploración/Refinar arbustos con hojas angulares")]
    public static void RefinarArbustos()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salí de Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Guardá las escenas abiertas.");
        string copia = ".local-codex/Backups/Arbustos-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(copia);
        File.Copy("Assets/Scenes/BusquedaIgnis.unity", copia + "/BusquedaIgnis.unity");
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(Carpeta + "/Arbusto frondoso.asset");
        File.Copy(Carpeta + "/Arbusto frondoso.asset", copia + "/Arbusto.asset");
        var nueva = CrearMallaArbusto();
        malla.Clear(); malla.vertices = nueva.vertices; malla.subMeshCount = nueva.subMeshCount;
        for (int i = 0; i < nueva.subMeshCount; i++) malla.SetTriangles(nueva.GetTriangles(i), i);
        malla.RecalculateNormals(); malla.RecalculateBounds(); UnityEngine.Object.DestroyImmediate(nueva);
        malla.name = "Arbusto frondoso";
        var materiales = new Material[3];
        Color[] verdes = { new Color(.12f, .32f, .045f), new Color(.23f, .46f, .075f), new Color(.18f, .39f, .055f) };
        for (int i = 0; i < 3; i++)
        {
            string ruta = Carpeta + "/Follaje angular " + i + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, ruta); }
            mat.color = verdes[i]; mat.SetFloat("_Smoothness", 0); mat.SetFloat("_Metallic", 0); mat.SetFloat("_Cull", 0);
            mat.enableInstancing = true; EditorUtility.SetDirty(mat); materiales[i] = mat;
        }
        var raiz = GameObject.Find("Arbustos grandes").transform;
        foreach (var r in raiz.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterials = materiales;
            // Mantener apoyo en el suelo al cambiar la silueta de la malla.
            r.transform.position += Vector3.up * (-.035f - r.bounds.min.y);
        }
        EditorUtility.SetDirty(malla); EditorSceneManager.SaveScene(escena); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/arbustos-hojas.txt", "OK: " + raiz.childCount + " arbustos con hojas angulares, tres tonos verdes y " + malla.triangles.Length / 3 + " triángulos por arbusto.");
        var primero = raiz.GetChild(16);
        Selection.activeGameObject = primero.gameObject;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(primero.position + Vector3.up * .8f, Quaternion.Euler(20, 145, 0), 2.8f);
    }

    [MenuItem("Pokimun/Exploración/Corregir apoyo y cobertura de búsqueda")]
    public static void CorregirBusqueda()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salí de Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Guardá las escenas abiertas.");
        string copia = ".local-codex/Backups/Busqueda-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(copia);
        File.Copy("Assets/Scenes/BusquedaIgnis.unity", copia + "/BusquedaIgnis.unity");
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");
        var gestor = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        int arboles = 0;
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var filtro = r.GetComponent<MeshFilter>();
            string nombre = filtro != null && filtro.sharedMesh != null ? filtro.sharedMesh.name : r.name;
            if (!(nombre.StartsWith("Fir_") || nombre.StartsWith("Pine_"))) continue;
            // Ajustar todos los árboles, incluidos los heredados del escenario original.
            r.transform.position += Vector3.up * (-.04f - r.bounds.min.y);
            arboles++;
        }
        var detalles = GameObject.Find("Detalles cobertizo").transform;
        string rutaFronton = Carpeta + "/Fronton cobertizo.asset";
        var fronton = AssetDatabase.LoadAssetAtPath<Mesh>(rutaFronton);
        if (fronton == null) { fronton = new Mesh { name = "Frontón de madera" }; AssetDatabase.CreateAsset(fronton, rutaFronton); }
        // Prisma cerrado: evita caras superpuestas y también cierra el hueco visto desde dentro.
        fronton.Clear();
        fronton.vertices = new[] {
            new Vector3(-4, 3.6f, -.08f), new Vector3(4, 3.6f, -.08f), new Vector3(0, 4.84f, -.08f),
            new Vector3(-4, 3.6f, .08f), new Vector3(4, 3.6f, .08f), new Vector3(0, 4.84f, .08f) };
        fronton.uv = new[] { Vector2.zero, Vector2.right, new Vector2(.5f, .55f), Vector2.zero, Vector2.right, new Vector2(.5f, .55f) };
        fronton.triangles = new[] { 2, 1, 0, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
        fronton.RecalculateNormals(); fronton.RecalculateBounds(); EditorUtility.SetDirty(fronton);
        if (detalles.Find("Cierre triangular frontal") == null)
        {
            foreach (bool frente in new[] { true, false })
            {
                var o = new GameObject(frente ? "Cierre triangular frontal" : "Cierre triangular trasero", typeof(MeshFilter), typeof(MeshRenderer));
                o.transform.SetParent(detalles, false); o.transform.position = new Vector3(0, 0, frente ? -8.51f : -15.49f);
                o.GetComponent<MeshFilter>().sharedMesh = fronton;
                o.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "/Tablas cobertizo CC0.mat");
            }
        }
        var animador = gestor.jugador.cuerpo.GetComponentInChildren<Animator>();
        animador.Rebind(); animador.Update(0);
        var cuerpo = gestor.jugador.cuerpo;
        var limites = Limites(cuerpo.gameObject);
        // La escala visual se ajusta al controlador de 1,8 m y a sus ojos a 1,65 m.
        cuerpo.localScale *= 1.8f / Mathf.Max(.01f, limites.size.y);
        limites = Limites(cuerpo.gameObject);
        cuerpo.position += Vector3.up * (gestor.jugador.transform.position.y - limites.min.y);
        var follaje = GameObject.Find("Follaje de escondites");
        if (follaje == null)
        {
            follaje = new GameObject("Follaje de escondites");
            var azar = new System.Random(112026);
            string[] plantas = { "Grass_v1_1.prefab", "Grass_v1_2.prefab", "Grass_v1_3.prefab", "Grass_v2_1.prefab" };
            foreach (var escondite in gestor.escondites)
            for (int i = 0; i < 7; i++)
            {
                // Dejar un claro de acceso por el lado derecho, donde llegan las rutas de voz.
                float angulo = (80 + i * 34 + (float)azar.NextDouble() * 12) * Mathf.Deg2Rad;
                float radio = 1.25f + (float)azar.NextDouble() * .9f;
                Vector3 p = escondite.position + new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo)) * radio;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Bosque + plantas[azar.Next(plantas.Length)]);
                var planta = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                planta.name = "Cobertura " + escondite.name;
                planta.transform.rotation = Quaternion.Euler(0, (float)azar.NextDouble() * 360, 0);
                var b = Limites(planta);
                float altura = 1.25f + (float)azar.NextDouble() * .55f;
                planta.transform.localScale *= altura / Mathf.Max(.01f, b.size.y);
                b = Limites(planta); planta.transform.position += p - new Vector3(b.center.x, b.min.y + .03f, b.center.z);
                planta.transform.SetParent(follaje.transform, true);
                foreach (var c in planta.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            }
        }
        EditorSceneManager.SaveScene(escena); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/correccion-busqueda.txt", "OK: árboles apoyados=" + arboles + "; cobertura=" + follaje.transform.childCount + "; Hydros=" + Limites(cuerpo.gameObject).size.y.ToString("F2") + " m; frontones cerrados.");
    }

    [MenuItem("Pokimun/Exploración/Aplicar acabado de materiales y bosque")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salí de Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Guardá las escenas abiertas antes de aplicar el acabado.");
        string copia = ".local-codex/Backups/Acabado-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(copia); Directory.CreateDirectory(Carpeta); AssetDatabase.Refresh();
        foreach (string nombre in new[] { "Lobby", "BusquedaIgnis" }) File.Copy("Assets/Scenes/" + nombre + ".unity", copia + "/" + nombre + ".unity");
        ImportarTexturas();
        var tierra = MaterialTextura("Tierra seca CC0", "brown_mud_dry", new Vector2(10, 1.6f), new Color(.92f, .88f, .79f), .23f);
        var tablas = MaterialTextura("Tablas cobertizo CC0", "wood_planks", new Vector2(4, 2), new Color(.88f, .83f, .74f), .22f);
        var suelo = MaterialTextura("Suelo madera CC0", "wood_planks", new Vector2(4, 3.5f), new Color(.8f, .75f, .65f), .2f);
        var vigas = MaterialTextura("Vigas madera CC0", "wood_planks", new Vector2(1, 3), new Color(.49f, .43f, .35f), .15f);
        var techo = MaterialTextura("Techo madera CC0", "wood_planks", new Vector2(2, 4), new Color(.7f, .65f, .54f), .2f);
        var lobby = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        GameObject.Find("Paso de tierra trasero").GetComponent<Renderer>().sharedMaterial = tierra;
        IluminarLobby();
        var luces = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.enabled).ToArray();
        if (luces.Length != 6 || luces.Any(l => l.type != LightType.Point))
            throw new Exception("El lobby debe iluminarse únicamente desde sus seis fuentes locales.");
        EditorSceneManager.SaveScene(lobby);
        var busqueda = EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");
        DetallarCobertizo(tablas, suelo, vigas, techo);
        CerrarBosque();
        Rehornear();
        EditorSceneManager.SaveScene(busqueda); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/acabado.txt", "OK: tierra y madera CC0, luz local en lobby, cobertizo detallado y cierre irregular del bosque.");
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
    }

    private static void ImportarTexturas()
    {
        foreach (string archivo in Directory.GetFiles(Texturas, "*.jpg", SearchOption.AllDirectories))
        {
            string ruta = archivo.Replace('\\', '/');
            var t = (TextureImporter)AssetImporter.GetAtPath(ruta);
            bool normal = ruta.Contains("nor_gl"), ao = ruta.Contains("_ao_");
            t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            t.sRGBTexture = !normal && !ao; t.mipmapEnabled = true; t.wrapMode = TextureWrapMode.Repeat;
            t.maxTextureSize = normal || ao ? 512 : 1024; t.textureCompression = TextureImporterCompression.Compressed;
            t.anisoLevel = normal || ao ? 1 : 4; t.SaveAndReimport();
        }
    }
    private static Material MaterialTextura(string nombre, string recurso, Vector2 escala, Color color, float relieve)
    {
        string ruta = Carpeta + "/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, ruta); }
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + recurso + "/" + recurso + "_diff_1k.jpg");
        mat.color = color; mat.mainTextureScale = escala; mat.SetFloat("_Metallic", 0); mat.SetFloat("_Smoothness", .015f);
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + recurso + "/" + recurso + "_nor_gl_1k.jpg"));
        mat.SetFloat("_BumpScale", relieve); mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Texturas + recurso + "/" + recurso + "_ao_1k.jpg"));
        mat.SetFloat("_OcclusionStrength", .45f); EditorUtility.SetDirty(mat); return mat;
    }
    private static void IluminarLobby()
    {
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)) l.enabled = false;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.black; RenderSettings.ambientIntensity = 0;
        RenderSettings.skybox = null; RenderSettings.sun = null; RenderSettings.reflectionIntensity = 0;
        RenderSettings.ambientProbe = new SphericalHarmonicsL2();
        var camara = Camera.main; if (camara != null) { camara.clearFlags = CameraClearFlags.SolidColor; camara.backgroundColor = new Color(.012f, .018f, .022f); }
        var raiz = GameObject.Find("Iluminación del santuario");
        if (raiz == null) raiz = new GameObject("Iluminación del santuario");
        Luz("Agua", new Vector3(-7.3f, .8f, 3.3f), new Color(.28f, .7f, 1), 8, 18, raiz.transform, true);
        Luz("Lava", new Vector3(7.3f, .8f, 3.3f), new Color(1, .37f, .11f), 7, 18, raiz.transform, false);
        Luz("Portal combate", new Vector3(6, 2.1f, 12.9f), new Color(1, .42f, .12f), 2.5f, 7, raiz.transform, false);
        Luz("Portal búsqueda", new Vector3(-6, 2.1f, 12.9f), new Color(.2f, .68f, 1), 2.5f, 7, raiz.transform, false);
        foreach (string personaje in new[] { "Hydros", "Ignis" })
        {
            bool esAgua = personaje == "Hydros";
            Luz("Estatua " + personaje, new Vector3(esAgua ? -6 : 6, 2.4f, -2.5f),
                esAgua ? new Color(.22f, .7f, 1) : new Color(1, .35f, .12f), 18, 24, raiz.transform, false);
            var estatua = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .First(o => o.name == "Estatua " + personaje);
            foreach (var renderer in estatua.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var materiales = renderer.sharedMaterials;
                for (int i = 0; i < materiales.Length; i++)
                {
                    string ruta = Carpeta + "/Emisión " + personaje + " " + renderer.name.Replace('/', '_') + " " + i + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (mat == null) { mat = new Material(materiales[i]); AssetDatabase.CreateAsset(mat, ruta); }
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        if (mat.mainTexture != null && mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", mat.mainTexture);
                        mat.SetColor("_EmissionColor", (esAgua ? new Color(.12f, .45f, .8f) : new Color(.65f, .16f, .03f)) * .35f);
                    }
                    materiales[i] = mat; EditorUtility.SetDirty(mat);
                }
                renderer.sharedMaterials = materiales;
            }
        }
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.name == "Portal combate" || r.name == "Portal busqueda" || r.name == "estanque de agua" || r.name == "estanque de lava")
            {
                var original = r.sharedMaterial;
                string ruta = Carpeta + "/Emisión " + r.name + ".mat";
                var emisor = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (emisor == null) { emisor = new Material(original); AssetDatabase.CreateAsset(emisor, ruta); }
                if (r.name.StartsWith("Portal "))
                {
                    bool combate = r.name == "Portal combate";
                    emisor.shader = Shader.Find("Pokimun/Portal Santuario");
                    emisor.DisableKeyword("_EMISSION");
                    emisor.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    emisor.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    emisor.SetColor("_BaseColor", combate
                        ? new Color(.85f, .31f, .065f) : new Color(.065f, .54f, .83f));
                    emisor.SetFloat("_Agua", combate ? 0f : 1f);
                }
                else
                {
                    emisor.EnableKeyword("_EMISSION");
                    Color c = r.name.Contains("lava") ? new Color(1, .25f, .04f) : new Color(.06f, .6f, 1);
                    emisor.SetColor("_EmissionColor", c * .9f);
                    if (emisor.mainTexture != null) emisor.SetTexture("_EmissionMap", emisor.mainTexture);
                }
                r.sharedMaterial = emisor; EditorUtility.SetDirty(emisor);
            }
        }
    }
    private static void Luz(string nombre, Vector3 posicion, Color color, float intensidad, float alcance, Transform padre, bool sombras)
    {
        var t = padre.Find(nombre); var go = t != null ? t.gameObject : new GameObject(nombre);
        go.transform.SetParent(padre, false); go.transform.position = posicion;
        var luz = go.GetComponent<Light>();
        if (luz == null) luz = go.AddComponent<Light>();
        luz.enabled = true; luz.type = LightType.Point;
        luz.color = color; luz.intensity = intensidad; luz.range = alcance; luz.bounceIntensity = 0;
        luz.shadows = sombras ? LightShadows.Soft : LightShadows.None; luz.shadowStrength = .6f;
        luz.shadowNormalBias = .25f; luz.renderMode = LightRenderMode.ForcePixel;
        var datos = new SerializedObject(luz.GetUniversalAdditionalLightData());
        datos.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 0;
        datos.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DetallarCobertizo(Material tablas, Material suelo, Material vigas, Material techo)
    {
        var volumen = GameObject.Find("Interior cobertizo").GetComponent<BoxCollider>();
        var gestor = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        var regreso = gestor.destinos.First(d => d.nombreVoz == "lobby");
        regreso.llegada.position = new Vector3(0, .06f, -13.2f);
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.name == "Suelo cobertizo") r.sharedMaterial = suelo;
            if (r.name.StartsWith("Pared ") && r.name.Contains("cobertizo") || r.name.StartsWith("Frente ") || r.name == "Dintel cobertizo") r.sharedMaterial = tablas;
            bool vegetacion = r.name.StartsWith("Grass_") || r.name.StartsWith("Fir_") || r.name.StartsWith("Pine_");
            if (vegetacion && volumen.bounds.Contains(new Vector3(r.bounds.center.x, 1, r.bounds.center.z))) r.gameObject.SetActive(false);
        }
        if (GameObject.Find("Detalles cobertizo") != null) return;
        var raiz = new GameObject("Detalles cobertizo").transform;
        var viejoTecho = GameObject.Find("Techo cobertizo"); if (viejoTecho != null) viejoTecho.SetActive(false);
        foreach (int lado in new[] { -1, 1 })
        {
            var agua = Cubo("Faldón de techo", new Vector3(lado * 2.05f, 4.18f, -12), new Vector3(4.5f, .22f, 8), techo, raiz);
            agua.transform.rotation = Quaternion.Euler(0, 0, lado * -18);
            foreach (float z in new[] { -15.4f, -12, -8.6f })
                Cubo("Poste del cobertizo", new Vector3(lado * 3.82f, 1.8f, z), new Vector3(.2f, 3.6f, .22f), vigas, raiz);
            Cubo("Marco de puerta", new Vector3(lado * 1.2f, 1.65f, -8.43f), new Vector3(.18f, 3.3f, .22f), vigas, raiz);
            Cubo("Zócalo de madera", new Vector3(lado * 3.8f, .22f, -12), new Vector3(.15f, .35f, 6.9f), vigas, raiz);
            var tirante = Cubo("Tirante de techo", new Vector3(lado * 1.8f, 4, -8.55f), new Vector3(4.1f, .15f, .18f), vigas, raiz);
            tirante.transform.rotation = Quaternion.Euler(0, 0, lado * -18);
        }
        Cubo("Dintel de puerta", new Vector3(0, 3.32f, -8.43f), new Vector3(2.6f, .22f, .24f), vigas, raiz);
        Cubo("Viga cumbrera", new Vector3(0, 4.85f, -12), new Vector3(.22f, .24f, 8), vigas, raiz);
        foreach (Vector3 p in new[] { new Vector3(-2.8f, .5f, -14.1f), new Vector3(2.8f, .5f, -14.1f) })
        {
            Cubo("Caja de madera", p, Vector3.one, tablas, raiz);
            for (int i = -1; i <= 1; i += 2) Cubo("Listón de caja", p + new Vector3(i * .4f, 0, -.51f), new Vector3(.1f, .98f, .08f), vigas, raiz);
        }
        Cubo("Banco de trabajo", new Vector3(2.8f, .95f, -11.7f), new Vector3(1.4f, .14f, 1.2f), tablas, raiz);
        foreach (int x in new[] { -1, 1 })
        foreach (int z in new[] { -1, 1 })
            Cubo("Pata banco de trabajo", new Vector3(2.8f + x * .55f, .46f, -11.7f + z * .45f), new Vector3(.12f, .92f, .12f), vigas, raiz);
    }

    private static void CerrarBosque()
    {
        if (GameObject.Find("Cierre natural irregular") != null) return;
        var raiz = new GameObject("Cierre natural irregular").transform;
        // La barrera física rectangular ya existente se conserva. Este anillo queda fuera de ella.
        var originales = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Árbol límite").ToArray();
        foreach (var t in originales) t.gameObject.SetActive(false);
        var azar = new System.Random(19112026);
        string[] arboles = { "Fir_v1_1.prefab", "Fir_v1_2.prefab", "Pine_v1_1.prefab", "Pine_v1_2.prefab" };
        for (int lado = 0; lado < 4; lado++)
        {
            float largo = lado < 2 ? 40 : 34;
            for (int fila = 0; fila < 2; fila++)
            for (float paso = -1; paso < largo + 2; paso += 2.8f + (float)azar.NextDouble() * 1.7f)
            {
                float distancia = 1.4f + fila * 3.2f + (float)azar.NextDouble() * 1.5f;
                Vector3 p = PuntoBorde(lado, paso, distancia);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Bosque + arboles[azar.Next(arboles.Length)]);
                var arbol = (GameObject)PrefabUtility.InstantiatePrefab(prefab); arbol.name = "Árbol del perímetro";
                arbol.transform.rotation = Quaternion.Euler(0, (float)azar.NextDouble() * 360, 0);
                float escala = 1.15f + (float)azar.NextDouble() * .75f;
                arbol.transform.localScale = new Vector3(escala, escala * (.9f + (float)azar.NextDouble() * .3f), escala);
                Bounds b = Limites(arbol); arbol.transform.position += p - new Vector3(b.center.x, b.min.y, b.center.z);
                arbol.transform.SetParent(raiz, true);
                foreach (var c in arbol.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                GameObjectUtility.SetStaticEditorFlags(arbol, StaticEditorFlags.BatchingStatic);
            }
            for (float paso = -1; paso < largo + 2; paso += 3.6f + (float)azar.NextDouble() * 1.1f)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Rocas + (azar.Next(2) == 0 ? "Rock Type1 01.prefab" : "Rock Type2 01.prefab"));
                var roca = (GameObject)PrefabUtility.InstantiatePrefab(prefab); roca.name = "Roca grande de cierre";
                Bounds b = Limites(roca);
                Vector3 tamano = new Vector3(5.2f + (float)azar.NextDouble() * 2, 2.9f + (float)azar.NextDouble() * 1.3f, 4.8f);
                roca.transform.localScale = new Vector3(tamano.x / b.size.x, tamano.y / b.size.y, tamano.z / b.size.z);
                roca.transform.rotation = Quaternion.Euler(0, (lado < 2 ? 90 : 0) + (float)azar.NextDouble() * 30, 0);
                Vector3 p = PuntoBorde(lado, paso, 1.2f); b = Limites(roca);
                roca.transform.position += p - new Vector3(b.center.x, b.min.y + .25f, b.center.z);
                roca.transform.SetParent(raiz, true);
                foreach (var c in roca.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                b = Limites(roca);
                if (b.Intersects(new Bounds(new Vector3(0, 2, 2), new Vector3(34, 6, 40))))
                {
                    var colision = new GameObject("Colisión simple roca de cierre");
                    colision.transform.position = b.center; colision.transform.SetParent(roca.transform, true);
                    colision.AddComponent<BoxCollider>().size = b.size * .85f;
                }
                GameObjectUtility.SetStaticEditorFlags(roca, StaticEditorFlags.BatchingStatic);
            }
        }
        var piso = AssetDatabase.LoadAssetAtPath<GameObject>(Bosque + "Floor_Grass_v1_2.prefab");
        var fondo = GameObject.CreatePrimitive(PrimitiveType.Plane); fondo.name = "Suelo exterior de fondo";
        fondo.transform.SetParent(raiz, false); fondo.transform.position = new Vector3(0, -.12f, 2); fondo.transform.localScale = new Vector3(6.8f, 1, 7.4f);
        UnityEngine.Object.DestroyImmediate(fondo.GetComponent<Collider>());
        string ruta = Carpeta + "/Suelo fondo bosque.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null) { mat = new Material(piso.GetComponentInChildren<Renderer>().sharedMaterial); mat.mainTextureScale = new Vector2(10, 10); AssetDatabase.CreateAsset(mat, ruta); }
        fondo.GetComponent<Renderer>().sharedMaterial = mat;
    }
    private static Vector3 PuntoBorde(int lado, float t, float distancia)
    {
        if (lado == 0) return new Vector3(-17 - distancia, 0, -18 + t);
        if (lado == 1) return new Vector3(17 + distancia, 0, -18 + t);
        if (lado == 2) return new Vector3(-17 + t, 0, -18 - distancia);
        return new Vector3(-17 + t, 0, 22 + distancia);
    }
    private static Bounds Limites(GameObject obj)
    {
        var rs = obj.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    private static GameObject Cubo(string nombre, Vector3 posicion, Vector3 tamano, Material mat, Transform padre)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = nombre; o.transform.position = posicion;
        o.transform.localScale = tamano; o.transform.SetParent(padre, true); o.GetComponent<Renderer>().sharedMaterial = mat; return o;
    }
    private static void Rehornear()
    {
        var g = UnityEngine.Object.FindFirstObjectByType<GestorExploracion>(); var surface = g.GetComponent<NavMeshSurface>();
        var anterior = surface.navMeshData; surface.RemoveData();
        var cols = g.ignis != null ? g.ignis.GetComponentsInChildren<Collider>() : Array.Empty<Collider>(); foreach (var c in cols) c.enabled = false;
        surface.BuildNavMesh(); surface.RemoveData(); string nombre = anterior.name;
        EditorUtility.CopySerialized(surface.navMeshData, anterior); anterior.name = nombre;
        surface.navMeshData = anterior; surface.AddData(); EditorUtility.SetDirty(anterior);
        foreach (var c in cols) c.enabled = true;
    }
}
