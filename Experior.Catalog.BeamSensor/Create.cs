using Experior.Core.Assemblies;

namespace Experior.Catalog.Hannover
{
    public class Create
    {
       
        public static Assembly BeamSensor(string group, string title, object properties)
        {
            var info = new Assemblies.BeamSensorInfo
            {
                name= Assembly.GetValidName("Hannover Motor")
            };
            return new Assemblies.BeamSensor(info);
        }
    }

 

    //}
}