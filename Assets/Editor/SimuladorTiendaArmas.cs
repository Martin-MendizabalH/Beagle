using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Comprueba catálogo, adquisición, saldo, compra duplicada y enlaces de escena.
/// </summary>
public static class SimuladorTiendaArmas
{
    private const string RutaNivel1 = "Assets/Escenas/Niveles/Nivel 1.unity";

    [MenuItem("Herramientas/Beagle/Simular tienda de armas")]
    public static void Ejecutar()
    {
        Scene escenaAnterior = SceneManager.GetActiveScene();
        string rutaAnterior = escenaAnterior.path;

        if (escenaAnterior.isDirty &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            throw new InvalidOperationException("Se canceló la simulación para no perder cambios.");
        }

        ProbarLogicaDeCompra();
        ProbarConfiguracionDeEscena();

        if (!string.IsNullOrEmpty(rutaAnterior) && rutaAnterior != RutaNivel1)
        {
            EditorSceneManager.OpenScene(rutaAnterior, OpenSceneMode.Single);
        }

        Debug.Log("[SIMULACIÓN TIENDA] OK: precios, compra, saldo, duplicados, inventario y tarjetas verificados.");
    }

    public static void EjecutarEnLote()
    {
        ProbarLogicaDeCompra();
        ProbarConfiguracionDeEscena();
        Debug.Log("[SIMULACIÓN TIENDA] OK: todos los casos fueron superados.");
    }

    private static void ProbarLogicaDeCompra()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ProgresoArmasSesion.Reiniciar();

        DatosArma pistola = CargarArma("Arma_Pistola");
        DatosArma katana = CargarArma("Arma_Katana");
        DatosArma metralleta = CargarArma("Arma_Metralleta");
        DatosArma escopeta = CargarArma("Arma_Escopeta");

        GameObject monedero = new GameObject("Prueba_Tienda");
        Tienda tienda = monedero.AddComponent<Tienda>();
        Tienda.Instancia = tienda;
        tienda.dineroJugador = 100;

        GameObject jugador = new GameObject("Prueba_Jugador");
        InventarioArmas inventario = jugador.AddComponent<InventarioArmas>();
        inventario.armasDisponibles = new[] { pistola, katana, metralleta, escopeta };
        InvocarPrivado(inventario, "InicializarInventario");

        Exigir(inventario.ArmasAdquiridas.Count == 1, "El jugador no comenzó únicamente con la Pistola.");
        Exigir(inventario.PoseeArma(pistola), "La Pistola inicial no fue registrada.");
        Exigir(!inventario.PoseeArma(katana), "La Katana apareció adquirida antes de comprarla.");

        ItemTiendaUI tarjetaKatana = CrearTarjeta("Prueba_Katana", katana);
        InvocarPrivado(tarjetaKatana, "ComprarArma");
        Exigir(tienda.DineroJugador == 50, "La Katana no descontó exactamente su precio.");
        Exigir(inventario.PoseeArma(katana), "La Katana pagada no llegó al inventario.");
        Exigir(inventario.ArmaEquipada == katana, "La Katana comprada no se equipó.");

        InvocarPrivado(tarjetaKatana, "ComprarArma");
        Exigir(tienda.DineroJugador == 50, "Una compra repetida volvió a descontar monedas.");
        Exigir(inventario.ArmasAdquiridas.Count == 2, "Una compra repetida duplicó el arma.");

        ItemTiendaUI tarjetaMetralleta = CrearTarjeta("Prueba_Metralleta", metralleta);
        InvocarPrivado(tarjetaMetralleta, "ComprarArma");
        Exigir(tienda.DineroJugador == 0, "La Metralleta no descontó exactamente su precio.");
        Exigir(inventario.PoseeArma(metralleta), "La Metralleta pagada no llegó al inventario.");

        UnityEngine.Object.DestroyImmediate(jugador);
        GameObject jugadorNuevaEscena = new GameObject("Prueba_Jugador_NuevaEscena");
        inventario = jugadorNuevaEscena.AddComponent<InventarioArmas>();
        inventario.armasDisponibles = new[] { pistola, katana, metralleta, escopeta };
        InvocarPrivado(inventario, "InicializarInventario");
        Exigir(inventario.PoseeArma(katana) && inventario.PoseeArma(metralleta),
            "Las armas compradas no sobrevivieron al cambio de jugador/escena.");

        ItemTiendaUI tarjetaEscopeta = CrearTarjeta("Prueba_Escopeta", escopeta);
        InvocarPrivado(tarjetaEscopeta, "ComprarArma");
        Exigir(tienda.DineroJugador == 0, "Una compra sin fondos alteró el saldo.");
        Exigir(!inventario.PoseeArma(escopeta), "La Escopeta se entregó sin dinero suficiente.");

        inventario.CambiarArmaSiguiente();
        Exigir(inventario.ArmaEquipada != escopeta, "Tab permitió equipar un arma no adquirida.");

        UnityEngine.Object.DestroyImmediate(monedero);
        UnityEngine.Object.DestroyImmediate(jugadorNuevaEscena);
        Tienda.Instancia = null;
        ProgresoArmasSesion.Reiniciar();
    }

    private static void ProbarConfiguracionDeEscena()
    {
        Scene nivel1 = EditorSceneManager.OpenScene(RutaNivel1, OpenSceneMode.Single);
        VerificarTarjeta(nivel1, "ItemKatana", "Arma_Katana");
        VerificarTarjeta(nivel1, "ItemMetralleta", "Arma_Metralleta");
        VerificarTarjeta(nivel1, "ItemEscopeta", "Arma_Escopeta");
    }

    private static void VerificarTarjeta(Scene escena, string nombreTarjeta, string nombreActivo)
    {
        GameObject tarjeta = BuscarPorNombre(escena, nombreTarjeta);
        Exigir(tarjeta != null, $"No se encontró {nombreTarjeta}.");

        ItemTiendaUI item = tarjeta.GetComponent<ItemTiendaUI>();
        Exigir(item != null, $"{nombreTarjeta} no tiene ItemTiendaUI.");
        Exigir(item.arma != null && item.arma.name == nombreActivo,
            $"{nombreTarjeta} no está enlazada con {nombreActivo}.");
        Exigir(item.objeto == null, $"{nombreTarjeta} conserva un objeto genérico además del arma.");
        Exigir(item.textoNombre != null, $"{nombreTarjeta} no tiene texto de nombre.");
        Exigir(item.textoPrecio != null, $"{nombreTarjeta} no tiene texto de precio.");
        Exigir(item.imagenSprite != null, $"{nombreTarjeta} no tiene imagen.");
        Exigir(item.botonComprar != null, $"{nombreTarjeta} no tiene botón.");
        Exigir(item.textoPrecio.text == item.arma.precio.ToString(),
            $"{nombreTarjeta} muestra un precio distinto al de DatosArma.");
    }

    private static ItemTiendaUI CrearTarjeta(string nombre, DatosArma arma)
    {
        GameObject objeto = new GameObject(nombre);
        ItemTiendaUI tarjeta = objeto.AddComponent<ItemTiendaUI>();
        tarjeta.arma = arma;
        return tarjeta;
    }

    private static DatosArma CargarArma(string nombre)
    {
        DatosArma arma = AssetDatabase.LoadAssetAtPath<DatosArma>($"Assets/Datos/Armas/{nombre}.asset");
        Exigir(arma != null, $"No se pudo cargar {nombre}.");
        return arma;
    }

    private static void InvocarPrivado(object objetivo, string metodo)
    {
        MethodInfo informacion = objetivo.GetType().GetMethod(
            metodo,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Exigir(informacion != null, $"No se encontró el método {metodo}.");
        informacion.Invoke(objetivo, null);
    }

    private static GameObject BuscarPorNombre(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform transformacion in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (transformacion.name == nombre) return transformacion.gameObject;
            }
        }

        return null;
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException("[SIMULACIÓN TIENDA] " + mensaje);
    }
}
