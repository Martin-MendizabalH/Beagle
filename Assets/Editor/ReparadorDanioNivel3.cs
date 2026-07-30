using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Instala el HUD reutilizable del Jugador en el Nivel 3 y conecta sus
/// referencias. No modifica la geometría ni la configuración de ArenaJefe.
/// </summary>
public static class ReparadorDanioNivel3
{
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";
    private const string RutaNivel3 = "Assets/Escenas/Niveles/Nivel 3.unity";
    private const string CarpetaHUD = "Assets/Prefabs/Interfaz/Jugador";
    private const string RutaPrefabHUD = CarpetaHUD + "/Canvas_HUDJugador.prefab";

    [MenuItem("Herramientas/Proyecto Beagle/Reparar daño y HUD del Nivel 3")]
    public static void Aplicar()
    {
        CrearCarpetaSiNoExiste("Assets/Prefabs/Interfaz");
        CrearCarpetaSiNoExiste(CarpetaHUD);

        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        GameObject canvasOriginal = BuscarObjetoEnEscena(nivel1, "Canvas");
        Exigir(canvasOriginal != null, "No se encontró el Canvas principal del Nivel 1.");

        GameObject prefabHUD = PrefabUtility.SaveAsPrefabAsset(canvasOriginal, RutaPrefabHUD);
        Exigir(prefabHUD != null, "No se pudo crear Canvas_HUDJugador.prefab.");

        Scene nivel3 = EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
        Jugador jugador = BuscarComponenteEnEscena<Jugador>(nivel3);
        Exigir(jugador != null, "No se encontró el Jugador en el Nivel 3.");

        GameObject hud = BuscarObjetoEnEscena(nivel3, "Canvas");
        if (hud == null)
        {
            hud = PrefabUtility.InstantiatePrefab(prefabHUD, nivel3) as GameObject;
            Exigir(hud != null, "No se pudo instanciar el HUD en el Nivel 3.");
            hud.name = "Canvas";
        }

        Image vida1 = BuscarComponentePorNombre<Image>(hud, "BeagleVida1");
        Image vida2 = BuscarComponentePorNombre<Image>(hud, "BeagleVida2");
        Image vida3 = BuscarComponentePorNombre<Image>(hud, "BeagleVida3");
        Transform borde = BuscarTransformPorPrefijo(hud, "BordeRojo");
        TextMeshProUGUI contadorPociones =
            BuscarComponentePorNombre<TextMeshProUGUI>(hud, "TextoContadorPociones");

        Exigir(vida1 != null && vida2 != null && vida3 != null,
            "El HUD no contiene las tres imágenes de vida.");
        Exigir(borde != null, "El HUD no contiene el borde rojo de daño.");
        Exigir(contadorPociones != null, "El HUD no contiene el contador de pociones.");

        jugador.beaglesUI = new[] { vida1, vida2, vida3 };
        jugador.bordeRojo = borde.gameObject;
        jugador.textoContadorPociones = contadorPociones;

        hud.SetActive(true);
        EditorUtility.SetDirty(jugador);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(nivel3);
        EditorSceneManager.SaveScene(nivel3);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidarNivel3();
        Debug.Log("[DAÑO NIVEL 3] HUD instalado y referencias del Jugador reparadas.");
    }

    [MenuItem("Herramientas/Proyecto Beagle/Validar daño y HUD del Nivel 3")]
    public static void ValidarNivel3()
    {
        Scene nivel3 = EditorSceneManager.OpenScene(RutaNivel3, OpenSceneMode.Single);
        Jugador jugador = BuscarComponenteEnEscena<Jugador>(nivel3);
        GameObject hud = BuscarObjetoEnEscena(nivel3, "Canvas");

        Exigir(jugador != null, "Falta el Jugador.");
        Exigir(hud != null && hud.activeSelf, "Falta el HUD activo.");
        Exigir(jugador.beaglesUI != null && jugador.beaglesUI.Length == 3,
            "El arreglo de vidas no tiene tres elementos.");
        Exigir(jugador.beaglesUI.All(imagen => imagen != null),
            "Hay imágenes de vida sin conectar.");
        Exigir(jugador.bordeRojo != null, "Falta la referencia al borde rojo.");
        Exigir(jugador.textoContadorPociones != null,
            "Falta la referencia al contador de pociones.");

        Debug.Log("[DAÑO NIVEL 3] Validación del HUD completada sin errores.");
    }

    private static T BuscarComponentePorNombre<T>(GameObject raiz, string nombre)
        where T : Component
    {
        return raiz.GetComponentsInChildren<T>(true)
            .FirstOrDefault(componente => componente.name == nombre);
    }

    private static Transform BuscarTransformPorPrefijo(GameObject raiz, string prefijo)
    {
        return raiz.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(transformacion =>
                transformacion.name.StartsWith(prefijo, StringComparison.Ordinal));
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

    private static T BuscarComponenteEnEscena<T>(Scene escena) where T : Component
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            T componente = raiz.GetComponentInChildren<T>(true);
            if (componente != null) return componente;
        }

        return null;
    }

    private static void CrearCarpetaSiNoExiste(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string padre = ruta.Substring(0, ruta.LastIndexOf('/'));
        string nombre = ruta.Substring(ruta.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(padre)) CrearCarpetaSiNoExiste(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException("[DAÑO NIVEL 3] " + mensaje);
    }
}
