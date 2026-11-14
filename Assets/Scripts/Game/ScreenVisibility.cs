using UnityEngine;

namespace Game
{
    /// <summary>
    /// 判断一个世界坐标在相机画面里是否看得见：在最大距离内、落在视口内、且视线没有被场景挡住。
    /// </summary>
    public static class ScreenVisibility
    {
        /// <summary>
        /// 世界坐标是否在相机画面内且可见。
        /// </summary>
        /// <param name="camera">用于判断的相机；为空时返回 false。</param>
        /// <param name="worldPosition">目标世界坐标。</param>
        /// <param name="maxDistance">相机到目标的最大距离，超过即认为看不见。</param>
        /// <param name="blockMask">会遮挡视线的层，通常排除玩家层。</param>
        public static bool IsOnScreen(Camera camera, Vector3 worldPosition, float maxDistance, LayerMask blockMask)
        {
            if (camera == null)
            {
                return false;
            }

            Vector3 cameraPosition = camera.transform.position;
            if (Vector3.Distance(cameraPosition, worldPosition) > maxDistance)
            {
                return false;
            }

            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            if (viewport.z <= 0f)
            {
                return false;
            }

            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
            {
                return false;
            }

            return !Physics.Linecast(cameraPosition, worldPosition, blockMask, QueryTriggerInteraction.Ignore);
        }
    }
}
