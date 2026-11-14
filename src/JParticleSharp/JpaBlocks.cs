using System.Buffers.Binary;
using System.Numerics;

namespace JParticleSharp;

/// <summary>A typed view over a block's bytes. Offsets are from the block start, header included, as JPA tools give them.</summary>
public abstract class JpaBlockView(JpaBlock block)
{
    public JpaBlock Block { get; } = block;
    protected byte[] B => Block.Body;

    protected byte U8(int at) => B[at - 8];
    protected void SetU8(int at, byte v) => B[at - 8] = v;
    protected ushort U16(int at) => BinaryPrimitives.ReadUInt16BigEndian(B.AsSpan(at - 8));
    protected void SetU16(int at, ushort v) => BinaryPrimitives.WriteUInt16BigEndian(B.AsSpan(at - 8), v);
    protected short S16(int at) => BinaryPrimitives.ReadInt16BigEndian(B.AsSpan(at - 8));
    protected void SetS16(int at, short v) => BinaryPrimitives.WriteInt16BigEndian(B.AsSpan(at - 8), v);
    protected uint U32(int at) => BinaryPrimitives.ReadUInt32BigEndian(B.AsSpan(at - 8));
    protected void SetU32(int at, uint v) => BinaryPrimitives.WriteUInt32BigEndian(B.AsSpan(at - 8), v);
    protected float F32(int at) => BinaryPrimitives.ReadSingleBigEndian(B.AsSpan(at - 8));
    protected void SetF32(int at, float v) => BinaryPrimitives.WriteSingleBigEndian(B.AsSpan(at - 8), v);
    protected Vector3 Vec3(int at) => new(F32(at), F32(at + 4), F32(at + 8));
    protected void SetVec3(int at, Vector3 v) { SetF32(at, v.X); SetF32(at + 4, v.Y); SetF32(at + 8, v.Z); }
    protected bool Bit(int at, int bit) => (U32(at) >> bit & 1) != 0;
    protected uint Bits(int at, int shift, int count) => U32(at) >> shift & ((1u << count) - 1);

    public int BlockSize => B.Length + 8;
}

/// <summary>BEM1: the emitter (dynamics).</summary>
public sealed class JpaDynamics(JpaBlock block) : JpaBlockView(block)
{
    public uint EmitFlags { get => U32(0x08); set => SetU32(0x08, value); }
    public int VolumeType => (int)Bits(0x08, 8, 3);
    public Vector3 EmitterScale { get => Vec3(0x10); set => SetVec3(0x10, value); }
    public Vector3 EmitterTranslation { get => Vec3(0x1C); set => SetVec3(0x1C, value); }
    public Vector3 EmitterDirection { get => Vec3(0x28); set => SetVec3(0x28, value); }
    public float InitialVelocityOmni { get => F32(0x34); set => SetF32(0x34, value); }
    public float InitialVelocityAxis { get => F32(0x38); set => SetF32(0x38, value); }
    public float InitialVelocityRandom { get => F32(0x3C); set => SetF32(0x3C, value); }
    public float InitialVelocityDirection { get => F32(0x40); set => SetF32(0x40, value); }
    public float Spread { get => F32(0x44); set => SetF32(0x44, value); }
    public float InitialVelocityRatio { get => F32(0x48); set => SetF32(0x48, value); }
    public float Rate { get => F32(0x4C); set => SetF32(0x4C, value); }
    public float RateRandom { get => F32(0x50); set => SetF32(0x50, value); }
    public float LifeTimeRandom { get => F32(0x54); set => SetF32(0x54, value); }
    public float VolumeSweep { get => F32(0x58); set => SetF32(0x58, value); }
    public float VolumeMinRadius { get => F32(0x5C); set => SetF32(0x5C, value); }
    public float AirResistance { get => F32(0x60); set => SetF32(0x60, value); }
    public float MomentRandom { get => F32(0x64); set => SetF32(0x64, value); }
    /// <summary>Emitter rotation in degrees.</summary>
    public (short X, short Y, short Z) EmitterRotation
    {
        get => (S16(0x68), S16(0x6A), S16(0x6C));
        set { SetS16(0x68, value.X); SetS16(0x6A, value.Y); SetS16(0x6C, value.Z); }
    }
    public short MaxFrame { get => S16(0x6E); set => SetS16(0x6E, value); }
    public short StartFrame { get => S16(0x70); set => SetS16(0x70, value); }
    public short LifeTime { get => S16(0x72); set => SetS16(0x72, value); }
    public short VolumeSize { get => S16(0x74); set => SetS16(0x74, value); }
    public short DivNumber { get => S16(0x76); set => SetS16(0x76, value); }
    public byte RateStep { get => U8(0x78); set => SetU8(0x78, value); }
}

public readonly record struct JpaColorKey(ushort Time, uint Rgba);

/// <summary>BSP1: the base particle shape (JPAC2-11 flag layout).</summary>
public sealed class JpaBaseShape(JpaBlock block) : JpaBlockView(block)
{
    public uint Flags { get => U32(0x08); set => SetU32(0x08, value); }
    public int ShapeType => (int)Bits(0x08, 0, 4);
    public int DirType => (int)Bits(0x08, 4, 3);
    public int RotType => (int)Bits(0x08, 7, 3);
    public int PlaneType => (int)Bits(0x08, 10, 1);
    public bool GlobalColorAnimation => Bit(0x08, 12);
    public bool GlobalTextureAnimation => Bit(0x08, 14);
    public int ColorInSelect => (int)Bits(0x08, 15, 3);
    public int AlphaInSelect => (int)Bits(0x08, 18, 1);
    public bool Projection => Bit(0x08, 22);
    public bool DrawForwardAhead => Bit(0x08, 23);
    public bool DrawParentAhead => Bit(0x08, 24);
    public bool TextureScrollAnimation => Bit(0x08, 26);
    public bool TilingS => Bit(0x08, 27);
    public bool TilingT => Bit(0x08, 28);
    public bool NoDrawParent => Bit(0x08, 29);
    public bool NoDrawChild => Bit(0x08, 30);

    public Vector2 BaseSize { get => new(F32(0x10), F32(0x14)); set { SetF32(0x10, value.X); SetF32(0x14, value.Y); } }
    public ushort BlendModeFlags { get => U16(0x18); set => SetU16(0x18, value); }
    public byte AlphaCompareFlags { get => U8(0x1A); set => SetU8(0x1A, value); }
    public byte AlphaRef0 { get => U8(0x1B); set => SetU8(0x1B, value); }
    public byte AlphaRef1 { get => U8(0x1C); set => SetU8(0x1C, value); }
    public byte ZModeFlags { get => U8(0x1D); set => SetU8(0x1D, value); }
    public byte TextureFlags { get => U8(0x1E); set => SetU8(0x1E, value); }
    public bool TextureIndexAnimation => (TextureFlags & 1) != 0;
    public int TextureCalcIndexType => TextureFlags >> 2 & 7;
    public byte TextureIndexAnimationCount => U8(0x1F);
    public byte TextureIndex { get => U8(0x20); set => SetU8(0x20, value); }
    public byte ColorFlags { get => U8(0x21); set => SetU8(0x21, value); }
    public bool PrimaryColorAnimation => (ColorFlags & 2) != 0;
    public bool EnvironmentColorAnimation => (ColorFlags & 8) != 0;
    public int ColorCalcIndexType => ColorFlags >> 4 & 7;
    public ushort ColorAnimationMaxFrame { get => U16(0x24); set => SetU16(0x24, value); }
    public uint PrimaryColor { get => U32(0x26); set => SetU32(0x26, value); }
    public uint EnvironmentColor { get => U32(0x2A); set => SetU32(0x2A, value); }
    public byte AnimationRandom { get => U8(0x2E); set => SetU8(0x2E, value); }
    public byte ColorLoopOffsetMask { get => U8(0x2F); set => SetU8(0x2F, value); }
    public byte TextureIndexLoopOffsetMask { get => U8(0x30); set => SetU8(0x30, value); }

    /// <summary>Texture scroll: initial translate X/Y, scale X/Y, rotate, then the per-frame increments, in that order.</summary>
    public float[]? TextureScroll => TextureScrollAnimation ? [.. Enumerable.Range(0, 10).Select(i => F32(0x34 + i * 4))] : null;

    public IReadOnlyList<byte> TextureIndexAnimationTable
    {
        get
        {
            if (!TextureIndexAnimation) return [];
            int at = 0x34 + (TextureScrollAnimation ? 0x28 : 0);
            return [.. Enumerable.Range(0, TextureIndexAnimationCount).Select(i => U8(at + i))];
        }
    }

    public IReadOnlyList<JpaColorKey> PrimaryColorTable => PrimaryColorAnimation ? ColorTable(U16(0x0C), U8(0x22)) : [];
    public IReadOnlyList<JpaColorKey> EnvironmentColorTable => EnvironmentColorAnimation ? ColorTable(U16(0x0E), U8(0x23)) : [];

    private JpaColorKey[] ColorTable(int at, int count)
        => [.. Enumerable.Range(0, count).Select(i => new JpaColorKey(U16(at + i * 6), U32(at + i * 6 + 2)))];
}

/// <summary>ESP1: scale, alpha and rotation over a particle's life.</summary>
public sealed class JpaExtraShape(JpaBlock block) : JpaBlockView(block)
{
    public uint Flags { get => U32(0x08); set => SetU32(0x08, value); }
    public bool ScaleEnabled => Bit(0x08, 0);
    public bool ScaleDiffXY => Bit(0x08, 1);
    public int ScaleAnimationTypeX => (int)Bits(0x08, 8, 2);
    public int ScaleAnimationTypeY => (int)Bits(0x08, 10, 2);
    public int PivotX => (int)Bits(0x08, 12, 2);
    public int PivotY => (int)Bits(0x08, 14, 2);
    public bool AlphaEnabled => Bit(0x08, 16);
    public bool AlphaSineWave => Bit(0x08, 17);
    public bool RotateEnabled => Bit(0x08, 24);

    public float ScaleInTiming { get => F32(0x0C); set => SetF32(0x0C, value); }
    public float ScaleOutTiming { get => F32(0x10); set => SetF32(0x10, value); }
    public float ScaleInValueX { get => F32(0x14); set => SetF32(0x14, value); }
    public float ScaleOutValueX { get => F32(0x18); set => SetF32(0x18, value); }
    public float ScaleInValueY { get => F32(0x1C); set => SetF32(0x1C, value); }
    public float ScaleOutValueY { get => F32(0x20); set => SetF32(0x20, value); }
    public float ScaleOutRandom { get => F32(0x24); set => SetF32(0x24, value); }
    public ushort ScaleAnimationMaxFrameX { get => U16(0x28); set => SetU16(0x28, value); }
    public ushort ScaleAnimationMaxFrameY { get => U16(0x2A); set => SetU16(0x2A, value); }
    public float AlphaInTiming { get => F32(0x2C); set => SetF32(0x2C, value); }
    public float AlphaOutTiming { get => F32(0x30); set => SetF32(0x30, value); }
    public float AlphaInValue { get => F32(0x34); set => SetF32(0x34, value); }
    public float AlphaBaseValue { get => F32(0x38); set => SetF32(0x38, value); }
    public float AlphaOutValue { get => F32(0x3C); set => SetF32(0x3C, value); }
    public float AlphaWaveFrequency { get => F32(0x40); set => SetF32(0x40, value); }
    public float AlphaWaveRandom { get => F32(0x44); set => SetF32(0x44, value); }
    public float AlphaWaveAmplitude { get => F32(0x48); set => SetF32(0x48, value); }
    /// <summary>In 0x10000ths of a turn.</summary>
    public float RotateAngle { get => F32(0x4C); set => SetF32(0x4C, value); }
    public float RotateAngleRandom { get => F32(0x50); set => SetF32(0x50, value); }
    public float RotateSpeed { get => F32(0x54); set => SetF32(0x54, value); }
    public float RotateSpeedRandom { get => F32(0x58); set => SetF32(0x58, value); }
    public float RotateDirection { get => F32(0x5C); set => SetF32(0x5C, value); }
}

/// <summary>SSP1: the child particles a particle spawns.</summary>
public sealed class JpaChildShape(JpaBlock block) : JpaBlockView(block)
{
    public uint Flags { get => U32(0x08); set => SetU32(0x08, value); }
    public int ShapeType => (int)Bits(0x08, 0, 4);
    public int DirType => (int)Bits(0x08, 4, 3);
    public int RotType => (int)Bits(0x08, 7, 3);
    public int PlaneType => (int)Bits(0x08, 10, 1);
    public bool InheritScale => Bit(0x08, 16);
    public bool InheritAlpha => Bit(0x08, 17);
    public bool InheritRgb => Bit(0x08, 18);
    public bool FieldEnabled => Bit(0x08, 21);
    public bool ScaleOut => Bit(0x08, 22);
    public bool AlphaOut => Bit(0x08, 23);
    public bool RotateEnabled => Bit(0x08, 24);

    public float PositionRandom { get => F32(0x0C); set => SetF32(0x0C, value); }
    public float BaseVelocity { get => F32(0x10); set => SetF32(0x10, value); }
    public float BaseVelocityRandom { get => F32(0x14); set => SetF32(0x14, value); }
    public float VelocityInfluenceRate { get => F32(0x18); set => SetF32(0x18, value); }
    public float Gravity { get => F32(0x1C); set => SetF32(0x1C, value); }
    public Vector2 GlobalScale { get => new(F32(0x20), F32(0x24)); set { SetF32(0x20, value.X); SetF32(0x24, value.Y); } }
    public float InheritScaleRate { get => F32(0x28); set => SetF32(0x28, value); }
    public float InheritAlphaRate { get => F32(0x2C); set => SetF32(0x2C, value); }
    public float InheritRgbRate { get => F32(0x30); set => SetF32(0x30, value); }
    public uint PrimaryColor { get => U32(0x34); set => SetU32(0x34, value); }
    public uint EnvironmentColor { get => U32(0x38); set => SetU32(0x38, value); }
    public float Timing { get => F32(0x3C); set => SetF32(0x3C, value); }
    public ushort Life { get => U16(0x40); set => SetU16(0x40, value); }
    public ushort Rate { get => U16(0x42); set => SetU16(0x42, value); }
    public byte Step { get => U8(0x44); set => SetU8(0x44, value); }
    public byte TextureIndex { get => U8(0x45); set => SetU8(0x45, value); }
    /// <summary>In 0x10000ths of a turn per frame.</summary>
    public ushort RotateSpeed { get => U16(0x46); set => SetU16(0x46, value); }
}

/// <summary>ETX1: indirect and second textures (JPAC2-11 layout).</summary>
public sealed class JpaExtraTexture(JpaBlock block) : JpaBlockView(block)
{
    public uint Flags { get => U32(0x08); set => SetU32(0x08, value); }
    public bool IndirectTextureMode => Bit(0x08, 0);
    public bool SecondTextureEnabled => Bit(0x08, 8);
    /// <summary>The indirect matrix, rows (p00, p01, p02) and (p10, p11, p12).</summary>
    public float[] IndirectMatrix => [F32(0x0C), F32(0x10), F32(0x14), F32(0x18), F32(0x1C), F32(0x20)];
    public sbyte IndirectScaleExponent { get => (sbyte)U8(0x4D); set => SetU8(0x4D, (byte)value); }
    public byte IndirectTextureIndex { get => U8(0x4E); set => SetU8(0x4E, value); }
    public byte SecondTextureIndex { get => U8(0x4F); set => SetU8(0x4F, value); }
}

public readonly record struct JpaKey(float Time, float Value, float TangentIn, float TangentOut);

public enum JpaKeyType : byte
{
    Rate, VolumeSize, VolumeSweep, VolumeMinRadius, LifeTime, Moment, InitialVelocityOmni, InitialVelocityAxis,
    InitialVelocityDirection, Spread, Scale,
}

/// <summary>KFA1: a key-framed curve driving one emitter value.</summary>
public sealed class JpaKeyFrames(JpaBlock block) : JpaBlockView(block)
{
    public JpaKeyType KeyType { get => (JpaKeyType)U8(0x08); set => SetU8(0x08, (byte)value); }
    public int KeyCount => U8(0x09);
    public bool Loop { get => U8(0x0B) != 0; set => SetU8(0x0B, (byte)(value ? 1 : 0)); }
    public IReadOnlyList<JpaKey> Keys
        => [.. Enumerable.Range(0, KeyCount).Select(i => 0x0C + i * 16).Select(at => new JpaKey(F32(at), F32(at + 4), F32(at + 8), F32(at + 12)))];
}

public enum JpaFieldType { Gravity, Air, Magnet, Newton, Vortex, Random, Drag, Convection, Spin }

/// <summary>FLD1: a force acting on particles.</summary>
public sealed class JpaField(JpaBlock block) : JpaBlockView(block)
{
    public uint Flags { get => U32(0x08); set => SetU32(0x08, value); }
    public JpaFieldType Type => (JpaFieldType)Bits(0x08, 0, 4);
    public int AddType => (int)Bits(0x08, 8, 2);
    public int StatusFlags => (int)(U32(0x08) >> 16);
    public Vector3 Position { get => Vec3(0x0C); set => SetVec3(0x0C, value); }
    public Vector3 Direction { get => Vec3(0x18); set => SetVec3(0x18, value); }
    public float Param1 { get => F32(0x24); set => SetF32(0x24, value); }
    public float Param2 { get => F32(0x28); set => SetF32(0x28, value); }
    public float Param3 { get => F32(0x2C); set => SetF32(0x2C, value); }
    public float FadeIn { get => F32(0x30); set => SetF32(0x30, value); }
    public float FadeOut { get => F32(0x34); set => SetF32(0x34, value); }
    public float EnableTime { get => F32(0x38); set => SetF32(0x38, value); }
    public float DisableTime { get => F32(0x3C); set => SetF32(0x3C, value); }
    public byte Cycle { get => U8(0x40); set => SetU8(0x40, value); }
}
