using UnityEngine;
using UnityEditor;

public class GeneradorAtaquesNuevos
{
    [MenuItem("Pokimun/Magia/Generar Nuevos Ataques (Agua y Fuego)")]
    public static void GenerarTodo()
    {
        if (!System.IO.Directory.Exists("Assets/Prefabs/VFX"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs/VFX");

        Shader shaderURP = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shaderURP == null)
        {
            Debug.LogError("No se encontró el shader URP.");
            return;
        }

        // --- 1. Ataque Agua Basico V2 ---
        GenerarAguaBasicoV2(shaderURP);
        
        // --- 2. Ataque Agua Crítico ---
        GenerarAguaCritico(shaderURP);

        // --- 3. Ataque Fuego Basico V2 ---
        GenerarFuegoBasicoV2(shaderURP);

        // --- 4. Ataque Fuego Crítico ---
        GenerarFuegoCritico(shaderURP);

        AssetDatabase.Refresh();
        Debug.Log("[IA] ¡Nuevos efectos generados en Assets/Prefabs/VFX!");
    }

    private static void GenerarAguaBasicoV2(Shader shaderURP)
    {
        GameObject efecto = new GameObject("AtaqueAgua_Basico_V2_VFX");
        AtaqueVisualAgua script = efecto.AddComponent<AtaqueVisualAgua>();
        script.velocidad = 25f;
        script.tiempoDeVida = 2f;

        ParticleSystem ps = efecto.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startColor = new Color(0.1f, 0.7f, 1f, 0.6f); // Más translúcido
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 250f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 2f;
        shape.radius = 0.2f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.5f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0f, 0.2f, 0.9f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)
        ));

        ParticleSystemRenderer renderer = efecto.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 5f;
        renderer.velocityScale = 0.05f;

        Material mat = new Material(shaderURP);
        mat.SetColor("_BaseColor", new Color(0.2f, 0.8f, 1f, 0.4f)); 
        mat.SetFloat("_Surface", 1); 
        mat.SetFloat("_Blend", 0); // Alpha blending para agua real
        mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, "Assets/Prefabs/VFX/MaterialAguaV2.mat");
        renderer.material = mat;

        PrefabUtility.SaveAsPrefabAsset(efecto, "Assets/Prefabs/VFX/AtaqueAgua_Basico_V2_VFX.prefab");
        GameObject.DestroyImmediate(efecto);
    }

    private static void GenerarAguaCritico(Shader shaderURP)
    {
        GameObject efecto = new GameObject("AtaqueAgua_Critico_VFX");
        AtaqueVisualAgua script = efecto.AddComponent<AtaqueVisualAgua>();
        script.velocidad = 35f;
        script.tiempoDeVida = 2.5f;

        ParticleSystem ps = efecto.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 40f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.3f, 0.9f, 1f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 200, 300) }); // Explosión masiva

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.6f, 1f, 1f), 0f), new GradientColorKey(new Color(0f, 0.4f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.2f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.1f), new Keyframe(0.2f, 1f), new Keyframe(1f, 1.5f)
        ));

        ParticleSystemRenderer renderer = efecto.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        Material mat = new Material(shaderURP);
        mat.SetColor("_BaseColor", new Color(0.3f, 0.9f, 1f, 0.3f)); 
        mat.SetFloat("_Surface", 1); 
        mat.SetFloat("_Blend", 0); 
        mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, "Assets/Prefabs/VFX/MaterialAguaCritico.mat");
        renderer.material = mat;

        PrefabUtility.SaveAsPrefabAsset(efecto, "Assets/Prefabs/VFX/AtaqueAgua_Critico_VFX.prefab");
        GameObject.DestroyImmediate(efecto);
    }

    private static void GenerarFuegoBasicoV2(Shader shaderURP)
    {
        GameObject efecto = new GameObject("AtaqueFuego_Basico_V2_VFX");
        AtaqueVisualAgua script = efecto.AddComponent<AtaqueVisualAgua>();
        script.velocidad = 18f;
        script.tiempoDeVida = 2.5f;

        ParticleSystem ps = efecto.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.startLifetime = 0.5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 60f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.1f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.4f),
                new GradientColorKey(new Color(0.5f, 0.05f, 0f), 0.8f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f), // Más translúcido
                new GradientAlphaKey(0.4f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = grad;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.1f)
        ));

        ParticleSystemRenderer renderer = efecto.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Material mat = new Material(shaderURP);
        mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0.05f, 0.5f)); 
        mat.SetFloat("_Surface", 1); 
        mat.SetFloat("_Blend", 2); 
        mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, "Assets/Prefabs/VFX/MaterialFuegoV2.mat");
        renderer.material = mat;

        PrefabUtility.SaveAsPrefabAsset(efecto, "Assets/Prefabs/VFX/AtaqueFuego_Basico_V2_VFX.prefab");
        GameObject.DestroyImmediate(efecto);
    }

    private static void GenerarFuegoCritico(Shader shaderURP)
    {
        GameObject efecto = new GameObject("AtaqueFuego_Critico_VFX");
        AtaqueVisualAgua script = efecto.AddComponent<AtaqueVisualAgua>();
        script.velocidad = 12f; // Lento pero imparable
        script.tiempoDeVida = 4f;

        ParticleSystem ps = efecto.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.startLifetime = 0.8f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 3.5f); // Tamaño gigante
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 120f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.2f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(new Color(1f, 0.6f, 0f), 0.2f),
                new GradientColorKey(new Color(1f, 0.1f, 0f), 0.6f),
                new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = grad;

        ParticleSystemRenderer renderer = efecto.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Material mat = new Material(shaderURP);
        mat.SetColor("_BaseColor", new Color(1f, 0.5f, 0.1f, 1f)); 
        mat.SetFloat("_Surface", 1); 
        mat.SetFloat("_Blend", 2); 
        mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, "Assets/Prefabs/VFX/MaterialFuegoCritico.mat");
        renderer.material = mat;

        PrefabUtility.SaveAsPrefabAsset(efecto, "Assets/Prefabs/VFX/AtaqueFuego_Critico_VFX.prefab");
        GameObject.DestroyImmediate(efecto);
    }
}
