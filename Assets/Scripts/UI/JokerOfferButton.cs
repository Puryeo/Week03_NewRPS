using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jokers;

namespace NewRPS.UI
{
    public class JokerOfferButton : MonoBehaviour
    {
        public Toggle toggle;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descText;
        public Image archetypeBadge;

        private JokerData _data;
        private DraftFlowController _flow;
        private bool _suppress;

        public void Init(JokerData data, DraftFlowController flow)
        {
            _data = data; _flow = flow;
            if (titleText != null) titleText.text = string.IsNullOrEmpty(data.jokerName) ? data.name : data.jokerName;
            if (descText != null) descText.text = data.description;
            if (toggle != null)
            {
                _suppress = true;
                toggle.isOn = false;
                _suppress = false;
                toggle.onValueChanged.AddListener(OnToggle);
            }
            UpdateBadge();
        }

        private void OnDestroy()
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggle);
        }

        private void OnToggle(bool on)
        {
            if (_suppress) return;
            _flow?.OnOfferToggle(_data, on, this);
        }

        public void SetIsOn(bool on, bool silent)
        {
            if (toggle == null) return;
            if (silent)
            {
                _suppress = true;
                toggle.isOn = on;
                _suppress = false;
            }
            else toggle.isOn = on;
        }

        private void UpdateBadge()
        {
            // optional: set badge color by archetype
            if (archetypeBadge == null || _data == null) return;
            var a = _data.archetypes;
            Color c = new Color(0.8f,0.8f,0.8f,1f);
            if ((a & JokerArchetype.Anchor) != 0) c = new Color(0.4f,0.8f,1f,1f);
            else if ((a & JokerArchetype.Payoff) != 0) c = new Color(1f,0.85f,0.3f,1f);
            else if ((a & JokerArchetype.Catalyst) != 0) c = new Color(1f,0.4f,0.4f,1f);
            else if ((a & JokerArchetype.Utility) != 0) c = new Color(0.7f,0.7f,1f,1f);
            archetypeBadge.color = c;
        }
    }
}
