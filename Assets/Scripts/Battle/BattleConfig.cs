using System;
using UnityEngine;

namespace WordRPG.Battle
{
    // 전투 밸런스 값. 플레이테스트로 조정
    [Serializable]
    public class BattleConfig
    {
        [Tooltip("이 시간 안에 못 고르면 오답 처리")]
        [SerializeField] private float answerTimeLimitSeconds = 10f;
        [Tooltip("이 시간 안에 맞히면 크리티컬")]
        [SerializeField] private float criticalTimeSeconds = 3f;
        [SerializeField] private float criticalMultiplier = 1.5f;
        [Tooltip("데미지/회복량 ±랜덤 폭 (0.1 = ±10%)")]
        [SerializeField] private float variance = 0.1f;
        [Tooltip("정답 1개당 파티 전원이 추가로 받는 경험치 — 공부가 곧 성장")]
        [SerializeField] private int expPerCorrectAnswer = 2;

        public float AnswerTimeLimitSeconds => answerTimeLimitSeconds;
        public float CriticalTimeSeconds => criticalTimeSeconds;
        public float CriticalMultiplier => criticalMultiplier;
        public float Variance => variance;
        public int ExpPerCorrectAnswer => expPerCorrectAnswer;

        public BattleConfig() { }

        public BattleConfig(float answerTimeLimitSeconds, float criticalTimeSeconds, float criticalMultiplier,
            float variance, int expPerCorrectAnswer)
        {
            this.answerTimeLimitSeconds = answerTimeLimitSeconds;
            this.criticalTimeSeconds = criticalTimeSeconds;
            this.criticalMultiplier = criticalMultiplier;
            this.variance = variance;
            this.expPerCorrectAnswer = expPerCorrectAnswer;
        }
    }
}
