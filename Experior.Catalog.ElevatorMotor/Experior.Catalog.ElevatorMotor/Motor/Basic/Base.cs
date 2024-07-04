
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Serialization;
//using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using Experior.Interfaces;
using Experior.Core.Properties;
using Experior.Core.Communication.PLC;
using Experior.Core.Mathematics;
using Experior.Core.Motors;
using Experior.Core.Properties.TypeConverter;
//using Experior.Plugin.Sample.Actuators.Motors.Parts;
using Experior.Plugin.Sample.Actuators.Motors.Interfaces;
using Experior.Plugin.Sample.Actuators.Motors.Motion;
using Experior.Plugin.Sample.Actuators.Motors.Utilities;
//using Encoder = Experior.Plugin.Sample.Actuators.Motors.Parts.Encoder;
using Experior.Catalog.ElevatorMotor.Motor.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Experior.Catalog.ElevatorMotor.Motor
{
   

    namespace Experior.Plugin.Sample.Actuators.Motors.Basic
    {
        /// <summary>
        /// Abstract class <c>Base</c> contains common class members and behaviors to recreate a Upward/Downward motor functionality.
        /// </summary>
        public abstract class Base : Electric, IBaseMotor
        {
            #region Fields

            private readonly BaseInfo _info;
            private bool _move;
            private AuxiliaryData.Commands _command;

            #endregion

            #region Constructor

            protected Base(BaseInfo info) : base(info)
            {
                _info = info;

                OnNameChanged += (sender, args) => info.name = Name; //TODO: CHECK THIS !

                SetInfoMotorName();

                Motion = new VelocityController { EnableAcceleration = info.UseRamp };
                _command = AuxiliaryData.Commands.Upward;

                SetPlcSignals();

                Experior.Core.Environment.Scene.OnLoaded += SceneOnLoaded;
                _info.MechanicalSwitch.State.On();
            }

            private void SceneOnLoaded()
            {
                Core.Environment.Scene.OnLoaded -= SceneOnLoaded;

                ExecuteSceneOnLoaded();
            }


            #endregion

            #region Public Properties

            /// <summary>
            /// Gets or sets the Alternative speed. 
            /// </summary>
            [Display(Order = 1, GroupName = "Speed")]
            [DisplayName(@"Alternative Speed")]
            [TypeConverter(typeof(MeterPerSeconds))]
            [PropertyOrder(2)]
            public virtual float AlternativeSpeed
            {
                get => _info.AlternativeSpeed;
                set
                {
                    if (value <= 0)
                    {
                        return;
                    }

                    _info.AlternativeSpeed = value;
                    if (Running)
                    {
                        Controller();
                    }
                }
            }

            /// <summary>
            /// Enables/Disables the use of Ready PLC Input signal.
            /// </summary>
            [Category("PLC Input")]
            [DisplayName(@"Mechanical Switch")]
            [PropertyOrder(1)]
            public bool MechanicalSwitchEnabled
            {
                get => _info.MechanicalSwitch.Enabled;
                set
                {
                    _info.MechanicalSwitch.Enabled = value;

                    Core.Environment.Properties.Refresh();
                }
            }

            /// <summary>
            /// Gets or sets the Ready PLC Input signal.
            /// </summary>
            [Category("PLC Input")]
            [DisplayName(@"Ready")]
            [PropertyAttributesProvider("DynamicPropertyReady")]
            [PropertyOrder(2)]
            public Output OutputReady
            {
                get => _info.MechanicalSwitch.State;
                set => _info.MechanicalSwitch.State = value;
            }

            /// <summary>
            /// Gets or sets the Plateau PLC Input signal.
            /// This instance indicates if the <c>CurrentVelocity</c> of the motor is different from zero.
            /// </summary>
            [Category("PLC Input")]
            [DisplayName(@"Running")]
            [PropertyOrder(3)]
            public Output OutputRunning
            {
                get => _info.OutputRunning;
                set => _info.OutputRunning = value;
            }

            /// <summary>
            /// Gets or sets the Upward PLC Input signal.
            /// This instance move the motor in Upward direction when its value is true.
            /// </summary>
            [Category("PLC Output")]
            [DisplayName(@"Upward")]
            [PropertyOrder(1)]
            public Input InputUpward
            {
                get => _info.InputUpward;
                set => _info.InputUpward = value;
            }

            /// <summary>
            /// Gets or sets the Downward PLC Input signal.
            /// This instance move the motor in Downward direction when its value is true.
            /// </summary>
            [Category("PLC Output")]
            [DisplayName(@"Downward")]
            [PropertyOrder(2)]
            public Input InputDownward
            {
                get => _info.InputDownward;
                set => _info.InputDownward = value;
            }

            /// <summary>
            /// Gets or sets the Alternative Speed PLC Input signal.
            /// This instance sets the Alternative Speed as the new Target Speed.
            /// </summary>
            [Category("PLC Output")]
            [DisplayName(@"Alternative Speed")]
            [PropertyOrder(3)]
            public Input InputAlternativeSpeed
            {
                get => _info.InputAlternativeSpeed;
                set => _info.InputAlternativeSpeed = value;
            }

            /// <summary>
            /// Gets or sets Encoder.
            /// </summary>
            [Category("Encoder")]
            [DisplayName(@"Properties")]
            public virtual Encoder Encoder => _info.Encoder;

            [Display(Order = 1, GroupName = "Speed")]
            [DisplayName(@"Speed")]
            [TypeConverter(typeof(MeterPerSeconds))]
            [PropertyOrder(1)]
            public override float Speed
            {
                get => _info.Speed;
                set
                {
                    if (value <= 0)
                    {
                        return;
                    }

                    _info.Speed = value;
                    if (Running)
                    {
                        Controller();
                    }
                }
            }

            /// <summary>
            /// Gets or sets the use of ramp for acceleration and deceleration states.
            /// </summary>
            [Display(Order = 1, GroupName = "Acceleration/Deceleration")]
            [DisplayName(@"Enabled")]
            [PropertyOrder(3)]
            public virtual bool UseRamp
            {
                get => _info.UseRamp;
                set
                {
                    _info.UseRamp = value;

                    if (Motion != null)
                    {
                        Motion.EnableAcceleration = value;
                    }

                    Core.Environment.Properties.Refresh();
                }
            }

            /// <summary>
            /// Gets or sets the ramp value implemented in acceleration.
            /// </summary>
            [Display(Order = 1, GroupName = "Acceleration/Deceleration")]
            [DisplayName(@"Ramp Up")]
            [PropertyAttributesProvider("DynamicPropertyAcceleration")]
            [TypeConverter(typeof(MilliSeconds))]
            [PropertyOrder(4)]
            public virtual float RampUp
            {
                get => _info.RampUp;
                set
                {
                    if (Float.IsEffectivelyZero(RampDown) && value.IsEffectivelyZero())
                    {
                        UseRamp = false;
                        return;
                    }

                    if (value < 0)
                    {
                        return;
                    }

                    _info.RampUp = value;
                }
            }

            /// <summary>
            /// Gets or sets the ramp value implemented in deceleration.
            /// </summary>
            [Display(Order = 1, GroupName = "Acceleration/Deceleration")]
            [DisplayName(@"Ramp Down")]
            [PropertyAttributesProvider("DynamicPropertyAcceleration")]
            [TypeConverter(typeof(MilliSeconds))]
            [PropertyOrder(5)]
            public virtual float RampDown
            {
                get => _info.RampDown;
                set
                {
                    if (RampUp.IsEffectivelyZero() && value.IsEffectivelyZero())
                    {
                        UseRamp = false;
                        return;
                    }

                    if (value < 0)
                    {
                        return;
                    }

                    _info.RampDown = value;
                }
            }

            /// <summary>
            /// Instance <c>Motion</c> takes care of the calculations related to the motion of the motor taking into account acceleration and deceleration.
            /// </summary>
            [Browsable(false)]
            public VelocityController Motion { get; }

            /// <summary>
            /// Indicates if the motor is running.
            /// </summary>
            public override bool Running => !CurrentSpeed.IsEffectivelyZero();

            /// <summary>
            /// Gets or sets Current Speed.
            /// </summary>
            [Browsable(false)]
            public override float CurrentSpeed
            {
                get => base.CurrentSpeed;
                protected set
                {
                    base.CurrentSpeed = value;

                    if (!value.IsEffectivelyZero()) //TODO: SUBSCRIBE TO CURRENT SPEED ON CHANGED TO AVOID SONAR ISSUE?
                    {
                        LastSpeed = value;
                    }
                }
            }

            /// <summary>
            /// Gets the direction in which the motor is running.
            /// </summary>
            [Browsable(false)]
            public override MotorDirection Direction =>
                Command == AuxiliaryData.Commands.Upward || Command == AuxiliaryData.Commands.Stop
                    ? MotorDirection.Upward
                    : MotorDirection.Downward;

            /// <summary>
            /// Gets or sets the Target Speed the motor must reach.
            /// </summary>
            protected internal float TargetSpeed { get; internal set; }

            /// <summary>
            /// Gets or sets the Last Target Speed.
            /// </summary>
            protected internal float LastSpeed { get; internal set; }

            /// <summary>
            /// Instance <c>Move</c> allows the motion of the motor and notifies the Belt when it is about to start or stop.
            /// </summary>
            protected internal bool Move
            {
                get => _move;
                internal set
                {
                    _move = value;

                    if (!CurrentSpeed.IsEffectivelyZero())
                    {
                        return;
                    }

                    // Notifies the PhysX engine about it !
                    if (value)
                    {
                        InvokeBeginStart();
                        InvokeStarted();
                    }
                    else
                    {
                        InvokeStopped();
                    }
                }
            }

            /// <summary>
            /// Instance <c>Command</c> indicates the command to execute.
            /// </summary>
            protected internal AuxiliaryData.Commands Command
            {
                get => _command;
                set
                {
                    _command = value;
                    ExecuteCommandOperation();
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Method called when the user selects the Assembly and used right click.
            /// </summary>
            public override List<Core.Environment.UI.Toolbar.BarItem> ShowContextMenu()
            {
                var menu = new List<Core.Environment.UI.Toolbar.BarItem>();
                if (_info.MechanicalSwitch.Enabled && _info.MechanicalSwitch.State.Active)
                {
                    menu.Add(new Core.Environment.UI.Toolbar.Button("Off", EmbeddedResource.GetImage("MechanicalSwitch_Off"))
                    {
                        OnClick = (sender, args) =>
                        {
                            _info.MechanicalSwitch.State.Off();
                            StopBreak();
                        }
                    });
                }
                else if (_info.MechanicalSwitch.Enabled && !_info.MechanicalSwitch.State.Active)
                {
                    menu.Add(new Core.Environment.UI.Toolbar.Button("On", EmbeddedResource.GetImage("MechanicalSwitch_On"))
                    {
                        OnClick = (sender, args) => _info.MechanicalSwitch.State.On()
                    });
                }

                return menu;
            }

            /// <summary>
            /// Method called by the Physics Engine every time the engine steps.
            /// The method is automatically called by Experior if the motor is not embedded.
            /// </summary>
            public override void Step(float deltatime)
            {
                if (Encoder.Enabled && Running)
                {
                    Encoder.Step(deltatime, CurrentSpeed);
                }

                if (!_info.MechanicalSwitch.State.Active)
                {
                    return;
                }

                if (!Move)
                {
                    return;
                }

                CurrentSpeed = Motion.Step(deltatime);

                if (Motion.Plateau)
                {
                    Move = false;
                    UpdateColor(TargetSpeed.IsEffectivelyZero() ? Colors.Red : Colors.Green);
                }
                else
                {
                    UpdateColor(Colors.Orange);
                }

                SetArrowDirection(CurrentSpeed);

                MotorStatus();
            }

            /// <summary>
            /// Method called when the user resets the scene.
            /// </summary>
            public override void Reset()
            {
                if (InputUpward.Id >= 0 || InputDownward.Id >= 0)
                {
                    return;
                }

                Encoder?.Reset();

                base.Reset();
            }

            /// <summary>
            /// Sets the motor abruptly.
            /// </summary>
            public override void StopBreak()
            {
                base.StopBreak();

                MotorStatus();
            }

            public override void Dispose()
            {
                _info.InputUpward.OnReceived -= InputForwardBackwardReceived;
                _info.InputDownward.OnReceived -= InputForwardBackwardReceived;

                _info.InputAlternativeSpeed.OnReceived -= InputAlternativeSpeedReceived;

                Encoder?.Dispose();

                base.Dispose();
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void DynamicPropertyReady(PropertyAttributes attributes)
            {
                attributes.IsBrowsable = _info.MechanicalSwitch.Enabled;
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public void DynamicPropertyMotorType(PropertyAttributes attributes)
            {
                attributes.IsBrowsable = this is Surface;
            }

            #endregion

            #region Protected Methods

            /// <summary>
            /// Sets the Target speed and updates the Motion controller.
            /// </summary>
            protected virtual void Controller()
            {
                TargetSpeed = InputAlternativeSpeed.Active ? AlternativeSpeed : Speed;
                TargetSpeed *= (int)Command;

                Motion.SetTargetSpeed(TargetSpeed, RampUp / 1000f, RampDown / 1000f);
                Move = true;
            }

            /// <summary>
            /// Executed when the scene is loaded.
            /// </summary>
            protected abstract void ExecuteSceneOnLoaded();

            #endregion

            #region Private Methods

            private void SetPlcSignals()
            {
                if (_info.Encoder == null)
                {
                    _info.Encoder = new Encoder { IncrementsPrDistance = 0.1f };
                }

                Add(_info.Encoder.InputReset);
                Add(_info.Encoder.OutputPulse);
                Add(_info.Encoder.InputStart);

                _info.Encoder.InitCommunication(); // Subscribes to signal events !

                if (_info.MechanicalSwitch == null)
                {
                    _info.MechanicalSwitch = new MechanicalSwitch();
                }

                if (_info.MechanicalSwitch.State == null)
                {
                    _info.MechanicalSwitch.State = new Output { DataSize = DataSize.BOOL, Description = "Ready Signal", Symbol = "Ready" };
                }

                Add(_info.MechanicalSwitch.State);

                if (_info.OutputRunning == null)
                {
                    _info.OutputRunning = new Output { DataSize = DataSize.BOOL, Description = "Motor moving", Symbol = "Running" };
                }

                Add(_info.OutputRunning);

                if (_info.InputUpward == null)
                {
                    _info.InputUpward = new Input { DataSize = DataSize.BOOL, Description = "Move Upward", Symbol = "Upward" };
                }

                if (_info.InputDownward == null)
                {
                    _info.InputDownward = new Input { DataSize = DataSize.BOOL, Description = "Move Downward", Symbol = "Downward" };
                }

                if (_info.InputAlternativeSpeed == null)
                {
                    _info.InputAlternativeSpeed = new Input { DataSize = DataSize.BOOL, Description = "Use Alternative Speed", Symbol = "Alternative Speed" };
                }

                Add(_info.InputUpward);
                Add(_info.InputDownward);
                Add(_info.InputAlternativeSpeed);

                _info.InputUpward.OnReceived += InputForwardBackwardReceived;
                _info.InputDownward.OnReceived += InputForwardBackwardReceived;

                _info.InputAlternativeSpeed.OnReceived += InputAlternativeSpeedReceived;
            }

            /// < summary>
            /// This method takes care of feedback signals in relation to the status of the motor.
            /// </summary>
            protected virtual void MotorStatus()
            {
                switch (Running)
                {
                    case true when !OutputRunning.Active:
                        OutputRunning.On();
                        break;
                    case false when OutputRunning.Active:
                        OutputRunning.Off();
                        break;
                }
            }

            private void InputForwardBackwardReceived(Input sender, object value)
            {
                // Upward:
                if (sender == InputUpward)
                {
                    if (InputUpward.Active)
                    {
                        ExecuteForwardCommand();
                    }
                    else
                    {
                        if (!InputDownward.Active)
                        {
                            Stop();
                        }
                        else
                        {
                            ExecuteBackwardCommand();
                        }
                    }
                }
                // Downward:
                else if (sender == InputDownward)
                {
                    if (InputDownward.Active)
                    {
                        ExecuteBackwardCommand();
                    }
                    else
                    {
                        if (!InputUpward.Active)
                        {
                            Stop();
                        }
                        else
                        {
                            ExecuteForwardCommand();
                        }
                    }
                }
            }

            private void InputAlternativeSpeedReceived(Input sender, object value)
            {
                if (InputUpward.Active && Command == AuxiliaryData.Commands.Upward)
                {
                    Upward();
                }
                else if (InputDownward.Active && Command == AuxiliaryData.Commands.Downward)
                {
                    Downward();
                }
            }

            private void ExecuteForwardCommand()
            {
                if (Command == AuxiliaryData.Commands.Upward && Move)
                {
                    return;
                }

                Upward();

                if (!Running)
                {
                    Start();
                }
            }

            private void ExecuteBackwardCommand()
            {
                if (Command == AuxiliaryData.Commands.Downward && Move)
                {
                    return;
                }

                Downward();

                if (!Running)
                {
                    Start();
                }
            }

            #endregion


            /// <summary>
            /// Starts the motor in the direction specified previously.
            /// </summary>
            public override void Start()
            {
                // Started from context menu
                if (Command == AuxiliaryData.Commands.Stop)
                {
                    Command = LastSpeed >= 0f ? AuxiliaryData.Commands.Upward : AuxiliaryData.Commands.Downward;
                }

                Controller();
            }

            /// <summary>
            /// Sets the direction of the motor to move Upward.
            /// </summary>
            public override void upwa ()
            {
                Command = AuxiliaryData.Commands.Upward;
            }

            /// <summary>
            /// Sets the direction of the motor to move Downward.
            /// </summary>
            public override void Forward()
            {
                Command = AuxiliaryData.Commands.Downward;
            }

            /// <summary>
            /// Stops the motor considering acceleration/deceleration if enabled.
            /// </summary>
            public override void Stop()
            {
                Command = AuxiliaryData.Commands.Stop;
            }

            /// <summary>
            /// Sets the opposite direction to the one defined previously.
            /// </summary>
            public override void SwitchDirection()
            {
                switch (Command)
                {
                    case AuxiliaryData.Commands.Upward:
                        Downward();
                        break;
                    case AuxiliaryData.Commands.Downward:
                        Upward();
                        break;
                }
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public virtual void DynamicPropertyAcceleration(PropertyAttributes attributes)
            {
                attributes.IsBrowsable = _info.UseRamp;
            }

            protected abstract string GetMotorName();

            protected virtual void ExecuteCommandOperation()
            {
                if (Running)
                {
                    Controller();
                }
            }

            protected virtual void SetArrowDirection(float value)
            {
                if (value >= 0f)
                {
                    SetUpward();
                }
                else
                {
                    SetDownward();
                }
            }

            private void SetInfoMotorName()
            {
                if (string.IsNullOrEmpty(_info.name))
                {
                    _info.name = GetMotorName();
                }
            }
        }

        [Serializable, XmlInclude(typeof(BaseInfo)), XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Basic.BaseInfo")]
        public class BaseInfo : ElectricInfo
        {
            public float Speed { get; set; } = 0.3f;

            public bool UseRamp { get; set; } = true;

            public float RampUp { get; set; } = 300f;

            public float RampDown { get; set; } = 300f;

            public float AlternativeSpeed { get; set; } = 0.1f;

            public MechanicalSwitch MechanicalSwitch { get; set; }

            public Encoder Encoder { get; set; }

            public Output OutputRunning { get; set; }

            public Input InputUpward { get; set; }

            public Input InputDownward { get; set; }

            public Input InputAlternativeSpeed { get; set; }
        }
    }

}
