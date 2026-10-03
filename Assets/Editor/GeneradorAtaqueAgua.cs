using UnityEngine;
using UnityEditor;

public class GeneradorAtaqueAgua
{
    [MenuItem("Pokimun/Magia/Generar Efecto Rayo de Agua")]
    public static void GenerarPrefabAgua()
    {
        // 1. Crear el GameObject principal
        GameObject efectoAgua = new GameObject("AtaqueAgua_VFX");
        
        // 2. Agregar el script de movimiento (el que creamos antes)
        AtaqueVisualAgua scriptMovimiento = efectoAgua.AddComponent<AtaqueVisualAgua>();
        scriptMovimiento.velocidad = 25f; // Lo hacemos más rápido
        scriptMovimiento.tiempoDeVida = 2f;

        // 3. Agregar el Sistema de Partículas
        ParticleSystem ps = efectoAgua.AddComponent<ParticleSystem>();
        
        // Configuración Principal (Módulo Main)
        var main = ps.main;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(25f, 35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new Color(0.1f, 0.8f, 1f, 1f); 
        main.simulationSpace = ParticleSystemSimulationSpace.World; 
        
        // Configuración de Emisión
        var emission = ps.emission;
        emission.rateOverTime = 200f; 

        // Configuración de Forma (Para que salga como un láser/chorro concentrado)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 1.5f; 
        shape.radius = 0.15f; 

        // Color a lo largo del tiempo (Transparencia y desvanecimiento)
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradAgua = new Gradient();
        gradAgua.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0f, 0.3f, 0.8f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = gradAgua;

        // Tamaño a lo largo del tiempo (se agranda un poco y luego se hace pequeño)
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)
        ));

        // Configuración del Renderer
        ParticleSystemRenderer renderer = efectoAgua.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; 
        renderer.lengthScale = 4f; 
        renderer.velocityScale = 0.05f;
        
        // 4. Asegurarnos de que exista la carpeta para guardar el material
        if (!System.IO.Directory.Exists("Assets/Prefabs/VFX"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/VFX");
        }

        // Crear Material Especial URP Translúcido para el agua
        Shader shaderURP = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shaderURP != null)
        {
            Material matAgua = new Material(shaderURP);
            matAgua.SetColor("_BaseColor", new Color(0.2f, 0.8f, 1f, 1.5f)); 
            
            // Configurar para que sea Translúcido / Additive
            matAgua.SetFloat("_Surface", 1); // 1 = Transparent
            matAgua.SetFloat("_Blend", 2);   // 2 = Additive (Brillante/Translúcido)
            matAgua.renderQueue = 3000;      // Cola de renderizado transparente
            
            string matPath = "Assets/Prefabs/VFX/MaterialAgua.mat";
            AssetDatabase.CreateAsset(matAgua, matPath);
            renderer.material = matAgua;
        }
        
        // 5. Guardar como Prefab listo para usar
        string prefabPath = "Assets/Prefabs/VFX/AtaqueAgua_VFX.prefab";
        PrefabUtility.SaveAsPrefabAsset(efectoAgua, prefabPath);
        
        // 6. Limpiar la escena (ya está guardado en el proyecto)
        GameObject.DestroyImmediate(efectoAgua);
        
        Debug.Log($"[IA] ¡Magia lista! Efecto de Agua generado con éxito en la carpeta: {prefabPath}");
        AssetDatabase.Refresh();
    }
}
