using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Instala el inventario de armas del Nivel 1 como prefab reutilizable en el
/// Nivel 3 y prepara los canales de audio editables del Jefe Tanque.
/// </summary>
public static class ConfiguradorInventarioYSonidosNivel3
{
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";
    private const string RutaNivel3 = "Assets/Escenas/Niveles/Nivel 3.unity";
    private const string CarpetaInterfazJugador = "Assets/Prefabs/Interfaz/Jugador";
    private const string RutaPrefabInventario =
        CarpetaInterfazJugador + "/UI_InventarioArmas.prefab";
    private const string RutaPrefabJefe = "Assets/Prefabs/Jefes/Jefe_Tanque.prefab";
    private const string CarpetaAudioJefe = "Assets/Audio/Jefes/Tanque";

    [MenuItem("Herramientas/Proyecto Beagle/Preparar inventario y sonidos del Nivel 3")]
    public static void Aplicar()
    {
        CrearCarpetaSiNoExiste(CarpetaInterfazJugador);
        CrearCarpetaSiNoExiste(CarpetaAudioJefe);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        GameObject prefabInventario = CrearPrefabInventarioDesdeNivel1();
        ConfigurarAudioEnPrefabJefe();
        InstalarInventarioEnNivel3(prefabInventario);
        Validar();

        Debug.Log(
            "[NIVEL 3] Inventario instalado y biblioteca sonora del Jefe asignada.");
    }

    [MenuItem("Herramientas/Proyecto Beagle/Validar inventario y sonidos del Nivel 3")]
    public static void Validar()
    {
        Scene nivel3 = EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
        GameObject jugador = BuscarObjetoEnEscena(nivel3, "Jugador");
        GameObject interfaz = BuscarObjetoEnEscena(nivel3, "UI_InventarioArmas");
        GameObject jefe = BuscarObjetoEnEscena(nivel3, "Jefe_Tanque");

        Exigir(jugador != null, "No se encontró el Jugador.");
        Exigir(interfaz != null && interfaz.activeSelf,
            "La interfaz del inventario no está activa.");
        Exigir(interfaz.GetComponent<Canvas>() != null,
            "UI_InventarioArmas no contiene un Canvas.");

        InventarioArmas inventario =
            jugador.GetComponentInChildren<InventarioArmas>(true);
        Exigir(inventario != null, "El Jugador no contiene InventarioArmas.");
        Exigir(inventario.armasDisponibles != null &&
            inventario.armasDisponibles.Length > 0,
            "El inventario no contiene armas.");
        Exigir(inventario.iconoArmaEquipada != null,
            "Falta conectar el icono del arma equipada.");
        Exigir(inventario.animatorUI != null,
            "Falta conectar el Animator del inventario.");
        Exigir(inventario.animatorUI.runtimeAnimatorController != null,
            "El Animator del inventario no tiene controlador.");

        Exigir(jefe != null, "No se encontró el Jefe_Tanque.");
        SonidosJefeTanque sonidos = jefe.GetComponent<SonidosJefeTanque>();
        Exigir(sonidos != null, "El Jefe no contiene SonidosJefeTanque.");
        Exigir(sonidos.FuenteEfectos != null,
            "Falta el canal de efectos del Jefe.");
        Exigir(sonidos.FuenteMovimiento != null,
            "Falta el canal de movimiento del Jefe.");
        Exigir(sonidos.FuenteAtaques != null,
            "Falta el canal de ataques del Jefe.");
        Exigir(sonidos.FuenteEfectos != sonidos.FuenteMovimiento &&
            sonidos.FuenteEfectos != sonidos.FuenteAtaques &&
            sonidos.FuenteMovimiento != sonidos.FuenteAtaques,
            "Los tres canales del Jefe deben usar AudioSource diferentes.");
        Exigir(!sonidos.FuenteEfectos.playOnAwake &&
            !sonidos.FuenteMovimiento.playOnAwake &&
            !sonidos.FuenteAtaques.playOnAwake,
            "Ningún sonido del Jefe debe reproducirse al cargar la escena.");
        Exigir(
            sonidos.sonidoActivacion != null &&
            sonidos.sonidoMovimiento != null &&
            sonidos.sonidoAnticipoMetralla != null &&
            sonidos.sonidoDisparoMetralla != null &&
            sonidos.sonidoImpactoMetralla != null &&
            sonidos.sonidoAnticipoLaser != null &&
            sonidos.sonidoLaser != null &&
            sonidos.sonidoFinLaser != null &&
            sonidos.sonidoAnticipoEmbestida != null &&
            sonidos.sonidoEmbestida != null &&
            sonidos.sonidoImpactoPared != null &&
            sonidos.sonidoAnticipoMisil != null &&
            sonidos.sonidoLanzamientoMisil != null &&
            sonidos.sonidoExplosionMisil != null &&
            sonidos.sonidoRecibirDano != null &&
            sonidos.sonidoTransicionFase != null &&
            sonidos.sonidoMuerte != null,
            "Hay sonidos generados que todavía no están asignados.");

        Debug.Log(
            "[NIVEL 3] Validación correcta: inventario y 17 sonidos asignados.");
    }

    private static GameObject CrearPrefabInventarioDesdeNivel1()
    {
        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        GameObject interfazOriginal =
            BuscarObjetoEnEscena(nivel1, "UI_InventarioArmas");
        Exigir(interfazOriginal != null,
            "No se encontró UI_InventarioArmas en el Nivel 1.");

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(interfazOriginal, RutaPrefabInventario);
        Exigir(prefab != null, "No se pudo crear el prefab del inventario.");
        return prefab;
    }

    private static void InstalarInventarioEnNivel3(GameObject prefabInventario)
    {
        Scene nivel3 = EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
        GameObject jugador = BuscarObjetoEnEscena(nivel3, "Jugador");
        Exigir(jugador != null, "No se encontró el Jugador en el Nivel 3.");

        GameObject interfaz =
            BuscarObjetoEnEscena(nivel3, "UI_InventarioArmas");
        if (interfaz == null)
        {
            interfaz = PrefabUtility.InstantiatePrefab(
                prefabInventario, nivel3) as GameObject;
            Exigir(interfaz != null,
                "No se pudo instanciar UI_InventarioArmas.");
            interfaz.name = "UI_InventarioArmas";
        }

        InventarioArmas inventario =
            jugador.GetComponentInChildren<InventarioArmas>(true);
        Exigir(inventario != null,
            "El Jugador no contiene el componente InventarioArmas.");

        Image icono = BuscarComponentePorNombre<Image>(
            interfaz, "IconoArmaEquipada");
        Animator animator = BuscarComponentePorNombre<Animator>(
            interfaz, "UI_Fondo");
        Exigir(icono != null,
            "El prefab del inventario no contiene IconoArmaEquipada.");
        Exigir(animator != null,
            "El prefab del inventario no contiene el Animator de UI_Fondo.");

        inventario.iconoArmaEquipada = icono;
        inventario.animatorUI = animator;
        interfaz.SetActive(true);

        EditorUtility.SetDirty(inventario);
        EditorUtility.SetDirty(interfaz);
        EditorSceneManager.MarkSceneDirty(nivel3);
        EditorSceneManager.SaveScene(nivel3);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigurarAudioEnPrefabJefe()
    {
        GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabJefe);
        try
        {
            SonidosJefeTanque sonidos = contenido.GetComponent<SonidosJefeTanque>();
            if (sonidos == null) sonidos = contenido.AddComponent<SonidosJefeTanque>();

            AudioSource[] fuentes = contenido.GetComponents<AudioSource>();
            while (fuentes.Length < 3)
            {
                contenido.AddComponent<AudioSource>();
                fuentes = contenido.GetComponents<AudioSource>();
            }

            sonidos.ConfigurarFuentes(fuentes[0], fuentes[1], fuentes[2]);
            sonidos.sonidoActivacion = CargarAudio("Jefe_Activacion.wav");
            sonidos.sonidoMovimiento = CargarAudio("Jefe_Movimiento_Bucle.wav");
            sonidos.sonidoAnticipoMetralla =
                CargarAudio("Jefe_Metralla_Anticipo.wav");
            sonidos.sonidoDisparoMetralla =
                CargarAudio("Jefe_Metralla_Disparo.wav");
            sonidos.sonidoImpactoMetralla =
                CargarAudio("Jefe_Metralla_Impacto.wav");
            sonidos.sonidoAnticipoLaser =
                CargarAudio("Jefe_Laser_Anticipo.wav");
            sonidos.sonidoLaser = CargarAudio("Jefe_Laser_Bucle.wav");
            sonidos.sonidoFinLaser = CargarAudio("Jefe_Laser_Final.wav");
            sonidos.sonidoAnticipoEmbestida =
                CargarAudio("Jefe_Embestida_Anticipo.wav");
            sonidos.sonidoEmbestida =
                CargarAudio("Jefe_Embestida_Bucle.wav");
            sonidos.sonidoImpactoPared =
                CargarAudio("Jefe_Embestida_ImpactoPared.wav");
            sonidos.sonidoAnticipoMisil =
                CargarAudio("Jefe_Misil_Anticipo.wav");
            sonidos.sonidoLanzamientoMisil =
                CargarAudio("Jefe_Misil_Lanzamiento.wav");
            sonidos.sonidoExplosionMisil =
                CargarAudio("Jefe_Misil_Explosion.wav");
            sonidos.sonidoRecibirDano = CargarAudio("Jefe_Dano.wav");
            sonidos.sonidoTransicionFase =
                CargarAudio("Jefe_Fase2_Transicion.wav");
            sonidos.sonidoMuerte = CargarAudio("Jefe_Muerte.wav");
            EditorUtility.SetDirty(sonidos);
            foreach (AudioSource fuente in fuentes) EditorUtility.SetDirty(fuente);

            PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabJefe);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contenido);
        }

        AssetDatabase.SaveAssets();
    }

    private static AudioClip CargarAudio(string nombreArchivo)
    {
        string ruta = CarpetaAudioJefe + "/" + nombreArchivo;
        AudioImporter importador = AssetImporter.GetAtPath(ruta) as AudioImporter;
        Exigir(importador != null, "No se pudo importar el audio: " + ruta);

        AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
        ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
        ajustes.compressionFormat = AudioCompressionFormat.PCM;
        ajustes.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        ajustes.preloadAudioData = true;
        importador.defaultSampleSettings = ajustes;
        importador.forceToMono = true;
        importador.loadInBackground = false;

        SerializedObject importadorSerializado = new SerializedObject(importador);
        SerializedProperty normalizar =
            importadorSerializado.FindProperty("m_Normalize") ??
            importadorSerializado.FindProperty("normalize");
        if (normalizar != null)
        {
            normalizar.boolValue = false;
            importadorSerializado.ApplyModifiedPropertiesWithoutUndo();
        }
        importador.SaveAndReimport();

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        Exigir(clip != null, "No se pudo cargar el AudioClip: " + ruta);
        return clip;
    }

    private static T BuscarComponentePorNombre<T>(GameObject raiz, string nombre)
        where T : Component
    {
        return raiz.GetComponentsInChildren<T>(true)
            .FirstOrDefault(componente => componente.name == nombre);
    }

    private static GameObject BuscarObjetoEnEscena(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            Transform encontrado = raiz.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transformacion => transformacion.name == nombre);
            if (encontrado != null) return encontrado.gameObject;
        }

        return null;
    }

    private static void CrearCarpetaSiNoExiste(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        int separador = ruta.LastIndexOf('/');
        Exigir(separador > 0, "Ruta de carpeta inválida: " + ruta);
        string padre = ruta.Substring(0, separador);
        string nombre = ruta.Substring(separador + 1);
        if (!AssetDatabase.IsValidFolder(padre)) CrearCarpetaSiNoExiste(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException("[NIVEL 3] " + mensaje);
    }
}
