using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aplica de forma repetible la configuración base del Jefe y de su arena.
/// Puede ejecutarse varias veces sin duplicar componentes.
/// </summary>
public static class ConfiguradorPulidoJefe
{
    private const string RutaPrefabJefe = "Assets/Prefabs/Jefes/Jefe_Tanque.prefab";
    private const string RutaPrefabBalaJefe =
        "Assets/Prefabs/Proyectiles/Jefes/BalaJefe.prefab";
    private const string RutaPrefabMisil =
        "Assets/Prefabs/Jefes/MisilTeledirigido.prefab";
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";
    private const string NombreCapaProyectilEnemigo = "ProyectilEnemigo";

    [MenuItem("Herramientas/Proyecto Beagle/Configurar pulido del Jefe")]
    public static void AplicarConfiguracion()
    {
        GameObject balaJefe =
            AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabBalaJefe);
        if (balaJefe == null)
        {
            Debug.LogError("[CONFIGURADOR JEFE] No se encontró el prefab BalaJefe.");
            return;
        }

        int capaProyectilEnemigo = ObtenerOCrearCapa(NombreCapaProyectilEnemigo);
        if (capaProyectilEnemigo < 0)
        {
            Debug.LogError("[CONFIGURADOR JEFE] No hay una capa libre para ProyectilEnemigo.");
            return;
        }
        ConfigurarColisionesProyectil(capaProyectilEnemigo);

        ConfigurarPrefabJefe(balaJefe);
        ConfigurarPrefabBala(capaProyectilEnemigo);
        ConfigurarPrefabMisil(capaProyectilEnemigo);
        ConfigurarNivel1(balaJefe);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CONFIGURADOR JEFE] Configuración aplicada correctamente.");
    }

    private static void ConfigurarPrefabJefe(GameObject balaJefe)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabJefe);
        try
        {
            raiz.transform.localPosition = Vector3.zero;
            raiz.transform.localRotation = Quaternion.identity;
            raiz.transform.localScale = Vector3.one * 0.7f;

            JefeTanqueController controlador = raiz.GetComponent<JefeTanqueController>();
            if (controlador == null)
                controlador = raiz.AddComponent<JefeTanqueController>();

            if (raiz.GetComponent<DirectorAtaquesJefe>() == null)
                raiz.AddComponent<DirectorAtaquesJefe>();
            if (raiz.GetComponent<EfectosVisualesJefeTanque>() == null)
                raiz.AddComponent<EfectosVisualesJefeTanque>();
            if (raiz.GetComponent<SacudidaCamaraJefe>() == null)
                raiz.AddComponent<SacudidaCamaraJefe>();
            if (raiz.GetComponent<EstadoVisualJefe>() == null)
                raiz.AddComponent<EstadoVisualJefe>();
            if (raiz.GetComponent<PoolBalasMetrallaJefe>() == null)
                raiz.AddComponent<PoolBalasMetrallaJefe>();
            if (raiz.GetComponent<CinemachineImpulseSource>() == null)
                raiz.AddComponent<CinemachineImpulseSource>();

            controlador.balaMetrallaPrefab = balaJefe;
            controlador.velocidadMovimiento = 2f;
            controlador.cantidadBalasMetralla = 10;
            controlador.anchoDeLaArena = 23f;
            controlador.intervaloLanzamientoMetralla = 0.04f;
            controlador.tiempoMantenimientoLaser = 0.75f;
            controlador.mascaraEntorno = LayerMask.GetMask("Suelo");

            Rigidbody2D cuerpo = raiz.GetComponent<Rigidbody2D>();
            if (cuerpo != null)
            {
                cuerpo.mass = 8f;
                cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            SaludJefe salud = raiz.GetComponent<SaludJefe>();
            if (salud != null)
            {
                salud.vidaMaxima = 400;
                salud.umbralFase2 = 0.5f;
                salud.esVulnerable = false;
            }

            PuntoCritico puntoCritico = raiz.GetComponentInChildren<PuntoCritico>(true);
            if (puntoCritico != null) puntoCritico.multiplicador = 2;

            BotinMonedas botin = raiz.GetComponent<BotinMonedas>();
            if (botin != null)
            {
                SerializedObject botinSerializado = new SerializedObject(botin);
                botinSerializado.FindProperty("cantidadMinima").intValue = 8;
                botinSerializado.FindProperty("cantidadMaxima").intValue = 12;
                botinSerializado.FindProperty("valorPorMoneda").intValue = 5;
                botinSerializado.FindProperty("dispersionHorizontal").floatValue = 5f;
                botinSerializado.FindProperty("impulsoVertical").floatValue = 5.5f;
                botinSerializado.ApplyModifiedPropertiesWithoutUndo();
            }

            controlador.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabJefe);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static void ConfigurarPrefabBala(int capaProyectilEnemigo)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabBalaJefe);
        try
        {
            raiz.layer = capaProyectilEnemigo;

            Rigidbody2D cuerpo = raiz.GetComponent<Rigidbody2D>();
            if (cuerpo != null)
            {
                cuerpo.gravityScale = 1.5f;
                cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpo.constraints = RigidbodyConstraints2D.None;
            }

            if (raiz.GetComponent<MovimientoProyectil>() == null)
                raiz.AddComponent<MovimientoProyectil>();

            BalaEnemiga bala = raiz.GetComponent<BalaEnemiga>();
            if (bala != null)
            {
                bala.danoAlSerDesviada = 10;
                bala.tiempoVida = 6f;
                bala.persistenciaVisualImpacto = 0.04f;
            }

            int capasPermitidas = LayerMask.GetMask("Default", "Suelo", "Jugador");
            foreach (Collider2D hitbox in raiz.GetComponentsInChildren<Collider2D>(true))
            {
                hitbox.includeLayers = 0;
                hitbox.excludeLayers = unchecked((int)~capasPermitidas);
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabBalaJefe);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static void ConfigurarPrefabMisil(int capaProyectilEnemigo)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabMisil);
        try
        {
            raiz.layer = capaProyectilEnemigo;

            Rigidbody2D cuerpo = raiz.GetComponent<Rigidbody2D>();
            if (cuerpo != null)
            {
                cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            MisilTeledirigido misil = raiz.GetComponent<MisilTeledirigido>();
            if (misil != null)
            {
                misil.danoAlSerDesviado = 25;
                misil.velocidadSalidaVertical = 6f;
                misil.duracionSalidaVertical = 0.35f;
            }

            int capasPermitidas = LayerMask.GetMask("Default", "Suelo", "Jugador");
            foreach (Collider2D hitbox in raiz.GetComponentsInChildren<Collider2D>(true))
            {
                hitbox.includeLayers = 0;
                hitbox.excludeLayers = unchecked((int)~capasPermitidas);
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabMisil);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static void ConfigurarNivel1(GameObject balaJefe)
    {
        Scene escena = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        ArenaJefe[] arenas = Object.FindObjectsOfType<ArenaJefe>(true);

        if (arenas.Length == 0)
        {
            Debug.LogError("[CONFIGURADOR JEFE] Nivel 1 no contiene ArenaJefe.");
            return;
        }

        foreach (ArenaJefe arena in arenas)
        {
            if (arena.camaraArena != null)
            {
                LimitesArenaJefe limites =
                    arena.camaraArena.GetComponent<LimitesArenaJefe>();
                if (limites == null)
                    limites = arena.camaraArena.AddComponent<LimitesArenaJefe>();

                limites.ancho = 23f;
                limites.margenInterior = 0.8f;
                limites.alturaGizmo = 10f;
                arena.limitesArena = limites;
                SacudidaCamaraJefe.PrepararReceptor(arena.camaraArena);
            }

            SacudidaCamaraJefe.PrepararReceptor(arena.camaraJugador);

            if (arena.jefeTanque != null)
            {
                arena.jefeTanque.balaMetrallaPrefab = balaJefe;
                arena.jefeTanque.mascaraEntorno = LayerMask.GetMask("Suelo");

                Rigidbody2D cuerpo = arena.jefeTanque.GetComponent<Rigidbody2D>();
                if (cuerpo != null)
                {
                    cuerpo.mass = 8f;
                    cuerpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                }
            }

            EditorUtility.SetDirty(arena);
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    private static int ObtenerOCrearCapa(string nombre)
    {
        int existente = LayerMask.NameToLayer(nombre);
        if (existente >= 0) return existente;

        Object configuracionTags =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject serializado = new SerializedObject(configuracionTags);
        SerializedProperty capas = serializado.FindProperty("layers");

        for (int indice = 8; indice < capas.arraySize; indice++)
        {
            SerializedProperty capa = capas.GetArrayElementAtIndex(indice);
            if (!string.IsNullOrEmpty(capa.stringValue)) continue;

            capa.stringValue = nombre;
            serializado.ApplyModifiedProperties();
            EditorUtility.SetDirty(configuracionTags);
            return indice;
        }

        return -1;
    }

    private static void ConfigurarColisionesProyectil(int capaProyectil)
    {
        for (int capa = 0; capa < 32; capa++)
            Physics2D.IgnoreLayerCollision(capaProyectil, capa, true);

        int[] capasPermitidas =
        {
            LayerMask.NameToLayer("Default"),
            LayerMask.NameToLayer("Suelo"),
            LayerMask.NameToLayer("Jugador")
        };

        foreach (int capa in capasPermitidas)
        {
            if (capa >= 0)
                Physics2D.IgnoreLayerCollision(capaProyectil, capa, false);
        }

        Object configuracionFisica =
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0];
        EditorUtility.SetDirty(configuracionFisica);
    }
}
