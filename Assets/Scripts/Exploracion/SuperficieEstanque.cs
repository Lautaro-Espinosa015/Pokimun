using UnityEngine;

namespace Pokimun.Exploracion
{
    /// <summary>Movimiento visual ligero, sin físicas de fluidos ni emisores de audio.</summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class SuperficieEstanque : MonoBehaviour
    {
        public bool lava;
        [SerializeField] private Material piedraFuente;
        [SerializeField] private Material gotasFuente;
        private Renderer superficie;
        private GameObject fuenteCreada;
        private MaterialPropertyBlock propiedades;
        private Vector4 escala;
        private void Awake()
        {
            superficie = GetComponent<Renderer>(); propiedades = new MaterialPropertyBlock();
            Vector2 tiling = superficie.sharedMaterial.mainTextureScale;
            escala = new Vector4(tiling.x, tiling.y, 0, 0);
            if (!lava && piedraFuente != null && gotasFuente != null)
                CrearFuente();
        }

        private void CrearFuente()
        {
            // El estanque está escalado para formar un disco bajo. La fuente usa metros.
            fuenteCreada = new GameObject("Fuente de Hydros");
            var fuente = fuenteCreada.transform;
            fuente.SetParent(transform.parent, false);
            fuente.SetPositionAndRotation(
                new Vector3(transform.position.x, superficie.bounds.max.y, transform.position.z),
                transform.rotation);

            Piedra(fuente, "Pedestal", PrimitiveType.Cylinder,
                new Vector3(0, .36f, .45f), new Vector3(.46f, .36f, .46f));
            Piedra(fuente, "Boca del surtidor", PrimitiveType.Cube,
                new Vector3(0, .7f, .22f), new Vector3(.27f, .15f, .47f));

            Agua(fuente, "Agua cayendo", new Vector3(0, .69f, -.03f),
                Quaternion.Euler(23, 180, 0),
                125f, .58f, .46f, .075f, .075f, 4f);
            Agua(fuente, "Salpicadura del surtidor", new Vector3(0, .018f, -.31f),
                Quaternion.Euler(-90, 0, 0),
                24f, .26f, .65f, .035f, .8f, 52f);
        }

        private void OnDestroy()
        {
            if (fuenteCreada != null)
                Destroy(fuenteCreada);
        }

        private void Piedra(Transform padre, string nombre, PrimitiveType tipo, Vector3 posicion, Vector3 tamano)
        {
            var pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre;
            pieza.layer = 2; // Ignore Raycast. El estanque conserva su interacción.
            pieza.transform.SetParent(padre, false);
            pieza.transform.localPosition = posicion;
            pieza.transform.localScale = tamano;
            var collider = pieza.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            pieza.GetComponent<Renderer>().sharedMaterial = piedraFuente;
        }

        private void Agua(Transform padre, string nombre, Vector3 posicion, Quaternion direccion,
            float frecuencia, float vida, float velocidad, float tamano, float radio, float angulo)
        {
            var objeto = new GameObject(nombre);
            objeto.SetActive(false);
            objeto.transform.SetParent(padre, false);
            objeto.transform.SetLocalPositionAndRotation(posicion, direccion);
            var sistema = objeto.AddComponent<ParticleSystem>();
            var principal = sistema.main;
            principal.loop = true;
            principal.prewarm = true;
            principal.startLifetime = vida;
            principal.startSpeed = velocidad;
            principal.startSize = new ParticleSystem.MinMaxCurve(tamano * .65f, tamano);
            principal.gravityModifier = .4f;
            principal.startColor = new Color(.68f, .87f, 1f, .65f);
            principal.maxParticles = 100;

            var emision = sistema.emission;
            emision.rateOverTime = frecuencia;
            var forma = sistema.shape;
            forma.shapeType = ParticleSystemShapeType.Cone;
            forma.radius = radio;
            forma.angle = angulo;

            var color = sistema.colorOverLifetime;
            color.enabled = true;
            var desvanecer = new Gradient();
            desvanecer.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .65f), new GradientAlphaKey(0, 1) });
            color.color = desvanecer;
            var renderer = objeto.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = gotasFuente;
            objeto.SetActive(true);
        }
        private void Update()
        {
            escala.z = Time.time * (lava ? .008f : .025f); escala.w = Time.time * .009f;
            propiedades.SetVector("_BaseMap_ST", escala);
            if (lava) propiedades.SetColor("_EmissionColor", new Color(1, .22f, .025f) * (1.2f + Mathf.Sin(Time.time * 1.2f) * .15f));
            superficie.SetPropertyBlock(propiedades);
        }
    }
}
