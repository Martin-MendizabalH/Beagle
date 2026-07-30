using UnityEngine;

/// <summary>
/// Misil teledirigido que puede ser evadido, estrellado contra el entorno
/// o devuelto al Jefe mediante un parry.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MisilTeledirigido : MonoBehaviour
{
    [Header("--- Vuelo y Físicas ---")]
    [Min(0f)] public float velocidad = 4f;
    [Min(0f)] public float velocidadRotacion = 150f;
    [Min(0f)] public float velocidadSalidaVertical = 6f;
    [Min(0f)] public float duracionSalidaVertical = 0.35f;

    [Header("--- Daño y Vida ---")]
    [Min(1)] public int danoAlJugador = 1;
    [Min(1)] public int danoAlSerDesviado = 25;
    [Min(0.1f)] public float tiempoDeVida = 7f;

    private Transform jugador;
    private Transform emisor;
    private Rigidbody2D rb;
    private Collider2D hitbox;
    private Collider2D[] hitboxesEmisor;
    private EfectosVisualesJefeTanque efectosJefe;
    private bool fueDesviado;
    private bool impactoProcesado;
    private float tiempoSiguienteHumo;

    public bool FueDesviado => fueDesviado;
    public bool EstaEnSalidaVertical =>
        !fueDesviado && Time.time < tiempoFinSalidaVertical;

    private float tiempoFinSalidaVertical;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        IniciarSalidaVertical();
    }

    private void Start()
    {
        efectosJefe = FindObjectOfType<EfectosVisualesJefeTanque>();
        Destroy(gameObject, tiempoDeVida);

        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJugador != null) jugador = objetoJugador.transform;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (!fueDesviado && Time.time < tiempoFinSalidaVertical)
        {
            rb.angularVelocity = 0f;
            rb.rotation = 90f;
            rb.velocity = Vector2.up * velocidadSalidaVertical;
        }
        else if (!fueDesviado && jugador != null)
        {
            Vector2 direccion = ((Vector2)jugador.position - rb.position).normalized;
            float cantidadGiro = Vector3.Cross(transform.right, direccion).z;
            rb.angularVelocity = cantidadGiro * velocidadRotacion;
            rb.velocity = transform.right * velocidad;
        }

        if (Time.time >= tiempoSiguienteHumo)
        {
            tiempoSiguienteHumo = Time.time + 0.08f;
            efectosJefe?.EmitirHumoMisil(transform.position);
        }
    }

    /// <summary>
    /// Define quién disparó el misil. Mientras sea hostil, sus colliders se
    /// ignoran mutuamente; el parry reactiva esas colisiones.
    /// </summary>
    public void ConfigurarEmisor(GameObject nuevoEmisor)
    {
        emisor = nuevoEmisor != null ? nuevoEmisor.transform.root : null;
        hitboxesEmisor = emisor != null
            ? emisor.GetComponentsInChildren<Collider2D>(true)
            : null;

        ConfigurarColisionesConEmisor(true);
        IniciarSalidaVertical();
    }

    public void Desviar(Vector2 direccion, float velocidadDesvio)
    {
        if (fueDesviado || rb == null) return;

        fueDesviado = true;
        gameObject.tag = "BalaJugador";
        ConfigurarColisionesConEmisor(false);
        rb.angularVelocity = 0f;
        rb.velocity = direccion.normalized * Mathf.Max(velocidadDesvio, velocidad * 1.5f);
        rb.rotation = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = Color.cyan;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (impactoProcesado) return;

        if (!fueDesviado && PerteneceAlEmisor(collision)) return;

        if (fueDesviado)
        {
            ProcesarImpactoDesviado(collision);
            return;
        }

        if (collision.CompareTag("Player"))
        {
            Jugador scriptJugador = collision.GetComponentInParent<Jugador>();
            if (scriptJugador != null)
                scriptJugador.RecibirDano(danoAlJugador, transform.position);

            impactoProcesado = true;
            Explotar();
        }
        else if (collision.CompareTag("Pared"))
        {
            impactoProcesado = true;
            Explotar();
        }
    }

    private void ProcesarImpactoDesviado(Collider2D collision)
    {
        if (collision.CompareTag("Player")) return;

        PuntoCritico puntoCritico = collision.GetComponent<PuntoCritico>();
        if (puntoCritico != null)
        {
            impactoProcesado = true;
            puntoCritico.ImpactoCritico(danoAlSerDesviado);
            Explotar();
            return;
        }

        SaludJefe saludJefe = collision.GetComponentInParent<SaludJefe>();
        SaludEnemigo saludEnemigo = collision.GetComponentInParent<SaludEnemigo>();
        SoldadoEnemigo soldado = collision.GetComponentInParent<SoldadoEnemigo>();

        if (saludJefe != null)
        {
            impactoProcesado = true;
            saludJefe.RecibirDano(danoAlSerDesviado);
            Explotar();
        }
        else if (saludEnemigo != null)
        {
            impactoProcesado = true;
            saludEnemigo.RecibirDano(danoAlSerDesviado);
            Explotar();
        }
        else if (soldado != null)
        {
            impactoProcesado = true;
            soldado.RecibirDano(danoAlSerDesviado);
            Explotar();
        }
        else if (collision.CompareTag("Pared"))
        {
            impactoProcesado = true;
            Explotar();
        }
    }

    private void Explotar()
    {
        efectosJefe?.EmitirExplosionEn(transform.position);
        Destroy(gameObject);
    }

    private void IniciarSalidaVertical()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        tiempoFinSalidaVertical = Time.time + duracionSalidaVertical;
        rb.angularVelocity = 0f;
        rb.rotation = 90f;
        rb.velocity = Vector2.up * velocidadSalidaVertical;
    }

    private void ConfigurarColisionesConEmisor(bool ignorar)
    {
        if (hitbox == null || hitboxesEmisor == null) return;

        foreach (Collider2D hitboxEmisor in hitboxesEmisor)
        {
            if (hitboxEmisor != null && hitboxEmisor != hitbox)
                Physics2D.IgnoreCollision(hitbox, hitboxEmisor, ignorar);
        }
    }

    private bool PerteneceAlEmisor(Collider2D collision)
    {
        return emisor != null && collision != null &&
            collision.transform.root == emisor;
    }
}
