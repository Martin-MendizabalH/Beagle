using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisparoBeagle : MonoBehaviour
{
    public GameObject balaPrefab;
    public float offsetDisparo = 0.6f;
    private Vector2 ultimaDireccion = Vector2.right;
    private Collider2D col; // 👈 referencia al collider

    void Start()
    {
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        if (x != 0 || y != 0)
        {
            ultimaDireccion = new Vector2(x, y).normalized;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))    Disparar(Vector2.up);
        if (Input.GetKeyDown(KeyCode.DownArrow))  Disparar(Vector2.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  Disparar(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Disparar(Vector2.right);
    }

    void Disparar(Vector2 dir)
    {
        // Usa el centro del collider en vez de transform.position
        Vector2 centroCollider = col != null ? col.bounds.center : (Vector2)transform.position;
        Vector2 spawnPos = centroCollider + dir * offsetDisparo;

        GameObject balaBeagle = Instantiate(balaPrefab, spawnPos, Quaternion.identity);
        BalaBeagle scriptBala = balaBeagle.GetComponent<BalaBeagle>();

        if (scriptBala != null)
        {
            scriptBala.ConfigurarDireccion(dir);
        }
    }
}