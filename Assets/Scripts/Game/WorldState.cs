using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordRPG.Game
{
    // 필드 진행 상태: 마지막 위치(지역+칸), 열어 본 보물상자, 쓰러뜨린 보스, 탐험한 칸(안개). 세이브 파일에 들어간다
    [Serializable]
    public class WorldState
    {
        // 걸을 때 주변이 밝혀지는 반지름 (칸)
        public const int SightRadius = 3;

        [SerializeField] private string areaId;
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private bool hasPosition;
        [SerializeField] private List<string> openedChests = new List<string>();
        [SerializeField] private List<string> defeatedBosses = new List<string>();
        [SerializeField] private List<ExploredArea> explored = new List<ExploredArea>(); // 예전 세이브에는 없음 → 처음부터 탐험

        public string AreaId => hasPosition ? areaId : null;
        public IReadOnlyList<string> OpenedChests => openedChests;

        public bool TryGetPosition(string area, out Vector2Int position)
        {
            position = new Vector2Int(x, y);
            return hasPosition && areaId == area;
        }

        public void SetPosition(string area, Vector2Int position)
        {
            areaId = area;
            x = position.x;
            y = position.y;
            hasPosition = true;
        }

        public void ClearPosition() => hasPosition = false;

        public bool IsBossDefeated(string bossId) => defeatedBosses.Contains(bossId);

        public void MarkBossDefeated(string bossId)
        {
            if (!defeatedBosses.Contains(bossId)) defeatedBosses.Add(bossId);
        }

        // 탐험 안개: center 둘레를 밝히고 새로 밝힌 칸 수를 돌려준다
        public int Reveal(string area, int mapWidth, int mapHeight, Vector2Int center, int radius = SightRadius)
        {
            var record = explored.Find(e => e.AreaId == area);
            if (record == null)
            {
                record = new ExploredArea(area, mapWidth, mapHeight);
                explored.Add(record);
            }
            return record.Reveal(mapWidth, mapHeight, center, radius);
        }

        public bool IsExplored(string area, Vector2Int cell)
        {
            var record = explored.Find(e => e.AreaId == area);
            return record != null && record.IsExplored(cell);
        }

        public bool IsChestOpened(string chestId) => openedChests.Contains(chestId);

        public void MarkChestOpened(string chestId)
        {
            if (!openedChests.Contains(chestId)) openedChests.Add(chestId);
        }
    }
}
