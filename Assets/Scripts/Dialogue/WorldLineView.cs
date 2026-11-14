using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// 3D 飘字实例池。同一时间只维护一条 active；旧句下落时 active 置空，
    /// 新句可以立刻复用或创建新实例。
    /// </summary>
    public sealed class WorldLineView : MonoBehaviour
    {
        [SerializeField, Required, LabelText("3D 字预制体")] FloatingLine prefab;
        [SerializeField, LabelText("池根节点")] Transform poolRoot;
        [SerializeField, Required, LabelText("世界相机")] Camera worldCamera;
        [SerializeField, LabelText("地面层")] LayerMask groundMask;
        [SerializeField, LabelText("最大实例数"), MinValue(1)] int maxLines = 4;
        [SerializeField, LabelText("最近可读距离（更近就改用对话框）"), MinValue(0f)] float minReadableDistance = 1.2f;

        readonly List<FloatingLine> allLines = new List<FloatingLine>();
        readonly Queue<FloatingLine> idle = new Queue<FloatingLine>();
        FloatingLine active;

        public bool HasActive => active != null;

        /// <summary>
        /// 指定世界位置上的 3D 字在相机画面里是否看得清：相机到字的距离在 [minReadableDistance, readableDistance] 之间、
        /// 字在视锥内，且视线没有被场景挡住。太近时字会糊满屏幕，同样算看不清（调用方改用对话框）。
        /// 没有配置世界相机时返回 false（3D 字本来就无法正确显示）。
        /// </summary>
        /// <param name="worldPosition">3D 字的世界位置，通常是文字锚点。</param>
        /// <param name="readableDistance">相机到字的最大可读距离，超过即认为看不清。</param>
        public bool IsLineVisible(Vector3 worldPosition, float readableDistance)
        {
            if (worldCamera == null) return false;
            if (Vector3.Distance(worldCamera.transform.position, worldPosition) < minReadableDistance) return false;

            int mask = ~LayerMask.GetMask("Player");
            return ScreenVisibility.IsOnScreen(worldCamera, worldPosition, readableDistance, mask);
        }

        void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError("WorldLineView 未指定 FloatingLine 预制体", this);
                enabled = false;
                return;
            }

            if (worldCamera == null)
            {
                Debug.LogError("WorldLineView 未指定世界相机", this);
                enabled = false;
                return;
            }

            if (maxLines <= 0)
            {
                Debug.LogError("WorldLineView 最大实例数必须大于 0", this);
                enabled = false;
                return;
            }

            if (groundMask == 0)
            {
                Debug.LogWarning("WorldLineView 地面层为 Nothing，3D 字不会落到地面", this);
            }
        }

        /// <summary>显示一条新 3D 字；已有 active 时拒绝并警告。</summary>
        public void ShowLine(string text, int visibleCount, Vector3 worldPosition)
        {
            if (prefab == null)
            {
                Debug.LogError("WorldLineView 未指定 FloatingLine 预制体", this);
                return;
            }

            if (worldCamera == null)
            {
                Debug.LogError("WorldLineView 未指定世界相机", this);
                return;
            }

            if (active != null)
            {
                Debug.LogWarning("WorldLineView 已有 active，忽略新的 3D 字", this);
                return;
            }

            FloatingLine line = GetLine();
            if (line == null) return;

            active = line;
            line.Configure(worldCamera, groundMask);
            line.Show(text, visibleCount, worldPosition);
        }

        public void SetVisibleCount(int count)
        {
            if (active == null) return;
            active.SetVisibleCount(count);
        }

        public void DismissActive()
        {
            if (active == null) return;

            FloatingLine line = active;
            active = null;
            line.Dismiss();
        }

        public void HideAll()
        {
            active = null;
            idle.Clear();

            foreach (FloatingLine line in allLines)
            {
                if (line == null) continue;
                line.HideImmediate();
                if (!idle.Contains(line)) idle.Enqueue(line);
            }
        }

        FloatingLine GetLine()
        {
            while (idle.Count > 0)
            {
                FloatingLine line = idle.Dequeue();
                if (line != null) return line;
            }

            if (allLines.Count < maxLines)
            {
                FloatingLine line = CreateLine();
                if (line != null) return line;
            }

            if (allLines.Count == 0) return null;

            FloatingLine oldest = allLines[0];
            allLines.RemoveAt(0);
            allLines.Add(oldest);
            if (oldest == null) return null;

            Debug.LogWarning("WorldLineView 池已满，复用最老的 3D 字", this);
            oldest.HideImmediate();
            return oldest;
        }

        FloatingLine CreateLine()
        {
            Transform parent = poolRoot != null ? poolRoot : transform;
            FloatingLine line = Instantiate(prefab, parent);
            line.gameObject.SetActive(false);
            line.Finished += OnLineFinished;
            allLines.Add(line);
            return line;
        }

        void OnLineFinished(FloatingLine line)
        {
            if (line == null) return;
            if (active == line) active = null;
            line.gameObject.SetActive(false);
            if (!idle.Contains(line)) idle.Enqueue(line);
        }

        void OnValidate()
        {
            if (maxLines < 1) maxLines = 1;
        }
    }
}
