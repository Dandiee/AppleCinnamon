using System;
using System.Linq;
using SharpDX.DirectInput;

namespace AppleCinnamon
{
    public class DebugContext
    {
        public DebugLine[] Actions;
        public string Lines { get; private set; }

        public DebugContext(params DebugLine[] lines)
        {
            Actions = lines;
            Lines = string.Empty;
        }

        public bool Update(Camera camera)
        {
            var hasChanged = false;
            foreach (var action in Actions)
            {
                hasChanged |= action.Update(camera.LastKeyboardState, camera.CurrentKeyboardState);
            }

            if (hasChanged)
            {
                Lines = string.Join(Environment.NewLine, Actions.Select(s => s.Line));
            }

            return hasChanged;
        }
    }

    public abstract class DebugLine
    {
        public string Line { get; protected set; }

        public virtual bool Update(KeyboardState prev, KeyboardState next)
        {
            return false;
        }
    }

  
    public class DebugIncDecAction : DebugLine
    {
        private readonly float _step;
        private readonly Func<float, float> _callback;
        public string Name { get; }
        public Key Key { get; }

        public string Format { get; set; } = "N4";

        public DebugIncDecAction(Key key, string name, float step, Func<float, float> callback)
        {
            _step = step;
            _callback = callback;
            Name = name;
            Key = key;

            GetLine(0);
        }

        public override bool Update(KeyboardState prev, KeyboardState current)
        {
            if (current.IsPressed(Key))
            {
                if (prev.IsPressed(Key.Add) && current.IsPressed(Key.Add))
                {
                    Line = GetLine(_callback(+_step));
                    return true;
                }
                
                if (prev.IsPressed(Key.Subtract) && current.IsPressed(Key.Subtract))
                {
                    Line = GetLine(_callback(-_step));
                    return true;
                }
                
            }

            return false;
        }

        private string GetLine(float value) => $"[{Key}] {Name}: {value.ToString(Format)}";
    }
}
