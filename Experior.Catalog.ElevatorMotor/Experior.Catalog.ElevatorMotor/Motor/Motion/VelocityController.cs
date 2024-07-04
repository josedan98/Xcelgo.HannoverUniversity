using System;
using System.ComponentModel;
using Experior.Core.Mathematics;

namespace Experior.Plugin.Sample.Actuators.Motors.Motion
{
    /// <summary>
    /// Class <c>Motion</c> defines the current speed based on the target speed, acceleration and deceleration parameters.
    /// </summary>
    public class VelocityController
    {
        #region Fields

        private float _currentVelocity, _targetVelocity, _rampTime, _setPoint, _slope, _deltaSpeed;
        private float _rampUp, _rampDown;
        private bool _switchDirection, _plateau;

        #endregion

        #region Delegates

        public delegate void VelocityReachedDelegate(float velocity);

        public delegate void DirectionSwitchedDelegate();

        #endregion

        #region Events


        public event VelocityReachedDelegate VelocityReachedHandler;

        public event DirectionSwitchedDelegate DirectionSwitchedHandler;

        #endregion

        #region Public Properties

        [Browsable(false)]
        public float TargetVelocity
        {
            get => _targetVelocity;
            private set
            {
                if (float.IsInfinity(value))
                {
                    return;
                }

                _targetVelocity = value;
            }
        }

        [Browsable(false)]
        public float CurrentVelocity
        {
            get => _currentVelocity;
            private set => _currentVelocity = value;
        }

        [Browsable(false)]
        public bool Plateau => _plateau;

        [Browsable(false)]
        public float RampTime
        {
            get => _rampTime;
            private set => _rampTime = value;
        }

        [Browsable(false)]
        public float RampUp
        {
            get => _rampUp;
            private set
            {
                if (value <= 0 || float.IsInfinity(value))
                {
                    return;
                }

                _rampUp = value;
            }
        }

        [Browsable(false)]
        public float RampDown
        {
            get => _rampDown;
            private set
            {
                if (value <= 0 || float.IsInfinity(value))
                {
                    return;
                }

                _rampDown = value;
            }
        }

        [Browsable(false)]
        public float Slope
        {
            get => _slope;
            private set
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    _slope = 0f;
                }
                else
                {
                    _slope = value;
                }
            }
        }

        [Browsable(false)]
        public bool EnableAcceleration { get; set; } = true;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the Tolerance value which is used to identify if the <c>CurrentSpeed</c> is near to the <c>TargetVelocity</c>
        /// </summary>
        protected float SpeedTolerance => Math.Abs(_deltaSpeed);

        #endregion

        #region Public Methods

        /// <summary>
        /// This method steps the motor using <c>deltaTime</c> to calculate the new <c>CurrentVelocity</c> based on the acceleration/deceleration previously calculated.
        /// </summary>
        public float Step(float deltaTime)
        {
            if (_currentVelocity.IsEffectivelyEqual(TargetVelocity))
            {
                return TargetVelocity;
            }

            if (!EnableAcceleration || _rampTime.IsEffectivelyZero())
            {
                CurrentVelocity = TargetVelocity;
            }
            else
            {
                if (_switchDirection && _currentVelocity + SpeedTolerance >= 0 && _currentVelocity - SpeedTolerance <= 0)
                {
                    SwitchRamp();
                    DirectionSwitchedHandler?.Invoke();
                }

                _deltaSpeed = _slope * deltaTime;
                CurrentVelocity += _deltaSpeed;

                if (_currentVelocity + SpeedTolerance >= TargetVelocity && _currentVelocity - SpeedTolerance <= TargetVelocity)
                {
                    CurrentVelocity = TargetVelocity;

                    VelocityReachedHandler?.Invoke(TargetVelocity);
                }
                else
                {
                    _plateau = false;
                    return CurrentVelocity;
                }
            }

            _plateau = true;

            return CurrentVelocity;
        }

        /// <summary>
        /// This method calculates the acceleration value to use in <c>Step</c>.
        /// </summary>
        public void SetTargetSpeed(float targetSpeed, float rampUp, float rampDown)
        {
            TargetVelocity = targetSpeed;
            RampUp = rampUp;
            RampDown = rampDown;

            _switchDirection = false;
            // Upward Domain :
            if (CurrentVelocity.IsEffectivelyZero() && TargetVelocity > 0f || CurrentVelocity > 0f && TargetVelocity >= 0f)
            {
                RampTime = TargetVelocity < CurrentVelocity ? -RampDown : RampUp;
            }
            // Downward Domain :
            else if (CurrentVelocity.IsEffectivelyZero() && TargetVelocity < 0f || CurrentVelocity < 0f && TargetVelocity <= 0f)
            {
                RampTime = TargetVelocity < CurrentVelocity ? -RampUp : RampDown;
            }
            // Change Direction :
            else if (CurrentVelocity > 0 && TargetVelocity < 0 || CurrentVelocity < 0 && TargetVelocity > 0)
            {
                _switchDirection = true;
                RampTime = TargetVelocity < CurrentVelocity ? -RampDown : RampDown;
            }

            _setPoint = _switchDirection ? 0f : TargetVelocity;
            Slope = GetSlope(_setPoint);
        }

        /// <summary>
        /// This method resets the <c>CurrentVelocity</c>.
        /// </summary>
        public void Reset()
        {
            CurrentVelocity = 0f;
            Slope = 0f;
            TargetVelocity = 0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method returns the acceleration/deceleration value required to reach the <c>target</c> velocity.
        /// </summary>
        private float GetSlope(float target)
        {
            return Math.Abs(target - CurrentVelocity) / RampTime;
        }

        /// <summary>
        /// This method redefines the <c>RampTime</c> when a change of direction is performed.
        /// </summary>
        private void SwitchRamp()
        {
            _switchDirection = false;
            RampTime = TargetVelocity < CurrentVelocity ? -RampUp : RampUp;
            Slope = GetSlope(TargetVelocity);
        }

        #endregion
    }
}
