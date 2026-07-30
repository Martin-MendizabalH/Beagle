using UnityEngine;

/// <summary>
/// Configura y crea el prefab del slash. El daño y la hitbox viven en el prefab
/// para que la representación visual y el área de impacto permanezcan unidas.
/// </summary>
public class AtaqueMelee : MonoBehaviour
{
    [Header("--- Daño y empuje ---")]
    [SerializeField] private int danoInfligido = 20;
    [SerializeField] private float fuerzaEmpujeEnemigo = 4f;

    [Header("--- Pogo descendente ---")]
    [SerializeField] private float fuerzaPogo = 15f;

    [Header("--- Corte dirigido ---")]
    [Tooltip("Punto editable en el prefab desde el que nace el arco y la hitbox.")]
    [SerializeField] private Transform puntoOrigenSlash;
    [SerializeField] private float distanciaSlash = 0.9f;

    [Header("--- Prefab del slash ---")]
    [SerializeField] private GameObject prefabEfectoSlash;
    [Tooltip("Ajusta juntos la orientación visual y la hitbox del prefab.")]
    [SerializeField] private float correccionAnguloSlash = 0f;
    [SerializeField] private float retardoImpacto = 0.03f;
    [SerializeField] private float duracionImpacto = 0.1f;

    [Header("--- Parry ---")]
    [SerializeField] private float velocidadParry = 25f;

    private Jugador jugador;

    private void Awake()
    {
        jugador = GetComponentInParent<Jugador>();
    }

    /// <summary>Instancia un slash orientado al ángulo capturado al iniciar el ataque.</summary>
    public void PrepararAtaque(Vector2 direccionCorte)
    {
        if (jugador == null || prefabEfectoSlash == null || direccionCorte.sqrMagnitude < 0.001f) return;

        direccionCorte.Normalize();
        bool ataqueDescendente = direccionCorte.y < -0.5f;
        Vector2 origenSlash = puntoOrigenSlash != null
            ? puntoOrigenSlash.position
            : jugador.transform.position;
        Vector2 posicionSlash = origenSlash + direccionCorte * distanciaSlash;
        float anguloSlash = Mathf.Atan2(direccionCorte.y, direccionCorte.x) * Mathf.Rad2Deg;

        GameObject objetoSlash = Instantiate(prefabEfectoSlash, posicionSlash,
            Quaternion.Euler(0f, 0f, anguloSlash + correccionAnguloSlash));
        SlashKatana slash = objetoSlash.GetComponent<SlashKatana>();
        if (slash == null) slash = objetoSlash.AddComponent<SlashKatana>();

        slash.Configurar(jugador, danoInfligido, fuerzaEmpujeEnemigo, fuerzaPogo,
            velocidadParry, ataqueDescendente, retardoImpacto, duracionImpacto);
    }
}
