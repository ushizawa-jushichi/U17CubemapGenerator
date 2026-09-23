using UnityEngine;
using UnityEngine.UIElements;

namespace Uchuhikoshi.U17CubemapGenerator
{
    /// <summary>
    ///     X/Y/Z軸の回転角度スライダーと有効トグル、数値入力を統合した回転設定用 VisualElement。
    /// </summary>
    public class RotationField : BaseField<Vector3>
    {
        public const float MinAngle = 0f;
        public const float MaxAngle = 360f;

        private readonly Vector3IntField _eulerField;
        private readonly Slider _sliderX;
        private readonly Slider _sliderY;
        private readonly Slider _sliderZ;

        public RotationField(string? label) : this(label, new VisualElement())
        {
        }

        private RotationField(string? label, VisualElement container) : base(label, container)
        {
            container.style.flexDirection = FlexDirection.Column;

            _eulerField = new Vector3IntField("Rotation");
            container.Add(_eulerField);

            var containerX = new VisualElement();
            containerX.style.flexDirection = FlexDirection.Row;
            containerX.style.alignItems = Align.Center;
            ToggleX = new Toggle();
            ToggleX.text = "X";
            ToggleX.value = true;
            _sliderX = new Slider { lowValue = MinAngle, highValue = MaxAngle };
            _sliderX.style.flexGrow = 1;
            containerX.Add(ToggleX);
            containerX.Add(_sliderX);
            container.Add(containerX);

            var containerY = new VisualElement();
            containerY.style.flexDirection = FlexDirection.Row;
            containerY.style.alignItems = Align.Center;
            ToggleY = new Toggle();
            ToggleY.text = "Y";
            ToggleY.value = true;
            _sliderY = new Slider { lowValue = MinAngle, highValue = MaxAngle };
            _sliderY.style.flexGrow = 1;
            containerY.Add(ToggleY);
            containerY.Add(_sliderY);
            container.Add(containerY);

            var containerZ = new VisualElement();
            containerZ.style.flexDirection = FlexDirection.Row;
            containerZ.style.alignItems = Align.Center;
            ToggleZ = new Toggle();
            ToggleZ.text = "Z";
            ToggleZ.value = true;
            _sliderZ = new Slider { lowValue = MinAngle, highValue = MaxAngle };
            _sliderZ.style.flexGrow = 1;
            containerZ.Add(ToggleZ);
            containerZ.Add(_sliderZ);
            container.Add(containerZ);

            EventCallback<ChangeEvent<float>> onSliderChanged = _ =>
            {
                value = new Vector3(_sliderX.value, _sliderY.value, _sliderZ.value);
            };
            _sliderX.RegisterValueChangedCallback(onSliderChanged);
            _sliderY.RegisterValueChangedCallback(onSliderChanged);
            _sliderZ.RegisterValueChangedCallback(onSliderChanged);

            _eulerField.RegisterValueChangedCallback(evt => { value = evt.newValue; });

            ToggleX.RegisterValueChangedCallback(_ => _sliderX.SetEnabled(ToggleX.value));
            ToggleY.RegisterValueChangedCallback(_ => _sliderY.SetEnabled(ToggleY.value));
            ToggleZ.RegisterValueChangedCallback(_ => _sliderZ.SetEnabled(ToggleZ.value));
        }

        public RotationField() : this(null)
        {
        }

        public Toggle ToggleX { get; }

        public Toggle ToggleY { get; }

        public Toggle ToggleZ { get; }

        public override void SetValueWithoutNotify(Vector3 newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var vx = Mathf.Repeat(newValue.x, MaxAngle);
            var vy = Mathf.Repeat(newValue.y, MaxAngle);
            var vz = Mathf.Repeat(newValue.z, MaxAngle);

            _sliderX.SetValueWithoutNotify(vx);
            _sliderY.SetValueWithoutNotify(vy);
            _sliderZ.SetValueWithoutNotify(vz);

            _eulerField.SetValueWithoutNotify(new Vector3Int(
                Mathf.RoundToInt(vx),
                Mathf.RoundToInt(vy),
                Mathf.RoundToInt(vz)
            ));
        }
    }
}
