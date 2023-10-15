using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using OpenCover.Framework.Model;
using UnityEngine;
using ServerData;
using PlayerData;
using File = System.IO.File;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

public class DataManager
{
    private const string DataPath = "Assets/Resources/PlayerData/";
    public Dictionary<string, stPlayerInfo> PlayerInfos = new Dictionary<string, stPlayerInfo>();

    public bool Login(string id, string password) // 로그인
    {
        if (!CheckData(id) || PlayerInfos.ContainsKey(id)) // 해당 정보가 없거나 이미 로그인 되어 있으면 실패 반환
            return false;

        stPlayerInfo player = JsonUtility.FromJson<stPlayerInfo>(File.ReadAllText(FilePath(id)));
        if (player.Password == password) // 로그인 정보가 일치하면 플레이어 정보 추가 및 성공 반환
        {
            PlayerInfos.Add(player.ID, player); 
            return true; 
        }
        return false;
    }
    private bool CheckData(string id) // 해당 데이터가 있는지 확인
    {
        if (id == null)
            return false;

        return File.Exists(FilePath(id));
    }
    private string FilePath(string id)
    {
        return Path.Combine(DataPath, $"{id}.json");
    }

    public bool Register(string id, string password) // 회원가입
    {
        if (CheckData(id)) // 해당 데이터(아이디)가 있으면 실패 반환
            return false;

        Debug.Log(password);
        stPlayerInfo player = new stPlayerInfo(id, password);
        SaveData(player); // 데이터 저장
        return true;
    }

    private void SaveData(stPlayerInfo player) // 회원 가입시 플레이어 저장용
    {
        string saveData = JsonUtility.ToJson(player);
        File.WriteAllText(FilePath(player.ID), saveData);
    }
    public void SaveData(string id, stPlayerInfo player) // 로그인 혹은 가입된 플레이어 정보 저장용
    {
        PlayerInfos[id] = player;
        string saveData = JsonUtility.ToJson(PlayerInfos[id]);
        File.WriteAllText(FilePath(id), saveData);
    }
    public stPlayerInfo GetPlayerInfo(string id) // 해당 아이디의 플레이어 정보 반환
    {
        PlayerInfos.TryGetValue(id, out stPlayerInfo playerInfo);
        return playerInfo;
    }
    public void LogOut(string id) // 로그아웃
    {
        if (!PlayerInfos.ContainsKey(id)) 
            return;

        SaveData(id, PlayerInfos[id]);
        PlayerInfos.Remove(id);
    }
}
