using System;
using UnityEngine;

namespace WordRPG.Game
{
    // 누적 전적. 세이브 파일에 들어간다
    [Serializable]
    public class PlayerRecord
    {
        [SerializeField] private int battlesWon;
        [SerializeField] private int battlesLost;
        [SerializeField] private int correctAnswers;
        [SerializeField] private int wrongAnswers;

        public int BattlesWon => battlesWon;
        public int BattlesLost => battlesLost;
        public int CorrectAnswers => correctAnswers;
        public int WrongAnswers => wrongAnswers;

        public void RecordBattle(bool won, int correct, int wrong)
        {
            if (won) battlesWon++;
            else battlesLost++;
            correctAnswers += correct;
            wrongAnswers += wrong;
        }
    }
}
