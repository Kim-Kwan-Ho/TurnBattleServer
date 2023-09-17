using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using PlayerData;
using ServerData;
using Unity.VisualScripting;
using System.Runtime.InteropServices;
using System.Net;
using System.Threading.Tasks;
using UnityEngine.UIElements;

public class TCPNetworkModule
{
    public TcpClient ClientSocket = null;
    public NetworkStream Stream = null;
    public byte[] Bytes = new byte[ServerData.Constants.BufferSize];
    public byte[] TempBytes = new byte[ServerData.Constants.TempBufferSize];

    public string Name;
    public TCPNetworkModule(TcpClient clientSocket, string name = "client")
    {
        this.ClientSocket = clientSocket;
        this.Stream = clientSocket.GetStream();
        this.Name = name;
    }

    private bool _isTempByte = false;
    private int _tempByteSize = 0;

    public void SetIsTempByte(bool isTempByte)
    {
        this._isTempByte = isTempByte;
    }

    public bool GetIsTempByte()
    {
        return this._isTempByte;
    }

    public void SetTempByteSize(int tempByteSize)
    {
        this._tempByteSize = tempByteSize;
    }

    public int GetTempByteSize()
    {
        return this._tempByteSize;
    }


}



public class NetworkManager
{

    private Thread _connectListenerThread = null;
    private Thread _tcpListenerThread = null;
    private Thread _gameRoomThread = null;

    private TcpListener _tcpListener = null;
    private NetworkStream _theStream = null;

    private List<TCPNetworkModule> _connectedClients = new List<TCPNetworkModule>();
    private List<TCPNetworkModule> _disConnectedClients = new List<TCPNetworkModule>();
    public Dictionary<string, TCPNetworkModule> _loginedClients = new Dictionary<string, TCPNetworkModule>();
    public string IP = "127.0.0.1";
    public int Port = 9001;

    public bool ServerReady = false;

    private LinkedList<string> _matchingLinkedList = new LinkedList<string>();
    private HashSet<string> _canceledMatch = new HashSet<string>();
    public Dictionary<int, GameRoom> GameRooms = new Dictionary<int, GameRoom>();
    private ManualResetEvent _playerAddedEvent = new ManualResetEvent(false);

    public byte[] GetObjectToByte<T>(T str) where T : struct
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return arr;
    }

    public T GetObjectFromByte<T>(byte[] arr) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(arr, 0, ptr, size);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public void Init()
    {
        CreateServer();
    }

    public void CreateServer()
    {
        _connectListenerThread = new Thread(new ThreadStart(ListenForIncomingRequest));
        _connectListenerThread.IsBackground = true;
        _connectListenerThread.Start();

        _tcpListenerThread = new Thread(new ThreadStart(TcpListenForIncomingRequest));
        _tcpListenerThread.IsBackground = true;
        _tcpListenerThread.Start();

        _gameRoomThread = new Thread(new ThreadStart(MatchMakingAndGameRooms));
        _gameRoomThread.IsBackground = true;
        _gameRoomThread.Start();

    }

    public void ListenForIncomingRequest()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();

            ServerReady = true;

            while (true)
            {
                if (!ServerReady)
                    break;

                if (_tcpListener.Pending())
                {
                    int index = 0;
                    while (true)
                    {
                        bool isFind = false;
                        foreach (TCPNetworkModule client in _connectedClients)
                        {
                            if (client.Name == "client_" + index.ToString())
                            {
                                isFind = true;
                                break;
                            }
                        }
                        if (!isFind)
                        {
                            _connectedClients.Add(new TCPNetworkModule(_tcpListener.AcceptTcpClient(), "client_" + index.ToString()));
                            break;
                        }
                        index++;
                    }
                }

                foreach (TCPNetworkModule client in _connectedClients)
                {
                    if (client != null)
                    {
                        if (!IsConnected(client.ClientSocket))
                        {
                            _disConnectedClients.Add(client);
                        }
                    }
                }

                for (int i = _disConnectedClients.Count - 1; i >= 0; i--)
                {
                    Managers.Data.LogOut(_disConnectedClients[i].Name);
                    _connectedClients.Remove(_disConnectedClients[i]);
                    if (_loginedClients.ContainsKey(_disConnectedClients[i].Name))
                        _loginedClients.Remove(_disConnectedClients[i].Name);
                    _disConnectedClients.Remove(_disConnectedClients[i]);

                }

                Thread.Sleep(10);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    public void TcpListenForIncomingRequest()
    {
        while (true)
        {
            if (_connectedClients.Count <= 0)
            {
                Thread.Sleep(100);
            }

            for (int c = 0; c < _connectedClients.Count; c++)
            {
                if (_connectedClients[c].ClientSocket.Connected)
                {
                    _theStream = _connectedClients[c].ClientSocket.GetStream();
                    Thread.Sleep(10);
                    if (_theStream is { DataAvailable: true })
                    {
                        int length = 0;
                        if ((length = _theStream.Read(_connectedClients[c].Bytes, 0,
                                _connectedClients[c].Bytes.Length)) != 0)
                        {

                            byte[] inData = new byte[length + _connectedClients[c].GetTempByteSize()];
                            if (_connectedClients[c].GetIsTempByte())
                            {
                                Array.Copy(_connectedClients[c].TempBytes, 0, inData, 0, _connectedClients[c].GetTempByteSize());
                                Array.Copy(_connectedClients[c].Bytes, 0, inData, _connectedClients[c].GetTempByteSize(), length);
                            }
                            else
                                Array.Copy(_connectedClients[c].Bytes, 0, inData, 0, length);

                            int nDataCur = 0;

                            while (true)
                            {
                                byte[] headerData = new byte[ServerData.Constants.HeaderSize];
                                Array.Copy(inData, nDataCur, headerData, 0, 6);
                                stHeader header = GetObjectFromByte<stHeader>(headerData);
                                if (header.PacketSize > length - nDataCur)
                                {
                                    Array.Copy(inData, nDataCur, _connectedClients[c].TempBytes, 0,
                                        length - nDataCur);
                                    _connectedClients[c].SetIsTempByte(true);
                                    _connectedClients[c].SetTempByteSize(length - nDataCur);
                                    break;
                                }
                                byte[] msgData = new byte[header.PacketSize];
                                Array.Copy(inData, nDataCur, msgData, 0, header.PacketSize);
                                IncomingDataProcess(header.MsgID, msgData, c);

                                nDataCur += header.PacketSize;
                                if (length == nDataCur)
                                {
                                    _connectedClients[c].SetIsTempByte(false);
                                    _connectedClients[c].SetTempByteSize(0);
                                    break;
                                }

                            }
                        }
                    }
                }

            }

            Thread.Sleep(1);
        }
    }
    private bool IsConnected(TcpClient client)
    {
        if (client?.Client == null || !client.Client.Connected)
            return false;

        try
        {
            return !(client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
        }
        catch
        {
            return false;
        }
    }

    public void SendMsg(TCPNetworkModule client, byte[] message)
    {
        if (!ServerReady)
            return;

        client.Stream.Write(message, 0, message.Length);
        client.Stream.Flush();
    }

    public void SendMsg<T>(TCPNetworkModule client, T msg) where T : struct
    {
        if (!ServerReady)
            return;

        var message = GetObjectToByte<T>(msg);

        client.Stream.Write(message, 0, message.Length);
        client.Stream.Flush();
    }

    public void CloseSocket()
    {
        if (!ServerReady)
        {
            return;
        }

        if (_tcpListener != null)
        {
            _tcpListener.Stop();
            _tcpListener = null;

            ServerReady = false;

            _connectListenerThread.Abort();
            _connectListenerThread = null;
            _tcpListenerThread.Abort();
            _tcpListenerThread = null;
            _gameRoomThread.Abort();
            _gameRoomThread = null;

            foreach (TCPNetworkModule client in _connectedClients)
            {
                client.Stream = null;
                client.ClientSocket.Close();
            }

            _connectedClients.Clear();
        }
    }

    private void IncomingDataProcess(ushort msgId, byte[] msgData, int c)
    {
        switch (msgId)
        {
            case ServerData.MessageID.LoginRegister:
            {
                stLoginRegister info = GetObjectFromByte<stLoginRegister>(msgData);
                if (info.IsLogin) // 로그인 여부
                {
                    info.Succeed = Managers.Data.Login(info.ID, info.Password); // 로그인 성공 여부
                    if (info.Succeed)
                    {
                        _connectedClients[c].Name = info.ID;
                        _loginedClients.Add(info.ID, _connectedClients[c]);
                        stPlayerInfo playerInfo = Managers.Data.GetPlayerInfo(info.ID); // 해당 플레이어 정보
                        SendMsg(_connectedClients[c], GetObjectToByte(playerInfo)); // 플레이어 정보 반환
                    }
                    else
                    {
                        SendMsg(_connectedClients[c], GetObjectToByte(info)); // 실패 반환
                    }
                }
                else
                {
                    info.Succeed =
                        Managers.Data.Register(info.ID, info.Password); // 회원가입 성공 정보
                    SendMsg(_connectedClients[c], GetObjectToByte(info));
                }
                break;
            }
            case ServerData.MessageID.PlayerInfo:
            {
                stPlayerInfo info = GetObjectFromByte<stPlayerInfo>(msgData);
                Managers.Data.SaveData(info.ID, info);
                break;
            }
            case ServerData.MessageID.MatchPlayerInfo:
            {
                stMatchPlayerInfo info = GetObjectFromByte<stMatchPlayerInfo>(msgData);
                if (info.Matching)
                {
                    lock (_matchingLinkedList)
                    {
                        _matchingLinkedList.AddLast(info.ID);
                    }

                    _playerAddedEvent.Set();
                }
                else
                {
                    _canceledMatch.Add(info.ID);
                }
                break;
            }
            case ServerData.MessageID.BattleReadyInfo:
            {
                stBattleReadyInfo info = GetObjectFromByte<stBattleReadyInfo>(msgData);
                Debug.Log($"Room ID: {info.RoomID} Character ID {info.ID}");
                if (GameRooms.ContainsKey(info.RoomID))
                {
                    Debug.Log($"ID: {info.ID} Ready");
                    GameRooms[info.RoomID].ReadyPlayer(info.ID);
                }
                break;
            }
            case ServerData.MessageID.BattleSelectInfo:
            {
                stBattleMyOrder info = GetObjectFromByte<stBattleMyOrder>(msgData);
                if (GameRooms.ContainsKey(info.RoomID))
                {
                    GameRooms[info.RoomID].GetPlayerOrder(info.ID, info);
                }
                break;
            }
            case ServerData.MessageID.BattleParticularInfo:
            {
                stBattleParticualInfo info = GetObjectFromByte<stBattleParticualInfo>(msgData);
                if (GameRooms.ContainsKey(info.RoomID))
                {
                        GameRooms[info.RoomID].GetParticularInfo(info);
                }
                break;
            }
        }
    }

    private void MatchMakingAndGameRooms()
    {
        while (true)
        {
            _playerAddedEvent.WaitOne();
            if (_matchingLinkedList.Count >= 2)
            {
                Debug.Log("MatchStart");

                string player1 = null;
                string player2 = null;

                while (player1 == null && _matchingLinkedList.Count > 0)
                {
                    player1 = _matchingLinkedList.First.Value;
                    _matchingLinkedList.RemoveFirst();

                    if (_canceledMatch.Contains(player1) || !Managers.Data.PlayerInfos.ContainsKey(player1))
                    {
                        _canceledMatch.Remove(player1);
                        player1 = null;
                    }
                }

                while (player2 == null && _matchingLinkedList.Count > 0 && player1 != null)
                {
                    player2 = _matchingLinkedList.First.Value;
                    _matchingLinkedList.RemoveFirst();

                    if (_canceledMatch.Contains(player2) || player1 == player2 ||
                        !Managers.Data.PlayerInfos.ContainsKey(player2))
                    {
                        _canceledMatch.Remove(player2);
                        player2 = null;
                    }
                }


                if (player1 != null && player2 == null)
                {
                    _matchingLinkedList.AddFirst(player1);
                }
                else if (player1 != null)
                {
                    GamePlayerInfo playerInfo1 = new GamePlayerInfo
                    {
                        ID = player1,
                        MainCharacters = Managers.Data.PlayerInfos[player1].MainCharacters
                    };

                    GamePlayerInfo playerInfo2 = new GamePlayerInfo
                    {
                        ID = player2,
                        MainCharacters = Managers.Data.PlayerInfos[player2].MainCharacters
                    };

                    GameRoom room = new GameRoom(playerInfo1, playerInfo2);
                    GameRooms.Add(room.RoomID, room);
                }

                _playerAddedEvent.Reset();
                Thread.Sleep(100);
            }
        }
    }


}

