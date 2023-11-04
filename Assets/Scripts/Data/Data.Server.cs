using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using JetBrains.Annotations;
using System.Runtime.Serialization;
using Data;

namespace Data
{
    public static class Constants
    {
        public const int HeaderIdSize = 4;
        public const int HeaderSize = HeaderIdSize + 2;
        public const int MaxNameByte = 10;
        public const int MaxPasswordByte = 10;
        public const int BufferSize = 2048;
        public const int TempBufferSize = 1048;
        public const int MainCharacterCount = 3;
        public const int MaxCharacterCount = 50;
        public const UInt16 PlayerRegisterGold = 1500;
        public const UInt16 PlayerRegisterToken = 2000;

    }
    public static class MessageID // 헤더 메시지 정보
    {
        public const ushort Header = 0x01;
        public const ushort LoginRegister = 0x02;
        public const ushort PlayerInfo = 0x03;
        public const ushort MatchPlayerInfo = 0x04;
        public const ushort BattleRoomInfo = 0x05;
        public const ushort BattleReady = 0x06;
        public const ushort BattleStart = 0x07;
        public const ushort BattlePlayerOrderInfo = 0x08;
        public const ushort BattleOrdersInfo = 0x09;
        public const ushort BattleParticularInfo = 0x10;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stHeader // 헤더 메시지 (모든 통신 구조체 앞 패킷에 구성) 
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stLoginRegister // 회원가입 및 로그인
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)] // 로그인 or 회원가입
        public bool IsLogin;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.MaxNameByte)]
        public string ID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.MaxPasswordByte)]
        public string Password;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)] // 로그인 성공 여부 ( 서버에서 반환 )
        public bool Succeed;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stPlayerInfo // 플레이어 정보
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)(Constants.MaxNameByte))]
        public string ID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.MaxPasswordByte)]
        public string Password;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 GoldAmount;
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public UInt32 TokenCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MainCharacterCount)] // 대표 캐릭터
        public stCharacterInfo[] MainCharacters;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 CharacterCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MaxCharacterCount)] // 보유 캐릭터
        public stCharacterInfo[] OwnedCharacters;
        public stPlayerInfo(string id, string password) // 회원가입 시 Id, Password를 바탕으로 정보생성
        {
            MsgID = MessageID.PlayerInfo;
            PacketSize = (ushort)Marshal.SizeOf(typeof(stPlayerInfo));
            ID = id;
            Password = password;
            GoldAmount = Constants.PlayerRegisterGold;
            TokenCount = Constants.PlayerRegisterToken;
            CharacterCount = 0;
            MainCharacters = new stCharacterInfo[Constants.MainCharacterCount];
            OwnedCharacters = new stCharacterInfo[CharacterCount];
        }
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stCharacterInfo // 캐릭터 정보
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ChID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ChLevel;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ChHp;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ChDamage;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ChArmor;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ChSpd;

    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stMatchPlayerInfo // 매치 시작 및 나의 아이디
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)(Constants.MaxNameByte))]
        public string ID;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)]
        public bool Matching;
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleRoomInfo // 배틀 정보
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 RoomID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)(Constants.MaxNameByte))]
        public string ID;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)]
        public bool IsPlayer1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MainCharacterCount)]
        public stCharacterInfo[] PlayerCharacters;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MainCharacterCount)]
        public stCharacterInfo[] OtherCharacters;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.MainCharacterCount * 2)]
        public stBattleTurnInfo[] BattleTurn;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleTurnInfo // 캐릭터 순서
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 CharacterID;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)]
        public bool IsPlayer1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleReady // 플레이어가 씬을 로드했는지 (준비 완료)
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)(Constants.MaxNameByte))]
        public string ID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 RoomID;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)]
        public bool IsReady;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleStart // 준비 완료 (캐릭터 행동 선택 가능)
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)]
        public bool Start;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattlePlayerOrder // 플레이어 캐릭터 행동 정보
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)(Constants.MaxNameByte))]
        public string ID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 RoomID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public stBattleMyCharacterOrder[] CharactersOrder;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleMyCharacterOrder // 캐릭터 행동 지정
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 CharacterIndex; // 내 캐릭터 정보
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 State; // 0 - 죽음, 1 - 고르지 않음, 2 - 방어, 3 - 공격
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 TargetIndex; // 공격을 선택 했을시 타겟

    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleOrdersInfo // 전투 정보 (한 턴)
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public stBattleCharacterOrder[] Order;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 GameState; // 0 - 계속 진행, 1 - 플레이어 1 승리, 2 - 플레이어 2 승리

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleCharacterOrder // 전투 정보 (캐릭터)
    {
        [MarshalAs(UnmanagedType.Bool, SizeConst = 1)]
        public bool IsMyCharacter;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 CharacterIndex; // 공격을 선택 했을시
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 State; // 0 - 죽음, 1 - 고르지 않음, 2 - 방어, 3 - 공격
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 TargetIndex; // 공격을 선택 했을시
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stBattleParticularInfo // 전투 특이사항
    {
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 MsgID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 PacketSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)(Constants.MaxNameByte))]
        public string ID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 RoomID;
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public UInt16 ParticularInfo; // 0 - 항복, 1 - 나감
    }
    public struct GamePlayerInfo
    {
        public string ID;
        public stCharacterInfo[] MainCharacters;

    }
}