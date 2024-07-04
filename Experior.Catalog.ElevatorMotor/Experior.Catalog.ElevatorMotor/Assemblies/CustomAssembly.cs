using Experior.Core.Assemblies;
using System;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Experior.Catalog.ElevatorMotor.Assemblies
{
    public class CustomAssembly : Assembly
    {
        public CustomAssembly(CustomAssemblyInfo info)
            : base(info)
        {
        }

        public override string Category { get; } = "Assembly";

        public override ImageSource Image { get; } = EmbeddedResource.GetImage("CustomAssembly");
    }

    [Serializable, XmlInclude(typeof(CustomAssemblyInfo)), XmlType(TypeName = "Experior.Catalog.ElevatorMotor.Assemblies.CustomAssemblyInfo")]
    public class CustomAssemblyInfo : Experior.Core.Assemblies.AssemblyInfo
    {
    }
}