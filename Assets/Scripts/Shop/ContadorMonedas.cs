using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ContadorMonedas : MonoBehaviour
{
    private TextMeshProUGUI textoMonedas;

    // Start is called before the first frame update
    void Start()
    {
        textoMonedas = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (textoMonedas != null && Tienda.Instancia != null)
        {
            textoMonedas.text = Tienda.Instancia.dineroJugador.ToString(); // Actualiza el texto con el dinero actual del jugador
        }
    }
}
