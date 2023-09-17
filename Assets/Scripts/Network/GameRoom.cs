using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using ServerData;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Linq;
using PlayerData;
using Unity.VisualScripting;
using Debug = UnityEngine.Debug;

public class GameRoom 
{

    [Header("RoomInfo")]
    public UInt16 RoomID = 1;
    public static UInt16 RoomInedx = 1;
    private GameRoomCharacter[] _characterOrder = new GameRoomCharacter[6];


    [Header("Player 1")]
    private GamePlayerInfo _player1;
    private bool _player1Ready = false;

    private GameRoomCharacter[] _player1Character = new GameRoomCharacter[3];

    [Header("Player2")]
    private GamePlayerInfo _player2;

    private bool _player2Ready = false;

    private GameRoomCharacter[] _player2Character = new GameRoomCharacter[3];
    
    public GameRoom(GamePlayerInfo p1, GamePlayerInfo p2)
    {
        RoomID = RoomInedx;
        _player1 = p1;
        _player2 = p2;
        Debug.Log($"Room {RoomID} Created");


        SetGameRoomCharacters(p1, p2);
        SendOtherPlayerInfo(_player1, _player2);
        RoomInedx++;
    }

    private void SetGameRoomCharacters(GamePlayerInfo p1, GamePlayerInfo p2)
    {
        for (int i = 0; i < p1.MainCharacters.Length; i++)
        {
            _player1Character[i] = new GameRoomCharacter(p1.MainCharacters[i], true);
        }
        for (int i = 0; i < p2.MainCharacters.Length; i++)
        {
            _player2Character[i] = new GameRoomCharacter(p2.MainCharacters[i], false);
        }
    }

    private void SendOtherPlayerInfo(GamePlayerInfo p1, GamePlayerInfo p2)
    {
        _characterOrder = _player1Character.Concat(_player2Character)
            .OrderByDescending(character => character.Speed)
            .ToArray();

        stBattleTurnInfo[] turn = new stBattleTurnInfo[_characterOrder.Length];

        for (int i = 0; i < _characterOrder.Length; i++)
        {
            turn[i].Id = _characterOrder[i].ID;
            turn[i].IsPlayer1 = _characterOrder[i].IsPlayer1;
        }
        

        stBattleRoomCharactersInfo p1Info = new stBattleRoomCharactersInfo
        {
            MsgID = ServerData.MessageID.BattleRoomCharactersInfo,
            PacketSize = (ushort)Marshal.SizeOf(typeof(stBattleRoomCharactersInfo)),
            PlayerCharacters = p1.MainCharacters,
            OtherCharacters = p2.MainCharacters,
            RoomID = RoomID,
            IsPlayer1 = true,
            BattleTurn = turn
        };
        stBattleRoomCharactersInfo p2Info = new stBattleRoomCharactersInfo
        {
            MsgID = ServerData.MessageID.BattleRoomCharactersInfo,
            PacketSize = (ushort)Marshal.SizeOf(typeof(stBattleRoomCharactersInfo)),
            PlayerCharacters = p2.MainCharacters,
            OtherCharacters = p1.MainCharacters,
            RoomID = RoomID,
            IsPlayer1 = false,
            BattleTurn = turn

        };
        Managers.Network.SendMsg<stBattleRoomCharactersInfo>(Managers.Network._loginedClients[p1.ID], p1Info);
        Managers.Network.SendMsg<stBattleRoomCharactersInfo>(Managers.Network._loginedClients[p2.ID], p2Info);

    }



    public void ReadyPlayer(string id)
    {
        if (id == _player1.ID)
        {
            _player1Ready = true;
        }
        else if (id == _player2.ID)
        {
            _player2Ready = true;
        }

        if (_player1Ready && _player2Ready)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        stBattleStart start = new stBattleStart();
        start.MsgID = ServerData.MessageID.BattleStart;
        start.PacketSize = (ushort)Marshal.SizeOf(start);
        start.Start = true;
        Managers.Network.SendMsg(Managers.Network._loginedClients[_player1.ID], start);
        Managers.Network.SendMsg(Managers.Network._loginedClients[_player2.ID], start);

        _player1Ready = false;
        _player2Ready = false;
    }
    public void GetPlayerOrder(string id, stBattleMyOrder info)
    {
        if (id == _player1.ID)
        {
            _player1Ready = true;
            for (int i = 0; i < 3; i++)
            {
                _player1Character[i].BattleInfo = info.CharactersOrder[i];
            }
        }
        else if (id == _player2.ID)
        {
            _player2Ready = true;
            for (int i = 0; i < 3; i++)
            {
                _player2Character[i].BattleInfo = info.CharactersOrder[i];
            }
        }

        if (_player1Ready && _player2Ready)
        {
            SimulatePlayerActions();
        }
    }


    private void SimulatePlayerActions()
    {
        _player1Ready = false;
        _player2Ready = false;

        stBattleInfo battleInfo = new stBattleInfo();
        battleInfo.MsgID = ServerData.MessageID.BattleInfo;
        battleInfo.PacketSize = (ushort)Marshal.SizeOf(battleInfo);
        battleInfo.Order = new stBattleOrder[6];

        for (int i = 0; i < _characterOrder.Length; i++)
        {
            stBattleOrder order = new stBattleOrder();
            order.IsMyCharacter = _characterOrder[i].IsPlayer1;
            order.CharacterIndex = _characterOrder[i].BattleInfo.CharacterIndex;
            order.TargetIndex = _characterOrder[i].BattleInfo.TargetIndex;
            switch (_characterOrder[i].BattleInfo.State)
            {
                case (ushort)CharacterState.Attack:
                    if (_characterOrder[i].IsPlayer1)
                    {
                        _characterOrder[i].Attack(_player2Character[_characterOrder[i].BattleInfo.TargetIndex]);
                    }
                    else
                    {
                        _characterOrder[i].Attack(_player1Character[_characterOrder[i].BattleInfo.TargetIndex]);
                    }

                    break;
            }

            order.State = _characterOrder[i].BattleInfo.State;
            battleInfo.Order[i] = order;
        }

        battleInfo.GameState = CheckGameState();

        Debug.Log("SendInfo");
        Managers.Network.SendMsg<stBattleInfo>(Managers.Network._loginedClients[_player1.ID], battleInfo);
        for (int i = 0; i < battleInfo.Order.Length; i++)
        {
            battleInfo.Order[i].IsMyCharacter = !battleInfo.Order[i].IsMyCharacter;
        }

        Managers.Network.SendMsg<stBattleInfo>(Managers.Network._loginedClients[_player2.ID], battleInfo);




        switch (battleInfo.GameState)
        {
            case ((UInt16)GameState.ContinueSelect):
                break;
            case ((UInt16)GameState.Player1Win):
                Debug.Log("Player 1 Win");
                break;
            case ((UInt16)GameState.Player2Win):
                Debug.Log("Player 2 Win");
                break;
        }
    }

    private UInt16 CheckGameState() // 동시에 두 플레이어 캐릭터가 모두 죽는 경우는 없음 0 -> 
    {
        bool isEnd = true;
        for (int i = 0; i < _player1Character.Length; i++)
        {
            if (!_player1Character[i].IsDeath())
            {
                isEnd = false;
                break;
            }
        }

        if (isEnd)
            return (UInt16)GameState.Player1Win;

        isEnd = true;
        for (int i = 0; i < _player2Character.Length; i++)
        {
            if (!_player2Character[i].IsDeath())
            {
                isEnd = false;
                break;
            }
        }
        if (isEnd)
            return (UInt16)GameState.Player2Win;
        else
            return (UInt16)GameState.ContinueSelect;
    }

    public void GetParticularInfo(stBattleParticualInfo info)
    {
        stBattleParticualInfo particularInfo = new stBattleParticualInfo();
        particularInfo.MsgID = ServerData.MessageID.BattleParticularInfo;
        particularInfo.PacketSize = (ushort)Marshal.SizeOf(particularInfo);
        particularInfo.ParticularInfo = info.ParticularInfo;
        if (info.ID == _player1.ID)
        {
            Managers.Network.SendMsg<stBattleParticualInfo>(Managers.Network._loginedClients[_player2.ID], particularInfo);
        }
        else
        {
            Managers.Network.SendMsg<stBattleParticualInfo>(Managers.Network._loginedClients[_player1.ID], particularInfo);
        }

        EndGame();

    }
    private void EndGame()
    {
        if (!Managers.Network.GameRooms.ContainsKey(RoomID))
            return;
        else
        {
            Debug.Log($"Room {RoomID} has removed");
            Managers.Network.GameRooms.Remove(RoomID);
        }

    }

}

public class GameRoomCharacter
{
    private UInt16 _id = 0;
    private UInt16 _curHp = 0;
    private UInt16 _damage = 0 ;
    private UInt16 _armor = 0;
    private UInt16 _speed = 0;
    public bool IsPlayer1 = false;
    public UInt16 Speed { get { return _speed; } }
    public UInt16  ID { get { return _id; } }
    public stBattleMyCharacterOrder BattleInfo;


    public GameRoomCharacter(stCharacterInfo info, bool isPlayer1)
    {
        _id = info.ChID;
        _curHp = info.ChHp;
        _damage = info.ChDamage;
        _armor = info.ChArmor;
        _speed = info.ChSpd;
        IsPlayer1 = isPlayer1;
    }

    public void Attack(GameRoomCharacter character)
    {
        if (IsDeath())
            return;
        character.TakeHit(_damage);
    }
    public void TakeHit(UInt16 amount)
    {
        if (IsDeath())
            return;

        UInt16 damage = (ushort)(amount - _armor);
        if (BattleInfo.State == (ushort)CharacterState.Defense)
        {
            damage /= 2;
        }

        if (_curHp <= damage)
        {
            _curHp = 0;
        }
        else
        {
          _curHp -= damage;

        }
    }

    public bool IsDeath()
    {
        if (_curHp <= 0)
            return true;
        return false;
    }

}
