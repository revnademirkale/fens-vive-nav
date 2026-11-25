#if UNITY_ANDROID
using System.IO;
using System.Xml;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.XR.OpenXR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;

/// <summary>
/// Meta Quest: turns off the boundary (guardian) for this app, so participants can walk the long routes
/// in the real FENS building without the "you've moved too far" warning.
/// At every Android build it adds this line to the AndroidManifest.xml that Unity generates:
///   <uses-feature android:name="com.oculus.feature.BOUNDARYLESS_APP" android:required="true"/>
/// Only for META QUEST builds: does nothing when "Meta Quest Support" is off (= VIVE build).
/// Nothing else in the manifest is changed. If the line is already there, nothing happens.
/// Notes (Meta "Boundaryless" docs):
///  - Works only in the standalone APK on the headset, not over PC / Quest Link.
///  - The system checks that passthrough is active before hiding the boundary.
///  - Without the boundary, the experiment staff are responsible for the participant's safety.
/// </summary>
public class FensQuestBoundarylessManifest : IPostGenerateGradleAndroidProject
{
    const string AndroidNs = "http://schemas.android.com/apk/res/android";
    const string Feature = "com.oculus.feature.BOUNDARYLESS_APP";
    const string Tag = "[FENS Quest Boundaryless]";

    public int callbackOrder => 999; // after Unity / OpenXR have written their manifest entries

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        bool quest = settings != null && settings.GetFeatures<OpenXRFeature>()
            .Any(f => f != null && f.enabled && f.GetType().Name == "MetaQuestFeature");
        if (!quest) return; // VIVE build: leave the manifest alone

        // 'path' is the unityLibrary module folder of the generated Gradle project
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"{Tag} AndroidManifest.xml not found at {manifestPath}. Boundary NOT disabled.");
            return;
        }

        var doc = new XmlDocument();
        doc.Load(manifestPath);
        var root = doc.DocumentElement; // <manifest>
        if (root == null)
        {
            Debug.LogWarning($"{Tag} Empty manifest. Boundary NOT disabled.");
            return;
        }

        foreach (XmlNode node in root.SelectNodes("uses-feature"))
        {
            var name = (node as XmlElement)?.GetAttribute("name", AndroidNs);
            if (name == Feature)
            {
                Debug.Log($"{Tag} {Feature} already in the manifest.");
                return;
            }
        }

        var el = doc.CreateElement("uses-feature");
        el.SetAttribute("name", AndroidNs, Feature);
        el.SetAttribute("required", AndroidNs, "true");
        root.AppendChild(el);
        doc.Save(manifestPath);
        Debug.Log($"{Tag} Added {Feature} to the Android manifest (boundary off in the APK).");
    }
}
#endif
