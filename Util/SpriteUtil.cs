// Parts of this code were adapted from the HK modding API under the terms of the MIT license
// https://github.com/hk-modding/api/blob/master/Assembly-CSharp/Utils/AssemblyExtensions.cs

/*
MIT License

Copyright (c) 2017 seanpr96, iamwyza, firzen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UnityHelper.Util;

public static class SpriteUtil
{
    /// <summary>
    /// Load an image from the assembly's embedded resources, and return a Sprite.
    /// </summary>
    /// <param name="asm">The assembly to load from.</param>
    /// <param name="path">The path to the image.</param>
    /// <param name="pixelsPerUnit">The pixels per unit. Changing this value will scale the size of the sprite accordingly.</param>
    /// <returns>A Sprite object.</returns>
    [SuppressMessage("Reliability", "CA2022:Avoid inexact read with 'Stream.Read'",
        Justification = "It's already stream.Length")]
    public static Sprite LoadEmbeddedSprite(Assembly asm, string path, float pixelsPerUnit = 64f)
    {
        using Stream stream = asm.GetManifestResourceStream(path);

        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);

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

}
