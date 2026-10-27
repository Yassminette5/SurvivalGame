#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

// Ajoute au manifeste Android la permission du podometre (bonus) : ACTIVITY_RECOGNITION.
public class AndroidPermissionsPostProcessor : IPostGenerateGradleAndroidProject
{
    const string AndroidNs = "http://schemas.android.com/apk/res/android";

    public int callbackOrder => 1;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath)) return;

        var doc = new XmlDocument();
        doc.Load(manifestPath);
        var manifest = doc.DocumentElement;
        AddPermission(doc, manifest, "android.permission.ACTIVITY_RECOGNITION");
        doc.Save(manifestPath);
    }

    static void AddPermission(XmlDocument doc, XmlElement manifest, string permission)
    {
        foreach (XmlNode node in manifest.SelectNodes("uses-permission"))
            if (node.Attributes?["android:name"]?.Value == permission) return;

        var element = doc.CreateElement("uses-permission");
        element.SetAttribute("name", AndroidNs, permission);
        manifest.AppendChild(element);
    }
}
#endif
