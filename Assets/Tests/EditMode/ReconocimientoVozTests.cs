using System;
using NUnit.Framework;
using Pokimun.Voz;

public class ReconocimientoVozTests
{
    [TestCase(" NÚMERO   UNO ", "numero uno")]
    [TestCase("¡Música, cien!", "musica cien")]
    [TestCase("letra alfa", "letra alfa")]
    [TestCase("letra té", "letra te")]
    public void NormalizaFrasesEspanolas(string texto, string esperado)
    {
        Assert.That(ComandosVoz.Normalizar(texto), Is.EqualTo(esperado));
    }

    [Test]
    public void ConservaLaClaveQueEsperaElConsumidor()
    {
        var comandos = new ComandosVoz(new[] { "música cien", "musica cien", "letra alfa" });
        Assert.That(comandos.IntentarReconocer("MUSICA CIEN", out string comando), Is.True);
        Assert.That(comando, Is.EqualTo("música cien"));
        Assert.That(comandos.IntentarReconocer("letra alfa", out comando), Is.True);
        Assert.That(comando, Is.EqualTo("letra alfa"));
    }

    [TestCase("oscuramente")]
    [TestCase("no quiero cura")]
    [TestCase("escudo tijera")]
    [TestCase("")]
    public void RechazaSubcadenasYOtrasFrases(string texto)
    {
        var comandos = new ComandosVoz(new[] { "cura", "escudo", "tijera" });
        Assert.That(comandos.IntentarReconocer(texto, out _), Is.False);
    }

    [Test]
    public void GramaticaIncluyeRechazoYEliminaDuplicados()
    {
        var comandos = new ComandosVoz(new[] { "letra alfa", "letra alfa ", "NÚMERO UNO" });
        Assert.That(comandos.PalabrasClave, Has.Length.EqualTo(2));
        Assert.That(comandos.CrearGramatica(), Is.EqualTo("[\"letra alfa\",\"número uno\",\"[unk]\"]"));
    }

    [Test]
    public void PcmMonoRespetaExtremosYSaturaSinDesbordamiento()
    {
        var pcm = new short[5];
        AudioVoz.Convertir(new[] { -2f, -1f, 0f, 1f, 2f }, 5, 1, pcm);
        Assert.That(pcm, Is.EqualTo(new short[] { -32768, -32768, 0, 32767, 32767 }));
    }

    [Test]
    public void MezclaEstereoSinDuplicarLaDuracion()
    {
        var pcm = new short[2];
        float rms = AudioVoz.Convertir(new[] { 1f, -1f, .5f, .5f }, 2, 2, pcm);
        Assert.That(pcm, Is.EqualTo(new short[] { 0, 16384 }));
        Assert.That(rms, Is.EqualTo((float)Math.Sqrt(.125)).Within(.00001));
    }

    [Test]
    public void MuestrasNoFinitasNoProducenCriticos()
    {
        var pcm = new short[2];
        float rms = AudioVoz.Convertir(new[] { float.NaN, float.PositiveInfinity }, 2, 1, pcm);
        Assert.That(rms, Is.Zero);
        Assert.That(pcm, Is.EqualTo(new short[] { 0, 0 }));
    }

    [Test]
    public void PotenciaSeMideEnElMomentoDelAudioAunqueElResultadoLlegueDespues()
    {
        var historial = new HistorialNivelVoz(1);
        historial.Agregar(1, .05f);
        historial.Agregar(2, .2f);
        historial.Agregar(3, .01f); // Silencio mientras Vosk termina la frase.
        Assert.That(historial.Obtener(2), Is.EqualTo(.2f));
        Assert.That(historial.Obtener(4), Is.EqualTo(.01f));
        historial.Agregar(10, 0);
        Assert.That(historial.Obtener(10), Is.Zero);
    }
}
