using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers s_instance = null;
    public static Managers Instance
    {
        get { return s_instance; }
    }

    private NetworkManager _network = new NetworkManager();
    public static NetworkManager Network
    {
        get { return Instance._network; }
    }

    private DataManager _data = new DataManager();
    public static DataManager Data
    {
        get { return Instance._data; }
    }
    private GameRoomManager _gameRoom  = new GameRoomManager();

    public static GameRoomManager GameRoom
    {
        get { return Instance._gameRoom; }
    }

    private void Start()
    {
        s_instance = this;
        DontDestroyOnLoad(s_instance);
        Application.targetFrameRate = 60;
        Init();
    }

    private void Init()
    {
        _network.Init();
        _gameRoom.Init();
    }

    private void OnApplicationQuit()
    {
        s_instance._network.CloseSocket();
    }


}
