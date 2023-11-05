using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using MySql.Data.MySqlClient;
using Data;

public class DBManager
{
    private MySqlConnection _connection = null;
    public Dictionary<string, stPlayerInfo> PlayerInfos = new Dictionary<string, stPlayerInfo>();
    private string _ip = "127.0.0.1";
    private string _dbName = "turnbattle";
    private string _dbId = "turn";
    private string _dbPwd = "turn";

    public void Init()
    {
        string conStr = string.Format($"Server={_ip};DataBase={_dbName};Uid={_dbId};Pwd={_dbPwd};");
        try
        {
            _connection = new MySqlConnection(conStr);
            _connection.Open();
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
    }
    public async Task<bool> LoginAsync(string id, string pwd) // 로그인
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = _connection;
        cmd.CommandText = $"SELECT password FROM playerinfo WHERE id = '{id}'"; // id에 맞는 비밀번호 가져오기
        var result = await cmd.ExecuteScalarAsync();
        if (result == null) // 정보가 없을경우 실패 반환
            return false;
        else
            return (string)result == pwd; // 비밀번호가 맞는지 여부 반환
    }
    public async Task<bool> RegisterAsync(string id, string pwd) // 회원가입
    {
        try
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = _connection;
            cmd.CommandText = $"INSERT INTO playerinfo(id,password,gold,token) VALUES('{id}','{pwd}'," +
                              $"'{Constants.PlayerRegisterGold}','{Constants.PlayerRegisterToken}');"; // 플렝이어 정보 생성
            await cmd.ExecuteNonQueryAsync();
            for (int i = 0; i < Constants.MainCharacterCount; i++)
            {
                cmd.CommandText = $"INSERT INTO maincharacters(id,indexnum) VALUES('{id}','{i}');"; // 메인 캐릭터 추가
                await cmd.ExecuteNonQueryAsync();
            }
            return true;
        }
        catch (MySqlException e)
        {
            if (e.Number == 1062) // 해당 아이디가 있을 경우 실패 반환
            {
                return false;
            }
            else
            {
                Debug.Log(e);
                throw;
            }
        }
    }
    public async Task<stPlayerInfo> GetPlayerInfoAsync(string id) // 플레이어 정보 반환
    {
        stPlayerInfo info = new stPlayerInfo();
        info.MsgID = MessageID.PlayerInfo;
        info.ID = id;
        info.PacketSize = (ushort)Marshal.SizeOf(typeof(stPlayerInfo));
        try
        {
            MySqlCommand cmd = new MySqlCommand();
            DataSet ds = new DataSet();
            cmd.Connection = _connection;
            cmd.CommandText = $"SELECT * FROM playerinfo WHERE id = '{id}';";
            MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(ds, "uInfo");
            info.GoldAmount = (UInt32)ds.Tables[0].Rows[0]["gold"];
            info.TokenCount = (UInt32)ds.Tables[0].Rows[0]["token"];
            info.CharacterCount = (UInt16)ds.Tables[0].Rows[0]["charactercount"];

            cmd.CommandText = $"SELECT chid, chlevel, chhp, chdamage, charmor, chspd" +
                              $" FROM playerinfo JOIN maincharacters ON playerinfo.id = maincharacters.id " +
                              $"WHERE maincharacters.id ='{id}';";
            adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(ds, "mainChInfo");
            stCharacterInfo[] mainChs = new stCharacterInfo[Constants.MainCharacterCount];
            for (int i = 0; i < Constants.MainCharacterCount; i++)
            {
                mainChs[i] = new stCharacterInfo
                {
                    ChID = (UInt16)ds.Tables[1].Rows[i]["chid"],
                    ChLevel = (UInt16)ds.Tables[1].Rows[i]["chlevel"],
                    ChHp = (UInt16)ds.Tables[1].Rows[i]["chhp"],
                    ChDamage = (UInt16)ds.Tables[1].Rows[i]["chdamage"],
                    ChArmor = (UInt16)ds.Tables[1].Rows[i]["charmor"],
                    ChSpd = (UInt16)ds.Tables[1].Rows[i]["chspd"]
                };

            }
            info.MainCharacters = mainChs;
            if (info.CharacterCount > 0)
            {
                stCharacterInfo[] chs = new stCharacterInfo[info.CharacterCount];
                cmd.CommandText = $"SELECT chid, chlevel, chhp, chdamage, charmor, chspd" +
                                  $" FROM playerinfo JOIN characters ON playerinfo.id = characters.id " +
                                  $"WHERE characters.id ='{id}';";
                adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(ds, "chsInfo");

                for (int i = 0; i < info.CharacterCount; i++)
                {
                    chs[i] = new stCharacterInfo
                    {
                        ChID = (UInt16)ds.Tables[2].Rows[i]["chid"],
                        ChLevel = (UInt16)ds.Tables[2].Rows[i]["chlevel"],
                        ChHp = (UInt16)ds.Tables[2].Rows[i]["chhp"],
                        ChDamage = (UInt16)ds.Tables[2].Rows[i]["chdamage"],
                        ChArmor = (UInt16)ds.Tables[2].Rows[i]["charmor"],
                        ChSpd = (UInt16)ds.Tables[2].Rows[i]["chspd"]
                    };
                }
                info.OwnedCharacters = chs;
            }
            PlayerInfos.TryAdd(id, info);
            return info;
        }
        catch (Exception e)
        {
            Debug.Log(e);   
            throw;
        }
    }

    public async Task LogOutAsync(string id)
    {
        await SaveDataAsync(id, PlayerInfos[id]);
        PlayerInfos.Remove(id);
    }
    public async Task SaveDataAsync(string id, stPlayerInfo player)
    {
        try
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = _connection;
            cmd.CommandText = $"UPDATE playerinfo SET charactercount = {player.CharacterCount} WHERE id = '{id}';";
            await cmd.ExecuteNonQueryAsync();
            for (int i = 0; i < Constants.MainCharacterCount; i++)
            {
                cmd.CommandText = $"UPDATE maincharacters SET chid = {player.MainCharacters[i].ChID}, chlevel = {player.MainCharacters[i].ChLevel}," +
                                  $"chhp = {player.MainCharacters[i].ChHp}, chdamage = {player.MainCharacters[i].ChDamage}, charmor = {player.MainCharacters[i].ChArmor}" +
                                  $", chspd = {player.MainCharacters[i].ChSpd} " +
                                  $"WHERE id = '{id}' AND indexnum = {i};";
                await cmd.ExecuteNonQueryAsync();

            }

            cmd.CommandText = $"UPDATE characters SET deletion = TRUE WHERE id = '{id}';";
            await cmd.ExecuteNonQueryAsync();

            for (int i = 0; i < player.CharacterCount; i++)
            {
                cmd.CommandText = $"UPDATE characters SET chid = {player.MainCharacters[i].ChID}, chlevel = {player.MainCharacters[i].ChLevel}," +
                                  $"chhp = {player.MainCharacters[i].ChHp}, chdamage = {player.MainCharacters[i].ChDamage}, charmor = {player.MainCharacters[i].ChArmor}" +
                                  $", chspd = {player.MainCharacters[i].ChSpd}, deletion = FALSE " +
                                  $"WHERE id = '{id}' AND indexnum = {i};";
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    cmd.CommandText = $"INSERT INTO characters (id, indexnum, chid, chlevel, chhp, chdamage, charmor, chspd) " +
                                      $"VALUES ('{id}', {i}, {player.MainCharacters[i].ChID}, {player.MainCharacters[i].ChLevel}, " +
                                      $"{player.MainCharacters[i].ChHp}, {player.MainCharacters[i].ChDamage}, {player.MainCharacters[i].ChArmor}, " +
                                      $"{player.MainCharacters[i].ChSpd});";
                    cmd.ExecuteNonQuery();
                }
            }
            cmd.CommandText = $"DELETE FROM characters WHERE id = '{id}' AND deletion = TRUE;";
            await cmd.ExecuteNonQueryAsync();
            PlayerInfos[id] = player;
        }
        catch (MySqlException e)
        {
            Debug.Log(e);
            throw;
        }
    }


    
}
