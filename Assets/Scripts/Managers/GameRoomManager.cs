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
    
    private ConcurrentQueue<string> _matchingQueue = new ConcurrentQueue<string>(); // ��Ī ��⿭
    private HashSet<string> _canceledMatch = new HashSet<string>(); // ��Ī ����� �÷��̾�
    // ��Ī ��⿭�� �÷��̾ �߰��ƴٴ� ��ȣ
    private ManualResetEventSlim _playerAddedEvent = new ManualResetEventSlim(false);
    // �������� ���� ���� ������ ��ųʸ�
    private ConcurrentDictionary<int, GameRoom> _gameRooms = new ConcurrentDictionary<int, GameRoom>();
    // Ŭ���̾�Ʈ���� ������ �޾� ���ӷ� ������ ��������ִ� ť
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
    private Task MatchMaking() // �÷��̾� ��Ī �ý���
    {
        while (true)
        {
            _playerAddedEvent.Wait(); // �÷��̾� ��Īť�� �߰� �ɶ����� ���
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
                        GameRoom room = new GameRoom(playerInfo1, playerInfo2); // ���ӷ� ����
                        _gameRooms.TryAdd(room.RoomID, room);
                    }
                    else
                    {
                        _matchingQueue.Enqueue(player1); // �ٽ� ��Ī ��⿭�� ����
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
            if (!_canceledMatch.Contains(player)) // ��Ī��� �ؽ��� ���� ���
            {
                return player;
            }
            _canceledMatch.Remove(player);
        }
        return null;
    }
    private GamePlayerInfo CreateGamePlayerInfo(string playerId)
    {
        return new GamePlayerInfo // ID�� ��ǥ ĳ���͸� �޾� GamePlayerInfo����
        {
            ID = playerId,
            MainCharacters = Managers.Data.PlayerInfos[playerId].MainCharacters
        };
    }

    private void ExecuteGameRoomActions() // ���� ������(_gameRoomExecutorThread)���� ����, ���� �� ����
    {
        // ��������� �ڵ����� ������, �߰��� �ڵ����� �۵�
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
