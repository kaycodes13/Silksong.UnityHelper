using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityHelper.Extensions;

public static class UnityExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }

    /// <inheritdoc cref="GetNameInHierarchy(Transform)"/>
    public static string GetNameInHierarchy(this GameObject go) => go.transform.GetNameInHierarchy();

    /// <summary>
    /// Get the name of the gameobject in the form .../grandparent/parent/self starting at the root.
    /// There is no leading slash.
    /// </summary>
    public static string GetNameInHierarchy(this Transform t)
    {
        List<string> parts = [t.gameObject.name];

        while (t.parent != null)
        {
            t = t.parent;
            parts.Add(t.name);
        }

        parts.Reverse();
        return string.Join('/', parts);
    }

    /// <summary>
    /// Find a game object by name in the scene. The object's name must be given in the hierarchy.
    /// </summary>
    /// <param name="scene">The scene to search.</param>
    /// <param name="objName">The name of the object in the hierarchy, with '/' separating parent GameObjects from child GameObjects.</param>
    /// <returns>The GameObject if found; null if not.</returns>
    public static GameObject? FindGameObject(this Scene scene, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string[] pathParts = path.Split('/');
        string rootName = pathParts[0];

        GameObject? root = scene.GetRootGameObjects().FirstOrDefault(x => x.name == rootName);
        if (root == null)
        {
            return null;
        }

        if (pathParts.Length == 1)
        {
            return root;
        }

        string childPath = path[(rootName.Length + 1)..];
        Transform childTransform = root.transform.Find(childPath);

        if (childTransform != null)
        {
            return childTransform.gameObject;
        }

        return null;
    }
}
