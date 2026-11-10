using System.Collections.Generic;
using Pokimun.Exploracion;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Roce con la vegetación: cuando el jugador entra en el radio de un arbusto, planta o flor, suena su evento 3D
    /// desde esa planta. Los objetos se buscan por el comienzo del nombre (también dentro de prefabs).
    /// Un intervalo mínimo por grupo evita que se amontonen sonidos cuando hay varias plantas juntas.
    /// </summary>
    public sealed class RoceVegetacion : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Grupo
        {
            public string nombre;
            [Tooltip("Comienzos de nombre de los objetos de este grupo")] public string[] prefijos;
            public string evento;
            public float radio = 1f;
            [Tooltip("Segundos mínimos entre dos roces de este grupo")] public float intervaloMinimo = 0.6f;

            [System.NonSerialized] public readonly List<Transform> objetos = new List<Transform>();
            [System.NonSerialized] public readonly HashSet<Transform> dentro = new HashSet<Transform>();
            [System.NonSerialized] public float ultimo = -10f;
        }

        public Grupo[] grupos = new Grupo[0];
        public Transform jugador;

        private void Start()
        {
            if (jugador == null)
            {
                var j = FindFirstObjectByType<JugadorExploracion>();
                if (j != null) jugador = j.transform;
            }

            var asignados = new HashSet<Transform>();
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.gameObject.scene != gameObject.scene) continue;
                foreach (var g in grupos)
                {
                    if (!Coincide(t.name, g.prefijos)) continue;
                    g.objetos.Add(t); asignados.Add(t);
                    break;
                }
            }
            // Si un prefab y su hijo tienen el mismo nombre, queda solo el de más arriba
            foreach (var g in grupos)
                g.objetos.RemoveAll(t => t.parent != null && TieneAncestro(t.parent, asignados));
        }

        private static bool Coincide(string nombre, string[] prefijos)
        {
            if (prefijos == null) return false;
            foreach (var p in prefijos) if (!string.IsNullOrEmpty(p) && nombre.StartsWith(p)) return true;
            return false;
        }

        private static bool TieneAncestro(Transform t, HashSet<Transform> conjunto)
        {
            for (; t != null; t = t.parent) if (conjunto.Contains(t)) return true;
            return false;
        }

        private void Update()
        {
            if (jugador == null) return;
            Vector3 pj = jugador.position;
            foreach (var g in grupos)
            {
                float r2 = g.radio * g.radio;
                foreach (var o in g.objetos)
                {
                    if (o == null) continue;
                    Vector3 d = o.position - pj; d.y = 0;
                    bool esta = d.sqrMagnitude < r2;
                    if (esta && g.dentro.Add(o))
                    {
                        if (Time.time - g.ultimo > g.intervaloMinimo) { g.ultimo = Time.time; AudioTP.Post(g.evento, o.gameObject); }
                    }
                    else if (!esta) g.dentro.Remove(o);
                }
            }
        }

        /// <summary>Crea un emisor 3D en loop en una posición (por ejemplo, abejas sobre unas flores).</summary>
        public static EmisorSonidoLoop EmisorEn(string nombre, Vector3 posicion, string evento, Transform padre = null)
        {
            var go = new GameObject(nombre);
            if (padre != null) go.transform.SetParent(padre, false);
            go.transform.position = posicion;
            var em = go.AddComponent<EmisorSonidoLoop>(); em.evento = evento;
            return em;
        }
    }
}
