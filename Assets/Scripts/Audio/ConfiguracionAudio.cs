using System;
using UnityEngine;

/// <summary>
/// Guarda y comunica los dos canales de volumen utilizados por el proyecto.
/// No depende de ninguna escena y queda preparado para futuras fuentes de audio.
/// </summary>
public static class ConfiguracionAudio
{
    private const string ClaveVolumenMusica = "VolumenMusica";
    private const string ClaveVolumenEfectos = "VolumenEfectos";

    private const float VolumenMusicaPredeterminado = 0.7f;
    private const float VolumenEfectosPredeterminado = 1f;

    private static bool inicializada;
    private static float volumenMusica;
    private static float volumenEfectos;

    public static event Action<float> AlCambiarMusica;
    public static event Action<float> AlCambiarEfectos;

    public static float VolumenMusica
    {
        get
        {
            AsegurarInicializacion();
            return volumenMusica;
        }
        set
        {
            AsegurarInicializacion();
            float nuevoValor = Mathf.Clamp01(value);
            if (Mathf.Approximately(volumenMusica, nuevoValor)) return;

            volumenMusica = nuevoValor;
            PlayerPrefs.SetFloat(ClaveVolumenMusica, volumenMusica);
            AlCambiarMusica?.Invoke(volumenMusica);
        }
    }

    public static float VolumenEfectos
    {
        get
        {
            AsegurarInicializacion();
            return volumenEfectos;
        }
        set
        {
            AsegurarInicializacion();
            float nuevoValor = Mathf.Clamp01(value);
            if (Mathf.Approximately(volumenEfectos, nuevoValor)) return;

            volumenEfectos = nuevoValor;
            PlayerPrefs.SetFloat(ClaveVolumenEfectos, volumenEfectos);
            AlCambiarEfectos?.Invoke(volumenEfectos);
        }
    }

    public static float AplicarMusica(float volumenBase)
    {
        return Mathf.Clamp01(volumenBase) * VolumenMusica;
    }

    public static float AplicarEfectos(float volumenBase)
    {
        return Mathf.Clamp01(volumenBase) * VolumenEfectos;
    }

    public static void Guardar()
    {
        AsegurarInicializacion();
        PlayerPrefs.Save();
    }

    private static void AsegurarInicializacion()
    {
        if (inicializada) return;

        volumenMusica = Mathf.Clamp01(
            PlayerPrefs.GetFloat(ClaveVolumenMusica, VolumenMusicaPredeterminado));
        volumenEfectos = Mathf.Clamp01(
            PlayerPrefs.GetFloat(ClaveVolumenEfectos, VolumenEfectosPredeterminado));
        inicializada = true;
    }
}
