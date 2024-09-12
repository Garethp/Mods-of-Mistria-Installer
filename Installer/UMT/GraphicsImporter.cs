using System.Collections;
using System.Drawing;
using System.Text.RegularExpressions;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace Garethp.ModsOfMistriaInstaller.Installer.UMT;

/**
 * @TODO:
 * - Store the textures/page items for each mod in a TextureGroupInfo
 * - Before adding sprites for a mod, check the TextureGroupInfo and clean up existing embedded textures/page items
 * - Find a better way of getting the next texture and texture page item name
 * - Refactor the TextureGroupInfo item adding into an "ensure resource" function
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
            
            // @TODO: We should keep this all in memory instead of reading/writing to a temp file
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

                var spriteData =
                    sprites.Find(sprite =>
                        (sprite.HasFrames && Path.Combine(sourcePath, sprite.Location) ==
                            Path.GetDirectoryName(node.Texture.Source))
                        || Path.Combine(sourcePath, sprite.Location) == node.Texture.Source
                    );

                spriteData?.PageItems.Add(stripped, texturePageItem);

                // ImportSprite(gameData, sprFrameRegex, stripped, texturePageItem, node, atlasBitmap);
            }

            // Increment atlas
            atlasCount++;
        }

        sprites.ForEach(sprite => ImportSprite(gameData, sprite));
    }

    public void ImportSprite(UndertaleData gameData, SpriteData spriteData)
    {
        var count = spriteData.PageItems.Count;
        if (count == 0) return;

        var pageItems = spriteData
            .PageItems
            .ToList()
            .Select(item =>
            {
                var indexMatch = new Regex(@"(\d+)$").Match(item.Key);
                if (!indexMatch.Success) return item;

                var indexNumber = int.Parse(indexMatch.Groups[1].Value);
                var index = indexNumber.ToString($"D{count.ToString().Length}");
                return new KeyValuePair<string, UndertaleTexturePageItem>(index, item.Value);
            })
            .OrderBy(item => item.Key)
            .Select(item => item.Value)
            .ToList();


        var sprite = gameData.Sprites.ByName(spriteData.Name);

        if (sprite is null)
        {
            sprite = new UndertaleSprite()
            {
                Name = gameData.Strings.MakeString(spriteData.Name),
                Width = pageItems[0].SourceWidth,
                Height = pageItems[0].SourceHeight,
                MarginLeft = 0,
                MarginRight = pageItems[0].SourceWidth - 1,
                MarginTop = 0,
                MarginBottom = pageItems[0].SourceHeight - 1,
                OriginX = 0,
                OriginY = 0
            };

            gameData.Sprites.Add(sprite);
        }

        if (spriteData.DeleteCollisionMask)
            sprite.CollisionMasks.Clear();

        if (spriteData.IsPlayerSprite)
        {
            var playerInformation = gameData.TextureGroupInfo.ByName("player");
            if (playerInformation.Sprites.All(item => item.Resource.Name != sprite.Name))
                playerInformation.Sprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(sprite));
        }

        if (spriteData.IsUiSprite)
        {
            var uiInformation = gameData.TextureGroupInfo.ByName("ui");
            if (uiInformation.Sprites.All(item => item.Resource.Name != sprite.Name))
                uiInformation.Sprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(sprite));
        }

        sprite.BBoxMode = spriteData.BoundingBoxMode;
        sprite.IsSpecialType = spriteData.SpecialType;
        sprite.SVersion = spriteData.SpecialTypeVersion;
        sprite.GMS2PlaybackSpeed = spriteData.SpecialPlaybackSpeed;

        Dictionary<string, UndertaleEmbeddedTexture> allTextures = [];

        for (var pageIndex = 0; pageIndex < pageItems.Count; pageIndex++)
        {
            allTextures[pageItems[pageIndex].TexturePage.Name.ToString()] = pageItems[pageIndex].TexturePage;

            var textureEntry = new UndertaleSprite.TextureEntry()
            {
                Texture = pageItems[pageIndex]
            };

            if (sprite.Textures.Count - 1 < pageIndex)
            {
                sprite.Textures.Add(textureEntry);
            }
            else
            {
                sprite.Textures[pageIndex] = textureEntry;
            }
        }


        foreach (var embeddedTexture in allTextures.Values.ToList())
        {
            if (spriteData.IsPlayerSprite)
            {
                var playerInformation = gameData.TextureGroupInfo.ByName("player");
                if (playerInformation.TexturePages.All(item => item.Resource.Name != embeddedTexture.Name))
                    playerInformation.TexturePages.Add(
                        new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>(embeddedTexture));
            }
            
            if (spriteData.IsUiSprite)
            {
                var uiInformation = gameData.TextureGroupInfo.ByName("ui");
                if (uiInformation.TexturePages.All(item => item.Resource.Name != embeddedTexture.Name))
                    uiInformation.TexturePages.Add(
                        new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>(embeddedTexture));
            }
        }
    }
    
    void SetTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
    {
        tex.TargetX = 0;
        tex.TargetY = 0;
        tex.TargetWidth = (ushort)n.Bounds.Width;
        tex.TargetHeight = (ushort)n.Bounds.Height;
    }
}