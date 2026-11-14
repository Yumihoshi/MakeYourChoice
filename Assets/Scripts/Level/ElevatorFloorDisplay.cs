using System;
using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 电梯门旁的竖版楼层显示器：上方楼号持续递减滚动，下方下行箭头循环下移；
    /// 开始游戏时减速停到目标楼层。
    /// </summary>
    public sealed class ElevatorFloorDisplay : MonoBehaviour
    {
        [LabelText("楼层数字"), SerializeField] TMP_Text floorText;
        [LabelText("下一楼层数字"), SerializeField] TMP_Text nextFloorText;
        [LabelText("下行箭头"), SerializeField] TMP_Text[] arrows;
        [LabelText("箭头起点高度"), SerializeField] float arrowTop = 0f;
        [LabelText("箭头滚动距离"), SerializeField] float arrowTravel = 0.34f;
        [LabelText("到站楼层高度"), SerializeField] float arrivedFloorY = 0f;
        [LabelText("待机最高楼层"), SerializeField] int idleTopFloor = 32;
        [LabelText("待机最低楼层"), SerializeField] int idleBottomFloor = 3;
        [LabelText("待机换层间隔"), SerializeField] float idleStepSeconds = 0.75f;
        [LabelText("到站用时"), SerializeField] float arrivalSeconds = 2.4f;
        [LabelText("到站停留"), SerializeField] float arrivalHoldSeconds = 0.45f;

        static readonly Color DigitColor = new Color(1f, 0.68f, 0.22f, 1f);
        static readonly Color ArrowColor = new Color(1f, 0.55f, 0.12f, 1f);
        const float RollTravel = 0.06f;
        const float ArrowCycleSeconds = 1.1f;
        const float ArrowFadeSeconds = 0.12f;

        static ElevatorFloorDisplay _instance;

        Coroutine _idleRoutine;
        Coroutine _arrivalRoutine;
        Sequence _rollSequence;
        Tween _scaleTween;
        Tween _colorTween;
        bool _arrowsRunning;
        bool _arrived;
        float _arrowPhase;
        float _arrowFade;
        int _floor;
        Vector3 _floorHome;
        Vector3 _nextHome;

        /// <summary>当前显示的楼层号。</summary>
        public int Floor => _floor;

        /// <summary>是否正在播放到站动画。</summary>
        public bool IsArriving { get; private set; }

        /// <summary>是否已经停稳在目标楼层。</summary>
        public bool HasArrived => _arrived;

        /// <summary>场景中的楼层显示器；不存在时为 null。</summary>
        public static ElevatorFloorDisplay Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState()
        {
            _instance = null;
        }

        void Awake()
        {
            if (floorText == null || nextFloorText == null || arrows == null || arrows.Length == 0)
            {
                Debug.LogError("ElevatorFloorDisplay 缺少楼层数字或箭头引用", this);
                enabled = false;
                return;
            }

            _instance = this;
            _floorHome = floorText.rectTransform.localPosition;
            _nextHome = nextFloorText.rectTransform.localPosition;
            SetFloor(idleTopFloor);
            SetArrowAlpha(0f);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            _rollSequence?.Kill();
            _scaleTween?.Kill();
            _colorTween?.Kill();
            StopAllCoroutines();
        }

        /// <summary>开始待机下行：楼号按固定节拍递减，到最低层后回到最高层继续。</summary>
        public void BeginIdleDescent()
        {
            if (!enabled)
            {
                return;
            }

            StopIdle();
            StopArrival();
            IsArriving = false;
            _arrived = false;
            _arrowsRunning = true;
            SetFloor(idleTopFloor);
            _idleRoutine = StartCoroutine(IdleRoutine());
        }

        /// <summary>从当前楼层减速停到目标楼层，停稳后回调；返回是否成功开始。</summary>
        public bool ArriveAt(int targetFloor, Action onArrived)
        {
            if (!enabled || IsArriving)
            {
                return false;
            }

            StopIdle();
            IsArriving = true;
            _arrivalRoutine = StartCoroutine(ArrivalRoutine(targetFloor, onArrived));
            return true;
        }

        IEnumerator IdleRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(idleStepSeconds);
                var next = _floor - 1;
                if (next < idleBottomFloor)
                {
                    next = idleTopFloor;
                }

                yield return RollTo(next, 0.12f);
            }
        }

        IEnumerator ArrivalRoutine(int targetFloor, Action onArrived)
        {
            var start = _floor;
            var steps = Mathf.Max(0, start - targetFloor);
            var rollSeconds = 0.18f;
            var travelSeconds = Mathf.Max(0.2f, arrivalSeconds - rollSeconds);

            if (steps > 0)
            {
                var totalWeight = 0f;
                for (var i = 0; i < steps; i++)
                {
                    totalWeight += StepWeight(i, steps);
                }

                for (var i = 1; i <= steps; i++)
                {
                    var gap = travelSeconds * StepWeight(i - 1, steps) / totalWeight;
                    if (gap > 0.001f)
                    {
                        yield return new WaitForSeconds(gap);
                    }

                    if (i < steps)
                    {
                        SetFloor(start - i);
                        PunchDigit();
                    }
                    else
                    {
                        yield return RollTo(targetFloor, rollSeconds);
                    }
                }
            }
            else
            {
                SetFloor(targetFloor);
            }

            _arrowsRunning = false;
            _arrived = true;
            yield return SettleFloorToArrived();
            FlashDigit();
            yield return new WaitForSeconds(arrivalHoldSeconds);

            IsArriving = false;
            _arrivalRoutine = null;
            onArrived?.Invoke();
        }

        /// <summary>到站后箭头收起，楼号滑到面板竖直中央，避免竖版面板下半截空着。</summary>
        IEnumerator SettleFloorToArrived()
        {
            var target = new Vector3(_floorHome.x, arrivedFloorY, _floorHome.z);
            if ((floorText.rectTransform.localPosition - target).sqrMagnitude < 0.000001f)
            {
                yield break;
            }

            _rollSequence?.Kill();
            _rollSequence = DOTween.Sequence();
            _rollSequence.Join(DOTween.To(
                () => floorText.rectTransform.localPosition,
                value => floorText.rectTransform.localPosition = value,
                target, 0.28f).SetEase(Ease.OutQuad));
            yield return _rollSequence.WaitForCompletion();
        }

        /// <summary>越接近目标层，单层停留越久，读起来像电梯减速。</summary>
        static float StepWeight(int index, int steps)
        {
            var p = steps <= 1 ? 1f : index / (float)(steps - 1);
            return Mathf.Lerp(0.35f, 1.75f, p * p);
        }

        IEnumerator RollTo(int targetFloor, float duration)
        {
            _floor = targetFloor;
            nextFloorText.text = Format(targetFloor);
            nextFloorText.alpha = 0f;

            var nextFrom = _nextHome + new Vector3(0f, RollTravel, 0f);
            nextFloorText.rectTransform.localPosition = nextFrom;
            floorText.rectTransform.localPosition = _floorHome;
            floorText.alpha = 1f;

            _rollSequence?.Kill();
            _rollSequence = DOTween.Sequence();
            _rollSequence.Join(DOTween.To(
                () => floorText.alpha, value => floorText.alpha = value, 0f, duration));
            _rollSequence.Join(DOTween.To(
                () => floorText.rectTransform.localPosition,
                value => floorText.rectTransform.localPosition = value,
                _floorHome - new Vector3(0f, RollTravel, 0f), duration));
            _rollSequence.Join(DOTween.To(
                () => nextFloorText.alpha, value => nextFloorText.alpha = value, 1f, duration));
            _rollSequence.Join(DOTween.To(
                () => nextFloorText.rectTransform.localPosition,
                value => nextFloorText.rectTransform.localPosition = value,
                _nextHome, duration));

            yield return _rollSequence.WaitForCompletion();

            floorText.text = Format(targetFloor);
            floorText.alpha = 1f;
            floorText.rectTransform.localPosition = _floorHome;
            nextFloorText.alpha = 0f;
            nextFloorText.rectTransform.localPosition = _nextHome;
        }

        void Update()
        {
            AnimateArrows(Time.deltaTime);
        }

        /// <summary>箭头按同一相位错开，自上而下滚动，两端淡入淡出。</summary>
        void AnimateArrows(float deltaTime)
        {
            var targetRate = _arrowsRunning ? 1f : 0f;
            _arrowPhase = Mathf.Repeat(
                _arrowPhase + deltaTime / ArrowCycleSeconds * targetRate, 1f);
            _arrowFade = Mathf.MoveTowards(_arrowFade, targetRate, deltaTime / ArrowFadeSeconds);

            for (var i = 0; i < arrows.Length; i++)
            {
                var arrow = arrows[i];
                var rect = arrow.rectTransform;
                var t = Mathf.Repeat(_arrowPhase + i / (float)arrows.Length, 1f);
                var position = rect.localPosition;
                position.y = arrowTop - arrowTravel * t;
                rect.localPosition = position;

                var fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.15f, (1f - t) / 0.25f));
                var alpha = fade * _arrowFade;
                var color = ArrowColor;
                color.a = alpha;
                arrow.color = color;
            }
        }

        void SetArrowAlpha(float alpha)
        {
            _arrowFade = alpha;
            for (var i = 0; i < arrows.Length; i++)
            {
                var color = ArrowColor;
                color.a = alpha;
                arrows[i].color = color;
            }
        }

        void SetFloor(int value)
        {
            _floor = value;
            _rollSequence?.Kill();
            _scaleTween?.Kill();
            floorText.alpha = 1f;
            floorText.rectTransform.localPosition = _floorHome;
            floorText.rectTransform.localScale = Vector3.one;
            floorText.text = Format(value);
            nextFloorText.alpha = 0f;
            nextFloorText.rectTransform.localPosition = _nextHome;
        }

        void PunchDigit()
        {
            _scaleTween?.Kill();
            floorText.rectTransform.localScale = Vector3.one * 1.06f;
            _scaleTween = floorText.rectTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad);
        }

        void FlashDigit()
        {
            _colorTween?.Kill();
            floorText.color = Color.white;
            _colorTween = DOTween.To(
                () => floorText.color, value => floorText.color = value, DigitColor, 0.45f);
        }

        static string Format(int value)
        {
            return value.ToString("00");
        }

        void StopIdle()
        {
            if (_idleRoutine != null)
            {
                StopCoroutine(_idleRoutine);
                _idleRoutine = null;
            }
        }

        void StopArrival()
        {
            if (_arrivalRoutine != null)
            {
                StopCoroutine(_arrivalRoutine);
                _arrivalRoutine = null;
            }
        }

        void OnDisable()
        {
            _rollSequence?.Kill();
            _scaleTween?.Kill();
            _colorTween?.Kill();
        }
    }
}

