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
        main.startLifetime = 0.4f; // Partículas mueren rápido para dejar rastro
        main.startSpeed = 30f;
        main.startSize = 0.4f;
        main.startColor = new Color(0.1f, 0.7f, 1f, 1f); // Azul agua/Cian
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Para que deje estela
        
        // Configuración de Emisión
        var emission = ps.emission;
        emission.rateOverTime = 150f; // Muchas partículas juntas

        // Configuración de Forma (Para que salga como un láser/chorro)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 2f; // Casi recto
        shape.radius = 0.1f; // Salida concentrada

        // Configuración del Renderer (Para que parezca agua estirada a velocidad)
        ParticleSystemRenderer renderer = efectoAgua.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; // Estirar
        renderer.lengthScale = 3f; // Hacerlas más largas
        renderer.velocityScale = 0.1f;
        
        // 4. Asegurarnos de que exista la carpeta para guardar el material
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
        }
        if (!System.IO.Directory.Exists("Assets/Prefabs/VFX"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/VFX");
        }

        // Crear Material Especial URP para el agua y guardarlo
        Shader shaderURP = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shaderURP != null)
        {
            Material matAgua = new Material(shaderURP);
            // Configurar el material para que sea brillante y azul
            matAgua.SetColor("_BaseColor", new Color(0.1f, 0.8f, 1f, 2f)); 
            
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
