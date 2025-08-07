using CounterStrikeSharp.API.Core;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CS2_SimpleAdminApi;

public struct QAngle_t : IAdditionOperators<QAngle_t, QAngle_t, QAngle_t>,
        ISubtractionOperators<QAngle_t, QAngle_t, QAngle_t>,
        IMultiplyOperators<QAngle_t, float, QAngle_t>,
        IDivisionOperators<QAngle_t, float, QAngle_t>
{
    public float X, Y, Z;

    public const int SIZE = 3;

    public unsafe float this[int i]
    {
        readonly get
        {
            if (i < 0 || i > SIZE)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* ptr = &this)
            {
                return Unsafe.Read<float>(Unsafe.Add<float>(ptr, i));
            }
        }
        set
        {
            if (i < 0 || i > SIZE)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* ptr = &this)
            {
                Unsafe.Write(Unsafe.Add<float>(ptr, i), value);
            }
        }
    }

    public QAngle_t()
    {
    }

    public unsafe QAngle_t(nint ptr) : this(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<float>((void*)ptr), SIZE))
    {
    }
    
    public QAngle_t(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public QAngle_t(ReadOnlySpan<float> values)
    {
        if (values.Length < SIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        this = Unsafe.ReadUnaligned<QAngle_t>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
    }

    public unsafe (Vector_t fwd, Vector_t right, Vector_t up) AngleVectors()
    {
        Vector_t fwd = default, right = default, up = default;

        nint pFwd = (nint)Unsafe.AsPointer(ref fwd);
        nint pRight = (nint)Unsafe.AsPointer(ref right);
        nint pUp = (nint)Unsafe.AsPointer(ref up);

        fixed (void* ptr = &this)
        {
            NativeAPI.AngleVectors((nint)ptr, pFwd, pRight, pUp);
        }

        return ( fwd, right, up );
    }

    public unsafe void AngleVectors(out Vector_t fwd, out Vector_t right, out Vector_t up)
    {
        fixed (void* ptr = &this, pFwd = &fwd, pRight = &right, pUp = &up)
        {
            NativeAPI.AngleVectors((nint)ptr, (nint)pFwd, (nint)pRight, (nint)pUp);
        }
    }

    public readonly override string ToString()
    {
        return $"{X:n2} {Y:n2} {Z:n2}";
    }

    public static QAngle_t operator +(QAngle_t a, QAngle_t b)
    {
        return new QAngle_t(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static QAngle_t operator -(QAngle_t a, QAngle_t b)
    {
        return new QAngle_t(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static QAngle_t operator -(QAngle_t a)
    {
        return new QAngle_t(-a.X, -a.Y, -a.Z);
    }

    public static QAngle_t operator *(QAngle_t a, float b)
    {
        return new QAngle_t(a.X * b, a.Y * b, a.Z * b);
    }

    public static QAngle_t operator /(QAngle_t a, float b)
    {
        return new QAngle_t(a.X / b, a.Y / b, a.Z / b);
    }
}