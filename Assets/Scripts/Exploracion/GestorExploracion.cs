using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Pokimun.Exploracion
{
    public sealed class GestorExploracion : MonoBehaviour
    {
        public bool busqueda;
        public JugadorExploracion jugador;
        public Transform ignis;
        public Transform[] escondites;
        public DestinoExploracion[] destinos;
        public Collider interiorCobertizo;
        public InterfazExploracion interfaz;
        public ReglasEscondite Partida { get; private set; }
        public bool Pausado { get; private set; }
        public bool Cargando { get; private set; }
        public event Action PistaDisponible;
        public event Action CambioEscondite;
        public event Action Hallazgo;
        public event Action<DestinoExploracion> DestinoInvestigado;
        private GestorVozMultiplataforma voz;
        private readonly Dictionary<string, Action> comandos = new Dictionary<string, Action>();
        private DestinoExploracion objetivo;
        private bool mirandoIgnis;
        private DateTime ventanaEntrada;
        private bool pausaPorAplicacion;

        private void Start()
        {
            Time.timeScale = 1; ventanaEntrada = DateTime.UtcNow;
            if (busqueda)
            {
                if (escondites == null || escondites.Length != ReglasEscondite.CantidadEscondites || ignis == null)
                    throw new InvalidOperationException("La búsqueda requiere exactamente nueve escondites e Ignis.");
                Partida = new ReglasEscondite(Guid.NewGuid().GetHashCode());
                ColocarIgnis();
            }
            interfaz.Crear(this);
            ConfigurarVoz();
        }
        private void ConfigurarVoz()
        {
            comandos["detener"] = () => { jugador.Detener();interfaz.Notificar("Nos detenemos aquí. Mirá a tu alrededor."); };
            comandos["ayuda"] = AlternarAyuda;
            comandos["cerrar ayuda"] = () => EstablecerAyuda(false);
            comandos["correr"] = jugador.Correr;
            comandos["caminar"] = jugador.Caminar;
            comandos["avanzar"] = () => jugador.MoverPorVoz(1);
            comandos["retroceder"] = () => jugador.MoverPorVoz(-1);
            comandos["mirar izquierda"] = () => jugador.Girar(-45);
            comandos["mirar derecha"] = () => jugador.Girar(45);
            comandos["mirar atras"] = () => jugador.Girar(180);
            comandos["interactuar"] = Interactuar;
            comandos["buscar"] = Interactuar;
            comandos["entrar"] = Interactuar;
            comandos["pausa"] = () => EstablecerPausa(true);
            comandos["reanudar"] = () => EstablecerPausa(false);
            if (busqueda)
            {
                comandos["mapa"] = AlternarMapa;
                comandos["cerrar mapa"] = () => EstablecerMapa(false);
            }
            comandos["volver al lobby"] = () => Cargar("Lobby");
            comandos["menu principal"] = () => Cargar("Menus");
            comandos["repetir"] = () => { if (Partida != null && Partida.Finalizada) Cargar("BusquedaIgnis"); };
            foreach (var destino in destinos)
            {
                var local = destino;
                Action ir = () => {
                    if (!jugador.Bloqueado && !Cargando) jugador.IrA(local);
                };
                comandos["ir a " + destino.nombreVoz] = ir;
                comandos["ir al " + destino.nombreVoz] = ir;
                comandos["ir a la " + destino.nombreVoz] = ir;
                foreach (string alias in destino.aliasVoz ?? Array.Empty<string>())
                {
                    comandos["ir a " + alias] = ir; comandos["ir al " + alias] = ir; comandos["ir a la " + alias] = ir;
                }
            }
            // Vosk conserva tildes en su vocabulario. El normalizador compartido mantiene la misma acción.
            foreach (var par in new List<KeyValuePair<string, Action>>(comandos))
            {
                string acentuada = par.Key.Replace("busqueda", "búsqueda").Replace("arbol", "árbol").Replace("atras", "atrás").Replace("menu", "menú").Replace("jardin", "jardín").Replace("caido", "caído").Replace("tocon", "tocón");
                if (acentuada != par.Key) comandos[acentuada] = par.Value;
            }
            voz = gameObject.AddComponent<GestorVozMultiplataforma>();
            voz.Iniciar(comandos.Keys, resultado => {
                if (!Application.isFocused || Cargando || resultado.inicioUtc < ventanaEntrada) return;
                string frase = resultado.texto;
                bool permitida = !jugador.Bloqueado || frase == "reanudar" || frase == "volver al lobby" ||
                    frase == "menu principal" || frase == "repetir" || frase == "mapa" || frase == "cerrar mapa" || frase == "ayuda" || frase == "cerrar ayuda";
                if (permitida && comandos.TryGetValue(frase, out var accion)) accion();
            });
        }
        private void Update()
        {
            if (jugador == null || interfaz == null || Cargando) return;
            var teclado = Keyboard.current;
            if (teclado != null && teclado.hKey.wasPressedThisFrame) AlternarAyuda();
            if (teclado != null && teclado.mKey.wasPressedThisFrame) AlternarMapa();
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame && !(Partida != null && Partida.Finalizada))
            {
                if (interfaz.AyudaAbierta) EstablecerAyuda(false);
                else if (interfaz.MapaAbierto) EstablecerMapa(false);
                else EstablecerPausa(!Pausado);
            }
            if (Pausado || (Partida != null && Partida.Finalizada)) return;
            if (busqueda)
            {
                if (!Partida.Iniciada && interiorCobertizo != null && !interiorCobertizo.bounds.Contains(jugador.transform.position + Vector3.up))
                    Partida.Iniciar();
                bool cambio = Partida.Avanzar(Time.deltaTime, false, EnEncuadre, out bool pista);
                if (cambio) { ColocarIgnis(); CambioEscondite?.Invoke(); }
                if (pista) PistaDisponible?.Invoke(); // Se conectará al audio en la segunda etapa.
            }
            ActualizarObjetivo();
            if (teclado != null && teclado.eKey.wasPressedThisFrame) Interactuar();
            interfaz.Actualizar();
        }
        public bool EnEncuadre(int indice)
        {
            // Proyección geométrica: intencionalmente no consulta colliders ni Renderer.isVisible.
            Bounds volumen = new Bounds(escondites[indice].position + Vector3.up, new Vector3(1.8f, 2, 1.8f));
            return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(jugador.camara), volumen);
        }
        private void ColocarIgnis()
        {
            ignis.SetPositionAndRotation(escondites[Partida.Actual].position, escondites[Partida.Actual].rotation);
        }
        private void ActualizarObjetivo()
        {
            objetivo = null; mirandoIgnis = false;
            Ray rayo = jugador.camara.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            if (!Physics.Raycast(rayo, out var hit, 6.5f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            { interfaz.PistaInteraccion = ""; return; }
            float distancia = Vector3.Distance(jugador.transform.position, hit.point);
            mirandoIgnis = busqueda && Partida.Iniciada && hit.transform.IsChildOf(ignis);
            objetivo = hit.collider.GetComponentInParent<DestinoExploracion>();
            if (distancia > 3.3f)
            {
                interfaz.PistaInteraccion = mirandoIgnis || objetivo != null ? "Acercate para interactuar" : "";
                mirandoIgnis = false; objetivo = null; return;
            }
            interfaz.PistaInteraccion = mirandoIgnis ? "Encontrar a Ignis · E / Interactuar" :
                objetivo != null ? objetivo.descripcion + " · E / Interactuar" : "";
        }
        public void Interactuar()
        {
            if (jugador.Bloqueado || Cargando) return;
            ActualizarObjetivo(); // Nunca reutilizar una selección antigua después de mover la cámara.
            if (mirandoIgnis)
            {
                Partida.Finalizar(); jugador.Detener(); jugador.Bloqueado = true; jugador.SoltarCursor();
                float anterior = PlayerPrefs.GetFloat("BusquedaIgnis.MejorTiempo", float.MaxValue);
                if (Partida.Tiempo < anterior)
                { PlayerPrefs.SetFloat("BusquedaIgnis.MejorTiempo", (float)Partida.Tiempo); PlayerPrefs.Save(); }
                ventanaEntrada = DateTime.UtcNow; Hallazgo?.Invoke(); interfaz.MostrarResultado();
            }
            else if (objetivo != null)
            {
                if (!string.IsNullOrEmpty(objetivo.escenaDestino)) Cargar(objetivo.escenaDestino);
                else
                {
                    interfaz.Notificar(objetivo.descripcion);
                    DestinoInvestigado?.Invoke(objetivo);
                }
            }
        }
        public void EstablecerPausa(bool valor)
        {
            if (Cargando || Partida != null && Partida.Finalizada) return;
            interfaz.MostrarMapa(false);
            interfaz.MostrarAyuda(false);
            Pausado = valor; jugador.Bloqueado = valor; jugador.Detener(); jugador.SoltarCursor();
            Time.timeScale = valor ? 0 : 1; ventanaEntrada = DateTime.UtcNow;
            interfaz.MostrarPausa(valor);
        }
        public void AlternarMapa()
        {
            if (!busqueda || Pausado && !interfaz.MapaAbierto) return;
            EstablecerMapa(!interfaz.MapaAbierto);
        }
        public void AlternarAyuda()
        {
            if(Pausado&&!interfaz.AyudaAbierta)return;
            EstablecerAyuda(!interfaz.AyudaAbierta);
        }
        public void EstablecerAyuda(bool visible)
        {
            if(Cargando||Partida!=null&&Partida.Finalizada)return;
            if(visible&&Pausado&&!interfaz.AyudaAbierta)return;
            if(!visible&&!interfaz.AyudaAbierta)return;
            Pausado=visible;jugador.Bloqueado=visible;jugador.Detener();jugador.SoltarCursor();
            Time.timeScale=visible?0:1;ventanaEntrada=DateTime.UtcNow;interfaz.MostrarAyuda(visible);
        }
        public void EstablecerMapa(bool visible)
        {
            if (!busqueda || Cargando || Partida != null && Partida.Finalizada) return;
            if (!visible && !interfaz.MapaAbierto) return;
            if (visible && Pausado && !interfaz.MapaAbierto) return;
            Pausado = visible; jugador.Bloqueado = visible; jugador.Detener(); jugador.SoltarCursor();
            Time.timeScale = visible ? 0 : 1; ventanaEntrada = DateTime.UtcNow;
            interfaz.MostrarMapa(visible);
        }
        private void OnApplicationPause(bool pausa)
        {
            if (interfaz == null) return;
            if (pausa && !Pausado) { pausaPorAplicacion = true; EstablecerPausa(true); }
            else if (!pausa && pausaPorAplicacion) { pausaPorAplicacion = false; } // Reanudación explícita del jugador.
        }
        public void Cargar(string escena)
        {
            if (Cargando) return;
            Cargando = true; jugador.Bloqueado = true; jugador.SoltarCursor();
            voz?.Detener(); Time.timeScale = 1; SceneManager.LoadScene(escena);
        }
        private void OnDestroy() { voz?.Detener(); Time.timeScale = 1; }
    }
}
