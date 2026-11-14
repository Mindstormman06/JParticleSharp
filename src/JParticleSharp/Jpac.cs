using System.Buffers.Binary;
using System.Text;
using TexSharp;

namespace JParticleSharp;

/// <summary>One tagged block of a resource (BEM1, BSP1, ESP1, SSP1, ETX1, KFA1, FLD1, TDB1), kept as its body bytes.</summary>
public sealed class JpaBlock(string magic, byte[] body)
{
    public string Magic { get; set; } = magic;
    public byte[] Body { get; set; } = body;

    public override string ToString() => $"{Magic} (0x{Body.Length + 8:X} bytes)";
}

/// <summary>An emitter resource: an id and its blocks.</summary>
public sealed class JpaResource
{
    public ushort Id { get; set; }
    public byte FieldCount { get; set; }
    public byte KeyFrameCount { get; set; }
    public byte TextureIndexCount { get; set; }
    public byte Reserved { get; set; }
    public List<JpaBlock> Blocks { get; } = [];

    public JpaBlock? this[string magic] => Blocks.Find(b => b.Magic == magic);

    public JpaDynamics? Dynamics => this["BEM1"] is { } b ? new JpaDynamics(b) : null;
    public JpaBaseShape? BaseShape => this["BSP1"] is { } b ? new JpaBaseShape(b) : null;
    public JpaExtraShape? ExtraShape => this["ESP1"] is { } b ? new JpaExtraShape(b) : null;
    public JpaChildShape? ChildShape => this["SSP1"] is { } b ? new JpaChildShape(b) : null;
    public JpaExtraTexture? ExtraTexture => this["ETX1"] is { } b ? new JpaExtraTexture(b) : null;
    public IEnumerable<JpaField> Fields => Blocks.Where(b => b.Magic == "FLD1").Select(b => new JpaField(b));
    public IEnumerable<JpaKeyFrames> KeyFrames => Blocks.Where(b => b.Magic == "KFA1").Select(b => new JpaKeyFrames(b));

    /// <summary>The indices into the container's textures that this resource uses (from TDB1).</summary>
    public IReadOnlyList<ushort> TextureIndices
    {
        get
        {
            if (this["TDB1"] is not { } tdb) return [];
            return [.. Enumerable.Range(0, TextureIndexCount).Select(i => BinaryPrimitives.ReadUInt16BigEndian(tdb.Body.AsSpan(i * 2)))];
        }
    }

    public override string ToString() => $"resource {Id} ({string.Join(" ", Blocks.Select(b => b.Magic))})";
}

/// <summary>A texture: a 20-byte name and a BTI image.</summary>
public sealed class JpaTexture
{
    public uint Reserved { get; set; }
    public byte[] NameBytes { get; set; } = new byte[0x14];
    public byte[] Bti { get; set; } = [];

    public string Name
    {
        get
        {
            int end = Array.IndexOf(NameBytes, (byte)0);
            return Encoding.Latin1.GetString(NameBytes, 0, end < 0 ? NameBytes.Length : end);
        }
    }

    public GxImage Image => TexSharp.Bti.Read(Bti);

    public override string ToString() => Name;
}

public sealed class JpacFile
{
    private const int HeaderSize = 0x10;
    private const int TextureAlignment = 0x20;

    public string Version { get; set; } = "JPAC2-11";
    public List<JpaResource> Resources { get; } = [];
    public List<JpaTexture> Textures { get; } = [];

    public JpaResource? FindResource(ushort id) => Resources.Find(r => r.Id == id);

    public static bool IsJpac(ReadOnlySpan<byte> data) => data.Length >= HeaderSize && data[..4].SequenceEqual("JPAC"u8);

    public static JpacFile FromFile(string path) => FromBinary(File.ReadAllBytes(path));

    public static JpacFile FromBinary(ReadOnlySpan<byte> data)
    {
        if (!IsJpac(data)) throw new InvalidDataException("Not a JPAC file.");
        string version = Encoding.ASCII.GetString(data[..8]);
        if (version != "JPAC2-11") throw new NotSupportedException($"{version} is not supported (only JPAC2-11).");
        JpacFile file = new() { Version = version };
        int resources = U16(data, 8), textures = U16(data, 10), textureAt = (int)U32(data, 12);

        int p = HeaderSize;
        for (int r = 0; r < resources; r++)
        {
            JpaResource res = new()
            {
                Id = U16(data, p), FieldCount = data[p + 4], KeyFrameCount = data[p + 5], TextureIndexCount = data[p + 6], Reserved = data[p + 7],
            };
            int blocks = U16(data, p + 2);
            p += 8;
            for (int b = 0; b < blocks; b++)
            {
                int size = (int)U32(data, p + 4);
                if (size < 8 || p + size > data.Length) throw new InvalidDataException($"Resource {res.Id} block {b} at 0x{p:X} runs past the file.");
                res.Blocks.Add(new JpaBlock(Encoding.ASCII.GetString(data.Slice(p, 4)), data.Slice(p + 8, size - 8).ToArray()));
                p += size;
            }
            file.Resources.Add(res);
        }

        if (textureAt < p) throw new InvalidDataException("JPAC texture table starts inside the resources.");
        if (data[p..textureAt].IndexOfAnyExcept((byte)0) >= 0) throw new InvalidDataException("JPAC has non-zero bytes before its texture table.");

        p = textureAt;
        for (int t = 0; t < textures; t++)
        {
            if (!data.Slice(p, 4).SequenceEqual("TEX1"u8)) throw new InvalidDataException($"JPAC texture {t} has no TEX1 magic.");
            int size = (int)U32(data, p + 4);
            file.Textures.Add(new JpaTexture
            {
                Reserved = U32(data, p + 8), NameBytes = data.Slice(p + 0x0C, 0x14).ToArray(), Bti = data.Slice(p + 0x20, size - 0x20).ToArray(),
            });
            p += size;
        }
        return file;
    }

    public void WriteTo(string path) => File.WriteAllBytes(path, Write());

    public byte[] Write()
    {
        using MemoryStream o = new();
        o.Write(Encoding.ASCII.GetBytes(Version.PadRight(8)[..8]));
        Write16(o, (ushort)Resources.Count);
        Write16(o, (ushort)Textures.Count);
        Write32(o, 0);

        foreach (JpaResource r in Resources)
        {
            Write16(o, r.Id);
            Write16(o, (ushort)r.Blocks.Count);
            o.Write([r.FieldCount, r.KeyFrameCount, r.TextureIndexCount, r.Reserved]);
            foreach (JpaBlock b in r.Blocks)
            {
                o.Write(Encoding.ASCII.GetBytes(b.Magic));
                Write32(o, (uint)(b.Body.Length + 8));
                o.Write(b.Body);
            }
        }

        while (o.Length % TextureAlignment != 0) o.WriteByte(0);
        long textureAt = o.Length;
        foreach (JpaTexture t in Textures)
        {
            o.Write("TEX1"u8);
            Write32(o, (uint)(0x20 + t.Bti.Length));
            Write32(o, t.Reserved);
            o.Write(t.NameBytes);
            o.Write(t.Bti);
        }

        byte[] file = o.ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(12), (uint)textureAt);
        return file;
    }

    private static ushort U16(ReadOnlySpan<byte> d, int at) => BinaryPrimitives.ReadUInt16BigEndian(d[at..]);
    private static uint U32(ReadOnlySpan<byte> d, int at) => BinaryPrimitives.ReadUInt32BigEndian(d[at..]);
    private static void Write16(Stream o, ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b, v); o.Write(b); }
    private static void Write32(Stream o, uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, v); o.Write(b); }
}

/// <summary>A <c>.jpn</c> table: for each index, the resource it maps to (0xFFFF for none).</summary>
public sealed class JpnFile
{
    public List<ushort> Entries { get; } = [];

    public static JpnFile FromBinary(ReadOnlySpan<byte> data)
    {
        int count = BinaryPrimitives.ReadUInt16BigEndian(data);
        if (2 + count * 2 > data.Length) throw new InvalidDataException("JPN table runs past the file.");
        JpnFile file = new();
        for (int i = 0; i < count; i++) file.Entries.Add(BinaryPrimitives.ReadUInt16BigEndian(data[(2 + i * 2)..]));
        return file;
    }

    public byte[] Write()
    {
        byte[] o = new byte[2 + Entries.Count * 2];
        BinaryPrimitives.WriteUInt16BigEndian(o, (ushort)Entries.Count);
        for (int i = 0; i < Entries.Count; i++) BinaryPrimitives.WriteUInt16BigEndian(o.AsSpan(2 + i * 2), Entries[i]);
        return o;
    }
}
