using System.Numerics;
using System.Collections.Generic;
using Experior.Core.Assemblies;
using Experior.Core.Parts;
using Experior.Interfaces;

namespace Experior.Plugin.Sample.Actuators.Collections
{
    public class VectorAssemblyCollection
    {
        #region Fields

        private readonly Dictionary<Assembly, VectorModel> _items = new Dictionary<Assembly, VectorModel>();

        #endregion

        #region Public Properties

        public Dictionary<Assembly, VectorModel> Items => _items;

        #endregion

        #region Public Methods

        public void Add(Assembly assembly)
        {
            Add(assembly, 1);
        }

        public void Add(Assembly assembly, float gear)
        {
            if (!assembly.Configured)
            {
                Log.Write("You must add the assembly to an assembly before it can be controlled by a motor", System.Windows.Media.Colors.Red, LogFilter.Error);

                return;
            }

            _items.Remove(assembly);

            _items.Add(assembly, new VectorModel(assembly.LocalPosition) { Gear = gear });
        }

        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        /// Removes the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void Remove(Assembly assembly)
        {
            if (_items.ContainsKey(assembly))
            {
                _items.Remove(assembly);
            }
        }

        #endregion
    }

    public class VectorPartCollection
    {
        #region Fields

        private readonly Dictionary<RigidPart, VectorModel> _items = new Dictionary<RigidPart, VectorModel>();

        #endregion

        #region Public Properties

        public Dictionary<RigidPart, VectorModel> Items => _items;

        #endregion

        #region Public Methods

        public void Add(RigidPart part)
        {
            Add(part, 1);
        }

        public void Add(RigidPart part, float gear)
        {
            if (!part.Configured)
            {
                Log.Write("You must add the part to an assembly before it can be controlled by a motor", System.Windows.Media.Colors.Red, LogFilter.Error);

                return;
            }


            _items.Remove(part);

            _items.Add(part, new VectorModel(part.LocalPosition) { Gear = gear });
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void Remove(RigidPart part)
        {
            if (_items.ContainsKey(part))
            {
                _items.Remove(part);
            }
        }

        #endregion
    }

    public class VectorModel
    {
        public VectorModel(Vector3 syncLocalPosition)
        {
            SyncLocalPosition = syncLocalPosition;
        }

        public Vector3 SyncLocalPosition { get; }

        public float Gear { get; set; } = 1f;
    }
}
