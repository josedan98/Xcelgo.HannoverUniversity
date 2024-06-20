using Experior.Core.Assemblies;

namespace Experior.Catalog.BeamSensor
{
    public class Create
    {
        public static Assembly CustomAssembly(string group, string title, object properties)
        {
            var info = new Assemblies.CustomAssemblyInfo
            {
                name = Assembly.GetValidName("Custom Assembly")
            };
            return new Assemblies.CustomAssembly(info);
        }
        public static Assembly BeamSensor(string group, string title, object properties)
        {
            var info = new Assemblies.BeamSensorInfo
            {
                name= Assembly.GetValidName("BeamSensor")
            };
            return new Assemblies.BeamSensor(info);
        }
    }

 

    //}
}