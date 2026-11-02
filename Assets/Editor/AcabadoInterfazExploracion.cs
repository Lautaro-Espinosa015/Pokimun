using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Pokimun.Exploracion;

public static class AcabadoInterfazExploracion
{
    [MenuItem("Pokimun/Exploración/Aplicar estética de combate y placas de piedra")]
    public static void Aplicar()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Salí de Play Mode.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Guardá las escenas abiertas.");
        string copia=".local-codex/Backups/Interfaz-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(copia);
        foreach(string nombre in new[]{"Lobby","BusquedaIgnis"})File.Copy("Assets/Scenes/"+nombre+".unity",copia+"/"+nombre+".unity");
        Directory.CreateDirectory("Assets/Exploracion/Resources/Exploracion");AssetDatabase.Refresh();
        const string ruta="Assets/Exploracion/Resources/Exploracion/TemaCombate.asset";
        var tema=AssetDatabase.LoadAssetAtPath<TemaExploracion>(ruta);
        if(tema==null){tema=ScriptableObject.CreateInstance<TemaExploracion>();AssetDatabase.CreateAsset(tema,ruta);}
        tema.fuente=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/LuckiestGuy-Regular SDF.asset");
        tema.boton=AssetDatabase.LoadAllAssetsAtPath("Assets/Prefabs/Menus/Image/Boton1.png").OfType<Sprite>().Single(s=>{
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string guid,out long id);return id==3839355700136853231;
        });
        if(tema.fuente==null||tema.boton==null)throw new Exception("Faltan los recursos visuales originales del combate.");
        EditorUtility.SetDirty(tema);AssetDatabase.SaveAssets();
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Exploracion/Zonas/Placa de piedra.mat");
        if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Exploracion/Mejoras/Caliza envejecida.mat"));AssetDatabase.CreateAsset(material,"Assets/Exploracion/Zonas/Placa de piedra.mat");}
        material.color=new Color(.6f,.56f,.45f);material.mainTextureScale=new Vector2(2,1);material.SetFloat("_Smoothness",.02f);EditorUtility.SetDirty(material);
        int eliminados=0;
        foreach(string nombre in new[]{"Lobby","BusquedaIgnis"})
        {
            var escena=EditorSceneManager.OpenScene("Assets/Scenes/"+nombre+".unity");
            var carteles=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(t=>t.name.StartsWith("Cartel de madera ")||t.name.StartsWith("Cartel ")).ToArray();
            foreach(var cartel in carteles)if(cartel!=null){UnityEngine.Object.DestroyImmediate(cartel.gameObject);eliminados++;}
            var anterior=GameObject.Find("Placas de piedra del santuario");if(anterior!=null)UnityEngine.Object.DestroyImmediate(anterior);
            if(nombre=="Lobby")
            {
                var raiz=new GameObject("Placas de piedra del santuario").transform;
                Placa(raiz,"COMBATE",new Vector3(6,4.65f,14.58f),0,material,tema.fuente);
                Placa(raiz,"BÚSQUEDA",new Vector3(-6,4.65f,14.58f),0,material,tema.fuente);
                Placa(raiz,"JARDÍN AZUL",new Vector3(-12.58f,2.6f,-3),-90,material,tema.fuente);
                Placa(raiz,"ROSAL ROJO",new Vector3(12.58f,2.6f,-3),90,material,tema.fuente);
                Placa(raiz,"JARDÍN DEL FONDO",new Vector3(0,2.6f,14.58f),0,material,tema.fuente);
                ColoresLobby();
            }
            EditorSceneManager.SaveScene(escena);
        }
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/acabado-ui.txt","OK: recursos originales de combate enlazados; carteles retirados="+eliminados+"; lobby con cinco placas; escondite sin carteles físicos.");
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
    }
    [MenuItem("Pokimun/Exploración/Alinear portales y colores de personajes")]
    public static void AjustarColoresLobby()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Salí de Play Mode.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Guardá las escenas abiertas.");
        string copia=".local-codex/Backups/Colores-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(copia);
        File.Copy("Assets/Scenes/Lobby.unity",copia+"/Lobby.unity");
        var escena=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        ColoresLobby();EditorSceneManager.SaveScene(escena);
        File.WriteAllText("Temp/Exploracion/colores-lobby.txt","OK: búsqueda azul a la izquierda; combate lava a la derecha; placas de Hydros moradas y jardín azul; llegadas y miradas alineadas.");
    }
    private static void ColoresLobby()
    {
        foreach(var d in UnityEngine.Object.FindObjectsByType<DestinoExploracion>(FindObjectsSortMode.None))
        {
            if(d.escenaDestino!="Escenario"&&d.escenaDestino!="BusquedaIgnis")continue;
            float x=d.escenaDestino=="BusquedaIgnis"?-6:6;
            Vector3 delta=new Vector3(x-d.transform.position.x,0,0);
            if(d.mirada!=null&&!d.mirada.IsChildOf(d.transform))d.mirada.position+=delta;
            if(d.llegada!=null&&!d.llegada.IsChildOf(d.transform))d.llegada.position+=delta;
            d.transform.position+=delta;
            if(d.PuntoLlegada.x*x<=0||d.mirada==null||d.mirada.position.x*x<=0)
                throw new Exception("El destino del portal no coincide con su lado: "+d.nombreVoz);
        }
        var luces=GameObject.Find("Iluminación del santuario");
        if(luces!=null)foreach(var luz in luces.GetComponentsInChildren<Light>(true))
        {
            if(!luz.name.StartsWith("Portal "))continue;
            var p=luz.transform.position;p.x=luz.name.Contains("combate")?6:-6;luz.transform.position=p;
        }
        var placas=GameObject.Find("Placas de piedra del santuario");
        if(placas!=null)foreach(Transform placa in placas.transform)
        {
            var p=placa.position;
            if(placa.name=="Placa COMBATE")p.x=6;
            if(placa.name=="Placa BÚSQUEDA")p.x=-6;
            placa.position=p;
            var texto=placa.GetComponentInChildren<TMP_Text>();
            if(placa.name=="Placa BÚSQUEDA")texto.color=new Color(.6f,.3f,1);
            if(placa.name=="Placa JARDÍN AZUL")texto.color=new Color(.35f,.72f,1);
            if(placa.name=="Placa COMBATE"||placa.name=="Placa ROSAL ROJO")texto.color=new Color(1,.55f,.25f);
        }
    }
    private static void Placa(Transform padre,string nombre,Vector3 p,float yaw,Material material,TMP_FontAsset fuente)
    {
        var raiz=new GameObject("Placa "+nombre).transform;raiz.SetParent(padre,false);raiz.position=p;raiz.rotation=Quaternion.Euler(0,yaw,0);
        foreach(bool marco in new[]{true,false})
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=marco?"Marco de piedra":"Piedra incrustada";o.transform.SetParent(raiz,false);
            o.transform.localPosition=new Vector3(0,0,marco?0:-.07f);o.transform.localScale=marco?new Vector3(4.2f,1,.12f):new Vector3(4,.8f,.04f);
            o.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        }
        var texto=new GameObject("Inscripción",typeof(TextMeshPro));texto.transform.SetParent(raiz,false);texto.transform.localPosition=new Vector3(0,0,-.096f);
        var t=texto.GetComponent<TextMeshPro>();t.font=fuente;t.text=nombre;t.fontSize=4.5f;t.enableAutoSizing=true;t.fontSizeMin=2.7f;t.fontSizeMax=4.5f;
        t.rectTransform.sizeDelta=new Vector2(3.8f,.7f);t.alignment=TextAlignmentOptions.Center;t.color=new Color(.98f,.9f,.65f);t.raycastTarget=false;
    }
    public static void Probar(GestorExploracion g)
    {
        g.GetComponent<GestorVozMultiplataforma>()?.Detener();if(g.Pausado)g.EstablecerPausa(false);
        bool tactil=(g.interfaz.GetComponentsInChildren<EntradaTactilExploracion>(true).Length>0)==TemaExploracion.EsAndroid;
        bool sinLista=!g.interfaz.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text.Contains("WASD"));
        bool estilos=TemaExploracion.Actual!=null&&g.interfaz.GetComponentsInChildren<TMP_Text>().All(t=>t.font==TemaExploracion.Actual.fuente);
        bool abrir=false,cerrar=false,volumen=false,resultado=true;int etapa=0;
        var teclado=UnityEngine.InputSystem.Keyboard.current;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.H));
        double fin=EditorApplication.timeSinceStartup+.3;
        EditorApplication.CallbackFunction revisar=null;
        revisar=()=>{
            if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=revisar;return;}
            if(EditorApplication.timeSinceStartup<fin)return;fin=EditorApplication.timeSinceStartup+.3;
            try
            {
                if(etapa==0){abrir=g.Pausado&&g.interfaz.AyudaAbierta&&Time.timeScale==0;UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState());}
                if(etapa==1)UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
                if(etapa==2){cerrar=!g.Pausado&&!g.interfaz.AyudaAbierta&&Time.timeScale==1;UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState());g.EstablecerPausa(true);}
                if(etapa==3)
                {
                    volumen=g.interfaz.GetComponentsInChildren<UnityEngine.UI.Slider>().Length==2;
                    estilos&=g.interfaz.GetComponentsInChildren<UnityEngine.UI.Button>().All(b=>b.GetComponent<UnityEngine.UI.Image>().sprite==TemaExploracion.Actual.boton);
                    g.EstablecerPausa(false);
                }
                if(etapa==4&&g.busqueda)g.interfaz.MostrarResultado();
                if(etapa==5)
                {
                    resultado=g.interfaz.GetComponentsInChildren<TMP_Text>().All(t=>t.font==TemaExploracion.Actual.fuente);
                    g.EstablecerPausa(false);
                    bool ok=tactil&&sinLista&&estilos&&abrir&&cerrar&&volumen&&resultado;
                    File.WriteAllText("Temp/Exploracion/prueba-tema-ui.txt",(ok?"OK":"ERROR")+": táctil por plataforma="+tactil+" sin lista permanente="+sinLista+" recursos combate="+estilos+" H abre ayuda="+abrir+" Escape cierra="+cerrar+" volumen="+volumen+" fuente resultados="+resultado);
                    EditorApplication.update-=revisar;
                }
                etapa++;
            }
            catch(Exception e){File.WriteAllText("Temp/Exploracion/prueba-tema-ui.txt","ERROR "+e);g.EstablecerPausa(false);EditorApplication.update-=revisar;}
        };EditorApplication.update+=revisar;
    }
}
