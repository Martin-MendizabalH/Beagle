using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prepara la infraestructura reutilizable de la batalla contra el Jefe Tanque
/// en el Nivel 3. No crea el trigger ni las puertas del encuentro.
/// </summary>
public static class ConfiguradorTrasladoJefeNivel3
{
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";
    private const string RutaNivel2 = "Assets/Escenas/Niveles/Nivel 2.unity";
    private const string RutaNivel3 = "Assets/Escenas/Niveles/Nivel 3.unity";
    private const string RutaSelectorNiveles = "Assets/Escenas/Menus/SelectorNiveles.unity";
    private const string RutaMenuPrincipal = "Assets/Escenas/Menus/MenuPrincipal.unity";
    private const string RutaPrefabJefe = "Assets/Prefabs/Jefes/Jefe_Tanque.prefab";
    private const string CarpetaInterfazJefes = "Assets/Prefabs/Interfaz/Jefes";
    private const string RutaPrefabInterfazJefe = CarpetaInterfazJefes + "/Canvas_UIJefe.prefab";

    private static readonly Vector3 CentroCamaraArena = new Vector3(193.5f, 3f, -10f);
    private static readonly Vector3 PosicionInicialJefe = new Vector3(193.5f, -0.8f, 0f);

    [MenuItem("Herramientas/Jefe Tanque/Preparar traslado al Nivel 3")]
    public static void PrepararTrasladoDesdeMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Preparar traslado del Jefe",
                "Se configurarán las cámaras, la interfaz y una instancia del Jefe en el Nivel 3. " +
                "También se actualizarán Build Settings y el selector de niveles.",
                "Preparar",
                "Cancelar"))
        {
            return;
        }

        PrepararTraslado();
    }

    /// <summary>
    /// Punto de entrada utilizado tanto desde el menú como desde línea de comandos.
    /// </summary>
    public static void PrepararTraslado()
    {
        try
        {
            VerificarAssetsNecesarios();
            CrearPrefabInterfazJefe();
            ConfigurarNivel3();
            ConfigurarSelectorNiveles();
            ConfigurarBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);

            Debug.Log(
                "[Traslado Jefe Nivel 3] Configuración completada. " +
                "Quedan pendientes el trigger, los bloqueos y la conexión de ArenaJefe.");
        }
        catch (Exception excepcion)
        {
            Debug.LogException(excepcion);
            throw;
        }
    }

    [MenuItem("Herramientas/Jefe Tanque/Validar preparación del Nivel 3")]
    public static void ValidarConfiguracion()
    {
        Scene nivel3 = EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
        GameObject jugador = BuscarObjetoEnEscena(nivel3, "Jugador");
        GameObject camaraPrincipal = BuscarObjetoEnEscena(nivel3, "Main Camera");
        GameObject camaraJugador = BuscarObjetoEnEscena(nivel3, "VCam_Jugador");
        GameObject camaraArena = BuscarObjetoEnEscena(nivel3, "VCam_ArenaJefe");
        GameObject interfaz = BuscarObjetoEnEscena(nivel3, "Canvas_UIJefe");
        GameObject jefe = BuscarObjetoEnEscena(nivel3, "Jefe_Tanque");
        GameObject raizEncuentro = BuscarObjetoEnEscena(nivel3, "Encuentro_JefeTanque");

        Exigir(jugador != null, "Falta el Jugador.");
        Exigir(camaraPrincipal != null, "Falta Main Camera.");
        Exigir(camaraJugador != null, "Falta VCam_Jugador.");
        Exigir(camaraArena != null, "Falta VCam_ArenaJefe.");
        Exigir(interfaz != null, "Falta Canvas_UIJefe.");
        Exigir(jefe != null, "Falta la instancia Jefe_Tanque.");
        Exigir(raizEncuentro != null, "Falta Encuentro_JefeTanque.");

        Exigir(
            camaraPrincipal.GetComponent<CinemachineBrain>() != null,
            "Main Camera no tiene CinemachineBrain.");
        Exigir(
            camaraPrincipal.GetComponent<CamaraSeguimiento>() == null ||
            !camaraPrincipal.GetComponent<CamaraSeguimiento>().enabled,
            "CamaraSeguimiento continúa activa.");

        CinemachineVirtualCamera virtualJugador =
            camaraJugador.GetComponent<CinemachineVirtualCamera>();
        CinemachineVirtualCamera virtualArena =
            camaraArena.GetComponent<CinemachineVirtualCamera>();
        LimitesArenaJefe limites = camaraArena.GetComponent<LimitesArenaJefe>();

        Exigir(
            virtualJugador != null && virtualJugador.Follow == jugador.transform,
            "VCam_Jugador no sigue al Jugador del Nivel 3.");
        Exigir(
            camaraJugador.GetComponent<CinemachineConfiner2D>() == null,
            "VCam_Jugador conserva el confiner del Nivel 1.");
        Exigir(virtualArena != null && virtualArena.Follow == null, "VCam_ArenaJefe no es fija.");
        Exigir(!camaraArena.activeSelf, "VCam_ArenaJefe debe comenzar desactivada.");
        Exigir(
            limites != null &&
            Mathf.Approximately(limites.ancho, 23f) &&
            Mathf.Approximately(camaraArena.transform.position.x, CentroCamaraArena.x),
            "Los límites o el centro de la arena no coinciden con la geometría creada.");

        JefeTanqueController controladorJefe = jefe.GetComponent<JefeTanqueController>();
        SaludJefe saludJefe = jefe.GetComponent<SaludJefe>();
        Exigir(
            controladorJefe != null && !controladorJefe.enabled,
            "El controlador del Jefe debe comenzar desactivado.");
        Exigir(
            saludJefe != null && !saludJefe.esVulnerable,
            "El Jefe debe comenzar invulnerable.");
        Exigir(
            jefe.transform.parent == raizEncuentro.transform,
            "El Jefe no está organizado dentro de Encuentro_JefeTanque.");

        BarraVidaJefe barra = interfaz.GetComponent<BarraVidaJefe>();
        SerializedObject barraSerializada = barra != null ? new SerializedObject(barra) : null;
        Exigir(
            barra != null &&
            barraSerializada.FindProperty("barraRoja").objectReferenceValue != null &&
            barraSerializada.FindProperty("barraBlanca").objectReferenceValue != null,
            "La interfaz perdió referencias internas.");
        Exigir(!interfaz.activeSelf, "Canvas_UIJefe debe comenzar desactivado.");

        string[] escenasBuild = EditorBuildSettings.scenes
            .Where(escena => escena.enabled)
            .Select(escena => escena.path)
            .ToArray();
        Exigir(escenasBuild.Contains(RutaNivel1), "Nivel 1 no está en Build Settings.");
        Exigir(escenasBuild.Contains(RutaNivel2), "Nivel 2 no está en Build Settings.");
        Exigir(escenasBuild.Contains(RutaNivel3), "Nivel 3 no está en Build Settings.");

        Scene selector = EditorSceneManager.OpenScene(RutaSelectorNiveles, OpenSceneMode.Single);
        MapMovement mapa = BuscarComponenteEnEscena<MapMovement>(selector);
        Exigir(
            mapa != null &&
            mapa.nombresEscenas.SequenceEqual(new[] { "Nivel 1", "Nivel 2", "Nivel 3" }),
            "Los nombres de escenas del selector no coinciden con los archivos reales.");

        EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
        Debug.Log("[Traslado Jefe Nivel 3] Validación completada sin errores.");
    }

    private static void VerificarAssetsNecesarios()
    {
        string[] rutasNecesarias =
        {
            RutaNivel1,
            RutaNivel2,
            RutaNivel3,
            RutaSelectorNiveles,
            RutaMenuPrincipal,
            RutaPrefabJefe
        };

        foreach (string ruta in rutasNecesarias)
        {
            if (AssetDatabase.LoadMainAssetAtPath(ruta) == null)
                throw new InvalidOperationException("No se encontró el asset necesario: " + ruta);
        }
    }

    private static void CrearPrefabInterfazJefe()
    {
        CrearCarpetaSiNoExiste("Assets/Prefabs/Interfaz");
        CrearCarpetaSiNoExiste(CarpetaInterfazJefes);

        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        GameObject canvasOriginal = BuscarObjetoEnEscena(nivel1, "Canvas_UIJefe");

        if (canvasOriginal == null)
            throw new InvalidOperationException("No se encontró Canvas_UIJefe en el Nivel 1.");

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasOriginal, RutaPrefabInterfazJefe);
        if (prefab == null)
            throw new InvalidOperationException("Unity no pudo crear el prefab Canvas_UIJefe.");

        BarraVidaJefe barra = prefab.GetComponent<BarraVidaJefe>();
        SerializedObject barraSerializada = barra != null ? new SerializedObject(barra) : null;
        SerializedProperty barraRoja = barraSerializada?.FindProperty("barraRoja");
        SerializedProperty barraBlanca = barraSerializada?.FindProperty("barraBlanca");

        if (barra == null ||
            barraRoja == null ||
            barraBlanca == null ||
            barraRoja.objectReferenceValue == null ||
            barraBlanca.objectReferenceValue == null)
            throw new InvalidOperationException(
                "El prefab Canvas_UIJefe no conservó correctamente las referencias de sus barras.");
    }

    private static void ConfigurarNivel3()
    {
        Scene nivel3 = EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);

        GameObject jugador = BuscarObjetoEnEscena(nivel3, "Jugador");
        GameObject camaraPrincipal = BuscarObjetoEnEscena(nivel3, "Main Camera");

        if (jugador == null)
            throw new InvalidOperationException("No se encontró el Jugador en el Nivel 3.");

        if (camaraPrincipal == null)
            throw new InvalidOperationException("No se encontró Main Camera en el Nivel 3.");

        ConfigurarCamaraPrincipal(camaraPrincipal);

        EliminarObjetoSiExiste(nivel3, "VCam_Jugador");
        EliminarObjetoSiExiste(nivel3, "VCam_ArenaJefe");
        EliminarObjetoSiExiste(nivel3, "Canvas_UIJefe");
        EliminarObjetoSiExiste(nivel3, "Encuentro_JefeTanque");

        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Additive);
        GameObject camaraJugadorOriginal = BuscarObjetoEnEscena(nivel1, "VCam_Jugador");
        GameObject camaraArenaOriginal = BuscarObjetoEnEscena(nivel1, "VCam_ArenaJefe");

        if (camaraJugadorOriginal == null || camaraArenaOriginal == null)
            throw new InvalidOperationException(
                "No se encontraron las cámaras virtuales de referencia en el Nivel 1.");

        GameObject camaraJugador = UnityEngine.Object.Instantiate(camaraJugadorOriginal);
        GameObject camaraArena = UnityEngine.Object.Instantiate(camaraArenaOriginal);
        camaraJugador.name = "VCam_Jugador";
        camaraArena.name = "VCam_ArenaJefe";

        SceneManager.MoveGameObjectToScene(camaraJugador, nivel3);
        SceneManager.MoveGameObjectToScene(camaraArena, nivel3);
        EditorSceneManager.CloseScene(nivel1, true);

        ConfigurarCamaraJugador(camaraJugador, jugador);
        ConfigurarCamaraArena(camaraArena);

        GameObject raizEncuentro = new GameObject("Encuentro_JefeTanque");
        SceneManager.MoveGameObjectToScene(raizEncuentro, nivel3);

        camaraArena.transform.SetParent(raizEncuentro.transform, true);

        GameObject jefe = InstanciarPrefabEnEscena(RutaPrefabJefe, nivel3);
        jefe.name = "Jefe_Tanque";
        jefe.transform.SetParent(raizEncuentro.transform, true);
        jefe.transform.position = PosicionInicialJefe;

        JefeTanqueController controladorJefe = jefe.GetComponent<JefeTanqueController>();
        SaludJefe saludJefe = jefe.GetComponent<SaludJefe>();

        if (controladorJefe == null || saludJefe == null)
            throw new InvalidOperationException(
                "El prefab Jefe_Tanque no contiene sus componentes principales.");

        controladorJefe.enabled = false;
        saludJefe.esVulnerable = false;

        GameObject interfaz = InstanciarPrefabEnEscena(RutaPrefabInterfazJefe, nivel3);
        interfaz.name = "Canvas_UIJefe";
        interfaz.SetActive(false);

        EditorUtility.SetDirty(camaraPrincipal);
        EditorUtility.SetDirty(camaraJugador);
        EditorUtility.SetDirty(camaraArena);
        EditorUtility.SetDirty(jefe);
        EditorUtility.SetDirty(interfaz);
        EditorSceneManager.MarkSceneDirty(nivel3);
        EditorSceneManager.SaveScene(nivel3);
    }

    private static void ConfigurarCamaraPrincipal(GameObject camaraPrincipal)
    {
        CamaraSeguimiento seguimientoAnterior = camaraPrincipal.GetComponent<CamaraSeguimiento>();
        if (seguimientoAnterior != null)
            seguimientoAnterior.enabled = false;

        CinemachineBrain cerebro = camaraPrincipal.GetComponent<CinemachineBrain>();
        if (cerebro == null)
            cerebro = camaraPrincipal.AddComponent<CinemachineBrain>();

        cerebro.m_DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Style.EaseInOut,
            2f);
        cerebro.enabled = true;
    }

    private static void ConfigurarCamaraJugador(GameObject camaraJugador, GameObject jugador)
    {
        CinemachineVirtualCamera virtualCamera =
            camaraJugador.GetComponent<CinemachineVirtualCamera>();

        if (virtualCamera == null)
            throw new InvalidOperationException("VCam_Jugador no contiene CinemachineVirtualCamera.");

        virtualCamera.Follow = jugador.transform;
        virtualCamera.LookAt = null;
        virtualCamera.m_Lens.OrthographicSize = 5.45f;

        CinemachineConfiner2D confinerAnterior =
            camaraJugador.GetComponent<CinemachineConfiner2D>();
        if (confinerAnterior != null)
            UnityEngine.Object.DestroyImmediate(confinerAnterior);

        camaraJugador.transform.SetParent(null);
        camaraJugador.transform.position =
            new Vector3(jugador.transform.position.x, jugador.transform.position.y, -10f);
        camaraJugador.SetActive(true);

        SacudidaCamaraJefe.PrepararReceptor(camaraJugador);
    }

    private static void ConfigurarCamaraArena(GameObject camaraArena)
    {
        CinemachineVirtualCamera virtualCamera =
            camaraArena.GetComponent<CinemachineVirtualCamera>();

        if (virtualCamera == null)
            throw new InvalidOperationException("VCam_ArenaJefe no contiene CinemachineVirtualCamera.");

        virtualCamera.Follow = null;
        virtualCamera.LookAt = null;
        virtualCamera.m_Lens.OrthographicSize = 5.45f;

        camaraArena.transform.position = CentroCamaraArena;
        camaraArena.SetActive(false);

        LimitesArenaJefe limites = camaraArena.GetComponent<LimitesArenaJefe>();
        if (limites == null)
            limites = camaraArena.AddComponent<LimitesArenaJefe>();

        limites.ancho = 23f;
        limites.margenInterior = 0.8f;
        limites.alturaGizmo = 10f;

        SacudidaCamaraJefe.PrepararReceptor(camaraArena);
    }

    private static void ConfigurarSelectorNiveles()
    {
        Scene selector = EditorSceneManager.OpenScene(RutaSelectorNiveles, OpenSceneMode.Single);
        MapMovement mapa = BuscarComponenteEnEscena<MapMovement>(selector);

        if (mapa == null)
            throw new InvalidOperationException(
                "No se encontró MapMovement en la escena SelectorNiveles.");

        mapa.nombresEscenas = new[] { "Nivel 1", "Nivel 2", "Nivel 3" };
        EditorUtility.SetDirty(mapa);
        EditorSceneManager.MarkSceneDirty(selector);
        EditorSceneManager.SaveScene(selector);
    }

    private static void ConfigurarBuildSettings()
    {
        string[] ordenDeseado =
        {
            RutaMenuPrincipal,
            RutaSelectorNiveles,
            RutaNivel1,
            RutaNivel2,
            RutaNivel3
        };

        Dictionary<string, EditorBuildSettingsScene> escenasActuales =
            EditorBuildSettings.scenes.ToDictionary(escena => escena.path, escena => escena);

        List<EditorBuildSettingsScene> escenasOrdenadas = new List<EditorBuildSettingsScene>();

        foreach (string ruta in ordenDeseado)
        {
            escenasOrdenadas.Add(
                escenasActuales.TryGetValue(ruta, out EditorBuildSettingsScene escena)
                    ? new EditorBuildSettingsScene(escena.path, true)
                    : new EditorBuildSettingsScene(ruta, true));
        }

        foreach (EditorBuildSettingsScene escena in EditorBuildSettings.scenes)
        {
            if (!ordenDeseado.Contains(escena.path))
                escenasOrdenadas.Add(escena);
        }

        EditorBuildSettings.scenes = escenasOrdenadas.ToArray();
    }

    private static GameObject InstanciarPrefabEnEscena(string ruta, Scene escena)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null)
            throw new InvalidOperationException("No se encontró el prefab: " + ruta);

        GameObject instancia = PrefabUtility.InstantiatePrefab(prefab, escena) as GameObject;
        if (instancia == null)
            throw new InvalidOperationException("No se pudo instanciar el prefab: " + ruta);

        return instancia;
    }

    private static GameObject BuscarObjetoEnEscena(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            Transform encontrado = raiz
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transformacion => transformacion.name == nombre);

            if (encontrado != null)
                return encontrado.gameObject;
        }

        return null;
    }

    private static T BuscarComponenteEnEscena<T>(Scene escena) where T : Component
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            T componente = raiz.GetComponentInChildren<T>(true);
            if (componente != null)
                return componente;
        }

        return null;
    }

    private static void EliminarObjetoSiExiste(Scene escena, string nombre)
    {
        GameObject existente = BuscarObjetoEnEscena(escena, nombre);
        if (existente != null)
            UnityEngine.Object.DestroyImmediate(existente);
    }

    private static void CrearCarpetaSiNoExiste(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta))
            return;

        string carpetaPadre = ruta.Substring(0, ruta.LastIndexOf('/'));
        string nombreCarpeta = ruta.Substring(ruta.LastIndexOf('/') + 1);

        if (!AssetDatabase.IsValidFolder(carpetaPadre))
            CrearCarpetaSiNoExiste(carpetaPadre);

        AssetDatabase.CreateFolder(carpetaPadre, nombreCarpeta);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(
                "[Traslado Jefe Nivel 3] Validación fallida: " + mensaje);
    }
}
