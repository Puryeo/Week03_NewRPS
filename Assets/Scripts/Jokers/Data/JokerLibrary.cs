using System.Collections.Generic;
using UnityEngine;
using Jokers;

[CreateAssetMenu(fileName = "JokerLibrary", menuName = "NewRPS/Joker Library", order = 10)]
public class JokerLibrary : ScriptableObject
{
    [Tooltip("프로토/테스트 시 사용되는 조커 목록 풀")]
    public List<JokerData> jokers = new List<JokerData>();

    [System.Serializable]
    public struct OfferConfig
    {
        public int offerCount;   // 제안 개수(예: 10)
        public int pickCount;    // 선택 개수(예: 5)
        public int minAnchor;    // 앵커 최소
        public int minPayoff;    // 페이오프 최소
        public int minUtility;   // 유틸 최소
        public int maxCatalyst;  // 촉매 최대
        public bool allowDuplicate; // 중복 허용 여부(기본 false 권장)
        public int seed;         // 재현용 시드
    }

    public List<JokerData> PickOffer(OfferConfig cfg)
    {
        var rng = new System.Random(cfg.seed);
        var anchor = new List<JokerData>();
        var payoff = new List<JokerData>();
        var catalyst = new List<JokerData>();
        var utility = new List<JokerData>();

        for (int i = 0; i < jokers.Count; i++)
        {
            var d = jokers[i]; if (d == null) continue;
            var a = d.archetypes;
            if ((a & JokerArchetype.Anchor) != 0) anchor.Add(d);
            if ((a & JokerArchetype.Payoff) != 0) payoff.Add(d);
            if ((a & JokerArchetype.Catalyst) != 0) catalyst.Add(d);
            if ((a & JokerArchetype.Utility) != 0) utility.Add(d);
        }

        var result = new List<JokerData>(cfg.offerCount);
        var used = new HashSet<JokerData>();
        // 로컬 함수: 가중치 샘플링
        JokerData WeightedPick(List<JokerData> list)
        {
            if (list == null || list.Count == 0) return null;
            int total = 0; for (int i = 0; i < list.Count; i++) total += Mathf.Max(1, list[i].weight);
            int roll = rng.Next(0, Mathf.Max(1, total));
            int acc = 0;
            for (int i = 0; i < list.Count; i++)
            {
                acc += Mathf.Max(1, list[i].weight);
                if (roll < acc) return list[i];
            }
            return list[list.Count - 1];
        }
        void TryAddFrom(List<JokerData> pool)
        {
            int guard = 0;
            while (result.Count < cfg.offerCount && pool != null && pool.Count > 0 && guard++ < 100)
            {
                var pick = WeightedPick(pool);
                if (pick == null) break;
                if (!cfg.allowDuplicate && used.Contains(pick)) continue;
                result.Add(pick);
                used.Add(pick);
            }
        }

        // 1) 쿼터 충족
        for (int i = 0; i < cfg.minAnchor && result.Count < cfg.offerCount; i++) TryAddFrom(anchor);
        for (int i = 0; i < cfg.minPayoff && result.Count < cfg.offerCount; i++) TryAddFrom(payoff);
        for (int i = 0; i < cfg.minUtility && result.Count < cfg.offerCount; i++) TryAddFrom(utility);

        // 2) 촉매제 최대치 제한을 고려하며 잔여 채우기
        int catalystCount = 0;
        for (int i = 0; i < result.Count; i++)
        {
            if ((result[i].archetypes & JokerArchetype.Catalyst) != 0) catalystCount++;
        }

        // 전체 풀(가중치 샘플)에서 부족분 채우기
        int guardAll = 0;
        while (result.Count < cfg.offerCount && guardAll++ < 500)
        {
            // 카테고리 균형을 위해 간단 라운드로빈 후보 선택
            int mod = result.Count % 4;
            List<JokerData> pool = mod == 0 ? anchor : (mod == 1 ? payoff : (mod == 2 ? utility : catalyst));
            var pick = WeightedPick(pool);
            if (pick == null) break;
            if (!cfg.allowDuplicate && used.Contains(pick)) continue;
            bool isCatalyst = (pick.archetypes & JokerArchetype.Catalyst) != 0;
            if (isCatalyst && catalystCount >= Mathf.Max(0, cfg.maxCatalyst)) continue;
            result.Add(pick);
            used.Add(pick);
            if (isCatalyst) catalystCount++;
        }

        return result;
    }
}