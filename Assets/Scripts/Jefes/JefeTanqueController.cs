using System.Collections;
using UnityEngine;

/// <summary>
/// Inteligencia Artificial principal del Jefe Tanque.
/// Controla la máquina de estados, el seguimiento del jugador mediante físicas 
/// y la ejecución de ataques modulares con telegrafiado visual (Principio DRY).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))] // Garantiza que Unity añada este componente automáticamente
[RequireComponent(typeof(SpriteRenderer))]
public class JefeTanqueController : MonoBehaviour
{
    [Header("--- Referencias Internas ---")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private Transform jugador;
    private SaludJefe saludJefe; 
    
    // Almacena la escala original pura (absoluta) asignada en el Inspector para evitar deformaciones
    private Vector3 escalaOriginal;

    [Header("--- Puntos de Disparo ---")]
    [Tooltip("Transform vacío posicionado en el cañón principal.")]
    public Transform puntoDisparoCanon;
    [Tooltip("Transform vacío posicionado arriba del tanque para la lluvia de balas.")]
    public Transform puntoDisparoMetralla;
    public LineRenderer lineaLaser;

    [Header("--- Prefabs de Ataque ---")]
    [Tooltip("Prefab de la bala enemiga que contiene el script 'MovimientoProyectil'.")]
    public GameObject balaMetrallaPrefab;
    public GameObject misilPrefab; 

    [Header("--- Configuración de Movimiento ---")]
    [Tooltip("Velocidad de persecución del jefe durante sus tiempos de recarga.")]
    public float velocidadMovimiento = 3f;
    [Tooltip("Tiempo en segundos que el jefe persigue al jugador antes de atacar.")]
    public float tiempoEntreAtaques = 2.5f;

    [Header("--- Parámetros: Lluvia de Metralla ---")]
    [Tooltip("Cantidad exacta de balas que lloverán cubriendo toda la arena.")]
    public int cantidadBalasMetralla = 5;
    [Tooltip("Ancho total de la arena (en unidades de Unity) para calcular la parábola perfecta.")]
    public float anchoDeLaArena = 23f;
    [Tooltip("Fuerza vertical (salto) que tendrán las balas al salir disparadas.")]
    public float fuerzaSaltoMetralla = 12f;

    [Header("--- Parámetros: Embestida y Láser ---")]
    public float velocidadAnticipacionEmbestida = 4f;
    public float velocidadEmbestida = 20f;
    public int danoLaser = 1;

    [Tooltip("Tiempo en segundos que el láser se mantiene a su máximo grosor antes de desvanecerse.")]
    public float tiempoMantenimientoLaser = 0.5f; // NUEVA VARIABLE

    [Header("--- Colores de Telegrafiado ---")]
    [Tooltip("Color de parpadeo para el Láser.")]
    public Color colorAvisoLaser = Color.red;
    [Tooltip("Color de parpadeo para la Metralla.")]
    public Color colorAvisoMetralla = Color.yellow;
    [Tooltip("Color de parpadeo para la Embestida.")]
    public Color colorAvisoEmbestida = Color.gray; 
    [Tooltip("Color de parpadeo para el Misil (Fase 2).")]
    public Color colorAvisoMisil = Color.magenta;

    // Bandera de control para la Máquina de Estados
    private bool estaAtacando = false;

    // ==========================================
    // CICLO DE VIDA DE UNITY
    // ==========================================

    void Awake()
    {
        // Obtenemos las referencias a los componentes en el primer frame de existencia[cite: 1, 2]
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        saludJefe = GetComponent<SaludJefe>(); 
        
        if (spriteRenderer != null) colorOriginal = spriteRenderer.color;

        // Guardamos el tamaño absoluto para girar el sprite limpiamente con transform.localScale[cite: 1]
        escalaOriginal = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void OnEnable()
    {
        // Se ejecuta cuando el ArenaManager enciende este script
        if (lineaLaser != null) lineaLaser.enabled = false; 
        StartCoroutine(CicloDeCombate());
    }

    void FixedUpdate()
    {
        // FixedUpdate es el lugar correcto para manipular físicas continuas[cite: 1]
        // Solo perseguimos al jugador si NO estamos ejecutando un ataque
        if (jugador != null && !estaAtacando)
        {
            MirarAlJugador();
            MoverHaciaJugador();
        }
    }

    // ==========================================
    // LÓGICA DE PERSECUCIÓN
    // ==========================================

    private void MirarAlJugador()
    {
        // Giramos al jefe invirtiendo el valor X de la escala, basándonos en la posición del jugador[cite: 1]
        if (jugador.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(-escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
        }
        else if (jugador.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(escalaOriginal.x, escalaOriginal.y, escalaOriginal.z);
        }
    }

    private void MoverHaciaJugador()
    {
        // Extraemos la dirección leyendo hacia dónde mira la escala
        float direccionX = transform.localScale.x > 0 ? -1f : 1f;

        // Aplicamos la velocidad al Rigidbody2D conservando la gravedad en Y[cite: 2]
        rb.velocity = new Vector2(direccionX * velocidadMovimiento, rb.velocity.y); 
    }

    // ==========================================
    // MÁQUINA DE ESTADOS
    // ==========================================

    private IEnumerator CicloDeCombate()
    {
        // Pausa inicial para que el jugador asimile la entrada a la arena
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            // Búsqueda dinámica del jugador (A prueba de fallos si la escena reinicia)
            if (jugador == null)
            {
                GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
                if (objJugador != null) jugador = objJugador.transform;
            }

            if (jugador != null)
            {
                // Si la salud está en Fase 2, habilitamos el 4to ataque (Misil)
                int opcionesDeAtaque = (saludJefe != null && saludJefe.estaEnFase2) ? 4 : 3;
                int ataqueAleatorio = Random.Range(0, opcionesDeAtaque);
                
                // Ejecutamos la Corrutina del ataque y ESPERAMOS a que termine por completo
                yield return StartCoroutine(EjecutarAtaque(ataqueAleatorio));

                // Tiempo de respiro/persecución antes del siguiente ataque
                yield return new WaitForSeconds(tiempoEntreAtaques);
            }
            else
            {
                // Si no hay jugador (ej. murió), esperamos al siguiente frame para no colapsar la memoria
                yield return null; 
            }
        }
    }

    private IEnumerator EjecutarAtaque(int tipoAtaque)
    {
        // Bloqueamos la persecución en el FixedUpdate
        estaAtacando = true; 
        
        switch (tipoAtaque)
        {
            case 0: yield return StartCoroutine(AtaqueLaserInstantaneo()); break;
            case 1: yield return StartCoroutine(AtaqueMetralla()); break;
            case 2: yield return StartCoroutine(AtaqueEmbestida()); break;
            case 3: yield return StartCoroutine(AtaqueMisilTeledirigido()); break;
        }

        // Restaurar estado visual tras el ataque (mantiene el rojo si está en fase 2)
        spriteRenderer.color = (saludJefe != null && saludJefe.estaEnFase2) ? new Color(1f, 0.6f, 0.6f) : colorOriginal;
        
        // Liberamos la bandera para que vuelva a perseguir
        estaAtacando = false; 
    }

    // ==========================================
    // RUTINA CENTRALIZADA DE TELEGRAFIADO (DRY)
    // ==========================================

    /// <summary>
    /// Gestiona el parpadeo de color y la anticipación de movimiento para cualquier ataque.
    /// </summary>
    private IEnumerator RutinaTelegrafiado(Color colorAviso, bool aplicarRetroceso = false)
    {
        float tiempoAnticipacion = 1f;
        float tiempoParpadeo = 0.1f; 
        bool alternadorColor = false; 

        // 1. Físicas de anticipación (Retroceder tomando impulso o Freno en seco)
        if (aplicarRetroceso)
        {
            float dirX = transform.localScale.x > 0 ? -1f : 1f;
            rb.velocity = new Vector2(-dirX * velocidadAnticipacionEmbestida, rb.velocity.y); //[cite: 2]
        }
        else
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        // 2. Efecto visual de parpadeo
        for (float t = 0; t < tiempoAnticipacion; t += tiempoParpadeo)
        {
            Color colorBase = (saludJefe != null && saludJefe.estaEnFase2) ? new Color(1f, 0.6f, 0.6f) : colorOriginal;
            spriteRenderer.color = alternadorColor ? colorAviso : colorBase;
            alternadorColor = !alternadorColor;
            
            yield return new WaitForSeconds(tiempoParpadeo);
        }

        // 3. Freno dramático final, color sólido justo antes del impacto
        spriteRenderer.color = colorAviso;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        yield return new WaitForSeconds(0.15f);
    }

    // ==========================================
    // MÉTODOS DE ATAQUE INDIVIDUALES
    // ==========================================

    private IEnumerator AtaqueLaserInstantaneo()
    {
        // 1. EVALUACIÓN DE FASE Y DIRECCIÓN INICIAL
        bool esFase2 = (saludJefe != null && saludJefe.estaEnFase2);
        
        // Obtenemos hacia dónde mira el tanque (-1 izquierda, 1 derecha)
        float dirX = transform.localScale.x > 0 ? -1f : 1f;
        Vector2 direccionBase = new Vector2(dirX, 0f);
        Vector2 direccionDisparo = direccionBase;

        // FASE 1: Memoria Fotográfica del objetivo
        if (!esFase2 && jugador != null)
        {
            // Guardamos la posición del jugador ANTES de iniciar la anticipación
            Vector2 posicionObjetivo = jugador.position;
            // Calculamos el vector direccional hacia ese punto específico
            direccionDisparo = (posicionObjetivo - (Vector2)puntoDisparoCanon.position).normalized;
        }

        // 2. ANTICIPACIÓN (Telegrafiado visual)
        // Durante este segundo, el jugador tiene tiempo de salir de la zona donde estaba
        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoLaser, false));

        // Validación de seguridad de referencias
        if (puntoDisparoCanon == null || lineaLaser == null) yield break;

        lineaLaser.enabled = true;
        bool jugadorDañado = false; 

        // 3. FASE DE MANTENIMIENTO Y DISPARO (Raycast Continuo)
        float tiempoRestante = tiempoMantenimientoLaser;

        while (tiempoRestante > 0)
        {
            // Actualizamos el origen visual del láser
            lineaLaser.SetPosition(0, puntoDisparoCanon.position);
            
            // Por defecto, usamos la dirección calculada (Apunta al jugador en Fase 1)
            Vector2 direccionActual = direccionDisparo;

            // FASE 2: Barrido de Área (Sweep de -45° a +45°)
            if (esFase2)
            {
                // Calculamos el progreso del láser (0.0 al inicio, 1.0 al final)
                float progreso = 1f - (tiempoRestante / tiempoMantenimientoLaser);

                // Calculamos el ángulo actual. 
                // Multiplicamos por dirX para que el barrido SIEMPRE sea de abajo hacia arriba sin importar a dónde mire el tanque.
                float anguloBarrido = Mathf.Lerp(-45f, 45f, progreso) * dirX;

                // Rotamos el vector base usando trigonometría pura de Unity
                Quaternion rotacion = Quaternion.Euler(0, 0, anguloBarrido);
                direccionActual = rotacion * direccionBase;
            }

            // FÍSICAS: Disparamos el rayo continuo en la dirección correspondiente a la Fase
            RaycastHit2D[] impactos = Physics2D.RaycastAll(puntoDisparoCanon.position, direccionActual, 50f);
            System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));
            
            Vector2 puntoImpacto = (Vector2)puntoDisparoCanon.position + (direccionActual * 50f);

            // EVALUACIÓN DE IMPACTOS DE ESTE FRAME
            foreach (RaycastHit2D impacto in impactos)
            {
                // Ignoramos al propio Jefe
                if (impacto.collider.transform.root == transform.root) continue;

                // Ignoramos Triggers invisibles, a menos que identifiquemos el tag del jugador[cite: 2]
                if (impacto.collider.isTrigger && !impacto.collider.CompareTag("Player")) continue; 

                // Intentamos acceder al comportamiento del Jugador[cite: 1, 2]
                Jugador scriptJugador = impacto.collider.GetComponentInParent<Jugador>();
                
                if (scriptJugador != null)
                {
                    if (!jugadorDañado)
                    {
                        scriptJugador.RecibirDano(danoLaser, puntoDisparoCanon.position);
                        jugadorDañado = true; 
                    }
                }
                else if (impacto.collider.CompareTag("Pared"))
                {
                    puntoImpacto = impacto.point;
                    break; 
                }
            }

            // Dibuja el láser hasta la pared o el infinito
            lineaLaser.SetPosition(1, puntoImpacto);

            tiempoRestante -= Time.deltaTime;
            yield return null; 
        }

        // 4. ANIMACIÓN: Fade Out suave del grosor
        float duracionFadeOut = 0.3f;
        float tiempoAnimacion = 0f;
        float multiplicadorGrosorInicial = lineaLaser.widthMultiplier;

        while (tiempoAnimacion < duracionFadeOut)
        {
            tiempoAnimacion += Time.deltaTime;
            lineaLaser.widthMultiplier = Mathf.Lerp(multiplicadorGrosorInicial, 0f, tiempoAnimacion / duracionFadeOut);
            yield return null; 
        }

        // 5. Restauración de componentes
        lineaLaser.enabled = false;
        lineaLaser.widthMultiplier = multiplicadorGrosorInicial; 
    }

    private IEnumerator AtaqueMetralla()
    {
        // 1. Anticipación
        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoMetralla, false));

        // 2. Ejecución
        if (balaMetrallaPrefab == null || puntoDisparoMetralla == null) yield break;

        float dirX = transform.localScale.x > 0 ? -1f : 1f;

        // Bucle matemático para distribución perfecta en la arena
        for (int i = 0; i < cantidadBalasMetralla; i++)
        {
            // Instanciamos el GameObject desde el Prefab[cite: 2]
            GameObject bala = Instantiate(balaMetrallaPrefab, puntoDisparoMetralla.position, Quaternion.identity);
            
            // Buscamos el componente de movimiento independiente
            MovimientoProyectil scriptMovimiento = bala.GetComponent<MovimientoProyectil>();
            Rigidbody2D rbBala = bala.GetComponent<Rigidbody2D>();
            
            if (scriptMovimiento != null && rbBala != null)
            {
                // A. Distribuimos el impacto a lo largo del ancho de la arena
                float fraccionDistancia = (float)(i + 1) / cantidadBalasMetralla;
                float distanciaObjetivo = anchoDeLaArena * fraccionDistancia;

                // B. Leemos la gravedad real del proyectil
                float gravedad = Mathf.Abs(Physics2D.gravity.y * rbBala.gravityScale);
                
                // C. Cinemática: Calculamos el tiempo de vuelo y la velocidad X requerida
                float tiempoDeVuelo = (2f * fuerzaSaltoMetralla) / gravedad;
                float velocidadXRequerida = distanciaObjetivo / tiempoDeVuelo;

                // D. Pasamos el vector calculado al script de la bala para que se mueva y rote sola
                Vector2 velocidadCalculada = new Vector2(velocidadXRequerida * dirX, fuerzaSaltoMetralla);
                scriptMovimiento.Impulsar(velocidadCalculada);
            }
        }
    }

    private IEnumerator AtaqueEmbestida()
    {
        // 1. Anticipación (Se le pasa 'true' para que retroceda tomando impulso)
        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoEmbestida, true));

        // 2. Ejecución
        float dirX = transform.localScale.x > 0 ? -1f : 1f;
        rb.velocity = new Vector2(dirX * velocidadEmbestida, rb.velocity.y); //[cite: 2]
        yield return new WaitForSeconds(0.5f);
        
        // 3. Recuperación (Freno tras la embestida)
        rb.velocity = new Vector2(0f, rb.velocity.y); 
    }

    private IEnumerator AtaqueMisilTeledirigido()
    {
        // 1. Anticipación
        yield return StartCoroutine(RutinaTelegrafiado(colorAvisoMisil, false));

        // 2. Ejecución
        if (misilPrefab == null || puntoDisparoMetralla == null) yield break;
        
        // Se crea el proyectil a través de la plantilla (Prefab)[cite: 2]
        Instantiate(misilPrefab, puntoDisparoMetralla.position, Quaternion.identity); 
    }
}
