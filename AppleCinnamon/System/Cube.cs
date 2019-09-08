using System;
using System.Collections.Generic;
using System.Linq;

namespace AppleCinnamon.System
{
    public enum Face
    {
        Top,
        Bottom,
        Left,
        Right,
        Front,
        Back
    }

    public sealed class Cube<T>
    {
        public T this[Face face]
        {
            get => Getters[face](this);
            set => Setters[face](this, value);
        }

        private static readonly IReadOnlyDictionary<Face, Action<Cube<T>, T>> Setters = new Dictionary<Face, Action<Cube<T>, T>>
        {
            [Face.Top] = (cube, value) => cube.Top = value,
            [Face.Bottom] = (cube, value) => cube.Bottom = value,
            [Face.Left] = (cube, value) => cube.Left = value,
            [Face.Right] = (cube, value) => cube.Right = value,
            [Face.Front] = (cube, value) => cube.Front = value,
            [Face.Back] = (cube, value) => cube.Back = value
        };

        private static readonly IReadOnlyDictionary<Face, Func<Cube<T>, T>> Getters = new Dictionary<Face, Func<Cube<T>, T>>
        {
            [Face.Top] = cube => cube.Top,
            [Face.Bottom] = cube => cube.Bottom,
            [Face.Left] = cube => cube.Left,
            [Face.Right] = cube => cube.Right,
            [Face.Front] = cube => cube.Front,
            [Face.Back] = cube => cube.Back
        };

        public T Top { get; set; }
        public T Bottom { get; set; }
        public T Left { get; set; }
        public T Right { get; set; }
        public T Front { get; set; }
        public T Back { get; set; }

        public Cube() { }
        public Cube(T top, T bottom, T left, T right, T front, T back)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Front = front;
            Back = back;
        }

        public static Cube<T> CreateDefault(Func<T> factory)
        {
            return new Cube<T>(factory(), factory(), factory(), factory(), factory(), factory());
        }

        public Cube<TNew> Transform<TNew>(Func<T, TNew> transformer)
        {
            return new Cube<TNew>(
                transformer(Top),
                transformer(Bottom),
                transformer(Left),
                transformer(Right),
                transformer(Front),
                transformer(Back));
        }

        public IEnumerable<KeyValuePair<Face, T>> GetAll()
        {
            return Getters.Select(g => new KeyValuePair<Face, T>(g.Key, g.Value(this)));
        }
    }
}
