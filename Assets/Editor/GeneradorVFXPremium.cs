using UnityEngine;
using UnityEditor;

/// <summary>
/// Genera VFX premium V3 para Hydros e Ignis.
/// Menu: Pokimun > Magia > Generar VFX Premium V3
/// </summary>
public class GeneradorVFXPremium : Editor
{
    [MenuItem("Pokimun/Magia/Generar VFX Premium V3")]
    public static void GenerarTodo()
    {
        if (!System.IO.Directory.Exists("Assets/Prefabs/VFX"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs/VFX");

        Shader particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Shader particlesLit = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (particles == null) { Debug.LogError("[VFX] No se encontró el shader URP Particles. Revisa que el proyecto use URP."); return; }

        GenerarAguaBasicoV3(particles, particlesLit);
        GenerarAguaCriticoV3(particles, particlesLit);
        GenerarFuegoBasicoV3(particles, particlesLit);
        GenerarFuegoCriticoV3(particles, particlesLit);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VFX] ¡4 efectos premium generados en Assets/Prefabs/VFX/!");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // AGUA BÁSICO V3 — Hidro Pulso
    // Rayo de agua con núcleo brillante blanco-celeste + halo exterior translúcido
    // ════════════════════════════════════════════════════════════════════════════
    static void GenerarAguaBasicoV3(Shader particles, Shader particlesLit)
    {
        var root = new GameObject("AtaqueAgua_Basico_V3_VFX");
        var mov = root.AddComponent<AtaqueVisualAgua>();
        mov.velocidad = 26f; mov.tiempoDeVida = 2f;

        // --- Núcleo brillante (blanco-celeste, intenso) ---
        var psNucleo = root.AddComponent<ParticleSystem>();
        {
            var m = psNucleo.main;
            m.duration = 1f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(28f, 36f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            m.startColor = new Color(0.85f, 0.97f, 1f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = psNucleo.emission; em.rateOverTime = 300f;
            var sh = psNucleo.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 1f; sh.radius = 0.08f;

            var col = psNucleo.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new(new Color(1f, 1f, 1f), 0f),
                new(new Color(0.3f, 0.85f, 1f), 0.5f),
                new(new Color(0f, 0.4f, 0.9f), 1f) },
                new GradientAlphaKey[] { new(1f, 0f), new(0.8f, 0.5f), new(0f, 1f) });
            col.color = g;

            var sz = psNucleo.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0,0.6f), new Keyframe(0.2f,1f), new Keyframe(1f,0f)));

            var rend = root.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.lengthScale = 5f; rend.velocityScale = 0.04f;
            rend.sortingOrder = 1;
            var mat = CreateParticleMat(particles, new Color(0.5f, 0.92f, 1f, 1f), 2, "MatAguaNucleoV3"); // Additive
            rend.material = mat;
        }

        // --- Halo agua exterior (azul translúcido, burbujas flotando) ---
        var goBurbujas = new GameObject("Halo_Agua"); goBurbujas.transform.SetParent(root.transform);
        var psBurbujas = goBurbujas.AddComponent<ParticleSystem>();
        {
            var m = psBurbujas.main;
            m.duration = 1f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(18f, 28f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            m.startColor = new Color(0.1f, 0.6f, 1f, 0.35f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = psBurbujas.emission; em.rateOverTime = 120f;
            var sh = psBurbujas.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 4f; sh.radius = 0.25f;

            var col = psBurbujas.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] { new(new Color(0.5f, 0.9f, 1f), 0f), new(new Color(0f, 0.3f, 0.8f), 1f) },
                new GradientAlphaKey[] { new(0.5f, 0f), new(0.25f, 0.7f), new(0f, 1f) });
            col.color = g;

            var rend = goBurbujas.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = CreateParticleMat(particles, new Color(0.1f, 0.5f, 1f, 0.35f), 0, "MatAguaHaloV3"); // Alpha
        }

        // --- Chispas de agua (destello blanco, muy pequeñas y rápidas) ---
        var goChispas = new GameObject("Chispas_Agua"); goChispas.transform.SetParent(root.transform);
        var psChispas = goChispas.AddComponent<ParticleSystem>();
        {
            var m = psChispas.main;
            m.duration = 1f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            m.startColor = Color.white;
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = psChispas.emission; em.rateOverTime = 80f;
            var sh = psChispas.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.1f;
            var rend = goChispas.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch; rend.lengthScale = 3f;
            rend.material = CreateParticleMat(particles, new Color(0.8f, 0.95f, 1f, 1f), 2, "MatAguaChispaV3");
        }

        SavePrefab(root, "Assets/Prefabs/VFX/AtaqueAgua_Basico_V3_VFX.prefab");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // AGUA CRÍTICO V3 — Explosión esférica alrededor de las manos
    // ════════════════════════════════════════════════════════════════════════════
    static void GenerarAguaCriticoV3(Shader particles, Shader particlesLit)
    {
        var root = new GameObject("AtaqueAgua_Critico_V3_VFX");
        var mov = root.AddComponent<AtaqueVisualAgua>();
        mov.velocidad = 40f; mov.tiempoDeVida = 2.8f;

        // --- Explosion central (burst masivo) ---
        var psExplosion = root.AddComponent<ParticleSystem>();
        {
            var m = psExplosion.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 18f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.8f);
            m.startColor = new Color(0.7f, 0.95f, 1f, 0.5f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.gravityModifier = -0.05f; // Sube levemente como agua

            var em = psExplosion.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new(0f, 250, 350) });
            var sh = psExplosion.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;

            var col = psExplosion.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new(Color.white, 0f),
                new(new Color(0.4f, 0.9f, 1f), 0.3f),
                new(new Color(0f, 0.3f, 0.9f), 1f) },
                new GradientAlphaKey[] { new(0.8f, 0f), new(0.4f, 0.5f), new(0f, 1f) });
            col.color = g;

            var sz = psExplosion.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0,0.2f), new Keyframe(0.15f,1.2f), new Keyframe(1f,1.8f)));

            var rend = root.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = CreateParticleMat(particles, new Color(0.4f, 0.88f, 1f, 0.55f), 0, "MatAguaExplosionV3");
        }

        // --- Destellos blancos (chispas rapidas) ---
        var goDestellos = new GameObject("Destellos"); goDestellos.transform.SetParent(root.transform);
        var psDestellos = goDestellos.AddComponent<ParticleSystem>();
        {
            var m = psDestellos.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(8f, 30f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            m.startColor = Color.white;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = psDestellos.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new(0f, 100, 150) });
            var sh = psDestellos.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var rend = goDestellos.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch; rend.lengthScale = 4f;
            rend.material = CreateParticleMat(particles, Color.white, 2, "MatAguaDestelloV3");
        }

        SavePrefab(root, "Assets/Prefabs/VFX/AtaqueAgua_Critico_V3_VFX.prefab");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // FUEGO BÁSICO V3 — Piro Pulso
    // Bola/rayo de fuego con nucleo blanco-amarillo + llamas naranjas translúcidas
    // ════════════════════════════════════════════════════════════════════════════
    static void GenerarFuegoBasicoV3(Shader particles, Shader particlesLit)
    {
        var root = new GameObject("AtaqueFuego_Basico_V3_VFX");
        var mov = root.AddComponent<AtaqueVisualAgua>();
        mov.velocidad = 20f; mov.tiempoDeVida = 2.5f;

        // --- Núcleo brillante (blanco-amarillo) ---
        var psNucleo = root.AddComponent<ParticleSystem>();
        {
            var m = psNucleo.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            m.startColor = new Color(1f, 0.95f, 0.7f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;

            var em = psNucleo.emission; em.rateOverTime = 90f;
            var sh = psNucleo.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.18f;

            var col = psNucleo.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new(new Color(1f, 1f, 0.8f), 0f),
                new(new Color(1f, 0.55f, 0f), 0.45f),
                new(new Color(0.6f, 0.05f, 0f), 1f) },
                new GradientAlphaKey[] { new(1f, 0f), new(0.7f, 0.5f), new(0f, 1f) });
            col.color = g;

            var sz = psNucleo.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0,0.4f), new Keyframe(0.25f,1f), new Keyframe(1f,0.05f)));

            var rend = root.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard; rend.sortingOrder = 1;
            rend.material = CreateParticleMat(particles, new Color(1f, 0.6f, 0.1f, 1f), 2, "MatFuegoNucleoV3");
        }

        // --- Llamas exteriores translúcidas (naranja-rojo) ---
        var goLlamas = new GameObject("Llamas"); goLlamas.transform.SetParent(root.transform);
        var psLlamas = goLlamas.AddComponent<ParticleSystem>();
        {
            var m = psLlamas.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0f, 1.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            m.startColor = new Color(1f, 0.3f, 0.05f, 0.45f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.gravityModifier = -0.08f;

            var em = psLlamas.emission; em.rateOverTime = 55f;
            var sh = psLlamas.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;

            var col = psLlamas.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new(new Color(1f, 0.5f, 0f), 0f),
                new(new Color(0.6f, 0.08f, 0f), 0.6f),
                new(new Color(0.15f, 0.1f, 0.1f), 1f) },
                new GradientAlphaKey[] { new(0.55f, 0f), new(0.3f, 0.6f), new(0f, 1f) });
            col.color = g;

            var rend = goLlamas.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = CreateParticleMat(particles, new Color(1f, 0.3f, 0.05f, 0.4f), 0, "MatFuegoLlamasV3");
        }

        // --- Chispas (embers) ---
        var goChispas = new GameObject("Chispas_Fuego"); goChispas.transform.SetParent(root.transform);
        var psChispas = goChispas.AddComponent<ParticleSystem>();
        {
            var m = psChispas.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            m.startColor = new Color(1f, 0.75f, 0.1f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.gravityModifier = 0.15f;

            var em = psChispas.emission; em.rateOverTime = 50f;
            var sh = psChispas.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
            var rend = goChispas.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch; rend.lengthScale = 2.5f;
            rend.material = CreateParticleMat(particles, new Color(1f, 0.75f, 0.1f, 1f), 2, "MatFuegoChispaV3");
        }

        SavePrefab(root, "Assets/Prefabs/VFX/AtaqueFuego_Basico_V3_VFX.prefab");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // FUEGO CRÍTICO V3 — Bola de Fuego Colosal
    // ════════════════════════════════════════════════════════════════════════════
    static void GenerarFuegoCriticoV3(Shader particles, Shader particlesLit)
    {
        var root = new GameObject("AtaqueFuego_Critico_V3_VFX");
        var mov = root.AddComponent<AtaqueVisualAgua>();
        mov.velocidad = 10f; mov.tiempoDeVida = 4.5f;

        // --- Núcleo central (blanco puro — el punto más caliente) ---
        var psNucleo = root.AddComponent<ParticleSystem>();
        {
            var m = psNucleo.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(1.8f, 2.5f);
            m.startColor = new Color(1f, 0.98f, 0.9f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = psNucleo.emission; em.rateOverTime = 80f;
            var sh = psNucleo.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;

            var col = psNucleo.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new(Color.white, 0f),
                new(new Color(1f, 0.8f, 0.2f), 0.3f),
                new(new Color(1f, 0.3f, 0f), 0.7f),
                new(new Color(0.2f, 0.05f, 0f), 1f) },
                new GradientAlphaKey[] { new(1f, 0f), new(0.9f, 0.4f), new(0f, 1f) });
            col.color = g;

            var sz = psNucleo.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0,0.3f), new Keyframe(0.2f,1f), new Keyframe(1f,0.6f)));

            var rend = root.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard; rend.sortingOrder = 2;
            rend.material = CreateParticleMat(particles, new Color(1f, 0.75f, 0.2f, 1f), 2, "MatFuegoCriticoNucleoV3");
        }

        // --- Corona de llamas (naranja/rojo, masiva) ---
        var goCorona = new GameObject("Corona_Llamas"); goCorona.transform.SetParent(root.transform);
        var psCorona = goCorona.AddComponent<ParticleSystem>();
        {
            var m = psCorona.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f);
            m.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
            m.startColor = new Color(1f, 0.25f, 0f, 0.55f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.gravityModifier = -0.12f;
            var em = psCorona.emission; em.rateOverTime = 65f;
            var sh = psCorona.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.0f;

            var col = psCorona.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new GradientColorKey[] {
                new(new Color(1f, 0.5f, 0f), 0f),
                new(new Color(0.7f, 0.08f, 0f), 0.5f),
                new(new Color(0.05f, 0.05f, 0.05f), 1f) },
                new GradientAlphaKey[] { new(0.65f, 0f), new(0.35f, 0.5f), new(0f, 1f) });
            col.color = g;

            var rend = goCorona.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = CreateParticleMat(particles, new Color(1f, 0.25f, 0f, 0.5f), 0, "MatFuegoCriticoCoronaV3");
        }

        // --- Chispas masivas volando (embers grandes) ---
        var goEmbers = new GameObject("Embers"); goEmbers.transform.SetParent(root.transform);
        var psEmbers = goEmbers.AddComponent<ParticleSystem>();
        {
            var m = psEmbers.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 14f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            m.startColor = new Color(1f, 0.7f, 0.1f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.gravityModifier = 0.2f;
            var em = psEmbers.emission; em.rateOverTime = 80f;
            var sh = psEmbers.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f;
            var rend = goEmbers.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch; rend.lengthScale = 3f;
            rend.material = CreateParticleMat(particles, new Color(1f, 0.7f, 0.1f, 1f), 2, "MatFuegoCriticoEmberV3");
        }

        SavePrefab(root, "Assets/Prefabs/VFX/AtaqueFuego_Critico_V3_VFX.prefab");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Utilidades
    // ════════════════════════════════════════════════════════════════════════════

    /// <param name="blendMode">0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply</param>
    static Material CreateParticleMat(Shader shader, Color color, int blendMode, string assetName)
    {
        string path = $"Assets/Prefabs/VFX/{assetName}.mat";
        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Surface", 1);      // Transparent
        mat.SetFloat("_Blend", blendMode);
        mat.renderQueue = 3000;
        mat.enableInstancing = true;

        // Si ya existe, sobrescribir
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);
        Debug.Log($"[VFX] Prefab guardado: {path}");
    }
}
