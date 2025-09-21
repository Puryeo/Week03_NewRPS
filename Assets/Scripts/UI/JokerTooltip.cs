using TMPro;
using UnityEngine;

namespace NewRPS.UI
{
    // Simple tooltip panel that expects a TextMeshProUGUI child; created/destroyed by JokerIconUI
    public class JokerTooltip : MonoBehaviour
    {
        public TextMeshProUGUI text;

        private void Reset()
        {
            if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
