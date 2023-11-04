using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Data
{
    public class Player
    {
        public int PlayerID;
        public string PlayerName;
        public int PlayerGold;
        public int UpgradeToken;
        public int Power;
        public Character[] PlayerMainCharacters;
        public Dictionary<int, Character> PlayerCharacters;

        public Player()
        {
            PlayerID = 0;
            PlayerName = "ChangeName";
            PlayerGold = 0;
            UpgradeToken = 0;
            Power = 0;
            PlayerMainCharacters = new Character[3];
            PlayerCharacters = new Dictionary<int, Character>();
        }
    }

    public enum CharacterType
    {
        Warrior,
        Archor,
        Magician,

    }

    public class Character
    {
        public int ChID;
        public string ChName;
        public int ChLevel;
        public int ChHp;
        public int ChDamage;
        public int ChArmor;
        public int ChSpd;
        [CanBeNull] public WeaponEquipment ChEqWeapon;
        [CanBeNull] public ArmorEquipment ChEqArmor;
        [CanBeNull] public ShoeEquipment ChEqShoe;
        [CanBeNull] public HeadEquipment ChEqHead;
        [CanBeNull] public GloveEquipment ChEqGlove;

        public CharacterType ChType;
        public Character()
        {
            ChID = 0;
            ChName = string.Empty;
            ChLevel = 1;
            ChHp = 10;
            ChDamage = 10;
            ChArmor = 10;
            ChSpd = 10;
            ChType = CharacterType.Warrior;
        }

        public Character(Character ch)
        {
            ChID = ch.ChID;
            ChName = ch.ChName;
            ChLevel = ch.ChLevel;
            ChHp = ch.ChHp;
            ChDamage = ch.ChDamage;
            ChArmor = ch.ChArmor;
            ChSpd = ch.ChSpd;
            ChType = ch.ChType;
        }

    }

    public class Equipment
    {
        public int ItemID;
        public string ItemName;
    }

    public class WeaponEquipment : Equipment
    {
        public int WeaponDamage;
    }

    public class ArmorEquipment : Equipment
    {
        public int ArmorHP;
    }

    public class ShoeEquipment : Equipment
    {
        public int ShoeSpeed;
    }

    public class HeadEquipment : Equipment
    {
        public int HeadDefence;
    }

    public class GloveEquipment : Equipment
    {
        public CharacterType increaseType;
        public int DamagePer;
    }

    public enum CharacterState
    {
        Death = 0,
        None = 1,
        Defense = 2,
        Attack = 3,
        SetAttackTarget = 4
    }

    public enum GameState
    {
        ContinueSelect,
        Player1Win,
        Player2Win
    }

    public enum ParticularInfo
    {
        Surrender,
        LogOut
    }
}


