using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Enlaza las tarjetas visuales ya existentes del Nivel 1 con sus DatosArma.
/// Se conserva como herramienta para poder reconstruir o validar la tienda.
/// </summary>
public static class ConfiguradorTiendaArmas
{
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";

    private readonly struct DefinicionTarjeta
    {
        public readonly string nombreObjeto;
        public readonly string rutaArma;

        public DefinicionTarjeta(string nombreObjeto, string rutaArma)
        {
            this.nombreObjeto = nombreObjeto;
            this.rutaArma = rutaArma;
        }
    }

    private static readonly DefinicionTarjeta[] Tarjetas =
    {
        new DefinicionTarjeta("ItemKatana", "Assets/Datos/Armas/Arma_Katana.asset"),
        new DefinicionTarjeta("ItemMetralleta", "Assets/Datos/Armas/Arma_Metralleta.asset"),
        new DefinicionTarjeta("ItemEscopeta", "Assets/Datos/Armas/Arma_Escopeta.asset")
    };

    [MenuItem("Herramientas/Beagle/Configurar tienda de armas")]
    public static void Configurar()
    {
        Scene escenaAnterior = SceneManager.GetActiveScene();
        string rutaAnterior = escenaAnterior.path;
        bool anteriorModificada = escenaAnterior.isDirty;

        if (anteriorModificada &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            throw new InvalidOperationException("Se canceló la configuración para no perder cambios de la escena abierta.");
        }

        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        foreach (DefinicionTarjeta definicion in Tarjetas)
        {
            ConfigurarTarjeta(nivel1, definicion);
        }

        EditorSceneManager.MarkSceneDirty(nivel1);
        EditorSceneManager.SaveScene(nivel1);
        AssetDatabase.SaveAssets();

        Debug.Log("[TIENDA] Katana, Metralleta y Escopeta quedaron enlazadas a sus precios y botones.");

        if (!string.IsNullOrEmpty(rutaAnterior) && rutaAnterior != RutaNivel1)
        {
            EditorSceneManager.OpenScene(rutaAnterior, OpenSceneMode.Single);
        }
    }

    public static void ConfigurarEnLote()
    {
        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        foreach (DefinicionTarjeta definicion in Tarjetas)
        {
            ConfigurarTarjeta(nivel1, definicion);
        }

        EditorSceneManager.MarkSceneDirty(nivel1);
        EditorSceneManager.SaveScene(nivel1);
        AssetDatabase.SaveAssets();
        Debug.Log("[TIENDA] Configuración en lote completada.");
    }

    private static void ConfigurarTarjeta(Scene escena, DefinicionTarjeta definicion)
    {
        GameObject tarjeta = BuscarPorNombre(escena, definicion.nombreObjeto);
        Exigir(tarjeta != null, $"No se encontró {definicion.nombreObjeto}.");

        DatosArma arma = AssetDatabase.LoadAssetAtPath<DatosArma>(definicion.rutaArma);
        Exigir(arma != null, $"No se encontró el arma en {definicion.rutaArma}.");

        ItemTiendaUI item = tarjeta.GetComponent<ItemTiendaUI>();
        if (item == null) item = tarjeta.AddComponent<ItemTiendaUI>();

        item.objeto = null;
        item.arma = arma;
        item.textoNombre = BuscarTextoNombre(tarjeta, arma);
        item.textoPrecio = BuscarTextoExacto(tarjeta, "Precio");
        item.imagenSprite = BuscarImagenArma(tarjeta, arma);
        item.botonComprar = tarjeta.GetComponentInChildren<Button>(true);

        Exigir(item.textoNombre != null, $"{definicion.nombreObjeto} no tiene texto de nombre.");
        Exigir(item.textoPrecio != null, $"{definicion.nombreObjeto} no tiene texto Precio.");
        Exigir(item.imagenSprite != null, $"{definicion.nombreObjeto} no tiene imagen para el arma.");
        Exigir(item.botonComprar != null, $"{definicion.nombreObjeto} no tiene botón de compra.");

        item.textoNombre.text = arma.nombreArma;
        item.textoPrecio.text = arma.precio.ToString();
        item.imagenSprite.sprite = arma.spriteArma;
        item.imagenSprite.preserveAspect = true;

        EditorUtility.SetDirty(item);
        EditorUtility.SetDirty(item.textoNombre);
        EditorUtility.SetDirty(item.textoPrecio);
        EditorUtility.SetDirty(item.imagenSprite);
    }

    private static TextMeshProUGUI BuscarTextoNombre(GameObject tarjeta, DatosArma arma)
    {
        foreach (TextMeshProUGUI texto in tarjeta.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (texto.text.Trim().Equals(arma.nombreArma, StringComparison.OrdinalIgnoreCase))
            {
                return texto;
            }
        }

        string nombreBuscado = "texto" + arma.nombreArma.Replace(" ", string.Empty);
        foreach (TextMeshProUGUI texto in tarjeta.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (texto.gameObject.name.Replace(" ", string.Empty)
                .Equals(nombreBuscado, StringComparison.OrdinalIgnoreCase))
            {
                return texto;
            }
        }

        return null;
    }

    private static TextMeshProUGUI BuscarTextoExacto(GameObject raiz, string nombre)
    {
        foreach (TextMeshProUGUI texto in raiz.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (texto.gameObject.name.Equals(nombre, StringComparison.OrdinalIgnoreCase))
            {
                return texto;
            }
        }

        return null;
    }

    private static Image BuscarImagenArma(GameObject tarjeta, DatosArma arma)
    {
        foreach (Image imagen in tarjeta.GetComponentsInChildren<Image>(true))
        {
            if (imagen.sprite == arma.spriteArma) return imagen;
        }

        foreach (Image imagen in tarjeta.GetComponentsInChildren<Image>(true))
        {
            string nombre = imagen.gameObject.name.ToLowerInvariant();
            if (nombre.Contains("sprite") || nombre.Contains("arma") || nombre.Contains("icono"))
            {
                return imagen;
            }
        }

        return null;
    }

    private static GameObject BuscarPorNombre(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform transformacion in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (transformacion.name == nombre) return transformacion.gameObject;
            }
        }

        return null;
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException("[TIENDA] " + mensaje);
    }
}
