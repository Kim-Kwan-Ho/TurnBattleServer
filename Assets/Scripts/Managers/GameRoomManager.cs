using ServerData;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GameRoomManager 
{
    
    private ConcurrentQueue<string> _matchingQueue = new ConcurrentQueue<string>(); // 매칭 대기열
    private HashSet<string> _canceledMatch = new HashSet<string>(); // 매칭 취소한 플레이어
    // 매칭 대기열에 플레이어가 추가됐다는 신호
    private ManualResetEventSlim _playerAddedEvent = new ManualResetEventSlim(false);
    // 진행중인 게임 방을 관리할 딕셔너리
    private ConcurrentDictionary<int, GameRoom> _gameRooms = new ConcurrentDictionary<int, GameRoom>();
    // 클라이언트에서 정보를 받아 게임룸 게임을 진행시켜주는 큐
    private BlockingCollection<Action> _gameRoomActionQueue = new BlockingCollection<Action>();

    public void Init()
    {
        Task.Run(ExecuteGameRoomActions);
        Task.Run(MatchMaking);
    }

    public void AddMatch(string id)
    {
        _matchingQueue.Enqueue(id);
        if (!_playerAddedEvent.IsSet)
        {
            _playerAddedEvent.Set();
        }
    }

    public void AddCanceledMatch(string id)
    {
        if (!_canceledMatch.Contains(id))
            _canceledMatch.Add(id);
    }

    public void ReadyPlayerAction(stBattleReady info)
    {
        _gameRoomActionQueue.Add(() => { _gameRooms[info.RoomID].ReadyPlayer(info.ID); });
    }

    public void GetPlayerOrderAction(stBattlePlayerOrder info)
    {
        _gameRoomActionQueue.Add(() => { _gameRooms[info.RoomID].GetPlayerOrder(info.ID, info); });
    }

    public void GetParticularInfoAction(stBattleParticularInfo info)
    {
        _gameRoomActionQueue.Add(() => { Managers.GameRoom._gameRooms[info.RoomID].GetParticularInfo(info); });
    }
    private Task MatchMaking() // 플레이어 매칭 시스템
    {
        while (true)
        {
            _playerAddedEvent.Wait(); // 플레이어 매칭큐에 추가 될때까지 대기
            if (_matchingQueue.Count >= 2)
            {
                string player1 = GetNextMatchingPlayer();
                if (player1 != null)
                {
                    string player2 = GetNextMatchingPlayer();

                    if (player1 != player2 && player2 != null)
                    {
                        GamePlayerInfo playerInfo1 = CreateGamePlayerInfo(player1);
                        GamePlayerInfo playerInfo2 = CreateGamePlayerInfo(player2);
                        GameRoom room = new GameRoom(playerInfo1, playerInfo2); // 게임룸 생성
                        _gameRooms.TryAdd(room.RoomID, room);
                    }
                    else
                    {
                        _matchingQueue.Enqueue(player1); // 다시 매칭 대기열에 삽입
                    }
                }
            }
            else
            {
                _playerAddedEvent.Reset();
            }
        }
    }
    
    private string GetNextMatchingPlayer()
    {
        while (_matchingQueue.Count > 0)
        {
            _matchingQueue.TryDequeue(out string player);
            if (!_canceledMatch.Contains(player)) // 매칭취소 해쉬에 없을 경우
            {
                return player;
            }
            _canceledMatch.Remove(player);
        }
        return null;
    }
    private GamePlayerInfo CreateGamePlayerInfo(string playerId)
    {
        return new GamePlayerInfo // ID와 대표 캐릭터를 받아 GamePlayerInfo생성
        {
            ID = playerId,
            MainCharacters = Managers.Data.PlayerInfos[playerId].MainCharacters
        };
    }

    private void ExecuteGameRoomActions() // 개별 스레드(_gameRoomExecutorThread)에서 실행, 게임 룸 관리
    {
        // 비어있으면 자동으로 대기상태, 추가시 자동으로 작동
        foreach (var action in _gameRoomActionQueue.GetConsumingEnumerable())
        {
            action?.Invoke();
        }
    }

    public void CloseGameRoom(int roomId)
    {
        if (_gameRooms.TryRemove(roomId, out _))
        {
            Debug.Log($"Room {roomId} has removed");
        }
        else
        {
            Debug.Log($"Failed to remove room {roomId}");
        }
    }
}
