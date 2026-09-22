using UnityEngine;

namespace ThenDie.Ingame
{
    [DisallowMultipleComponent]
    public class MiniMapPoint : MonoBehaviour
    {
        [SerializeField] private Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);

        public Vector2 NormalizedPosition => new Vector2(
            Mathf.Clamp01(normalizedPosition.x),
            Mathf.Clamp01(normalizedPosition.y));

        private void OnValidate()
        {
            normalizedPosition = NormalizedPosition;
        }
    }
}
