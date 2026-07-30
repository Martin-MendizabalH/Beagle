using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Conserva las armas compradas mientras la partida está abierta, incluso al
/// cambiar de escena. No escribe en disco; un futuro sistema de guardado puede
/// reemplazar esta implementación sin cambiar la tienda ni el inventario.
/// </summary>
public static class ProgresoArmasSesion
{
    private static readonly HashSet<DatosArma> armasAdquiridas = new HashSet<DatosArma>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarAlComenzarPartida()
    {
        armasAdquiridas.Clear();
    }

    public static bool Posee(DatosArma arma)
    {
        return arma != null && armasAdquiridas.Contains(arma);
    }

    public static bool Registrar(DatosArma arma)
    {
        return arma != null && armasAdquiridas.Add(arma);
    }

    public static void Reiniciar()
    {
        armasAdquiridas.Clear();
    }
}
