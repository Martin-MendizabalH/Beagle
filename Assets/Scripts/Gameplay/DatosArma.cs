using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contenedor de datos para definir las propiedades únicas de cada arma.
/// Permite crear configuraciones personalizadas desde el menú de Unity.
/// </summary>
[CreateAssetMenu(fileName = "NuevaArma", menuName = "Beagle/Arma", order = 1)]
public class DatosArma : ScriptableObject
{
    [Header("Información General")]
    public string nombreArma;       // Pistola, Metralleta, Katana
    public Sprite iconoArma;        // Icono para la UI breve

    [Header("Estadísticas de Combate")]
    public float daño;
    public float cadenciaFuego;     // Tiempo de espera entre ataques/disparos
    public bool esCuerpoACuerpo;   // True para la Katana, False para armas de fuego

    [Header("Prefabs Asociados")]
    public GameObject proyectilPrefab; // Prefab de la bala (no aplica a la Katana)
}
