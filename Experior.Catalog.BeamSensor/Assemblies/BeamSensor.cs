using Experior.Core.Communication.PLC;
using Experior.Core.Parts;
using Experior.Core.Parts.Sensors;
using Experior.Core.Properties.TypeConverter;
using Experior.Core.Properties;
using Experior.Interfaces;
using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Experior.Catalog.Hannover.Assemblies
{
    public class BeamSensor : Core.Assemblies.Assembly
    {
        #region FIELDS

        private Experior.Core.Parts.Sensors.Box _sensor;
        private bool _isSensorActive;
        private readonly BeamSensorInfo _info;
        private Experior.Core.Parts.Box Box1, Box2;
        

        #endregion
        public BeamSensor(BeamSensorInfo info) : base(info)
        {
            _info = info;
            _sensor = new Core.Parts.Sensors.Box(Colors.SeaGreen, 6f, 0.09f, 0.09f);
            Box1 = new Core.Parts.Box(Colors.DarkGray, 0.4f, 0.7f, 0.4f);
            Box2 = new Core.Parts.Box(Colors.DarkGray, 0.4f, 0.7f, 0.4f);

            Add(_sensor);
            Add(Box1);
            Add(Box2);
            

            _sensor.OnEnter += LoadOnEnter;
            _sensor.OnLeave += LoadOnLeave;
            InitializeInputsOutputs();
            InvokeRefresh();
        }
        #region Public Methods

        public override void Refresh()
        {
            //Sensor length is changed
            //The boxes shouyld be repositioned
            _sensor.Length = Length;
            Box1.LocalPosition = new Vector3(-(_sensor.Length + Box1.Length) / 2 , 0, 0);
            Box2.LocalPosition = new Vector3((_sensor.Length + Box2.Length) / 2, 0, 0);
        }
        private void ActivateSensor(bool value)
        {
            _isSensorActive = value;

            _sensor.Visible = _isSensorActive;


        }

        private void LoadOnEnter(Sensor sensor, object trigger)
        {
            if (!_isSensorActive)
            {
                return;
            }
            SensorOutput.Send(true);
        }


        private void ActivateInput_OnReceived(Input sender, object value)
        {
            if (value is bool boolValue)
            {
                ActivateSensor(boolValue);
            }
        }
        private void LoadOnLeave(Sensor sensor, object trigger)
        {

            if (_sensor.Loads.Any())
            {
                return;
            }
            SensorOutput.Send(false);
        }

        private void InitializeInputsOutputs()
        {
            //ON = new Input()
            //{
            //    DataSize = DataSize.BOOL,
            //    SymbolName = "ON"
            //};
            if (ActivateInput == null) 
            {
                ActivateInput = new Input()
                {
                    DataSize = DataSize.BOOL,
                    SymbolName = "Activate"
                };
            }
           

            if (SensorOutput == null)
            {
                SensorOutput = new Output()
                {
                    DataSize = DataSize.BOOL,
                    SymbolName = "SensorActive"
                };
            }


            //ON.OnReceived += ON_O;
            //DownInput.OnReceived += DownInput_OnReceived;
            ActivateInput.OnReceived += ActivateInput_OnReceived;

            Add(ActivateInput);
            Add(SensorOutput);
        }
        #endregion

        #region Private Methods
        //private void AttachJoints()
        //{
        //    //Joint0.LocalPosition = Vector3.Zero;
        //    ////Joint1.LocalYaw = (float)Math.PI / 2;
        //    //Joint1.LocalPosition = new Vector3(0, _sensor.Length, 0);
        //    //Box1.LocalPosition = Vector3.Zero;
        //    //Box2.LocalPosition=  new Vector3(0, _sensor.Length , 0);


        //}

        #endregion

        //[Category("PLC I/O")]
        //[DisplayName("ON")]
        //public Input ON { get; set; }

        //[Category("PLC I/O")]
        //[DisplayName("OFF")]
        //public Input OFF { get; set; }


        [Category("Size")]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        [DisplayName("Length")]
        [PropertyOrder(0)]
        public float Length
        {
            get => _info.length;
            set
            {
                if (value <= 0f)
                    return;

                _info.length = value;
                InvokeRefresh();
            }
        }

        [Category("PLC I/O")]
        [DisplayName("Activate Beam Sensor")]
        public Input ActivateInput { get => _info.ActivateInput; set => _info.ActivateInput = value; }

        [Category("PLC I/O")]
        [DisplayName("Sensor Status")]
        public Output SensorOutput { get => _info.SensorOutput; set => _info.SensorOutput = value; }

        public override string Category => "Assembly";
        public override ImageSource Image => EmbeddedResource.GetImage("´BeamSensor");


    }
    [Serializable, XmlInclude(typeof(BeamSensorInfo)), XmlType(TypeName = "Experior.Catalog.Hannover.Assemblies.BeamSensorInfo")]
    public class BeamSensorInfo : Core.Assemblies.AssemblyInfo
    {

        public Output SensorOutput { get; set; }
        public Input ActivateInput { get; set; }
    }
}
