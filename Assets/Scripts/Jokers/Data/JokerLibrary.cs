using System.Collections.Generic;
using UnityEngine;
using Jokers;

[CreateAssetMenu(fileName = "JokerLibrary", menuName = "NewRPS/Joker Library", order = 10)]
public class JokerLibrary : ScriptableObject
{
    [Tooltip("������/�׽�Ʈ �� ���Ǵ� ��Ŀ ��� Ǯ")]
    public List<JokerData> jokers = new List<JokerData>();

    [System.Serializable]
    public struct OfferConfig
    {
        public int offerCount;   // ���� ����(��: 10)
        public int pickCount;    // ���� ����(��: 5)
        public int minAnchor;    // ��Ŀ �ּ�
        public int minPayoff;    // ���̿��� �ּ�
        public int minUtility;   // ��ƿ �ּ�
        public int maxCatalyst;  // �˸� �ִ�
        public bool allowDuplicate; // �ߺ� ��� ����(�⺻ false ����)
        public int seed;         // ������ �õ�
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
        // ���� �Լ�: ����ġ ���ø�
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

        // 1) ���� ����
        for (int i = 0; i < cfg.minAnchor && result.Count < cfg.offerCount; i++) TryAddFrom(anchor);
        for (int i = 0; i < cfg.minPayoff && result.Count < cfg.offerCount; i++) TryAddFrom(payoff);
        for (int i = 0; i < cfg.minUtility && result.Count < cfg.offerCount; i++) TryAddFrom(utility);

        // 2) �˸��� �ִ�ġ ������ ����ϸ� �ܿ� ä���
        int catalystCount = 0;
        for (int i = 0; i < result.Count; i++)
        {
            if ((result[i].archetypes & JokerArchetype.Catalyst) != 0) catalystCount++;
        }

        // ��ü Ǯ(����ġ ����)���� ������ ä���
        int guardAll = 0;
        while (result.Count < cfg.offerCount && guardAll++ < 500)
        {
            // ī�װ� ������ ���� ���� ����κ� �ĺ� ����
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