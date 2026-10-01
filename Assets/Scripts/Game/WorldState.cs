using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordRPG.Game
{
    // 필드 진행 상태: 마지막 위치(지역+칸), 열어 본 보물상자, 쓰러뜨린 보스. 세이브 파일에 들어간다
    [Serializable]
    public class WorldState
    {
        [SerializeField] private string areaId;
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private bool hasPosition;
        [SerializeField] private List<string> openedChests = new List<string>();
        [SerializeField] private List<string> defeatedBosses = new List<string>();

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

        public bool IsChestOpened(string chestId) => openedChests.Contains(chestId);

        public void MarkChestOpened(string chestId)
        {
            if (!openedChests.Contains(chestId)) openedChests.Add(chestId);
        }
    }
}
