using System;

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
        public readonly T[] Faces;
        
        public T Top;
        public T Bottom;
        public T Left;
        public T Right;
        public T Front;
        public T Back;

        public Cube(T top, T bottom, T left, T right, T front, T back)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Front = front;
            Back = back;

            Faces = new[] { Top, Bottom, Left, Right, Front, Back };
        }

        public static Cube<T> CreateDefault(Func<T> factory)
        {
            return new(factory(), factory(), factory(), factory(), factory(), factory());
        }

        public void SetTop(T value)
        {
            Top = value;
            Faces[(byte)Face.Top] = value;
        }

        public void SetBottom(T value)
        {
            Bottom = value;
            Faces[(byte)Face.Bottom] = value;
        }

        public void SetLeft(T value)
        {
            Left = value;
            Faces[(byte)Face.Left] = value;
        }

        public void SetRight(T value)
        {
            Right = value;
            Faces[(byte)Face.Right] = value;
        }

        public void SetFront(T value)
        {
            Front = value;
            Faces[(byte)Face.Front] = value;
        }

        public void SetBack(T value)
        {
            Back = value;
            Faces[(byte)Face.Back] = value;
        }
    }
}
