namespace Garethp.ModsOfMistriaInstallerLib.Collector;

// A set of related .meta.toml files that share the same base name (after stripping the prefix).
// Example: "spr_foo_idle_east" and "poly_foo_idle_east" → BaseName "foo_idle_east"
public class AnimationGroup
{
    public string BaseName { get; init; } = "";

    // Path relative to mod root, e.g. "animations/Animals/.../spr_foo_idle_east.meta.toml"
    public string? AnimationMetaRelPath { get; init; }

    // Path relative to mod root, e.g. "animations/Animals/.../spr_foo_idle_east.png"
    public string? PngRelPath { get; init; }

    // Path relative to mod root, e.g. "shapes/Animals/.../poly_foo_idle_east.meta.toml"
    public string? ShapeMetaRelPath { get; init; }

    public bool HasAnimation => AnimationMetaRelPath is not null;
    public bool HasPng       => PngRelPath           is not null;
    public bool HasShape     => ShapeMetaRelPath      is not null;
}
