using UnityEngine;

namespace Pokimun.Exploracion
{
    public sealed class DestinoExploracion : MonoBehaviour
    {
        public string nombreVoz;
        public Transform llegada;
        public Transform mirada;
        public string[] aliasVoz;
        public string escenaDestino;
        [TextArea] public string descripcion;
        public Vector3 PuntoLlegada => llegada != null ? llegada.position : transform.position;
    }
}
