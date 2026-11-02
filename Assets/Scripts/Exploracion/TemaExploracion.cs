using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pokimun.Exploracion
{
    /// <summary>Referencias a los mismos botones y tipografía del combate.</summary>
    public sealed class TemaExploracion : ScriptableObject
    {
        public Sprite boton;
        public TMP_FontAsset fuente;
        private static TemaExploracion instancia;
        public static TemaExploracion Actual => instancia != null ? instancia : instancia = Resources.Load<TemaExploracion>("Exploracion/TemaCombate");
        public static bool EsAndroid => Application.platform == RuntimePlatform.Android;
        public static Color Marron => new Color(.35f, .22f, .10f, .96f);
        public static Color Oro => new Color(1, .78f, .3f);
        public static void Texto(TMP_Text texto, bool titulo = false)
        {
            if(Actual != null && Actual.fuente != null) texto.font = Actual.fuente;
            texto.color = titulo ? Oro : new Color(.9f,.95f,1);
            texto.enableAutoSizing = true; texto.fontSizeMax = texto.fontSize; texto.fontSizeMin = texto.fontSize * .72f;
            texto.raycastTarget = false;
        }
        public static void Boton(Button boton, bool animar = true)
        {
            var imagen = boton.GetComponent<Image>();
            if(Actual != null && Actual.boton != null) { imagen.sprite = Actual.boton; imagen.type = Image.Type.Sliced; imagen.color = Color.white; }
            else imagen.color = new Color(.11f,.26f,.32f);
            boton.targetGraphic = imagen; boton.navigation = new Navigation { mode = Navigation.Mode.None };
            if(animar && boton.GetComponent<BotonAnimado>() == null)
            {
                var efecto = boton.gameObject.AddComponent<BotonAnimado>(); efecto.multiplicadorHover = 1.035f;
            }
        }
        public static void Panel(Image imagen)
        {
            imagen.color = Marron;
            var borde = imagen.GetComponent<Outline>(); if(borde == null)borde = imagen.gameObject.AddComponent<Outline>();
            borde.effectColor = new Color(.74f,.49f,.18f,.8f); borde.effectDistance = new Vector2(2,-2);
        }
    }
}
