using Experior.Core.Assemblies;
using Experior.Interfaces;
using System.Windows;
using System.Reflection;
using Experior.Core.Motors;
using Experior.Interfaces;
using Environment = Experior.Core.Environment;

namespace Experior.Catalog.ElevatorMotor
{
    public class Create
    {
        //public static Assembly CustomAssembly(string group, string title, object properties)
        //{
        //    var info = new Assemblies.CustomAssemblyInfo
        //    {
        //        name = Assembly.GetValidName("Custom Assembly")
        //    };
        //    return new Assemblies.CustomAssembly(info);
        //}

        public static Vector CreateBasicVectorMotor(VectorInfo info)
        {
            if (info == null)
            {
                info = new VectorInfo();
            }
        }
    }
}