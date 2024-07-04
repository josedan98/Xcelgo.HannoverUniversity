using System;
using System.Xml.Serialization;

namespace Experior.Plugin.Sample.Actuators.Motors.Utilities
{
    [Serializable, XmlInclude(typeof(AuxiliaryData)), XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Utilities.AuxiliaryData")]
    public static class AuxiliaryData
    {
        [XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Utilities.AuxiliaryData.Commands")]
        public enum Commands
        {
            Upward = 1,
            Downward = -1,
            Stop = 0
        }

        [XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Utilities.AuxiliaryData.MotorTypes")]
        public enum MotorTypes
        {
           
            Vector
          
        }

        [XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Utilities.AuxiliaryData.VectorPositions")]
        public enum VectorPositions
        {
            Down,
            Middle,
            Up
        }

        [XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Utilities.AuxiliaryData.VectorMovementLimits")]
        public enum VectorMovementLimits
        {
            Stop,
            Eccentric
        }

       
        
    }
}
