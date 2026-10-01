using System;
using UnityEngine;

namespace WordRPG.Field
{
    public enum StepKind
    {
        Moved,
        Blocked,    // 나무·물·맵 끝 — 방향만 바뀜
        Interacted  // 상자·샘·제단·상점에 부딪힘 — TargetTile로 무엇인지 구분
    }

    public readonly struct StepOutcome
    {
        public StepKind Kind { get; }
        public Vector2Int Target { get; }
        public FieldTile TargetTile { get; }

        public bool EnteredGrass => Kind == StepKind.Moved && TargetTile == FieldTile.Grass;
        public bool EnteredDoor => Kind == StepKind.Moved && TargetTile == FieldTile.Door;

        public StepOutcome(StepKind kind, Vector2Int target, FieldTile targetTile)
        {
            Kind = kind;
            Target = target;
            TargetTile = targetTile;
        }
    }

    // 격자 위 한 칸씩 이동 (포켓몬식). 상호작용은 '부딪히기'로 — 모바일에서 A버튼 없이 상자를 연다
    public class FieldWalker
    {
        public FieldMap Map { get; }
        public Vector2Int Position { get; private set; }
        public Direction Facing { get; private set; } = Direction.Down;

        public FieldWalker(FieldMap map, Vector2Int position)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            WarpTo(position);
        }

        public StepOutcome TryStep(Direction direction)
        {
            Facing = direction;
            var target = Position + direction.ToOffset();
            var tile = Map.Get(target);

            switch (tile)
            {
                case FieldTile.Floor:
                case FieldTile.Grass:
                case FieldTile.Door:
                    Position = target;
                    return new StepOutcome(StepKind.Moved, target, tile);
                default:
                    return new StepOutcome(FieldMap.IsInteractive(tile) ? StepKind.Interacted : StepKind.Blocked, target, tile);
            }
        }

        public void WarpTo(Vector2Int position)
        {
            if (!Map.IsWalkable(position))
                throw new ArgumentException($"{position}은(는) 걸을 수 없는 칸입니다", nameof(position));
            Position = position;
        }
    }

    // 풀숲 조우 판정. 직전 조우 후 최소 걸음 수가 지나야 확률 판정을 시작해 연속 조우로 지치지 않게 한다
    public class EncounterCounter
    {
        private readonly float rate;
        private readonly int minStepsBetween;

        public int StepsSinceLast { get; private set; }

        public EncounterCounter(float rate, int minStepsBetween)
        {
            this.rate = rate;
            this.minStepsBetween = minStepsBetween;
        }

        public bool OnStep(bool onGrass, System.Random rng)
        {
            if (!onGrass) return false;
            StepsSinceLast++;
            if (StepsSinceLast <= minStepsBetween) return false;
            if (rng.NextDouble() >= rate) return false;
            StepsSinceLast = 0;
            return true;
        }

        public void Reset() => StepsSinceLast = 0;
    }
}
