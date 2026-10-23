using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Mide el retorno a reposo en una escena de previsualización, sin alterar la escena abierta.</summary>
[InitializeOnLoad]
public static class DiagnosticoHydros
{
    private const string Carpeta = "Temp/DiagnosticoHydros";
    private const string Modelo = "Assets/ModelosDePokimun/Hydros/Ender_Hydros@Fight Idle.fbx";
    private const string Controlador = "Assets/ModelosDePokimun/Hydros/AnimadorPokimun.controller";
    private static double proximaConsulta;

    static DiagnosticoHydros() { EditorApplication.update += RevisarSolicitud; }

    private static void RevisarSolicitud()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.timeSinceStartup < proximaConsulta) return;
        proximaConsulta = EditorApplication.timeSinceStartup + 1;
        string solicitud = Path.Combine(Carpeta, "solicitud.txt");
        if (!File.Exists(solicitud)) return;
        AssetDatabase.Refresh();
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string nombre = File.ReadAllText(solicitud).Trim();
        File.Delete(solicitud);
        Medir(nombre == "antes" ? "antes" : "despues");
    }

    [MenuItem("Pokimun/Animaciones/Medir retorno de Hydros a reposo")]
    public static void MedirDesdeMenu() => Medir("despues");

    private static void Medir(string nombre)
    {
        Directory.CreateDirectory(Carpeta);
        var resumen = new StringBuilder();
        var trazas = new StringBuilder("accion,tiempo,rootYaw,bodyYaw,enTransicion\n");
        try
        {
            foreach (string estado in new[] { "Basico", "Critico", "Defensa" })
                MedirAccion(estado, resumen, trazas);
            File.WriteAllText(Path.Combine(Carpeta, nombre + ".txt"), resumen.ToString());
            File.WriteAllText(Path.Combine(Carpeta, nombre + ".csv"), trazas.ToString());
            Debug.Log("[Hydros] Diagnóstico guardado en " + Carpeta + "/" + nombre + ".txt");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(Carpeta, nombre + ".txt"), "ERROR: " + ex);
            Debug.LogException(ex);
        }
    }

    private static void MedirAccion(string estado, StringBuilder resumen, StringBuilder trazas)
    {
        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Modelo);
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo, escena);
            instancia.hideFlags = HideFlags.HideAndDontSave;
            instancia.SetActive(true);
            var animator = instancia.GetComponent<Animator>();
            animator.enabled = true;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controlador);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0);
            var acciones = instancia.AddComponent<ControladorAnimaciones>();
            acciones.animator = animator;
            if (estado == "Defensa") animator.SetBool("Defendiendo", true);
            animator.Play("Base Layer." + estado, 0, 0);
            animator.Update(0);
            float dt = 1f / 60;
            float duracion = estado == "Defensa" ? 1.5f : 4f;
            float yawInicio = animator.bodyRotation.eulerAngles.y;
            float yawPrevio = yawInicio;
            float saltoNatural = 0;
            float retornoNatural = 0;
            for (int i = 0; i < Mathf.RoundToInt(duracion / dt); i++)
            {
                bool volviendo = animator.IsInTransition(0) &&
                    animator.GetNextAnimatorStateInfo(0).IsName("Base Layer.Reposo");
                animator.Update(dt);
                float yaw = animator.bodyRotation.eulerAngles.y;
                if (volviendo)
                {
                    retornoNatural += dt;
                    saltoNatural = Mathf.Max(saltoNatural, Mathf.Abs(Mathf.DeltaAngle(yawPrevio, yaw)));
                }
                yawPrevio = yaw;
                trazas.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F4},{2:F4},{3:F4},{4}\n", estado, i * dt - duracion,
                    instancia.transform.eulerAngles.y, yaw, volviendo);
            }

            float yawAnterior = animator.bodyRotation.eulerAngles.y;
            float saltoMaximo = 0;
            float transicion = 0;
            resumen.AppendFormat("{0}: activo={1}, inicializado={2}, enReposoAntes={3}, tiempoEstado={4:F3}\n",
                estado, animator.isActiveAndEnabled, animator.isInitialized,
                animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Reposo"),
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            resumen.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "{0}: yawInicio={1:F3}°, yawAntesFinalizar={2:F3}°, retornoNatural={3:F3}s, saltoNaturalMaxPorFrame={4:F3}°\n",
                estado, yawInicio, yawAnterior, retornoNatural, saltoNatural);
            acciones.FinalizarAccion();
            for (int i = 0; i < 90; i++)
            {
                animator.Update(dt);
                float yaw = animator.bodyRotation.eulerAngles.y;
                saltoMaximo = Mathf.Max(saltoMaximo, Mathf.Abs(Mathf.DeltaAngle(yawAnterior, yaw)));
                yawAnterior = yaw;
                bool enTransicion = animator.IsInTransition(0);
                if (enTransicion) transicion += dt;
                trazas.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F4},{2:F4},{3:F4},{4}\n", estado, (i + 1) * dt,
                    instancia.transform.eulerAngles.y, yaw, enTransicion);
            }
            acciones.FinalizarAccion();
            animator.Update(dt);
            resumen.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "{0}: retorno={1:F3}s, saltoYawMaxPorFrame={2:F3}°, retornoRepetidoCreaTransicion={3}\n",
                estado, transicion, saltoMaximo, animator.IsInTransition(0));
        }
        finally { EditorSceneManager.ClosePreviewScene(escena); }
    }
}
