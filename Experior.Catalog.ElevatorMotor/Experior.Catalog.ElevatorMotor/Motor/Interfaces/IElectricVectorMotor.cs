using Experior.Core.Communication.PLC;
using Experior.Interfaces;
using Experior.Plugin.Sample.Actuators.Motors.Utilities;

namespace Experior.Plugin.Sample.Actuators.Motors.Interfaces
{
    public interface IElectricVectorMotor : IBaseVectorMotor
    {
        AuxiliaryData.VectorMovementLimits VectorMovementLimit { get; set; }

        AuxiliaryData.VectorPositions ResetPosition { get; set; }

        Output OutputMaxLimit { get; set; }

        Output OutputMidLimit { get; set; }

        Output OutputMinLimit { get; set; }

        float MaxLimit { get; set; }

        float MidLimit { get; set; }

        float MinLimit { get; set; }

        void MoveToLimit(AuxiliaryData.VectorPositions position, bool stop);
    }
}
