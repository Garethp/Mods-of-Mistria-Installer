using System.Collections;
using System.Drawing;
using System.Text.RegularExpressions;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace Garethp.ModsOfMistriaInstaller.Installer.UMT;

/**
 * @TODO:
 * - Keep the Atlas Bitmap entirely in memory
 * - Check if we can move away from the Bitmap class
 */
public class GraphicsImporter
{
    void ClearTextureData(UndertaleData gameData, string modName)
    {
        var groupInfo = gameData.TextureGroupInfo.ByName($"mod_{modName}");
        var playerInfo = gameData.TextureGroupInfo.ByName("player");
        var uiInfo = gameData.TextureGroupInfo.ByName("ui");

        if (groupInfo is null) return;

        foreach (var texturePage in groupInfo.TexturePages.ToList())
        {
            groupInfo.TexturePages.Remove(texturePage);
            gameData.EmbeddedTextures.Remove(texturePage.Resource);

            playerInfo.TexturePages.Where(resource =>
                    resource.Resource.Name.ToString() == texturePage.Resource.Name.ToString())
                .ToList()
                .ForEach(resource => playerInfo.TexturePages.Remove(resource));

            uiInfo.TexturePages.Where(resource =>
                    resource.Resource.Name.ToString() == texturePage.Resource.Name.ToString())
                .ToList()
                .ForEach(resource => uiInfo.TexturePages.Remove(resource));
        }

        foreach (var sprite in groupInfo.Sprites.ToList())
        {
            var pageItems = sprite.Resource.Textures.ToList();
            foreach (var pageItem in pageItems)
            {
                sprite.Resource.Textures.Remove(pageItem);
                gameData.TexturePageItems.Remove(pageItem.Texture);
            }

            groupInfo.Sprites.Remove(sprite);
        }
    }

    public void ImportSpriteData(
        string fieldsOfMistriaPath,
        UndertaleData gameData,
        List<SpriteData> sprites,
        string modName)
    {
        // @TODO: Either support multiple base paths or group sprites by base path
        var sourcePath = sprites[0].BaseLocation;

        ClearTextureData(gameData, modName);

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

        var prefix = outName.Replace(Path.GetExtension(outName), "");
        var atlasCount = 0;

        foreach (var atlas in packer.Atlasses)
        {
            var atlasName = Path.Combine(packDir, String.Format(prefix + "{0:000}" + ".png", atlasCount));
            var atlasBitmap = new Bitmap(atlasName);
            var texture = new UndertaleEmbeddedTexture();
            texture.Name = new UndertaleString(GetNextTextureName(gameData));

            // @TODO: We should keep this all in memory instead of reading/writing to a temp file
            texture.TextureData.TextureBlob = File.ReadAllBytes(atlasName);
            gameData.EmbeddedTextures.Add(texture);

            EnsureEmbeddedTextureInformation(gameData, $"mod_{modName}", texture);

            foreach (var node in atlas.Nodes)
            {
                if (node.Texture == null) continue;

                // Initalize values of this texture
                var texturePageItem = new UndertaleTexturePageItem();
                texturePageItem.Name = new UndertaleString(GetNextTexturePageItemName(gameData));
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

                // String processing
                var stripped = Path.GetFileNameWithoutExtension(node.Texture.Source);

                var spriteType = SpriteType.Sprite;

                SetTextureTargetBounds(texturePageItem, stripped, node);

                var spriteData =
                    sprites.Find(sprite =>
                        (sprite.HasFrames && Path.GetFullPath(Path.Combine(sourcePath, sprite.Location)) ==
                            Path.GetFullPath(Path.GetDirectoryName(node.Texture.Source)))
                        || Path.GetFullPath(Path.Combine(sourcePath, sprite.Location)) ==
                        Path.GetFullPath(node.Texture.Source)
                    );

                if (spriteData is null) continue;

                gameData.TexturePageItems.Add(texturePageItem);
                spriteData.PageItems.Add(stripped, texturePageItem);

                // ImportSprite(gameData, sprFrameRegex, stripped, texturePageItem, node, atlasBitmap);
            }

            // Increment atlas
            atlasCount++;
        }

        sprites.ForEach(sprite => ImportSprite(gameData, sprite, modName));
    }

    string GetNextTextureName(UndertaleData gameData)
    {
        var lastTextureIndex = gameData.EmbeddedTextures.Count - 1;
        var lastTexture = gameData.EmbeddedTextures[lastTextureIndex];
        var lastTextureName = lastTexture.Name.ToString();
        var numberMatch = Regex.Match(lastTextureName, "(\\d+)\"?$");
        if (!numberMatch.Success)
            throw new Exception($"Texture name does not end with a number: {lastTextureName}");

        var lastTextureNumber = int.Parse(numberMatch.Groups[1].Value);
        return $"Texture {lastTextureNumber + 1}";
    }

    string GetNextTexturePageItemName(UndertaleData gameData)
    {
        var lastTexturePageItemIndex = gameData.TexturePageItems.Count - 1;
        var lastTexturePageItem = gameData.TexturePageItems[lastTexturePageItemIndex];
        var lastTexturePageItemName = lastTexturePageItem.Name.ToString();
        var numberMatch = Regex.Match(lastTexturePageItemName, "(\\d+)\"?$");
        if (!numberMatch.Success)
            throw new Exception($"Texture Page Item name does not end with a number: {lastTexturePageItemName}");

        var lastPageItemNumber = int.Parse(numberMatch.Groups[1].Value);
        return $"PageItem {lastPageItemNumber + 1}";
    }

    void ImportSprite(UndertaleData gameData, SpriteData spriteData, string modName)
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
                Name = gameData.Strings.MakeString(spriteData.Name)
            };

            gameData.Sprites.Add(sprite);
        }

        sprite.Width = pageItems[0].SourceWidth;
        sprite.Height = pageItems[0].SourceHeight;
        sprite.MarginLeft = 0;
        sprite.MarginRight = pageItems[0].SourceWidth - 1;
        sprite.MarginTop = 0;
        sprite.MarginBottom = pageItems[0].SourceHeight - 1;
        sprite.OriginX = 0;
        sprite.OriginY = 0;

        if (spriteData.DeleteCollisionMask)
            sprite.CollisionMasks.Clear();

        EnsureSpriteInformation(gameData, $"mod_{modName}", sprite);

        if (spriteData.IsPlayerSprite)
        {
            EnsureSpriteInformation(gameData, "player", sprite);
        }

        if (spriteData.IsUiSprite)
        {
            EnsureSpriteInformation(gameData, "ui", sprite);
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
                EnsureEmbeddedTextureInformation(gameData, "player", embeddedTexture);
            }

            if (spriteData.IsUiSprite)
            {
                EnsureEmbeddedTextureInformation(gameData, "ui", embeddedTexture);
            }
        }
    }

    UndertaleTextureGroupInfo AddOrGetGroupInfo(UndertaleData gameData, string name)
    {
        var resourceInfo = gameData.TextureGroupInfo.ByName(name);
        if (resourceInfo is not null) return resourceInfo;

        resourceInfo = new UndertaleTextureGroupInfo()
        {
            Name = gameData.Strings.MakeString(name)
        };
        gameData.TextureGroupInfo.Add(resourceInfo);

        return resourceInfo;
    }

    void EnsureSpriteInformation(UndertaleData gameData, string name, UndertaleSprite resource)
    {
        var resourceInfo = AddOrGetGroupInfo(gameData, name);

        if (resourceInfo.Sprites.All(item => item.Resource.Name != resource.Name))
            resourceInfo.Sprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(resource));
    }

    void EnsureEmbeddedTextureInformation(UndertaleData gameData, string name, UndertaleEmbeddedTexture resource)
    {
        var resourceInfo = AddOrGetGroupInfo(gameData, name);

        if (resourceInfo.TexturePages.All(item => item.Resource.Name != resource.Name))
            resourceInfo.TexturePages.Add(
                new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>(resource));
    }

    void SetTextureTargetBounds(UndertaleTexturePageItem tex, string textureName, Node n)
    {
        tex.TargetX = 0;
        tex.TargetY = 0;
        tex.TargetWidth = (ushort)n.Bounds.Width;
        tex.TargetHeight = (ushort)n.Bounds.Height;
    }
}