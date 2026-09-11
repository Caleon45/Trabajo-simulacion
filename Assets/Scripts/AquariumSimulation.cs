using System.Collections.Generic;
using UnityEngine;

public class AquariumSimulation : MonoBehaviour
{
    [Header("Condiciones Iniciales")]
    // Variables iniciales fijas del ecosistema
    public int initialFish = 10;
    public float initialAlgae = 5f;

    [Header("Variables en Ejecución")]
    // Variables dinámicas que cambian durante la ejecución
    public int actualFish;
    public float actualAlgae;
    public int minute = 0;

    [Header("Parámetros del Modelo")]
    // Tasas y reglas del modelo matemático
    public float consumptionRate = 0.1f;    // Lambda: kg que consume cada pez por minuto
    public float growthRate = 0.5f;         // G(t): kg de crecimiento natural de algas por minuto
    public float algaeThreshold = 1.0f;     // Umbral crítico de algas
    public int fishAddedInterval = 5;       // Cada cuántos minutos se agregan peces
    public int fishAddedAmount = 2;         // Cuántos peces agrega el cuidador

    [Header("Control de Tiempo")]
    // Velocidad de simulación (en segundos por minuto simulado)
    public float secondsPerMinute = 2.0f;
    private float timer = 0f;

    [Header("Visual View (Game View)")]
    // Prefabs opcionales y área del acuario
    public GameObject fishPrefab;
    public GameObject algaePrefab;
    public Transform aquariumArea;

    // Listas para almacenar y gestionar los objetos visuales
    private List<GameObject> fishObjects = new List<GameObject>();
    private List<GameObject> algaeObjects = new List<GameObject>();

    // Sprites procedurales reutilizables
    private Sprite defaultFishSprite;
    private Sprite defaultAlgaeSprite;
    private Sprite defaultBgSprite;

    void Start()
    {
        // Definir condiciones iniciales
        actualFish = initialFish;
        actualAlgae = initialAlgae;
        minute = 0;

        // Dibujar el ecosistema inicial en Game View
        DrawView();

        // Mostrar condiciones iniciales en consola
        Debug.Log($"[Inicio] Minuto {minute}: {actualFish} peces | {actualAlgae:F2} kg de algas");
    }

    void Update()
    {
        // Controlar el paso del tiempo
        timer += Time.deltaTime;

        // Llamar a la función de simulación al cumplirse el minuto
        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            Simulate();
        }
    }

    void Simulate()
    {
        // Avanzar el tiempo minuto a minuto
        minute++;

        // Crecimiento natural de algas
        actualAlgae += growthRate;

        // Consumo de algas por peces (solo comen lo que hay disponible)
        float wantedConsumption = actualFish * consumptionRate;
        float actualConsumption = Mathf.Min(wantedConsumption, actualAlgae);
        actualAlgae -= actualConsumption;

        // Asegurar que las algas nunca sean negativas
        if (actualAlgae < 0f) actualAlgae = 0f;

        // Mortalidad: si las algas bajan del umbral crítico muere 1 pez
        if (actualAlgae < algaeThreshold && actualFish > 0)
        {
            actualFish--;
            Debug.Log($"[Alerta] Minuto {minute}: Algas críticas ({actualAlgae:F2} kg < {algaeThreshold} kg). Muere 1 pez.");
        }

        // Incorporación: cada 5 minutos el cuidador agrega peces
        if (minute % fishAddedInterval == 0)
        {
            actualFish += fishAddedAmount;
            Debug.Log($"[Cuidador] Minuto {minute}: Múltiplo de {fishAddedInterval}. Se agregaron {fishAddedAmount} peces.");
        }

        // Asegurar que la población de peces no sea negativa
        if (actualFish < 0) actualFish = 0;

        // Representar visualmente el nuevo estado en Game View
        DrawView();

        // Mostrar evolución del sistema en consola
        Debug.Log($"Minuto {minute}: {actualFish} peces | {actualAlgae:F2} kg de algas (Consumo: {actualConsumption:F2} kg)");

        // Verificar extinción de peces
        if (actualFish <= 0)
        {
            Debug.Log($"[Fin] Minuto {minute}: Todos los peces han muerto. Las algas comenzarán a recuperarse.");
        }
    }

    void DrawView()
    {
        // Limpiar objetos visuales anteriores de peces
        foreach (GameObject fish in fishObjects)
        {
            if (fish != null) Destroy(fish);
        }

        // Limpiar objetos visuales anteriores de algas
        foreach (GameObject algae in algaeObjects)
        {
            if (algae != null) Destroy(algae);
        }

        fishObjects.Clear();
        algaeObjects.Clear();

        // Generar o vincular el escenario del acuario (fondo azul marino)
        if (aquariumArea == null)
        {
            GameObject existingArea = GameObject.Find("AquariumArea");
            if (existingArea != null)
            {
                aquariumArea = existingArea.transform;
            }
            else
            {
                GameObject bgObj = new GameObject("AquariumArea");
                bgObj.transform.position = Vector3.zero;
                bgObj.transform.localScale = new Vector3(14f, 8.5f, 1f);
                SpriteRenderer bgRenderer = bgObj.AddComponent<SpriteRenderer>();

                if (defaultBgSprite == null)
                {
                    Texture2D bgTex = new Texture2D(2, 2);
                    Color oceanBlue = new Color(0.05f, 0.15f, 0.32f, 1f);
                    bgTex.SetPixels(new Color[] { oceanBlue, oceanBlue, oceanBlue, oceanBlue });
                    bgTex.Apply();
                    defaultBgSprite = Sprite.Create(bgTex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
                }

                bgRenderer.sprite = defaultBgSprite;
                bgRenderer.sortingOrder = -10;
                aquariumArea = bgObj.transform;
            }
        }

        float halfW = aquariumArea.localScale.x / 2f;
        float halfH = aquariumArea.localScale.y / 2f;
        float floorY = aquariumArea.position.y - halfH;
        float ceilingY = aquariumArea.position.y + halfH;

        // Generar sprite circular verde para algas si no existe
        if (algaePrefab == null && defaultAlgaeSprite == null)
        {
            Texture2D circleTex = new Texture2D(32, 32);
            Vector2 center = new Vector2(16f, 16f);
            Color greenColor = new Color(0.18f, 0.82f, 0.28f, 1f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), center) <= 14f)
                        circleTex.SetPixel(x, y, greenColor);
                    else
                        circleTex.SetPixel(x, y, Color.clear);
                }
            }
            circleTex.Apply();
            defaultAlgaeSprite = Sprite.Create(circleTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        // Generar sprite triangular cian para peces si no existe
        if (fishPrefab == null && defaultFishSprite == null)
        {
            Texture2D triTex = new Texture2D(32, 32);
            Color cyanColor = new Color(0.15f, 0.92f, 1.0f, 1f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float normX = x / 31f;
                    float halfHeightPx = (1f - normX) * 14f;
                    if (Mathf.Abs(y - 16f) <= halfHeightPx)
                        triTex.SetPixel(x, y, cyanColor);
                    else
                        triTex.SetPixel(x, y, Color.clear);
                }
            }
            triTex.Apply();
            defaultFishSprite = Sprite.Create(triTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        // Dibujar algas: círculos verdes creciendo desde el fondo hacia arriba
        if (actualAlgae > 0f)
        {
            int algaeCircles = Mathf.RoundToInt(actualAlgae / 0.5f);
            int columns = 10;

            for (int i = 0; i < algaeCircles; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float xPos = aquariumArea.position.x + Mathf.Lerp(-halfW * 0.82f, halfW * 0.82f, (col + 0.5f) / columns);
                float yPos = floorY + 0.55f + (row * 0.60f);
                Vector3 algaePos = new Vector3(xPos, yPos, 0f);

                GameObject algae;
                if (algaePrefab != null)
                {
                    algae = Instantiate(algaePrefab, algaePos, Quaternion.identity);
                }
                else
                {
                    algae = new GameObject("AlgaeCircle_" + i);
                    algae.transform.position = algaePos;
                    algae.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                    SpriteRenderer sr = algae.AddComponent<SpriteRenderer>();
                    sr.sprite = defaultAlgaeSprite;
                    sr.sortingOrder = 1;
                }
                algaeObjects.Add(algae);
            }
        }

        // Dibujar peces: triángulos cian nadando en aguas abiertas (por encima de las algas)
        float minFishY = floorY + 2.3f;
        float maxFishY = ceilingY - 0.7f;

        for (int i = 0; i < actualFish; i++)
        {
            Vector3 fishPos = new Vector3(
                aquariumArea.position.x + Random.Range(-halfW * 0.8f, halfW * 0.8f),
                Random.Range(minFishY, maxFishY),
                0f
            );

            GameObject fish;
            if (fishPrefab != null)
            {
                fish = Instantiate(fishPrefab, fishPos, Quaternion.identity);
            }
            else
            {
                fish = new GameObject("FishTriangle_" + i);
                fish.transform.position = fishPos;
                float flip = Random.value > 0.5f ? 1f : -1f;
                fish.transform.localScale = new Vector3(0.65f * flip, 0.45f, 1f);
                SpriteRenderer sr = fish.AddComponent<SpriteRenderer>();
                sr.sprite = defaultFishSprite;
                sr.sortingOrder = 2;
            }
            fishObjects.Add(fish);
        }
    }
}
