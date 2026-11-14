using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 菜单期间给第一人称相机叠加电梯运行的低频晃动，到站时做一次下沉回弹。
    /// </summary>
    public sealed class ElevatorRideMotion : MonoBehaviour
    {
        [LabelText("横向晃动幅度"), SerializeField] float horizontalAmplitude = 0.01f;
        [LabelText("纵向晃动幅度"), SerializeField] float verticalAmplitude = 0.02f;
        [LabelText("晃动频率"), SerializeField] float frequency = 0.9f;
        [LabelText("到站下沉距离"), SerializeField] float arrivalDip = 0.045f;
        [LabelText("到站回弹时间"), SerializeField] float arrivalSettle = 0.5f;

        Vector3 _baseLocalPosition;
        Quaternion _baseLocalRotation;
        Tween _arrivalTween;
        bool _riding;
        float _seed;

        void Awake()
        {
            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _seed = Random.value * 16f;
        }

        /// <summary>开始模拟电梯运行时的轻微晃动。</summary>
        public void Ride()
        {
            _arrivalTween?.Kill();
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            _riding = true;
        }

        /// <summary>停止晃动，并播放下沉回弹表现电梯停稳。</summary>
        public void Arrive()
        {
            _riding = false;
            _arrivalTween?.Kill();
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;

            var dip = _baseLocalPosition + new Vector3(0f, -arrivalDip, 0f);
            var dipRotation = _baseLocalRotation * Quaternion.Euler(-1.1f, 0f, 0f);

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOLocalMove(dip, 0.07f).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOLocalRotateQuaternion(dipRotation, 0.07f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOLocalMove(_baseLocalPosition, arrivalSettle).SetEase(Ease.OutBack));
            sequence.Join(transform.DOLocalRotateQuaternion(_baseLocalRotation, arrivalSettle).SetEase(Ease.OutQuad));
            _arrivalTween = sequence;
        }

        void Update()
        {
            if (!_riding)
            {
                return;
            }

            var time = Time.time * frequency;
            var x = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f * horizontalAmplitude;
            var y = (Mathf.PerlinNoise(_seed + 17.3f, time * 1.4f) - 0.5f) * 2f * verticalAmplitude;
            y += Mathf.Sin(Time.time * 0.55f) * verticalAmplitude * 0.4f;

            transform.localPosition = _baseLocalPosition + new Vector3(x, y, 0f);

            var roll = (Mathf.PerlinNoise(_seed + 5.7f, time * 0.7f) - 0.5f) * 0.3f;
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, roll);
        }

        void OnDisable()
        {
            _riding = false;
            _arrivalTween?.Kill();
            _arrivalTween = null;
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
        }
    }
}
