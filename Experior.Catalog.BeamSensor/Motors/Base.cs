using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using Experior.Catalog.Hannover.Assemblies;
using Experior.Core.Communication.PLC;
using Experior.Core.Properties;
using Experior.Core.Properties.TypeConverter;
using Experior.Interfaces;

namespace Experior.Catalog.Hannover.Motors
{
    public abstract class Base : Core.Motors.Electric
    {
        #region Fields

        private BaseInfo _info;

        private bool _move;
        private float _ramp;
        private float _slope;
        private float _deltaSpeed;
        private float _deviation;
        private bool _flagSwitch;

        private float _targetSpeed;
        private Utility.Commands _command;

        #endregion

        #region Constructor

        internal Base(BaseInfo info) : base(info)
        {
            _info = info;

            if (info.inputForward == null)
                info.inputForward = new Input() { DataSize = DataSize.BOOL, Description = "Move Forward", Symbol = "Forward" };

            if (info.inputBackward == null)
                info.inputBackward = new Input() { DataSize = DataSize.BOOL, Description = "Move Backward", Symbol = "Backward" };

            if (info.inputAlternativeSpeed == null)
                info.inputAlternativeSpeed = new Input() { DataSize = DataSize.BOOL, Description = "Use Alternative Speed", Symbol = "Alternative Speed" };

            Add(info.inputForward);
            Add(info.inputBackward);
            Add(info.inputAlternativeSpeed);

            info.inputForward.On += InputForward_On;
            info.inputForward.Off += InputForward_Off;

            info.inputBackward.On += InputBackward_On;
            info.inputBackward.Off += InputBackward_Off;

            info.inputAlternativeSpeed.OnReceived += InputAlternativeSpeed_OnReceived;
        }

        #endregion

        #region Public Properties

        [Display(Order = 1, GroupName = "Speed")]
        [DisplayName("Base Speed")]
        [PropertyOrder(1)]
        [TypeConverter(typeof(MeterPerSeconds))]
        public virtual float BaseSpeed
        {
            get => _info.baseSpeed;
            set
            {
                if (value <= 0)
                    return;

                _info.baseSpeed = value;

                if (CurrentSpeed != 0f)
                    Controller();
            }
        }

        [Display(Order = 1, GroupName = "Speed")]
        [DisplayName("Alternative Speed")]
        [PropertyOrder(2)]
        [TypeConverter(typeof(MeterPerSeconds))]
        public virtual float AlternativeSpeed
        {
            get => _info.alternativeSpeed;
            set
            {
                if (value <= 0)
                    return;

                _info.alternativeSpeed = value;

                if (CurrentSpeed != 0f)
                    Controller();
            }
        }

        [Category("Acceleration / Deceleration")]
        [DisplayName("Use Ramp")]
        [PropertyOrder(3)]
        public bool UseRamp
        {
            get => _info.useRamp;
            set
            {
                _info.useRamp = value;
                Core.Environment.Properties.Refresh();
            }
        }

        [Category("Acceleration / Deceleration")]
        [DisplayName("Ramp up")]
        [PropertyOrder(4)]
        [TypeConverter(typeof(MilliSeconds))]
        [PropertyAttributesProvider("DynamicPropertyAcceleration")]
        public float RampUp
        {
            get => _info.rampUp;
            set
            {
                if (value <= 0)
                    return;

                _info.rampUp = value;
            }
        }

        [Category("Acceleration / Deceleration")]
        [DisplayName("Ramp down")]
        [PropertyOrder(5)]
        [TypeConverter(typeof(MilliSeconds))]
        [PropertyAttributesProvider("DynamicPropertyAcceleration")]
        public float RampDown
        {
            get => _info.rampDown;
            set
            {
                if (value <= 0)
                    return;

                _info.rampDown = value;
            }
        }

        [Category("PLC Output Signals")]
        [DisplayName("Forward")]
        [PropertyOrder(1)]
        public Input InputForward
        {
            get => _info.inputForward;
            set => _info.inputForward = value;
        }

        [Category("PLC Output Signals")]
        [DisplayName("Backward")]
        [PropertyOrder(2)]
        public Input InputBackward
        {
            get => _info.inputBackward;
            set => _info.inputBackward = value;
        }

        [Category("PLC Output Signals")]
        [DisplayName("Alternative Speed")]
        [PropertyOrder(3)]
        public Input InputAlternativeSpeed
        {
            get => _info.inputAlternativeSpeed;
            set => _info.inputAlternativeSpeed = value;
        }

        [Browsable(false)]
        public override float Speed { get => base.Speed; set => base.Speed = value; }

        #endregion

        #region Protected Properties

        [Browsable(false)]
        protected internal bool Move
        {
            get => _move;
            set
            {
                _move = value;

                if (CurrentSpeed == 0f)
                {
                    if (value)
                        InvokeBeginStart();
                    else
                        InvokeStopped();
                }
            }
        }

        protected internal Utility.Commands Command
        {
            get => _command;
            set
            {
                _command = value;

                if (CurrentSpeed != 0f)
                    Controller();
            }
        }

        #endregion

        #region Public Methods

        public override void Step(float deltatime)
        {
            if (!_move)
                return;

            _deviation = Math.Abs(_deltaSpeed);
            if (UseRamp)
            {
                if (_flagSwitch && CurrentSpeed + _deviation >= 0 && CurrentSpeed - _deviation <= 0)
                    SwitchRamp();

                _deltaSpeed = _slope * deltatime;
                CurrentSpeed += _deltaSpeed;
            }
            else
                CurrentSpeed = _targetSpeed;

            // Arrow UpdateOutput :
            if (CurrentSpeed >= 0)
                SetForward();
          
            else
                SetBackward();

            // Stable Speed :
            if (CurrentSpeed + _deviation >= _targetSpeed && CurrentSpeed - _deviation <= _targetSpeed)
            {
                CurrentSpeed = _targetSpeed;
                Move = false;

                UpdateColor(_targetSpeed == 0f ? Colors.Red : Colors.Green);
            }
            else
                UpdateColor(Colors.Orange);
        }

        public override void Start()
        {
            // In case of using the Motor menu...
            if (Command == Utility.Commands.Stop)
                Command = Utility.Commands.Forward;

            Controller();
        }

        public override void Forward()
        {
            Command = Utility.Commands.Forward;
        }

        public override void Backward()
        {
            Command = Utility.Commands.Backward;
        }

        public override void Stop()
        {
            Command = Utility.Commands.Stop;
        }

        public override void StopBreak()
        {
            base.StopBreak();

            _move = false;
            _flagSwitch = false;
        }

        public override void SwitchDirection()
        {
            switch (Command)
            {
                case Utility.Commands.Forward:
                    Backward();
                    break;
                case Utility.Commands.Backward:
                    Forward();
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();
            StopBreak();
        }

        public override void Dispose()
        {
            base.Dispose();

            _info.inputForward.On -= InputForward_On;
            _info.inputForward.Off -= InputForward_Off;

            _info.inputBackward.On -= InputBackward_On;
            _info.inputBackward.Off -= InputBackward_Off;

            _info.inputAlternativeSpeed.OnReceived -= InputAlternativeSpeed_OnReceived;
        }

        public void DynamicPropertyAcceleration(PropertyAttributes attributes)
        {
            attributes.IsBrowsable = UseRamp;
        }

        #endregion

        #region Protected Methods

        protected virtual void Controller()
        {
            _targetSpeed = InputAlternativeSpeed.Active ? AlternativeSpeed : BaseSpeed;
            _targetSpeed *= (int)_command;

            // Forward Domain :
            if (CurrentSpeed == 0f && _targetSpeed > 0f || CurrentSpeed > 0f && _targetSpeed >= 0f)
                _ramp = _targetSpeed < CurrentSpeed ? -RampDown : RampUp;
            // Backward Domain :
            else if (CurrentSpeed == 0f && _targetSpeed < 0f || CurrentSpeed < 0f && _targetSpeed <= 0f)
                _ramp = _targetSpeed < CurrentSpeed ? -RampUp : RampDown;
            // Change Direction :
            else if (CurrentSpeed > 0 && _targetSpeed < 0 || CurrentSpeed < 0 && _targetSpeed > 0)
            {
                _flagSwitch = true;
                _ramp = _targetSpeed < CurrentSpeed ? -RampDown : RampDown;
            }

            var setPoint = _flagSwitch ? 0f : _targetSpeed;
            if (_ramp != 0f)
            {
                _ramp /= 1000f;
                _slope = Math.Abs(setPoint - CurrentSpeed) / _ramp;
            }

            if (_targetSpeed != CurrentSpeed)
                Move = true;
        }

        private void SwitchRamp()
        {
            _flagSwitch = false;
            _ramp = _targetSpeed < CurrentSpeed ? -RampUp : RampUp;

            if (_ramp != 0f)
            {
                _ramp /= 1000f;
                _slope = Math.Abs(_targetSpeed - CurrentSpeed) / _ramp;
            }
        }

        #endregion

        #region Private Methods

        private void InputForward_On(Input sender)
        {
            if (InputBackward.Active)
                return;

            Forward();
            Start();
        }

        private void InputForward_Off(Input sender)
        {
            if (!InputBackward.Active)
                Stop();
        }

        private void InputBackward_On(Input sender)
        {
            if (InputForward.Active)
                return;

            Backward();
            Start();
        }

        private void InputBackward_Off(Input sender)
        {
            if (!InputForward.Active)
                Stop();
        }

        private void InputAlternativeSpeed_OnReceived(Input sender, object value)
        {
            switch (InputForward.Active)
            {
                case true when !InputBackward.Active:
                    Forward();
                    break;
                case false when InputBackward.Active:
                    Backward();
                    break;
            }
        }

        #endregion
    }

    [Serializable, XmlInclude(typeof(BaseInfo)), XmlType(TypeName = "Experior.Catalog.Hannover.Motors.BaseInfo")]
    public class BaseInfo : Core.Motors.ElectricInfo
    {
        public float baseSpeed = 0.3f;
        public float alternativeSpeed = 0.1f;

        public float rampUp = 300f;
        public float rampDown = 300f;
        public bool useRamp = true;

        public Input inputForward;
        public Input inputBackward;
        public Input inputAlternativeSpeed;
    }
}
