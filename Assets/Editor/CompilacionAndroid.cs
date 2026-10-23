using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>APK de prueba reproducible, desde menú, solicitud local o modo batch.</summary>
[InitializeOnLoad]
public static class CompilacionAndroid
{
    private const string Solicitud = "Temp/Android/solicitud.txt";
    private const string Resultado = "Temp/Android/resultado.txt";
    private static bool ocupado;
    private static double siguienteConsulta;

    static CompilacionAndroid() { EditorApplication.update += RevisarSolicitud; }

    private static void RevisarSolicitud()
    {
        if (ocupado || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < siguienteConsulta) return;
        siguienteConsulta = EditorApplication.timeSinceStartup + 2;
        if (!File.Exists(Solicitud)) return;
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                File.Delete(Solicitud);
                File.WriteAllText(Resultado, "ERROR: No se pudo activar el destino Android.");
            }
            return; // Esperar la importación y recompilación del nuevo destino.
        }
        File.Delete(Solicitud);
        Compilar();
    }

    [MenuItem("Pokimun/Android/Generar APK de prueba")]
    public static void Solicitar()
    {
        Directory.CreateDirectory("Temp/Android");
        File.WriteAllText(Resultado, "PENDIENTE: esperando a que Unity procese la solicitud.");
        File.WriteAllText(Solicitud, "apk");
        Debug.Log("[Android] APK solicitado. Se activará Android y el resultado quedará en Builds/Android.");
    }

    // Unity -batchmode -projectPath ... -buildTarget Android -executeMethod CompilacionAndroid.CompilarBatch -quit
    public static void CompilarBatch()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException("Iniciar Unity con -buildTarget Android.");
        Compilar(true);
    }

    private static void Compilar(bool propagarError = false)
    {
        ocupado = true;
        Directory.CreateDirectory("Temp/Android");
        File.WriteAllText(Resultado, "EN CURSO: generando APK Android.");
        try
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Falta el módulo Android Build Support de Unity.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new BuildFailedException("Guardar las escenas abiertas antes de generar el APK.");
            var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (escenas.Length == 0) throw new BuildFailedException("No hay escenas habilitadas en Build Settings.");
            ComprobarBiblioteca();
            GenerarIndiceModelo();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.pokimun.demo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.forceInternetPermission = true;
            EditorUserBuildSettings.buildAppBundle = false;
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds/Android");
            var informe = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = escenas, target = BuildTarget.Android,
                locationPathName = "Builds/Android/Pokimun-prueba.apk",
                options = BuildOptions.Development
            });
            if (informe.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("APK: " + informe.summary.result + "; errores=" + informe.summary.totalErrors);
            File.WriteAllText(Resultado, "OK: " + Path.GetFullPath("Builds/Android/Pokimun-prueba.apk") +
                "\nDuración: " + informe.summary.totalTime + "\nTamaño APK (bytes): " + new FileInfo("Builds/Android/Pokimun-prueba.apk").Length);
            Debug.Log("[Android] APK generado: Builds/Android/Pokimun-prueba.apk");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Resultado, "ERROR: " + ex);
            Debug.LogException(ex);
            if (propagarError) throw;
        }
        finally { ocupado = false; }
    }

    private static void ComprobarBiblioteca()
    {
        const string ruta = "Assets/Plugins/Android/arm64-v8a/libvosk.so";
        var importador = AssetImporter.GetAtPath(ruta) as PluginImporter;
        if (importador == null || !importador.GetCompatibleWithPlatform(BuildTarget.Android) ||
            importador.GetCompatibleWithEditor() || importador.GetCompatibleWithAnyPlatform() ||
            importador.GetPlatformData(BuildTarget.Android, "CPU") != "ARM64")
            throw new BuildFailedException("Vosk Android debe estar habilitado solo para Android ARM64.");
    }

    public static void GenerarIndiceModelo()
    {
        const string carpeta = "Assets/StreamingAssets/vosk-model-small-es";
        if (!File.Exists(Path.Combine(carpeta, "am/final.mdl")))
            throw new BuildFailedException("Falta el modelo español de Vosk.");
        var archivos = Directory.GetFiles(carpeta, "*", SearchOption.AllDirectories)
            .Where(p => !p.EndsWith(".meta") && Path.GetFileName(p) != "archivos.json")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => {
                using (var sha = SHA256.Create())
                using (var datos = File.OpenRead(p))
                    return new Archivo {
                        ruta = p.Substring(carpeta.Length + 1).Replace('\\', '/'), bytes = new FileInfo(p).Length,
                        sha256 = BitConverter.ToString(sha.ComputeHash(datos)).Replace("-", "").ToLowerInvariant()
                    };
            }).ToArray();
        File.WriteAllText(Path.Combine(carpeta, "archivos.json"), JsonUtility.ToJson(new Indice { archivos = archivos }, true));
        AssetDatabase.Refresh();
    }

    [Serializable] private class Indice { public Archivo[] archivos; }
    [Serializable] private class Archivo { public string ruta; public long bytes; public string sha256; }
}
