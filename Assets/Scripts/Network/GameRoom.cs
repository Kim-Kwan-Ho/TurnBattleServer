using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using ServerData;
using System.Linq;
using PlayerData;
using Debug = UnityEngine.Debug;

public class GameRoom 
{

    [Header("RoomInfo")]
    public UInt16 RoomID = 1;
    private static UInt16 RoomIndex = 1;
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
        RoomID = RoomIndex; //  RoomIndex => static으로 설정
        _player1 = p1;
        _player2 = p2;
        Debug.Log($"Room {RoomID} Created");
        SetGameRoomCharacters(p1, p2); // 게임룸 캐릭터 설정
        SendOtherPlayerInfo(_player1, _player2); // 게임룸 정보를 플레이어에게 전송
        RoomIndex++;
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
            turn[i].CharacterID = _characterOrder[i].ID;
            turn[i].IsPlayer1 = _characterOrder[i].IsPlayer1;
        }
        

        stBattleRoomInfo p1Info = new stBattleRoomInfo
        {
            MsgID = ServerData.MessageID.BattleRoomInfo,
            PacketSize = (ushort)Marshal.SizeOf(typeof(stBattleRoomInfo)),
            PlayerCharacters = p1.MainCharacters,
            OtherCharacters = p2.MainCharacters,
            RoomID = RoomID,
            IsPlayer1 = true,
            BattleTurn = turn
        };
        stBattleRoomInfo p2Info = new stBattleRoomInfo
        {
            MsgID = ServerData.MessageID.BattleRoomInfo,
            PacketSize = (ushort)Marshal.SizeOf(typeof(stBattleRoomInfo)),
            PlayerCharacters = p2.MainCharacters,
            OtherCharacters = p1.MainCharacters,
            RoomID = RoomID,
            IsPlayer1 = false,
            BattleTurn = turn

        };
        Managers.Network.SendMsg<stBattleRoomInfo>(Managers.Network._loginedClients[p1.ID], p1Info);
        Managers.Network.SendMsg<stBattleRoomInfo>(Managers.Network._loginedClients[p2.ID], p2Info);

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
    public void GetPlayerOrder(string id, stBattlePlayerOrder info)
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


    private void SimulatePlayerActions() // 행동 처리
    {
        _player1Ready = false;
        _player2Ready = false;

        stBattleOrdersInfo battleInfo = new stBattleOrdersInfo(); // 전투 정보(한 턴)
        battleInfo.MsgID = ServerData.MessageID.BattleOrdersInfo;
        battleInfo.PacketSize = (ushort)Marshal.SizeOf(battleInfo);
        battleInfo.Order = new stBattleCharacterOrder[6]; // 전투 정보(캐릭터)
        for (int i = 0; i < _characterOrder.Length; i++)
        {
            stBattleCharacterOrder order = new stBattleCharacterOrder();
            order.IsMyCharacter = _characterOrder[i].IsPlayer1; // Player1 캐릭터인지
            order.CharacterIndex = _characterOrder[i].BattleInfo.CharacterIndex; // 캐릭터 번호
            order.TargetIndex = _characterOrder[i].BattleInfo.TargetIndex; // 공격일 경우 타겟이 누구인지
            switch (_characterOrder[i].BattleInfo.State) // 현재는 공격, 방어, 행동 없음 3가지만 유효
            {
                case (ushort)CharacterState.Attack: // 공격일 경우
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

            order.State = _characterOrder[i].BattleInfo.State; // 해당 캐릭터의 상태 저장
            battleInfo.Order[i] = order; 
        }

        battleInfo.GameState = CheckGameState(); // 게임 상태 저장

        // 정보 전송
        Managers.Network.SendMsg<stBattleOrdersInfo>(Managers.Network._loginedClients[_player1.ID], battleInfo);
        for (int i = 0; i < battleInfo.Order.Length; i++)
        {
            battleInfo.Order[i].IsMyCharacter = !battleInfo.Order[i].IsMyCharacter;
        }
        Managers.Network.SendMsg<stBattleOrdersInfo>(Managers.Network._loginedClients[_player2.ID], battleInfo);


        switch (battleInfo.GameState)
        {
            case ((UInt16)GameState.ContinueSelect):
                break;
            case ((UInt16)GameState.Player1Win):
                EndGame();
                Debug.Log("Player 1 Win");
                break;
            case ((UInt16)GameState.Player2Win):
                EndGame();
                Debug.Log("Player 2 Win");
                break;
        }
    }

    private UInt16 CheckGameState() // 게임 진행 상태
    {
        // 동시에 두 플레이어 캐릭터가 모두 죽는 경우는 없어 순차적으로 탐색
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

    public void GetParticularInfo(stBattleParticularInfo info)
    {
        stBattleParticularInfo particularInfo = new stBattleParticularInfo();
        particularInfo.MsgID = ServerData.MessageID.BattleParticularInfo;
        particularInfo.PacketSize = (ushort)Marshal.SizeOf(particularInfo);
        particularInfo.ParticularInfo = info.ParticularInfo;
        if (info.ID == _player1.ID)
        {
            Managers.Network.SendMsg<stBattleParticularInfo>(Managers.Network._loginedClients[_player2.ID], particularInfo);
        }
        else
        {
            Managers.Network.SendMsg<stBattleParticularInfo>(Managers.Network._loginedClients[_player1.ID], particularInfo);
        }

        EndGame();

    }

    private void EndGame()
    {
        Managers.GameRoom.CloseGameRoom(RoomID);
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
