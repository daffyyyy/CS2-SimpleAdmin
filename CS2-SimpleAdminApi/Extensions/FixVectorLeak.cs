using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using CounterStrikeSharp.API.Modules.Utils;

namespace FixVectorLeak;

public struct Vector_t : IAdditionOperators<Vector_t, Vector_t, Vector_t>,
        ISubtractionOperators<Vector_t, Vector_t, Vector_t>,
        IMultiplyOperators<Vector_t, float, Vector_t>,
        IDivisionOperators<Vector_t, float, Vector_t>
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

    public Vector_t()
    {
    }

    public unsafe Vector_t(nint ptr) : this(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<float>((void*)ptr), SIZE))
    {
    }

    public Vector_t(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector_t(ReadOnlySpan<float> values)
    {
        if (values.Length < SIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        this = Unsafe.ReadUnaligned<Vector_t>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
    }

    public readonly float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public readonly float Length2D()
    {
        return (float)Math.Sqrt(X * X + Y * Y);
    }

    public readonly bool IsZero(float tolerance = 0.0001f)
    {
        return Math.Abs(X) <= tolerance && Math.Abs(Y) <= tolerance && Math.Abs(Z) <= tolerance;
    }

    public void Scale(float scale)
    {
        X *= scale;
        Y *= scale;
        Z *= scale;
    }

    public readonly override string ToString()
    {
        return $"{X:n2} {Y:n2} {Z:n2}";
    }

    public static Vector_t operator +(Vector_t a, Vector_t b)
    {
        return new Vector_t(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector_t operator -(Vector_t a, Vector_t b)
    {
        return new Vector_t(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector_t operator -(Vector_t a)
    {
        return new Vector_t(-a.X, -a.Y, -a.Z);
    }

    public static Vector_t operator *(Vector_t a, float b)
    {
        return new Vector_t(a.X * b, a.Y * b, a.Z * b);
    }

    public static Vector_t operator /(Vector_t a, float b)
    {
        return new Vector_t(a.X / b, a.Y / b, a.Z / b);
    }
}

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

        return (fwd, right, up);
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

public unsafe static class Extensions
{
    public static void Teleport(this CBaseEntity entity, Vector_t? position = null, QAngle_t? angles = null, Vector_t? velocity = null)
    {
        Guard.IsValidEntity(entity);

        void* pPos = null, pAng = null, pVel = null;

        // Structs are stored on the stack, GC should not break pointers.

        if (position.HasValue)
        {
            var pos = position.Value; // Remove nullable wrapper
            pPos = &pos;
        }

        if (angles.HasValue)
        {
            var ang = angles.Value;
            pAng = &ang;
        }

        if (velocity.HasValue)
        {
            var vel = velocity.Value;
            pVel = &vel;
        }

        VirtualFunction.CreateVoid<IntPtr, IntPtr, IntPtr, IntPtr>(entity.Handle, GameData.GetOffset("CBaseEntity_Teleport"))(entity.Handle, (nint)pPos,
            (nint)pAng, (nint)pVel);
    }

    public static (Vector_t fwd, Vector_t right, Vector_t up) AngleVectors(this QAngle vec) => vec.ToQAngle_t().AngleVectors();
    public static void AngleVectors(this QAngle vec, out Vector_t fwd, out Vector_t right, out Vector_t up) => vec.ToQAngle_t().AngleVectors(out fwd, out right, out up);

    public static Vector_t ToVector_t(this CounterStrikeSharp.API.Modules.Utils.Vector vec) => new(vec.Handle);
    public static QAngle_t ToQAngle_t(this QAngle vec) => new(vec.Handle);
}