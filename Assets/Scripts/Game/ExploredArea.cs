using System;
using System.Text;
using UnityEngine;

namespace WordRPG.Game
{
    // 지역 하나의 '가 본 칸' (탐험 안개). 칸마다 한 비트, 세이브에는 16진수 문자열로 들어간다.
    // 맵 크기가 바뀌면 (x, y)가 그대로 남아 있는 칸만 옮겨 담는다 — 맵을 고쳐도 탐험 기록이 통째로 날아가지 않게
    [Serializable]
    public class ExploredArea
    {
        [SerializeField] private string areaId;
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private string cells = "";

        [NonSerialized] private bool[] decoded;

        public string AreaId => areaId;

        private ExploredArea() { } // Unity 직렬화용

        public ExploredArea(string areaId, int width, int height)
        {
            this.areaId = areaId;
            this.width = width;
            this.height = height;
            decoded = new bool[width * height];
            Encode();
        }

        public bool IsExplored(Vector2Int cell)
        {
            var bits = Bits();
            return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height && bits[cell.y * width + cell.x];
        }

        public int ExploredCount()
        {
            int n = 0;
            foreach (bool b in Bits()) if (b) n++;
            return n;
        }

        // center 둘레 radius 칸(원 모양)을 밝힌다. 새로 밝힌 칸 수를 돌려준다
        public int Reveal(int mapWidth, int mapHeight, Vector2Int center, int radius)
        {
            Resize(mapWidth, mapHeight);
            var bits = Bits();
            float limit = (radius + 0.5f) * (radius + 0.5f);
            int added = 0;
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > limit) continue;
                int x = center.x + dx, y = center.y + dy;
                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                int i = y * width + x;
                if (bits[i]) continue;
                bits[i] = true;
                added++;
            }
            if (added > 0) Encode();
            return added;
        }

        private void Resize(int newWidth, int newHeight)
        {
            if (newWidth == width && newHeight == height) return;
            var old = Bits();
            var resized = new bool[newWidth * newHeight];
            for (int y = 0; y < Math.Min(height, newHeight); y++)
            for (int x = 0; x < Math.Min(width, newWidth); x++)
                resized[y * newWidth + x] = old[y * width + x];
            width = newWidth;
            height = newHeight;
            decoded = resized;
            Encode();
        }

        private bool[] Bits()
        {
            if (decoded != null && decoded.Length == width * height) return decoded;
            decoded = new bool[Math.Max(0, width * height)];
            for (int i = 0; i < decoded.Length; i++)
            {
                int hex = i / 4;
                if (cells == null || hex >= cells.Length) break;
                int value = Convert.ToInt32(cells[hex].ToString(), 16);
                decoded[i] = (value & (1 << (i % 4))) != 0;
            }
            return decoded;
        }

        private void Encode()
        {
            var bits = decoded;
            var text = new StringBuilder((bits.Length + 3) / 4);
            for (int i = 0; i < bits.Length; i += 4)
            {
                int value = 0;
                for (int b = 0; b < 4 && i + b < bits.Length; b++)
                    if (bits[i + b]) value |= 1 << b;
                text.Append(value.ToString("x"));
            }
            cells = text.ToString();
        }
    }
}
