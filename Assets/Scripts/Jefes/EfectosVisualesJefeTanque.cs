using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera humo, polvo, chispas y marcadores utilizando recursos nativos de Unity.
/// No depende de sprites externos.
/// </summary>
[DisallowMultipleComponent]
public class EfectosVisualesJefeTanque : MonoBehaviour
{
    private ParticleSystem sistemaParticulas;
    private Coroutine rutinaHumo;
    private readonly Queue<LineRenderer> marcadoresDisponibles = new Queue<LineRenderer>();
    private readonly HashSet<LineRenderer> marcadoresActivos = new HashSet<LineRenderer>();
    private Transform contenedorMarcadores;
    private static Material materialParticulas;
    private static Material materialLineas;

    private void Awake()
    {
        sistemaParticulas = CrearSistemaParticulas();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        rutinaHumo = null;

        LineRenderer[] copia = new LineRenderer[marcadoresActivos.Count];
        marcadoresActivos.CopyTo(copia);
        foreach (LineRenderer marcador in copia)
            DevolverMarcador(marcador);
    }

    public void PrepararMarcadoresMetralla(int cantidad)
    {
        CrearContenedorMarcadores();
        while (marcadoresDisponibles.Count + marcadoresActivos.Count < Mathf.Max(1, cantidad))
            marcadoresDisponibles.Enqueue(CrearMarcador());
    }

    public void ActivarHumoFase2()
    {
        if (rutinaHumo == null) rutinaHumo = StartCoroutine(EmitirHumoContinuo());
    }

    public void EmitirPreparacionEmbestida()
    {
        Vector2 origen = (Vector2)transform.position + Vector2.down * 0.8f;
        EmitirRafaga(origen, 14, new Color(0.5f, 0.42f, 0.34f, 0.85f),
            0.12f, 0.28f, 0.3f, 0.75f, 1.5f);
    }

    public void EmitirImpactoEmbestida(Vector2 posicion)
    {
        EmitirRafaga(posicion, 28, new Color(1f, 0.55f, 0.15f, 1f),
            0.06f, 0.16f, 0.2f, 0.55f, 4.5f);
        EmitirRafaga(posicion, 18, new Color(0.48f, 0.42f, 0.38f, 0.9f),
            0.16f, 0.34f, 0.45f, 0.95f, 2.8f);
    }

    public void EmitirSobrecalentamiento()
    {
        Vector2 origen = (Vector2)transform.position + Vector2.up * 1.1f;
        EmitirRafaga(origen, 10, new Color(0.35f, 0.35f, 0.35f, 0.65f),
            0.2f, 0.4f, 0.65f, 1.2f, 1.4f);
    }

    public void EmitirTransicionFase()
    {
        Vector2 origen = transform.position;
        EmitirRafaga(origen, 42, new Color(1f, 0.2f, 0.05f, 1f),
            0.06f, 0.16f, 0.3f, 0.7f, 5.5f);
        EmitirRafaga(origen, 26, new Color(0.25f, 0.25f, 0.25f, 0.8f),
            0.22f, 0.5f, 0.7f, 1.5f, 3f);
    }

    public void EmitirExplosionEn(Vector2 posicion)
    {
        EmitirRafaga(posicion, 22, new Color(1f, 0.48f, 0.05f, 1f),
            0.08f, 0.22f, 0.2f, 0.55f, 4f);
    }

    public void EmitirHumoMisil(Vector2 posicion)
    {
        EmitirRafaga(posicion, 1, new Color(0.38f, 0.38f, 0.38f, 0.55f),
            0.12f, 0.22f, 0.45f, 0.8f, 0.65f);
    }

    public void CrearMarcadorSuelo(Vector2 posicion, float radio, float duracion)
    {
        LineRenderer linea = ObtenerMarcador();
        Vector2 centro = posicion + Vector2.up * 0.04f;

        for (int i = 0; i < linea.positionCount; i++)
        {
            float angulo = i / (float)linea.positionCount * Mathf.PI * 2f;
            Vector2 desplazamiento =
                new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo) * 0.3f) * radio;
            linea.SetPosition(i, centro + desplazamiento);
        }

        StartCoroutine(AnimarMarcador(linea, duracion));
    }

    private IEnumerator AnimarMarcador(LineRenderer linea, float duracion)
    {
        float tiempo = 0f;
        while (linea != null && tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float pulso = 0.45f + Mathf.PingPong(tiempo * 4f, 0.55f);
            Color color = new Color(1f, 0.15f + pulso * 0.35f, 0.02f, pulso);
            linea.startColor = color;
            linea.endColor = color;
            yield return null;
        }

        DevolverMarcador(linea);
    }

    private LineRenderer ObtenerMarcador()
    {
        CrearContenedorMarcadores();
        LineRenderer linea = marcadoresDisponibles.Count > 0
            ? marcadoresDisponibles.Dequeue()
            : CrearMarcador();

        marcadoresActivos.Add(linea);
        linea.gameObject.SetActive(true);
        return linea;
    }

    private LineRenderer CrearMarcador()
    {
        CrearContenedorMarcadores();
        GameObject marcador = new GameObject("Marcador_Peligro_Metralla_Reutilizable");
        marcador.transform.SetParent(contenedorMarcadores, false);

        LineRenderer linea = marcador.AddComponent<LineRenderer>();
        linea.loop = true;
        linea.useWorldSpace = true;
        linea.positionCount = 20;
        linea.widthMultiplier = 0.07f;
        linea.material = ObtenerMaterialLineas();
        linea.sortingOrder = 8;
        marcador.SetActive(false);
        return linea;
    }

    private void DevolverMarcador(LineRenderer linea)
    {
        if (linea == null || !marcadoresActivos.Remove(linea)) return;
        linea.gameObject.SetActive(false);
        linea.transform.SetParent(contenedorMarcadores, false);
        marcadoresDisponibles.Enqueue(linea);
    }

    private void CrearContenedorMarcadores()
    {
        if (contenedorMarcadores != null) return;

        Transform existente = transform.Find("Pool_Marcadores_Metralla");
        if (existente != null)
        {
            contenedorMarcadores = existente;
            return;
        }

        GameObject objeto = new GameObject("Pool_Marcadores_Metralla");
        objeto.transform.SetParent(transform, false);
        contenedorMarcadores = objeto.transform;
    }

    private IEnumerator EmitirHumoContinuo()
    {
        while (true)
        {
            Vector2 origen = (Vector2)transform.position + new Vector2(0f, 1.25f);
            EmitirRafaga(origen, 2, new Color(0.28f, 0.28f, 0.28f, 0.55f),
                0.2f, 0.42f, 0.8f, 1.45f, 1.1f);
            yield return new WaitForSeconds(0.16f);
        }
    }

    private void EmitirRafaga(Vector2 posicion, int cantidad, Color color,
        float tamanoMinimo, float tamanoMaximo, float vidaMinima, float vidaMaxima,
        float velocidad)
    {
        if (sistemaParticulas == null) sistemaParticulas = CrearSistemaParticulas();

        for (int i = 0; i < cantidad; i++)
        {
            Vector2 direccion = Random.insideUnitCircle.normalized;
            direccion.y = Mathf.Abs(direccion.y) + 0.15f;

            var parametros = new ParticleSystem.EmitParams
            {
                position = posicion + Random.insideUnitCircle * 0.18f,
                velocity = direccion.normalized * Random.Range(velocidad * 0.55f, velocidad),
                startColor = color,
                startLifetime = Random.Range(vidaMinima, vidaMaxima),
                startSize = Random.Range(tamanoMinimo, tamanoMaximo)
            };

            sistemaParticulas.Emit(parametros, 1);
        }
    }

    private ParticleSystem CrearSistemaParticulas()
    {
        Transform existente = transform.Find("Particulas_Jefe");
        GameObject objeto = existente != null ? existente.gameObject : new GameObject("Particulas_Jefe");
        objeto.transform.SetParent(transform, false);

        ParticleSystem sistema = objeto.GetComponent<ParticleSystem>();
        if (sistema == null) sistema = objeto.AddComponent<ParticleSystem>();

        var principal = sistema.main;
        principal.loop = false;
        principal.playOnAwake = false;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = 250;
        principal.gravityModifier = 0.1f;

        var emision = sistema.emission;
        emision.enabled = false;

        var forma = sistema.shape;
        forma.enabled = false;

        ParticleSystemRenderer renderer = objeto.GetComponent<ParticleSystemRenderer>();
        renderer.material = ObtenerMaterialParticulas();
        renderer.sortingOrder = 7;
        sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        return sistema;
    }

    private static Material ObtenerMaterialParticulas()
    {
        if (materialParticulas != null) return materialParticulas;

        Shader shader = Shader.Find("Sprites/Default");
        materialParticulas = new Material(shader)
        {
            name = "Material_Particulas_Jefe_Runtime",
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = CrearTexturaCircular()
        };
        return materialParticulas;
    }

    private static Material ObtenerMaterialLineas()
    {
        if (materialLineas != null) return materialLineas;

        materialLineas = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Material_Lineas_Jefe_Runtime",
            hideFlags = HideFlags.HideAndDontSave
        };
        return materialLineas;
    }

    private static Texture2D CrearTexturaCircular()
    {
        const int tamano = 32;
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false)
        {
            name = "Textura_Particula_Circular_Runtime",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixeles = new Color[tamano * tamano];
        Vector2 centro = new Vector2((tamano - 1) * 0.5f, (tamano - 1) * 0.5f);
        float radio = tamano * 0.5f;

        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), centro) / radio;
                float alfa = Mathf.Clamp01(1f - distancia);
                alfa *= alfa;
                pixeles[y * tamano + x] = new Color(1f, 1f, 1f, alfa);
            }
        }

        textura.SetPixels(pixeles);
        textura.Apply();
        return textura;
    }
}
