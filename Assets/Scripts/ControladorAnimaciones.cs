using UnityEngine;
using System.Collections; // Necesario para las Corrutinas

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

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // --- MÉTODOS PARA LLAMAR DESDE GESTORNIVEL O BOTONES DE PRUEBA ---

    public void EjecutarAtaque(JugadaRPS tipoAtaque)
    {
        animator.SetTrigger("Atacar");
        Debug.Log($"[Animador] Ejecutando ataque normal de {tipoAtaque}");
        
        if (prefabAtaqueAgua != null)
        {
            StartCoroutine(AparecerEfectoConRetraso(retrasoAtaqueNormal)); 
        }
    }

    public void EjecutarAtaqueCritico(JugadaRPS tipoAtaque)
    {
        animator.SetTrigger("AtacarCritico");
        Debug.Log($"[Animador] ¡CRÍTICO! Ejecutando ataque mágico de área por {tipoAtaque}");

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

        Instantiate(prefabAtaqueAgua, posicionDisparo, rotacionDisparo);
    }

    public void EjecutarDefensa(bool estaDefendiendo)
    {
        // La defensa suele ser una postura que se mantiene, así que usamos un Booleano
        animator.SetBool("Defendiendo", estaDefendiendo);
        if (estaDefendiendo) Debug.Log("[Animador] Cubriéndose...");
    }

    public void RecibirDano()
    {
        // Reacción a recibir un golpe
        animator.SetTrigger("RecibirDano");
    }

    public void CelebrarVictoria()
    {
        animator.SetTrigger("Victoria");
    }

    public void CaerDerrotado()
    {
        // Animación de muerte/desmayo
        animator.SetTrigger("Derrota");
    }
}
