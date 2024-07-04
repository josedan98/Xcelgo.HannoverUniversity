using System.Numerics;
using Experior.Catalog.ElevatorMotor.Motor.Interfaces;
using Experior.Plugin.Sample.Actuators.Collections;

namespace Experior.Plugin.Sample.Actuators.Motors.Interfaces
{
    public interface IBaseVectorMotor : IBaseMotor
    {
        VectorPartCollection Parts { get; }

        VectorAssemblyCollection Assemblies { get; }

        float DistanceTraveled { get; }

        Vector3 TranslationDirection { get; set; }

        void InvokeCalibrate();
    }
}
