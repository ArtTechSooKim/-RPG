using System.Collections.Generic;
using UnityEngine;

namespace WordRPG.Words
{
    // 지역(난이도) 하나의 단어장. Assets/Data/Words/*.csv 를 임포트해서 만든다 (메뉴: WordRPG > Data > Import Word CSVs)
    [CreateAssetMenu(fileName = "NewWordDatabase", menuName = "WordRPG/Word Database", order = 0)]
    public class WordDatabase : ScriptableObject
    {
        [Tooltip("난이도 단계. 1 = 초원")]
        [SerializeField] private int tier = 1;
        [SerializeField] private List<WordEntry> words = new List<WordEntry>();

        public int Tier => tier;
        public IReadOnlyList<WordEntry> Words => words;

        // CSV 임포터 전용. 런타임에서는 호출하지 않는다
        public void ReplaceWords(IEnumerable<WordEntry> newWords)
        {
            words = new List<WordEntry>(newWords);
        }
    }
}
