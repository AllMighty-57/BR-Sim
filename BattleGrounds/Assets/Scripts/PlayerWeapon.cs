using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Stats")]
    public int damage;
    public int curAmmo;
    public int magSize;
    public int reserveAmmo;
    public int reserveSize;
    public float bulletSpeed;
    public float shootRate;

    private float lastShootTime;

    public GameObject bulletPrefab;
    public Transform bulletSpawnPos;

    private PlayerController player;

    void Awake()
    {
        // get required components
        player = GetComponent<PlayerController>();
    }

    public void Reload()
    {
        int ammoToGive = magSize - curAmmo;
        if (curAmmo == magSize)
            return;
        else if (curAmmo < magSize)
        { 
            if(reserveAmmo < ammoToGive)
                ammoToGive = reserveAmmo;
            curAmmo += ammoToGive;
            reserveAmmo -= ammoToGive;
           
            // update the ammo UI 
            GameUI.instance.UpdateAmmoText();
        }
        else
        {
            curAmmo = magSize;
            reserveAmmo -= magSize;
            
            // update the ammo UI 
            GameUI.instance.UpdateAmmoText();
        }
    }

    public void TryShoot()
    {
        // can we shoot?
        if (curAmmo <= 0 || Time.time - lastShootTime < shootRate)
            return;

        curAmmo--;
        lastShootTime = Time.time;
        
        // update the ammo UI 
        GameUI.instance.UpdateAmmoText();
        
        // spawn the bullet
        player.photonView.RPC("SpawnBullet", RpcTarget.All, bulletSpawnPos.transform.position, 
        Camera.main.transform.forward);
    }

    [PunRPC]
    void SpawnBullet(Vector3 pos, Vector3 dir)
    {
        // spawn and orientate it
        GameObject bulletObj = Instantiate(bulletPrefab, pos, Quaternion.identity);
        bulletObj.transform.forward = dir;

        // get bullet script
        Bullet bulletScript = bulletObj.GetComponent<Bullet>();

        // initialize it and set the velocity
        bulletScript.Initialize(damage, player.id, player.photonView.IsMine);
        bulletScript.rig.linearVelocity = dir * bulletSpeed;
    }

    [PunRPC]
    public void GiveAmmo(int ammoToGive)
    {
        reserveAmmo += ammoToGive;
        if (reserveAmmo > reserveSize)
            reserveAmmo = reserveSize;

        // update the ammo text 
        GameUI.instance.UpdateAmmoText();
    }

}
