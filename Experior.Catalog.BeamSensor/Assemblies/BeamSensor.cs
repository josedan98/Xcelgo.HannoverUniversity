using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Experior.Core.Communication.PLC;
using Experior.Core.Parts.Sensors;
using Experior.Interfaces;
using Experior.Core.Properties.TypeConverter;
using Experior.Core.Loads;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Numerics;

namespace Experior.Catalog.BeamSensor.Assemblies
{
    public class BeamSensor : Core.Assemblies.Assembly
    {
        #region FIELDS

        private Experior.Core.Parts.Sensors.Box _sensor;
        private bool _isSensorActive;
        private readonly BeamSensorInfo _info;
        //private bool SensorState { get; set; }
        #endregion
        public BeamSensor(BeamSensorInfo info) : base(info)
        {
            _info = info;
            _sensor = new Core.Parts.Sensors.Box(Colors.Blue, 0.5f, 2f,0.5f);

            InitializeInputsOutputs();

            Add(_sensor);
            _sensor.OnEnter += LoadOnEnter;
            _sensor.OnLeave += LoadOnLeave;
            
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

            ActivateInput = new Input()
            {
                DataSize = DataSize.BOOL,
                SymbolName = "Activate"
            };

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


        //[Category("PLC I/O")]
        //[DisplayName("ON")]
        //public Input ON { get; set; }

        //[Category("PLC I/O")]
        //[DisplayName("OFF")]
        //public Input OFF { get; set; }

        [Category("PLC I/O")]
        [DisplayName("Activate Beam Sensor")]
        public Input ActivateInput { get => _info.ActivateInput; set => _info.ActivateInput = value; }

        [Category("PLC I/O")]
        [DisplayName("Sensor Status")]
        public Output SensorOutput { get => _info.SensorOutput; set => _info.SensorOutput = value; }

        public override string Category => "Assembly";
        public override ImageSource Image => EmbeddedResource.GetImage("´BeamSensor");


        //private bool SensorState
        //{
        //    get => _info.SensorOutput; set => _info.SensorOutput= value;
        //}

    }
    [Serializable, XmlInclude(typeof(BeamSensorInfo)), XmlType(TypeName = "Experior.Catalog.BeamSensor.Assemblies.BeamSensorInfo")]
    public class BeamSensorInfo : Core.Assemblies.AssemblyInfo
    {
        
        public Output SensorOutput { get; set; }
        public Input ActivateInput { get; set; }
    }
}
