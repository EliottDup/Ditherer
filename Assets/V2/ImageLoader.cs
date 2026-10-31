using System.IO;
using UnityEngine;

public static class ImageLoader
{
    public static Texture2D LoadImage(string path)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(path))
        {
            fileData = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(tex, fileData))
            {
                throw new System.Exception("Image failed to load!");
            }
        }
        return tex;
    }
}
