using System.Linq;
using NUnit.Framework;
using Pokimun.Exploracion;

public class ReglasEsconditeTests
{
    [Test]
    public void CadaPartidaSeleccionaTresDistintosDeLosNueve()
    {
        for (int semilla = 0; semilla < 100; semilla++)
        {
            var r = new ReglasEscondite(semilla);
            Assert.That(r.Seleccionados.Distinct().Count(), Is.EqualTo(3));
            Assert.That(r.Seleccionados.All(i => i >= 0 && i < 9), Is.True);
            Assert.That(r.Seleccionados.Contains(r.Actual), Is.True);
        }
    }
    [Test]
    public void NoCuentaHastaSalirDelCobertizoNiDurantePausa()
    {
        var r = new ReglasEscondite(1);
        r.Avanzar(60, false, _ => false, out bool pista); Assert.That(r.Tiempo, Is.Zero); Assert.That(pista, Is.False);
        r.Iniciar(); r.Avanzar(60, true, _ => false, out pista); Assert.That(r.Tiempo, Is.Zero);
    }
    [Test]
    public void EstarEnEncuadreImpideElCambioAunqueElTiempoHayaVencido()
    {
        var r = new ReglasEscondite(1); r.Iniciar(); int original = r.Actual;
        Assert.That(r.Avanzar(90, false, i => i == original, out bool pista), Is.False);
        Assert.That(r.Actual, Is.EqualTo(original)); Assert.That(pista, Is.False);
        Assert.That(r.Avanzar(.01, false, _ => false, out _), Is.True);
        Assert.That(r.Actual, Is.Not.EqualTo(original));
        Assert.That(r.Avanzar(.01, false, _ => false, out _), Is.False, "No acumula dos saltos por el retraso.");
    }
    [Test]
    public void NuncaApareceEnUnDestinoQueEstaEnPantalla()
    {
        var r = new ReglasEscondite(2); r.Iniciar(); int original = r.Actual;
        Assert.That(r.Avanzar(46, false, i => i != original, out _), Is.False);
        Assert.That(r.Actual, Is.EqualTo(original));
    }
    [Test]
    public void CambiaIndefinidamenteUsandoSoloLosTresYSinRepetirElActual()
    {
        var r = new ReglasEscondite(3); r.Iniciar();
        for (int n = 0; n < 100; n++)
        {
            int previo = r.Actual;
            Assert.That(r.Avanzar(45, false, _ => false, out _), Is.True);
            Assert.That(r.Actual, Is.Not.EqualTo(previo)); Assert.That(r.Seleccionados.Contains(r.Actual), Is.True);
        }
    }
    [Test]
    public void PistaSoloFueraDeEncuadreYCadaQuinceSegundosSinRafagas()
    {
        var r = new ReglasEscondite(4); r.Iniciar();
        r.Avanzar(14, false, _ => false, out bool pista); Assert.That(pista, Is.False);
        r.Avanzar(1, false, _ => false, out pista); Assert.That(pista, Is.True);
        r.Avanzar(16, false, _ => true, out pista); Assert.That(pista, Is.False);
        r.Avanzar(.1, false, _ => false, out pista); Assert.That(pista, Is.True);
        r.Avanzar(.1, false, _ => false, out pista); Assert.That(pista, Is.False);
    }
    [Test]
    public void HallazgoCongelaElResultadoYNoSigueMoviendoAIgnis()
    {
        var r = new ReglasEscondite(5); r.Iniciar(); r.Avanzar(12, false, _ => false, out _); r.Finalizar(); int actual = r.Actual;
        Assert.That(r.Avanzar(100, false, _ => false, out bool pista), Is.False);
        Assert.That(pista, Is.False); Assert.That(r.Tiempo, Is.EqualTo(12)); Assert.That(r.Actual, Is.EqualTo(actual));
    }
}
