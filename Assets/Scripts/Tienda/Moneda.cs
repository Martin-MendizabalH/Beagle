using UnityEngine;

/// <summary>Moneda recogible que suma su valor al monedero compartido del jugador.</summary>
public class Moneda : MonoBehaviour
{
    [SerializeField] private int valor = 5;
    [SerializeField] private AudioClip sonidoColeccion;
    [SerializeField] private float tiempoAntesDeRecoger = 0.12f;
    [SerializeField, Min(0f)] private float escalaGravedad = 1.15f;
    [SerializeField, Min(0f)] private float resistenciaAire = 0.35f;

    private Rigidbody2D cuerpoFisico;
    private bool puedeRecogerse;
    private bool fueRecogida;

    public int Valor => valor;

    private void Awake()
    {
        cuerpoFisico = GetComponent<Rigidbody2D>();
        if (cuerpoFisico == null) cuerpoFisico = gameObject.AddComponent<Rigidbody2D>();

        cuerpoFisico.bodyType = RigidbodyType2D.Dynamic;
        cuerpoFisico.gravityScale = escalaGravedad;
        cuerpoFisico.drag = resistenciaAire;
        cuerpoFisico.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
        Invoke(nameof(ActivarRecogida), tiempoAntesDeRecoger);
        Destroy(gameObject, 15f);
    }

    public void ConfigurarValor(int nuevoValor)
    {
        valor = Mathf.Max(1, nuevoValor);
    }

    public void Lanzar(Vector2 velocidadInicial)
    {
        if (cuerpoFisico == null) cuerpoFisico = GetComponent<Rigidbody2D>();
        if (cuerpoFisico == null) return;

        cuerpoFisico.velocity = velocidadInicial;
        cuerpoFisico.angularVelocity = 0f;
    }

    private void ActivarRecogida() => puedeRecogerse = true;

    public void HabilitarRecogidaInmediata() => puedeRecogerse = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        IntentarRecoger(collision);
    }

    public bool IntentarRecoger(Collider2D collision)
    {
        return collision != null && collision.CompareTag("Player") && Recoger();
    }

    public bool Recoger()
    {
        if (!puedeRecogerse || fueRecogida) return false;
        if (Tienda.Instancia == null)
        {
            Debug.LogWarning("No existe una Tienda activa para registrar la moneda recogida.");
            return false;
        }

        fueRecogida = true;
        Tienda.Instancia.AgregarMonedas(valor);

        if (sonidoColeccion != null)
        {
            AudioSource.PlayClipAtPoint(sonidoColeccion, transform.position);
        }

        DestruirMoneda();
        return true;
    }

    private void DestruirMoneda()
    {
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
}
