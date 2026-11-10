using System.Collections;
using Pokimun.Exploracion;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Puerta doble de madera para el cobertizo del nivel de búsqueda. Se arma entre los dos "Marco de puerta"
    /// (visible también en el editor; las piezas no se guardan en la escena) y no tiene colisión.
    /// En juego, cuando el jugador se acerca, las hojas se abren hacia afuera, suena la puerta y quedan abiertas.
    /// </summary>
    [ExecuteAlways]
    public sealed class PuertaCobertizo : MonoBehaviour
    {
        public string nombreMarco = "Marco de puerta";
        public string evento = "Play_SFX_Puerta_Cobertizo";
        [Tooltip("Altura del piso del cobertizo y alto de la puerta (debajo del dintel)")]
        public float baseY = 0.06f, alto = 2.72f;
        public float grosor = 0.06f;
        [Tooltip("Distancia horizontal del jugador a la puerta para que se abra")]
        public float radioApertura = 1.8f;
        public float anguloApertura = 95f;
        [Tooltip("Retraso de la animación respecto del sonido y duración del giro")]
        public float retraso = 0.15f, duracion = 1.4f;
        public Color madera = new Color(0.42f, 0.26f, 0.13f);
        public Color metal = new Color(0.2f, 0.2f, 0.22f);
        [Tooltip("Las hojas abren hacia +Z del mundo (afuera del cobertizo)")]
        public bool abrirHaciaMasZ = true;

        public bool Abierta { get; private set; }

        private GameObject raiz;
        private Transform bisagraIzq, bisagraDer;
        private Vector3 centro;
        private Transform jugador;

        private void OnEnable() => Construir();
        private void OnDisable() => Destruir();

        private void Construir()
        {
            if (raiz != null) return;
            Transform izq = null, der = null;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name != nombreMarco || t.gameObject.scene != gameObject.scene) continue;
                if (izq == null || t.position.x < izq.position.x) izq = t;
                if (der == null || t.position.x > der.position.x) der = t;
            }
            if (izq == null || der == null || izq == der) return;

            // Bordes interiores de los marcos
            Vector3 eje = der.position - izq.position; eje.y = 0;
            Vector3 dir = eje.normalized;
            float medioMarco = izq.lossyScale.x * 0.5f;
            Vector3 a = izq.position + dir * medioMarco, b = der.position - dir * medioMarco;
            a.y = b.y = baseY;
            float anchoHoja = Vector3.Distance(a, b) * 0.5f - 0.01f;
            centro = (a + b) * 0.5f + Vector3.up * (alto * 0.5f);
            // Local +X = de un marco al otro; local +Z = hacia afuera (en este cobertizo, +Z del mundo)
            Vector3 haciaAfuera = Vector3.Cross(dir, Vector3.up) * (abrirHaciaMasZ ? 1f : -1f);
            Quaternion orientacion = Quaternion.LookRotation(haciaAfuera);
            if (!abrirHaciaMasZ) { var tmp = a; a = b; b = tmp; }

            var matA = Material(madera, 0.2f);
            var matB = Material(madera * 0.85f, 0.2f);
            var matC = Material(madera * 0.7f, 0.15f);
            var matMetal = Material(metal, 0.55f, 0.8f);

            raiz = new GameObject("Puerta cobertizo (visual)") { hideFlags = HideFlags.HideAndDontSave };
            raiz.transform.position = centro;
            bisagraIzq = Hoja(a, orientacion, 1f, anchoHoja, matA, matB, matC, matMetal);
            bisagraDer = Hoja(b, orientacion, -1f, anchoHoja, matA, matB, matC, matMetal);
            if (Abierta) { bisagraIzq.localRotation = Quaternion.Euler(0, -anguloApertura, 0); bisagraDer.localRotation = Quaternion.Euler(0, anguloApertura, 0); }
        }

        /// <summary>Una hoja con bisagra en "pivote"; s = +1 se extiende hacia la derecha, -1 hacia la izquierda.</summary>
        private Transform Hoja(Vector3 pivote, Quaternion orientacion, float s, float w, Material mA, Material mB, Material mC, Material mM)
        {
            var marco = new GameObject("Hoja") { hideFlags = HideFlags.HideAndDontSave }.transform;
            marco.SetParent(raiz.transform, false);
            marco.position = pivote; marco.rotation = orientacion;
            var bisagra = new GameObject("Bisagra") { hideFlags = HideFlags.HideAndDontSave }.transform;
            bisagra.SetParent(marco, false);

            float h = alto, t = grosor, adentro = -(t * 0.5f + 0.02f), afuera = t * 0.5f + 0.008f;
            // Tablas verticales
            for (int i = 0; i < 3; i++)
                Pieza(PrimitiveType.Cube, bisagra, new Vector3(s * (w / 6f + i * w / 3f), h * 0.5f, 0), Vector3.zero,
                      new Vector3(w / 3f - 0.012f, h - (i == 1 ? 0f : 0.02f), t), i == 1 ? mB : mA);
            // Travesaños y diagonal (cara interior)
            foreach (float y in new[] { 0.35f, h - 0.35f })
                Pieza(PrimitiveType.Cube, bisagra, new Vector3(s * w * 0.5f, y, adentro), Vector3.zero, new Vector3(w - 0.06f, 0.14f, 0.04f), mC);
            float dx = w - 0.2f, dy = h - 0.84f;
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            Pieza(PrimitiveType.Cube, bisagra, new Vector3(s * w * 0.5f, h * 0.5f, adentro), new Vector3(0, 0, s * ang),
                  new Vector3(Mathf.Sqrt(dx * dx + dy * dy), 0.12f, 0.035f), mC);
            // Herrajes (cara exterior) y tiradores
            foreach (float y in new[] { 0.45f, h - 0.45f })
                Pieza(PrimitiveType.Cube, bisagra, new Vector3(s * w * 0.32f, y, afuera), Vector3.zero, new Vector3(w * 0.64f, 0.07f, 0.015f), mM);
            Pieza(PrimitiveType.Cube, bisagra, new Vector3(s * (w - 0.12f), 1.05f, t * 0.5f + 0.04f), Vector3.zero, new Vector3(0.04f, 0.24f, 0.04f), mM);
            Pieza(PrimitiveType.Cube, bisagra, new Vector3(s * (w - 0.12f), 1.05f, -(t * 0.5f + 0.06f)), Vector3.zero, new Vector3(0.04f, 0.24f, 0.04f), mM);
            return bisagra;
        }

        private void Destruir()
        {
            if (raiz != null) { if (Application.isPlaying) Destroy(raiz); else DestroyImmediate(raiz); }
            raiz = null;
        }

        private void Update()
        {
            if (!Application.isPlaying || Abierta || raiz == null) return;
            if (jugador == null)
            {
                var gestor = FindFirstObjectByType<GestorExploracion>();
                var j = gestor != null ? gestor.jugador : FindFirstObjectByType<JugadorExploracion>();
                if (j == null) return;
                jugador = j.transform;
            }
            Vector3 d = jugador.position - centro; d.y = 0;
            if (d.sqrMagnitude < radioApertura * radioApertura) Abrir();
        }

        /// <summary>Abre las dos hojas hacia afuera (una sola vez) con el sonido de la puerta.</summary>
        public void Abrir()
        {
            if (Abierta || !Application.isPlaying) return;
            Abierta = true;
            AudioTP.Post(evento, raiz != null ? raiz : gameObject);
            StartCoroutine(Animar());
        }

        private IEnumerator Animar()
        {
            yield return new WaitForSeconds(retraso);
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duracion);
                // Pequeño rebote al final, como una hoja pesada
                float ang = anguloApertura * k + Mathf.Sin(k * Mathf.PI) * 4f * k;
                if (bisagraIzq != null) bisagraIzq.localRotation = Quaternion.Euler(0, -ang, 0);
                if (bisagraDer != null) bisagraDer.localRotation = Quaternion.Euler(0, ang, 0);
                yield return null;
            }
            if (bisagraIzq != null) bisagraIzq.localRotation = Quaternion.Euler(0, -anguloApertura, 0);
            if (bisagraDer != null) bisagraDer.localRotation = Quaternion.Euler(0, anguloApertura, 0);
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
            c.a = 1f;
            m.SetColor("_BaseColor", c); m.color = c;
            m.SetFloat("_Smoothness", suavidad); m.SetFloat("_Glossiness", suavidad); m.SetFloat("_Metallic", metalico);
            return m;
        }
    }
}
