using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Validación explícita por menú o por solicitud local en Temp/ValidacionVoz/solicitud.txt.</summary>
[InitializeOnLoad]
public static class ValidacionVoz
{
    private const string Carpeta = "Temp/ValidacionVoz";
    private static TestRunnerApi api;
    private static double proximaConsulta;
    private static bool ocupado;

    static ValidacionVoz() { EditorApplication.update += RevisarSolicitud; }

    private static void RevisarSolicitud()
    {
        if (ocupado || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < proximaConsulta) return;
        proximaConsulta = EditorApplication.timeSinceStartup + 1;
        string solicitud = Path.Combine(Carpeta, "solicitud.txt");
        if (!File.Exists(solicitud)) return;
        // Importar cambios externos antes de ejecutar pruebas contra assemblies ya cargados.
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string accion = File.ReadAllText(solicitud).Trim();
        File.Delete(solicitud);
        if (accion == "pruebas") EjecutarPruebas();
        else if (accion == "linux") CompilarLinux();
        else if (accion == "windows") CompilarWindows();
        else if (accion == "android") CompilarAndroid();
        else if (accion == "plataformas")
        {
            CompilarWindows();
            CompilarLinux();
            CompilarAndroid();
        }
    }

    [MenuItem("Pokimun/Voz/Ejecutar pruebas de reconocimiento")]
    public static void EjecutarPruebas()
    {
        if (ocupado) return;
        Directory.CreateDirectory(Carpeta);
        ocupado = true;
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Resultados());
        api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode, assemblyNames = new[] { "Pokimun.Voz.Tests" }
        }));
    }

    [MenuItem("Pokimun/Voz/Comprobar scripts Linux x86_64")]
    public static void CompilarLinux() => Compilar(BuildTarget.StandaloneLinux64, "linux");

    [MenuItem("Pokimun/Voz/Comprobar scripts Windows x86_64")]
    public static void CompilarWindows() => Compilar(BuildTarget.StandaloneWindows64, "windows");

    [MenuItem("Pokimun/Voz/Comprobar scripts Android")]
    public static void CompilarAndroid() => Compilar(BuildTarget.Android, "android");

    private static void Compilar(BuildTarget destino, string nombre)
    {
        if (ocupado) return;
        ocupado = true;
        Directory.CreateDirectory(Carpeta);
        try
        {
            if (destino == BuildTarget.StandaloneLinux64) ComprobarRecursosLinux();
            var configuracion = new ScriptCompilationSettings {
                target = destino, group = destino == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone,
                options = ScriptCompilationOptions.None
            };
            var resultado = PlayerBuildInterface.CompilePlayerScripts(configuracion, Path.Combine(Carpeta, nombre));
            if (resultado.assemblies == null || resultado.assemblies.Count == 0)
                throw new InvalidOperationException("Unity no generó assemblies para " + nombre);
            File.WriteAllText(Path.Combine(Carpeta, nombre + ".txt"),
                "OK: scripts compilados para " + destino + "\n" +
                (destino == BuildTarget.StandaloneLinux64 ? "Recursos e importación Linux: OK\n" : "") +
                string.Join("\n", resultado.assemblies));
            Debug.Log("[Voz] Scripts compilados para " + destino);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(Carpeta, nombre + ".txt"), "ERROR: " + ex);
            Debug.LogException(ex);
        }
        finally { ocupado = false; }
    }

    private static void ComprobarRecursosLinux()
    {
        const string biblioteca = "Assets/Plugins/Linux/x86_64/libvosk.so";
        const string modelo = "Assets/StreamingAssets/vosk-model-small-es";
        foreach (string archivo in new[] { "am/final.mdl", "conf/model.conf", "conf/mfcc.conf", "graph/HCLr.fst", "graph/Gr.fst" })
            if (!File.Exists(Path.Combine(modelo, archivo))) throw new FileNotFoundException("Modelo incompleto: " + archivo);
        var importador = AssetImporter.GetAtPath(biblioteca) as PluginImporter;
        if (importador == null || !importador.GetCompatibleWithPlatform(BuildTarget.StandaloneLinux64) ||
            importador.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64) ||
            !importador.GetCompatibleWithEditor() || importador.GetEditorData("OS") != "Linux")
            throw new InvalidOperationException("La biblioteca Vosk debe importarse solo para Linux x86_64 y el editor Linux.");
    }

    private sealed class Resultados : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(Path.Combine(Carpeta, "pruebas.xml")));
            File.WriteAllText(Path.Combine(Carpeta, "pruebas.txt"),
                $"Resultado: {result.TestStatus}; correctas={result.PassCount}; fallidas={result.FailCount}; omitidas={result.SkipCount}");
            EditorApplication.delayCall += () => {
                if (api != null) UnityEngine.Object.DestroyImmediate(api);
                api = null;
                ocupado = false;
            };
        }
    }
}
