using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pokimun.Voz
{
    /// <summary>Relaciona el texto reconocido con una orden completa, nunca con subcadenas.</summary>
    public sealed class ComandosVoz
    {
        private readonly Dictionary<string, string> comandos = new Dictionary<string, string>();
        private readonly List<string> vocabulario = new List<string>();
        public string[] PalabrasClave => vocabulario.ToArray();

        public ComandosVoz(IEnumerable<string> palabras)
        {
            if (palabras == null) throw new ArgumentNullException(nameof(palabras));
            var unicas = new HashSet<string>(StringComparer.Ordinal);
            foreach (string palabra in palabras)
            {
                if (string.IsNullOrWhiteSpace(palabra)) continue;
                string original = palabra.Trim().ToLowerInvariant();
                string normalizada = Normalizar(original);
                if (normalizada.Length == 0) continue;
                if (!comandos.ContainsKey(normalizada)) comandos.Add(normalizada, original);
                if (unicas.Add(original)) vocabulario.Add(original);
            }
        }

        public bool IntentarReconocer(string texto, out string comando) =>
            comandos.TryGetValue(Normalizar(texto), out comando);

        public static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var salida = new StringBuilder();
            bool espacioPendiente = false;
            foreach (char c in texto.Normalize(NormalizationForm.FormD).ToLowerInvariant())
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c))
                {
                    if (espacioPendiente && salida.Length > 0) salida.Append(' ');
                    salida.Append(c);
                    espacioPendiente = false;
                }
                else espacioPendiente = true;
            }
            return salida.ToString().Normalize(NormalizationForm.FormC);
        }

        public string CrearGramatica()
        {
            var json = new StringBuilder("[");
            foreach (string palabra in vocabulario)
            {
                if (json.Length > 1) json.Append(',');
                json.Append('"').Append(palabra.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
            }
            if (json.Length > 1) json.Append(',');
            // Permite rechazar ruido en lugar de forzar siempre una orden conocida.
            return json.Append("\"[unk]\"]").ToString();
        }
    }
}
