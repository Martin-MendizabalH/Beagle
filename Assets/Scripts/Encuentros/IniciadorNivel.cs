using UnityEngine;

public class IniciadorNivel : MonoBehaviour
{
    public GameObject robotPerseguidor; 

    private void OnTriggerEnter2D(Collider2D colision)
    {
        if (colision.gameObject.CompareTag("Player")) 
        {
            if (robotPerseguidor != null)
            {
                robotPerseguidor.SetActive(true); 
            }
            
            Destroy(gameObject); 
        }
    }
}