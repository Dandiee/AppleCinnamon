using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Xml.Linq;
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
            Lines = GetLines();
        }

        public bool Update(Camera camera)
        {
            var hasChanged = false;
            foreach (var action in Actions)
            {
                hasChanged |= action.Update(camera.LastKeyboardStateButForRealz, camera.CurrentKeyboardState);
            }

            if (hasChanged)
            {
                Lines = GetLines();
            }

            return hasChanged;
        }

        private string GetLines() => string.Join(Environment.NewLine, Actions.Select(s => s.Line));
    }

    public abstract class DebugLine
    {
        public string Line { get; protected set; }

        public virtual bool Update(KeyboardState prev, KeyboardState next)
        {
            return false;
        }
    }

    public class DebugInfoMultiLine<T> : DebugLine
    {
        private readonly Func<T, IEnumerable<string>> _lineBuilder;
        private readonly Func<T> _getter;
        public T Value { get; private set; }

        public DebugInfoMultiLine(Expression<Func<T>> fieldSelector, Func<T, IEnumerable<string>> lineBuilder)
        {
            _lineBuilder = lineBuilder;
            _getter = fieldSelector.Compile();
            SetLines();
        }

        public override bool Update(KeyboardState prev, KeyboardState current)
        {
            var newValue = _getter();
            if (!Equals(Value, newValue))
            {
                Value = newValue;
                SetLines();
                return true;
            }

            return false;
        }

        private void SetLines() => Line = string.Join(Environment.NewLine, _lineBuilder(Value));

    }

    public class DebugInfoLine<T> : DebugLine
    {
        private readonly string _unit;
        public string Name { get; }
        public T Value { get; private set; }

        public string Format { get; init; }

        private readonly Func<T> _getter;

        public DebugInfoLine(Expression<Func<T>> fieldSelector, string name = default, string unit = default)
        {
            _unit = unit;
            Name = name ?? (fieldSelector.Body as MemberExpression).Member.Name;
            _getter = fieldSelector.Compile();
            SetLine();
        }

        public override bool Update(KeyboardState prev, KeyboardState current)
        {
            var newValue = _getter();
            if (newValue != null && !newValue.Equals(Value))
            {
                Value = newValue;
                SetLine();
                return true;
            }

            return false;
        }

        private void SetLine() => Line = $"{Name}: {Value}{(string.IsNullOrEmpty(_unit) ? string.Empty : _unit)}";
    }

    public class DebugToggleAction : DebugLine
    {
        public string Name { get; }
        public Key Key { get; }

        private readonly Func<bool> _getter;
        private readonly Action<bool> _setter;

        public DebugToggleAction(Key key, Expression<Func<bool>> fieldSelector)
        {
            Name = (fieldSelector.Body as MemberExpression).Member.Name;
            Key = key;

            _getter = fieldSelector.Compile();
            _setter = GetSetter(fieldSelector);

            SetLine();
        }

        public override bool Update(KeyboardState prev, KeyboardState current)
        {
            if (prev.IsPressed(Key) && !current.IsPressed(Key))
            {
                _setter(!_getter());
                SetLine();
                return true;
            }

            return false;
        }

        private void SetLine() => Line = $"[{Key}] {Name} ({(_getter() ? "ON" : "OFF")})";

        private static Action<T> GetSetter<T>(Expression<Func<T>> expression)
        {
            var parameter = Expression.Parameter(typeof(T), "value");
            var body = Expression.Assign(expression.Body, parameter);
            var lambda = Expression.Lambda<Action<T>>(body, parameter);
            return lambda.Compile();
        }
    }

    public class DebugIncDecAction<T> : DebugLine
        where T : INumber<T>
    {
        private readonly T _step;
        private readonly Func<T, T> _callback;
        public string Name { get; }
        public Key Key { get; }
        public bool IsContinous { get; }
        public string Format { get; }

        public DebugIncDecAction(Key key, string name, T step, Func<T, T> callback, bool isContinous = true)
        {
            _step = step;
            _callback = callback;
            Name = name;
            Key = key;
            IsContinous = isContinous;
            Format = $"N{(1.0 / double.Parse(step.ToString())).ToString().Length - 1}";
            SetLine(_callback(default));
        }

        public override bool Update(KeyboardState prev, KeyboardState current)
        {
            if (current.IsPressed(Key))
            {
                if ((IsContinous && current.IsPressed(Key.Add)) || (!IsContinous && !current.IsPressed(Key.Add) && prev.IsPressed(Key.Add)))
                {
                    SetLine(_callback(+_step));
                    return true;
                }

                if ((IsContinous && current.IsPressed(Key.Subtract)) || (!IsContinous && !current.IsPressed(Key.Subtract) && prev.IsPressed(Key.Subtract)))
                {
                    SetLine(_callback(-_step));
                    return true;
                }

            }

            return false;
        }

        private void SetLine(T value) => Line = $"[{Key}] {Name}: {value.ToString(Format, CultureInfo.InvariantCulture)}";
    }
}
