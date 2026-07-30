using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Genera el prefab de moneda y equipa a enemigos existentes con feedback y botin.</summary>
public static class GeneradorSistemaBotin
{
    private const string RutaPrefabMoneda = "Assets/Prefabs/Coleccionables/Moneda.prefab";
    private const string RutaSpriteMoneda = "Assets/Arte/Objetos/Monedas/Sprites/Gold_1.png";
    private const string RutaShaderSilueta = "Assets/Arte/SiluetaDano.shader";
    private const string RutaMaterialSilueta = "Assets/Arte/MaterialSiluetaDano.mat";
    [MenuItem("Herramientas/Beagle/Configurar dano y botin de enemigos")]
    public static void CrearYConfigurarSistema()
    {
        GameObject prefabMoneda = CrearOActualizarPrefabMoneda();
        Material materialSilueta = CrearOActualizarMaterialSilueta();
        ConfigurarPrefabsEnemigos(prefabMoneda, materialSilueta);
        ConfigurarEscenas(prefabMoneda, materialSilueta);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Sistema de dano y botin configurado correctamente.");
    }

    private static GameObject CrearOActualizarPrefabMoneda()
    {
        AsegurarCarpeta("Assets/Prefabs/Coleccionables");
        GameObject moneda = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMoneda);

        if (moneda == null)
        {
            GameObject nuevaMoneda = new GameObject("Moneda");
            ConfigurarMoneda(nuevaMoneda);
            PrefabUtility.SaveAsPrefabAsset(nuevaMoneda, RutaPrefabMoneda);
            Object.DestroyImmediate(nuevaMoneda);
        }
        else
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(RutaPrefabMoneda);
            ConfigurarMoneda(contenido);
            PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabMoneda);
            PrefabUtility.UnloadPrefabContents(contenido);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMoneda);
    }

    private static Material CrearOActualizarMaterialSilueta()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialSilueta);
        if (material != null) return material;

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShaderSilueta);
        if (shader == null)
        {
            Debug.LogError("No se encontro el shader de silueta de dano.");
            return null;
        }

        material = new Material(shader) { name = "MaterialSiluetaDano" };
        AssetDatabase.CreateAsset(material, RutaMaterialSilueta);
        return material;
    }

    private static void ConfigurarMoneda(GameObject moneda)
    {
        SpriteRenderer renderizador = moneda.GetComponent<SpriteRenderer>();
        if (renderizador == null) renderizador = moneda.AddComponent<SpriteRenderer>();
        renderizador.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSpriteMoneda);
        renderizador.sortingLayerName = "Player";
        renderizador.sortingOrder = 5;

        Animator animador = moneda.GetComponent<Animator>();
        if (animador != null) Object.DestroyImmediate(animador, true);

        Rigidbody2D cuerpoFisico = moneda.GetComponent<Rigidbody2D>();
        if (cuerpoFisico == null) cuerpoFisico = moneda.AddComponent<Rigidbody2D>();
        cuerpoFisico.bodyType = RigidbodyType2D.Dynamic;
        cuerpoFisico.gravityScale = 1.15f;
        cuerpoFisico.drag = 0.35f;
        cuerpoFisico.constraints = RigidbodyConstraints2D.FreezeRotation;
        cuerpoFisico.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D colliderFisico = ObtenerOCrearCollider(moneda, false);
        colliderFisico.radius = 0.12f;

        CircleCollider2D areaRecogida = ObtenerOCrearCollider(moneda, true);
        areaRecogida.radius = 0.22f;

        if (moneda.GetComponent<Moneda>() == null) moneda.AddComponent<Moneda>();
    }

    private static void ConfigurarPrefabsEnemigos(GameObject prefabMoneda, Material materialSilueta)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (ruta.StartsWith("Assets/Prefabs/Coleccionables/")) continue;

            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);
            bool fueModificado = false;

            foreach (SaludEnemigo salud in contenido.GetComponentsInChildren<SaludEnemigo>(true))
            {
                ConfigurarEntidad(salud.gameObject, prefabMoneda, materialSilueta);
                fueModificado = true;
            }

            foreach (SoldadoEnemigo soldado in contenido.GetComponentsInChildren<SoldadoEnemigo>(true))
            {
                ConfigurarEntidad(soldado.gameObject, prefabMoneda, materialSilueta);
                fueModificado = true;
            }

            foreach (SaludJefe saludJefe in contenido.GetComponentsInChildren<SaludJefe>(true))
            {
                ConfigurarEntidad(saludJefe.gameObject, prefabMoneda, materialSilueta);
                fueModificado = true;
            }

            if (fueModificado) PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            PrefabUtility.UnloadPrefabContents(contenido);
        }
    }

    private static void ConfigurarEscenas(GameObject prefabMoneda, Material materialSilueta)
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Escenas" });

        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            bool fueModificada = false;

            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (SaludEnemigo salud in raiz.GetComponentsInChildren<SaludEnemigo>(true))
                {
                    ConfigurarEntidad(salud.gameObject, prefabMoneda, materialSilueta);
                    fueModificada = true;
                }

                foreach (SoldadoEnemigo soldado in raiz.GetComponentsInChildren<SoldadoEnemigo>(true))
                {
                    ConfigurarEntidad(soldado.gameObject, prefabMoneda, materialSilueta);
                    fueModificada = true;
                }

                foreach (SaludJefe saludJefe in raiz.GetComponentsInChildren<SaludJefe>(true))
                {
                    ConfigurarEntidad(saludJefe.gameObject, prefabMoneda, materialSilueta);
                    fueModificada = true;
                }
            }

            if (fueModificada) EditorSceneManager.SaveScene(escena);
        }
    }

    private static void ConfigurarEntidad(GameObject entidad, GameObject prefabMoneda, Material materialSilueta)
    {
        RetroalimentacionDanio retroalimentacion = entidad.GetComponent<RetroalimentacionDanio>();
        if (retroalimentacion == null)
        {
            retroalimentacion = entidad.AddComponent<RetroalimentacionDanio>();
        }
        retroalimentacion.ConfigurarMaterialSilueta(materialSilueta);

        BotinMonedas botin = entidad.GetComponent<BotinMonedas>();
        if (botin == null) botin = entidad.AddComponent<BotinMonedas>();
        botin.Configurar(prefabMoneda, 2, 3, 5);
        botin.ConfigurarExplosion(3.5f, 4f);
        EditorUtility.SetDirty(entidad);
    }

    private static CircleCollider2D ObtenerOCrearCollider(GameObject objeto, bool esTrigger)
    {
        foreach (CircleCollider2D collider in objeto.GetComponents<CircleCollider2D>())
        {
            if (collider.isTrigger == esTrigger) return collider;
        }

        CircleCollider2D nuevoCollider = objeto.AddComponent<CircleCollider2D>();
        nuevoCollider.isTrigger = esTrigger;
        return nuevoCollider;
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string carpetaPadre = System.IO.Path.GetDirectoryName(ruta)?.Replace('\\', '/');
        string nombreCarpeta = System.IO.Path.GetFileName(ruta);
        AssetDatabase.CreateFolder(carpetaPadre, nombreCarpeta);
    }
}
