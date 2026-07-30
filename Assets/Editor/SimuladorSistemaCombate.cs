using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Simulacion automatizada de los casos principales: dano, muerte, botin y recogida.
/// Se puede ejecutar desde Herramientas/Beagle o en batch con -executeMethod.
/// </summary>
public static class SimuladorSistemaCombate
{
    [MenuItem("Herramientas/Beagle/Simular dano, muerte y monedas")]
    public static void EjecutarSimulacion()
    {
        GeneradorSistemaBotin.CrearYConfigurarSistema();
        GameObject prefabMoneda = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Coleccionables/Moneda.prefab");
        Verificar(prefabMoneda != null, "No se pudo cargar el prefab de moneda.");
        Verificar(prefabMoneda.GetComponent<Moneda>() != null, "El prefab de moneda no tiene Moneda.");
        Verificar(TieneCollider(prefabMoneda, true), "La moneda no tiene un trigger de recogida.");
        Verificar(TieneCollider(prefabMoneda, false), "La moneda no tiene un collider fisico para caer al suelo.");
        Rigidbody2D cuerpoFisicoMoneda = prefabMoneda.GetComponent<Rigidbody2D>();
        Verificar(cuerpoFisicoMoneda != null, "La moneda no tiene Rigidbody2D.");
        Verificar(cuerpoFisicoMoneda.gravityScale > 0f, "La moneda no tiene gravedad configurada.");

        List<UnityEngine.Object> objetosTemporales = new List<UnityEngine.Object>();

        try
        {
            GameObject monedero = new GameObject("Simulacion_Monedero");
            objetosTemporales.Add(monedero);
            Tienda tienda = monedero.AddComponent<Tienda>();
            tienda.dineroJugador = 100;
            Tienda.Instancia = tienda;

            GameObject jugador = new GameObject("Simulacion_Jugador");
            objetosTemporales.Add(jugador);
            jugador.tag = "Player";
            CircleCollider2D colliderJugador = jugador.AddComponent<CircleCollider2D>();

            SimularEnemigoNormal(prefabMoneda, tienda, colliderJugador, objetosTemporales);
            SimularSoldado(prefabMoneda, tienda, colliderJugador, objetosTemporales);
            SimularJefe(prefabMoneda, tienda, colliderJugador, objetosTemporales);

            Debug.Log("[SIMULACION] OK: dano visual, muertes unicas, botin de 2-3 monedas y recogida de 5 por moneda verificados.");
        }
        finally
        {
            foreach (Moneda moneda in ObtenerMonedasEscena())
            {
                if (moneda != null) UnityEngine.Object.DestroyImmediate(moneda.gameObject);
            }

            foreach (UnityEngine.Object objeto in objetosTemporales)
            {
                if (objeto != null) UnityEngine.Object.DestroyImmediate(objeto);
            }

            Tienda.Instancia = null;
        }
    }

    private static void SimularEnemigoNormal(GameObject prefabMoneda, Tienda tienda,
        Collider2D colliderJugador, List<UnityEngine.Object> objetosTemporales)
    {
        GameObject enemigo = CrearEntidad("Simulacion_EnemigoNormal", objetosTemporales);
        RetroalimentacionDanio feedback = enemigo.AddComponent<RetroalimentacionDanio>();
        BotinMonedas botin = enemigo.AddComponent<BotinMonedas>();
        botin.Configurar(prefabMoneda, 2, 2, 5);
        SaludEnemigo salud = enemigo.AddComponent<SaludEnemigo>();

        salud.RecibirDano(1f);
        Verificar(feedback.CantidadImpactos == 1, "El enemigo normal no mostro feedback al recibir dano.");
        salud.RecibirDano(999f);
        Verificar(botin.UltimaCantidadSoltada == 2, "El enemigo normal no solto exactamente 2 monedas configuradas.");
        Verificar(botin.SoltarMonedas() == 0, "El enemigo normal genero botin duplicado.");
        RecogerMonedasEsperadas(2, tienda, colliderJugador);
    }

    private static void SimularSoldado(GameObject prefabMoneda, Tienda tienda,
        Collider2D colliderJugador, List<UnityEngine.Object> objetosTemporales)
    {
        GameObject soldadoObjeto = CrearEntidad("Simulacion_Soldado", objetosTemporales);
        soldadoObjeto.AddComponent<Rigidbody2D>();
        RetroalimentacionDanio feedback = soldadoObjeto.AddComponent<RetroalimentacionDanio>();
        BotinMonedas botin = soldadoObjeto.AddComponent<BotinMonedas>();
        botin.Configurar(prefabMoneda, 2, 3, 5);
        SoldadoEnemigo soldado = soldadoObjeto.AddComponent<SoldadoEnemigo>();
        soldado.vida = 10f;

        soldado.RecibirDano(5f);
        Verificar(feedback.CantidadImpactos == 1, "El soldado no mostro feedback al recibir dano.");
        soldado.RecibirDano(5f);
        Verificar(botin.UltimaCantidadSoltada >= 2 && botin.UltimaCantidadSoltada <= 3,
            "El soldado no respeto el rango de botin 2-3.");
        RecogerMonedasEsperadas(botin.UltimaCantidadSoltada, tienda, colliderJugador);
    }

    private static void SimularJefe(GameObject prefabMoneda, Tienda tienda,
        Collider2D colliderJugador, List<UnityEngine.Object> objetosTemporales)
    {
        GameObject jefe = CrearEntidad("Simulacion_Jefe", objetosTemporales);
        RetroalimentacionDanio feedback = jefe.AddComponent<RetroalimentacionDanio>();
        BotinMonedas botin = jefe.AddComponent<BotinMonedas>();
        botin.Configurar(prefabMoneda, 3, 3, 5);
        SaludJefe saludJefe = jefe.AddComponent<SaludJefe>();

        saludJefe.RecibirDano(10);
        Verificar(feedback.CantidadImpactos == 0,
            "El jefe recibio dano antes de que comenzara la batalla.");
        Verificar(botin.UltimaCantidadSoltada == 0,
            "El jefe solto botin antes de ser vulnerable.");

        saludJefe.esVulnerable = true;

        saludJefe.RecibirDano(80);
        Verificar(feedback.CantidadImpactos == 1, "El jefe no mostro feedback al recibir dano.");
        saludJefe.RecibirDano(999);
        Verificar(botin.UltimaCantidadSoltada == 3, "El jefe no solto exactamente 3 monedas configuradas.");
        Verificar(botin.SoltarMonedas() == 0, "El jefe genero botin duplicado.");
        RecogerMonedasEsperadas(3, tienda, colliderJugador);
    }

    private static GameObject CrearEntidad(string nombre, List<UnityEngine.Object> objetosTemporales)
    {
        GameObject entidad = new GameObject(nombre);
        entidad.AddComponent<SpriteRenderer>();
        objetosTemporales.Add(entidad);
        return entidad;
    }

    private static void RecogerMonedasEsperadas(int cantidadEsperada, Tienda tienda, Collider2D colliderJugador)
    {
        List<Moneda> monedas = ObtenerMonedasEscena();
        Verificar(monedas.Count == cantidadEsperada,
            $"Se esperaban {cantidadEsperada} monedas, pero se encontraron {monedas.Count}.");

        int dineroInicial = tienda.dineroJugador;
        foreach (Moneda moneda in monedas)
        {
            moneda.HabilitarRecogidaInmediata();
            Verificar(moneda.IntentarRecoger(colliderJugador), "La moneda no se pudo recoger.");
        }

        Verificar(tienda.dineroJugador == dineroInicial + cantidadEsperada * 5,
            "La recogida de monedas no sumo 5 por cada moneda al contador.");

    }

    private static List<Moneda> ObtenerMonedasEscena()
    {
        List<Moneda> resultado = new List<Moneda>();
        foreach (Moneda moneda in UnityEngine.Object.FindObjectsOfType<Moneda>(true))
        {
            if (moneda.gameObject.scene.IsValid()) resultado.Add(moneda);
        }
        return resultado;
    }

    private static bool TieneCollider(GameObject objeto, bool esTrigger)
    {
        foreach (CircleCollider2D collider in objeto.GetComponents<CircleCollider2D>())
        {
            if (collider.isTrigger == esTrigger) return true;
        }

        return false;
    }

    private static void Verificar(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception("[SIMULACION] " + mensaje);
    }
}
