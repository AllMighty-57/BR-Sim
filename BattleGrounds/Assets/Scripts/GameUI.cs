using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GameUI : MonoBehaviour
{
    public GameObject game;
    public GameObject tutorial;

    public Slider healthBar;
    public TextMeshProUGUI playerInfoText;
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI winText;
    public Image winBackground;

    public Image damageVignette;

    private bool flashingDamage;

    private PlayerController player;
    
    // instance
    public static GameUI instance;

    void Awake()
    {
        instance = this;
    }

    public void UiFlash()
    {
        if (damageVignette == null)
        {
            Debug.LogError("Damage Vignette Image is not assigned!");
            return;
        }

        if (flashingDamage)
            return;

        StartCoroutine(DamageFlashCoRoutine());
    }

    private IEnumerator DamageFlashCoRoutine()
    {
        flashingDamage = true;

        damageVignette.gameObject.SetActive(true);

        // Keep the vignette visible long enough to notice.
        yield return new WaitForSeconds(0.25f);

        damageVignette.gameObject.SetActive(false);
        flashingDamage = false;
    }

    public void closeTutorial()
    {
        if (tutorial != false)
        {
            tutorial.SetActive(false);
            game.SetActive(true);
        }
        else
            return;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) 
            closeTutorial();
    }

    public void Initialize(PlayerController localPlayer)
    {
        player = localPlayer;
        healthBar.maxValue = player.maxHp;
        healthBar.value = player.curHp;

        UpdatePlayerInfoText();
        UpdateAmmoText();
    }

    public void UpdateHealthBar()
    {
        healthBar.value = player.curHp;
    }

    public void UpdatePlayerInfoText()
    {
        playerInfoText.text = "<b>Alive:</b> " + GameManager.instance.alivePlayers + 
        "\n" + "<b>Kills:</b> " + player.kills;
    }

    public void UpdateAmmoText()
    {
        ammoText.text = player.weapon.curAmmo + " / " + player.weapon.reserveAmmo;
    }

    public void SetWinText(string winnerName)
    {
        winBackground.gameObject.SetActive(true);
        winText.text = winnerName + " wins";
    }
}
