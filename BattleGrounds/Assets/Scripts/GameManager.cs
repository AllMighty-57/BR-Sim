using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPun
{
    [Header("Players")]
    public string playerPrefabLocation;
    public PlayerController[] players;
    public Transform[] spawnPoints;
    public int alivePlayers;
    private int playersInGame;

    public float postGameTime;

    private bool gameEnded = false;


    // instance
    public static GameManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        alivePlayers = players.Length;

        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    } 

    [PunRPC]
    void ImInGame()
    {
        playersInGame++;

        if (PhotonNetwork.IsMasterClient && playersInGame == PhotonNetwork.PlayerList.Length)
            photonView.RPC("SpawnPlayer", RpcTarget.All);
    }

    [PunRPC]
    void SpawnPlayer()
    {
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, 
        spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity);
        
        // initialize the player for all other players 
        playerObj.GetComponent<PlayerController>().photonView.RPC("Initialize", RpcTarget.All,
        PhotonNetwork.LocalPlayer);
    }

    public PlayerController GetPlayer(int playerId)
    {
        foreach (PlayerController player in players)
        {
            if (player != null && player.id == playerId)
                return player;
        }
        return null;
    }
    public PlayerController GetPlayer(GameObject playerObj)
    {
        foreach (PlayerController player in players)
        {
            if (player != null && player.gameObject == playerObj)
                return player;
        }
        return null;
    }

    
    public void CheckWinCondition()
    {
        // Only the Master Client can decide when the match ends.
        if (!PhotonNetwork.IsMasterClient || gameEnded)
            return;

        // Count players who exist and are still alive.
        PlayerController[] livingPlayers = players
            .Where(p => p != null && !p.dead)
            .ToArray();

        alivePlayers = livingPlayers.Length;

        // Do not declare a winner until exactly one player remains.
        if (alivePlayers == 1)
        {
            gameEnded = true;

            int winningPlayerId = livingPlayers[0].id;

            photonView.RPC(
                "WinGame",
                RpcTarget.All,
                winningPlayerId
            );
        }
    }

    [PunRPC]
    void WinGame(int winningPlayer)
    {
        // Prevent duplicate end-game handling on each client.
        if (gameEnded == false)
            gameEnded = true;

        PlayerController winner = GetPlayer(winningPlayer);

        if (winner == null || winner.photonPlayer == null)
        {
            Debug.LogError("Could not find the winning player!");
            return;
        }

        // Display the winner on every client.
        GameUI.instance.SetWinText(
            winner.photonPlayer.NickName
        );

        Invoke("GoBackToMenu", postGameTime);
    }
    void GoBackToMenu()
    {
        NetworkManager.instance.ChangeScene("Menu");
    }
}
