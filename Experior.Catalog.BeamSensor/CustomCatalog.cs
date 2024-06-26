﻿using Experior.Core.Resources;
using System.Windows.Media;

namespace Experior.Catalog.Hannover
{
    public class CustomCatalog : Experior.Core.Catalog
    {
        public CustomCatalog() : base("Hannover")
        {
            Simulation = Experior.Core.Environment.Simulation.Events | Experior.Core.Environment.Simulation.Physics;

            Add(EmbeddedResource.GetImage("Custom Sensors"), "Elevator Component", "Sensor", Experior.Core.Environment.Simulation.Events | Experior.Core.Environment.Simulation.Physics, Create.BeamSensor);
           
        }

        public override ImageSource Logo => EmbeddedResource.GetImage("Logo");
    }

    internal static class EmbeddedResource
    {
        private static EmbeddedImageLoader Images { get; } = new Experior.Core.Resources.EmbeddedImageLoader();
        private static EmbeddedResourceLoader Resource { get; } = new Experior.Core.Resources.EmbeddedResourceLoader();

        public static ImageSource GetImage(string resourceFileName) => Images.Get(resourceFileName);
        public static Experior.Core.Resources.EmbeddedResource GetResource(string resourceFileName) => Resource.Get(resourceFileName);
    }
}