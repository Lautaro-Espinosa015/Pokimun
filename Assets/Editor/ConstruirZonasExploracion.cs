using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using TMPro;
using Pokimun.Exploracion;

/// <summary>Referencias reales, señalética de dos caras y assets botánicos propios.</summary>
public static class ConstruirZonasExploracion
{
    const string Carpeta = "Assets/Exploracion/Zonas";
    static Material madera, corteza, corte, verde, azul, celeste, rojo, centro, piedra;
    static Mesh flor, rosa;
    static readonly string[] Nombres = { "pino solitario", "tronco caido", "rocas partidas", "arbustos", "ruinas", "claro", "jardin de flores", "roca alta", "tocon" };

    [MenuItem("Pokimun/Exploración/Construir zonas y carteles coherentes")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Salí de Play Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Guardá las escenas abiertas.");
        string backup = ".local-codex/Backups/Zonas-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(backup);
        foreach (string s in new[] { "Lobby", "BusquedaIgnis" }) File.Copy("Assets/Scenes/" + s + ".unity", backup + "/" + s + ".unity");
        Directory.CreateDirectory(Carpeta); AssetDatabase.Refresh(); Recursos();
        Lobby(); Busqueda(); AssetDatabase.SaveAssets();
        File.WriteAllText(Carpeta + "/Procedencia.md", "# Assets de zonas de exploración\n\nFlores de cinco pétalos, rosas de pétalos en capas, troncos, tocones y carteles: modelos procedurales propios generados para Pokimun. El código generador se conserva en Assets/Editor/ConstruirZonasExploracion.cs. No incorporan imágenes ni modelos descargados.\n\nSe revisó Nature Kit de Kenney (https://kenney.nl/assets/nature-kit), publicado como CC0; la descarga no fue utilizable y no se incorporaron archivos de ese paquete. Se reutilizan los pinos, rocas y materiales de madera ya presentes en el proyecto.\n");
        AssetDatabase.Refresh(); Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/zonas.txt", "OK: nueve zonas, nueve escondites exclusivos, carteles de dos caras y jardines azul/celeste y rosal rojo. Rutas horneadas.");
        EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
    }
    static Material Mat(string nombre, Color color)
    {
        string ruta = Carpeta + "/" + nombre + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, ruta); }
        m.color = color; m.SetFloat("_Smoothness", .03f); m.SetFloat("_Cull", 0); m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
    }
    static void Recursos()
    {
        madera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Exploracion/Mejoras/Tablas cobertizo CC0.mat");
        corteza = Mat("Corteza", new Color(.24f,.12f,.055f)); corte = Mat("Madera cortada", new Color(.67f,.43f,.21f));
        verde = Mat("Tallos y hojas", new Color(.12f,.39f,.065f)); azul = Mat("Petalos azules", new Color(.08f,.27f,.95f));
        celeste = Mat("Petalos celestes", new Color(.15f,.76f,1)); rojo = Mat("Petalos de rosa", new Color(.78f,.025f,.065f));
        centro = Mat("Polen", new Color(1,.65f,.08f)); piedra = Mat("Piedra de ruinas", new Color(.38f,.4f,.31f));
        flor = Flor(false); rosa = Flor(true);
    }
    // Tres submallas: follaje, pétalos y centro. Pétalos curvos con volumen y bordes levantados.
    static Mesh Flor(bool esRosa)
    {
        string ruta = Carpeta + (esRosa ? "/Rosa.asset" : "/Flor azul.asset"); var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        var v = new List<Vector3>(); var caras = new[] { new List<int>(), new List<int>(), new List<int>() };
        Action<int,Vector3,Vector3,Vector3> tri = (k,a,b,c) => { foreach (var p in new[] {a,b,c}) { caras[k].Add(v.Count); v.Add(p); } };
        // Tallo octogonal.
        for (int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4, b=(i+1)*Mathf.PI/4; var p=new Vector3(Mathf.Cos(a)*.018f,0,Mathf.Sin(a)*.018f); var q=new Vector3(Mathf.Cos(b)*.018f,0,Mathf.Sin(b)*.018f);
            tri(0,p,q,q+Vector3.up*.72f); tri(0,p,q+Vector3.up*.72f,p+Vector3.up*.72f);
        }
        for(int lado=-1;lado<=1;lado+=2)
        {
            var baseHoja=new Vector3(0,.25f+(lado+1)*.09f,0); var punta=baseHoja+new Vector3(lado*.25f,.13f,.03f);
            tri(0,baseHoja,baseHoja+new Vector3(lado*.12f,.02f,.09f),punta); tri(0,baseHoja,punta,baseHoja+new Vector3(lado*.12f,.02f,-.09f));
        }
        int capas=esRosa?4:1;
        for(int capa=0;capa<capas;capa++)
        {
            int petalos=esRosa?7:5; float radio=esRosa?.22f-capa*.045f:.24f;
            for(int i=0;i<petalos;i++)
            {
                float a=(i+(capa%2)*.5f)*Mathf.PI*2/petalos; var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)); var tangente=new Vector3(-radial.z,0,radial.x);
                var basePetalo=new Vector3(0,.72f+capa*.035f,0); var medio=basePetalo+radial*radio*.65f+Vector3.down*.025f;
                var punta=basePetalo+radial*radio+Vector3.up*(esRosa?.07f:.025f);
                tri(1,basePetalo,punta,medio-tangente*radio*.65f); tri(1,basePetalo,medio+tangente*radio*.65f,punta);
            }
        }
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI/5,b=(i+1)*Mathf.PI/5;
            tri(esRosa?1:2,new Vector3(0,esRosa?.87f:.745f,0), new Vector3(Mathf.Cos(a)*.05f,.735f,Mathf.Sin(a)*.05f),new Vector3(Mathf.Cos(b)*.05f,.735f,Mathf.Sin(b)*.05f));
        }
        var m = existente;
        if(m==null) m=new Mesh { name=esRosa?"Rosa":"Flor azul" };
        m.Clear();m.SetVertices(v); m.subMeshCount=3;
        for(int i=0;i<3;i++) m.SetTriangles(caras[i],i); m.RecalculateNormals(); m.RecalculateBounds();
        if(existente==null)AssetDatabase.CreateAsset(m,ruta);else EditorUtility.SetDirty(m);return m;
    }
    static GameObject Objeto(string nombre, PrimitiveType tipo, Vector3 p, Vector3 escala, Material mat, Transform padre, bool col=true)
    {
        var o=GameObject.CreatePrimitive(tipo); o.name=nombre; o.transform.SetParent(padre,true); o.transform.position=p; o.transform.localScale=escala; o.GetComponent<Renderer>().sharedMaterial=mat;
        var c=o.GetComponent<Collider>(); if(!col && c!=null) UnityEngine.Object.DestroyImmediate(c); return o;
    }
    static void Plantar(Transform padre, Vector3 p, bool esRosa, int i)
    {
        var o=new GameObject(esRosa?"Rosa roja":"Flor azul o celeste",typeof(MeshFilter),typeof(MeshRenderer)); o.transform.SetParent(padre,true); o.transform.position=p;
        o.transform.rotation=Quaternion.Euler(0,i*137,0); o.transform.localScale=Vector3.one*(.8f+(i%4)*.11f);
        o.GetComponent<MeshFilter>().sharedMesh=esRosa?rosa:flor; o.GetComponent<MeshRenderer>().sharedMaterials=new[]{verde,esRosa?rojo:i%2==0?azul:celeste,centro};
    }
    static Transform Punto(string nombre,Vector3 p,Transform padre)
    { var o=new GameObject(nombre).transform; o.SetParent(padre,true); o.position=p; return o; }
    static void Cartel(DestinoExploracion d,Vector3 p,Transform padre)
    {
        var raiz=Punto("Cartel de madera "+d.nombreVoz,p,padre);
        Objeto("Poste",PrimitiveType.Cube,p+Vector3.up*.65f,new Vector3(.1f,1.3f,.1f),corteza,raiz,false);
        Objeto("Tablero",PrimitiveType.Cube,p+Vector3.up*1.4f,new Vector3(2.1f,.65f,.12f),madera,raiz,false);
        for(int lado=-1;lado<=1;lado+=2)
        {
            var o=new GameObject(lado<0?"Texto frontal":"Texto posterior",typeof(TextMeshPro)); o.transform.SetParent(raiz,false);
            o.transform.localPosition=new Vector3(0,1.4f,lado*.067f); o.transform.localRotation=Quaternion.Euler(0,lado<0?0:180,0);
            var t=o.GetComponent<TextMeshPro>(); t.text=Mostrar(d.nombreVoz).ToUpperInvariant(); t.fontSize=2.6f; t.enableAutoSizing=true; t.fontSizeMin=1.4f;t.fontSizeMax=2.6f;
            t.alignment=TextAlignmentOptions.Center; t.rectTransform.sizeDelta=new Vector2(1.95f,.56f); t.color=new Color(1,.94f,.74f); t.raycastTarget=false;
        }
    }
    static string Mostrar(string s) => s.Replace("caido","caído").Replace("jardin","jardín").Replace("tocon","tocón").Replace("busqueda","búsqueda");
    static void LimpiarCarteles()
    { foreach(var o in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) if(o.name.StartsWith("Cartel ")) UnityEngine.Object.DestroyImmediate(o); }
    static void Lobby()
    {
        var escena=EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity"); var g=UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        if(GameObject.Find("Señalética y jardines")!=null) { AjustarCarteles(g);Hornear(g);AjustarLlegadas(g);EditorSceneManager.SaveScene(escena);return; }
        LimpiarCarteles(); var raiz=new GameObject("Señalética y jardines").transform;
        foreach(var r in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Planta del santuario").ToArray()) UnityEngine.Object.DestroyImmediate(r.gameObject);
        var destinos=g.destinos.ToList();
        foreach(var d in destinos)
        {
            string anterior=d.nombreVoz;
            if(anterior=="hydros"||anterior=="ignis") { d.nombreVoz="estatua de "+anterior; d.aliasVoz=new[]{anterior}; }
            if(anterior=="combate"||anterior=="busqueda") { d.nombreVoz="portal de "+anterior; d.aliasVoz=new[]{anterior}; }
            if(anterior=="estanque de agua") d.aliasVoz=new[]{"agua"}; if(anterior=="estanque de lava") d.aliasVoz=new[]{"lava"};
            if(anterior=="jardin izquierdo") { d.nombreVoz="jardin azul"; d.transform.position=new Vector3(-10.2f,0,-3); }
            if(anterior=="jardin derecho") { d.nombreVoz="rosal rojo"; d.transform.position=new Vector3(10.2f,0,-3); }
            Vector3 mirar=d.transform.position+Vector3.up*.85f;
            if(d.nombreVoz=="jardin azul"||d.nombreVoz=="rosal rojo") mirar=new Vector3(d.nombreVoz=="jardin azul"?-11.8f:11.8f,.9f,-3);
            d.mirada=Punto("Mirada "+d.nombreVoz,mirar,raiz);
            Vector3 p=d.PuntoLlegada;
            if(d.nombreVoz.Contains("portal")) p=new Vector3(d.transform.position.x+2,0,12.2f);
            else if(d.nombreVoz.Contains("estatua")) p=new Vector3(d.transform.position.x-1.8f,0,-3.7f);
            else p+=Vector3.right*1.05f;
            Cartel(d,p,raiz);
        }
        for(int lado=-1;lado<=1;lado+=2)
        for(int jardin=0;jardin<4;jardin++)
        for(int i=0;i<12;i++) Plantar(raiz,new Vector3(lado*(11.5f+(i%2)*.5f),.5f,-9+jardin*6+(i/2-2.5f)*.42f),lado>0,i+jardin);
        AjustarCarteles(g);Hornear(g);AjustarLlegadas(g); EditorSceneManager.SaveScene(escena);
    }
    static void Tronco(Vector3 p,Transform padre,bool tocon)
    {
        var o=Objeto(tocon?"Tocón de árbol":"Tronco caído",PrimitiveType.Cylinder,p+Vector3.up*(tocon?.55f:.45f),new Vector3(tocon?1.5f:.9f,tocon?.55f:1.6f,tocon?1.5f:.9f),corteza,padre,false);
        if(!tocon) o.transform.rotation=Quaternion.Euler(0,0,90);
        var col=new GameObject("Colisión simple tronco");col.transform.SetParent(padre,true);col.transform.position=p+Vector3.up*(tocon?.55f:.45f); col.AddComponent<BoxCollider>().size=tocon?new Vector3(1.4f,1.1f,1.4f):new Vector3(3.2f,.9f,.9f);
        if(tocon) Objeto("Corte superior",PrimitiveType.Cylinder,p+Vector3.up*1.105f,new Vector3(1.43f,.012f,1.43f),corte,padre,false);
        else for(int lado=-1;lado<=1;lado+=2)
        { var fin=Objeto("Anillos del tronco",PrimitiveType.Cylinder,p+new Vector3(lado*1.61f,.45f,0),new Vector3(.86f,.01f,.86f),corte,padre,false); fin.transform.rotation=Quaternion.Euler(0,0,90); }
    }
    static void Busqueda()
    {
        var escena=EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity"); var g=UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        if(GameObject.Find("Zonas reconocibles")!=null) {AjustarCarteles(g);Hornear(g);AjustarLlegadas(g);EditorSceneManager.SaveScene(escena);return;}
        LimpiarCarteles();var raiz=new GameObject("Zonas reconocibles").transform;
        var anteriores=g.destinos.Where(d=>d.nombreVoz!="lobby"&&d.nombreVoz!="cobertizo").ToArray();
        var destinos=g.destinos.Except(anteriores).ToList();
        foreach(var d in anteriores) UnityEngine.Object.DestroyImmediate(d.gameObject);
        for(int i=0;i<9;i++)
        {
            Vector3 escondite=g.escondites[i].position; Vector3 p=escondite+Vector3.back*1.9f;p.y=0;
            var zona=Punto("Zona "+Nombres[i],p,raiz);
            var d=zona.gameObject.AddComponent<DestinoExploracion>();d.nombreVoz=Nombres[i]; d.descripcion="Investigar "+Mostrar(Nombres[i]);
            d.llegada=Punto("Llegada "+Nombres[i],escondite+new Vector3(2.2f,0,.6f),zona);d.mirada=Punto("Mirada "+Nombres[i],p+Vector3.up*.9f,zona);
            var detector=zona.gameObject.AddComponent<BoxCollider>(); detector.center=Vector3.up*.7f;detector.size=new Vector3(3,1.4f,2);detector.isTrigger=true;
            if(i==0)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NIVELES/recursosNiveles/Bosque/Handpainted_Forest_pack/Models/Pine_v1_1.prefab");
                var arbol=(GameObject)PrefabUtility.InstantiatePrefab(prefab);arbol.name="Pino solitario";arbol.transform.SetParent(zona,true);var b=arbol.GetComponentInChildren<Renderer>().bounds;
                arbol.transform.localScale*=4.5f/b.size.y;b=arbol.GetComponentInChildren<Renderer>().bounds;arbol.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);
                var tronco=new GameObject("Colisión pino");tronco.transform.SetParent(zona,true);tronco.transform.position=p+Vector3.up;tronco.AddComponent<CapsuleCollider>().height=2;
            }
            if(i==1||i==8) Tronco(p,zona,i==8);
            if(i==2||i==7)
            {
                string fuente="Assets/NIVELES/recursosNiveles/Bosque/BrokenVector/LowPolyRockPack/Prefabs/Rock Type2 01.prefab";
                for(int j=0;j<(i==2?2:1);j++)
                {
                    var roca=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fuente));roca.transform.SetParent(zona,true);
                    var b=roca.GetComponentInChildren<Renderer>().bounds;Vector3 tam=i==7?new Vector3(1.5f,3.2f,1.5f):new Vector3(1.1f,1.8f,1.6f);
                    roca.transform.localScale=new Vector3(tam.x/b.size.x,tam.y/b.size.y,tam.z/b.size.z);b=roca.GetComponentInChildren<Renderer>().bounds;
                    roca.transform.position+=p+Vector3.right*(i==2?(j==0?-.85f:.85f):0)-new Vector3(b.center.x,b.min.y,b.center.z);
                    foreach(var c in roca.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                    Objeto("Colisión roca",PrimitiveType.Cube,roca.GetComponentInChildren<Renderer>().bounds.center,tam*.85f,piedra,zona).GetComponent<Renderer>().enabled=false;
                }
            }
            if(i==3)
            {
                var m=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Exploracion/Mejoras/Arbusto frondoso.asset");
                var mats=Enumerable.Range(0,3).Select(k=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Exploracion/Mejoras/Follaje angular "+k+".mat")).ToArray();
                for(int j=0;j<3;j++){var o=new GameObject("Arbusto de referencia",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(zona,true);o.transform.position=p+Vector3.right*(j-1)*.9f;o.GetComponent<MeshFilter>().sharedMesh=m;o.GetComponent<MeshRenderer>().sharedMaterials=mats;}
            }
            if(i==4)
            {
                Objeto("Muro derruido",PrimitiveType.Cube,p+new Vector3(0,.5f,0),new Vector3(2.8f,1,.45f),piedra,zona);
                for(int j=-1;j<=1;j+=2){Objeto("Columna rota",PrimitiveType.Cylinder,p+new Vector3(j,1,0),new Vector3(.65f,1,.65f),piedra,zona);Objeto("Capitel",PrimitiveType.Cube,p+new Vector3(j,2.05f,0),new Vector3(.85f,.2f,.85f),piedra,zona);}
            }
            if(i==5) Objeto("Suelo del claro",PrimitiveType.Cylinder,p+Vector3.up*.008f,new Vector3(3.5f,.006f,2.5f),corte,zona,false);
            if(i==6) for(int j=0;j<24;j++) Plantar(zona,p+new Vector3((j%6-2.5f)*.4f,0,(j/6-1.5f)*.4f),false,j);
            Cartel(d,p+new Vector3(-2,0,-.9f),raiz);destinos.Add(d);
            // Únicos nueve anclajes de aparición: los mismos índices que consume ReglasEscondite.
            g.escondites[i].name="Escondite "+Nombres[i];g.escondites[i].rotation=Quaternion.LookRotation(d.llegada.position-g.escondites[i].position);
        }
        foreach(var d in destinos.Where(d=>d.nombreVoz=="lobby"||d.nombreVoz=="cobertizo"))
        {d.mirada=Punto("Mirada "+d.nombreVoz,d.transform.position+Vector3.up,raiz);Cartel(d,new Vector3(d.nombreVoz=="lobby"?-2.1f:2.3f,0,d.nombreVoz=="lobby"?-13.9f:-7.5f),raiz);}
        g.destinos=destinos.ToArray();AjustarCarteles(g);Hornear(g);
        AjustarLlegadas(g);
        EditorSceneManager.SaveScene(escena);
    }
    static void Hornear(GestorExploracion g)
    {
        foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if(c.isTrigger||c.gameObject.layer==2||c.bounds.max.y<.1f||g.ignis!=null&&c.transform.IsChildOf(g.ignis))continue;
            var modificador=c.GetComponent<NavMeshModifier>();if(modificador==null)modificador=c.gameObject.AddComponent<NavMeshModifier>();
            modificador.overrideArea=true;modificador.area=1; // Obstáculos, nunca superficies para caminar encima.
        }
        var s=g.GetComponent<NavMeshSurface>();var anterior=s.navMeshData;s.RemoveData();var cols=g.ignis!=null?g.ignis.GetComponentsInChildren<Collider>():Array.Empty<Collider>();
        foreach(var c in cols)c.enabled=false;s.BuildNavMesh();s.RemoveData();string nombre=anterior.name;EditorUtility.CopySerialized(s.navMeshData,anterior);anterior.name=nombre;s.navMeshData=anterior;s.AddData();EditorUtility.SetDirty(anterior);foreach(var c in cols)c.enabled=true;
    }
    static void AjustarCarteles(GestorExploracion g)
    {
        if(g.busqueda&&GameObject.Find("Zonas despejadas para mapa")!=null)return;
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Cartel de madera ")))
        {
            string nombre=t.name.Substring("Cartel de madera ".Length);var d=g.destinos.FirstOrDefault(x=>x.nombreVoz==nombre);if(d==null)continue;
            if(!g.busqueda&&nombre.StartsWith("estatua"))t.position=new Vector3(d.transform.position.x+Mathf.Sign(d.transform.position.x)*1.9f,0,-.5f);
            if(!g.busqueda&&nombre.StartsWith("estanque"))t.position=new Vector3(Mathf.Sign(d.transform.position.x)*9.9f,0,4.6f);
            if(g.busqueda&&nombre!="lobby"&&nombre!="cobertizo")
            {
                Vector3 haciaZona=d.mirada.position-d.PuntoLlegada;haciaZona.y=0;haciaZona.Normalize();
                Vector3 lateral=Vector3.Cross(Vector3.up,haciaZona);
                Vector3 p=d.PuntoLlegada+lateral*2.5f+haciaZona*.3f;
                t.position=new Vector3(Mathf.Clamp(p.x,-15.5f,15.5f),0,Mathf.Clamp(p.z,-16.5f,20.5f));
            }
            Vector3 hacia=-t.position;hacia.y=0;if(hacia.sqrMagnitude>1)t.rotation=Quaternion.LookRotation(-hacia);
        }
    }
    static void AjustarLlegadas(GestorExploracion g)
    {
        NavMesh.SamplePosition(g.jugador.transform.position,out var inicio,3,NavMesh.AllAreas);
        foreach(var d in g.destinos)
        {
            Vector3 origen=d.PuntoLlegada;origen.y=.1f;bool encontrado=false;
            for(float radio=0;radio<=4&&!encontrado;radio+=.4f)
            for(int i=0;i<16&&!encontrado;i++)
            {
                var p=origen+new Vector3(Mathf.Cos(i*Mathf.PI/8),0,Mathf.Sin(i*Mathf.PI/8))*radio;
                if(!NavMesh.SamplePosition(p,out var hit,.3f,NavMesh.AllAreas))continue;
                var ruta=new NavMeshPath();if(!NavMesh.CalculatePath(inicio.position,hit.position,NavMesh.AllAreas,ruta)||ruta.status!=NavMeshPathStatus.PathComplete)continue;
                if(d.llegada==null)d.llegada=Punto("Llegada "+d.nombreVoz,hit.position,d.transform);else d.llegada.position=hit.position;
                encontrado=true;
            }
            if(!encontrado)throw new Exception("Llegada sin acceso: "+d.nombreVoz);
        }
        AjustarCarteles(g);
    }
    public static void Diagnostico()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();var texto=new List<string>();
        bool valido=NavMesh.SamplePosition(g.jugador.transform.position,out var inicio,3,NavMesh.AllAreas);texto.Add("Inicio="+g.jugador.transform.position+" nav="+inicio.position+" valido="+valido);
        foreach(var d in g.destinos){bool ok=NavMesh.SamplePosition(d.PuntoLlegada,out var p,10,NavMesh.AllAreas);var ruta=new NavMeshPath();bool camino=NavMesh.CalculatePath(inicio.position,p.position,NavMesh.AllAreas,ruta);texto.Add(d.nombreVoz+" llegada="+d.PuntoLlegada+" nav="+p.position+" valido="+ok+" ruta="+camino+"/"+ruta.status);}
        File.WriteAllLines("Temp/Exploracion/diag-zonas.txt",texto);
    }
    [MenuItem("Pokimun/Exploración/Despejar referencias y carteles para el mapa")]
    public static void DespejarZonas()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Salí de Play Mode.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Guardá las escenas abiertas.");
        string copia=".local-codex/Backups/Mapa-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(copia);File.Copy("Assets/Scenes/BusquedaIgnis.unity",copia+"/BusquedaIgnis.unity");
        var escena=EditorSceneManager.OpenScene("Assets/Scenes/BusquedaIgnis.unity");var g=UnityEngine.Object.FindFirstObjectByType<GestorExploracion>();
        var pastos=GameObject.Find("Follaje de escondites");if(pastos!=null)pastos.SetActive(false);
        var arbustos=GameObject.Find("Arbustos grandes").transform;
        Vector3[] offsets={new Vector3(-.85f,0,.75f),new Vector3(.85f,0,.8f),new Vector3(0,0,1.7f)};
        for(int i=0;i<arbustos.childCount;i++)
        {
            var t=arbustos.GetChild(i);t.localScale=Vector3.one*(.8f+(i%3)*.07f);
            t.position=g.escondites[i/3].position+offsets[i%3];var r=t.GetComponent<Renderer>();t.position+=Vector3.up*(-.025f-r.bounds.min.y);
        }
        var hojas=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.sharedMesh!=null&&f.sharedMesh.name=="Arbusto frondoso").Select(f=>f.GetComponent<Renderer>()).ToArray();
        int sinEspacio=0;
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Cartel de madera ")))
        {
            string nombre=t.name.Substring("Cartel de madera ".Length);var d=g.destinos.First(x=>x.nombreVoz==nombre);
            if(nombre=="lobby"||nombre=="cobertizo")continue;
            Vector3 hacia=d.mirada.position-d.PuntoLlegada;hacia.y=0;hacia.Normalize();Vector3 lateral=Vector3.Cross(Vector3.up,hacia);
            bool libre=false;
            for(float radio=2.8f;radio<=4.5f&&!libre;radio+=.5f)
            for(int i=0;i<16&&!libre;i++)
            {
                Vector3 direccion=Quaternion.Euler(0,i*22.5f,0)*lateral;Vector3 p=d.PuntoLlegada+direccion*radio;p.y=.1f;
                if(Mathf.Abs(p.x)>15.5f||p.z< -16.5f||p.z>20.5f||!NavMesh.SamplePosition(p,out var hit,.4f,NavMesh.AllAreas))continue;
                // Evitar el eje entre el jugador y la referencia, así el tablero no tapa la llegada.
                if(Vector3.Dot(direccion,hacia)>.65f)continue;
                var espacio=new Bounds(hit.position+Vector3.up*1.1f,new Vector3(2.5f,2.2f,1.2f));
                if(hojas.Any(r=>r.bounds.Intersects(espacio)))continue;
                t.position=new Vector3(hit.position.x,0,hit.position.z);Vector3 vista=d.PuntoLlegada-t.position;vista.y=0;
                t.rotation=Quaternion.LookRotation(-vista);libre=true;
            }
            if(!libre)sinEspacio++;
        }
        if(GameObject.Find("Zonas despejadas para mapa")==null)new GameObject("Zonas despejadas para mapa");
        EditorSceneManager.SaveScene(escena);Directory.CreateDirectory("Temp/Exploracion");
        File.WriteAllText("Temp/Exploracion/limpieza-mapa.txt","OK: follaje de picos anterior desactivado; arbustos separados de referencias; carteles sin espacio despejado="+sinEspacio);
    }

    public static void ProbarRecorridos(GestorExploracion g)
    {
        // Comprobación real con CharacterController: no se teletransporta entre destinos.
        g.GetComponent<GestorVozMultiplataforma>()?.Detener();
        var pendientes = new Queue<DestinoExploracion>(g.destinos);
        string archivo="Temp/Exploracion/recorridos-"+(g.busqueda?"Busqueda":"Lobby")+".txt";
        File.WriteAllText(archivo,"Inicio de recorridos reales\n");
        DestinoExploracion actual=null;double limite=0,quieto=0;bool aceptado=false;
        EditorApplication.CallbackFunction paso=null;
        paso=()=> {
            if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=paso;return;}
            if(g.Pausado)g.EstablecerPausa(false);
            if(actual==null)
            {
                if(pendientes.Count==0){File.AppendAllText(archivo,"FIN\n");EditorApplication.update-=paso;return;}
                actual=pendientes.Dequeue();aceptado=g.jugador.IrA(actual);limite=EditorApplication.timeSinceStartup+35;quieto=0;
            }
            if(!g.jugador.Navegando&&quieto==0)quieto=EditorApplication.timeSinceStartup+2;
            if((!g.jugador.Navegando&&EditorApplication.timeSinceStartup>=quieto)||EditorApplication.timeSinceStartup>=limite)
            {
                var delta=g.jugador.transform.position-actual.PuntoLlegada;delta.y=0;
                Vector3 objetivo=actual.mirada.position-g.jugador.camara.transform.position;objetivo.y=0;
                Vector3 frente=g.jugador.camara.transform.forward;frente.y=0;
                float angulo=Vector3.Angle(frente,objetivo);
                bool ok=aceptado&&delta.magnitude<.7f&&angulo<15&&!g.jugador.Navegando;
                File.AppendAllText(archivo,(ok?"OK ":"ERROR ")+actual.nombreVoz+" distancia="+delta.magnitude.ToString("F2")+" angulo="+angulo.ToString("F1")+" aviso="+g.jugador.Aviso+"\n");
                g.jugador.Detener();actual=null;
            }
        };EditorApplication.update+=paso;
    }
    public static void ProbarMapa(GestorExploracion g)
    {
        if(!g.busqueda)throw new Exception("Abrí la búsqueda para probar el mapa.");
        g.GetComponent<GestorVozMultiplataforma>()?.Detener();
        if(g.Pausado)g.EstablecerPausa(false);
        var destino=g.destinos.First(d=>d.nombreVoz=="tronco caido");g.jugador.IrA(destino);
        if(!g.Partida.Iniciada)g.Partida.Iniciar();
        Vector3 inicio=g.jugador.transform.position;double tiempo=g.Partida.Tiempo;int escondite=g.Partida.Actual;
        g.EstablecerMapa(true);
        bool apertura=g.interfaz.MapaAbierto&&g.Pausado&&g.jugador.Bloqueado&&Time.timeScale==0&&!g.jugador.Navegando;
        var mapa=g.interfaz.GetComponent<MapaExploracion>();mapa.Elegir(destino);
        bool seleccion=!g.jugador.Navegando;
        double fin=EditorApplication.timeSinceStartup+1.5;
        EditorApplication.CallbackFunction revisar=null;
        revisar=()=>{
            if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=revisar;return;}
            if(EditorApplication.timeSinceStartup<fin)return;
            try
            {
                bool congelado=Vector3.Distance(inicio,g.jugador.transform.position)<.01f&&Math.Abs(g.Partida.Tiempo-tiempo)<.0001&&g.Partida.Actual==escondite;
                var panel=GameObject.Find("Mapa del bosque");int botones=panel.GetComponentsInChildren<UnityEngine.UI.Button>().Length;
                mapa.Viajar();bool viaje=!g.Pausado&&!g.interfaz.MapaAbierto&&!g.jugador.Bloqueado&&Time.timeScale==1&&g.jugador.Navegando;
                g.jugador.Detener();g.EstablecerMapa(true);g.EstablecerMapa(false);
                bool cerrar=!g.Pausado&&!g.interfaz.MapaAbierto&&Time.timeScale==1;
                bool ok=apertura&&seleccion&&congelado&&viaje&&cerrar&&botones==22;
                File.WriteAllText("Temp/Exploracion/prueba-mapa.txt",(ok?"OK":"ERROR")+": apertura="+apertura+" seleccion sin mover="+seleccion+" pausa reloj/personaje/rival="+congelado+" botones="+botones+" viaje tras confirmar="+viaje+" cierre="+cerrar);
            }
            catch(Exception e){File.WriteAllText("Temp/Exploracion/prueba-mapa.txt","ERROR "+e);}
            finally{if(g.interfaz.MapaAbierto)g.EstablecerMapa(false);EditorApplication.update-=revisar;}
        };EditorApplication.update+=revisar;
    }
    public static void ProbarAtajosMapa(GestorExploracion g)
    {
        var teclado=UnityEngine.InputSystem.Keyboard.current;
        if(teclado==null)throw new Exception("No hay teclado para comprobar los atajos.");
        if(g.Pausado)g.EstablecerPausa(false);
        int etapa=0;bool abrir=false,cerrar=false,escape=false;
        double siguiente=EditorApplication.timeSinceStartup+.25;
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.M));
        EditorApplication.CallbackFunction revisar=null;
        revisar=()=>{
            if(!EditorApplication.isPlaying||g==null){EditorApplication.update-=revisar;return;}
            if(EditorApplication.timeSinceStartup<siguiente)return;siguiente=EditorApplication.timeSinceStartup+.25;
            if(etapa==0){abrir=g.interfaz.MapaAbierto&&g.Pausado;UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState());}
            if(etapa==1)UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.M));
            if(etapa==2){cerrar=!g.interfaz.MapaAbierto&&!g.Pausado;UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState());}
            if(etapa==3)g.EstablecerMapa(true);
            if(etapa==4)UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
            if(etapa==5)
            {
                escape=!g.interfaz.MapaAbierto&&!g.Pausado;UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado,new UnityEngine.InputSystem.LowLevel.KeyboardState());
                File.WriteAllText("Temp/Exploracion/atajos-mapa.txt",(abrir&&cerrar&&escape?"OK":"ERROR")+": M abre="+abrir+" M cierra="+cerrar+" Escape cierra="+escape);
                if(g.Pausado)g.EstablecerPausa(false);EditorApplication.update-=revisar;
            }
            etapa++;
        };EditorApplication.update+=revisar;
    }
}
