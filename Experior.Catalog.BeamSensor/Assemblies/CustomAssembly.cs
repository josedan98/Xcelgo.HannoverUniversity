using Experior.Core.Assemblies;
using System;
using System.Windows.Media;
using System.Xml.Serialization;
using Experior.Core.Properties.TypeConverter;
namespace Experior.Catalog.BeamSensor.Assemblies
{
    public class CustomAssembly : Assembly
    {
        private Experior.Core.Parts.Sensors.Box _sensor;
        public CustomAssembly(CustomAssemblyInfo info)
            : base(info)
        {
            _sensor = new Experior.Core.Parts.Sensors.Box(Colors.Blue, 0.5f, 0.5f, 3);
            Add(_sensor);
        }

        public override string Category { get; } = "Assembly";

        public override ImageSource Image { get; } = EmbeddedResource.GetImage("CustomAssembly");
    }

    [Serializable, XmlInclude(typeof(CustomAssemblyInfo)), XmlType(TypeName = "Experior.Catalog.BeamSensor.Assemblies.CustomAssemblyInfo")]
    public class CustomAssemblyInfo : Experior.Core.Assemblies.AssemblyInfo
    {
    }
}