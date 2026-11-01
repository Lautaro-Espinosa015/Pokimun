using UnityEngine;
using UnityEngine.EventSystems;

namespace Pokimun.Exploracion
{
    /// <summary>Cada zona captura un dedo independiente; los botones quedan por encima en el Canvas.</summary>
    public sealed class EntradaTactilExploracion : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public bool giro;
        public Vector2 Valor { get; private set; }
        public RectTransform indicador;
        private Vector2 origen;
        private int dedo = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (dedo != int.MinValue) return;
            dedo = e.pointerId; origen = e.position; Valor = Vector2.zero;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != dedo) return;
            if (giro) Valor += e.delta;
            else
            {
                float radio = 75 * GetComponentInParent<Canvas>().scaleFactor;
                Valor = Vector2.ClampMagnitude((e.position - origen) / radio, 1);
                if (indicador != null) indicador.anchoredPosition = Valor * 60;
            }
        }
        public Vector2 ConsumirGiro() { var valor = Valor; Valor = Vector2.zero; return valor; }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == dedo) Limpiar(); }
        public void Limpiar()
        {
            dedo = int.MinValue; Valor = Vector2.zero;
            if (indicador != null) indicador.anchoredPosition = Vector2.zero;
        }
        private void OnDisable() => Limpiar();
    }
}
