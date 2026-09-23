using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed;
    public float jumpForce;

    [Header("Components")]
    public Rigidbody rig;

    
    void Start()
    {
        
    }

    void Move()
    {
        // get the input axis
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 dir = (transform.forward * z + transform.right * x) * (moveSpeed * 2);
            dir.y = rig.linearVelocity.y;

            // set that as our velocity
            rig.linearVelocity = dir;
        }
        else
        {
            // calculate a direction relative to where we're facing
            Vector3 dir = (transform.forward * z + transform.right * x) * moveSpeed;
            dir.y = rig.linearVelocity.y;

            // set that as our velocity
            rig.linearVelocity = dir;
        }
    }

    void TryJump()
    {
        // create a ray facing down
        Ray ray = new Ray(transform.position, Vector3.down);

        // shoot the raycast
        if (Physics.Raycast(ray, 1.5f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void Update()
    {
        Move(); 

        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
    }
}
