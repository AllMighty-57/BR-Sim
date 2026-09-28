using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviourPun
{
    [Header("Stats")]
    public float moveSpeed;
    public float jumpForce;

    [Header("Components")]
    public Rigidbody rig;

    private int curAttackerId;
    
    public int curHp;
    public int maxHp;
    public int kills;
    public bool dead;
    private bool flashingDamage;
    public MeshRenderer mr;

    public PlayerWeapon weapon;

    public int id;
    public Player photonPlayer;


    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        GameManager.instance.players[id - 1] = this;
        // is this not our local player?
        if (!photonView.IsMine)
        {
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            rig.isKinematic = true;
        }
    }

    void Move()
    {
        // get the input axis
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.5f);

        Vector3 inputDirection =
        (transform.forward * z + transform.right * x).normalized;

        if (isGrounded)
        {
            float currentSpeed = moveSpeed;

            // Sprint only while grounded
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= 2f;
            }

            Vector3 velocity = inputDirection * currentSpeed;

            // Keep vertical velocity
            velocity.y = rig.linearVelocity.y;

            rig.linearVelocity = velocity;
        }
        else
        {
            // AIR MOVEMENT
            // Don't replace horizontal velocity with sprint speed.
            // Instead, apply a small amount of air control.
            float airControl = 0.5f;

            Vector3 airVelocity = rig.linearVelocity;

            Vector3 desiredVelocity = inputDirection * moveSpeed;

            airVelocity.x = Mathf.Lerp(airVelocity.x, desiredVelocity.x, airControl * Time.deltaTime);

            airVelocity.z = Mathf.Lerp(airVelocity.z, desiredVelocity.z, airControl * Time.deltaTime);

            rig.linearVelocity = airVelocity;
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

    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead)
            return;

        curHp -= damage;
        curAttackerId = attackerId;

        // flash the player red
        photonView.RPC("DamageFlash", RpcTarget.Others);

        // update the health bar UI

        // die if no health left
        if (curHp <= 0)
            photonView.RPC("Die", RpcTarget.All);
    }

    [PunRPC]
    void DamageFlash()
    {
        if (flashingDamage)
            return;
        StartCoroutine(DamageFlashCoRoutine());
        IEnumerator DamageFlashCoRoutine()
        {
            flashingDamage = true;
            Color defaultColor = mr.material.color;
            mr.material.color = Color.red;
            yield return new WaitForSeconds(0.05f);
            mr.material.color = defaultColor;
            flashingDamage = false;
        }
    }

    [PunRPC]
    void Die()
    {
    }

    void Update()
    {
         if (!photonView.IsMine || dead)
            return;
        
        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
        
        if (Input.GetMouseButtonDown(0))
            weapon.TryShoot();
    }
    void FixedUpdate()
    {
        Move();
    }
}
