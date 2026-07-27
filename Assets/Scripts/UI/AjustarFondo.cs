using UnityEngine;

public class AjustarFondo : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // 1. Obtener el tamaño de la cámara en unidades de Unity
        float altoCamara = Camera.main.orthographicSize * 2f;
        float anchoCamara = altoCamara * Screen.width / Screen.height;

        // 2. Obtener el tamaño real del sprite
        float altoSprite = sr.sprite.bounds.size.y;
        float anchoSprite = sr.sprite.bounds.size.x;

        // 3. Aplicar la escala exacta para que cubra la pantalla completa
        transform.localScale = new Vector3(anchoCamara / anchoSprite, altoCamara / altoSprite, 1f);
    }
}