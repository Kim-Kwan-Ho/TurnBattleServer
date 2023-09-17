using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers s_instance = null;
    public static Managers Instance
    {
        get { //Init();
            return s_instance;
        }
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


    private void Start()
    {
        s_instance = this;
        DontDestroyOnLoad(s_instance);
        Application.targetFrameRate = 60;
        Init();
    }

    private void Init()
    {
       //s_instance = GameObject.Find("@Managers").GetComponent<Managers>();
        if (s_instance != null)
        {
            //GameObject go = GameObject.Find("@Managers");
            //if (go == null)
            //    go = new GameObject("@Managers");

            //s_instance = go.AddComponent<Managers>();
            //DontDestroyOnLoad(s_instance);
            //Application.targetFrameRate = 60;
            //s_instance._network.Init();
        }
        _network.Init();
    }

    private void OnApplicationQuit()
    {
        s_instance._network.CloseSocket();
    }


}
