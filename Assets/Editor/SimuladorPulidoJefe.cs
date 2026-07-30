using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verifica referencias, física, límites y reglas del director de ataques.
/// </summary>
public static class SimuladorPulidoJefe
{
    private const string RutaJefe = "Assets/Prefabs/Jefes/Jefe_Tanque.prefab";
    private const string RutaBala = "Assets/Prefabs/Proyectiles/Jefes/BalaJefe.prefab";
    private const string RutaMisil = "Assets/Prefabs/Jefes/MisilTeledirigido.prefab";
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";

    [MenuItem("Herramientas/Proyecto Beagle/Verificar pulido del Jefe")]
    public static void Ejecutar()
    {
        VerificarPrefabJefe();
        VerificarProyectiles();
        VerificarDirectorAtaques();
        VerificarNivel1();

        Debug.Log(
            "[SIMULACIÓN JEFE] OK: prefab, metralla, misil, límites, cámaras y " +
            "selección contextual verificados.");
    }

    private static void VerificarPrefabJefe()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe);
        Verificar(prefab != null, "No se encontró Jefe_Tanque.prefab.");

        JefeTanqueController controlador = prefab.GetComponent<JefeTanqueController>();
        Verificar(controlador != null, "El prefab no tiene JefeTanqueController.");
        Verificar(prefab.GetComponent<DirectorAtaquesJefe>() != null,
            "El prefab no tiene DirectorAtaquesJefe.");
        Verificar(prefab.GetComponent<EfectosVisualesJefeTanque>() != null,
            "El prefab no tiene EfectosVisualesJefeTanque.");
        Verificar(prefab.GetComponent<SacudidaCamaraJefe>() != null,
            "El prefab no tiene SacudidaCamaraJefe.");
        Verificar(prefab.GetComponent<CinemachineImpulseSource>() != null,
            "El prefab no tiene fuente de impulsos de Cinemachine.");

        Verificar(controlador.balaMetrallaPrefab != null,
            "La bala de metralla no está asignada.");
        Verificar(AssetDatabase.GetAssetPath(controlador.balaMetrallaPrefab) == RutaBala,
            "La metralla no utiliza BalaJefe.");
        Verificar(controlador.puntoDisparoCanon != null &&
            controlador.puntoDisparoMetralla != null && controlador.lineaLaser != null,
            "Faltan referencias de disparo o LineRenderer.");

        Rigidbody2D cuerpo = prefab.GetComponent<Rigidbody2D>();
        Verificar(cuerpo != null, "El Jefe no tiene Rigidbody2D.");
        Verificar(cuerpo.collisionDetectionMode == CollisionDetectionMode2D.Continuous,
            "La embestida no utiliza detección continua.");
        Verificar(cuerpo.interpolation == RigidbodyInterpolation2D.Interpolate,
            "El Jefe no tiene interpolación de física.");
        Verificar(prefab.GetComponent<SaludJefe>().vidaMaxima == 400,
            "La vida del prefab no coincide con la pelea configurada.");
    }

    private static void VerificarProyectiles()
    {
        GameObject bala = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBala);
        Verificar(bala != null, "No se encontró BalaJefe.");
        Verificar(bala.GetComponent<MovimientoProyectil>() != null,
            "BalaJefe no tiene MovimientoProyectil.");

        Rigidbody2D cuerpoBala = bala.GetComponent<Rigidbody2D>();
        Verificar(cuerpoBala != null && cuerpoBala.gravityScale > 0f,
            "BalaJefe no tiene gravedad.");
        Verificar(cuerpoBala.collisionDetectionMode == CollisionDetectionMode2D.Continuous,
            "BalaJefe no utiliza detección continua.");
        Verificar(bala.GetComponent<BalaEnemiga>().danoAlSerDesviada > 0,
            "BalaJefe no tiene daño de parry.");

        GameObject misil = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMisil);
        Verificar(misil != null, "No se encontró MisilTeledirigido.");
        MisilTeledirigido comportamiento = misil.GetComponent<MisilTeledirigido>();
        Verificar(comportamiento != null && comportamiento.danoAlSerDesviado > 0,
            "El misil no tiene daño al ser desviado.");
        Verificar(misil.GetComponent<Rigidbody2D>().collisionDetectionMode ==
            CollisionDetectionMode2D.Continuous,
            "El misil no utiliza detección continua.");
    }

    private static void VerificarDirectorAtaques()
    {
        GameObject jefe = new GameObject("Simulacion_Director_Jefe");
        GameObject jugador = new GameObject("Simulacion_Director_Jugador");
        GameObject objetoLimites = new GameObject("Simulacion_Limites");

        try
        {
            DirectorAtaquesJefe director = jefe.AddComponent<DirectorAtaquesJefe>();
            LimitesArenaJefe limites = objetoLimites.AddComponent<LimitesArenaJefe>();
            limites.ancho = 23f;

            var conteoFase1 = new Dictionary<TipoAtaqueTanque, int>();
            UnityEngine.Random.State estadoAnterior = UnityEngine.Random.state;
            UnityEngine.Random.InitState(20260729);

            TipoAtaqueTanque? anterior = null;
            TipoAtaqueTanque? anteAnterior = null;

            for (int i = 0; i < 240; i++)
            {
                jugador.transform.position = new Vector3(
                    i % 3 == 0 ? 9f : i % 3 == 1 ? 5f : 2.5f,
                    i % 4 == 0 ? 2.5f : 0f,
                    0f);

                TipoAtaqueTanque ataque = director.ElegirAtaque(
                    jefe.transform, jugador.transform, false, false, limites);
                Verificar(ataque != TipoAtaqueTanque.Misil,
                    "El director eligió misil durante la fase 1.");

                if (!conteoFase1.ContainsKey(ataque)) conteoFase1[ataque] = 0;
                conteoFase1[ataque]++;

                Verificar(!(anterior.HasValue && anteAnterior.HasValue &&
                    ataque == anterior.Value && ataque == anteAnterior.Value),
                    "El director repitió el mismo ataque tres veces.");

                anteAnterior = anterior;
                anterior = ataque;
            }

            Verificar(conteoFase1.ContainsKey(TipoAtaqueTanque.Laser) &&
                conteoFase1.ContainsKey(TipoAtaqueTanque.Metralla) &&
                conteoFase1.ContainsKey(TipoAtaqueTanque.Embestida),
                "No aparecieron todos los ataques de fase 1.");

            director.Reiniciar();
            bool aparecioMisil = false;
            for (int i = 0; i < 120; i++)
            {
                TipoAtaqueTanque ataque = director.ElegirAtaque(
                    jefe.transform, jugador.transform, true, true, limites);
                if (ataque == TipoAtaqueTanque.Misil) aparecioMisil = true;
            }

            Verificar(aparecioMisil, "El misil nunca apareció durante la fase 2.");
            UnityEngine.Random.state = estadoAnterior;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(jefe);
            UnityEngine.Object.DestroyImmediate(jugador);
            UnityEngine.Object.DestroyImmediate(objetoLimites);
        }
    }

    private static void VerificarNivel1()
    {
        Scene escena = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        Verificar(escena.IsValid(), "No se pudo abrir Nivel 1.");

        ArenaJefe[] arenas = UnityEngine.Object.FindObjectsOfType<ArenaJefe>(true);
        Verificar(arenas.Length > 0, "Nivel 1 no contiene ArenaJefe.");

        foreach (ArenaJefe arena in arenas)
        {
            Verificar(arena.jefeTanque != null, "ArenaJefe no tiene Jefe asignado.");
            Verificar(arena.limitesArena != null, "ArenaJefe no tiene límites asignados.");
            Verificar(arena.camaraArena != null && arena.camaraJugador != null,
                "ArenaJefe no tiene sus cámaras asignadas.");
            Verificar(arena.camaraArena.GetComponent<CinemachineImpulseListener>() != null,
                "La cámara de arena no tiene receptor de sacudida.");
            Verificar(arena.jefeTanque.balaMetrallaPrefab != null &&
                AssetDatabase.GetAssetPath(arena.jefeTanque.balaMetrallaPrefab) == RutaBala,
                "La instancia del Jefe no utiliza BalaJefe para la metralla.");
        }
    }

    private static void Verificar(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception("[SIMULACIÓN JEFE] " + mensaje);
    }
}
