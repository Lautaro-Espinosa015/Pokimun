using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aplica un brillo de silueta (inverted-hull glow) sobre el personaje al curar o defender.
/// Funciona duplicando los SkinnedMeshRenderers en runtime para garantizar que URP los dibuje.
/// Colocar en el mismo GameObject raíz que ControladorAnimaciones.
/// </summary>
public class EfectoVisualPersonaje : MonoBehaviour
{
    [Header("Duraciones")]
    public float duracionCuracion = 2.5f;
    public float duracionDefensa  = 5.5f;

    [Header("Colores del Brillo")]
    public Color colorCuracion = new Color(0.15f, 1f, 0.4f, 0.85f);
    public Color colorDefensa  = new Color(0.5f, 0.2f, 1f, 0.85f);

    [Header("Grosor del borde")]
    [Range(0.01f, 0.12f)]
    public float grosoBorde = 0.04f;

    private Coroutine corrutinaCuracion;
    private Coroutine corrutinaDefensa;
    private Material matCuracion;
    private Material matDefensa;
    private bool defensaActiva;

    // Clones de los mesh renderers que usarán el material de glow
    private List<SkinnedMeshRenderer> glowRenderersCuracion = new List<SkinnedMeshRenderer>();
    private List<SkinnedMeshRenderer> glowRenderersDefensa = new List<SkinnedMeshRenderer>();

    private static readonly int PropGlowColor   = Shader.PropertyToID("_GlowColor");
    private static readonly int PropIntensidad  = Shader.PropertyToID("_Intensity");
    private static readonly int PropOutlineWidth= Shader.PropertyToID("_OutlineWidth");
    private static readonly int PropUsePattern  = Shader.PropertyToID("_UsePattern");

    private void Awake()
    {
        Shader glowShader = Shader.Find("Pokimun/CharacterGlow");
        if (glowShader == null)
        {
            Debug.LogError($"[EfectoVisual] Shader 'Pokimun/CharacterGlow' no encontrado en '{gameObject.name}'.");
            return;
        }

        // Crear los materiales de glow
        matCuracion = new Material(glowShader);
        matCuracion.name = $"GlowCuracion_{gameObject.name}";
        matCuracion.SetColor(PropGlowColor, colorCuracion);
        matCuracion.SetFloat(PropOutlineWidth, grosoBorde);
        matCuracion.SetFloat(PropUsePattern, 0f);
        matCuracion.SetFloat(PropIntensidad, 0f);

        matDefensa = new Material(glowShader);
        matDefensa.name = $"GlowDefensa_{gameObject.name}";
        matDefensa.SetColor(PropGlowColor, colorDefensa);
        matDefensa.SetFloat(PropOutlineWidth, grosoBorde * 1.2f);
        matDefensa.SetFloat(PropUsePattern, 1f);
        matDefensa.SetFloat(PropIntensidad, 0f);

        // Crear jerarquías clonadas independientes para que no se pisen
        CrearClonesGlow(matCuracion, glowRenderersCuracion, "Curacion");
        CrearClonesGlow(matDefensa, glowRenderersDefensa, "Defensa");
    }

    private void CrearClonesGlow(Material mat, List<SkinnedMeshRenderer> listaDestino, string sufijo)
    {
        var originalRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Transform glowRoot = new GameObject($"GlowRenderers_{sufijo}").transform;
        glowRoot.SetParent(transform);
        glowRoot.localPosition = Vector3.zero;
        glowRoot.localRotation = Quaternion.identity;
        glowRoot.localScale = Vector3.one;

        foreach (var original in originalRenderers)
        {
            if (original.gameObject.name.Contains("_GlowClone")) continue;

            GameObject cloneGo = new GameObject($"{original.gameObject.name}_GlowClone_{sufijo}");
            cloneGo.transform.SetParent(glowRoot);
            cloneGo.transform.localPosition = Vector3.zero;
            cloneGo.transform.localRotation = Quaternion.identity;
            cloneGo.transform.localScale = Vector3.one;

            var cloneSMR = cloneGo.AddComponent<SkinnedMeshRenderer>();
            cloneSMR.sharedMesh = original.sharedMesh;
            cloneSMR.bones = original.bones;
            cloneSMR.rootBone = original.rootBone;
            cloneSMR.updateWhenOffscreen = true;

            // Asignar el material permanentemente a estos clones
            Material[] mats = new Material[original.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            cloneSMR.sharedMaterials = mats;

            // Inicialmente apagado
            cloneSMR.enabled = false;
            listaDestino.Add(cloneSMR);
        }
    }

    private void OnDestroy()
    {
        if (matCuracion != null) Destroy(matCuracion);
        if (matDefensa  != null) Destroy(matDefensa);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void MostrarCuracion()
    {
        if (matCuracion == null) return;
        Debug.Log($"[EfectoVisual] MostrarCuracion → '{gameObject.name}'");
        
        SetClonesState(glowRenderersCuracion, true);
        
        if (corrutinaCuracion != null) StopCoroutine(corrutinaCuracion);
        corrutinaCuracion = StartCoroutine(FadeGlow(matCuracion, 0f, 1f, 0.35f,
            () => StartCoroutine(FadeGlow(matCuracion, 1f, 0f, 0.7f,
            () => SetClonesState(glowRenderersCuracion, false),
            duracionCuracion - 0.7f))));
    }

    public void MostrarDefensa()
    {
        if (matDefensa == null) return;
        Debug.Log($"[EfectoVisual] MostrarDefensa → '{gameObject.name}'");
        defensaActiva = true;
        
        SetClonesState(glowRenderersDefensa, true);
        
        if (corrutinaDefensa != null) StopCoroutine(corrutinaDefensa);
        corrutinaDefensa = StartCoroutine(FadeGlow(matDefensa, 0f, 1f, 0.35f, null));
    }

    public void QuitarDefensa()
    {
        if (!defensaActiva) return;
        defensaActiva = false;
        if (corrutinaDefensa != null) { StopCoroutine(corrutinaDefensa); corrutinaDefensa = null; }
        corrutinaDefensa = StartCoroutine(FadeGlow(matDefensa, matDefensa.GetFloat(PropIntensidad), 0f, 0.7f,
            () => SetClonesState(glowRenderersDefensa, false)));
        Debug.Log($"[EfectoVisual] Escudo quitado → '{gameObject.name}'");
    }

    public void QuitarTodosLosEfectos()
    {
        if (corrutinaCuracion != null) { StopCoroutine(corrutinaCuracion); corrutinaCuracion = null; }
        if (corrutinaDefensa  != null) { StopCoroutine(corrutinaDefensa);  corrutinaDefensa  = null; }
        defensaActiva = false;
        if (matCuracion != null) matCuracion.SetFloat(PropIntensidad, 0f);
        if (matDefensa  != null) matDefensa.SetFloat(PropIntensidad, 0f);
        SetClonesState(glowRenderersCuracion, false);
        SetClonesState(glowRenderersDefensa, false);
    }

    // ── Lógica Interna ────────────────────────────────────────────────────────

    private void SetClonesState(List<SkinnedMeshRenderer> lista, bool state)
    {
        foreach (var smr in lista)
        {
            if (smr != null) smr.enabled = state;
        }
    }

    private IEnumerator FadeGlow(Material mat, float desde, float hasta, float segundos,
        System.Action callback, float espera = 0f)
    {
        if (espera > 0f) yield return new WaitForSeconds(espera);
        float t = 0f;
        while (t < segundos)
        {
            t += Time.deltaTime;
            if (mat != null) mat.SetFloat(PropIntensidad, Mathf.Lerp(desde, hasta, t / segundos));
            yield return null;
        }
        if (mat != null) mat.SetFloat(PropIntensidad, hasta);
        callback?.Invoke();
    }
}
