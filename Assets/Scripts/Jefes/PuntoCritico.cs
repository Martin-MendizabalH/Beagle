using UnityEngine;

/// <summary>
/// Actúa como un multiplicador de daño para el Hitbox de la torreta.
/// Sigue el principio de Responsabilidad Única.
/// </summary>
public class PuntoCritico : MonoBehaviour
{
    [Header("--- Configuración de Daño ---")]
    [Tooltip("Por cuánto se multiplicará el daño base de la bala al impactar aquí.")]
    public int multiplicador = 3;

    // Referencia interna al script padre
    private SaludJefe saludDelJefe;

    void Start()
    {
        // Buscamos automáticamente el componente SaludJefe en el objeto Padre[cite: 3]
        // Esto hace que el script sea "Drag & Drop" sin tener que configurar referencias manuales.
        saludDelJefe = GetComponentInParent<SaludJefe>();

        if (saludDelJefe == null)
        {
            Debug.LogError("El Punto Crítico no encontró un script SaludJefe en su objeto padre.");
        }
    }

    /// <summary>
    /// Método llamado exclusivamente por el script 'Proyectil' al detectar una colisión.
    /// </summary>
    public void ImpactoCritico(int danoBaseDeBala)
    {
        if (saludDelJefe != null)
        {
            int danoTotal = danoBaseDeBala * multiplicador;
            Debug.Log($"[CRÍTICO] ¡Impacto en la torreta! {danoBaseDeBala} x {multiplicador} = {danoTotal} de daño.");
            
            // Le pasamos el daño ya multiplicado al cerebro del jefe
            saludDelJefe.RecibirDano(danoTotal);
        }
    }
}