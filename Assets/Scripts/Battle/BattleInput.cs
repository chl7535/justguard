using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// 패링/가드 입력을 받아서 이벤트로 알려주는 컴포넌트.
// PC(마우스, 키보드)와 모바일(터치)을 새 Input System으로 함께 처리한다.
//   패링: 마우스 좌클릭 / 키보드 A / 화면 왼쪽 절반 터치 / 화면의 패링 버튼
//   가드: 마우스 우클릭 / 키보드 D / 화면 오른쪽 절반 터치 / 화면의 가드 버튼
// (Input System에 이미 PlayerInput이라는 컴포넌트가 있어서 이름이 겹치지 않게 BattleInput으로 지었다)
public class BattleInput : MonoBehaviour
{
    public event Action ParryPressed;
    public event Action GuardPressed;

    private InputAction parryKeyAction;
    private InputAction guardKeyAction;
    private InputAction mouseLeftAction;
    private InputAction mouseRightAction;
    private InputAction touchAction;

    // UI 레이캐스트 결과를 담을 목록 (매번 새로 만들지 않도록 재사용)
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        parryKeyAction = new InputAction("ParryKey", InputActionType.Button, "<Keyboard>/a");
        parryKeyAction.performed += _ => ParryPressed?.Invoke();

        guardKeyAction = new InputAction("GuardKey", InputActionType.Button, "<Keyboard>/d");
        guardKeyAction.performed += _ => GuardPressed?.Invoke();

        // 마우스와 터치는 누른 위치에 UI 버튼이 있는지 먼저 확인해야 해서 따로 처리한다
        mouseLeftAction = new InputAction("MouseLeft", InputActionType.Button, "<Mouse>/leftButton");
        mouseLeftAction.performed += _ => OnPointerPressed(Mouse.current.position.ReadValue(), BattleActionButton.ActionType.Parry);

        mouseRightAction = new InputAction("MouseRight", InputActionType.Button, "<Mouse>/rightButton");
        mouseRightAction.performed += _ => OnPointerPressed(Mouse.current.position.ReadValue(), BattleActionButton.ActionType.Guard);

        // PassThrough로 모든 손가락의 누름/뗌을 받아서, 누른 순간만 골라낸다
        touchAction = new InputAction("Touch", InputActionType.PassThrough, "<Touchscreen>/touch*/press");
        touchAction.performed += OnTouch;
    }

    private void OnTouch(InputAction.CallbackContext context)
    {
        // 손가락을 뗄 때도 호출되므로 누른 순간만 처리한다
        if (!context.ReadValueAsButton())
        {
            return;
        }

        // press 컨트롤의 부모가 그 손가락의 터치 정보(위치 등)를 가진다
        if (!(context.control.parent is TouchControl touch))
        {
            return;
        }

        Vector2 position = touch.position.ReadValue();
        var defaultAction = position.x < Screen.width * 0.5f
            ? BattleActionButton.ActionType.Parry
            : BattleActionButton.ActionType.Guard;
        OnPointerPressed(position, defaultAction);
    }

    // 마우스/터치를 누른 순간의 처리.
    // 패링/가드 버튼 위면 그 버튼의 행동, 다른 UI(재시작 버튼 등) 위면 무시, UI가 없는 곳이면 기본 행동.
    private void OnPointerPressed(Vector2 screenPosition, BattleActionButton.ActionType defaultAction)
    {
        GameObject uiObject = RaycastUI(screenPosition);
        if (uiObject != null)
        {
            BattleActionButton button = uiObject.GetComponentInParent<BattleActionButton>();
            if (button == null)
            {
                return;
            }

            Invoke(button.action);
            return;
        }

        Invoke(defaultAction);
    }

    private void Invoke(BattleActionButton.ActionType action)
    {
        if (action == BattleActionButton.ActionType.Parry)
        {
            ParryPressed?.Invoke();
        }
        else
        {
            GuardPressed?.Invoke();
        }
    }

    // 화면 좌표에 있는 가장 위의 UI 오브젝트를 찾는다. 없으면 null.
    private GameObject RaycastUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return null;
        }

        var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);
        return raycastResults.Count > 0 ? raycastResults[0].gameObject : null;
    }

    private void OnEnable()
    {
        parryKeyAction.Enable();
        guardKeyAction.Enable();
        mouseLeftAction.Enable();
        mouseRightAction.Enable();
        touchAction.Enable();
    }

    private void OnDisable()
    {
        parryKeyAction.Disable();
        guardKeyAction.Disable();
        mouseLeftAction.Disable();
        mouseRightAction.Disable();
        touchAction.Disable();
    }

    private void OnDestroy()
    {
        parryKeyAction.Dispose();
        guardKeyAction.Dispose();
        mouseLeftAction.Dispose();
        mouseRightAction.Dispose();
        touchAction.Dispose();
    }
}
