using System.Collections;
using UnityEngine;

// Agregamos esta línea para obligar a Unity a ponerle vida al Golem
[RequireComponent(typeof(SaludEnemigo))] 
public class GolemIA : MonoBehaviour
{
    [Header("Configuración de IA")]
    public float velocidad = 2f;
    public float distanciaDeteccion = 8f; 
    public float distanciaAtaque = 1.2f;  
    public int danoAtaque = 1;

    private Transform jugador;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    
    // Variable para saber si ya está golpeando y no repetir la animación
    private bool estaAtacando = false; 

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        // Si NO está atacando actualmente, puede pensar qué hacer
        if (!estaAtacando)
        {
            if (distancia <= distanciaDeteccion && distancia > distanciaAtaque)
            {
                PerseguirJugador();
            }
            else if (distancia <= distanciaAtaque)
            {
                // Iniciar la rutina de ataque con pausa
                StartCoroutine(RutinaAtaque());
            }
            else
            {
                anim.SetBool("isWalking", false);
            }
        }
    }

    void PerseguirJugador()
    {
        anim.SetBool("isWalking", true);

        if (jugador.position.x > transform.position.x)
            spriteRenderer.flipX = false; 
        else
            spriteRenderer.flipX = true;  

        Vector2 posicionObjetivo = new Vector2(jugador.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, posicionObjetivo, velocidad * Time.deltaTime);
    }

    // Corrutina: Permite hacer pausas en el tiempo
    System.Collections.IEnumerator RutinaAtaque()
    {
        estaAtacando = true; // Bloquea otros movimientos
        anim.SetBool("isWalking", false);
        anim.SetTrigger("Atacar"); // Estira el brazo

        // Espera exactamente 1 segundo
        yield return new WaitForSeconds(1f);

        estaAtacando = false; // Le permite volver a moverse o atacar
    }

    // Detecta el golpe físico contra el jugador
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // BUSCAMOS TU SCRIPT "Jugador" PARA HACERLE DAÑO
            Jugador scriptJugador = collision.gameObject.GetComponent<Jugador>();
            
            if (scriptJugador != null)
            {
                // Le pasamos el daño (1) y la posición del Golem para el empuje
                scriptJugador.RecibirDano(danoAtaque, transform.position);
                Debug.Log("¡El Golem le hizo daño al Beagle!");
            }
        }
    }
}