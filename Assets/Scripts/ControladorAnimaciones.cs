using UnityEngine;
using System.Collections; // Necesario para las Corrutinas
using System.Collections.Generic;

public class ControladorAnimaciones : MonoBehaviour
{
    [Header("Componentes")]
    [Tooltip("El Animator de este personaje (se auto-asigna si está vacío)")]
    public Animator animator;

    [Header("Efectos Visuales (VFX)")]
    [Tooltip("El prefab del rayo normal (agua o fuego)")]
    public GameObject prefabAtaqueAgua;
    
    [Tooltip("El prefab del ataque crítico (explosión o bola gigante)")]
    public GameObject prefabAtaqueCritico;
    
    [Tooltip("Si disparas con una sola mano, ponla aquí.")]
    public Transform puntoDeDisparo;
    
    [Tooltip("Si disparas tipo Kamehameha, pon la mano Izquierda aquí")]
    public Transform manoIzquierda;
    [Tooltip("Si disparas tipo Kamehameha, pon la mano Derecha aquí")]
    public Transform manoDerecha;

    [Header("Ajustes de Disparo")]
    [Tooltip("Tiempo que tarda en salir el agua en el ataque básico")]
    public float retrasoAtaqueNormal = 1.0f; // Medio segundo antes (1.0s)
    [Tooltip("Tiempo que tarda en salir el agua en el ataque crítico")]
    public float retrasoAtaqueCritico = 1.5f; // Retrasado a 1.5s
    [Tooltip("Mueve el disparo X metros hacia adelante para que no salga desde dentro del cuerpo")]
    public float desplazamientoAdelante = 1.0f;

    [Header("Sonidos de Ataque")]
    [Tooltip("Audio que suena al lanzar el ataque básico")]
    public AudioClip audioAtaqueNormal;
    [Tooltip("Retraso del audio del ataque básico (puede ser menor al del VFX)")]
    public float retrasoAudioNormal = 0.3f;
    [Tooltip("Velocidad/Tono del audio básico")]
    [Range(0.5f, 2f)] public float pitchAudioNormal = 1f;

    [Tooltip("Audio que suena al lanzar el ataque crítico")]
    public AudioClip audioAtaqueCritico;
    [Tooltip("Retraso del audio del ataque crítico")]
    public float retrasoAudioCritico = 0.8f;
    [Tooltip("Velocidad/Tono del audio crítico")]
    [Range(0.5f, 2f)] public float pitchAudioCritico = 1f;
    
    [Tooltip("Tiempo en segundos para destruir el ataque y liberar memoria")]
    public float tiempoVidaVFX = 10.0f;

    private readonly HashSet<string> avisosParametrosAusentes = new HashSet<string>();
    private readonly List<GameObject> efectosActivos = new List<GameObject>();

    private void Start()
    {
        AsegurarAnimator();
        
        bool esHydros = gameObject.name.IndexOf("hydros", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                        (prefabAtaqueAgua != null && prefabAtaqueAgua.name.IndexOf("agua", System.StringComparison.OrdinalIgnoreCase) >= 0);
        bool esIgnis = gameObject.name.IndexOf("ignis", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                       (prefabAtaqueAgua != null && prefabAtaqueAgua.name.IndexOf("fuego", System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (esHydros)
        {
            retrasoAtaqueNormal = 1.0f;
            retrasoAtaqueCritico = 1.5f;
            retrasoAudioNormal = 0.3f; // 0.7 segundos ANTES del visual (1.0 - 0.7 = 0.3)
            retrasoAudioCritico = 0.8f; // 0.7 segundos ANTES del visual (1.5 - 0.7 = 0.8)
        }
        else if (esIgnis)
        {
            // Para Ignis: el básico 1 seg antes, el crítico 0.6 segs antes
            retrasoAudioNormal = Mathf.Max(0f, retrasoAtaqueNormal - 1.0f);
            retrasoAudioCritico = Mathf.Max(0f, retrasoAtaqueCritico - 0.6f);
        }
    }

    // --- MÉTODOS PARA LLAMAR DESDE GESTORNIVEL O BOTONES DE PRUEBA ---

    public void EjecutarAtaque(JugadaRPS tipoAtaque)
    {
        PrepararAccion();
        IntentarActivarTrigger("Atacar");
        
        if (prefabAtaqueAgua != null)
        {
            StartCoroutine(ManejarAtaqueVFX(retrasoAtaqueNormal, prefabAtaqueAgua, retrasoAudioNormal, audioAtaqueNormal, pitchAudioNormal)); 
        }
    }

    public void EjecutarAtaqueCritico(JugadaRPS tipoAtaque)
    {
        PrepararAccion();
        IntentarActivarTrigger("AtacarCritico");

        GameObject prefabAEmitir = prefabAtaqueCritico != null ? prefabAtaqueCritico : prefabAtaqueAgua;
        if (prefabAEmitir != null)
        {
            StartCoroutine(ManejarAtaqueVFX(retrasoAtaqueCritico, prefabAEmitir, retrasoAudioCritico, audioAtaqueCritico, pitchAudioCritico));
        }
    }

    private IEnumerator ManejarAtaqueVFX(float retrasoVfx, GameObject prefab, float retrasoAudio, AudioClip clip, float pitch)
    {
        GameObject vfxInstancia = null;
        GameObject audioObj = null;

        Vector3 posBase = transform.position + Vector3.up * 1f; 
        if (manoIzquierda != null && manoDerecha != null) posBase = (manoIzquierda.position + manoDerecha.position) / 2f;
        else if (puntoDeDisparo != null) posBase = puntoDeDisparo.position;

        Transform emisorInicial = puntoDeDisparo != null ? puntoDeDisparo : transform;

        float tiempo = 0;
        bool vfxSpawned = false;
        bool audioSpawned = clip == null;

        while (!vfxSpawned || !audioSpawned)
        {
            if (!vfxSpawned && tiempo >= retrasoVfx)
            {
                Vector3 posDisparo = posBase + transform.forward * desplazamientoAdelante;
                vfxInstancia = Instantiate(prefab, posDisparo, transform.rotation);
                efectosActivos.Add(vfxInstancia);
                Destroy(vfxInstancia, tiempoVidaVFX);
                vfxSpawned = true;

                if (audioObj != null)
                {
                    audioObj.transform.SetParent(vfxInstancia.transform);
                    audioObj.transform.localPosition = Vector3.zero;
                }
            }

            if (!audioSpawned && tiempo >= retrasoAudio)
            {
                audioObj = new GameObject("AudioAtaque_" + clip.name);
                if (vfxInstancia != null) audioObj.transform.SetParent(vfxInstancia.transform);
                else audioObj.transform.SetParent(emisorInicial);
                
                audioObj.transform.localPosition = Vector3.zero;
                AudioSource src = audioObj.AddComponent<AudioSource>();
                src.clip = clip;
                src.spatialBlend = 1f; // Sonido 3D
                src.pitch = pitch;
                src.volume = PlayerPrefs.GetFloat("VolumenEfectos", 0.85f);
                src.Play();
                Destroy(audioObj, (clip.length / Mathf.Max(0.1f, pitch)) + 3.0f); // 3s extra de cola para ecos
                audioSpawned = true;
            }

            tiempo += Time.deltaTime;
            yield return null;
        }
    }

    public void EjecutarDefensa(bool estaDefendiendo)
    {
        if (estaDefendiendo) PrepararAccion();
        if (TieneParametro("Defendiendo", AnimatorControllerParameterType.Bool))
        {
            animator.SetBool("Defendiendo", estaDefendiendo);
        }
        else if (TieneParametro("Defendiendo", AnimatorControllerParameterType.Trigger))
        {
            if (estaDefendiendo) animator.SetTrigger("Defendiendo");
            else animator.ResetTrigger("Defendiendo");
        }
        else
        {
            AdvertirParametroAusente("Defendiendo", "Bool o Trigger");
        }

    }

    private void PrepararAccion()
    {
        FinalizarAccion();
        if (AsegurarAnimator() && animator.HasState(0, Animator.StringToHash("Base Layer.Reposo")))
            animator.Play("Base Layer.Reposo", 0, 0f);
    }

    public void FinalizarAccion()
    {
        StopAllCoroutines();
        foreach (GameObject efecto in efectosActivos) if (efecto != null) Destroy(efecto);
        efectosActivos.Clear();
        if (!AsegurarAnimator()) return;
        if (TieneParametro("Defendiendo", AnimatorControllerParameterType.Bool)) animator.SetBool("Defendiendo", false);
        foreach (string trigger in new[] { "Atacar", "AtacarCritico", "Defendiendo" })
            if (TieneParametro(trigger, AnimatorControllerParameterType.Trigger)) animator.ResetTrigger(trigger);
        if (animator.isActiveAndEnabled && animator.HasState(0, Animator.StringToHash("Base Layer.Reposo")))
            animator.CrossFade("Base Layer.Reposo", .12f, 0);
    }

    private void OnDisable() { FinalizarAccion(); }

    public void RecibirDano()
    {
        // La animación de daño es opcional y no existe en los controllers actuales.
        if (TieneParametro("RecibirDano", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("RecibirDano");
        }
    }

    public void CelebrarVictoria()
    {
        if (TieneParametro("Victoria", AnimatorControllerParameterType.Trigger)) animator.SetTrigger("Victoria");
    }

    public void CaerDerrotado()
    {
        // Animación de muerte/desmayo
        if (TieneParametro("Derrota", AnimatorControllerParameterType.Trigger)) animator.SetTrigger("Derrota");
    }

    private bool AsegurarAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        return animator != null;
    }

    private bool TieneParametro(string nombre, AnimatorControllerParameterType tipo)
    {
        if (!AsegurarAnimator()) return false;

        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nombre && parametro.type == tipo)
            {
                return true;
            }
        }

        return false;
    }

    private void IntentarActivarTrigger(string nombre)
    {
        if (TieneParametro(nombre, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(nombre);
        }
        else
        {
            AdvertirParametroAusente(nombre, "Trigger");
        }
    }

    private void AdvertirParametroAusente(string nombre, string tipo)
    {
        if (animator == null) return;

        string clave = nombre + ":" + tipo;
        if (avisosParametrosAusentes.Add(clave))
        {
            Debug.LogWarning(
                $"[ControladorAnimaciones] El Animator de '{gameObject.name}' no tiene el parámetro {tipo} '{nombre}'. Se omite esa animación.",
                this);
        }
    }
}
