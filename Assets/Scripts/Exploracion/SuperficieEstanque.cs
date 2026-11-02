using UnityEngine;

namespace Pokimun.Exploracion
{
    /// <summary>Movimiento visual ligero, sin físicas de fluidos ni emisores de audio.</summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class SuperficieEstanque : MonoBehaviour
    {
        public bool lava;
        private Renderer superficie;
        private MaterialPropertyBlock propiedades;
        private Vector4 escala;
        private void Awake()
        {
            superficie = GetComponent<Renderer>(); propiedades = new MaterialPropertyBlock();
            Vector2 tiling = superficie.sharedMaterial.mainTextureScale;
            escala = new Vector4(tiling.x, tiling.y, 0, 0);
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
