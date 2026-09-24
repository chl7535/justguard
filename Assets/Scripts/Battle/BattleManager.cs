using System;
using System.Collections;
using UnityEngine;

// 전투 흐름을 관리하는 상태 머신.
// RoundStart → EnemyTurn → PlayerTurn → EnemyTurn … 을 반복하다가
// 플레이어나 적의 체력이 0이 되면 Result로 넘어간다.
public class BattleManager : MonoBehaviour
{
    // 적 공격 하나에 대해 플레이어가 정한 대응
    private enum Defense
    {
        None,         // 아무것도 안 함
        ParrySuccess, // 패링 타이밍이 맞음
        ParryFail,    // 패링을 눌렀지만 타이밍이 벗어남
        Guard         // 가드함
    }

    [Header("설정 에셋")]
    [Tooltip("전투 밸런스 수치 에셋")]
    [SerializeField] private BattleConfig config;

    [Tooltip("적이 적 턴마다 사용할 공격 패턴 에셋")]
    [SerializeField] private EnemyPattern enemyPattern;

    [Header("입력")]
    [Tooltip("패링/가드 입력을 받는 컴포넌트")]
    [SerializeField] private BattleInput battleInput;

    // 적 공격 하나의 최종 결과 (UI 표시용)
    public enum AttackOutcome
    {
        ParrySuccess,
        ParryFail, // 패링을 눌렀지만 타이밍이 벗어남
        NoInput,   // 아무것도 안 눌러서 맞음
        Guard
    }

    // UI 등이 전투 상황을 알 수 있도록 알려주는 이벤트
    public event Action<BattleState> StateChanged;
    public event Action<int, int, float, float> AttackStarted; // 공격 번호(0부터), 공격 수, 예고 시작 시각, 명중 시각
    public event Action<AttackOutcome, int> AttackResolved;    // 결과, 플레이어가 받은 피해
    public event Action GuardUnavailable;                      // 가드 게이지가 없어 가드 입력이 무시됨
    public event Action<int, int> CounterAttacked;             // 반격에 쓴 스택 수, 데미지

    // 현재 전투 상태와 수치. 다른 스크립트(UI 등)는 읽기만 할 수 있다.
    public BattleConfig Config => config;
    public BattleState CurrentState { get; private set; }
    public int PlayerHealth { get; private set; }
    public int EnemyHealth { get; private set; }
    public int GuardGauge { get; private set; }
    public int ParryStack { get; private set; }

    // 지금 진행 중인 적 공격의 판정 정보
    private bool isAcceptingInput; // 적 공격이 날아오는 중이라 입력을 받는지
    private float currentHitTime;  // 현재 공격의 명중 시각 (Time.time 기준)
    private float parryInputTime;  // 패링을 누른 시각
    private Defense currentDefense;

    private void OnEnable()
    {
        if (battleInput != null)
        {
            battleInput.ParryPressed += OnParryInput;
            battleInput.GuardPressed += OnGuardInput;
        }
    }

    private void OnDisable()
    {
        if (battleInput != null)
        {
            battleInput.ParryPressed -= OnParryInput;
            battleInput.GuardPressed -= OnGuardInput;
        }
    }

    private void Start()
    {
        if (config == null || enemyPattern == null || battleInput == null)
        {
            Debug.LogError("[전투] BattleConfig, EnemyPattern, BattleInput 중 연결되지 않은 것이 있습니다. 인스펙터에서 연결해 주세요.");
            enabled = false;
            return;
        }

        StartBattle();
    }

    // 전투를 처음부터 다시 시작한다 (결과 화면의 재시작 버튼에서 호출)
    public void RestartBattle()
    {
        StopAllCoroutines();
        isAcceptingInput = false;
        StartBattle();
    }

    private void StartBattle()
    {
        PlayerHealth = config.playerMaxHealth;
        EnemyHealth = config.enemyMaxHealth;
        ChangeState(BattleState.RoundStart);
    }

    // 상태를 바꾸고, 그 상태에서 할 일을 시작한다
    private void ChangeState(BattleState next)
    {
        CurrentState = next;

        // 가드 게이지는 라운드 시작 때만 채우고, 라운드가 끝날 때까지 회복하지 않는다
        if (next == BattleState.RoundStart)
        {
            GuardGauge = config.guardGaugeMax;
            ParryStack = 0;
        }

        Debug.Log($"[전투] ===== 상태: {next} ===== " +
                  $"체력 {PlayerHealth}/{config.playerMaxHealth}, " +
                  $"가드 게이지 {GuardGauge}/{config.guardGaugeMax}, " +
                  $"패링 스택 {ParryStack}/{config.maxParryStack}, " +
                  $"적 체력 {EnemyHealth}/{config.enemyMaxHealth}");

        StateChanged?.Invoke(next);

        switch (next)
        {
            case BattleState.RoundStart:
                StartCoroutine(RoundStartRoutine());
                break;
            case BattleState.EnemyTurn:
                StartCoroutine(EnemyTurnRoutine());
                break;
            case BattleState.PlayerTurn:
                StartCoroutine(PlayerTurnRoutine());
                break;
            case BattleState.Result:
                ShowResult();
                break;
        }
    }

    private IEnumerator RoundStartRoutine()
    {
        yield return new WaitForSeconds(config.turnDelay);
        ChangeState(BattleState.EnemyTurn);
    }

    // 패턴의 공격을 순서대로 진행한다. 도중에 플레이어가 쓰러지면 바로 Result로 간다.
    private IEnumerator EnemyTurnRoutine()
    {
        if (enemyPattern.attacks.Count == 0)
        {
            Debug.LogWarning("[적 턴] 패턴에 공격이 하나도 없습니다.");
        }

        for (int i = 0; i < enemyPattern.attacks.Count; i++)
        {
            EnemyAttack attack = enemyPattern.attacks[i];

            // 새 공격이 시작되면 입력을 받기 시작한다. 한 공격에는 첫 입력 하나만 인정한다.
            float telegraphStartTime = Time.time;
            currentHitTime = telegraphStartTime + attack.telegraphTime + attack.startupTime;
            currentDefense = Defense.None;
            isAcceptingInput = true;

            float parryOpenTime = currentHitTime - config.parryWindow - config.parryInputBuffer;
            Debug.Log($"[적 턴] ▶ {i + 1}번째 공격 예고 시작 (t={telegraphStartTime:0.00}) " +
                      $"→ 명중 예정 t={currentHitTime:0.00}, 패링 인정 구간 t={parryOpenTime:0.00}~{currentHitTime:0.00}");
            AttackStarted?.Invoke(i, enemyPattern.attacks.Count, telegraphStartTime, currentHitTime);

            yield return WaitUntilTime(telegraphStartTime + attack.telegraphTime);
            Debug.Log($"[적 턴] {i + 1}번째 공격 발동! (t={Time.time:0.00})");

            yield return WaitUntilTime(currentHitTime);
            isAcceptingInput = false;
            Debug.Log($"[적 턴] ■ {i + 1}번째 공격 명중 시점 (t={Time.time:0.00})");

            ResolveAttack(attack);

            if (PlayerHealth <= 0)
            {
                ChangeState(BattleState.Result);
                yield break;
            }
        }

        yield return new WaitForSeconds(config.turnDelay);
        ChangeState(BattleState.PlayerTurn);
    }

    // 지정한 시각(Time.time 기준)이 될 때까지 기다린다
    private IEnumerator WaitUntilTime(float time)
    {
        while (Time.time < time)
        {
            yield return null;
        }
    }

    // 패링 입력. 판정은 누른 시각을 기록해 두었다가 명중 시점에 한다.
    // (나중에 화면 버튼 UI에서도 호출할 수 있게 public으로 둔다)
    public void OnParryInput()
    {
        if (!isAcceptingInput)
        {
            Debug.Log("[입력] 패링 - 지금은 적 공격 중이 아니라 무시합니다.");
            return;
        }

        if (currentDefense != Defense.None)
        {
            return; // 이 공격에는 이미 대응했다 (연타 방지)
        }

        parryInputTime = Time.time;
        float timeBeforeHit = currentHitTime - parryInputTime;

        // 명중 전 parryWindow 안이면 정상 판정, 그보다 parryInputBuffer만큼 더 이른 것까지는 선입력으로 인정
        if (timeBeforeHit >= 0f && timeBeforeHit <= config.parryWindow + config.parryInputBuffer)
        {
            currentDefense = Defense.ParrySuccess;
        }
        else
        {
            currentDefense = Defense.ParryFail;
        }

        Debug.Log($"[입력] 패링 입력 (t={parryInputTime:0.00}, 명중 {timeBeforeHit:0.000}초 전)");
    }

    // 가드 입력. 게이지가 남아 있을 때만 쓸 수 있고, 누르는 즉시 게이지를 소모한다.
    public void OnGuardInput()
    {
        if (!isAcceptingInput)
        {
            Debug.Log("[입력] 가드 - 지금은 적 공격 중이 아니라 무시합니다.");
            return;
        }

        if (currentDefense != Defense.None)
        {
            return; // 이 공격에는 이미 대응했다
        }

        if (GuardGauge <= 0)
        {
            Debug.Log("[입력] 가드 게이지 없음 - 가드 입력을 무시합니다.");
            GuardUnavailable?.Invoke();
            return;
        }

        GuardGauge--;
        currentDefense = Defense.Guard;
        Debug.Log($"[입력] 가드 입력 (t={Time.time:0.00}), 남은 가드 게이지 {GuardGauge}/{config.guardGaugeMax}");
    }

    // 공격이 명중하는 순간, 플레이어가 정한 대응에 따라 결과를 처리한다
    private void ResolveAttack(EnemyAttack attack)
    {
        switch (currentDefense)
        {
            case Defense.ParrySuccess:
                float timeBeforeHit = currentHitTime - parryInputTime;
                string timing = timeBeforeHit <= config.parryWindow ? "판정 시간 안" : "선입력 허용";
                ParryStack = Mathf.Min(ParryStack + 1, config.maxParryStack);
                Debug.Log($"[판정] 패링 성공! ({timing}, 명중 {timeBeforeHit:0.000}초 전) " +
                          $"피해 0, 패링 스택 {ParryStack}/{config.maxParryStack}");
                AttackResolved?.Invoke(AttackOutcome.ParrySuccess, 0);
                break;

            case Defense.Guard:
                int guardedDamage = Mathf.RoundToInt(attack.damage * (1f - config.guardDamageReduction));
                PlayerHealth = Mathf.Max(0, PlayerHealth - guardedDamage);
                Debug.Log($"[판정] 가드 성공! 받은 피해 {guardedDamage}, 체력 {PlayerHealth}/{config.playerMaxHealth}");
                AttackResolved?.Invoke(AttackOutcome.Guard, guardedDamage);
                break;

            case Defense.ParryFail:
                TakeHit(attack, AttackOutcome.ParryFail,
                        $"패링 실패 - 타이밍이 벗어남 (명중 {currentHitTime - parryInputTime:0.000}초 전 입력, " +
                        $"인정 범위 {config.parryWindow + config.parryInputBuffer:0.000}초 이내)");
                break;

            default:
                TakeHit(attack, AttackOutcome.NoInput, "패링 실패 - 입력 없음");
                break;
        }
    }

    private void TakeHit(EnemyAttack attack, AttackOutcome outcome, string reason)
    {
        PlayerHealth = Mathf.Max(0, PlayerHealth - attack.damage);
        Debug.Log($"[판정] {reason}. 받은 피해 {attack.damage}, 체력 {PlayerHealth}/{config.playerMaxHealth}");
        AttackResolved?.Invoke(outcome, attack.damage);
    }

    // 쌓인 스택만큼 반격하고 스택을 0으로 되돌린다
    private IEnumerator PlayerTurnRoutine()
    {
        if (ParryStack > 0)
        {
            int damage = GetStackDamage(ParryStack);
            EnemyHealth = Mathf.Max(0, EnemyHealth - damage);
            Debug.Log($"[내 턴] 패링 스택 {ParryStack}개로 반격! 데미지 {damage}, 적 체력 {EnemyHealth}/{config.enemyMaxHealth}");
            CounterAttacked?.Invoke(ParryStack, damage);
        }
        else
        {
            Debug.Log("[내 턴] 쌓인 패링 스택이 없어 반격하지 못했습니다.");
            CounterAttacked?.Invoke(0, 0);
        }

        ParryStack = 0;

        yield return new WaitForSeconds(config.turnDelay);
        ChangeState(EnemyHealth <= 0 ? BattleState.Result : BattleState.EnemyTurn);
    }

    // 스택 수에 맞는 반격 데미지를 BattleConfig.stackDamage에서 찾는다
    private int GetStackDamage(int stack)
    {
        if (config.stackDamage == null || config.stackDamage.Length == 0)
        {
            Debug.LogWarning("[전투] BattleConfig의 stackDamage가 비어 있어 반격 데미지가 0입니다.");
            return 0;
        }

        // 배열 칸이 모자라면 마지막 칸의 데미지를 쓴다
        if (stack > config.stackDamage.Length)
        {
            Debug.LogWarning($"[전투] stackDamage에 {stack}스택 데미지가 없어 마지막 칸 값을 사용합니다.");
            stack = config.stackDamage.Length;
        }

        // 배열은 0번부터 시작하므로 1스택 = 0번 칸
        return config.stackDamage[stack - 1];
    }

    private void ShowResult()
    {
        if (PlayerHealth <= 0)
        {
            Debug.Log("[결과] 패배... 플레이어가 쓰러졌습니다.");
        }
        else
        {
            Debug.Log("[결과] 승리! 적을 쓰러뜨렸습니다.");
        }
    }
}
