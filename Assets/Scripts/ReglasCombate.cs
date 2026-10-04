using System;
using System.Collections.Generic;

public enum JugadaRPS { Piedra, Papel, Tijera }
public enum AccionCombate { Atacar, Defender }
public enum ResultadoPartida { EnCurso, GanaJugador, GanaRival, Empate, Cancelada }

[Serializable]
public struct AccionTurno
{
    public AccionCombate tipo;
    public JugadaRPS jugada;
    public bool critico;

    public static AccionTurno Ataque(JugadaRPS jugada, bool critico = false) =>
        new AccionTurno { tipo = AccionCombate.Atacar, jugada = jugada, critico = critico };

    public static AccionTurno Defensa() => new AccionTurno { tipo = AccionCombate.Defender };
}

[Serializable]
public class ResultadoIntercambio
{
    public int pareja;
    public AccionTurno jugador;
    public AccionTurno rival;
    // 0 = empate/ambos defienden, 1 = ataca Hydros, -1 = ataca Ignis.
    public int ganador;
    public bool bloqueo;
    public bool critico;
    public int danioSinDefensa;
    public int danioCalculado;
    public int danioAplicado;
    public int vidaJugadorAntes;
    public int vidaRivalAntes;
    public int vidaJugadorDespues;
    public int vidaRivalDespues;
}

[Serializable]
public class ResultadoRonda
{
    public int ronda;
    public ResultadoIntercambio[] intercambios;
    public ResultadoPartida resultado;
    public string motivo;
}

/// <summary>Reglas deterministas. No leen entradas, animaciones, red ni objetos de la escena.</summary>
public static class ReglasCombate
{
    public const int AccionesPorRonda = 2;

    public static bool EsValida(AccionTurno accion) =>
        Enum.IsDefined(typeof(AccionCombate), accion.tipo) &&
        Enum.IsDefined(typeof(JugadaRPS), accion.jugada) &&
        (accion.tipo != AccionCombate.Defender || !accion.critico);

    public static bool PuedeAgregar(IReadOnlyList<AccionTurno> plan, AccionTurno accion, out string motivo)
    {
        motivo = "";
        if (!EsValida(accion)) motivo = "Acción no válida.";
        else if (plan.Count >= AccionesPorRonda) motivo = "Ya elegiste tus dos acciones.";
        else if (accion.tipo == AccionCombate.Defender)
        {
            for (int i = 0; i < plan.Count; i++)
                if (plan[i].tipo == AccionCombate.Defender)
                    motivo = "Solo puedes usar un escudo por ronda. Elige un ataque.";
        }
        return motivo.Length == 0;
    }

    public static bool PlanValido(AccionTurno[] plan)
    {
        if (plan == null || plan.Length != AccionesPorRonda) return false;
        var acumuladas = new List<AccionTurno>();
        foreach (AccionTurno accion in plan)
        {
            if (!PuedeAgregar(acumuladas, accion, out _)) return false;
            acumuladas.Add(accion);
        }
        return true;
    }

    // 1: gana a; -1: gana b; 0: empate.
    public static int Comparar(JugadaRPS a, JugadaRPS b)
    {
        if (!Enum.IsDefined(typeof(JugadaRPS), a) || !Enum.IsDefined(typeof(JugadaRPS), b))
            throw new ArgumentOutOfRangeException(nameof(a));
        if (a == b) return 0;
        return (a == JugadaRPS.Piedra && b == JugadaRPS.Tijera) ||
               (a == JugadaRPS.Papel && b == JugadaRPS.Piedra) ||
               (a == JugadaRPS.Tijera && b == JugadaRPS.Papel) ? 1 : -1;
    }

    public static ResultadoRonda Resolver(int ronda, int maxRondas, AccionTurno[] jugador,
        AccionTurno[] rival, int vidaJugador, int vidaRival, int danioBase,
        float multiplicadorCritico, float reduccionDefensa)
    {
        if (!PlanValido(jugador) || !PlanValido(rival)) throw new ArgumentException("Cada plan requiere dos acciones y como máximo un escudo.");
        if (ronda < 1 || maxRondas < ronda || vidaJugador <= 0 || vidaRival <= 0 || danioBase < 0 ||
            float.IsNaN(multiplicadorCritico) || float.IsInfinity(multiplicadorCritico) || multiplicadorCritico < 1f ||
            float.IsNaN(reduccionDefensa) || reduccionDefensa < 0f || reduccionDefensa > 1f)
            throw new ArgumentOutOfRangeException(nameof(ronda), "Configuración de combate inválida.");

        var resultados = new List<ResultadoIntercambio>(AccionesPorRonda);
        for (int i = 0; i < AccionesPorRonda; i++)
        {
            var a = jugador[i];
            var b = rival[i];
            var intercambio = new ResultadoIntercambio {
                pareja = i + 1, jugador = a, rival = b,
                vidaJugadorAntes = vidaJugador, vidaRivalAntes = vidaRival
            };
            if (a.tipo == AccionCombate.Atacar && b.tipo == AccionCombate.Atacar)
                intercambio.ganador = Comparar(a.jugada, b.jugada);
            else if (a.tipo != b.tipo)
            {
                intercambio.ganador = a.tipo == AccionCombate.Atacar ? 1 : -1;
                intercambio.bloqueo = true;
            }

            if (intercambio.ganador != 0)
            {
                intercambio.critico = intercambio.ganador == 1 ? a.critico : b.critico;
                // Redondeo único y explícito para que host y cliente obtengan lo mismo.
                intercambio.danioSinDefensa = Redondear(danioBase * (intercambio.critico ? (double)multiplicadorCritico : 1d));
                intercambio.danioCalculado = intercambio.bloqueo
                    ? Redondear(intercambio.danioSinDefensa * (1d - reduccionDefensa))
                    : intercambio.danioSinDefensa;
                int vidaObjetivo = intercambio.ganador == 1 ? vidaRival : vidaJugador;
                intercambio.danioAplicado = Math.Min(vidaObjetivo, intercambio.danioCalculado);
                if (intercambio.ganador == 1) vidaRival -= intercambio.danioAplicado;
                else vidaJugador -= intercambio.danioAplicado;
            }
            intercambio.vidaJugadorDespues = vidaJugador;
            intercambio.vidaRivalDespues = vidaRival;
            resultados.Add(intercambio);
            if (vidaJugador == 0 || vidaRival == 0) break;
        }

        bool knockout = vidaJugador == 0 || vidaRival == 0;
        bool limite = ronda == maxRondas;
        ResultadoPartida resultado = ResultadoPartida.EnCurso;
        if (knockout || limite)
            resultado = vidaJugador == vidaRival ? ResultadoPartida.Empate :
                vidaJugador > vidaRival ? ResultadoPartida.GanaJugador : ResultadoPartida.GanaRival;
        return new ResultadoRonda {
            ronda = ronda, intercambios = resultados.ToArray(), resultado = resultado,
            motivo = knockout ? "Debilitamiento" : limite ? "Límite de rondas" : ""
        };
    }

    private static int Redondear(double valor) => (int)Math.Min(int.MaxValue, Math.Round(valor, MidpointRounding.AwayFromZero));

    public static string Nombre(AccionTurno accion) => accion.tipo == AccionCombate.Defender ? "ESCUDO" :
        accion.jugada == JugadaRPS.Piedra ? "ROCA" : accion.jugada == JugadaRPS.Papel ? "HOJA" : "HIDRO PULSO";

    public static string Resumen(AccionTurno accion) =>
        Nombre(accion) + (accion.critico ? " ★" : "");
}
