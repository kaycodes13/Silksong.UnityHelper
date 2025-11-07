using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UnityHelper.Util;

public static class WavUtil
{
    /// <summary>
    /// Read a file from embedded resources and output a Unity <see cref="AudioClip"/>.
    /// </summary>
    /// <param name="path">Embedded resource path of wav file</param>
    /// <param name="asm">The assembly to get the embedded resource from</param>
    /// <param name="name">Name that the Unity <see cref="AudioClip"/> should have</param>
    /// <returns>A Unity <see cref="AudioClip"/></returns>
    public static AudioClip AudioClipFromEmbeddedResource(string path, Assembly asm, string? name = null)
    {
        name ??= path;

        using Stream stream = asm.GetManifestResourceStream(path);

        return AudioClipFromStream(stream, name);
    }

    /// <summary>
    /// Read a file from disc and output a Unity <see cref="AudioClip"/>.
    /// </summary>
    /// <param name="fileName">Path of wav file</param>
    /// <param name="name">Name that the Unity <see cref="AudioClip"/> should have</param>
    /// <returns>A Unity <see cref="AudioClip"/></returns>
    public static AudioClip AudioClipFromFile(string fileName, string? name = null)
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

        using Stream stream = File.Open(fileName, FileMode.Open);

        return AudioClipFromStream(stream, name);
    }

    /// <summary>
    /// Converts audio data from a stream into an Unity <see cref="AudioClip"/> using WavLib.
    /// </summary>
    /// <param name="dataStream">The wav data stream</param>
    /// <param name="origName">The Unity <see cref="AudioClip"/> object name. optional.</param>
    /// <returns>The Unity <see cref="AudioClip"/>.</returns>
    public static AudioClip AudioClipFromStream(Stream dataStream, string origName = "")
    {
        WavLib.WavData wavData = new WavLib.WavData();

        float[] wavSoundData = wavData.GetSamples();
        AudioClip audioClip = AudioClip.Create(origName, wavSoundData.Length / wavData.FormatChunk.NumChannels, wavData.FormatChunk.NumChannels, (int)wavData.FormatChunk.SampleRate, false);
        audioClip.SetData(wavSoundData, 0);

        return audioClip;
    }
}
