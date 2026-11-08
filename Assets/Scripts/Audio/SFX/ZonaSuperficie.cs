using System.Collections.Generic;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Zona que cambia el sonido de los pasos (madera, charco...).
    /// Usa los límites de un Collider o, si no hay, un radio alrededor del objeto.
    /// Opcionalmente dibuja un charco simple para que la zona se vea.
    /// </summary>
    public sealed class ZonaSuperficie : MonoBehaviour
    {
        public string eventoPaso = "Play_SFX_Paso_Madera";
        [Tooltip("Si se asigna, la zona es el volumen de este collider")] public Collider limite;
        [Tooltip("Si no hay collider: radio horizontal en metros")] public float radio = 1.5f;
        [Tooltip("Mayor prioridad gana cuando hay zonas superpuestas")] public int prioridad = 0;
        public bool dibujarCharco;

        private static readonly List<ZonaSuperficie> zonas = new List<ZonaSuperficie>();

        private void OnEnable() => zonas.Add(this);
        private void OnDisable() => zonas.Remove(this);

        private void Start()
        {
            if (!dibujarCharco) return;
            var charco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            charco.name = "Charco (visual)";
            Destroy(charco.GetComponent<Collider>());
            charco.transform.SetParent(transform, false);
            charco.transform.localPosition = new Vector3(0, 0.015f, 0);
            charco.transform.localScale = new Vector3(radio * 2f, 0.004f, radio * 1.6f);
            var r = charco.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var m = new Material(shader);
                m.SetColor("_BaseColor", new Color(0.16f, 0.22f, 0.28f));
                m.SetFloat("_Smoothness", 0.95f);
                m.SetFloat("_Metallic", 0.1f);
                r.sharedMaterial = m;
            }
        }

        public bool Contiene(Vector3 p)
        {
            if (limite != null) return limite.bounds.Contains(p + Vector3.up * 0.5f);
            Vector3 d = p - transform.position; d.y = 0;
            return d.sqrMagnitude <= radio * radio;
        }

        public static string EventoEn(Vector3 p, string porDefecto)
        {
            ZonaSuperficie mejor = null;
            foreach (var z in zonas)
                if (z.Contiene(p) && (mejor == null || z.prioridad > mejor.prioridad)) mejor = z;
            return mejor != null ? mejor.eventoPaso : porDefecto;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            if (limite != null) Gizmos.DrawWireCube(limite.bounds.center, limite.bounds.size);
            else Gizmos.DrawWireSphere(transform.position, radio);
        }
#endif
    }
}
