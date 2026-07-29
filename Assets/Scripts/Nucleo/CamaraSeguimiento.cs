using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    public Transform jugador; 
    
    [Header("Configuración de Seguimiento")]
    // Este vector empuja la cámara hacia adelante y hacia arriba respecto al jugador
    public Vector2 desfase = new Vector2(5.5f, 0f); 

    [Header("Límites de la Cámara en X")]
    public float limiteIzquierdo = 0f; // Ajusta este valor al punto de inicio de tu cámara
    public float limiteDerecho = 50f;   

    void Update() // Update se ejecuta en cada frame para actualizar la posición constantemente [3]
    {
        if (jugador != null)
        {
            // 1. Calculamos dónde DEBERÍA estar la cámara sumando el desfase al jugador
            float posicionDeseadaX = jugador.position.x + desfase.x;
            float posicionDeseadaY = jugador.position.y + desfase.y;

            // 2. Le aplicamos el límite SOLO al eje X para que la cámara frene en los bordes
            float posicionCamaraX = Mathf.Clamp(posicionDeseadaX, limiteIzquierdo, limiteDerecho);

            // 3. Movemos la cámara. Ahora sigue la Y del jugador (salto) y mantiene su profundidad Z original
            transform.position = new Vector3(posicionCamaraX, posicionDeseadaY, transform.position.z);
        }
    }
}