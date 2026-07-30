using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Valida los clips, el prefab del jugador y la configuración sonora de armas.
/// </summary>
public static class SimuladorSonidosJugadorYArmas
{
    private const string RutaPrefabJugador = "Assets/Prefabs/Jugador/Jugador.prefab";

    private static readonly string[] RutasSonidosJugador =
    {
        "Assets/Audio/Jugador/Jugador_Paso_1.wav",
        "Assets/Audio/Jugador/Jugador_Paso_2.wav",
        "Assets/Audio/Jugador/Jugador_Salto.wav",
        "Assets/Audio/Jugador/Jugador_Dash.wav"
    };

    private static readonly string[] RutasArmas =
    {
        "Assets/Datos/Armas/Arma_Pistola.asset",
        "Assets/Datos/Armas/Arma_Metralleta.asset",
        "Assets/Datos/Armas/Arma_Escopeta.asset",
        "Assets/Datos/Armas/Arma_Katana.asset"
    };

    [MenuItem("Herramientas/Beagle/Simular sonidos del jugador y armas")]
    public static void Ejecutar()
    {
        ValidarClipsJugador();
        ValidarPrefabJugador();
        ValidarArmas();
        Debug.Log("[SIMULACIÓN AUDIO] OK: jugador y cuatro armas tienen sonidos válidos y configurados.");
    }

    public static void EjecutarEnLote()
    {
        Ejecutar();
    }

    private static void ValidarClipsJugador()
    {
        foreach (string ruta in RutasSonidosJugador)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            Exigir(clip != null, "No se pudo importar " + ruta);
            Exigir(clip.length >= 0.05f && clip.length <= 0.4f,
                $"{ruta} tiene una duración inadecuada: {clip.length:F3} s.");
            Exigir(clip.channels == 1, ruta + " debe ser mono.");
        }
    }

    private static void ValidarPrefabJugador()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
        Exigir(prefab != null, "No se pudo cargar el prefab del Jugador.");

        SonidosJugador sonidos = prefab.GetComponent<SonidosJugador>();
        Exigir(sonidos != null, "El prefab del Jugador no tiene SonidosJugador.");

        SerializedObject serializado = new SerializedObject(sonidos);
        SerializedProperty pasos = serializado.FindProperty("sonidosPasos");
        SerializedProperty salto = serializado.FindProperty("sonidoSalto");
        SerializedProperty dash = serializado.FindProperty("sonidoDash");

        Exigir(pasos != null && pasos.arraySize >= 2,
            "El Jugador necesita al menos dos variantes de pasos.");
        for (int i = 0; i < pasos.arraySize; i++)
        {
            Exigir(pasos.GetArrayElementAtIndex(i).objectReferenceValue != null,
                $"La variante de paso {i + 1} no está asignada.");
        }

        Exigir(salto != null && salto.objectReferenceValue != null,
            "El sonido de salto no está asignado.");
        Exigir(dash != null && dash.objectReferenceValue != null,
            "El sonido de dash no está asignado.");
    }

    private static void ValidarArmas()
    {
        foreach (string ruta in RutasArmas)
        {
            DatosArma arma = AssetDatabase.LoadAssetAtPath<DatosArma>(ruta);
            Exigir(arma != null, "No se pudo cargar " + ruta);
            Exigir(arma.sonidoUso != null, arma.nombreArma + " no tiene sonido de uso.");
            Exigir(arma.volumenSonido > 0f && arma.volumenSonido <= 0.65f,
                arma.nombreArma + " tiene un volumen fuera del rango cómodo.");
            Exigir(arma.variacionTonoSonido >= 0f && arma.variacionTonoSonido <= 0.08f,
                arma.nombreArma + " tiene una variación de tono excesiva.");
        }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException("[SIMULACIÓN AUDIO] " + mensaje);
    }
}
