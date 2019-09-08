using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public class DataflowContext<TPayload> : DataflowContext
    {
        
        public TPayload Payload { get; }
        
        public DataflowContext(DataflowContext previousStep, TPayload payload, long time, string name)
            :base(previousStep, time, name)
        {
            Payload = payload;
        }

        public DataflowContext(DataflowContext previousStep, TPayload payload)
            : base(previousStep)
        {
            Payload = payload;
        }

        public DataflowContext(TPayload payload, Device device)
            : base(device)
        {
            Payload = payload;
        }

    }

    public class DataflowContext
    {
        public Dictionary<string, long> Debug { get; }
        public Device Device { get; }

        public DataflowContext(DataflowContext previousStep, long time, string name)
            : this(previousStep)
        {
            Debug.Add(name, time);
        }

        public DataflowContext(DataflowContext previousStep)
        {
            Device = previousStep.Device;
            Debug = previousStep.Debug.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public DataflowContext(Device device)
        {
            Device = device;
            Debug = new Dictionary<string, long>();
        }
    }
}