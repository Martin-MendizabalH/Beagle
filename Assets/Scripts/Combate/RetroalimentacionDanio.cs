using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Destello de silueta blanca reutilizable al recibir dano.</summary>
public class RetroalimentacionDanio : MonoBehaviour
{
    [SerializeField] private Material materialSiluetaBlanca;
    [SerializeField, Min(0.01f)] private float duracionDestello = 0.09f;

    private readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
    private readonly List<Material[]> materialesBase = new List<Material[]>();
    private Coroutine rutinaDestello;
    private static Material materialSiluetaGenerado;

    public int CantidadImpactos { get; private set; }

    private void Awake()
    {
        BuscarSprites();
        RefrescarMaterialesBase();
    }

    private void OnDisable()
    {
        if (rutinaDestello != null)
        {
            StopCoroutine(rutinaDestello);
            rutinaDestello = null;
        }

        RestaurarEstadoBase();
    }

    public void ConfigurarMaterialSilueta(Material nuevoMaterial)
    {
        materialSiluetaBlanca = nuevoMaterial;
    }

    public void MostrarDanio()
    {
        BuscarSprites();
        if (sprites.Count == 0) return;

        Material silueta = ObtenerMaterialSilueta();
        if (silueta == null)
        {
            Debug.LogWarning($"[{name}] No se encontro el material de silueta de dano.");
            return;
        }

        CantidadImpactos++;
        if (rutinaDestello != null)
        {
            StopCoroutine(rutinaDestello);
        }
        else
        {
            RefrescarMaterialesBase();
        }

        AplicarSilueta(silueta);

        if (Application.isPlaying)
        {
            rutinaDestello = StartCoroutine(RestaurarDespuesDelDestello());
        }
        else
        {
            RestaurarEstadoBase();
        }
    }

    /// <summary>
    /// Conserva compatibilidad con llamadas existentes. El destello ya no captura
    /// ni restaura colores porque estos pueden pertenecer a otro estado visual.
    /// </summary>
    public void RefrescarColoresBase()
    {
        RefrescarMaterialesBase();
    }

    /// <summary>Actualiza únicamente los materiales permanentes de los sprites.</summary>
    public void RefrescarMaterialesBase()
    {
        BuscarSprites();
        materialesBase.Clear();

        for (int i = 0; i < sprites.Count; i++)
        {
            SpriteRenderer sprite = sprites[i];
            materialesBase.Add(sprite != null ? sprite.sharedMaterials : null);
        }
    }

    private IEnumerator RestaurarDespuesDelDestello()
    {
        yield return new WaitForSeconds(duracionDestello);
        RestaurarEstadoBase();
        rutinaDestello = null;
    }

    private void AplicarSilueta(Material silueta)
    {
        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite == null) continue;

            Material[] materiales = sprite.sharedMaterials;
            int cantidadMateriales = Mathf.Max(1, materiales.Length);
            Material[] materialesSilueta = new Material[cantidadMateriales];

            for (int i = 0; i < materialesSilueta.Length; i++)
            {
                materialesSilueta[i] = silueta;
            }

            sprite.sharedMaterials = materialesSilueta;
        }
    }

    private void RestaurarEstadoBase()
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            SpriteRenderer sprite = sprites[i];
            if (sprite == null) continue;

            if (i < materialesBase.Count && materialesBase[i] != null)
            {
                sprite.sharedMaterials = materialesBase[i];
            }
        }
    }

    private Material ObtenerMaterialSilueta()
    {
        if (materialSiluetaBlanca != null) return materialSiluetaBlanca;
        if (materialSiluetaGenerado != null) return materialSiluetaGenerado;

        Shader shader = Shader.Find("Beagle/SiluetaDano");
        if (shader == null) return null;

        materialSiluetaGenerado = new Material(shader) { hideFlags = HideFlags.DontSave };
        return materialSiluetaGenerado;
    }

    private void BuscarSprites()
    {
        if (sprites.Count > 0) return;
        sprites.AddRange(GetComponentsInChildren<SpriteRenderer>(true));
    }
}
