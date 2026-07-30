using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona daño, parry y ciclo de vida de una bala enemiga. Puede destruirse
/// normalmente o regresar a un pool cuando el emisor proporciona una devolución.
/// </summary>
public class BalaEnemiga : MonoBehaviour
{
    [Header("--- Configuración de Daño ---")]
    [Tooltip("Cantidad de daño que esta bala inflige al jugador.")]
    public int dano = 10;

    [Tooltip("Daño que inflige a un enemigo después de ser desviada mediante parry.")]
    [Min(1)] public int danoAlSerDesviada = 10;

    [Header("--- Ciclo de Vida ---")]
    [Tooltip("Tiempo de seguridad antes de retirar una bala que no impactó nada.")]
    [Min(0.1f)] public float tiempoVida = 6f;

    [Tooltip("Mantiene el sprite un instante en la posición física del impacto.")]
    [Min(0f)] public float persistenciaVisualImpacto = 0.04f;

    private Rigidbody2D cuerpo;
    private Collider2D hitbox;
    private SpriteRenderer sprite;
    private Coroutine rutinaVida;
    private Coroutine rutinaFinalizacion;
    private Action<GameObject> devolverAlPool;
    private string tagOriginal;
    private Color colorOriginal;
    private bool fueDesviada;
    private bool impactoProcesado;
    private bool finalizando;

    public bool FueDesviada => fueDesviada;
    public bool EstaFinalizando => finalizando;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        tagOriginal = gameObject.tag;
        colorOriginal = sprite != null ? sprite.color : Color.white;
    }

    private void OnEnable()
    {
        RestablecerEstado();
        rutinaVida = StartCoroutine(EsperarTiempoDeVida());
    }

    private void OnDisable()
    {
        if (rutinaVida != null) StopCoroutine(rutinaVida);
        if (rutinaFinalizacion != null) StopCoroutine(rutinaFinalizacion);
        rutinaVida = null;
        rutinaFinalizacion = null;
    }

    /// <summary>Asigna la devolución utilizada por un pool antes de activar la bala.</summary>
    public void PrepararParaUso(Action<GameObject> nuevaDevolucion)
    {
        devolverAlPool = nuevaDevolucion;
    }

    /// <summary>Convierte la bala en un proyectil capaz de dañar enemigos.</summary>
    public void Desviar()
    {
        if (fueDesviada || finalizando) return;

        fueDesviada = true;
        gameObject.tag = "BalaJugador";
        if (sprite != null) sprite.color = Color.cyan;
    }

    /// <summary>Retira inmediatamente la bala, respetando el pool si existe.</summary>
    public void Retirar()
    {
        if (finalizando) return;
        finalizando = true;
        Liberar();
    }

    private IEnumerator EsperarTiempoDeVida()
    {
        yield return new WaitForSeconds(tiempoVida);
        rutinaVida = null;
        Retirar();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (finalizando) return;

        if (fueDesviada)
        {
            ProcesarImpactoDesviado(collision);
            return;
        }

        if (collision.CompareTag("Player"))
        {
            Jugador jugador = collision.GetComponentInParent<Jugador>();
            if (jugador != null)
            {
                jugador.RecibirDano(dano, transform.position);
            }
            else
            {
                Debug.LogWarning(
                    "<color=yellow>[BalaEnemiga] Impacto con Player detectado, " +
                    "pero no se encontró el script Jugador.</color>");
            }

            FinalizarPorImpacto();
        }
        else if (EsEntorno(collision))
        {
            FinalizarPorImpacto();
        }
    }

    private void ProcesarImpactoDesviado(Collider2D collision)
    {
        if (impactoProcesado || finalizando) return;

        // El proyectil reflejado ya no puede herir al jugador que hizo el parry.
        if (collision.CompareTag("Player")) return;

        PuntoCritico puntoCritico = collision.GetComponent<PuntoCritico>();
        if (puntoCritico != null)
        {
            impactoProcesado = true;
            puntoCritico.ImpactoCritico(danoAlSerDesviada);
            FinalizarPorImpacto();
            return;
        }

        SaludJefe saludJefe = collision.GetComponentInParent<SaludJefe>();
        SaludEnemigo saludEnemigo = collision.GetComponentInParent<SaludEnemigo>();
        SoldadoEnemigo soldado = collision.GetComponentInParent<SoldadoEnemigo>();

        if (saludJefe != null)
        {
            impactoProcesado = true;
            saludJefe.RecibirDano(danoAlSerDesviada);
            FinalizarPorImpacto();
        }
        else if (saludEnemigo != null)
        {
            impactoProcesado = true;
            saludEnemigo.RecibirDano(danoAlSerDesviada);
            FinalizarPorImpacto();
        }
        else if (soldado != null)
        {
            impactoProcesado = true;
            soldado.RecibirDano(danoAlSerDesviada);
            FinalizarPorImpacto();
        }
        else if (EsEntorno(collision))
        {
            impactoProcesado = true;
            FinalizarPorImpacto();
        }
    }

    private void FinalizarPorImpacto()
    {
        if (finalizando) return;
        finalizando = true;

        if (rutinaVida != null)
        {
            StopCoroutine(rutinaVida);
            rutinaVida = null;
        }

        // Fuerza el sprite a la pose física real antes de desactivar la simulación.
        // Así la interpolación no lo deja visualmente suspendido sobre el suelo.
        if (cuerpo != null)
        {
            transform.position = cuerpo.position;
            transform.rotation = Quaternion.Euler(0f, 0f, cuerpo.rotation);
            cuerpo.velocity = Vector2.zero;
            cuerpo.angularVelocity = 0f;
            cuerpo.simulated = false;
        }

        if (hitbox != null) hitbox.enabled = false;

        if (persistenciaVisualImpacto > 0f)
            rutinaFinalizacion = StartCoroutine(LiberarDespuesDelImpacto());
        else
            Liberar();
    }

    private IEnumerator LiberarDespuesDelImpacto()
    {
        yield return new WaitForSeconds(persistenciaVisualImpacto);
        rutinaFinalizacion = null;
        Liberar();
    }

    private void Liberar()
    {
        Action<GameObject> devolucion = devolverAlPool;
        if (devolucion != null)
            devolucion(gameObject);
        else
            Destroy(gameObject);
    }

    private void RestablecerEstado()
    {
        fueDesviada = false;
        impactoProcesado = false;
        finalizando = false;

        if (!string.IsNullOrEmpty(tagOriginal)) gameObject.tag = tagOriginal;
        if (sprite != null)
        {
            sprite.color = colorOriginal;
            sprite.enabled = true;
        }

        if (hitbox != null) hitbox.enabled = true;
        if (cuerpo != null)
        {
            cuerpo.simulated = true;
            cuerpo.velocity = Vector2.zero;
            cuerpo.angularVelocity = 0f;
        }
    }

    private static bool EsEntorno(Collider2D collision)
    {
        if (collision.CompareTag("Pared")) return true;
        int capaSuelo = LayerMask.NameToLayer("Suelo");
        if (capaSuelo >= 0 && collision.gameObject.layer == capaSuelo) return true;
        if (collision.isTrigger) return false;

        Rigidbody2D cuerpoImpactado = collision.attachedRigidbody;
        return cuerpoImpactado == null ||
            cuerpoImpactado.bodyType == RigidbodyType2D.Static;
    }
}
