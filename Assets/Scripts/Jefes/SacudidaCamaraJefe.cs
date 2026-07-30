using Cinemachine;
using UnityEngine;

/// <summary>
/// Encapsula los impulsos de Cinemachine utilizados por la pelea del Jefe.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class SacudidaCamaraJefe : MonoBehaviour
{
    private CinemachineImpulseSource fuenteImpulso;

    private void Awake()
    {
        fuenteImpulso = GetComponent<CinemachineImpulseSource>();
        ConfigurarFuente();
    }

    public void Sacudir(float intensidad, float duracion)
    {
        if (fuenteImpulso == null) fuenteImpulso = GetComponent<CinemachineImpulseSource>();
        ConfigurarFuente();

        fuenteImpulso.m_ImpulseDefinition.m_ImpulseDuration = Mathf.Max(0.05f, duracion);
        Vector2 direccion = Random.insideUnitCircle.normalized;
        if (direccion.sqrMagnitude < 0.01f) direccion = Vector2.up;
        fuenteImpulso.GenerateImpulseWithVelocity(direccion * Mathf.Max(0f, intensidad));
    }

    public static void PrepararReceptor(GameObject camaraVirtual)
    {
        if (camaraVirtual == null) return;

        CinemachineImpulseListener receptor =
            camaraVirtual.GetComponent<CinemachineImpulseListener>();
        if (receptor == null) receptor = camaraVirtual.AddComponent<CinemachineImpulseListener>();

        receptor.m_ChannelMask = 1;
        receptor.m_Gain = 1f;
        receptor.m_Use2DDistance = true;
        receptor.m_UseCameraSpace = true;
    }

    private void ConfigurarFuente()
    {
        if (fuenteImpulso == null || fuenteImpulso.m_ImpulseDefinition == null) return;

        CinemachineImpulseDefinition definicion = fuenteImpulso.m_ImpulseDefinition;
        definicion.m_ImpulseChannel = 1;
        definicion.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        definicion.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        definicion.m_DissipationDistance = 100f;
        definicion.m_DissipationRate = 0.25f;
        definicion.m_PropagationSpeed = 343f;
    }
}
