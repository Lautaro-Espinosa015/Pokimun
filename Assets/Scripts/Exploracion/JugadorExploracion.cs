using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using StarterAssets;

namespace Pokimun.Exploracion
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-100)]
    public sealed class JugadorExploracion : MonoBehaviour
    {
        public Camera camara;
        public bool terceraPersona;
        public Transform cuerpo;
        public float velocidad = 3.5f;
        public EntradaTactilExploracion movimientoTactil, giroTactil;
        private bool bloqueado;
        public bool Bloqueado
        {
            get => bloqueado;
            set
            {
                bloqueado = value;
                if (entradaOficial != null) entradaOficial.move = entradaOficial.look = Vector2.zero;
                if (controlOficial != null) controlOficial.enabled = !value;
            }
        }
        public bool Navegando => ruta != null;
        public string Aviso { get; private set; }
        private CharacterController motor;
        private NavMeshPath ruta;
        private int esquina;
        private float yaw, pitch = 12;
        private Vector3 seguro;
        private float tiempoAtascado;
        private StarterAssetsInputs entradaOficial;
        private FirstPersonController controlOficial;
        private Animator animador;
        private Transform cabeza;

        private void Awake()
        {
            motor = GetComponent<CharacterController>(); seguro = transform.position; yaw = transform.eulerAngles.y;
            entradaOficial = GetComponent<StarterAssetsInputs>(); controlOficial = GetComponent<FirstPersonController>();
            animador = cuerpo != null ? cuerpo.GetComponentInChildren<Animator>() : null;
            if (animador != null && animador.isHuman && !terceraPersona) cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
        }
        public void Detener() { ruta = null; tiempoAtascado = 0; }
        public void Girar(float grados) { Detener(); yaw += grados; }
        public bool IrA(Vector3 destino)
        {
            if (Bloqueado) return false;
            if (!NavMesh.SamplePosition(transform.position, out var inicio, 3, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(destino, out var fin, 2, NavMesh.AllAreas))
            { Aviso = "No hay un camino accesible a ese destino."; return false; }
            var nueva = new NavMeshPath();
            if (!NavMesh.CalculatePath(inicio.position, fin.position, NavMesh.AllAreas, nueva) || nueva.status != NavMeshPathStatus.PathComplete)
            { Aviso = "Ese destino está bloqueado."; return false; }
            ruta = nueva; esquina = 0; tiempoAtascado = 0; Aviso = "Caminando al destino. Decí detener para cancelar.";
            return true;
        }
        private void Update()
        {
            if (camara == null) return;
            bool ui = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;
            if (Bloqueado || ui) return;
            var teclado = Keyboard.current; var mouse = Mouse.current;
            Vector2 entrada = movimientoTactil != null ? movimientoTactil.Valor : Vector2.zero;
            if (teclado != null)
            {
                entrada += new Vector2((teclado.dKey.isPressed ? 1 : 0) - (teclado.aKey.isPressed ? 1 : 0),
                    (teclado.wKey.isPressed ? 1 : 0) - (teclado.sKey.isPressed ? 1 : 0));
            }
            if (entrada.sqrMagnitude > .01f) Detener();
            Vector2 mirar = giroTactil != null ? giroTactil.ConsumirGiro() * .11f : Vector2.zero;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked) mirar += mouse.delta.ReadValue() * .12f;
            if (mirar.sqrMagnitude > 0) { yaw += mirar.x; pitch = Mathf.Clamp(pitch - mirar.y, -65, 75); }
            if (!Application.isMobilePlatform && mouse != null && mouse.leftButton.wasPressedThisFrame &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            Vector3 movimiento = Quaternion.Euler(0, yaw, 0) * new Vector3(entrada.x, 0, entrada.y);
            if (ruta != null)
            {
                var puntos = ruta.corners;
                while (esquina < puntos.Length && Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                    new Vector2(puntos[esquina].x, puntos[esquina].z)) < .4f) esquina++;
                if (esquina >= puntos.Length) { Detener(); Aviso = "Llegaste al destino. Mirá el objeto para interactuar."; }
                else
                {
                    movimiento = puntos[esquina] - transform.position; movimiento.y = 0; movimiento.Normalize();
                    yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(movimiento).eulerAngles.y, 180 * Time.deltaTime);
                }
            }
            // El movimiento y la gravedad los ejecuta el FirstPersonController oficial de Starter Assets.
            Vector3 local = Quaternion.Inverse(Quaternion.Euler(0, yaw, 0)) * Vector3.ClampMagnitude(movimiento, 1);
            entradaOficial.move = new Vector2(local.x, local.z); entradaOficial.look = Vector2.zero;
            entradaOficial.jump = entradaOficial.sprint = false;
            if (animador != null) animador.SetFloat("Velocidad", new Vector3(motor.velocity.x, 0, motor.velocity.z).magnitude);
            if (ruta != null)
            {
                tiempoAtascado = new Vector3(motor.velocity.x, 0, motor.velocity.z).sqrMagnitude < .01f ? tiempoAtascado + Time.deltaTime : 0;
                if (tiempoAtascado > 2) { Detener(); Aviso = "Camino bloqueado. Podés moverte manualmente."; }
            }
            if (transform.position.y < seguro.y - 12)
            {
                motor.enabled = false; transform.position = seguro; motor.enabled = true; Detener();
            }
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        private void LateUpdate()
        {
            if (camara == null) return;
            if (cabeza != null) cabeza.localScale = Vector3.one * .001f;
            Quaternion rotacion = Quaternion.Euler(pitch, yaw, 0);
            Vector3 ojo = transform.position + Vector3.up * 1.65f;
            Vector3 posicion = ojo;
            if (terceraPersona)
            {
                Vector3 direccion = rotacion * Vector3.back;
                float distancia = 4;
                if (Physics.SphereCast(ojo, .18f, direccion, out var hit, distancia, 1 << 0, QueryTriggerInteraction.Ignore))
                    distancia = Mathf.Max(.2f, hit.distance - .1f);
                posicion += direccion * distancia;
            }
            else posicion += transform.forward * .24f;
            camara.transform.SetPositionAndRotation(posicion, rotacion);
        }
        private void OnApplicationFocus(bool foco) { if (!foco) SoltarCursor(); }
        public void SoltarCursor()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            movimientoTactil?.Limpiar(); giroTactil?.Limpiar();
        }
        private void OnDisable() => SoltarCursor();
    }
}
