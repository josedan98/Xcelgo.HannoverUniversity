using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Xml.Serialization;
using Experior.Core.Collections.Translation;
using Experior.Core.Communication.PLC;
using Experior.Core.Properties;
using Experior.Core.Properties.TypeConverter;
using Experior.Interfaces;
using Experior.Interfaces.Collections;
using static Experior.Core.Reports.Statistics.Statistic;

namespace Experior.Catalog.Hannover.Motors
{
    public class Vector : Base, IElectricMotorTranslation
    {
        #region Fields

        private VectorInfo _info;

        private IMotorTranslationAssemblyCollection _assemblies = new AssemblyCollection();
        private IMotorTranslationCoordinateSystemCollection _coordinateSystems = new CoordinateSystemCollection();
        private IMotorTranslationPartCollection _parts = new PartCollection();

        private float _deltaMovement;
        
       

        #endregion

        #region Constructor

        public Vector(VectorInfo info) : base(info)
        {
            _info = info;

            if (info.outputMinLimit == null)
                info.outputMinLimit = new Output() { DataSize = DataSize.BOOL, Symbol = "Min. Limit" };

            if (info.outputMaxLimit == null)
                info.outputMaxLimit = new Output() { DataSize = DataSize.BOOL, Symbol = "Max. Limit" };


            Add(info.outputMinLimit);
            Add(info.outputMaxLimit);

            if (info.name == string.Empty)
                info.name = GetValidName("Hannover Motor");

            Core.Environment.Scene.OnLoaded += Scene_OnLoaded;

            AddSensorSignals();

            ControlSignals();
        }

        public void AddSensorSignals()
        {
            foreach (var sensor in Sensors)
            {
                Add(sensor.SensorOutput);
            }
        }

        #endregion

        #region Public Properties

        public List<Sensor> Sensors => _info.sensors;

        [Category("Sensors")]
        [DisplayName("Number of Sensors")]
        [RefreshProperties(RefreshProperties.All)]
        [PropertyOrder(1)]
        public int Amount
        {
            get => Sensors.Count;
            set
            {
                if (value < 0f)
                    return;

                while (Sensors.Count > value) 
                {
                    var sensorToRemove = Sensors[Sensors.Count - 1];
                    sensorToRemove.Dispose();
                    Remove(sensorToRemove.SensorOutput);
                    Sensors.Remove(sensorToRemove);
                }

                while (Sensors.Count < value)
                {
                    var sensor = new Sensor();
                    Sensors.Add(sensor);
                    Add(sensor.SensorOutput);
                }
            }
        }

        [Category("Movement")]
        [DisplayName("Offset")]
        [Description("Value used to activate limit PLC Input signals")]
        [PropertyOrder(1)]
        public float Offset
        {
            get => _info.offset;
            set
            {
                if (value < 0)
                    return;

                _info.offset = value;
            }
        }

        [Category("Movement")]
        [DisplayName("Max. Limit")]
        [PropertyOrder(2)]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        public float MaxLimit
        {
            get => _info.maxLimit;
            set
            {
                if (value <= MinLimit)
                    return;

                _info.maxLimit = value;
                Calibrate();
            }
        }

        [Category("Movement")]
        [DisplayName("Min. Limit")]
        [PropertyOrder(3)]
        [TypeConverter(typeof(FloatMeterToMillimeter))]
        public float MinLimit
        {
            get => _info.minLimit;
            set
            {
                if (value >= MaxLimit)
                    return;

                _info.minLimit = value;
            }
        }

        [Category("PLC Input Signals")]
        [DisplayName("Min. Limit")]
        [PropertyOrder(1)]
        public Output OutputMinLimit
        {
            get => _info.outputMinLimit;
            set => _info.outputMinLimit = value;
        }

        [Category("PLC Input Signals")]
        [DisplayName("Max. Limit")]
        [PropertyOrder(2)]
        public Output OutputMaxLimit
        {
            get => _info.outputMaxLimit;
            set => _info.outputMaxLimit = value;
        }

   


        [Browsable(false)]
        public float CurrentPosition
        {
            get => _info.currentPosition;
            private set => _info.currentPosition = value;
        }

        [Browsable(false)]
        public IMotorTranslationAssemblyCollection Assemblies => _assemblies;

        [Browsable(false)]
        public IMotorTranslationCoordinateSystemCollection CoordinateSystems => _coordinateSystems;

        [Browsable(false)]
        public IMotorTranslationPartCollection Parts => _parts;

        #endregion

        #region Protected Properties

        [Browsable(false)]
        protected Vector3 VectorDirection
        {
            get => _info.vectorDirection;
            set => _info.vectorDirection = value;
        }

        #endregion

        #region Public Methods

        public static Vector Create()
        {
            return Create(string.Empty);
        }

        public static Vector Create(string motorName)
        {
            var oldMotor = Items.Get(motorName);
            if (oldMotor is Vector surface)
                return surface;

            var motorInfo = new VectorInfo();
            if (motorName != string.Empty && !NameUsed(motorName))
                motorInfo.name = motorName;

            var motor = new Vector(motorInfo);
            Items.Add(motor);
            return motor;
        }

        public static Vector Create(VectorInfo motorInfo)
        {
            var oldMotor = Items.Get(motorInfo.name);
            if (oldMotor is Vector surface)
                return surface;

            var motor = new Vector(motorInfo);

            if (NameUsed(motor.Name) || motor.Name == string.Empty)
                motor.Name = IncrementName(motor.Name);

            Items.Add(motor);
            return motor;
        }

        public override void Step(float deltatime)
        {
            base.Step(deltatime);

            if (CurrentSpeed == 0f)
                return;

            //Distance UpdateOutput:
            _deltaMovement = deltatime * CurrentSpeed;
            CurrentPosition += _deltaMovement;

            Moving(_deltaMovement);
            ControlSignals();

            //Movement Constraints:
            LimitController();

            UpdateSensors();
        }

        private void UpdateSensors()
        {
            foreach (var sensor in Sensors) 
            {
                sensor.UpdateOutput(CurrentPosition);
            }
        }

        public override void Reset()
        {
            base.Reset();

            Calibrate();
        }

        #endregion

        #region Private Methods

        private void ControlSignals()
        {
            // Max. Position :
            if (CurrentPosition >= MaxLimit - Offset && !OutputMaxLimit.Active)
                OutputMaxLimit.On();
            else if (CurrentPosition < MaxLimit - Offset && OutputMaxLimit.Active)
                OutputMaxLimit.Off();

            // Min. Position :
            if (CurrentPosition <= MinLimit + Offset && !OutputMinLimit.Active)
                OutputMinLimit.On();
            else if (CurrentPosition > MinLimit + Offset && OutputMinLimit.Active)
                OutputMinLimit.Off();
        }

        private void Moving(float delta)
        {
            if (Parts != null)
            {
                foreach (var part in Parts.Items)
                    part.LocalPosition += VectorDirection * delta * _parts.Gears[part];
            }

            if (Assemblies != null)
            {
                foreach (var assembly in Assemblies.Items)
                    assembly.LocalPosition += VectorDirection * delta * _assemblies.Gears[assembly];
            }
        }

        private void Calibrate()
        {
            if (CurrentSpeed != 0f)
                StopBreak();

            Moving(-CurrentPosition);
            CurrentPosition = 0f;

            ControlSignals();
        }

        private void LimitController()
        {
            float limit;
            if (CurrentPosition >= MaxLimit && CurrentSpeed > 0f)
                limit = MaxLimit;
            else if (CurrentPosition <= MinLimit && CurrentSpeed < 0f)
                limit = MinLimit;
            else
                return;

            _deltaMovement = limit - CurrentPosition;
            CurrentPosition = limit;

            Moving(_deltaMovement);
            StopBreak();
        }

        private void Scene_OnLoaded()
        {
            Core.Environment.Scene.OnLoaded -= Scene_OnLoaded;

            Moving(CurrentPosition);
            ControlSignals();
        }

        #endregion
    }

    [Serializable, XmlInclude(typeof(VectorInfo)), XmlType(TypeName = "Experior.Catalog.Hannover.Motors.VectorInfo")]
    public class VectorInfo : BaseInfo
    {
       
        public float currentPosition;
        public Vector3 vectorDirection = Vector3.UnitX;

        public float maxLimit = 0.5f;
        public float minLimit = 0f;
        public float offset = 0.05f;

        public Output outputMaxLimit;
        public Output outputMinLimit;

        public List<Sensor> sensors { get; set; } = new List<Sensor>();
    }

    [Serializable, XmlInclude(typeof(Sensor)), XmlType("Experior.Catalog.Hannover.Motors.Sensor")]
    public sealed class Sensor : IDisposable
    {
        private bool disposedValue;

        public float Position {  get; set; }
        public float Range { get; set; }
        public Output SensorOutput { get; set; } = new Output { DataSize = DataSize.BOOL, SymbolName = "Sensor" };
        public void UpdateOutput(float position)
        {

            if (Position- (Range / 2) <= position && Position + (Range/ 2) >= position)
            {
                
                Log.Write($"{ToString()} is in range");
                SensorOutput.On();
            }
            else
            {
                SensorOutput.Off(); 
            }
            //Compare position with Position and range
        }

        public override string ToString()
        {
            return $"A sensor at position {Position}";
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SensorOutput?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
