using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque autocontenido del prefab de slash. Su Collider2D comparte posición,
/// rotación y escala con el sprite animado del arco.
/// </summary>
public class SlashKatana : MonoBehaviour
{
    private readonly HashSet<int> objetivosGolpeados = new HashSet<int>();

    private Jugador jugador;
    private Camera camaraPrincipal;
    private Collider2D hitbox;
    private Rigidbody2D cuerpoFisico;
    private int danoInfligido;
    private float fuerzaEmpujeEnemigo;
    private float fuerzaPogo;
    private float velocidadParry;
    private bool ataqueDescendente;
    private bool pogoRealizado;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        if (hitbox == null)
        {
            BoxCollider2D nuevaHitbox = gameObject.AddComponent<BoxCollider2D>();
            nuevaHitbox.size = new Vector2(0.8f, 0.58f);
            hitbox = nuevaHitbox;
        }
        hitbox.isTrigger = true;
        hitbox.enabled = false;

        cuerpoFisico = GetComponent<Rigidbody2D>();
        if (cuerpoFisico == null) cuerpoFisico = gameObject.AddComponent<Rigidbody2D>();
        cuerpoFisico.bodyType = RigidbodyType2D.Kinematic;
        cuerpoFisico.gravityScale = 0f;
        cuerpoFisico.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void Configurar(Jugador jugadorAtacante, int dano, float empujeEnemigo,
        float impulsoPogo, float velocidadDeParry, bool esDescendente,
        float retardo, float duracion)
    {
        jugador = jugadorAtacante;
        danoInfligido = dano;
        fuerzaEmpujeEnemigo = empujeEnemigo;
        fuerzaPogo = impulsoPogo;
        velocidadParry = velocidadDeParry;
        ataqueDescendente = esDescendente;
        pogoRealizado = false;
        objetivosGolpeados.Clear();
        camaraPrincipal = Camera.main;

        StartCoroutine(ActivarVentanaImpacto(retardo, duracion));
    }

    private IEnumerator ActivarVentanaImpacto(float retardo, float duracion)
    {
        yield return new WaitForSeconds(retardo);
        hitbox.enabled = true;
        yield return new WaitForSeconds(duracion);
        hitbox.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision) => ProcesarImpacto(collision);
    private void OnTriggerStay2D(Collider2D collision) => ProcesarImpacto(collision);

    private void ProcesarImpacto(Collider2D collision)
    {
        if (collision == null || jugador == null) return;

        if (collision.CompareTag("BalaEnemiga"))
        {
            BalaEnemiga balaEnemiga = collision.GetComponent<BalaEnemiga>();
            if (balaEnemiga != null)
            {
                RegistrarParry(balaEnemiga);
                return;
            }

            MisilTeledirigido misil = collision.GetComponent<MisilTeledirigido>();
            if (misil != null) RegistrarParry(misil);
            return;
        }

        PuntoCritico puntoCritico = collision.GetComponent<PuntoCritico>();
        SaludJefe saludJefe = collision.GetComponentInParent<SaludJefe>();
        SaludEnemigo saludEnemigo = collision.GetComponentInParent<SaludEnemigo>();
        SoldadoEnemigo soldado = collision.GetComponentInParent<SoldadoEnemigo>();
        if (puntoCritico == null && saludJefe == null && saludEnemigo == null && soldado == null) return;

        GameObject objetivo = saludJefe != null ? saludJefe.gameObject
            : puntoCritico != null ? puntoCritico.gameObject
            : saludEnemigo != null ? saludEnemigo.gameObject : soldado.gameObject;
        if (!objetivosGolpeados.Add(objetivo.GetInstanceID())) return;

        if (puntoCritico != null) puntoCritico.ImpactoCritico(danoInfligido);
        else if (saludJefe != null) saludJefe.RecibirDano(danoInfligido);
        else if (saludEnemigo != null) saludEnemigo.RecibirDano(danoInfligido);
        else soldado.RecibirDano(danoInfligido);

        if (saludJefe == null) AplicarEmpujeEnemigo(collision);
        EjecutarPogoSiCorresponde();
    }

    private void AplicarEmpujeEnemigo(Collider2D collision)
    {
        Rigidbody2D cuerpoEnemigo = collision.attachedRigidbody;
        if (cuerpoEnemigo == null) return;

        float direccionX = Mathf.Sign(cuerpoEnemigo.position.x - jugador.transform.position.x);
        if (Mathf.Approximately(direccionX, 0f)) direccionX = jugador.transform.localScale.x;
        cuerpoEnemigo.AddForce(Vector2.right * direccionX * fuerzaEmpujeEnemigo, ForceMode2D.Impulse);
    }

    private void EjecutarPogoSiCorresponde()
    {
        if (pogoRealizado || !ataqueDescendente || jugador.EstaEnSuelo) return;

        pogoRealizado = true;
        jugador.EjecutarPogo(fuerzaPogo);
    }

    private void RegistrarParry(BalaEnemiga balaEnemiga)
    {
        if (!objetivosGolpeados.Add(balaEnemiga.gameObject.GetInstanceID())) return;

        Rigidbody2D rbBala = balaEnemiga.GetComponent<Rigidbody2D>();
        if (rbBala == null || camaraPrincipal == null) return;

        Vector3 posicionMouse = camaraPrincipal.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direccionParry = ((Vector2)posicionMouse - rbBala.position).normalized;
        if (direccionParry.sqrMagnitude < 0.001f)
            direccionParry = rbBala.velocity.sqrMagnitude > 0.001f
                ? rbBala.velocity.normalized
                : Vector2.right;

        rbBala.velocity = direccionParry * velocidadParry;
        balaEnemiga.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direccionParry.y, direccionParry.x) * Mathf.Rad2Deg);
        balaEnemiga.Desviar();
    }

    private void RegistrarParry(MisilTeledirigido misil)
    {
        if (!objetivosGolpeados.Add(misil.gameObject.GetInstanceID())) return;

        Rigidbody2D rbMisil = misil.GetComponent<Rigidbody2D>();
        if (rbMisil == null || camaraPrincipal == null) return;

        Vector3 posicionMouse = camaraPrincipal.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direccionParry = ((Vector2)posicionMouse - rbMisil.position).normalized;
        if (direccionParry.sqrMagnitude < 0.001f)
            direccionParry = rbMisil.velocity.sqrMagnitude > 0.001f
                ? rbMisil.velocity.normalized
                : Vector2.right;

        misil.Desviar(direccionParry, velocidadParry);
    }
}
