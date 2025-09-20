using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jokers;

namespace NewRPS.UI
{
    public class JokerPipelineItem : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public Button upButton;
        public Button downButton;

        private int _index;
        private DraftFlowController _flow;

        public void Init(JokerData data, int index, DraftFlowController flow)
        {
            _index = index; _flow = flow;
            if (titleText != null) titleText.text = string.IsNullOrEmpty(data.jokerName) ? data.name : data.jokerName;
            if (upButton != null)
            {
                upButton.onClick.RemoveAllListeners();
                upButton.onClick.AddListener(() => _flow.MovePipelineItem(_index, -1));
            }
            if (downButton != null)
            {
                downButton.onClick.RemoveAllListeners();
                downButton.onClick.AddListener(() => _flow.MovePipelineItem(_index, +1));
            }
        }
    }
}
