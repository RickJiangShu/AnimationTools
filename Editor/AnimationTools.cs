using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/*
 * 动画工具集
 * 
 * 需求：美术上传了新的动画FBX文件，如何快速替换原先的动画文件，关键是之前的动画都加好了帧事件。
 * 
 * 1、将FBX文件动画类型改为Legacy                 OK
 * 2、Copy出其中所有的动画文件（为添加事件）      OK
 * 3、复制之前的动画事件
 * 4、粘贴到新的动画事件上面
 * 
 */
public class AnimationTools : Editor
{
    /// <summary>
    /// 动画事件粘贴板（以名字为索引）
    /// </summary>
    private static Dictionary<string, AnimationEvent[]> EventsClipboard = new Dictionary<string, AnimationEvent[]>();

    /// <summary>
    /// 将选中的fbx文件的动画格式改成Legacy
    /// </summary>
    [MenuItem("Assets/Animation Tools/Set Legacy")]
    static void SetLegacy()
    {
        foreach (Object selectedObj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObj);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.SaveAndReimport();
        }

    }

    /// <summary>
    /// 赋值选中的fbx中的动画文件
    /// </summary>
    [MenuItem("Assets/Animation Tools/Dulicate Clips")]
    static void DulicateClips()
    {
        foreach (Object selectedObj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObj);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            Object[] importObjects = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath);
            foreach (Object obj in importObjects)
            {
                AnimationClip clip = obj as AnimationClip;
                if (clip != null && clip.name.IndexOf("__preview__") == -1)
                {
                    AnimationClip newClip = DuplicateAnimationClip(clip);
                }
            }
        }
    }

    /// <summary>
    /// 复制选中的动画的事件
    /// </summary>
    [MenuItem("Assets/Animation Tools/Copy Events")]
    static void CopyAnimationEvents()
    {
        EventsClipboard.Clear();
        foreach (Object selectedObj in Selection.objects)
        {
            AnimationClip clip = selectedObj as AnimationClip;
            if (clip == null)
                continue;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            EventsClipboard.Add(clip.name, events);
        }
    }


    /// <summary>
    /// 粘贴剪切板中的事件到动画上
    /// </summary>
    [MenuItem("Assets/Animation Tools/Past Events")]
    static void PastAnimationEvents()
    {
        foreach (Object selectedObj in Selection.objects)
        {
            AnimationClip clip = selectedObj as AnimationClip;
            if (clip == null)
                continue;

            AnimationEvent[] events;
            EventsClipboard.TryGetValue(clip.name, out events);
            if (events == null)
                continue;

            AnimationUtility.SetAnimationEvents(clip, events);
        }
        EventsClipboard.Clear();
    }



    private static AnimationClip DuplicateAnimationClip(AnimationClip sourceClip)
    {
        if (sourceClip != null)
        {
            string path = AssetDatabase.GetAssetPath(sourceClip);
            path = Path.Combine(Path.GetDirectoryName(path), sourceClip.name) + ".anim";
            string newPath = AssetDatabase.GenerateUniqueAssetPath (path);
            AnimationClip newClip = new AnimationClip();
            EditorUtility.CopySerialized(sourceClip, newClip);
            AssetDatabase.CreateAsset(newClip, newPath);
            return newClip;
        }
        return null;
    }
}

/**
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Reflection;
 
public class Exp
{
[MenuItem("Assets/导出当前选中的模型的动作")]
private static void ExportThis()
{
Object activeObj = Selection.activeObject;
string objPath = AssetDatabase.GetAssetPath(activeObj);
ModelImporter modelImport = AssetImporter.GetAtPath(objPath) as ModelImporter;
if (null == modelImport)
{
return;
}
ModelImporterClipAnimation [] animationClips = modelImport.clipAnimations;
//         foreach (ModelImporterClipAnimation clip in animationClips)
//         {
//             clip.maskSource
//         }
UnityEngine.Object[] importObjects = AssetDatabase.LoadAllAssetsAtPath(modelImport.assetPath);
string animationClipsFolder = CreateAnimationClipsFolder(modelImport);
foreach (Object obj in importObjects)
{
if (obj.GetType() == typeof(AnimationClip))
{
SetAnimationEvents(CopyClip(obj as AnimationClip, animationClipsFolder));
}
//Debug.Log("obj.type =" + obj.GetType() + "/name =" + obj.name);
}
 
}
 
/// <summary>
/// 根据名字来设置动画事件
/// </summary>
/// <param name="dstClip"></param>
public static void SetAnimationEvents(AnimationClip dstClip)
{
if (dstClip != null)
{
AnimationEventManager animationEventManager = new AnimationEventManager(dstClip);
//设置动画帧率//
dstClip.frameRate = 30;
switch(dstClip.name)
{
case "zombie_attack":
dstClip.wrapMode = WrapMode.Default;
break;
case "zombie_idle1":
dstClip.wrapMode = WrapMode.Loop;
break;
case "zombie_walk":
dstClip.wrapMode = WrapMode.Loop;
break;
default:
dstClip.wrapMode = WrapMode.Default;
break;
}
//在每个动作里面都添加一个动作结束的事件//
animationEventManager.AddAnimationEvent(dstClip.length, "AnimationEventEnd");
animationEventManager.SaveAnimationEvent();
}
}
 
/// <summary>
/// 复制Clip到目录copyPath里，并返回复制后的clip
/// </summary>
/// <param name="srcClip">源</param>
/// <param name="copyFolder">目标的文件夹</param>
/// <returns></returns>
private static AnimationClip CopyClip(AnimationClip srcClip, string copyFolder)
{
AnimationClip dstClip = null;
string copyPath = copyFolder + '/' + srcClip.name + ".anim";
if (File.Exists(copyPath))
{
AssetDatabase.DeleteAsset(copyPath);
}
dstClip = new AnimationClip();
dstClip.name = srcClip.name;
AssetDatabase.CreateAsset(dstClip, copyPath);
AssetDatabase.Refresh();
if (null == dstClip)
{
return null;
}
 
// Copy curves from imported to copy
//先拷贝浮点数据//
EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(srcClip);
for (int i = 0; i < bindings.Length; i++)
{
AnimationUtility.SetEditorCurve(dstClip, bindings[i], AnimationUtility.GetEditorCurve(srcClip, bindings[i]));
}
 
//再拷贝引用数据//
bindings = AnimationUtility.GetObjectReferenceCurveBindings(srcClip);
for (int i = 0; i < bindings.Length; ++i)
{
AnimationUtility.SetObjectReferenceCurve(dstClip, bindings[i],
AnimationUtility.GetObjectReferenceCurve(srcClip, bindings[i]));
}
if (dstClip.wrapMode == WrapMode.Loop)
{
AnimationClipSettings animSettings = AnimationUtility.GetAnimationClipSettings(dstClip);
animSettings.loopTime = true;
MethodInfo setAnimationClipSettingMethod = typeof(AnimationUtility).GetMethod("SetAnimationClipSettings", BindingFlags.NonPublic | BindingFlags.Static);
setAnimationClipSettingMethod.Invoke(null, new object[] { dstClip, animSettings });
}
return dstClip;
}
 
/// <summary>
/// 创建模块里的AnimationClips目录，如果存在，不创建
/// </summary>
/// <param name="modelImport"></param>
private static string CreateAnimationClipsFolder(ModelImporter modelImport)
{
if (null == modelImport)
{
//路径非模型，返回//
return null;
}
 
string modelPath = modelImport.assetPath;
string parentFolder = modelPath.Substring(0, modelPath.LastIndexOf('/'));
 
if (Directory.Exists(parentFolder+'/' + "AnimationClips"))
{
return parentFolder + '/' + "AnimationClips";
}
else
{
string guid = AssetDatabase.CreateFolder(parentFolder, "AnimationClips");
string folderPath = AssetDatabase.GUIDToAssetPath(guid);
AssetDatabase.Refresh();
return folderPath;
}
}
}
 * 
*/