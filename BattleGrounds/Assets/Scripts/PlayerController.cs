using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun
{
    [Header("Info")]
    public int id;
    private int curAttackerId;

    [Header("Movement")]
    public float moveSpeed;
    public float sprintMultiplier = 2f;

    [Header("Air Control")]
    public float airAcceleration = 8f;
    public float maxAirSpeed = 18f;

    [Header("Stats")]
    public float jumpForce;
    public int curHp;
    public float curStamina;
    public int maxHp;
    public int maxStamina;
    public int kills;
    public bool dead;

    [Header("Stamina")]
    public float staminaDrainRate;
    public float staminaRegenRate; 
    public float staminaRegenDelay; 

    private float staminaRegenTimer; 
    private bool isSprinting;

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
        else
        {
            GameUI.instance.Initialize(this);
        }
    }

    void Update()
    {
        // if this isn't our local player or we're dead - return
        if (!photonView.IsMine || dead)
            return;

        UpdateStamina();


        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();
        
        if (Input.GetMouseButtonDown(0))
            weapon.TryShoot(); 

        if (Input.GetKeyDown(KeyCode.R))
            weapon.Reload();
    }


    void UpdateStamina()
    {
        bool isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            1.5f
        );

        bool hasMovementInput =
            Input.GetAxisRaw("Horizontal") != 0 ||
            Input.GetAxisRaw("Vertical") != 0;

        bool wantsToSprint =
            isGrounded &&
            hasMovementInput &&
            Input.GetKey(KeyCode.LeftShift) &&
            curStamina > 0f;

        if (wantsToSprint)
        {
            isSprinting = true;

            curStamina -= staminaDrainRate * Time.deltaTime;
            curStamina = Mathf.Max(curStamina, 0f);

            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            isSprinting = false;

            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                curStamina += staminaRegenRate * Time.deltaTime;
                curStamina = Mathf.Min(curStamina, maxStamina);
            }
        }

        GameUI.instance.UpdateSprintBar();
    }


    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            1.5f
        );

        Vector3 inputDirection =
            (transform.forward * z + transform.right * x).normalized;

        if (isGrounded)
        {
            // -------------------------
            // GROUND MOVEMENT
            // -------------------------

            float currentSpeed = moveSpeed;

            if (isSprinting)
            {
                currentSpeed *= sprintMultiplier;
            }

            Vector3 velocity = inputDirection * currentSpeed;

            // Preserve vertical velocity
            velocity.y = rig.linearVelocity.y;

            rig.linearVelocity = velocity;
        }
        else
        {
            // -------------------------
            // AIR MOVEMENT
            // -------------------------

            Vector3 horizontalVelocity = new Vector3(
            rig.linearVelocity.x,
            0f,
            rig.linearVelocity.z
            );

            // Only air strafe when there is input
            if (x != 0f || z != 0f)
            {
                horizontalVelocity +=
                    inputDirection *
                    airAcceleration *
                    Time.fixedDeltaTime;

                // Prevent air acceleration from exceeding max speed
                if (horizontalVelocity.magnitude > maxAirSpeed)
                {
                    horizontalVelocity =
                        horizontalVelocity.normalized * maxAirSpeed;
                }
            }

            rig.linearVelocity = new Vector3(
                horizontalVelocity.x,
                rig.linearVelocity.y,
                horizontalVelocity.z
            );
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


    void UpdateHealthColor()
    {
        if (mr == null || maxHp <= 0)
            return;

        float healthPercent = (float)curHp / maxHp;

        // Color at 100% health: pale green
        Color healthyColor = new Color(0.75f, 0.85f, 0.68f);

        // Color at 50% health: medium roasted coffee
        Color mediumCoffeeColor = new Color(0.40f, 0.25f, 0.14f);

        // Color at 0% health: dark roasted coffee bean
        Color darkCoffeeColor = new Color(0.16f, 0.075f, 0.035f);

        Color targetColor;

        if (healthPercent >= 0.5f)
        {
            // Blend from pale green to medium coffee brown.
            float t = (1f - healthPercent) * 2f;

            targetColor = Color.Lerp(
                healthyColor,
                mediumCoffeeColor,
                t
            );
        }
        else
        {
            // Blend from medium coffee brown to dark coffee brown.
            float t = (0.5f - healthPercent) * 2f;

            targetColor = Color.Lerp(
                mediumCoffeeColor,
                darkCoffeeColor,
                t
            );
        } 
        mr.material.color = targetColor;
    }

    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead)
            return;

        curHp -= damage;
        curAttackerId = attackerId;

        // flash the player red
        DamageFlash();

        UpdateHealthColor();    

        // Update UI only for the player who owns this character.
        if (photonView.IsMine)
        {
            GameUI.instance.UpdateHealthBar();
            GameUI.instance.UiFlash();
        }

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

            mr.material.color = Color.red;

            yield return new WaitForSeconds(0.05f);

            // Restore the color appropriate for current health.
            UpdateHealthColor();

            flashingDamage = false;
        }
    }

    [PunRPC]
    void Die()
    { 
        if (dead)
            return;

        curHp = 0;
        dead = true;

        GameManager.instance.alivePlayers--;

        // Only the Master Client tracks the remaining player count.
        // The dead check above prevents counting this death twice.
        if (PhotonNetwork.IsMasterClient)
        {

            Debug.Log("Players alive: " +
                GameManager.instance.alivePlayers);

            GameManager.instance.CheckWinCondition();
        }

        // is this our local player?
        if (photonView.IsMine)
        {
            if (curAttackerId != 0)
            {
                PlayerController attacker =
                    GameManager.instance.GetPlayer(curAttackerId);

                if (attacker != null)
                {
                    attacker.photonView.RPC("AddKill", RpcTarget.All); 
                }
            }

            GetComponentInChildren<CameraController>().SetAsSpectator();

            rig.isKinematic = true;
            gunModel.SetActive(false);
            transform.position = new Vector3(0, -50, 0);
        }
    }

    [PunRPC]
    public void Heal(int amountToHeal)
    {
        curHp = Mathf.Clamp(curHp + amountToHeal, 0, maxHp);

        UpdateHealthColor();

        // update the health bar UI 
        if(photonView.IsMine) 
            GameUI.instance.UpdateHealthBar();
    } 


    [PunRPC]
    public void AddKill()
    {
        kills++; 

        GameUI.instance.UpdatePlayerInfoText();
    }



}
