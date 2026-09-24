using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 전투 상황을 화면에 보여주는 UI.
// 체력, 게이지, 스택처럼 계속 유지되는 값은 매 프레임 BattleManager에서 읽어 표시하고,
// 패링 성공이나 피격처럼 순간적인 일은 BattleManager의 이벤트를 받아 연출한다.
public class BattleUI : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private BattleManager battleManager;

    [Header("적")]
    [SerializeField] private UnityEngine.UI.Image enemyBody;
    [SerializeField] private RectTransform enemyHpFill;
    [SerializeField] private TMP_Text enemyHpText;

    [Header("플레이어")]
    [SerializeField] private UnityEngine.UI.Image playerBody;
    [Tooltip("체력 칸이 만들어질 부모. 칸 수는 BattleConfig.playerMaxHealth를 따른다.")]
    [SerializeField] private RectTransform playerHpContainer;

    [Header("가드 게이지 / 패링 스택")]
    [Tooltip("가드 게이지 칸이 만들어질 부모. 칸 수는 BattleConfig.guardGaugeMax를 따른다.")]
    [SerializeField] private RectTransform guardGaugeContainer;
    [Tooltip("패링 스택 칸이 만들어질 부모. 칸 수는 BattleConfig.maxParryStack을 따른다.")]
    [SerializeField] private RectTransform parryStackContainer;

    [Header("공격 타이밍 표시기")]
    [SerializeField] private GameObject timingRoot;
    [SerializeField] private RectTransform timingFill;
    [SerializeField] private RectTransform timingCursor;
    [Tooltip("선입력 허용 구간 (parryInputBuffer)")]
    [SerializeField] private RectTransform bufferZone;
    [Tooltip("패링 판정 구간 (parryWindow)")]
    [SerializeField] private RectTransform windowZone;
    [SerializeField] private TMP_Text timingLabel;

    [Header("피드백")]
    [SerializeField] private UnityEngine.UI.Image screenFlash;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text counterText;

    [Header("결과")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private UnityEngine.UI.Button restartButton;

    [Header("색상")]
    [SerializeField] private Color enemyColor = new Color(0.75f, 0.2f, 0.25f);
    [SerializeField] private Color enemyTelegraphColor = new Color(1f, 0.55f, 0.1f);
    [SerializeField] private Color playerColor = new Color(0.3f, 0.6f, 0.9f);
    [SerializeField] private Color hpCellColor = new Color(0.9f, 0.25f, 0.3f);
    [SerializeField] private Color guardCellColor = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color stackCellColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color emptyCellColor = new Color(0.3f, 0.3f, 0.33f);
    [SerializeField] private Color parryColor = new Color(0.3f, 1f, 0.9f);
    [SerializeField] private Color hitColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private Color guardColor = new Color(0.35f, 0.65f, 1f);
    [SerializeField] private Color noGaugeColor = new Color(0.6f, 0.6f, 0.6f);

    // 연출 시간(초)
    private const float FlashDuration = 0.25f;
    private const float FeedbackDuration = 0.8f;
    private const float CounterDuration = 1.3f;
    private const float BodyFlashDuration = 0.3f;
    private const float PunchDuration = 0.15f;

    private readonly List<UnityEngine.UI.Image> playerHpCells = new List<UnityEngine.UI.Image>();
    private readonly List<UnityEngine.UI.Image> guardCells = new List<UnityEngine.UI.Image>();
    private readonly List<UnityEngine.UI.Image> stackCells = new List<UnityEngine.UI.Image>();

    // 공격 타이밍 표시기 상태
    private bool isTimingActive;
    private float timingStartTime;
    private float timingHitTime;

    // 연출 타이머 (0이 되면 연출 끝)
    private Color flashColor;
    private float flashStrength;
    private float flashTimer;
    private float feedbackTimer;
    private float counterTimer;
    private Color playerFlashColor;
    private float playerFlashTimer;
    private float enemyFlashTimer;

    private int shownEnemyHealth = -1;
    private int shownEnemyMaxHealth = -1;

    private void Awake()
    {
        timingRoot.SetActive(false);
        resultPanel.SetActive(false);
        SetAlpha(screenFlash, 0f);
        SetAlpha(feedbackText, 0f);
        SetAlpha(counterText, 0f);
    }

    private void OnEnable()
    {
        if (battleManager == null)
        {
            Debug.LogError("[UI] BattleManager가 연결되지 않았습니다.");
            return;
        }

        battleManager.StateChanged += OnStateChanged;
        battleManager.AttackStarted += OnAttackStarted;
        battleManager.AttackResolved += OnAttackResolved;
        battleManager.GuardUnavailable += OnGuardUnavailable;
        battleManager.CounterAttacked += OnCounterAttacked;
        restartButton.onClick.AddListener(battleManager.RestartBattle);
    }

    private void OnDisable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StateChanged -= OnStateChanged;
        battleManager.AttackStarted -= OnAttackStarted;
        battleManager.AttackResolved -= OnAttackResolved;
        battleManager.GuardUnavailable -= OnGuardUnavailable;
        battleManager.CounterAttacked -= OnCounterAttacked;
        restartButton.onClick.RemoveListener(battleManager.RestartBattle);
    }

    private void Update()
    {
        BattleConfig config = battleManager != null ? battleManager.Config : null;
        if (config == null)
        {
            return;
        }

        UpdateEnemyHealth(config);
        UpdateCells(playerHpCells, playerHpContainer, config.playerMaxHealth, battleManager.PlayerHealth, hpCellColor);
        UpdateCells(guardCells, guardGaugeContainer, config.guardGaugeMax, battleManager.GuardGauge, guardCellColor);
        UpdateCells(stackCells, parryStackContainer, config.maxParryStack, battleManager.ParryStack, stackCellColor);
        UpdateTimingIndicator(config);
        UpdateEffects();
    }

    private void UpdateEnemyHealth(BattleConfig config)
    {
        int health = battleManager.EnemyHealth;
        int maxHealth = config.enemyMaxHealth;

        float ratio = maxHealth > 0 ? (float)health / maxHealth : 0f;
        enemyHpFill.anchorMax = new Vector2(ratio, 1f);

        // 글자는 값이 바뀔 때만 다시 쓴다 (매 프레임 새 문자열을 만들지 않도록)
        if (health != shownEnemyHealth || maxHealth != shownEnemyMaxHealth)
        {
            shownEnemyHealth = health;
            shownEnemyMaxHealth = maxHealth;
            enemyHpText.text = $"{health} / {maxHealth}";
        }
    }

    // 칸 수를 설정값에 맞추고, 앞에서부터 filledCount개만 색을 채운다
    private void UpdateCells(List<UnityEngine.UI.Image> cells, RectTransform container, int total, int filledCount, Color filledColor)
    {
        // 설정이 바뀌어 칸 수가 달라지면 그만큼 만들거나 지운다
        while (cells.Count < total)
        {
            cells.Add(CreateCell(container));
        }

        while (cells.Count > total)
        {
            Destroy(cells[cells.Count - 1].gameObject);
            cells.RemoveAt(cells.Count - 1);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].color = i < filledCount ? filledColor : emptyCellColor;
        }
    }

    private UnityEngine.UI.Image CreateCell(RectTransform container)
    {
        var cellObject = new GameObject("Cell", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        cellObject.transform.SetParent(container, false);

        var image = cellObject.GetComponent<UnityEngine.UI.Image>();
        image.raycastTarget = false;
        return image;
    }

    // 게이지는 예고 시작(왼쪽 끝)부터 명중 시점(오른쪽 끝)까지 채워진다.
    // 판정 구간은 매 프레임 BattleConfig 값으로 다시 계산하므로, 설정을 바꾸면 표시도 바로 바뀐다.
    private void UpdateTimingIndicator(BattleConfig config)
    {
        if (!isTimingActive)
        {
            return;
        }

        float totalTime = timingHitTime - timingStartTime;
        float progress = totalTime > 0f ? Mathf.Clamp01((Time.time - timingStartTime) / totalTime) : 1f;

        timingFill.anchorMax = new Vector2(progress, 1f);
        timingCursor.anchorMin = new Vector2(progress, 0f);
        timingCursor.anchorMax = new Vector2(progress, 1f);

        float windowRatio = totalTime > 0f ? Mathf.Clamp01(config.parryWindow / totalTime) : 1f;
        float bufferRatio = totalTime > 0f ? Mathf.Clamp01((config.parryWindow + config.parryInputBuffer) / totalTime) : 1f;

        windowZone.anchorMin = new Vector2(1f - windowRatio, 0f);
        windowZone.anchorMax = new Vector2(1f, 1f);
        bufferZone.anchorMin = new Vector2(1f - bufferRatio, 0f);
        bufferZone.anchorMax = new Vector2(1f - windowRatio, 1f);
    }

    private void UpdateEffects()
    {
        float deltaTime = Time.deltaTime;

        // 화면 플래시: 설정한 세기에서 0으로 서서히 사라진다
        flashTimer = Mathf.Max(0f, flashTimer - deltaTime);
        Color flash = flashColor;
        flash.a = flashStrength * (flashTimer / FlashDuration);
        screenFlash.color = flash;

        // 판정 글자: 처음에 살짝 커졌다가 줄어들고, 끝날 때 흐려진다
        feedbackTimer = Mathf.Max(0f, feedbackTimer - deltaTime);
        SetAlpha(feedbackText, Mathf.Clamp01(feedbackTimer / 0.3f));
        feedbackText.transform.localScale = Vector3.one * PunchScale(feedbackTimer, FeedbackDuration, 0.3f);

        counterTimer = Mathf.Max(0f, counterTimer - deltaTime);
        SetAlpha(counterText, Mathf.Clamp01(counterTimer / 0.4f));
        counterText.transform.localScale = Vector3.one * PunchScale(counterTimer, CounterDuration, 0.5f);

        // 플레이어 사각형: 판정 색으로 번쩍였다가 원래 색으로 돌아온다
        playerFlashTimer = Mathf.Max(0f, playerFlashTimer - deltaTime);
        playerBody.color = Color.Lerp(playerColor, playerFlashColor, playerFlashTimer / BodyFlashDuration);

        // 적 사각형: 공격 예고 중에는 경고색, 반격을 맞으면 흰색으로 번쩍인다
        enemyFlashTimer = Mathf.Max(0f, enemyFlashTimer - deltaTime);
        Color enemyBase = isTimingActive ? enemyTelegraphColor : enemyColor;
        enemyBody.color = Color.Lerp(enemyBase, Color.white, enemyFlashTimer / BodyFlashDuration);
    }

    // 연출이 막 시작됐을 때 (1 + amount)배로 커졌다가 PunchDuration 동안 1배로 돌아온다
    private float PunchScale(float timer, float duration, float amount)
    {
        float elapsed = duration - timer;
        return 1f + amount * Mathf.Clamp01(1f - elapsed / PunchDuration);
    }

    private void OnStateChanged(BattleState state)
    {
        if (state == BattleState.RoundStart)
        {
            resultPanel.SetActive(false);
            isTimingActive = false;
            timingRoot.SetActive(false);
            feedbackTimer = 0f;
            counterTimer = 0f;
        }
        else if (state == BattleState.Result)
        {
            isTimingActive = false;
            timingRoot.SetActive(false);

            bool isVictory = battleManager.PlayerHealth > 0;
            resultText.text = isVictory ? "VICTORY" : "DEFEAT";
            resultText.color = isVictory ? stackCellColor : hitColor;
            resultPanel.SetActive(true);
        }
    }

    private void OnAttackStarted(int index, int count, float startTime, float hitTime)
    {
        timingStartTime = startTime;
        timingHitTime = hitTime;
        isTimingActive = true;
        timingRoot.SetActive(true);
        timingLabel.text = $"ATTACK {index + 1} / {count}";
    }

    private void OnAttackResolved(BattleManager.AttackOutcome outcome, int damage)
    {
        isTimingActive = false;
        timingRoot.SetActive(false);

        switch (outcome)
        {
            case BattleManager.AttackOutcome.ParrySuccess:
                ShowFeedback("PARRY!", parryColor, 0.45f);
                break;
            case BattleManager.AttackOutcome.Guard:
                ShowFeedback(damage > 0 ? $"GUARD  -{damage}" : "GUARD", guardColor, 0.3f);
                break;
            case BattleManager.AttackOutcome.ParryFail:
                ShowFeedback($"MISS TIMING  -{damage}", hitColor, 0.5f);
                break;
            default:
                ShowFeedback($"HIT  -{damage}", hitColor, 0.5f);
                break;
        }
    }

    private void OnGuardUnavailable()
    {
        ShowFeedback("NO GUARD GAUGE", noGaugeColor, 0.15f);
    }

    private void OnCounterAttacked(int stack, int damage)
    {
        if (stack > 0)
        {
            counterText.text = $"{stack} STACK COUNTER!\n{damage} DAMAGE";
            counterText.color = stackCellColor;
            enemyFlashTimer = BodyFlashDuration;
            Flash(Color.white, 0.3f);
        }
        else
        {
            counterText.text = "NO STACK";
            counterText.color = noGaugeColor;
        }

        counterTimer = CounterDuration;
    }

    // 판정 글자 + 화면 플래시 + 플레이어 사각형 색 변화를 한 번에 보여준다
    private void ShowFeedback(string message, Color color, float flashAmount)
    {
        feedbackText.text = message;
        feedbackText.color = color;
        feedbackTimer = FeedbackDuration;

        playerFlashColor = color;
        playerFlashTimer = BodyFlashDuration;

        Flash(color, flashAmount);
    }

    private void Flash(Color color, float strength)
    {
        flashColor = color;
        flashStrength = strength;
        flashTimer = FlashDuration;
    }

    private static void SetAlpha(UnityEngine.UI.Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
