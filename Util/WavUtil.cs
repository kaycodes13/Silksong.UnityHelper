// Parts of this code have been lifted from Satchel under the terms of the MIT license
// https://github.com/PrashantMohta/Satchel/blob/master/Deprecated/WavUtils.cs

/*
MIT License

Copyright (c) 2022 Satchel

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
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UnityHelper.Util;

public class WavUtils
{
    // Force save as 16-bit .wav
    const int BlockSize_16Bit = 2;

    /// <summary>
    /// Read a file from embedded resources and output a Unity <see cref="AudioClip"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="asm"></param>
    /// <param name="offsetSamples"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static AudioClip AudioClipFromEmbeddedResource(string path, Assembly asm, int offsetSamples, string? name = null)
    {
        name ??= path;

        using Stream resourceStream = asm.GetManifestResourceStream(path);
        byte[] fileBytes;
        using (MemoryStream ms = new())
        {
            resourceStream.CopyTo(ms);
            fileBytes = ms.ToArray();
        }

        return AudioClipFromMemory(fileBytes, offsetSamples, name);
    }

    /// <summary>
    /// Read a file from disc and output a Unity <see cref="AudioClip"/>.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="offsetSamples"></param>
    /// <param name="name">The name of the AudioClip. By default uses the filename.</param>
    /// <returns></returns>
    public static AudioClip AudioClipFromFile(string fileName, int offsetSamples = 0, string? name = null)
    {
        name ??= Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Filename cannot be empty", nameof(fileName));
        }

        if (!File.Exists(fileName))
        {
            throw new ArgumentException($"File {fileName} not found", nameof(fileName));
        }

        byte[] fileBytes = File.ReadAllBytes(fileName);
        return AudioClipFromMemory(fileBytes, offsetSamples, name);
    }

    /// <summary>
    /// Convert a byte array from a .wav file to a Unity <see cref="AudioClip"/>
    /// </summary>
    /// <param name="fileBytes"></param>
    /// <param name="offsetSamples"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static AudioClip AudioClipFromMemory(byte[] fileBytes, int offsetSamples = 0, string name = "wav")
    {
        //string riff = Encoding.ASCII.GetString (fileBytes, 0, 4);
        //string wave = Encoding.ASCII.GetString (fileBytes, 8, 4);
        int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
        ushort audioFormat = BitConverter.ToUInt16(fileBytes, 20);

        // NB: Only uncompressed PCM wav files are supported.
        string formatCode = FormatCode(audioFormat);
        Debug.AssertFormat(audioFormat == 1 || audioFormat == 65534,
            "Detected format code '{0}' {1}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.",
            audioFormat, formatCode);

        ushort channels = BitConverter.ToUInt16(fileBytes, 22);
        int sampleRate = BitConverter.ToInt32(fileBytes, 24);
        //int byteRate = BitConverter.ToInt32 (fileBytes, 28);
        //UInt16 blockAlign = BitConverter.ToUInt16 (fileBytes, 32);
        ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);

        int headerOffset = 16 + 4 + subchunk1 + 4;
        int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);
        //Debug.LogFormat ("riff={0} wave={1} subchunk1={2} format={3} channels={4} sampleRate={5} byteRate={6} blockAlign={7} bitDepth={8} headerOffset={9} subchunk2={10} filesize={11}", riff, wave, subchunk1, formatCode, channels, sampleRate, byteRate, blockAlign, bitDepth, headerOffset, subchunk2, fileBytes.Length);

        float[] data;
        switch (bitDepth)
        {
            case 8:
                data = Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            case 16:
                data = Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            case 24:
                data = Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            case 32:
                data = Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2);
                break;
            default:
                throw new Exception(bitDepth + " bit depth is not supported.");
        }

        AudioClip audioClip = AudioClip.Create(name, data.Length / channels, channels, sampleRate, false);
        audioClip.SetData(data, 0);
        return audioClip;
    }

    private static float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize,
            "Failed to get valid 8-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize,
            headerOffset);

        float[] data = new float[wavSize];

        float maxValue = byte.MaxValue / 2.0f;

        int i = 0;
        while (i < wavSize)
        {
            data[i] = Mathf.Clamp(source[i] / maxValue - 1.0f, -1.0f, 1.0f);
            ++i;
        }

        return data;
    }

    private static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize,
            "Failed to get valid 16-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize,
            headerOffset);

        int x = sizeof(short); // block size = 2
        int convertedSize = wavSize / x;

        float[] data = new float[convertedSize];

        short maxValue = short.MaxValue;

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}",
            data.Length, convertedSize);

        return data;
    }

    private static float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize,
            "Failed to get valid 24-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize,
            headerOffset);

        int x = 3; // block size = 3
        int convertedSize = wavSize / x;

        int maxValue = int.MaxValue;

        float[] data = new float[convertedSize];

        byte[]
            block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            Buffer.BlockCopy(source, offset, block, 1, x);
            data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}",
            data.Length, convertedSize);

        return data;
    }

    private static float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        int wavSize = BitConverter.ToInt32(source, headerOffset);
        headerOffset += sizeof(int);
        Debug.AssertFormat(wavSize > 0 && wavSize == dataSize,
            "Failed to get valid 32-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize,
            headerOffset);

        int x = sizeof(float); //  block size = 4
        int convertedSize = wavSize / x;

        int maxValue = int.MaxValue;

        float[] data = new float[convertedSize];

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}",
            data.Length, convertedSize);

        return data;
    }

    private static string FormatCode(ushort code)
    {
        switch (code)
        {
            case 1:
                return "PCM";
            case 2:
                return "ADPCM";
            case 3:
                return "IEEE";
            case 7:
                return "μ-law";
            case 65534:
                return "WaveFormatExtensable";
            default:
                Debug.LogWarning("Unknown wav code format:" + code);
                return "";
        }
    }
}
