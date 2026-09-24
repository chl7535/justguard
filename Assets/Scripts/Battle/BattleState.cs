// 전투 상태 머신의 상태 목록.
// RoundStart → EnemyTurn → PlayerTurn → EnemyTurn … 을 반복하다가 승패가 나면 Result로 간다.
public enum BattleState
{
    RoundStart, // 라운드 준비: 가드 게이지를 채운다
    EnemyTurn,  // 적이 패턴의 공격을 순서대로 한다
    PlayerTurn, // 쌓인 패링 스택만큼 반격한다
    Result      // 승패 결과
}
