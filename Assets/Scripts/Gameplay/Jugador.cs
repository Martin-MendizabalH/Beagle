using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jugador : MonoBehaviour
{

    public float velocidad = 8f;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        JugadorMovement();
    }

    void JugadorMovement()
    {
        float movimientoX = Input.GetAxis("Horizontal");
        // float movimientoY = Input.GetAxis("Vertical");

        if (movimientoX > 0)
        {
            animator.SetBool("isWalking", true);
            spriteRenderer.flipX = false;
        }
        else if (movimientoX < 0)
        {
            animator.SetBool("isWalking", true);
            spriteRenderer.flipX = true;
        }

        else{
            animator.SetBool("isWalking", false);
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        rb.velocity = new Vector2(movimientoX * velocidad, rb.velocity.y);
    }
}
