using System;
using System.Numerics;
using System.ComponentModel;
using System.Xml.Serialization;
using Experior.Interfaces;
using Experior.Core.Mathematics;
using Experior.Core.Properties;
using Experior.Core.Communication.PLC;
using Experior.Core.Properties.TypeConverter;
using Experior.Plugin.Sample.Actuators.Collections;
using Experior.Plugin.Sample.Actuators.Motors.Utilities;
using Experior.Plugin.Sample.Actuators.Motors.Interfaces;
using Experior.Plugin.Sample.Actuators.Motors.Motion;
using Experior.Plugin.Sample.Actuators.Motors.Limits.Vector;
using Experior.Catalog.ElevatorMotor.Motor.Experior.Plugin.Sample.Actuators.Motors.Basic;

namespace Experior.Plugin.Sample.Actuators.Motors.Basic
{
    public class Vector : Base, IElectricVectorMotor
    {
        #region Fields

        private readonly VectorInfo _info;

        private readonly LinearMotion _linearMotion;
        private StandardLimitHandler _limitHandler;


        private bool _forwardActivated;

        #endregion

        #region Constructor

        public Vector(VectorInfo info) : base(info)
        {
            _info = info;

            if (_info.Limits == null)
            {
                _info.Limits = new VectorLimits();
            }
            _info.Limits.LimitChanged += OnLimitChanged;

            SetPlcSignals();

            _linearMotion = new LinearMotion(this);
            SetLimitHandler();

            UpdateLimitSignals();
        }

        #endregion

        #region Public Properties

        [Browsable(false)]
        public float DistanceTraveled
        {
            get => _info.DistanceTraveled;
            private set => _info.DistanceTraveled = value;
        }

        [Browsable(false)]
        public Vector3 TranslationDirection
        {
            get => _info.TranslationDirection;
            set
            {
                _info.TranslationDirection = value;
                Reset();
            }
        }

        [Category("Movement")]
        [DisplayName("Automatic Limit")]
        [PropertyOrder(0)]
        public AuxiliaryData.VectorMovementLimits VectorMovementLimit
        {
            get => _info.VectorMovementLimit;
            set
            {
                if (_info.VectorMovementLimit == value)
                {
                    return;
                }

                _info.VectorMovementLimit = value;

                InvokeCalibrate();
                SetLimitHandler();
            }
        }

        [Category("Movement")]
        [DisplayName("Tolerance")]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        [PropertyOrder(1)]
        public float Tolerance
        {
            get => _info.Limits.Tolerance;
            set
            {
                if (_info.Limits.Tolerance.IsEffectivelyEqual(value))
                {
                    return;
                }

                _info.Limits.Tolerance = value;
                InvokeCalibrate();
            }
        }

        [Category("Movement")]
        [DisplayName("Max.")]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        [PropertyOrder(2)]
        public float MaxLimit
        {
            get => _info.Limits.Max;
            set
            {
                if (_info.Limits.Max.IsEffectivelyEqual(value))
                {
                    return;
                }

                _info.Limits.Max = value;
                InvokeCalibrate();
            }
        }

        [Category("Movement")]
        [DisplayName("Mid.")]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        [PropertyOrder(3)]
        public float MidLimit
        {
            get => _info.Limits.Mid;
            set
            {
                if (_info.Limits.Mid.IsEffectivelyEqual(value))
                {
                    return;
                }

                _info.Limits.Mid = value;
                InvokeCalibrate();
            }
        }

        [Category("Movement")]
        [DisplayName("Min.")]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        [PropertyOrder(4)]
        public float MinLimit
        {
            get => _info.Limits.Min;
            set
            {
                if (_info.Limits.Min.IsEffectivelyEqual(value))
                {
                    return;
                }

                _info.Limits.Min = value;
                InvokeCalibrate();
            }
        }

        [Category("Movement")]
        [DisplayName("Reset Position")]
        [PropertyOrder(5)]
        public AuxiliaryData.VectorPositions ResetPosition
        {
            get => _info.ResetPosition;
            set
            {
                if (_info.ResetPosition == value)
                {
                    return;
                }

                _info.ResetPosition = value;
                InvokeCalibrate();
            }
        }

        [Category("PLC Input")]
        [DisplayName("Max. Limit")]
        [PropertyOrder(4)]
        public Output OutputMaxLimit
        {
            get => _info.OutputMaxLimit;
            set => _info.OutputMaxLimit = value;
        }

        [Category("PLC Input")]
        [DisplayName("Mid. Limit")]
        [PropertyOrder(5)]
        public Output OutputMidLimit
        {
            get => _info.OutputMidLimit;
            set => _info.OutputMidLimit = value;
        }

        [Category("PLC Input")]
        [DisplayName("Min. Limit")]
        [PropertyOrder(6)]
        public Output OutputMinLimit
        {
            get => _info.OutputMinLimit;
            set => _info.OutputMinLimit = value;
        }

        [Browsable(false)]
        public VectorPartCollection Parts { get; } = new VectorPartCollection();

        [Browsable(false)]
        public VectorAssemblyCollection Assemblies { get; } = new VectorAssemblyCollection();

        #endregion

        #region Public Methods

        public override void Step(float deltatime)
        {
            base.Step(deltatime);

            if (!Running)
            {
                return;
            }

            float delta = CurrentSpeed * deltatime;
            DistanceTraveled += delta;

            _linearMotion.Translate(delta);
            UpdateLimitSignals();
            _limitHandler.AssessLimits();
        }

        public override void StopBreak()
        {
            _forwardActivated = false;
            _limitHandler?.Reset();

            base.StopBreak();
        }

        public override void Reset()
        {
            if (Experior.Core.Environment.Scene.Loading)
            {
                return;
            }

            base.Reset();

            _limitHandler?.Reset();
            _linearMotion.Sync();
            DistanceTraveled = 0f;
        }

        public void Calibrate()
        {
            if (Experior.Core.Environment.Scene.Loading)
            {
                return;
            }

            _forwardActivated = false;
            MoveToLimit(ResetPosition, true);
            _limitHandler?.Reset();
        }

        public void InvokeCalibrate()
        {
            Core.Environment.Invoke(Calibrate);
        }

        public void MoveToLimit(AuxiliaryData.VectorPositions position, bool stop)
        {
            if (stop && Running)
            {
                StopBreak();
            }

            float delta = DistanceTraveled;
            switch (position)
            {
                case AuxiliaryData.VectorPositions.Up:

                    delta -= MaxLimit;
                    DistanceTraveled = MaxLimit;

                    break;

                case AuxiliaryData.VectorPositions.Middle:

                    delta -= MidLimit;
                    DistanceTraveled = MidLimit;

                    break;

                default:

                    delta -= MinLimit;
                    DistanceTraveled = MinLimit;

                    break;
            }

            if (Encoder.Running)
            {
                Encoder.Distance = DistanceTraveled * 1000f;
            }

            _linearMotion.Translate(-delta);
            UpdateLimitSignals();
        }

        public void RestorePosition()
        {
            _linearMotion.Translate(_info.DistanceTraveled);
            UpdateLimitSignals();
        }

        public void UpdateLimitSignals()
        {
            if (DistanceTraveled >= MaxLimit - Tolerance && !OutputMaxLimit.Active)
            {
                OutputMaxLimit.On();
            }
            else if (DistanceTraveled < MaxLimit - Tolerance && OutputMaxLimit.Active)
            {
                OutputMaxLimit.Off();
            }

            if (DistanceTraveled >= MidLimit - Tolerance && DistanceTraveled <= MidLimit + Tolerance && !OutputMidLimit.Active)
            {
                OutputMidLimit.On();
            }
            else if ((DistanceTraveled < MidLimit - Tolerance || DistanceTraveled > MidLimit + Tolerance) && OutputMidLimit.Active)
            {
                OutputMidLimit.Off();
            }

            if (DistanceTraveled <= MinLimit + Tolerance && !OutputMinLimit.Active)
            {
                OutputMinLimit.On();
            }
            else if (DistanceTraveled > MinLimit + Tolerance && OutputMinLimit.Active)
            {
                OutputMinLimit.Off();
            }
        }

        public void ResetLimitControlSignals()
        {
            OutputMinLimit.Off();
            OutputMidLimit.Off();
            OutputMaxLimit.Off();
        }

        public override void Dispose()
        {
            _info.Limits.LimitChanged -= OnLimitChanged; // DOUBLE CHECK THIS !

            base.Dispose();
        }

        #endregion

        #region Protected Methods

        protected override string GetMotorName() => GetValidName("Basic Vector Motor ");

        protected override void Controller()
        {
            switch (VectorMovementLimit)
            {
                case AuxiliaryData.VectorMovementLimits.Stop:

                    if (!((OutputMinLimit.Active && InputDownward.Active) || (OutputMaxLimit.Active && InputUpward.Active)))
                    {
                        base.Controller();
                    }

                    break;

                case AuxiliaryData.VectorMovementLimits.Eccentric:

                    TargetSpeed = InputAlternativeSpeed.Active ? AlternativeSpeed : Speed;

                    switch (_limitHandler.Transitioning)
                    {
                        case false when Command != AuxiliaryData.Commands.Stop && LastSpeed < 0 && _forwardActivated:
                            TargetSpeed *= -1;
                            break;

                        case false when Command != AuxiliaryData.Commands.Stop && LastSpeed > 0 && !_forwardActivated:
                            TargetSpeed *= 1;
                            break;

                        default:
                            TargetSpeed *= (int)Command;
                            break;
                    }

                    if (!TargetSpeed.IsEffectivelyZero())
                    {
                        _forwardActivated = InputUpward.Active;
                    }

                    Motion.SetTargetSpeed(TargetSpeed, RampUp / 1000, RampDown / 1000);
                    Move = true;

                    break;
            }
        }

        protected override void ExecuteSceneOnLoaded()
        {
            ResetLimitControlSignals();
            RestorePosition();
        }

        #endregion

        #region Private Methods

        private void SetPlcSignals()
        {
            if (_info.OutputMaxLimit == null)
            {
                _info.OutputMaxLimit = new Output() { DataSize = DataSize.BOOL, Symbol = "Max. Limit" };
            }

            if (_info.OutputMidLimit == null)
            {
                _info.OutputMidLimit = new Output() { DataSize = DataSize.BOOL, Symbol = "Mid. Limit" };
            }

            if (_info.OutputMinLimit == null)
            {
                _info.OutputMinLimit = new Output() { DataSize = DataSize.BOOL, Symbol = "Min. Limit" };
            }

            Add(_info.OutputMaxLimit);
            Add(_info.OutputMidLimit);
            Add(_info.OutputMinLimit);
        }

        private void SetLimitHandler()
        {
            if (VectorMovementLimit == AuxiliaryData.VectorMovementLimits.Stop)
            {
                _limitHandler = new StandardLimitHandler(this);
            }
        }

        private void OnLimitChanged(object sender, EventArgs e)
        {
            Reset();
        }

        #endregion
    }

    [Serializable, XmlInclude(typeof(VectorInfo)), XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Basic.VectorInfo")]
    public class VectorInfo : BaseInfo
    {
        public float DistanceTraveled { get; set; }

        public Vector3 TranslationDirection { get; set; } = Vector3.UnitZ;

        public AuxiliaryData.VectorPositions ResetPosition { get; set; } = AuxiliaryData.VectorPositions.Down;

        public AuxiliaryData.VectorMovementLimits VectorMovementLimit { get; set; } = AuxiliaryData.VectorMovementLimits.Stop;

        public VectorLimits Limits { get; set; }

        public Output OutputMaxLimit { get; set; }

        public Output OutputMidLimit { get; set; }

        public Output OutputMinLimit { get; set; }
    }
}