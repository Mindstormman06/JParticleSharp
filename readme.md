# JParticleSharp

JSystem JParticle containers (`.jpc`, `JPAC2-11`) and their `.jpn` tables, for *Skyward Sword HD* and other
JSystem titles.

```csharp
JpacFile jpc = JpacFile.FromBinary(arc["dat/Common.jpc"]!.Data);
JpaResource emitter = jpc.FindResource(9)!;
foreach (JpaBlock block in emitter.Blocks) Console.WriteLine(block);          // BEM1, BSP1, ESP1, FLD1, KFA1, ...
foreach (ushort t in emitter.TextureIndices) Console.WriteLine(jpc.Textures[t].Name);

GxImage image = jpc.Textures[0].Image;                                         // the texture's BTI, through TexSharp
byte[] packed = jpc.Write();
```

A container is its emitter resources (an id, field/key-frame/texture-index counts and tagged blocks) and its textures
(a 20-byte name and a BTI image), with the texture table aligned to 0x20. Blocks are kept as bytes; `TDB1` is read
as the resource's texture indices. A `.jpn` is a count and one u16 per entry (0xFFFF for none).

All 55 distinct `.jpc` and 51 `.jpn` files in the dump rebuild to the same bytes, every resource's texture indices
fall inside its container, and all 1,351 textures decode.

## Build

```bash
git submodule update --init --recursive
dotnet build JParticleSharp.slnx -c Release
```

## Licence

AGPL-3.0-or-later. See [license.md](license.md).
