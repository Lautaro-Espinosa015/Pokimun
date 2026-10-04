using System;
using System.Collections.Generic;

public enum JugadaRPS { Piedra, Papel, Tijera }
public enum AccionCombate { Atacar, Defender, Curar }
public enum ResultadoPartida { EnCurso, GanaJugador, GanaRival, Empate, Cancelada }

[Serializable]
public struct AccionTurno
{
    public AccionCombate tipo;
    public JugadaRPS jugada;
    public bool critico;

    public static AccionTurno Ataque(JugadaRPS jugada, bool critico = false) =>
        new AccionTurno { tipo = AccionCombate.Atacar, jugada = jugada, critico = critico };
        
    public static AccionTurno Curar() =>
        new AccionTurno { tipo = AccionCombate.Curar, jugada = JugadaRPS.Papel };

    public static AccionTurno Defensa() => new AccionTurno { tipo = AccionCombate.Defender };
}

[Serializable]
public class ResultadoIntercambio
{
    public int numero;
    public AccionTurno accion;
    public bool bloqueoAplicado;
    public int danioAplicado;
    public int curaAplicada;
    public int vidaAtacanteAntes;
    public int vidaDefensorAntes;
    public int vidaAtacanteDespues;
    public int vidaDefensorDespues;
}

[Serializable]
public class ResultadoRonda
{
    public int ronda;
    public ResultadoIntercambio[] intercambios;
    public ResultadoPartida resultado;
    public string motivo;
    public int bloqueosRestantesAtacante;
    public int bloqueosRestantesDefensor;
}

/// <summary>Reglas de combate por turnos alternos.</summary>
public static class ReglasCombate
{
    public const int AccionesPorRonda = 2;

    public static bool EsValida(AccionTurno accion) => Enum.IsDefined(typeof(AccionCombate), accion.tipo);

    public static bool PuedeAgregar(IReadOnlyList<AccionTurno> plan, AccionTurno accion, int rondaActual, out string motivo)
    {
        motivo = "";
        if (!EsValida(accion)) motivo = "Acción no válida.";
        else if (plan.Count >= AccionesPorRonda) motivo = "Ya elegiste tus dos acciones.";
        else if (accion.tipo == AccionCombate.Defender)
        {
            if (rondaActual == 1) motivo = "Los escudos están prohibidos en la primera ronda.";
            else
            {
                for (int i = 0; i < plan.Count; i++)
                    if (plan[i].tipo == AccionCombate.Defender)
                        motivo = "Solo puedes usar un escudo por turno.";
            }
        }
        return motivo.Length == 0;
    }

    public static bool PlanValido(AccionTurno[] plan, int rondaActual)
    {
        if (plan == null || plan.Length != AccionesPorRonda) return false;
        var acumuladas = new List<AccionTurno>();
        foreach (AccionTurno accion in plan)
        {
            if (!PuedeAgregar(acumuladas, accion, rondaActual, out _)) return false;
            acumuladas.Add(accion);
        }
        return true;
    }

    public static ResultadoRonda ResolverTurno(int ronda, int maxRondas, AccionTurno[] planAtacante, 
        int vidaAtacante, int vidaDefensor, int vidaMaxAtacante, int bloqueosAtacante, int bloqueosDefensor, 
        int danioBase, float multiplicadorCritico, float reduccionDefensa, bool atacanteEsJugador)
    {
        if (!PlanValido(planAtacante, ronda)) throw new ArgumentException("El plan es inválido o incluye escudos no permitidos.");

        var resultados = new List<ResultadoIntercambio>(AccionesPorRonda);
        
        for (int i = 0; i < AccionesPorRonda; i++)
        {
            var accion = planAtacante[i];
            var intercambio = new ResultadoIntercambio {
                numero = i + 1, accion = accion,
                vidaAtacanteAntes = vidaAtacante, vidaDefensorAntes = vidaDefensor
            };

            if (accion.tipo == AccionCombate.Atacar)
            {
                int danio = accion.critico ? Redondear(danioBase * multiplicadorCritico) : danioBase;
                if (bloqueosDefensor > 0)
                {
                    intercambio.bloqueoAplicado = true;
                    bloqueosDefensor--;
                    danio = Redondear(danio * (1d - reduccionDefensa));
                }
                intercambio.danioAplicado = Math.Min(vidaDefensor, danio);
                vidaDefensor -= intercambio.danioAplicado;
            }
            else if (accion.tipo == AccionCombate.Curar)
            {
                intercambio.curaAplicada = 7; // Cura fija del 7% (7 HP)
                vidaAtacante = Math.Min(vidaMaxAtacante, vidaAtacante + intercambio.curaAplicada);
            }
            else if (accion.tipo == AccionCombate.Defender)
            {
                // El escudo protege contra 2 ataques del rival.
                bloqueosAtacante = 2;
            }

            intercambio.vidaAtacanteDespues = vidaAtacante;
            intercambio.vidaDefensorDespues = vidaDefensor;
            resultados.Add(intercambio);
            if (vidaAtacante == 0 || vidaDefensor == 0) break;
        }

        bool knockout = vidaAtacante == 0 || vidaDefensor == 0;
        bool limite = ronda >= maxRondas && !knockout;
        ResultadoPartida resultado = ResultadoPartida.EnCurso;
        if (knockout || limite)
        {
            if (vidaAtacante == vidaDefensor) resultado = ResultadoPartida.Empate;
            else if (vidaAtacante > vidaDefensor) resultado = atacanteEsJugador ? ResultadoPartida.GanaJugador : ResultadoPartida.GanaRival;
            else resultado = atacanteEsJugador ? ResultadoPartida.GanaRival : ResultadoPartida.GanaJugador;
        }

        return new ResultadoRonda {
            ronda = ronda, intercambios = resultados.ToArray(), resultado = resultado,
            motivo = knockout ? "Debilitamiento" : limite ? "Límite de rondas" : "",
            bloqueosRestantesAtacante = bloqueosAtacante,
            bloqueosRestantesDefensor = bloqueosDefensor
        };
    }

    private static int Redondear(double valor) => (int)Math.Min(int.MaxValue, Math.Round(valor, MidpointRounding.AwayFromZero));

    public static string Nombre(AccionTurno accion, bool esIgnis = false) => accion.tipo == AccionCombate.Defender ? "ESCUDO" :
        accion.tipo == AccionCombate.Curar ? "SANAR" : (esIgnis ? "PIRO PULSO" : "HIDRO PULSO");

    public static string Resumen(AccionTurno accion, bool esIgnis = false) =>
        Nombre(accion, esIgnis) + (accion.critico ? " (CRÍTICO)" : "");
}
