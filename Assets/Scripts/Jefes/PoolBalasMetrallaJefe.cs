using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reutiliza las balas de metralla para evitar picos de Instantiate/Destroy
/// durante cada lluvia.
/// </summary>
[DisallowMultipleComponent]
public class PoolBalasMetrallaJefe : MonoBehaviour
{
    private readonly Queue<GameObject> disponibles = new Queue<GameObject>();
    private readonly HashSet<GameObject> activas = new HashSet<GameObject>();
    private GameObject prefab;
    private Transform contenedor;
    private Vector3 escalaPrefab = Vector3.one;
    private bool destruyendo;

    public int CantidadActivas => activas.Count;
    public int CantidadDisponibles => disponibles.Count;
    public int CantidadTotal => activas.Count + disponibles.Count;

    public void Preparar(GameObject nuevoPrefab, int cantidadInicial)
    {
        if (nuevoPrefab == null) return;
        CrearContenedorSiHaceFalta();

        if (prefab != null && prefab != nuevoPrefab)
            Vaciar();

        prefab = nuevoPrefab;
        escalaPrefab = new Vector3(
            Mathf.Abs(prefab.transform.localScale.x),
            Mathf.Abs(prefab.transform.localScale.y),
            Mathf.Abs(prefab.transform.localScale.z));
        while (disponibles.Count + activas.Count < Mathf.Max(1, cantidadInicial))
            disponibles.Enqueue(CrearBala());
    }

    public GameObject Obtener(Vector2 posicion, Quaternion rotacion)
    {
        if (prefab == null) return null;
        CrearContenedorSiHaceFalta();

        GameObject bala = disponibles.Count > 0 ? disponibles.Dequeue() : CrearBala();
        if (bala == null) return null;

        // Las balas inactivas viven bajo el Jefe, que cambia de escala y se refleja
        // al mirar al jugador. Al sacarlas del pool restauramos explícitamente la
        // escala del prefab para que nunca hereden ese reflejo ni se encojan.
        bala.transform.SetParent(null, false);
        bala.transform.localScale = escalaPrefab;
        bala.transform.SetPositionAndRotation(posicion, rotacion);

        BalaEnemiga comportamiento = bala.GetComponent<BalaEnemiga>();
        comportamiento?.PrepararParaUso(Devolver);

        activas.Add(bala);
        bala.SetActive(true);
        return bala;
    }

    public void RetirarActivas()
    {
        GameObject[] copia = new GameObject[activas.Count];
        activas.CopyTo(copia);

        foreach (GameObject bala in copia)
        {
            if (bala == null) continue;
            BalaEnemiga comportamiento = bala.GetComponent<BalaEnemiga>();
            if (comportamiento != null) comportamiento.Retirar();
            else Devolver(bala);
        }
    }

    private GameObject CrearBala()
    {
        GameObject bala = Instantiate(prefab);
        bala.name = prefab.name + "_Reutilizable";
        bala.SetActive(false);
        bala.transform.SetParent(contenedor, false);
        bala.transform.localScale = escalaPrefab;
        bala.transform.localRotation = Quaternion.identity;
        return bala;
    }

    private void Devolver(GameObject bala)
    {
        if (bala == null || !activas.Remove(bala)) return;

        bala.SetActive(false);
        if (destruyendo)
        {
            Destroy(bala);
            return;
        }

        bala.transform.SetParent(contenedor, false);
        bala.transform.localScale = escalaPrefab;
        bala.transform.localRotation = Quaternion.identity;
        disponibles.Enqueue(bala);
    }

    private void CrearContenedorSiHaceFalta()
    {
        if (contenedor != null) return;

        Transform existente = transform.Find("Pool_Balas_Metralla");
        if (existente != null)
        {
            contenedor = existente;
            return;
        }

        GameObject objeto = new GameObject("Pool_Balas_Metralla");
        objeto.transform.SetParent(transform, false);
        contenedor = objeto.transform;
    }

    private void Vaciar()
    {
        RetirarActivas();
        while (disponibles.Count > 0)
        {
            GameObject bala = disponibles.Dequeue();
            if (bala != null) Destroy(bala);
        }
    }

    private void OnDestroy()
    {
        destruyendo = true;
        foreach (GameObject bala in activas)
        {
            if (bala != null) Destroy(bala);
        }
        activas.Clear();
    }
}
