using UnityEngine;

public class DestruirEfecto : MonoBehaviour
{
    // Start se ejecuta una sola vez al inicio de la vida del objeto
    void Start()
    {
        // Destruye el objeto (la explosión) después de 0.3 segundos
        Destroy(gameObject, 0.5f); 
    }
}