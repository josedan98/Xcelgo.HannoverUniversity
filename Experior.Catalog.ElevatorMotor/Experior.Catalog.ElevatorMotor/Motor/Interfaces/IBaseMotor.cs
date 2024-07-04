using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Experior.Interfaces;
using Experior.Plugin.Sample.Actuators.Motors.Motion;



namespace Experior.Catalog.ElevatorMotor.Motor.Interfaces
{
        public interface IBaseMotor : IElectricMotor
        {
            VelocityController Motion { get; }

            void StopBreak();
        }
    }

