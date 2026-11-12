using Pokimun.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class BotonAnimado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public const string EventoHover = "Play_SFX_Menu_Hover";
    public const string EventoClick = "Play_SFX_Menu_Click";

    [Header("Ajustes de Escala")]
    [Tooltip("El tamaño que tendrá el botón al pasar el mouse por encima (1.1 = 10% más grande)")]
    public float multiplicadorHover = 1.1f;
    [Tooltip("El tamaño que tendrá el botón al hacer click (0.95 = 5% más pequeño)")]
    public float multiplicadorClick = 0.95f;
    
    [Header("Velocidad de Animación")]
    public float velocidadTransicion = 12f;

    private Vector3 escalaOriginal;
    private Vector3 escalaObjetivo;
    private bool mouseEncima = false;
    private Button botonConSonido;
    private GameObject emisor;

    public void ActivarSonido(Button boton, GameObject emisorAudio)
    {
        botonConSonido = boton;
        emisor = emisorAudio;
    }

    private void Awake()
    {
        // Guardamos la escala original del botón (normalmente 1,1,1)
        escalaOriginal = transform.localScale;
        escalaObjetivo = escalaOriginal;
    }

    private void OnDisable()
    {
        mouseEncima = false;
        escalaObjetivo = escalaOriginal;
        transform.localScale = escalaOriginal;
    }

    private void Update()
    {
        // Interpola suavemente la escala actual hacia la escala objetivo
        // Usamos unscaledDeltaTime para que la animación funcione incluso si el juego está pausado
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.unscaledDeltaTime * velocidadTransicion);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseEncima = true;
        escalaObjetivo = escalaOriginal * multiplicadorHover;
        if (eventData is ExtendedPointerEventData { device: Mouse } &&
            botonConSonido != null && botonConSonido.IsInteractable())
            AudioTP.Post(EventoHover, emisor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseEncima = false;
        escalaObjetivo = escalaOriginal;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal * multiplicadorClick;
        if (eventData.button == PointerEventData.InputButton.Left &&
            botonConSonido != null && botonConSonido.IsInteractable())
            AudioTP.Post(EventoClick, emisor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mouseEncima)
        {
            escalaObjetivo = escalaOriginal * multiplicadorHover;
        }
        else
        {
            escalaObjetivo = escalaOriginal;
        }
    }
}
