using Experior.Core.Properties;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Experior.Plugin.Sample.Actuators.Motors.Limits.Vector
{
    [TypeConverter(typeof(ObjectConverter))]
    [Serializable, XmlInclude(typeof(VectorLimits)), XmlType(TypeName = "Experior.Plugin.Sample.Actuators.Motors.Limits.VectorLimits")]
    public class VectorLimits
    {
        #region Fields

        private float _min = 0.0f;
        private float _mid = 0.25f;
        private float _max = 0.5f;
        private float _tolerance = 0.05f;

        #endregion

        #region Events

        [XmlIgnore]
        public EventHandler LimitChanged;

        #endregion

        #region Properties

        public float Tolerance
        {
            get => _tolerance;
            set
            {
                if (value < 0)
                {
                    return;
                }

                _tolerance = value;
                LimitChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public float Min
        {
            get => _min;
            set
            {
                if (value >= _mid)
                {
                    return;
                }

                _min = value;
                LimitChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public float Mid
        {
            get => _mid;
            set
            {
                if (value >= _max || value <= _min)
                {
                    return;
                }

                _mid = value;
                LimitChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public float Max
        {
            get => _max;
            set
            {
                if (value <= _mid)
                {
                    return;
                }

                _max = value;
                LimitChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
