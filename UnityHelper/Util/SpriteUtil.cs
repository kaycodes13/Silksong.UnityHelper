using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Silksong.UnityHelper.Util;

/// <summary>
/// Class containing utility methods for loading sprites.
/// </summary>
public static class SpriteUtil
{
    /// <summary>
    /// Load an image from the assembly's embedded resources, and return a Sprite.
    /// </summary>
    /// <param name="asm">The assembly to load from.</param>
    /// <param name="path">The path to the image.</param>
    /// <param name="pixelsPerUnit">The pixels per unit. Changing this value will scale the size of the sprite accordingly.</param>
    /// <returns>A Sprite object.</returns>
    public static Sprite LoadEmbeddedSprite(Assembly asm, string path, float pixelsPerUnit = 64f)
    {
        using Stream stream = asm.GetManifestResourceStream(path);

        byte[] buffer = new byte[stream.Length];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        if (bytesRead != buffer.Length)
        {
            throw new IOException($"""
                Failed to read the entire resource stream for path '{path}' in assembly '{asm.FullName}'.
                Expected {stream.Length} bytes, but read {bytesRead}.
                """);
        }

        return LoadSpriteFromArray(buffer, pixelsPerUnit);
    }

    /// <summary>
    /// Load an image from a file on disc and return a Sprite.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="pixelsPerUnit"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Sprite LoadSpriteFromFile(string fileName, float pixelsPerUnit = 64f)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Filename cannot be empty", nameof(fileName));
        }

        if (!File.Exists(fileName))
        {
            throw new ArgumentException($"File {fileName} not found", nameof(fileName));
        }

        byte[] fileBytes = File.ReadAllBytes(fileName);

        return LoadSpriteFromArray(fileBytes, pixelsPerUnit);
    }

    /// <summary>
    /// Create a sprite from a byte array.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="pixelsPerUnit"></param>
    /// <returns></returns>
    public static Sprite LoadSpriteFromArray(byte[] buffer, float pixelsPerUnit = 64f)
    {
        Texture2D tex = new(2, 2);

        tex.LoadImage(buffer, true);

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, pixelsPerUnit);
    }

    /// <summary>
    /// If the given texture is unreadable, returns a readable copy of it.
    /// If the given texture is readable, returns the same texture object.
    /// </summary>
    /// <param name="tex">The texture to make readable.</param>
    /// <returns>A readable <see cref="Texture2D"/> object.</returns>
    public static Texture2D GetReadableCopyOfTexture(Texture2D tex) {
        if (tex.isReadable)
            return tex;

        var temp = RenderTexture.GetTemporary(
            tex.width, tex.height, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Linear
        );
        Graphics.Blit(tex, temp);

        var previous = RenderTexture.active;
        RenderTexture.active = temp;

        var readable = new Texture2D(tex.width, tex.height);
        readable.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temp);

        return readable;
    }
}
