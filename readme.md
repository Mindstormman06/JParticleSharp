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
(a 20-byte name and a BTI image), with the texture table aligned to 0x20. A `.jpn` is a count and one u16 per entry
(0xFFFF for none).

Blocks are kept as bytes, with typed views that read and write them in place:

```csharp
JpaDynamics emitter = resource.Dynamics!;          // BEM1: rate, lifetime, velocities, spread, emitter transform
emitter.Rate *= 2;
JpaBaseShape shape = resource.BaseShape!;          // BSP1: shape/dir/rot types, size, blend, colours, colour tables
foreach (JpaColorKey key in shape.PrimaryColorTable) Console.WriteLine($"{key.Time}: {key.Rgba:X8}");
```

`ExtraShape` (ESP1: scale/alpha/rotation over life), `ChildShape` (SSP1), `ExtraTexture` (ETX1), `Fields` (FLD1:
gravity, air, vortex and the rest) and `KeyFrames` (KFA1: Hermite curves on rate, scale, lifetime and so on) work the
same way, and `TextureIndices` reads TDB1. The layouts follow noclip.website's JPA reader for JPAC2-11 (BSP1's flags
sit two bits higher than in JPAC2-10, and ETX1's indirect fields at 0x4D). Across all 5,474 resources in the dump the
values are consistent: every texture index is inside TDB1, field and key types are known, keys ascend and floats are
finite. 11 colour tables have a key past their max frame, which looks like authoring leftover.

All 55 distinct `.jpc` and 51 `.jpn` files in the dump rebuild to the same bytes, every resource's texture indices
fall inside its container, and all 1,351 textures decode.

## Build

```bash
git submodule update --init --recursive
dotnet build JParticleSharp.slnx -c Release
```

## Licence

AGPL-3.0-or-later. See [license.md](license.md).
