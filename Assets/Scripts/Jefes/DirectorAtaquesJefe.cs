using System.Collections.Generic;
using UnityEngine;

public enum TipoAtaqueTanque
{
    Laser,
    Metralla,
    Embestida,
    Misil
}

/// <summary>
/// Selecciona ataques según la situación y recuerda los ataques recientes.
/// Mantiene la toma de decisiones separada de la ejecución de cada ataque.
/// </summary>
[DisallowMultipleComponent]
public class DirectorAtaquesJefe : MonoBehaviour
{
    [Header("--- Pesos Base ---")]
    [Min(0f)] public float pesoLaser = 1f;
    [Min(0f)] public float pesoMetralla = 1f;
    [Min(0f)] public float pesoEmbestida = 1f;
    [Min(0f)] public float pesoMisil = 1f;

    [Header("--- Reglas de Selección ---")]
    [Tooltip("Multiplicador aplicado al ataque utilizado inmediatamente antes.")]
    [Range(0f, 1f)] public float penalizacionRepeticion = 0.08f;
    [Min(0f)] public float distanciaCorta = 3.5f;
    [Min(0f)] public float distanciaLarga = 7f;
    [Min(0f)] public float alturaConsiderable = 1.5f;
    [Min(0f)] public float distanciaSeguraAlBorde = 3f;

    private TipoAtaqueTanque? ultimoAtaque;
    private TipoAtaqueTanque? penultimoAtaque;

    public void Reiniciar()
    {
        ultimoAtaque = null;
        penultimoAtaque = null;
    }

    public TipoAtaqueTanque ElegirAtaque(Transform jefe, Transform jugador, bool fase2,
        bool puedeLanzarMisil, LimitesArenaJefe limites)
    {
        float distanciaX = Mathf.Abs(jugador.position.x - jefe.position.x);
        float diferenciaY = Mathf.Abs(jugador.position.y - jefe.position.y);
        float direccionJugador = Mathf.Sign(jugador.position.x - jefe.position.x);
        if (Mathf.Approximately(direccionJugador, 0f)) direccionJugador = 1f;

        var opciones = new List<OpcionAtaque>
        {
            new OpcionAtaque(TipoAtaqueTanque.Laser, pesoLaser),
            new OpcionAtaque(TipoAtaqueTanque.Metralla, pesoMetralla),
            new OpcionAtaque(TipoAtaqueTanque.Embestida, pesoEmbestida)
        };

        if (fase2 && puedeLanzarMisil)
        {
            opciones.Add(new OpcionAtaque(TipoAtaqueTanque.Misil, pesoMisil));
        }

        for (int i = 0; i < opciones.Count; i++)
        {
            OpcionAtaque opcion = opciones[i];

            if (opcion.tipo == TipoAtaqueTanque.Laser)
            {
                if (distanciaX >= distanciaLarga) opcion.peso *= 1.7f;
                if (diferenciaY >= alturaConsiderable) opcion.peso *= 1.8f;
            }
            else if (opcion.tipo == TipoAtaqueTanque.Metralla)
            {
                if (distanciaX > distanciaCorta && distanciaX < distanciaLarga)
                    opcion.peso *= 1.6f;
                if (diferenciaY >= alturaConsiderable) opcion.peso *= 1.25f;
            }
            else if (opcion.tipo == TipoAtaqueTanque.Embestida)
            {
                if (distanciaX <= distanciaCorta) opcion.peso *= 0.15f;
                else if (distanciaX >= distanciaLarga) opcion.peso *= 2f;

                if (limites != null &&
                    limites.EstaCercaDelLimite(jefe.position.x, direccionJugador, distanciaSeguraAlBorde))
                {
                    opcion.peso *= 0.1f;
                }
            }
            else if (opcion.tipo == TipoAtaqueTanque.Misil)
            {
                if (distanciaX >= distanciaCorta) opcion.peso *= 1.5f;
            }

            if (ultimoAtaque.HasValue && opcion.tipo == ultimoAtaque.Value)
                opcion.peso *= penalizacionRepeticion;

            if (penultimoAtaque.HasValue && ultimoAtaque.HasValue &&
                opcion.tipo == penultimoAtaque.Value && opcion.tipo == ultimoAtaque.Value)
            {
                opcion.peso = 0f;
            }

            opciones[i] = opcion;
        }

        float pesoTotal = 0f;
        foreach (OpcionAtaque opcion in opciones) pesoTotal += Mathf.Max(0f, opcion.peso);

        TipoAtaqueTanque elegido = TipoAtaqueTanque.Laser;
        if (pesoTotal > 0f)
        {
            float valor = Random.Range(0f, pesoTotal);
            foreach (OpcionAtaque opcion in opciones)
            {
                valor -= Mathf.Max(0f, opcion.peso);
                if (valor <= 0f)
                {
                    elegido = opcion.tipo;
                    break;
                }
            }
        }

        penultimoAtaque = ultimoAtaque;
        ultimoAtaque = elegido;
        return elegido;
    }

    private struct OpcionAtaque
    {
        public TipoAtaqueTanque tipo;
        public float peso;

        public OpcionAtaque(TipoAtaqueTanque tipo, float peso)
        {
            this.tipo = tipo;
            this.peso = peso;
        }
    }
}
