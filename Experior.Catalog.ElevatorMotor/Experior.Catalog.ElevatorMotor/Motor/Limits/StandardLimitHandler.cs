using System;
using Experior.Plugin.Sample.Actuators.Motors.Interfaces;
using Experior.Plugin.Sample.Actuators.Motors.Utilities;
//using Experior.Plugin.Sample.Actuators.Motors.Utilities;

namespace Experior.Plugin.Sample.Actuators.Motors.Limits.Vector
{
    public class StandardLimitHandler
    {
        #region Constructor

        public StandardLimitHandler(IElectricVectorMotor motor)
        {
            Motor = motor ?? throw new ArgumentNullException(nameof(motor));
        }

        #endregion

        #region Public Properties

        public bool Transitioning { get; protected set; }

        #endregion

        #region Protected Properties

        protected IElectricVectorMotor Motor { get; }

        #endregion

        #region Public Methods

        public virtual void AssessLimits()
        {
            LimitReached(true);
        }

        public virtual void Reset()
        {
        }

        #endregion

        #region Protected Methods

        protected virtual bool LimitReached(bool stop)
        {
            if (Motor.DistanceTraveled >= Motor.MaxLimit)
            {
                Motor.MoveToLimit(AuxiliaryData.VectorPositions.Up, stop);
            }
            else if (Motor.DistanceTraveled <= Motor.MinLimit)
            {
                Motor.MoveToLimit(AuxiliaryData.VectorPositions.Down, stop);
            }
            else
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
