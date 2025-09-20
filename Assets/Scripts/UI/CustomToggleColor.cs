using UnityEngine;
using UnityEngine.UI;

public class ToggleColorController : MonoBehaviour
{
    private Toggle toggle;
    private Image targetImage;

    // 인스펙터에서 설정할 색상
    public Color selectedColor;
    public Color normalColor;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        // 토글의 Graphic 속성에 할당된 Image 컴포넌트를 가져옵니다.
        targetImage = toggle.targetGraphic as Image;

        // 토글의 상태 변화를 감지하는 리스너를 추가합니다.
        toggle.onValueChanged.AddListener(OnToggleChanged);

        // 초기 상태에 따라 색상을 설정합니다.
        OnToggleChanged(toggle.isOn);
    }

    // 토글의 상태가 변경될 때 호출되는 함수
    private void OnToggleChanged(bool isOn)
    {
        if (targetImage == null) return;

        if (isOn)
        {
            // 토글이 켜졌을 때, Selected Color를 적용합니다.
            targetImage.color = selectedColor;
        }
        else
        {
            // 토글이 꺼졌을 때, Normal Color를 적용합니다.
            targetImage.color = normalColor;
        }
    }
}