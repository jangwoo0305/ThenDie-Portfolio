using Mirror;
using UnityEngine;

namespace ThenDie.Ingame
{
    public class OwlMailPlayerCarry : NetworkBehaviour
    {
        [SyncVar]
        private int carryLetterCount;
        
        public int CarryLetterCount => carryLetterCount;

        // 편지를 더 들 수 있는지 확인
        // maxCarryCount가 0이면 제한 없음
        public bool CanAddLetter(int maxCarryCount)
        {
            if(maxCarryCount <= 0)
                return true;
            
            return carryLetterCount < maxCarryCount;
        }
        // 보유 편지 한장 추가
        [Server]
        public void AddLetter()
        {
            carryLetterCount++;
        }
        
        // 우편함에 편지를 전달할 때 사용
        // 현재 들고 있는 편지 수를 반환하고, 보유 수량을 0으로 변경
        [Server]
        public int TakeAllLetters()
        {
            int count =  carryLetterCount;
            carryLetterCount = 0;
            return count;
        }

        // 사보타지가 끝나면 보유 편지를 초기화 할때 사용
        [Server]
        public void ClearLetters()
        {
            carryLetterCount = 0;
        }
    }
}
