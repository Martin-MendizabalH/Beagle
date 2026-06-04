using UnityEngine;

public class BalaBeagle : MonoBehaviour
{
    private Vector2 direccion;
    public float velocidad = 15f;

    void Start()
    {
        // 1. Buscamos al jugador por su etiqueta "Player"
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");

        // 2. Si lo encontramos, hacemos que la bala ignore sus colisiones
        if (jugador != null)
        {
            Collider2D colisionadorJugador = jugador.GetComponent<Collider2D>();
            Collider2D colisionadorBala = GetComponent<Collider2D>();

            if (colisionadorJugador != null && colisionadorBala != null)
            {
                Physics2D.IgnoreCollision(colisionadorBala, colisionadorJugador);
            }
        }

        // Destrucción automática a los 10 segundos
        Destroy(gameObject, 10f);
    }
    public void IgnorarShooter(GameObject shooter)
    {
        Collider2D colisionadorBala = GetComponent<Collider2D>();
        foreach (Collider2D col in shooter.GetComponentsInChildren<Collider2D>())
        {
            Physics2D.IgnoreCollision(colisionadorBala, col);
        }
    }

    public void ConfigurarDireccion(Vector2 dir)
    {
        direccion = dir;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    void Update()
    {
        transform.Translate(direccion * velocidad * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Como ahora la bala ignora al jugador en el Start, 
        // este Trigger no se activará con el jugador.
        if (!collision.CompareTag("Player"))
        {
            Debug.Log("Impacto contra: " + collision.name);
            Destroy(gameObject);
        }
    }
    
}