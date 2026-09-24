using UnityEngine;

// 전투 밸런스 수치를 모아두는 설정 에셋.
// 코드를 고치지 않고 인스펙터에서 값을 바꿔 밸런스를 조정한다.
[CreateAssetMenu(fileName = "BattleConfig", menuName = "JustGuard/Battle Config")]
public class BattleConfig : ScriptableObject
{
    [Header("패링")]
    [Tooltip("패링 판정 시간(초). 적 공격이 닿기 전 이 시간 안에 패링을 누르면 성공한다.")]
    [Min(0f)]
    public float parryWindow = 0.2f;

    [Tooltip("선입력 허용 시간(초). 판정 시간보다 이만큼 일찍 누른 입력도 패링으로 인정한다.")]
    [Min(0f)]
    public float parryInputBuffer = 0.1f;

    [Tooltip("패링 성공으로 쌓을 수 있는 최대 스택 수. 기본 1, 개발 테스트용으로 3까지 설정 가능.")]
    [Range(1, 3)]
    public int maxParryStack = 1;

    [Header("플레이어")]
    [Tooltip("플레이어 최대 체력. 적 공격 1대에 1씩 줄어든다.")]
    [Min(1)]
    public int playerMaxHealth = 2;

    [Header("적")]
    [Tooltip("적 최대 체력. 반격 데미지로 0이 되면 플레이어가 승리한다.")]
    [Min(1)]
    public int enemyMaxHealth = 100;

    [Header("가드")]
    [Tooltip("라운드당 가드 사용 가능 횟수. 소모하면 라운드가 끝날 때까지 회복되지 않는다.")]
    [Min(0)]
    public int guardGaugeMax = 1;

    [Tooltip("가드 시 피해 감소 비율. 1.0 = 완전 방어, 0.5 = 절반만 막음.")]
    [Range(0f, 1f)]
    public float guardDamageReduction = 1.0f;

    [Header("반격")]
    [Tooltip("스택 수별 반격 데미지. 0번 칸 = 1스택, 1번 칸 = 2스택, 2번 칸 = 3스택.")]
    public int[] stackDamage = { 10, 25, 50 };

    [Header("진행")]
    [Tooltip("상태가 바뀔 때 다음 상태로 넘어가기 전 대기 시간(초).")]
    [Min(0f)]
    public float turnDelay = 1.0f;
}
