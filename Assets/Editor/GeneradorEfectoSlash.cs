using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Importa el sheet de seis cuadros del slash y genera el clip, controller y prefab reutilizables.
/// Puede ejecutarse desde Herramientas/Beagle/Generar efecto de slash o en modo batch.
/// </summary>
public static class GeneradorEfectoSlash
{
    private const string RutaTextura = "Assets/Arte/Efectos/Katana/SlashFX Combo2 sheet.png";
    private const string CarpetaEfecto = "Assets/Animaciones/Efectos/Katana";
    private const string RutaClip = CarpetaEfecto + "/EfectoSlashKatana.anim";
    private const string RutaControlador = CarpetaEfecto + "/EfectoSlashKatana.controller";
    private const string RutaPrefab = "Assets/Prefabs/Efectos/EfectoSlashKatana.prefab";
    private const string RutaAnimacionKatana = "Assets/Animaciones/Armas/Katana_Slash.anim";
    private const string RutaControladorKatana = "Assets/Animaciones/Armas/Contenedor_Katana.controller";
    // El arte está dibujado hacia la esquina superior derecha del cuadro. Este
    // pivote lo centra visualmente para que una rotación de 180° no lo desplace.
    private static readonly Vector2 PivoteVisualSlash = new Vector2(0.684f, 0.629f);

    [InitializeOnLoadMethod]
    private static void GenerarAlAbrirProyecto()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null &&
                AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura) != null)
            {
                CrearRecursosSlash();
            }
        };
    }

    [InitializeOnLoadMethod]
    private static void ConfigurarCorteDeKatanaAlAbrir()
    {
        const string claveSesion = "Beagle.CorteKatana.Configurado";
        if (SessionState.GetBool(claveSesion, false)) return;

        EditorApplication.delayCall += () =>
        {
            ConfigurarAnimacionKatana();
            SessionState.SetBool(claveSesion, true);
        };
    }

    [InitializeOnLoadMethod]
    private static void CrearPuntoOrigenSlashAlAbrir()
    {
        const string claveSesion = "Beagle.PuntoOrigenSlash.Creado";
        if (SessionState.GetBool(claveSesion, false)) return;

        EditorApplication.delayCall += () =>
        {
            AsegurarPuntoOrigenSlash();
            SessionState.SetBool(claveSesion, true);
        };
    }

    [InitializeOnLoadMethod]
    private static void ConfigurarPrefabSlashAlAbrir()
    {
        const string claveSesion = "Beagle.PrefabSlash.Configurado";
        if (SessionState.GetBool(claveSesion, false)) return;

        EditorApplication.delayCall += () =>
        {
            AsegurarComponentesSlash();
            SessionState.SetBool(claveSesion, true);
        };
    }

    [MenuItem("Herramientas/Beagle/Generar efecto de slash")]
    public static void CrearRecursosSlash()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTextura) == null)
        {
            Debug.LogError("No se encontró el sheet del slash en: " + RutaTextura);
            return;
        }

        AsegurarCarpeta("Assets/Animaciones/Efectos");
        AsegurarCarpeta(CarpetaEfecto);

        TextureImporter importador = AssetImporter.GetAtPath(RutaTextura) as TextureImporter;
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Multiple;
        importador.spritePixelsPerUnit = 128;
        importador.filterMode = FilterMode.Bilinear;
        importador.alphaIsTransparency = true;

        const int cantidadCuadros = 6;
        const int anchoCuadro = 128;
        const int altoCuadro = 128;
        SpriteRect[] cuadros = new SpriteRect[cantidadCuadros];

        for (int i = 0; i < cantidadCuadros; i++)
        {
            cuadros[i] = new SpriteRect
            {
                name = $"SlashKatana_{i + 1:00}",
                rect = new Rect(i * anchoCuadro, 0, anchoCuadro, altoCuadro),
                alignment = SpriteAlignment.Custom,
                pivot = PivoteVisualSlash,
                spriteID = GUID.Generate()
            };
        }

        SpriteDataProviderFactories fabricasProveedores = new SpriteDataProviderFactories();
        fabricasProveedores.Init();
        ISpriteEditorDataProvider proveedorSprites = fabricasProveedores
            .GetSpriteEditorDataProviderFromObject(importador);
        proveedorSprites.InitSpriteEditorDataProvider();
        proveedorSprites.SetSpriteRects(cuadros);
        proveedorSprites.Apply();
        importador.SaveAndReimport();

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(RutaTextura)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();

        if (sprites.Length != cantidadCuadros)
        {
            Debug.LogError("El sheet del slash no generó los seis sprites esperados.");
            return;
        }

        AssetDatabase.DeleteAsset(RutaClip);
        AnimationClip clip = new AnimationClip { frameRate = 18f, name = "EfectoSlashKatana" };
        EditorCurveBinding enlaceSprite = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] cuadrosAnimacion = sprites
            .Select((sprite, indice) => new ObjectReferenceKeyframe
            {
                time = indice / clip.frameRate,
                value = sprite
            })
            .ToArray();

        AnimationUtility.SetObjectReferenceCurve(clip, enlaceSprite, cuadrosAnimacion);
        AssetDatabase.CreateAsset(clip, RutaClip);

        AssetDatabase.DeleteAsset(RutaControlador);
        AnimatorController controlador = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
        AnimatorState estado = controlador.layers[0].stateMachine.AddState("Slash");
        estado.motion = clip;
        controlador.layers[0].stateMachine.defaultState = estado;

        AssetDatabase.DeleteAsset(RutaPrefab);
        GameObject efecto = new GameObject("EfectoSlashKatana");
        SpriteRenderer renderizador = efecto.AddComponent<SpriteRenderer>();
        renderizador.sprite = sprites[0];
        renderizador.sortingLayerName = "Player";
        renderizador.sortingOrder = 6;

        Animator animador = efecto.AddComponent<Animator>();
        animador.runtimeAnimatorController = controlador;
        BoxCollider2D hitbox = efecto.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(0.8f, 0.58f);

        Rigidbody2D cuerpoFisico = efecto.AddComponent<Rigidbody2D>();
        cuerpoFisico.bodyType = RigidbodyType2D.Kinematic;
        cuerpoFisico.gravityScale = 0f;
        cuerpoFisico.constraints = RigidbodyConstraints2D.FreezeRotation;

        efecto.AddComponent<SlashKatana>();
        efecto.AddComponent<EfectoSlash>();
        PrefabUtility.SaveAsPrefabAsset(efecto, RutaPrefab);
        Object.DestroyImmediate(efecto);

        VincularHitboxKatana();
        AsegurarPuntoOrigenSlash();
        AsegurarComponentesSlash();
        ConfigurarAnimacionKatana();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Efecto de slash generado correctamente.");
    }

    private static void VincularHitboxKatana()
    {
        const string rutaJugador = "Assets/Prefabs/Jugador/Jugador.prefab";
        GameObject prefabEfecto = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        GameObject jugador = PrefabUtility.LoadPrefabContents(rutaJugador);
        AtaqueMelee ataqueMelee = jugador.GetComponentInChildren<AtaqueMelee>(true);

        if (ataqueMelee == null)
        {
            Debug.LogError("No se encontró AtaqueMelee dentro del prefab del jugador.");
            PrefabUtility.UnloadPrefabContents(jugador);
            return;
        }

        SerializedObject datosAtaque = new SerializedObject(ataqueMelee);
        datosAtaque.FindProperty("prefabEfectoSlash").objectReferenceValue = prefabEfecto;
        datosAtaque.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(jugador, rutaJugador);
        PrefabUtility.UnloadPrefabContents(jugador);
    }

    private static void AsegurarPuntoOrigenSlash()
    {
        const string rutaJugador = "Assets/Prefabs/Jugador/Jugador.prefab";
        GameObject jugador = PrefabUtility.LoadPrefabContents(rutaJugador);
        AtaqueMelee ataqueMelee = jugador.GetComponentInChildren<AtaqueMelee>(true);

        if (ataqueMelee == null)
        {
            PrefabUtility.UnloadPrefabContents(jugador);
            return;
        }

        Transform puntoOrigen = jugador.transform.Find("PuntoOrigenSlash");
        if (puntoOrigen == null)
        {
            GameObject objetoPuntoOrigen = new GameObject("PuntoOrigenSlash");
            puntoOrigen = objetoPuntoOrigen.transform;
            puntoOrigen.SetParent(jugador.transform, false);

            // Punto inicial cerca del centro del personaje. Ajustable libremente en el prefab.
            puntoOrigen.localPosition = new Vector3(0.364f, 0.069f, 0f);
        }

        SerializedObject datosAtaque = new SerializedObject(ataqueMelee);
        datosAtaque.FindProperty("puntoOrigenSlash").objectReferenceValue = puntoOrigen;
        datosAtaque.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(jugador, rutaJugador);
        PrefabUtility.UnloadPrefabContents(jugador);
        AssetDatabase.SaveAssets();
    }

    private static void AsegurarComponentesSlash()
    {
        GameObject efecto = PrefabUtility.LoadPrefabContents(RutaPrefab);
        if (efecto == null) return;

        BoxCollider2D hitbox = efecto.GetComponent<BoxCollider2D>();
        if (hitbox == null) hitbox = efecto.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.offset = Vector2.zero;
        hitbox.size = new Vector2(0.8f, 0.58f);

        Rigidbody2D cuerpoFisico = efecto.GetComponent<Rigidbody2D>();
        if (cuerpoFisico == null) cuerpoFisico = efecto.AddComponent<Rigidbody2D>();
        cuerpoFisico.bodyType = RigidbodyType2D.Kinematic;
        cuerpoFisico.gravityScale = 0f;
        cuerpoFisico.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (efecto.GetComponent<SlashKatana>() == null) efecto.AddComponent<SlashKatana>();

        PrefabUtility.SaveAsPrefabAsset(efecto, RutaPrefab);
        PrefabUtility.UnloadPrefabContents(efecto);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigurarAnimacionKatana()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaAnimacionKatana);
        AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControladorKatana);
        if (clip == null || controlador == null) return;

        // Se anima el contenedor de la katana: como contiene ambos brazos, recupera
        // el swing visual original sin controlar hitboxes ni mecánicas de daño.
        foreach (EditorCurveBinding enlace in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationUtility.SetEditorCurve(clip, enlace, null);
        }

        foreach (EditorCurveBinding enlace in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, enlace, null);
        }

        EditorCurveBinding rotacionEspada = new EditorCurveBinding
        {
            type = typeof(Transform),
            path = string.Empty,
            propertyName = "localEulerAnglesRaw.z"
        };

        AnimationCurve movimientoCorte = new AnimationCurve(
            new Keyframe(0f, 90f),
            new Keyframe(0.09f, -90f),
            new Keyframe(0.18f, 0f));
        AnimationUtility.SetEditorCurve(clip, rotacionEspada, movimientoCorte);
        clip.frameRate = 60f;
        clip.wrapMode = WrapMode.Once;
        EditorUtility.SetDirty(clip);

        foreach (ChildAnimatorState estadoHijo in controlador.layers[0].stateMachine.states)
        {
            if (estadoHijo.state.name != "Katana_Slash") continue;

            estadoHijo.state.speed = 1f;
            EditorUtility.SetDirty(estadoHijo.state);
            break;
        }

        EditorUtility.SetDirty(controlador);
        AssetDatabase.SaveAssets();
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string carpetaPadre = System.IO.Path.GetDirectoryName(ruta)?.Replace('\\', '/');
        string nombreCarpeta = System.IO.Path.GetFileName(ruta);
        AssetDatabase.CreateFolder(carpetaPadre, nombreCarpeta);
    }
}
