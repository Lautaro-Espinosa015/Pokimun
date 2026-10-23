using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Prueba los componentes del assembly predeterminado sin iniciar Relay ni abrir el micrófono.</summary>
public class IntegracionJuegoTests
{
    private readonly List<GameObject> objetos = new List<GameObject>();
    private Type tipoRelay;
    private FieldInfo salidaEditor;
    private object salidaAnterior;
    private const BindingFlags Privado = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void Preparar()
    {
        tipoRelay = Type.GetType("GestorRedRelay, Assembly-CSharp", true);
        salidaEditor = tipoRelay.GetField("saliendoDelEditor", BindingFlags.Static | BindingFlags.NonPublic);
        salidaAnterior = salidaEditor.GetValue(null);
        salidaEditor.SetValue(null, false);
    }

    [TearDown]
    public void Limpiar()
    {
        foreach (GameObject obj in objetos) UnityEngine.Object.DestroyImmediate(obj);
        objetos.Clear();
        salidaEditor.SetValue(null, salidaAnterior);
    }

    private Component Crear(string nombre)
    {
        var obj = new GameObject("Prueba " + nombre);
        obj.SetActive(false); // No ejecutar Start/OnEnable de los consumidores reales.
        objetos.Add(obj);
        return obj.AddComponent(Type.GetType(nombre + ", Assembly-CSharp", true));
    }

    private static void Desconexion(Component relay)
    {
        relay.GetType().GetMethod("AlDesconectar", Privado).Invoke(relay, new object[] { (ulong)1 });
    }

    [Test]
    public void SalidaLocalCierraRelayAntesDeDestruirElGestor()
    {
        Component relay = Crear("GestorRedRelay");
        Component gestor = Crear("GestorNivel");
        gestor.GetType().GetField("relay", Privado).SetValue(gestor, relay);
        int avisos = 0;
        tipoRelay.GetEvent("OnConexionPerdida").AddEventHandler(relay, new Action<string>(_ => avisos++));

        gestor.GetType().GetMethod("PrepararSalidaDelJuego").Invoke(gestor, null);
        Desconexion(relay);

        Assert.That(tipoRelay.GetField("cerrando", Privado).GetValue(relay), Is.True);
        Assert.That(gestor.GetType().GetProperty("EnBatalla").GetValue(gestor), Is.False);
        Assert.That(avisos, Is.Zero);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void DesconexionRemotaSigueNotificandoAlJugadorQuePermanece()
    {
        Component relay = Crear("GestorRedRelay");
        string aviso = null;
        tipoRelay.GetEvent("OnConexionPerdida").AddEventHandler(relay, new Action<string>(texto => aviso = texto));
        LogAssert.Expect(LogType.Warning, "[Red] El otro jugador se ha desconectado.");

        Desconexion(relay);

        Assert.That(aviso, Is.EqualTo("El otro jugador se ha desconectado."));
    }

    [Test]
    public void DetenerPlayModeNoSeInterpretaComoDesconexionRemotaLocal()
    {
        Component relay = Crear("GestorRedRelay");
        int avisos = 0;
        tipoRelay.GetEvent("OnConexionPerdida").AddEventHandler(relay, new Action<string>(_ => avisos++));
        tipoRelay.GetMethod("AlCambiarModoEditor", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { UnityEditor.PlayModeStateChange.ExitingPlayMode });

        Desconexion(relay);

        Assert.That(avisos, Is.Zero);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void NuevaSesionDelEditorRestableceLasNotificacionesRemotas()
    {
        salidaEditor.SetValue(null, true);
        tipoRelay.GetMethod("PrepararCicloEditor", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Assert.That(salidaEditor.GetValue(null), Is.False);
    }

    [Test]
    public void LinuxTieneUmbralIndependienteYWindowsConservaElAnterior()
    {
        Component detector = Crear("DetectorCommandControl");
        Component voz = detector.gameObject.AddComponent(Type.GetType("GestorVozMultiplataforma, Assembly-CSharp", true));
        detector.GetType().GetField("voz", Privado).SetValue(detector, voz);
        float windows = (float)detector.GetType().GetField("umbralGritoRms", Privado).GetValue(detector);
        float linux = (float)detector.GetType().GetField("umbralGritoRmsLinux", Privado).GetValue(detector);
        float actual = (float)detector.GetType().GetProperty("UmbralGritoActual", Privado).GetValue(detector);
        bool usaVosk = (bool)voz.GetType().GetProperty("UsaVosk").GetValue(voz);

        Assert.That(windows, Is.EqualTo(.12f));
        Assert.That(linux, Is.EqualTo(.24f));
        Assert.That(actual, Is.EqualTo(usaVosk ? linux : windows));
        Assert.That(linux, Is.GreaterThan(windows));
    }

    [TestCase("Standing 2H Magic Attack 04")]
    [TestCase("Standing 2H Magic Area Attack 02")]
    [TestCase("Sword And Shield Power Up")]
    public void HydrosConservaLasOrientacionesOriginalesDeReposoYCombate(string accion)
    {
        const string carpeta = "Assets/ModelosDePokimun/Hydros/Ender_Hydros@";
        var idle = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(carpeta + "Fight Idle.fbx");
        var ataque = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(carpeta + accion + ".fbx");
        Assert.That(idle.clipAnimations, Has.Length.EqualTo(1));
        Assert.That(ataque.clipAnimations, Has.Length.EqualTo(1));
        // La prueba visual del usuario confirma estas configuraciones distintas como correctas.
        Assert.That(idle.clipAnimations[0].keepOriginalOrientation, Is.True);
        Assert.That(ataque.clipAnimations[0].keepOriginalOrientation, Is.False);
        Assert.That(ataque.clipAnimations[0].rotationOffset, Is.EqualTo(idle.clipAnimations[0].rotationOffset));
        Assert.That(ataque.clipAnimations[0].lockRootRotation, Is.EqualTo(idle.clipAnimations[0].lockRootRotation));
    }
}
