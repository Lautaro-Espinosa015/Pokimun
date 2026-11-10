using System.Collections;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Convierte un bloque de madera (cubo de 1 m) en un cofre: cuerpo, tapa curva, herrajes y cerradura.
    /// Se ve también en el editor (las piezas no se guardan en la escena). Abrir() levanta la tapa.
    /// Mantiene el collider original, así sigue funcionando la interacción.
    /// </summary>
    [ExecuteAlways]
    public sealed class AparienciaCofre : MonoBehaviour
    {
        public Color madera = new Color(0.45f, 0.27f, 0.13f);
        public Color metal = new Color(0.22f, 0.22f, 0.24f);
        [Tooltip("Oculta los listones sueltos que estaban pegados al bloque")]
        public bool ocultarListones = true;
        [Tooltip("Cara frontal del cofre (la cerradura): +Z o -Z del bloque")]
        public bool frenteHaciaMasZ = true;
        public float anguloApertura = 65f;
        public float tiempoAbierto = 2.5f;

        private GameObject raiz;
        private Transform bisagra;
        private Renderer rendererOriginal;
        private readonly System.Collections.Generic.List<Renderer> listones = new System.Collections.Generic.List<Renderer>();
        private Coroutine animacion;

        private void OnEnable() => Construir();
        private void OnDisable() => Destruir();

        private void Construir()
        {
            if (raiz != null) return;
            rendererOriginal = GetComponent<Renderer>();
            if (rendererOriginal != null) rendererOriginal.enabled = false;
            if (ocultarListones && transform.parent != null)
                foreach (Transform h in transform.parent)
                    if (h.name.StartsWith("List") && h.name.Contains("caja") && Vector3.Distance(h.position, transform.position) < 0.9f)
                    { var r = h.GetComponent<Renderer>(); if (r != null && r.enabled) { r.enabled = false; listones.Add(r); } }

            var matMadera = Material(madera, 0.25f); var matMetal = Material(metal, 0.6f, 0.8f);
            raiz = new GameObject("Cofre (visual)") { hideFlags = HideFlags.HideAndDontSave };
            raiz.transform.SetParent(transform, false);
            float f = frenteHaciaMasZ ? 1f : -1f;

            // Cuerpo: 1.0 x 0.55 x 0.7, apoyado en el piso del bloque
            Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(0, -0.225f, 0), Vector3.zero, new Vector3(1f, 0.55f, 0.7f), matMadera);
            // Bandas de metal del cuerpo
            foreach (float x in new[] { -0.32f, 0.32f })
                Pieza(PrimitiveType.Cube, raiz.transform, new Vector3(x, -0.225f, 0), Vector3.zero, new Vector3(0.07f, 0.565f, 0.715f), matMetal);
            // Tapa curva con bisagra atrás
            bisagra = new GameObject("Bisagra") { hideFlags = HideFlags.HideAndDontSave }.transform;
            bisagra.SetParent(raiz.transform, false);
            bisagra.localPosition = new Vector3(0, 0.05f, -0.35f * f);
            Pieza(PrimitiveType.Cylinder, bisagra, new Vector3(0, 0, 0.35f * f), new Vector3(0, 0, 90), new Vector3(0.7f, 0.5f, 0.7f), matMadera);
            foreach (float x in new[] { -0.32f, 0.32f })
                Pieza(PrimitiveType.Cylinder, bisagra, new Vector3(x, 0, 0.35f * f), new Vector3(0, 0, 90), new Vector3(0.72f, 0.036f, 0.72f), matMetal);
            // Cerradura
            Pieza(PrimitiveType.Cube, bisagra, new Vector3(0, -0.03f, 0.71f * f), Vector3.zero, new Vector3(0.13f, 0.16f, 0.04f), matMetal);
        }

        private void Destruir()
        {
            if (raiz != null) { if (Application.isPlaying) Destroy(raiz); else DestroyImmediate(raiz); }
            raiz = null;
            if (rendererOriginal != null) rendererOriginal.enabled = true;
            foreach (var r in listones) if (r != null) r.enabled = true;
            listones.Clear();
        }

        /// <summary>Levanta la tapa, la deja abierta un momento y la vuelve a cerrar.</summary>
        public void Abrir()
        {
            if (bisagra == null || !Application.isPlaying) return;
            if (animacion != null) StopCoroutine(animacion);
            animacion = StartCoroutine(AnimarTapa());
        }

        private IEnumerator AnimarTapa()
        {
            float signo = frenteHaciaMasZ ? -1f : 1f;
            yield return Rotar(0f, anguloApertura * signo, 0.45f);
            yield return new WaitForSeconds(tiempoAbierto);
            yield return Rotar(anguloApertura * signo, 0f, 0.6f);
            animacion = null;
        }

        private IEnumerator Rotar(float desde, float hasta, float duracion)
        {
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                bisagra.localRotation = Quaternion.Euler(Mathf.SmoothStep(desde, hasta, t / duracion), 0, 0);
                yield return null;
            }
            bisagra.localRotation = Quaternion.Euler(hasta, 0, 0);
        }

        private static void Pieza(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 rot, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.hideFlags = HideFlags.HideAndDontSave;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos; go.transform.localEulerAngles = rot; go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material Material(Color c, float suavidad, float metalico = 0f)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            m.SetColor("_BaseColor", c); m.color = c;
            m.SetFloat("_Smoothness", suavidad); m.SetFloat("_Glossiness", suavidad); m.SetFloat("_Metallic", metalico);
            return m;
        }
    }
}
