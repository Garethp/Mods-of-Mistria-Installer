using System.Collections;
using System.Drawing;
using System.Text.RegularExpressions;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace Garethp.ModsOfMistriaInstaller.Installer.UMT;

/**
 * @TODO:
 * 1. Add sprite overrides for the following:
 *  - BoundingBox Mode
 *  - Delete the Collosion Mask
 *  - Set Special Type to True
 *    - Version 3
 *    - Speed 40
 *    - Add texture information
 */
public class GraphicsImporter
{
    public void ImportSpriteData(
        string sourcePath,
        string fieldsOfMistriaPath,
        UndertaleData gameData,
        List<SpriteData> sprites)
    {
        var packDir = Path.Combine(fieldsOfMistriaPath, "Packager");
        Directory.CreateDirectory(packDir);

        var searchPattern = "*.png";
        var outName = Path.Combine(packDir, "atlas.txt");
        var textureSize = 2048;
        var PaddingValue = 2;
        var debug = false;
        var packer = new Packer();
        packer.Process(sourcePath, searchPattern, textureSize, PaddingValue, debug);
        packer.SaveAtlasses(outName);

        var lastTextPage = gameData.EmbeddedTextures.Count - 1;
        var lastTextPageItem = gameData.TexturePageItems.Count - 1;

        var prefix = outName.Replace(Path.GetExtension(outName), "");
        var atlasCount = 0;
        
        foreach (var atlas in packer.Atlasses)
        {
            var atlasName = Path.Combine(packDir, String.Format(prefix + "{0:000}" + ".png", atlasCount));
            var atlasBitmap = new Bitmap(atlasName);
            var texture = new UndertaleEmbeddedTexture();
            texture.Name = new UndertaleString("Texture " + ++lastTextPage);
            texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
            gameData.EmbeddedTextures.Add(texture);
            foreach (var node in atlas.Nodes)
            {
                if (node.Texture == null) continue;

                // Initalize values of this texture
                var texturePageItem = new UndertaleTexturePageItem();
                texturePageItem.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
                texturePageItem.SourceX = (ushort)node.Bounds.X;
                texturePageItem.SourceY = (ushort)node.Bounds.Y;
                texturePageItem.SourceWidth = (ushort)node.Bounds.Width;
                texturePageItem.SourceHeight = (ushort)node.Bounds.Height;
                texturePageItem.TargetX = 0;
                texturePageItem.TargetY = 0;
                texturePageItem.TargetWidth = (ushort)node.Bounds.Width;
                texturePageItem.TargetHeight = (ushort)node.Bounds.Height;
                texturePageItem.BoundingWidth = (ushort)node.Bounds.Width;
                texturePageItem.BoundingHeight = (ushort)node.Bounds.Height;
                texturePageItem.TexturePage = texture;

                // Add this texture to UMT
                gameData.TexturePageItems.Add(texturePageItem);

                // String processing
                var stripped = Path.GetFileNameWithoutExtension(node.Texture.Source);

                var spriteType = SpriteType.Sprite;

                SetTextureTargetBounds(texturePageItem, stripped, node);

                // ImportSprite(gameData, sprFrameRegex, stripped, texturePageItem, node, atlasBitmap);
            }

            // Increment atlas
            atlasCount++;
        }
    }

    public void Import(string sourcePath, string fieldsOfMistriaPath, UndertaleData gameData,
        bool importAsSprite = false)
    {
        Regex sprFrameRegex = new(@"^(.+?)(?:_(\d+))$", RegexOptions.Compiled);

        var packDir = Path.Combine(fieldsOfMistriaPath, "Packager");
        Directory.CreateDirectory(packDir);

        var searchPattern = "*.png";
        var outName = Path.Combine(packDir, "atlas.txt");
        var textureSize = 2048;
        var PaddingValue = 2;
        var debug = false;
        var packer = new Packer();
        packer.Process(sourcePath, searchPattern, textureSize, PaddingValue, debug);
        packer.SaveAtlasses(outName);

        var lastTextPage = gameData.EmbeddedTextures.Count - 1;
        var lastTextPageItem = gameData.TexturePageItems.Count - 1;

        // Import everything into UMT
        var prefix = outName.Replace(Path.GetExtension(outName), "");
        var atlasCount = 0;
        foreach (var atlas in packer.Atlasses)
        {
            var atlasName = Path.Combine(packDir, String.Format(prefix + "{0:000}" + ".png", atlasCount));
            var atlasBitmap = new Bitmap(atlasName);
            var texture = new UndertaleEmbeddedTexture();
            texture.Name = new UndertaleString("Texture " + ++lastTextPage);
            texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
            gameData.EmbeddedTextures.Add(texture);
            foreach (var node in atlas.Nodes)
            {
                if (node.Texture == null) continue;

                // Initalize values of this texture
                var texturePageItem = new UndertaleTexturePageItem();
                texturePageItem.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
                texturePageItem.SourceX = (ushort)node.Bounds.X;
                texturePageItem.SourceY = (ushort)node.Bounds.Y;
                texturePageItem.SourceWidth = (ushort)node.Bounds.Width;
                texturePageItem.SourceHeight = (ushort)node.Bounds.Height;
                texturePageItem.TargetX = 0;
                texturePageItem.TargetY = 0;
                texturePageItem.TargetWidth = (ushort)node.Bounds.Width;
                texturePageItem.TargetHeight = (ushort)node.Bounds.Height;
                texturePageItem.BoundingWidth = (ushort)node.Bounds.Width;
                texturePageItem.BoundingHeight = (ushort)node.Bounds.Height;
                texturePageItem.TexturePage = texture;

                // Add this texture to UMT
                gameData.TexturePageItems.Add(texturePageItem);

                // String processing
                var stripped = Path.GetFileNameWithoutExtension(node.Texture.Source);

                var spriteType = GetSpriteType(node.Texture.Source);

                if (importAsSprite && spriteType is SpriteType.Unknown or SpriteType.Font)
                {
                    spriteType = SpriteType.Sprite;
                }

                SetTextureTargetBounds(texturePageItem, stripped, node);

                switch (spriteType)
                {
                    case SpriteType.Background:
                        ImportBackground(gameData, stripped, texturePageItem);
                        break;
                    case SpriteType.Sprite:
                        ImportSprite(gameData, sprFrameRegex, stripped, texturePageItem, node, atlasBitmap);
                        break;
                }
            }

            // Increment atlas
            atlasCount++;
        }
    }

    void ImportSprite(UndertaleData gameData, Regex sprFrameRegex, string fileName,
        UndertaleTexturePageItem texturePageItem, Node node, Bitmap atlasBitmap)
    {
        // Get sprite to add this texture to
        string spriteName;
        var frame = 0;
        try
        {
            var spriteParts = sprFrameRegex.Match(fileName);
            spriteName = spriteParts.Groups[1].Value;
            Int32.TryParse(spriteParts.Groups[2].Value, out frame);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: Image " + fileName + " has an invalid name. Skipping...");
            return;
        }

        UndertaleSprite sprite = null;
        sprite = gameData.Sprites.ByName(spriteName);

        // Create TextureEntry object
        UndertaleSprite.TextureEntry texentry = new UndertaleSprite.TextureEntry();
        texentry.Texture = texturePageItem;

        // Set values for new sprites
        if (sprite == null)
        {
            UndertaleString spriteUTString = gameData.Strings.MakeString(spriteName);
            UndertaleSprite newSprite = new UndertaleSprite();
            newSprite.Name = spriteUTString;
            newSprite.Width = (uint)node.Bounds.Width;
            newSprite.Height = (uint)node.Bounds.Height;
            newSprite.MarginLeft = 0;
            newSprite.MarginRight = node.Bounds.Width - 1;
            newSprite.MarginTop = 0;
            newSprite.MarginBottom = node.Bounds.Height - 1;
            newSprite.OriginX = 0;
            newSprite.OriginY = 0;

            if (frame > 0)
            {
                for (int i = 0; i < frame; i++)
                    newSprite.Textures.Add(null);
            }

            newSprite.CollisionMasks.Add(newSprite.NewMaskEntry());
            Rectangle bmpRect = new Rectangle(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height);
            System.Drawing.Imaging.PixelFormat format = atlasBitmap.PixelFormat;
            Bitmap cloneBitmap = atlasBitmap.Clone(bmpRect, format);
            int width = ((node.Bounds.Width + 7) / 8) * 8;
            BitArray maskingBitArray = new BitArray(width * node.Bounds.Height);
            for (int y = 0; y < node.Bounds.Height; y++)
            {
                for (int x = 0; x < node.Bounds.Width; x++)
                {
                    Color pixelColor = cloneBitmap.GetPixel(x, y);
                    maskingBitArray[y * width + x] = (pixelColor.A > 0);
                }
            }

            BitArray tempBitArray = new BitArray(width * node.Bounds.Height);
            for (int i = 0; i < maskingBitArray.Length; i += 8)
            {
                for (int j = 0; j < 8; j++)
                {
                    tempBitArray[j + i] = maskingBitArray[-(j - 7) + i];
                }
            }

            int numBytes;
            numBytes = maskingBitArray.Length / 8;
            byte[] bytes = new byte[numBytes];
            tempBitArray.CopyTo(bytes, 0);
            for (int i = 0; i < bytes.Length; i++)
                newSprite.CollisionMasks[0].Data[i] = bytes[i];
            newSprite.Textures.Add(texentry);
            gameData.Sprites.Add(newSprite);
            return;
        }

        if (frame > sprite.Textures.Count - 1)
        {
            while (frame > sprite.Textures.Count - 1)
            {
                sprite.Textures.Add(texentry);
            }

            return;
        }

        sprite.Textures[frame] = texentry;
    }

    void ImportBackground(UndertaleData gameData, string name, UndertaleTexturePageItem texturePageItem)
    {
        var background = gameData.Backgrounds.ByName(name);
        if (background != null)
        {
            background.Texture = texturePageItem;
        }
        else
        {
            // No background found, let's make one
            var backgroundUTString = gameData.Strings.MakeString(name);
            var newBackground = new UndertaleBackground();
            newBackground.Name = backgroundUTString;
            newBackground.Transparent = false;
            newBackground.Preload = false;
            newBackground.Texture = texturePageItem;
            gameData.Backgrounds.Add(newBackground);
        }
    }

    void SetTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
    {
        tex.TargetX = 0;
        tex.TargetY = 0;
        tex.TargetWidth = (ushort)n.Bounds.Width;
        tex.TargetHeight = (ushort)n.Bounds.Height;
    }

    SpriteType GetSpriteType(string path)
    {
        string folderPath = Path.GetDirectoryName(path);
        string folderName = new DirectoryInfo(folderPath).Name;
        string lowerName = folderName.ToLower();

        if (lowerName == "backgrounds" || lowerName == "background")
        {
            return SpriteType.Background;
        }
        else if (lowerName == "fonts" || lowerName == "font")
        {
            return SpriteType.Font;
        }
        else if (lowerName == "sprites" || lowerName == "sprite")
        {
            return SpriteType.Sprite;
        }

        return SpriteType.Unknown;
    }
}