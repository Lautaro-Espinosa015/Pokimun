using UnityEngine;

/// <summary>
/// Mueve el efecto visual de agua hacia adelante y lo destruye después de unos segundos.
/// </summary>
public class AtaqueVisualAgua : MonoBehaviour
{
    [Tooltip("Velocidad a la que viaja el rayo de agua")]
    public float velocidad = 15f;
    
    [Tooltip("Tiempo en segundos antes de que el agua desaparezca")]
    public float tiempoDeVida = 2f;

    private void Start()
    {
        // Se autodestruye para no consumir memoria infinita
        Destroy(gameObject, tiempoDeVida);
    }

    private void Update()
    {
        // Mueve el proyectil hacia adelante
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }
}
