using System;
using UnityEngine;

namespace WordRPG.Monsters
{
    [Serializable]
    public struct MonsterStats
    {
        [SerializeField] private int maxHp;
        [SerializeField] private int attack;
        [SerializeField] private int defense;

        public int MaxHp => maxHp;
        public int Attack => attack;
        public int Defense => defense;

        public MonsterStats(int maxHp, int attack, int defense)
        {
            this.maxHp = maxHp;
            this.attack = attack;
            this.defense = defense;
        }

        public static MonsterStats operator +(MonsterStats a, MonsterStats b)
        {
            return new MonsterStats(a.maxHp + b.maxHp, a.attack + b.attack, a.defense + b.defense);
        }

        public static MonsterStats operator *(MonsterStats a, int times)
        {
            return new MonsterStats(a.maxHp * times, a.attack * times, a.defense * times);
        }

        public override string ToString() => $"HP {maxHp} / ATK {attack} / DEF {defense}";
    }
}
