using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class ControladorCubo : MonoBehaviour
{
    [Header("Arrastra aquí tu HandLandmarkerRunner")]
    [SerializeField] private Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner runner;

    [Header("Velocidad de Movimiento")]
    public float velocidad = 3.0f;

    private Renderer rend;
    private int dedosLevantados = 0; // Guarda cuántos dedos hay levantados
    private float tiempoSiguienteColor = 0;

    private void Start()
    {
        rend = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (runner != null) runner.OnLandmarksResult += AlDetectarMano;
    }

    private void OnDisable()
    {
        if (runner != null) runner.OnLandmarksResult -= AlDetectarMano;
    }

    // 1. Cuenta los dedos de forma rápida
    private void AlDetectarMano(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
        {
            dedosLevantados = 0;
            return;
        }

        var mano = result.handLandmarks[0];
        int total = 0;

        // Comparamos punta del dedo vs nudillo para saber si está levantado
        if (mano.landmarks[8].y < mano.landmarks[6].y) total++;   // Índice
        if (mano.landmarks[12].y < mano.landmarks[10].y) total++; // Medio
        if (mano.landmarks[16].y < mano.landmarks[14].y) total++; // Anular
        if (mano.landmarks[20].y < mano.landmarks[18].y) total++; // Meñique

        // Pulgar (Si la punta está alejada)
        float distPulgar = Vector2.Distance(new Vector2(mano.landmarks[4].x, mano.landmarks[4].y), new Vector2(mano.landmarks[17].x, mano.landmarks[17].y));
        float distBase = Vector2.Distance(new Vector2(mano.landmarks[2].x, mano.landmarks[2].y), new Vector2(mano.landmarks[17].x, mano.landmarks[17].y));
        if (distPulgar > distBase) total++;

        dedosLevantados = total;
    }

    // 2. Mueve o cambia el color del cubo sin errores
    private void Update()
    {
        // ✌️ 2 DEDOS: Cambia de color
        if (dedosLevantados == 2)
        {
            if (Time.time > tiempoSiguienteColor)
            {
                rend.material.color = new Color(Random.value, Random.value, Random.value);
                tiempoSiguienteColor = Time.time + 0.4f; // Cambia cada medio segundo
            }
        }
        // 🤟 3 DEDOS: Sube el cubo
        else if (dedosLevantados == 3)
        {
            transform.Translate(Vector3.up * velocidad * Time.deltaTime);
        }
        // ✋ 5 DEDOS: Baja el cubo
        else if (dedosLevantados == 5)
        {
            transform.Translate(Vector3.down * velocidad * Time.deltaTime);
        }
    }
}