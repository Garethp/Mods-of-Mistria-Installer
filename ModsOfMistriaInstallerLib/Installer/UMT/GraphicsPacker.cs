using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UndertaleModLib.Util;
using Rectangle = System.Drawing.Rectangle;

namespace Garethp.ModsOfMistriaInstallerLib.Installer.UMT;

public class TextureInfo
{
    public string Source;
    public int Width;
    public int Height;
}

public enum SpriteType
{
    Sprite,
    Background,
    Font,
    Unknown
}


public enum SplitType
{
    Horizontal,
    Vertical
}

public enum BestFitHeuristic
{
    Area,
    MaxOneAxis
}

public class Node
{
    public Rectangle Bounds;
    public TextureInfo Texture;
    public SplitType SplitType;
}

public class Atlas
{
    public int Width;
    public int Height;
    public List<Node> Nodes;
}

public class Packer
{
    public List<TextureInfo> SourceTextures;
    public StringWriter Log;
    public StringWriter Error;
    public int Padding;
    public int AtlasSize;
    public bool DebugMode;
    public BestFitHeuristic FitHeuristic;
    public List<Atlas> Atlasses;

    public Packer()
    {
        SourceTextures = [];
        Log = new StringWriter();
        Error = new StringWriter();
    }

    public void Process(IMod mod, int _AtlasSize, int _Padding, bool _DebugMode)
    {
        Padding = _Padding;
        AtlasSize = _AtlasSize;
        DebugMode = _DebugMode;
        //1: scan for all the textures we need to pack
        ScanForTextures(mod);
        List<TextureInfo> textures = SourceTextures.ToList();
        //2: generate as many atlasses as needed (with the latest one as small as possible)
        Atlasses = [];
        while (textures.Count > 0)
        {
            var atlas = new Atlas();
            atlas.Width = _AtlasSize;
            atlas.Height = _AtlasSize;
            List<TextureInfo> leftovers = LayoutAtlas(textures, atlas);
            if (leftovers.Count == 0)
            {
                // we reached the last atlas. Check if this last atlas could have been twice smaller
                while (leftovers.Count == 0)
                {
                    atlas.Width /= 2;
                    atlas.Height /= 2;
                    leftovers = LayoutAtlas(textures, atlas);
                }
                // we need to go 1 step larger as we found the first size that is to small
                atlas.Width *= 2;
                atlas.Height *= 2;
                leftovers = LayoutAtlas(textures, atlas);
            }
            Atlasses.Add(atlas);
            textures = leftovers;
        }
    }


    public void SaveAtlasses(IMod mod, string _Destination)
    {
        var atlasCount = 0;
        var prefix = _Destination.Replace(Path.GetExtension(_Destination), "");

        var tw = new StreamWriter(_Destination);
        tw.WriteLine("source_tex, atlas_tex, x, y, width, height");
        foreach (var atlas in Atlasses)
        {
            var atlasName = $"{prefix}{atlasCount:000}.png";

            // 1: Save images
            using (var img = CreateAtlasImage(mod, atlas))
                TextureWorker.SaveImageToFile(img, atlasName);

            // 2: save description in file
            foreach (var n in atlas.Nodes)
            {
                if (n.Texture != null)
                {
                    tw.Write(n.Texture.Source + ", ");
                    tw.Write(atlasName + ", ");
                    tw.Write(n.Bounds.X + ", ");
                    tw.Write(n.Bounds.Y + ", ");
                    tw.Write(n.Bounds.Width + ", ");
                    tw.WriteLine(n.Bounds.Height.ToString());
                }
            }
            ++atlasCount;
        }
        tw.Close();
        tw = new StreamWriter(prefix + ".log");
        tw.WriteLine("--- LOG -------------------------------------------");
        tw.WriteLine(Log.ToString());
        tw.WriteLine("--- ERROR -----------------------------------------");
        tw.WriteLine(Error.ToString());
        tw.Close();
    }

    private void ScanForTextures(IMod mod)
    {
        var allFiles = mod.GetAllFiles(".png");
        foreach (var file in allFiles)
        {
            Image img = Image.Load<Rgba32>(mod.ReadFileAsStream(file));
            if (img is null) continue;

            if (img.Width > AtlasSize || img.Height > AtlasSize)
            {
                Error.WriteLine(file + " is too large to fix in the atlas. Skipping!");
            }
            else
            {
                var ti = new TextureInfo();

                ti.Source = file;
                ti.Width = img.Width;
                ti.Height = img.Height;

                SourceTextures.Add(ti);

                Log.WriteLine("Added " + file);
            }
        }
    }

    private void HorizontalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        var n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _Height;
        n1.SplitType = SplitType.Vertical;
        var n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _ToSplit.Bounds.Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private void VerticalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
    {
        var n1 = new Node();
        n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
        n1.Bounds.Y = _ToSplit.Bounds.Y;
        n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
        n1.Bounds.Height = _ToSplit.Bounds.Height;
        n1.SplitType = SplitType.Vertical;
        var n2 = new Node();
        n2.Bounds.X = _ToSplit.Bounds.X;
        n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
        n2.Bounds.Width = _Width;
        n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
        n2.SplitType = SplitType.Horizontal;
        if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
            _List.Add(n1);
        if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
            _List.Add(n2);
    }

    private TextureInfo FindBestFitForNode(Node _Node, List<TextureInfo> _Textures)
    {
        TextureInfo bestFit = null;
        float nodeArea = _Node.Bounds.Width * _Node.Bounds.Height;
        var maxCriteria = 0.0f;
        foreach (var ti in _Textures)
        {
            switch (FitHeuristic)
            {
                // Max of Width and Height ratios
                case BestFitHeuristic.MaxOneAxis:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        var wRatio = ti.Width / (float)_Node.Bounds.Width;
                        var hRatio = ti.Height / (float)_Node.Bounds.Height;
                        var ratio = wRatio > hRatio ? wRatio : hRatio;
                        if (ratio > maxCriteria)
                        {
                            maxCriteria = ratio;
                            bestFit = ti;
                        }
                    }
                    break;
                // Maximize Area coverage
                case BestFitHeuristic.Area:
                    if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                    {
                        float textureArea = ti.Width * ti.Height;
                        var coverage = textureArea / nodeArea;
                        if (coverage > maxCriteria)
                        {
                            maxCriteria = coverage;
                            bestFit = ti;
                        }
                    }
                    break;
            }
        }
        return bestFit;
    }

    private List<TextureInfo> LayoutAtlas(List<TextureInfo> _Textures, Atlas _Atlas)
    {
        List<Node> freeList = [];
        _Atlas.Nodes = [];
        var textures = _Textures.ToList();
        var root = new Node();
        root.Bounds.Width = _Atlas.Width;
        root.Bounds.Height = _Atlas.Height;
            
        root.SplitType = SplitType.Horizontal;
        freeList.Add(root);
        while (freeList.Count > 0 && textures.Count > 0)
        {
            var node = freeList[0];
            freeList.RemoveAt(0);
            var bestFit = FindBestFitForNode(node, textures);
            if (bestFit != null)
            {
                if (node.SplitType == SplitType.Horizontal)
                {
                    HorizontalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                else
                {
                    VerticalSplit(node, bestFit.Width, bestFit.Height, freeList);
                }
                node.Texture = bestFit;
                node.Bounds.Width = bestFit.Width;
                node.Bounds.Height = bestFit.Height;
                textures.Remove(bestFit);
            }
            _Atlas.Nodes.Add(node);
        }
        return textures;
    }


    private MagickImage CreateAtlasImage(IMod mod, Atlas _Atlas)
    {
        MagickImage img = new(MagickColors.Transparent, _Atlas.Width, _Atlas.Height);

        foreach (var n in _Atlas.Nodes)
        {
            if (n.Texture is not null)
            {
                using var sourceImg = GraphicsImporter.ReadBGRAImageFromStream(mod.ReadFileAsStream(n.Texture.Source));
                using var resizedSourceImg = TextureWorker.ResizeImage(sourceImg, n.Bounds.Width, n.Bounds.Height);
                img.Composite(resizedSourceImg, n.Bounds.X, n.Bounds.Y, CompositeOperator.Copy);
            }
        }

        return img;
    }
    
    // private Image CreateAtlasImage(Atlas _Atlas)
    // {
    //     Image img = new Bitmap(_Atlas.Width, _Atlas.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    //     Graphics g = Graphics.FromImage(img);
    //     foreach (Node n in _Atlas.Nodes)
    //     {
    //         if (n.Texture != null)
    //         {
    //             Image sourceImg = Image.FromFile(n.Texture.Source);
    //             g.DrawImage(sourceImg, n.Bounds);
    //         }
    //     }
    //     // DPI FIX START
    //     Bitmap ResolutionFix = new Bitmap(img);
    //     ResolutionFix.SetResolution(96.0F, 96.0F);
    //     Image img2 = ResolutionFix;
    //     return img2;
    //     // DPI FIX END
    // }
}