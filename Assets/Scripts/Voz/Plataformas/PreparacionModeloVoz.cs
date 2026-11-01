using System;
using System.Collections;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
using UnityEngine.Networking;
#endif

namespace Pokimun.Voz
{
    internal static class PreparacionModeloVoz
    {
        public static IEnumerator Preparar(Action<string> terminado)
        {
            if (!PlataformaVoz.UsaVosk) { terminado(null); yield break; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                bool respondido = false;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => respondido = true;
                callbacks.PermissionDenied += _ => respondido = true;
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                float limite = Time.realtimeSinceStartup + 120;
                while (!respondido && Time.realtimeSinceStartup < limite) yield return null;
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    throw new InvalidOperationException("Sin permiso de micrófono. Puedes jugar con los botones táctiles y habilitarlo en los ajustes de Android.");
            }

            string origen = Application.streamingAssetsPath + "/vosk-model-small-es/";
            string destino = Path.Combine(Application.persistentDataPath, "vosk-model-small-es");
            using (var peticion = UnityWebRequest.Get(origen + "archivos.json"))
            {
                yield return peticion.SendWebRequest();
                if (peticion.result != UnityWebRequest.Result.Success)
                    throw new IOException("No se pudo leer el índice del modelo: " + peticion.error);
                var indice = JsonUtility.FromJson<Indice>(peticion.downloadHandler.text);
                if (indice == null || indice.archivos == null || indice.archivos.Length == 0)
                    throw new IOException("Índice del modelo vacío.");
                Directory.CreateDirectory(destino);
                foreach (Archivo archivo in indice.archivos)
                {
                    // El índice se genera al compilar, a partir de los archivos del modelo del proyecto.
                    string ruta = Path.GetFullPath(Path.Combine(destino, archivo.ruta));
                    if (!ruta.StartsWith(Path.GetFullPath(destino) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                        throw new IOException("Ruta no válida en el índice del modelo.");
                    string firma = ruta + ".sha256";
                    if (File.Exists(ruta) && new FileInfo(ruta).Length == archivo.bytes &&
                        File.Exists(firma) && File.ReadAllText(firma) == archivo.sha256) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                    string temporal = ruta + ".tmp";
                    using (var copia = UnityWebRequest.Get(origen + archivo.ruta))
                    {
                        copia.downloadHandler = new DownloadHandlerFile(temporal) { removeFileOnAbort = true };
                        yield return copia.SendWebRequest();
                        if (copia.result != UnityWebRequest.Result.Success)
                            throw new IOException("No se pudo extraer " + archivo.ruta + ": " + copia.error);
                    }
                    if (new FileInfo(temporal).Length != archivo.bytes)
                        throw new IOException("Modelo incompleto: " + archivo.ruta);
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    using (var datos = File.OpenRead(temporal))
                    {
                        string hash = BitConverter.ToString(sha.ComputeHash(datos)).Replace("-", "").ToLowerInvariant();
                        if (hash != archivo.sha256) throw new IOException("Modelo dañado: " + archivo.ruta);
                    }
                    if (File.Exists(ruta)) File.Delete(ruta);
                    File.Move(temporal, ruta);
                    File.WriteAllText(firma, archivo.sha256);
                }
            }
            terminado(destino);
#else
            string ruta = Path.Combine(Application.streamingAssetsPath, "vosk-model-small-es");
            if (!File.Exists(Path.Combine(ruta, "am", "final.mdl")))
                throw new DirectoryNotFoundException("Falta el modelo español de Vosk en " + ruta);
            terminado(ruta);
            yield break;
#endif
        }

        [Serializable] private class Indice { public Archivo[] archivos; }
        [Serializable] private class Archivo { public string ruta; public long bytes; public string sha256; }
    }
}
