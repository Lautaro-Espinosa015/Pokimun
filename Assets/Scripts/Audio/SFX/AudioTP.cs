using System.Collections.Generic;
using UnityEngine;

namespace Pokimun.Audio
{
    /// <summary>
    /// Utilidades para postear eventos de Wwise por nombre (los SFX del TP).
    /// Con SoundBanks auto-definidos, cada evento tiene su propio banco: antes del
    /// primer uso se carga el banco del evento y se "prepara" (igual que hace la
    /// integración con los AK.Wwise.Event elegidos en el Inspector).
    /// También centraliza el control de voces para que nunca se pisen.
    /// </summary>
    public static class AudioTP
    {
        private static readonly HashSet<string> cargados = new HashSet<string>();
        private static float vozOcupadaHasta;

        public static void Cargar(string evento)
        {
            if (string.IsNullOrEmpty(evento) || cargados.Contains(evento)) return;
            if (!AkUnitySoundEngine.IsInitialized()) return;
            AkBankManager.LoadBank(evento, false, false, AkBankTypeEnum.AkBankType_Event);
            AkUnitySoundEngine.PrepareEvent(AkPreparationType.Preparation_Load, new[] { evento }, 1);
            cargados.Add(evento);
        }

        public static uint Post(string evento, GameObject emisor)
        {
            if (string.IsNullOrEmpty(evento) || emisor == null || !AkUnitySoundEngine.IsInitialized()) return 0;
            Cargar(evento);
            if (emisor.GetComponent<AkGameObj>() == null) emisor.AddComponent<AkGameObj>();
            return AkUnitySoundEngine.PostEvent(evento, emisor);
        }

        /// <summary>Detiene el evento en ese emisor con un fundido (milisegundos).</summary>
        public static void Detener(string evento, GameObject emisor, int fundidoMs = 400)
        {
            if (string.IsNullOrEmpty(evento) || emisor == null || !AkUnitySoundEngine.IsInitialized()) return;
            AkUnitySoundEngine.ExecuteActionOnEvent(evento, AkActionOnEventType.AkActionOnEventType_Stop, emisor,
                fundidoMs, AkCurveInterpolation.AkCurveInterpolation_Linear);
        }

        // ---------- Voces: una sola a la vez, con un pequeño respiro entre frases ----------
        public static bool VozLibre => Time.unscaledTime >= vozOcupadaHasta;

        /// <summary>Reproduce una voz si no hay otra sonando (o si se fuerza).</summary>
        public static bool Voz(string evento, GameObject emisor, float duracion, bool forzar = false)
        {
            if (!forzar && !VozLibre) return false;
            if (Post(evento, emisor) == 0) return false;
            vozOcupadaHasta = Time.unscaledTime + duracion + 0.6f;
            return true;
        }
    }
}
