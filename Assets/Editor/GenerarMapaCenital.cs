using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Pokimun.Exploracion;

public static class GenerarMapaCenital
{
    [MenuItem("Pokimun/Exploración/Generar imagen cenital del mapa")]
    public static void Generar()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Salí de Play Mode.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Guardá las escenas abiertas.");
        var escena=EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");
        var g=UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        var cobertizo=g.destinos.First(d=>d.nombreVoz=="cobertizo");
        cobertizo.mirada.position=new Vector3(g.interiorCobertizo.bounds.center.x,1,g.interiorCobertizo.bounds.center.z);
        EditorSceneManager.SaveScene(escena);
        var ocultos=new Dictionary<Renderer,bool>();
        foreach(var r in g.ignis.GetComponentsInChildren<Renderer>(true).Concat(g.jugador.GetComponentsInChildren<Renderer>(true)))ocultos[r]=r.enabled;
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Cartel de madera ")||t.name=="Nubes de atardecer"))
            foreach(var r in t.GetComponentsInChildren<Renderer>(true))ocultos[r]=r.enabled;
        var objeto=new GameObject("Cámara temporal de mapa");var camara=objeto.AddComponent<Camera>();camara.enabled=false;
        camara.transform.SetPositionAndRotation(new Vector3(0,85,2),Quaternion.Euler(90,0,0));
        camara.orthographic=true;camara.orthographicSize=20;camara.aspect=34f/40;
        camara.nearClipPlane=.1f;camara.farClipPlane=110;camara.clearFlags=CameraClearFlags.SolidColor;
        camara.backgroundColor=new Color(.13f,.2f,.12f);camara.cullingMask=~(1<<2);camara.allowHDR=false;camara.allowMSAA=false;
        var datos=camara.GetUniversalAdditionalCameraData();datos.renderPostProcessing=false;
        var textura=new RenderTexture(1020,1200,24,RenderTextureFormat.ARGB32);var anterior=RenderTexture.active;
        Texture2D png=null;
        try
        {
            foreach(var par in ocultos)par.Key.enabled=false;
            textura.Create();camara.targetTexture=textura;
            if(GraphicsSettings.currentRenderPipeline!=null)
                RenderPipeline.SubmitRenderRequest(camara,new UniversalRenderPipeline.SingleCameraRequest {destination=textura});
            else camara.Render();
            RenderTexture.active=textura;png=new Texture2D(1020,1200,TextureFormat.RGB24,false);
            png.ReadPixels(new Rect(0,0,1020,1200),0,0);png.Apply();
            const string ruta="Assets/Exploracion/Resources/Mapas/BosqueCenital.png";
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));File.WriteAllBytes(ruta,png.EncodeToPNG());
            AssetDatabase.ImportAsset(ruta,ImportAssetOptions.ForceSynchronousImport);
            var importador=(TextureImporter)AssetImporter.GetAtPath(ruta);
            importador.textureType=TextureImporterType.Default;importador.sRGBTexture=true;importador.mipmapEnabled=false;
            importador.maxTextureSize=2048;importador.textureCompression=TextureImporterCompression.Compressed;
            importador.wrapMode=TextureWrapMode.Clamp;importador.SaveAndReimport();
            Directory.CreateDirectory("Temp/Exploracion");
            File.WriteAllText("Temp/Exploracion/mapa-cenital.txt","OK: imagen 1020×1200 del escenario real, norte arriba, sin personajes, carteles ni nubes. Cobertizo corregido.");
        }
        finally
        {
            foreach(var par in ocultos)if(par.Key!=null)par.Key.enabled=par.Value;
            RenderTexture.active=anterior;camara.targetTexture=null;textura.Release();
            UnityEngine.Object.DestroyImmediate(textura);if(png!=null)UnityEngine.Object.DestroyImmediate(png);
            UnityEngine.Object.DestroyImmediate(objeto);
        }
    }
}
