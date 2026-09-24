using UnityEngine;

// 화면의 패링/가드 버튼에 붙여서 "이 버튼은 어떤 행동인지" 표시하는 컴포넌트.
// 실제 입력 처리는 BattleInput이 한다. 버튼 onClick은 손을 뗄 때 호출되어 타이밍이 늦기 때문에,
// BattleInput이 누르는 순간 이 버튼 위인지 확인해서 바로 처리한다.
public class BattleActionButton : MonoBehaviour
{
    public enum ActionType
    {
        Parry,
        Guard
    }

    [Tooltip("이 버튼을 눌렀을 때 할 행동")]
    public ActionType action;
}
