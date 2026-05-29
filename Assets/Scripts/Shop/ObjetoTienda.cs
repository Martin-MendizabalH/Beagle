using UnityEngine;

[CreateAssetMenu(fileName = "NuevoObjetoTienda", menuName = "Tienda/Nuevo Objeto Tienda")]
public class ObjetoTienda : ScriptableObject
{
    public string nombreObjeto;
    public int precio;
    public Sprite icono;
    public GameObject prefabObjeto; // Si el objeto tiene representación fisica
}