using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviourPun
{
    [Header("Info")]
    public int id;
    private int curAttackerId;

    [Header("Stats")]
    public float moveSpeed;
    public float jumpForce;
    public int curHp;
    public int maxHp;
    public int kills;
    public bool dead; 

    private bool flashingDamage;

    [Header("Components")]
    public Rigidbody rig;
    public Player photonPlayer;
    public PlayerWeapon weapon;
    public MeshRenderer mr;
    public GameObject gunModel;


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
        curHp = 0;
        dead = true;
        GameManager.instance.alivePlayers--;

        // host will check win condition
        if (PhotonNetwork.IsMasterClient)
            GameManager.instance.CheckWinCondition();

        // is this our local player?
        if (photonView.IsMine)
        {
            if (curAttackerId != 0)
                GameManager.instance.GetPlayer(curAttackerId).photonView.RPC("AddKill", RpcTarget.All);
            
            // set the cam to spectator
            GetComponentInChildren<CameraController>().SetAsSpectator();
            
            // disable the physics and hide the player
            rig.isKinematic = true; 
            gunModel.SetActive(false);
            transform.position = new Vector3(0, -50, 0); 
            Debug.Log("Player " + id + " has died and is now a spectator.");
        }
    }

    [PunRPC]
    public void Heal(int amountToHeal)
    {
        curHp = Mathf.Clamp(curHp + amountToHeal, 0, maxHp);
        // update the health bar UI
    } 


    [PunRPC]
    public void AddKill()
    {
        kills++;
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
