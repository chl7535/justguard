using System.Collections.Generic;
using UnityEngine;

// 적의 공격 한 번에 대한 데이터
[System.Serializable]
public class EnemyAttack
{
    [Tooltip("예고 시간(초). 이 시간 동안 공격이 온다는 신호를 보여준다.")]
    [Min(0f)]
    public float telegraphTime = 0.8f;

    [Tooltip("발생 시간(초). 예고가 끝난 뒤 공격이 실제로 플레이어에게 닿기까지 걸리는 시간.")]
    [Min(0f)]
    public float startupTime = 0.3f;

    [Tooltip("이 공격에 맞았을 때 플레이어가 받는 피해량.")]
    [Min(0)]
    public int damage = 1;
}

// 적이 한 번의 적 턴 동안 사용하는 공격 패턴.
// 공격 목록을 위에서부터 순서대로 사용한다.
[CreateAssetMenu(fileName = "EnemyPattern", menuName = "JustGuard/Enemy Pattern")]
public class EnemyPattern : ScriptableObject
{
    [Tooltip("적 턴에 위에서부터 순서대로 사용하는 공격 목록.")]
    public List<EnemyAttack> attacks = new List<EnemyAttack>();
}
