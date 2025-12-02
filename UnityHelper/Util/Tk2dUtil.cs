using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UObject = UnityEngine.Object;
using WrapMode = tk2dSpriteAnimationClip.WrapMode;

namespace Silksong.UnityHelper.Util;

/// <summary>
/// Class containing utility and extension methods for creating tk2d animations.
/// </summary>
public static class Tk2dUtil
{

    /// <summary>
    /// Creates a <see cref="tk2dSpriteAnimationClip"/> from a collection of sprites.
    /// </summary>
    /// <remarks>
    /// All sprites passed to this function should be unique. If you need to hold on a
    /// sprite for multiple frames in one animation, you should make a
    /// <see cref="tk2dSpriteCollectionData"/> from the unique sprites, and then create
    /// frames and animation clips manually.
    /// See <see cref="CreateTk2dSpriteCollection(IEnumerable{Sprite}, IEnumerable{string}?, IEnumerable{Vector2}?, float)"/>.
    /// </remarks>
    /// <param name="name">The name of the animation clip.</param>
    /// <param name="fps">The speed of the animation in frames per second.</param>
    /// <param name="frames">
    ///     A collection of sprites to use as the frames of the animation.
    ///     Each sprite must have a unique <see cref="UObject.name"/>.
    /// </param>
    /// <param name="wrapMode">
    ///     The wrap mode for the animation. This determines if and how the animation loops.
    /// </param>
    /// <param name="loopStart">
    ///     The frame index that each loop of an animation with
    ///     <see cref="WrapMode.LoopSection"/> starts at.
    /// </param>
    /// <param name="pixelsPerUnit">
    ///     The in-game resolution for all sprites in the collection. Higher values scale
    ///     them down, lower values scale them up.
    /// </param>
    /// <returns>A <see cref="tk2dSpriteAnimationClip"/> object.</returns>
    public static tk2dSpriteAnimationClip CreateTk2dAnimationClip(
        string name, int fps, IEnumerable<Texture2D> frames,
        WrapMode wrapMode = WrapMode.Once, int loopStart = 0, float pixelsPerUnit = 64f
    ) {
        var spriteCollection = CreateTk2dSpriteCollection(
            frames, pixelsPerUnit: pixelsPerUnit
        );
        return new() {
            name = name,
            fps = fps,
            wrapMode = wrapMode,
            loopStart = loopStart,
            frames = spriteCollection.CreateFrames(frames.Select(x => x.name)),
        };
    }

    /// <inheritdoc cref="CreateTk2dAnimationClip(string, int, IEnumerable{Texture2D}, WrapMode, int, float)"/>
    public static tk2dSpriteAnimationClip CreateTk2dAnimationClip(
        string name, int fps, IEnumerable<Sprite> frames,
        WrapMode wrapMode = WrapMode.Once, int loopStart = 0, float pixelsPerUnit = 64f
    ) {
        var spriteCollection = CreateTk2dSpriteCollection(
            frames, pixelsPerUnit: pixelsPerUnit
        );
        return new() {
            name = name,
            fps = fps,
            wrapMode = wrapMode,
            loopStart = loopStart,
            frames = spriteCollection.CreateFrames(frames.Select(x => x.name)),
        };
    }

    /// <summary>
    /// Create a <see cref="tk2dSpriteCollectionData" /> from a collection of sprites.
    /// Sprite collections can be used to create <see cref="tk2dSpriteAnimationFrame"/>s
    /// for use in <see cref="tk2dSpriteAnimationClip"/>s.
    /// </summary>
    /// <remarks>
    /// All sprites in a collection should be unique. If you need to hold on a sprite for
    /// multiple frames in an animation, you should add it to the collection once, then
    /// create multiple <see cref="tk2dSpriteAnimationFrame"/>s of the same sprite to
    /// pass to the animation clip.
    /// </remarks>
    /// <param name="sprites">
    ///     The sprites to construct a collection from. If <paramref name="spriteNames"/>
    ///     is not set, each sprite must have a unique <see cref="UObject.name"/>.
    /// </param>
    /// <param name="spriteNames">
    ///     Unique names to be assigned to each sprite in the collection.
    ///     If unset, each sprite's <see cref="UObject.name"/> field is used.
    /// </param>
    /// <param name="spriteCenters">
    ///     The center or pivot point, in pixels, of each sprite in the collection.
    ///     If unset, the center point of each sprite is used.
    /// </param>
    /// <param name="pixelsPerUnit">
    ///     The in-game resolution for all sprites in the collection. Higher values scale
    ///     them down, lower values scale them up.
    /// </param>
    /// <returns>A <see cref="tk2dSpriteCollectionData" /> object.</returns>
    public static tk2dSpriteCollectionData CreateTk2dSpriteCollection(
        IEnumerable<Texture2D> sprites,
        IEnumerable<string>? spriteNames = null,
        IEnumerable<Vector2>? spriteCenters = null,
        float pixelsPerUnit = 64f
    ) {
        spriteNames ??= sprites.Select(x => x.name);
        spriteCenters ??= sprites.Select(x => new Vector2(x.width / 2f, x.height / 2f));

        #region Error Checking
        int spriteCount = sprites.Count();
        if (spriteCount != spriteNames.Count())
        {
            throw new ArgumentException($"Number of {nameof(spriteNames)} does not match number of {nameof(sprites)}", nameof(spriteNames));
        }
        if (spriteCount != spriteCenters.Count())
        {
            throw new ArgumentException($"Number of {nameof(spriteCenters)} does not match number of {nameof(sprites)}", nameof(spriteCenters));
        }

        if (spriteNames.Distinct().Count() < spriteCount)
        {
            throw new ArgumentException($"Sprites in the same collection must have unique names.", nameof(spriteNames));
        }
        #endregion

        // Make sure all incoming textures are readable
        Texture2D[] readableSprites = [.. sprites.Select(SpriteUtil.GetReadableTexture)];

        // Make a basic spritesheet
        Texture2D atlas = new(1, 1);
        Rect[] spriteRegions = atlas.PackTextures(readableSprites, padding: 2);
        atlas.Apply(false, makeNoLongerReadable: true);

        for (int i = 0; i < spriteRegions.Length; i++) {
            Rect region = spriteRegions[i];

            // Sprite regions are in percentages of atlas dimensions,
            // but we need them in pixels
            region.x *= atlas.width;
            region.y *= atlas.height;
            region.width *= atlas.width;
            region.height *= atlas.height;

            // Also need them flipped vertically because unity and tk2d
            // disagree on whether Y starts at the top or the bottom
            region.y = atlas.height - region.yMax;

            spriteRegions[i] = region;
        }

        var spriteCollection = tk2dSpriteCollectionData.CreateFromTexture(
            atlas,
            tk2dSpriteCollectionSize.PixelsPerMeter(pixelsPerUnit),
            [.. spriteNames],
            spriteRegions,
            [.. spriteCenters]
        );

        UObject.DontDestroyOnLoad(spriteCollection);

        return spriteCollection;
    }

    /// <inheritdoc
    ///     cref="CreateTk2dSpriteCollection(IEnumerable{Texture2D}, IEnumerable{string}?, IEnumerable{Vector2}?, float)"
    ///     path="//*[not(self::param[@name='spriteCenters'])]" />
    /// <param name="spriteCenters">
    ///     The center or pivot point, in pixels, of each sprite in the collection.
    ///     If unset, the <see cref="Sprite.pivot"/> field of each sprite is used.
    /// </param>
    public static tk2dSpriteCollectionData CreateTk2dSpriteCollection(
        #pragma warning disable CS1573
        IEnumerable<Sprite> sprites,
        IEnumerable<string>? spriteNames = null,
        IEnumerable<Vector2>? spriteCenters = null,
        float pixelsPerUnit = 64f
        #pragma warning restore CS1573
    ) =>
        CreateTk2dSpriteCollection(
            sprites.Select(x => x.texture),
            spriteNames ?? sprites.Select(x => x.name),
            spriteCenters ?? sprites.Select(x => x.pivot),
            pixelsPerUnit
        );

    /// <summary>
    /// Create a <see cref="tk2dSpriteAnimationFrame"/> from a sprite in this collection.
    /// </summary>
    /// <param name="collection">The collection to pull the sprite from.</param>
    /// <param name="spriteName">The name of the sprite to use.</param>
    /// <param name="triggerEvent">
    ///     Whether or not this frame triggers an animation event.
    /// </param>
    /// <returns>A <see cref="tk2dSpriteAnimationFrame"/> object.</returns>
    public static tk2dSpriteAnimationFrame CreateFrame(
        this tk2dSpriteCollectionData collection,
        string spriteName,
        bool triggerEvent = false
    ) =>
        new() {
            spriteCollection = collection,
            spriteId = collection.GetSpriteIdByName(spriteName),
            triggerEvent = triggerEvent
        };

    /// <summary>
    /// Creates multiple <see cref="tk2dSpriteAnimationFrame"/>s from multiple
    /// sprites in this collection.
    /// </summary>
    /// <param name="collection">The collection to pull sprites from.</param>
    /// <param name="spriteNames">The names of the sprites to use.</param>
    /// <returns>An array of <see cref="tk2dSpriteAnimationFrame"/> objects.</returns>
    public static tk2dSpriteAnimationFrame[] CreateFrames(
        this tk2dSpriteCollectionData collection,
        params string[] spriteNames
    ) => 
        [.. spriteNames.Select(x => collection.CreateFrame(x))];

    /// <inheritdoc cref="CreateFrames(tk2dSpriteCollectionData, string[])"/>
    public static tk2dSpriteAnimationFrame[] CreateFrames(
        this tk2dSpriteCollectionData collection,
        IEnumerable<string> spriteNames
    ) =>
        [.. spriteNames.Select(x => collection.CreateFrame(x))];

}
