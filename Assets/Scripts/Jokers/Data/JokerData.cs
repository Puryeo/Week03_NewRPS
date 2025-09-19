using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    [CreateAssetMenu(fileName = "JokerData", menuName = "NewRPS/Joker Data", order = 1)]
    public class JokerData : ScriptableObject
    {
        public string jokerName;
        [TextArea] public string description;
        public List<JokerTag> tags = new List<JokerTag>();
    }
}
