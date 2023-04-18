using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using AppleCinnamon.Graphics;

namespace AppleCinnamon;

public class DebugContext
{
    private readonly TextFormat _format;
    private readonly GraphicsContext _grfx;
    private readonly RawVector2 _origin;
    private TextLayout _textLayout;
    private readonly SolidColorBrush _brush;

    public DebugLine[] Actions;

    public string Lines { get; private set; }

    public DebugContext(TextFormat format, GraphicsContext grfx, RawVector2 origin, params DebugLine[] lines)
    {
        _brush = new SolidColorBrush(grfx.RenderTarget2D, Color.White);
        _format = format;
        _grfx = grfx;
        _origin = origin;
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

        if (hasChanged || _textLayout == null)
        {
            Lines = GetLines();

            _textLayout?.Dispose();
            _textLayout = new TextLayout(_grfx.DirectWrite, Lines, _format, _grfx.RenderForm.Width - 30, _grfx.RenderForm.Height);
        }
        return hasChanged;
    }

    public void Draw(Camera camera)
    {
        Update(camera);
        if (_textLayout != null)
        {
            _grfx.RenderTarget2D.DrawTextLayout(_origin, _textLayout, _brush);
        }

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

public abstract class DebugLine<T> : DebugLine
{
    protected static Action<T> GetSetter(Expression<Func<T>> expression)
    {
        var parameter = Expression.Parameter(typeof(T), "value");
        var body = Expression.Assign(expression.Body, parameter);
        var lambda = Expression.Lambda<Action<T>>(body, parameter);
        return lambda.Compile();
    }
}

public class DebugInfoMultiLine<T> : DebugLine<T>
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

public class DebugInfoLine<T> : DebugLine<T>
{
    private readonly string _unit;
    private readonly Func<T, string> _textFactory;
    public string Name { get; }
    public T Value { get; private set; }

    private readonly Func<T> _getter;

    public DebugInfoLine(Expression<Func<T>> fieldSelector, string name = default, string unit = default, Func<T, string> textFactory = default)
    {
        _unit = unit;
        _textFactory = textFactory;
        Name = name ?? (fieldSelector.Body as MemberExpression).Member.Name;
        _getter = fieldSelector.Compile();
        Value = _getter();
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

    private void SetLine() => Line = _textFactory?.Invoke(Value) ?? $"{Name}: {Value}{(string.IsNullOrEmpty(_unit) ? string.Empty : _unit)}";
}

public class DebugToggleAction : DebugLine<bool>
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
}

public class DebugAction : DebugLine
{
    private readonly Action _callback;
    public string Name { get; }
    public Key Key { get; }

    public DebugAction(Key key, string name, Action callback)
    {
        _callback = callback;
        Name = name;
        Key = key;

        Line = $"[{Key}] {Name}";
    }

    public override bool Update(KeyboardState prev, KeyboardState current)
    {
        if (prev.IsPressed(Key) && !current.IsPressed(Key))
        {
            _callback();
            return true;
        }

        return false;
    }
}

public class DebugIncDecAction<T> : DebugLine<T>
    where T : INumber<T>
{
    private readonly T _step;
    private readonly Action _callback;
    public string Name { get; }
    public Key Key { get; }
    public bool IsContinous { get; }
    public string Format { get; }

    private readonly Func<T> _getter;
    private readonly Action<T> _setter;

    private readonly Action<T> _increment;
    private readonly Action<T> _decrement;

    public DebugIncDecAction(Key key, Expression<Func<T>> fieldSelector, T step, Action callback = default, bool isContinous = true, Action<T> increment = default, Action<T> decrement = default)
    {
        _step = step;
        _callback = callback;

        _getter = fieldSelector.Compile();
        _setter = GetSetter(fieldSelector);

        _increment = increment;
        _decrement = decrement;

        Name = (fieldSelector.Body as MemberExpression).Member.Name;

        Key = key;
        IsContinous = isContinous;
        Format = $"N{(1.0 / double.Parse(step.ToString())).ToString().Length - 1}";
        SetLine(_getter());
    }

        
    public override bool Update(KeyboardState prev, KeyboardState current)
    {
        if (current.IsPressed(Key))
        {
            if ((IsContinous && current.IsPressed(Key.Add)) || (!IsContinous && !current.IsPressed(Key.Add) && prev.IsPressed(Key.Add)))
            {
                if (_increment != null)
                {
                    _increment(_step);
                }
                else
                {
                    var newValue = _getter() + _step;
                    _setter(newValue);
                }

                SetLine(_getter());
                _callback?.Invoke();
                return true;
            }

            if ((IsContinous && current.IsPressed(Key.Subtract)) || (!IsContinous && !current.IsPressed(Key.Subtract) && prev.IsPressed(Key.Subtract)))
            {
                if (_decrement != null)
                {
                    _decrement(_step);
                }
                else
                {
                    var newValue = _getter() - _step;
                    _setter(newValue);
                }

                SetLine(_getter());
                _callback?.Invoke();
                return true;
            }

        }

        return false;
    }

    private void SetLine(T value) => Line = $"[{Key}] {Name}: {value.ToString(Format, CultureInfo.InvariantCulture)}";
}