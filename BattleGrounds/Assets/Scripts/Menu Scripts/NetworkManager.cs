using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{

    public int maxPlayers = 10;
    
    // instance
    public static NetworkManager instance;
    void Awake()
    {
        instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // connect to the master server
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("We've connected to the master server!");
        
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby.");
    }

    // creates a new room of the requested room name
    public void CreateRoom(string roomName)
    {
        if (PhotonNetwork.Server != ServerConnection.MasterServer)
        {
            Debug.LogWarning("Cannot create room: Not on Master Server.");
            return;
        } 

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)maxPlayers;

        PhotonNetwork.CreateRoom(roomName, options);
    }

    // attempts to join a room
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Cannot join room: Photon is not connected and ready.");
            return;
        }

        if (PhotonNetwork.Server != ServerConnection.MasterServer)
        {
            Debug.LogWarning(
                "Cannot join room: Client is currently on " +
                PhotonNetwork.Server +
                ". Waiting for Master Server."
            );
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    [PunRPC]
    public void ChangeScene(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }

    [PunRPC]
    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonNetwork.LoadLevel("Menu");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        GameManager.instance.alivePlayers--;
        GameUI.instance.UpdatePlayerInfoText();

        if (PhotonNetwork.IsMasterClient)
        {
            GameManager.instance.CheckWinCondition();
        }
    }


}
