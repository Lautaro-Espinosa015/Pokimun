using UnityEngine;
using System.Collections; // Necesario para las Corrutinas
using System.Collections.Generic;

public class ControladorAnimaciones : MonoBehaviour
{
    [Header("Componentes")]
    [Tooltip("El Animator de este personaje (se auto-asigna si está vacío)")]
    public Animator animator;

    [Header("Efectos Visuales (VFX)")]
    [Tooltip("El prefab del rayo de agua que vas a crear")]
    public GameObject prefabAtaqueAgua;
    
    [Tooltip("Si disparas con una sola mano, ponla aquí.")]
    public Transform puntoDeDisparo;
    
    [Tooltip("Si disparas tipo Kamehameha, pon la mano Izquierda aquí")]
    public Transform manoIzquierda;
    [Tooltip("Si disparas tipo Kamehameha, pon la mano Derecha aquí")]
    public Transform manoDerecha;

    [Header("Ajustes de Disparo")]
    [Tooltip("Tiempo que tarda en salir el agua en el ataque básico")]
    public float retrasoAtaqueNormal = 0.8f; // Aumentado para que salga más tarde
    [Tooltip("Tiempo que tarda en salir el agua en el ataque crítico")]
    public float retrasoAtaqueCritico = 1.0f;
    [Tooltip("Mueve el disparo X metros hacia adelante para que no salga desde dentro del cuerpo")]
    public float desplazamientoAdelante = 1.0f;

    private readonly HashSet<string> avisosParametrosAusentes = new HashSet<string>();
    private readonly List<GameObject> efectosActivos = new List<GameObject>();

    private void Start()
    {
        AsegurarAnimator();
    }

    // --- MÉTODOS PARA LLAMAR DESDE GESTORNIVEL O BOTONES DE PRUEBA ---

    public void EjecutarAtaque(JugadaRPS tipoAtaque)
    {
        PrepararAccion();
        IntentarActivarTrigger("Atacar");
        
        if (prefabAtaqueAgua != null)
        {
            StartCoroutine(AparecerEfectoConRetraso(retrasoAtaqueNormal)); 
        }
    }

    public void EjecutarAtaqueCritico(JugadaRPS tipoAtaque)
    {
        PrepararAccion();
        IntentarActivarTrigger("AtacarCritico");

        if (prefabAtaqueAgua != null)
        {
            StartCoroutine(AparecerEfectoConRetraso(retrasoAtaqueCritico));
        }
    }

    private IEnumerator AparecerEfectoConRetraso(float retraso)
    {
        yield return new WaitForSeconds(retraso);
        
        Vector3 posicionDisparo = transform.position + Vector3.up * 1f; 
        Quaternion rotacionDisparo = transform.rotation; 

        if (manoIzquierda != null && manoDerecha != null)
        {
            posicionDisparo = (manoIzquierda.position + manoDerecha.position) / 2f;
        }
        else if (puntoDeDisparo != null)
        {
            posicionDisparo = puntoDeDisparo.position;
        }

        // Empujamos el agua hacia adelante para que salga de las palmas y no del pecho/atrás
        posicionDisparo += transform.forward * desplazamientoAdelante;

        efectosActivos.Add(Instantiate(prefabAtaqueAgua, posicionDisparo, rotacionDisparo));
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
