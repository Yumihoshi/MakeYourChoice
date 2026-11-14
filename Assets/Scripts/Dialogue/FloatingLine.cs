using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// 单条 3D 飘字。打字进度由外部设置，自己负责朝向、浮动，以及收尾时逐字像纸片一样掉落平铺、
    /// 全部落地后逐字缩放到零。
    /// </summary>
    public sealed class FloatingLine : MonoBehaviour
    {
        enum LineState
        {
            Idle,
            Active,
            Dissolving,
            Finished,
        }

        /// <summary>单个字的掉落/缩小状态；顶点与枢轴都在 TMP 的局部空间里。</summary>
        struct Glyph
        {
            public int vertexIndex;
            public int meshIndex;
            public Vector3 pivot;
            public Vector3[] baseVertices;
            public Vector3 startPosition;
            public Vector3 landPosition;
            public Quaternion landRotation;
            public Vector3 spinAxis;
            public float spinAngle;
            public float fallDelay;
            public float fallDuration;
            public float shrinkStart;
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
            public bool landed;
        }

        const int VerticesPerGlyph = 4;

        [SerializeField, LabelText("文本")] TMP_Text text;
        [SerializeField, LabelText("面向相机")] Camera faceCamera;
        [SerializeField, LabelText("浮动幅度"), MinValue(0f)] float bobAmplitude = 0.04f;
        [SerializeField, LabelText("浮动速度"), MinValue(0f)] float bobSpeed = 2f;
        [SerializeField, LabelText("重力"), MinValue(0.01f)] float gravity = 9.8f;
        [SerializeField, LabelText("地面层")] LayerMask groundMask;
        [SerializeField, LabelText("落地偏移")] float groundOffset = 0.02f;
        [SerializeField, LabelText("全部落地停顿"), MinValue(0f)] float impactPause = 0.35f;
        [SerializeField, LabelText("逐字掉落间隔"), MinValue(0f)] float fallStagger = 0.035f;
        [SerializeField, LabelText("横向飘移")] float fallDrift = 0.12f;
        [SerializeField, LabelText("随机旋转角度"), MinValue(0f)] float randomTilt = 35f;
        [SerializeField, LabelText("逐字缩小间隔"), MinValue(0f)] float shrinkStagger = 0.02f;
        [SerializeField, LabelText("缩小时间"), MinValue(0.01f)] float shrinkDuration = 0.3f;

        Vector3 baseScale;
        Vector3 basePosition;
        LineState state;

        Glyph[] glyphs;
        float dissolveTime;
        float landedTimer;
        float shrinkBeginTime;
        bool allLanded;
        bool meshDirty;

        public event System.Action<FloatingLine> Finished;

        /// <summary>由 WorldLineView 注入世界相机与地面层。</summary>
        public void Configure(Camera camera, LayerMask mask)
        {
            faceCamera = camera;
            groundMask = mask;
        }

        void Awake()
        {
            if (text == null)
            {
                Debug.LogError("FloatingLine 未指定 TMP_Text", this);
                enabled = false;
                return;
            }

            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.0001f)
            {
                Debug.LogError("FloatingLine 的初始缩放不能为 0", this);
                enabled = false;
                return;
            }

            state = LineState.Idle;
        }

        /// <summary>显示一条新台词，并立即设置可见字数。</summary>
        public void Show(string content, int visibleCount, Vector3 worldPosition)
        {
            if (text == null)
            {
                Debug.LogError("FloatingLine 未指定 TMP_Text", this);
                return;
            }

            gameObject.SetActive(true);
            transform.SetPositionAndRotation(worldPosition, transform.rotation);
            transform.localScale = baseScale;

            glyphs = null;
            allLanded = false;
            meshDirty = false;
            landedTimer = 0f;

            text.text = content;
            text.ForceMeshUpdate();
            SetVisibleCount(visibleCount);

            basePosition = worldPosition;
            state = LineState.Active;
        }

        /// <summary>设置当前可见字数，超出范围自动夹取。</summary>
        public void SetVisibleCount(int count)
        {
            if (text == null) return;
            text.maxVisibleCharacters = Mathf.Clamp(count, 0, text.textInfo.characterCount);
        }

        /// <summary>
        /// 开始收尾：整句先补全，再逐字像纸片一样掉落、带随机旋转地摔成平铺，全部落地后逐字缩到零。
        /// 找不到地面时原地逐字缩小并记录警告。
        /// </summary>
        public void Dismiss()
        {
            if (state != LineState.Active) return;

            if (text == null)
            {
                Debug.LogError("FloatingLine 未指定 TMP_Text，无法收尾", this);
                HideImmediate();
                return;
            }

            text.maxVisibleCharacters = text.textInfo.characterCount;
            text.ForceMeshUpdate();

            bool hasGround = TryGetGroundY(out float groundWorldY);
            if (!hasGround)
            {
                Debug.LogWarning("FloatingLine 下方没有地面，改为原地逐字缩小", this);
            }

            BuildGlyphs(groundWorldY, hasGround);
            if (glyphs == null || glyphs.Length == 0)
            {
                HideImmediate();
                return;
            }

            dissolveTime = 0f;
            landedTimer = 0f;
            shrinkBeginTime = 0f;
            allLanded = false;
            meshDirty = true;
            state = LineState.Dissolving;
            ApplyGlyphMeshes();
        }

        /// <summary>立刻隐藏并复位，用于回收或清场。</summary>
        public void HideImmediate()
        {
            state = LineState.Idle;
            glyphs = null;
            allLanded = false;
            meshDirty = false;
            landedTimer = 0f;
            transform.localScale = baseScale;
            gameObject.SetActive(false);
        }

        void Update()
        {
            switch (state)
            {
                case LineState.Active:
                    transform.position = basePosition +
                                         Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
                    break;

                case LineState.Dissolving:
                    UpdateDissolve();

                    if (meshDirty)
                    {
                        ApplyGlyphMeshes();
                        meshDirty = false;
                    }

                    break;
            }
        }

        void LateUpdate()
        {
            if (faceCamera == null) return;
            if (state == LineState.Dissolving) return;

            float yaw = faceCamera.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>逐帧推进单字的掉落与缩小，并更新是否已经开始整体缩小。</summary>
        void UpdateDissolve()
        {
            dissolveTime += Time.deltaTime;

            bool stillFalling = false;
            bool anyVisible = false;

            for (int i = 0; i < glyphs.Length; i++)
            {
                Glyph glyph = glyphs[i];

                if (!glyph.landed)
                {
                    float progress = Mathf.Clamp01(
                        (dissolveTime - glyph.fallDelay) / Mathf.Max(0.01f, glyph.fallDuration));

                    if (progress >= 1f)
                    {
                        glyph.position = glyph.landPosition;
                        glyph.rotation = glyph.landRotation;
                        glyph.landed = true;
                    }
                    else
                    {
                        float eased = progress * progress;
                        glyph.position = new Vector3(
                            Mathf.Lerp(glyph.startPosition.x, glyph.landPosition.x, progress),
                            Mathf.Lerp(glyph.startPosition.y, glyph.landPosition.y, eased),
                            Mathf.Lerp(glyph.startPosition.z, glyph.landPosition.z, progress));
                        glyph.rotation = Quaternion.SlerpUnclamped(Quaternion.identity, glyph.landRotation, progress) *
                                         Quaternion.AngleAxis(glyph.spinAngle * progress, glyph.spinAxis);
                        stillFalling = true;
                    }
                }

                if (glyph.landed && glyph.scale > 0f)
                {
                    anyVisible = true;
                }

                glyphs[i] = glyph;
            }

            if (stillFalling)
            {
                meshDirty = true;
                return;
            }

            if (!allLanded)
            {
                allLanded = true;
                landedTimer = 0f;
                shrinkBeginTime = dissolveTime + impactPause;
                meshDirty = true;
                return;
            }

            landedTimer += Time.deltaTime;

            bool stillShrinking = false;
            for (int i = 0; i < glyphs.Length; i++)
            {
                Glyph glyph = glyphs[i];
                if (glyph.scale <= 0f) continue;

                float start = shrinkBeginTime + glyph.shrinkStart;
                float factor = 1f - Mathf.Clamp01((dissolveTime - start) / Mathf.Max(0.01f, shrinkDuration));
                glyph.scale = factor;
                glyphs[i] = glyph;

                if (factor > 0f) stillShrinking = true;
            }

            meshDirty = true;

            if (stillShrinking || anyVisible)
            {
                return;
            }

            Finish();
        }

        /// <summary>从当前锚点向下做一次射线检测，得到地面高度；没有地面返回 false。</summary>
        bool TryGetGroundY(out float groundWorldY)
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50f,
                    groundMask, QueryTriggerInteraction.Ignore))
            {
                groundWorldY = transform.position.y;
                return false;
            }

            groundWorldY = hit.point.y + groundOffset;
            return true;
        }

        /// <summary>为每个可见字符建立掉落数据：起点、落点、平铺姿态与随机旋转。</summary>
        void BuildGlyphs(float groundWorldY, bool hasGround)
        {
            TMP_TextInfo info = text.textInfo;
            float verticalScale = Mathf.Abs(text.transform.lossyScale.y) > 0.0001f
                ? text.transform.lossyScale.y
                : 1f;
            float landLocalY = (groundWorldY - transform.position.y) / verticalScale;

            var list = new System.Collections.Generic.List<Glyph>();

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;

                int vertexIndex = character.vertexIndex;
                int meshIndex = character.materialReferenceIndex;
                if (meshIndex < 0 || meshIndex >= info.meshInfo.Length) continue;

                Vector3[] vertices = info.meshInfo[meshIndex].vertices;
                if (vertexIndex + VerticesPerGlyph > vertices.Length) continue;

                Vector3 pivot = Vector3.zero;
                for (int v = 0; v < VerticesPerGlyph; v++)
                {
                    pivot += vertices[vertexIndex + v];
                }

                pivot /= VerticesPerGlyph;

                float startWorldY = transform.position.y + pivot.y * verticalScale;
                float fallDistance = Mathf.Max(0.01f, startWorldY - groundWorldY);
                float fallDuration = hasGround ? Mathf.Sqrt(2f * fallDistance / Mathf.Max(0.01f, gravity)) : 0.01f;

                float yaw = Random.Range(0f, 360f);
                float tiltX = Random.Range(-randomTilt, randomTilt) * 0.2f;
                float tiltZ = Random.Range(-randomTilt, randomTilt) * 0.2f;

                Vector3 landPosition = hasGround
                    ? new Vector3(
                        pivot.x + Random.Range(-fallDrift, fallDrift),
                        landLocalY,
                        pivot.z + Random.Range(-fallDrift, fallDrift))
                    : pivot;

                list.Add(new Glyph
                {
                    vertexIndex = vertexIndex,
                    meshIndex = meshIndex,
                    pivot = pivot,
                    baseVertices = new[]
                    {
                        vertices[vertexIndex], vertices[vertexIndex + 1],
                        vertices[vertexIndex + 2], vertices[vertexIndex + 3],
                    },
                    startPosition = pivot,
                    landPosition = landPosition,
                    landRotation = hasGround
                        ? Quaternion.Euler(90f + tiltX, yaw, tiltZ)
                        : Quaternion.identity,
                    spinAxis = Random.onUnitSphere,
                    spinAngle = hasGround ? Random.Range(-120f, 120f) : 0f,
                    fallDelay = list.Count * fallStagger,
                    fallDuration = fallDuration,
                    shrinkStart = list.Count * shrinkStagger,
                    position = pivot,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    landed = false,
                });
            }

            glyphs = list.Count > 0 ? list.ToArray() : null;
        }

        /// <summary>把每个字的当前姿态写回 TMP 网格并提交。</summary>
        void ApplyGlyphMeshes()
        {
            if (text == null || glyphs == null) return;

            TMP_TextInfo info = text.textInfo;

            for (int i = 0; i < glyphs.Length; i++)
            {
                Glyph glyph = glyphs[i];
                if (glyph.meshIndex < 0 || glyph.meshIndex >= info.meshInfo.Length) continue;

                Vector3[] vertices = info.meshInfo[glyph.meshIndex].vertices;
                if (glyph.vertexIndex + VerticesPerGlyph > vertices.Length) continue;

                for (int v = 0; v < VerticesPerGlyph; v++)
                {
                    Vector3 offset = glyph.rotation * (glyph.baseVertices[v] - glyph.pivot);
                    vertices[glyph.vertexIndex + v] = glyph.position + offset * glyph.scale;
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        void Finish()
        {
            state = LineState.Finished;
            Finished?.Invoke(this);
        }

        void OnValidate()
        {
            if (fallStagger < 0f) fallStagger = 0f;
            if (shrinkStagger < 0f) shrinkStagger = 0f;
            if (fallDrift < 0f) fallDrift = 0f;
            if (randomTilt < 0f) randomTilt = 0f;
            if (shrinkDuration < 0.01f) shrinkDuration = 0.01f;
            if (impactPause < 0f) impactPause = 0f;
            if (gravity < 0.01f) gravity = 0.01f;
        }
    }
}
