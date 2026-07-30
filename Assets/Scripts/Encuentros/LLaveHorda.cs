using UnityEngine;

public class LlaveHorda : MonoBehaviour
{
    [Tooltip("Arrastra aquí tu objeto SalidaNivel (el que te teletransporta)")]
    public GameObject puertaSalida;

    // Como NO le pusimos "Is Trigger" a la llave, usamos OnCollisionEnter2D
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 1. Activa la salida para que puedas escapar
            if (puertaSalida != null)
            {
                puertaSalida.SetActive(true);
            }
            
            // 2. Destruye la llave porque ya la recogiste
            Destroy(gameObject);
        }
    }
}