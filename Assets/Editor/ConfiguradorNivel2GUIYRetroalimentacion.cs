using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Deja el Nivel 2 con el HUD reutilizable del jugador, inventario, contador de
/// monedas y destello de daño en sus enemigos, sin modificar botín ni IA.
/// </summary>
public static class ConfiguradorNivel2GUIYRetroalimentacion
{
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";
    private const string RutaNivel2 = "Assets/Escenas/Niveles/Nivel 2.unity";
    private const string RutaPrefabHud =
        "Assets/Prefabs/Interfaz/Jugador/Canvas_HUDJugador.prefab";
    private const string RutaPrefabInventario =
        "Assets/Prefabs/Interfaz/Jugador/UI_InventarioArmas.prefab";
    private const string RutaMaterialSilueta =
        "Assets/Arte/MaterialSiluetaDano.mat";

    private static readonly string[] RutasPrefabsEnemigos =
    {
        "Assets/Prefabs/Enemigos/Enemigo_Golem.prefab",
        "Assets/Prefabs/Enemigos/Enemigo_Volador.prefab",
        "Assets/Prefabs/Enemigos/Enemigo_Golem_Horda.prefab",
        "Assets/Prefabs/Enemigos/Enemigo_Volador_Horda.prefab"
    };

    [MenuItem("Herramientas/Proyecto Beagle/Configurar GUI y dano del Nivel 2")]
    public static void Aplicar()
    {
        Material materialSilueta = AssetDatabase.LoadAssetAtPath<Material>(
            RutaMaterialSilueta);
        Exigir(materialSilueta != null,
            "No se encontro el material de silueta de dano.");

        ConfigurarPrefabsEnemigos(materialSilueta);
        ConfigurarEscenaNivel2(materialSilueta);
        Validar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NIVEL 2] GUI y retroalimentacion de dano configuradas.");
    }

    [MenuItem("Herramientas/Proyecto Beagle/Validar GUI y dano del Nivel 2")]
    public static void Validar()
    {
        Scene nivel2 = EditorSceneManager.OpenScene(RutaNivel2,
            OpenSceneMode.Single);
        GameObject jugador = BuscarObjeto(nivel2, "Jugador");
        GameObject hud = BuscarObjeto(nivel2, "Canvas_HUDJugador");
        GameObject inventarioUI = BuscarObjeto(nivel2, "UI_InventarioArmas");
        GameObject monedero = BuscarObjeto(nivel2, "MonederoPartida");
        GameObject monedasUI = BuscarObjeto(nivel2, "Monedas");

        Exigir(jugador != null, "No se encontro el Jugador.");
        Exigir(hud != null && hud.GetComponent<Canvas>() != null,
            "Falta Canvas_HUDJugador.");
        Exigir(inventarioUI != null && inventarioUI.GetComponent<Canvas>() != null,
            "Falta UI_InventarioArmas.");
        Exigir(monedero != null && monedero.GetComponent<Tienda>() != null,
            "Falta el monedero de la partida.");
        Exigir(monedasUI != null && monedasUI.GetComponent<ContadorMonedas>() != null,
            "Falta el contador visual de monedas.");

        Jugador controladorJugador = jugador.GetComponentInChildren<Jugador>(true);
        InventarioArmas inventario =
            jugador.GetComponentInChildren<InventarioArmas>(true);
        Exigir(controladorJugador != null, "Falta el componente Jugador.");
        Exigir(controladorJugador.beaglesUI != null &&
            controladorJugador.beaglesUI.Length == 3 &&
            controladorJugador.beaglesUI.All(imagen => imagen != null),
            "Las tres vidas no estan conectadas al Jugador.");
        Exigir(controladorJugador.bordeRojo != null,
            "Falta conectar el borde de dano al Jugador.");
        Exigir(inventario != null && inventario.iconoArmaEquipada != null &&
            inventario.animatorUI != null,
            "El inventario no esta conectado a su interfaz.");

        CanvasScaler escaladorHud = hud.GetComponent<CanvasScaler>();
        CanvasScaler escaladorHorda = BuscarObjeto(nivel2, "Canvas")
            ?.GetComponent<CanvasScaler>();
        Exigir(escaladorHud != null &&
            escaladorHud.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
            "El HUD no escala con la resolucion.");
        Exigir(escaladorHorda != null &&
            escaladorHorda.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
            "La barra de horda no escala con la resolucion.");

        SaludEnemigo[] enemigos = nivel2.GetRootGameObjects()
            .SelectMany(raiz => raiz.GetComponentsInChildren<SaludEnemigo>(true))
            .ToArray();
        Exigir(enemigos.Length > 0, "No se encontraron enemigos en Nivel 2.");
        Exigir(enemigos.All(enemigo =>
                enemigo.GetComponent<RetroalimentacionDanio>() != null),
            "Hay enemigos directos sin retroalimentacion de dano.");

        foreach (string ruta in RutasPrefabsEnemigos)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Exigir(prefab != null && prefab.GetComponentsInChildren<SaludEnemigo>(true)
                    .All(salud => salud.GetComponent<RetroalimentacionDanio>() != null),
                "El prefab no tiene flash de dano: " + ruta);
        }

        Debug.Log("[NIVEL 2] Validacion correcta.");
    }

    private static void ConfigurarPrefabsEnemigos(Material materialSilueta)
    {
        foreach (string ruta in RutasPrefabsEnemigos)
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                foreach (SaludEnemigo salud in
                    contenido.GetComponentsInChildren<SaludEnemigo>(true))
                {
                    ConfigurarFlash(salud.gameObject, materialSilueta);
                }

                PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }
    }

    private static void ConfigurarEscenaNivel2(Material materialSilueta)
    {
        Scene nivel2 = EditorSceneManager.OpenScene(RutaNivel2,
            OpenSceneMode.Single);
        GameObject jugador = BuscarObjeto(nivel2, "Jugador");
        Exigir(jugador != null, "No se encontro el Jugador en Nivel 2.");

        GameObject hud = InstalarHud(nivel2);
        ConectarHudJugador(nivel2, jugador, hud);
        InstalarInventario(nivel2, jugador);
        InstalarMonederoYContador(nivel2, hud);
        ConfigurarEscaladoHorda(nivel2);

        foreach (GameObject raiz in nivel2.GetRootGameObjects())
        {
            foreach (SaludEnemigo salud in
                raiz.GetComponentsInChildren<SaludEnemigo>(true))
            {
                ConfigurarFlash(salud.gameObject, materialSilueta);
            }
        }

        EditorSceneManager.MarkSceneDirty(nivel2);
        EditorSceneManager.SaveScene(nivel2);
    }

    private static GameObject InstalarHud(Scene nivel2)
    {
        GameObject hud = BuscarObjeto(nivel2, "Canvas_HUDJugador");
        if (hud == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabHud);
            hud = PrefabUtility.InstantiatePrefab(prefab, nivel2) as GameObject;
            Exigir(hud != null, "No se pudo instanciar Canvas_HUDJugador.");
            hud.name = "Canvas_HUDJugador";
        }

        ConfigurarEscalador(hud.GetComponent<CanvasScaler>());
        return hud;
    }

    private static void ConectarHudJugador(Scene nivel2, GameObject jugador,
        GameObject hud)
    {
        Jugador controladorJugador = jugador.GetComponentInChildren<Jugador>(true);
        Exigir(controladorJugador != null, "El objeto Jugador no tiene su controlador.");

        controladorJugador.beaglesUI = new[]
        {
            BuscarComponente<Image>(hud, "BeagleVida1"),
            BuscarComponente<Image>(hud, "BeagleVida2"),
            BuscarComponente<Image>(hud, "BeagleVida3")
        };
        controladorJugador.bordeRojo = BuscarObjetoEnRaiz(hud, "BordeRojoDaño");
        Exigir(controladorJugador.beaglesUI.All(imagen => imagen != null) &&
            controladorJugador.bordeRojo != null,
            "El prefab del HUD no contiene las referencias de vida requeridas.");
        EditorUtility.SetDirty(controladorJugador);

        // Estas dos interfaces antiguas eran objetos sueltos, sin Canvas. Ya no
        // deben coexistir con el HUD reutilizable porque duplicarían la vida.
        foreach (GameObject raiz in nivel2.GetRootGameObjects())
        {
            if (raiz == hud) continue;
            if (raiz.name == "VidasContador" || raiz.name == "BordeRojoDaño")
                UnityEngine.Object.DestroyImmediate(raiz);
        }
    }

    private static void InstalarInventario(Scene nivel2, GameObject jugador)
    {
        GameObject interfaz = BuscarObjeto(nivel2, "UI_InventarioArmas");
        if (interfaz == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RutaPrefabInventario);
            interfaz = PrefabUtility.InstantiatePrefab(prefab, nivel2) as GameObject;
            Exigir(interfaz != null, "No se pudo instanciar UI_InventarioArmas.");
            interfaz.name = "UI_InventarioArmas";
        }

        ConfigurarEscalador(interfaz.GetComponent<CanvasScaler>());
        InventarioArmas inventario =
            jugador.GetComponentInChildren<InventarioArmas>(true);
        Exigir(inventario != null, "El Jugador no contiene InventarioArmas.");
        inventario.iconoArmaEquipada = BuscarComponente<Image>(interfaz,
            "IconoArmaEquipada");
        inventario.animatorUI = BuscarComponente<Animator>(interfaz, "UI_Fondo");
        Exigir(inventario.iconoArmaEquipada != null && inventario.animatorUI != null,
            "El prefab de inventario esta incompleto.");
        interfaz.SetActive(true);
        EditorUtility.SetDirty(inventario);
    }

    private static void InstalarMonederoYContador(Scene nivel2, GameObject hud)
    {
        if (BuscarObjeto(nivel2, "MonederoPartida") == null)
        {
            GameObject monedero = new GameObject("MonederoPartida");
            SceneManager.MoveGameObjectToScene(monedero, nivel2);
            monedero.AddComponent<Tienda>();
        }

        if (BuscarObjeto(nivel2, "Monedas") != null) return;

        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Additive);
        try
        {
            GameObject contadorOriginal = BuscarObjeto(nivel1, "Monedas");
            Exigir(contadorOriginal != null,
                "No se encontro el contador de monedas del Nivel 1.");
            GameObject contador = UnityEngine.Object.Instantiate(contadorOriginal);
            contador.name = "Monedas";
            SceneManager.MoveGameObjectToScene(contador, nivel2);
            contador.transform.SetParent(hud.transform, false);
            contador.SetActive(true);
        }
        finally
        {
            EditorSceneManager.CloseScene(nivel1, true);
        }
    }

    private static void ConfigurarEscaladoHorda(Scene nivel2)
    {
        GameObject canvasHorda = BuscarObjeto(nivel2, "Canvas");
        Exigir(canvasHorda != null, "No se encontro el Canvas de la horda.");
        ConfigurarEscalador(canvasHorda.GetComponent<CanvasScaler>());
    }

    private static void ConfigurarEscalador(CanvasScaler escalador)
    {
        Exigir(escalador != null, "Un Canvas no tiene CanvasScaler.");
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(escalador);
    }

    private static void ConfigurarFlash(GameObject entidad, Material materialSilueta)
    {
        RetroalimentacionDanio flash =
            entidad.GetComponent<RetroalimentacionDanio>();
        if (flash == null) flash = entidad.AddComponent<RetroalimentacionDanio>();
        flash.ConfigurarMaterialSilueta(materialSilueta);
        EditorUtility.SetDirty(entidad);
    }

    private static T BuscarComponente<T>(GameObject raiz, string nombre)
        where T : Component
    {
        return raiz.GetComponentsInChildren<T>(true)
            .FirstOrDefault(componente => componente.name == nombre);
    }

    private static GameObject BuscarObjeto(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            Transform encontrado = raiz.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transformacion => transformacion.name == nombre);
            if (encontrado != null) return encontrado.gameObject;
        }

        return null;
    }

    private static GameObject BuscarObjetoEnRaiz(GameObject raiz, string nombre)
    {
        Transform encontrado = raiz.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transformacion => transformacion.name == nombre);
        return encontrado != null ? encontrado.gameObject : null;
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException("[NIVEL 2] " + mensaje);
    }
}
