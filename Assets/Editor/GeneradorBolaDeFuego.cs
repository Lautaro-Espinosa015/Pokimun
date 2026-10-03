using UnityEngine;
using UnityEditor;

public class GeneradorBolaDeFuego
{
    [MenuItem("Pokimun/Magia/Generar Efecto Bola de Fuego")]
    public static void GenerarPrefabFuego()
    {
        // 1. Crear el GameObject principal
        GameObject efectoFuego = new GameObject("AtaqueFuego_VFX");

        // 2. Agregar script de movimiento (reutilizamos el mismo de agua)
        AtaqueVisualAgua scriptMovimiento = efectoFuego.AddComponent<AtaqueVisualAgua>();
        scriptMovimiento.velocidad = 18f;
        scriptMovimiento.tiempoDeVida = 2.5f;

        // 3. ===== SISTEMA DE PARTÍCULAS - NÚCLEO DE FUEGO =====
        ParticleSystem psNucleo = efectoFuego.AddComponent<ParticleSystem>();

        var mainNucleo = psNucleo.main;
        mainNucleo.duration = 2f;
        mainNucleo.startLifetime = 0.5f;
        mainNucleo.startSpeed = 0f;
        mainNucleo.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        // Gradiente Naranja -> Rojo -> Negro
        var gradiente = new Gradient();
        gradiente.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.1f), 0f),   // Amarillo brillante
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.4f),    // Naranja
                new GradientColorKey(new Color(0.5f, 0.05f, 0f), 0.8f), // Rojo oscuro
                new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1f)   // Humo negro
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        mainNucleo.startColor = new ParticleSystem.MinMaxGradient(gradiente);
        mainNucleo.simulationSpace = ParticleSystemSimulationSpace.World;
        mainNucleo.loop = true;

        var emisionNucleo = psNucleo.emission;
        emisionNucleo.rateOverTime = 80f;

        var shapNucleo = psNucleo.shape;
        shapNucleo.shapeType = ParticleSystemShapeType.Sphere;
        shapNucleo.radius = 0.3f;

        // Hacer que las partículas escalen hacia arriba (efecto llamarada)
        var sizOverLife = psNucleo.sizeOverLifetime;
        sizOverLife.enabled = true;
        AnimationCurve curva = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0.1f)
        );
        sizOverLife.size = new ParticleSystem.MinMaxCurve(1f, curva);

        // 4. ===== CHISPAS =====
        GameObject chispas = new GameObject("Chispas");
        chispas.transform.SetParent(efectoFuego.transform);
        chispas.transform.localPosition = Vector3.zero;
        ParticleSystem psChispas = chispas.AddComponent<ParticleSystem>();

        var mainChispas = psChispas.main;
        mainChispas.startLifetime = 0.8f;
        mainChispas.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        mainChispas.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        mainChispas.startColor = new Color(1f, 0.7f, 0.1f, 1f);
        mainChispas.gravityModifier = 0.3f;
        mainChispas.simulationSpace = ParticleSystemSimulationSpace.World;

        var emisionChispas = psChispas.emission;
        emisionChispas.rateOverTime = 30f;

        var shapeChispas = psChispas.shape;
        shapeChispas.shapeType = ParticleSystemShapeType.Sphere;
        shapeChispas.radius = 0.2f;

        // 5. Asignar Material URP compatible
        if (!System.IO.Directory.Exists("Assets/Prefabs/VFX"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs/VFX");

        Shader shaderURP = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shaderURP != null)
        {
            // Material del núcleo (naranja brillante)
            Material matFuego = new Material(shaderURP);
            matFuego.SetColor("_BaseColor", new Color(1f, 0.4f, 0.05f, 1f));
            AssetDatabase.CreateAsset(matFuego, "Assets/Prefabs/VFX/MaterialFuego.mat");

            ParticleSystemRenderer rendNucleo = efectoFuego.GetComponent<ParticleSystemRenderer>();
            rendNucleo.renderMode = ParticleSystemRenderMode.Billboard;
            rendNucleo.material = matFuego;

            // Material de chispas (amarillo)
            Material matChispa = new Material(shaderURP);
            matChispa.SetColor("_BaseColor", new Color(1f, 0.85f, 0.1f, 1f));
            AssetDatabase.CreateAsset(matChispa, "Assets/Prefabs/VFX/MaterialChispa.mat");

            ParticleSystemRenderer rendChispas = chispas.GetComponent<ParticleSystemRenderer>();
            rendChispas.renderMode = ParticleSystemRenderMode.Stretch;
            rendChispas.lengthScale = 2f;
            rendChispas.material = matChispa;
        }

        // 6. Guardar como Prefab
        string prefabPath = "Assets/Prefabs/VFX/AtaqueFuego_VFX.prefab";
        PrefabUtility.SaveAsPrefabAsset(efectoFuego, prefabPath);
        GameObject.DestroyImmediate(efectoFuego);

        Debug.Log($"[IA] ¡Fuego listo! Bola de fuego generada en: {prefabPath}");
        AssetDatabase.Refresh();
    }
}
