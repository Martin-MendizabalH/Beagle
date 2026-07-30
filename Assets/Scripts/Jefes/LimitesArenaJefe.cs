using UnityEngine;

/// <summary>
/// Define el espacio horizontal utilizable por un encuentro de Jefe.
/// Se coloca normalmente en el centro de la cámara fija de la arena.
/// </summary>
public class LimitesArenaJefe : MonoBehaviour
{
    [Header("--- Dimensiones de la Arena ---")]
    [Min(2f)] public float ancho = 23f;
    [Min(0f)] public float margenInterior = 0.8f;
    [Min(1f)] public float alturaGizmo = 8f;

    public float Izquierda => transform.position.x - ancho * 0.5f + margenInterior;
    public float Derecha => transform.position.x + ancho * 0.5f - margenInterior;
    public float Centro => transform.position.x;

    public float LimitarX(float posicionX)
    {
        return Mathf.Clamp(posicionX, Izquierda, Derecha);
    }

    public bool EstaCercaDelLimite(float posicionX, float direccion, float distancia)
    {
        if (direccion < 0f) return posicionX - Izquierda <= distancia;
        return Derecha - posicionX <= distancia;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f);
        Vector3 centro = new Vector3(transform.position.x, transform.position.y, 0f);
        Gizmos.DrawWireCube(centro, new Vector3(ancho, alturaGizmo, 0f));

        Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
        float anchoInterior = Mathf.Max(0.1f, ancho - margenInterior * 2f);
        Gizmos.DrawWireCube(centro, new Vector3(anchoInterior, alturaGizmo * 0.9f, 0f));
    }
}
