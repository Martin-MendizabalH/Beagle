using UnityEngine;

/// <summary>El efecto visual se destruye una vez que su animación termina.</summary>
public class EfectoSlash : MonoBehaviour
{
    [SerializeField] private float tiempoDeVida = 0.45f;

    private void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }
}
