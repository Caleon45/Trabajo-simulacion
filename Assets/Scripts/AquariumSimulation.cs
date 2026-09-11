using System.Collections.Generic;
using UnityEngine;

public class AquariumSimulation : MonoBehaviour
{
    [Header("Condiciones Iniciales")]
    public int initialFish = 10;
    public float initialAlgae = 5f;

    [Header("Variables en Ejecución")]
    public int actualFish;
    public float actualAlgae;
    public int minute = 0;

    [Header("Parámetros del Modelo")]
    public float consumptionRate = 0.1f;
    public float growthRate = 0.5f;
    public float algaeThreshold = 1.0f;
    public int fishAddedInterval = 5;
    public int fishAddedAmount = 2;

    [Header("Control de Tiempo")]
    public float secondsPerMinute = 2.0f;
    private float timer = 0f;

    [Header("Visual View (Game View)")]
    public GameObject fishPrefab;
    public GameObject algaePrefab;
    public Transform aquariumArea;

    private List<GameObject> fishObjects = new List<GameObject>();
    private List<GameObject> algaeObjects = new List<GameObject>();

    private Sprite defaultFishSprite;
    private Sprite defaultAlgaeSprite;
    private Sprite defaultBgSprite;

    void Start()
    {
        actualFish = initialFish;
        actualAlgae = initialAlgae;
        minute = 0;

        DrawView();
        Debug.Log($"[Inicio] Minuto {minute}: {actualFish} peces | {actualAlgae:F2} kg de algas");
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            Simulate();
        }
    }

    void Simulate()
    {
        minute++;

        // Crecimiento y consumo de algas
        actualAlgae += growthRate;
        float wantedConsumption = actualFish * consumptionRate;
        float actualConsumption = Mathf.Min(wantedConsumption, actualAlgae);
        actualAlgae -= actualConsumption;

        if (actualAlgae < 0f) actualAlgae = 0f;

        // Mortalidad por falta de alimento
        if (actualAlgae < algaeThreshold && actualFish > 0)
        {
            actualFish--;
            Debug.Log($"[Alerta] Minuto {minute}: Algas críticas ({actualAlgae:F2} kg < {algaeThreshold} kg). Muere 1 pez.");
        }

        // Incorporación de peces cada 5 minutos
        if (minute % fishAddedInterval == 0)
        {
            actualFish += fishAddedAmount;
            Debug.Log($"[Cuidador] Minuto {minute}: Múltiplo de {fishAddedInterval}. Se agregaron {fishAddedAmount} peces.");
        }

        if (actualFish < 0) actualFish = 0;

        DrawView();

        Debug.Log($"Minuto {minute}: {actualFish} peces | {actualAlgae:F2} kg de algas (Consumo: {actualConsumption:F2} kg)");

        if (actualFish <= 0)
        {
            Debug.Log($"[Fin] Minuto {minute}: Todos los peces han muerto. Las algas comenzarán a recuperarse.");
        }
    }

    void DrawView()
    {
        // Limpiar objetos previos
        foreach (GameObject fish in fishObjects)
        {
            if (fish != null) Destroy(fish);
        }
        foreach (GameObject algae in algaeObjects)
        {
            if (algae != null) Destroy(algae);
        }
        fishObjects.Clear();
        algaeObjects.Clear();

        // Escenario del acuario
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

        // Sprites por defecto si no se asignaron prefabs
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

        // Dibujar algas desde el fondo hacia arriba
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

        // Dibujar peces sobre el nivel del piso
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

// Nota: Asistencia de IA utilizada únicamente para la estructuración parcial del código.
