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
    public static void SetLegacy()
    {
        SetLegacy(Selection.objects);
    }

    public static void SetLegacy(Object[] objects)
    {
        foreach (Object selectedObj in objects)
        {
            SetLegacy(selectedObj);
        }
    }

    public static void SetLegacy(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// 复制选中的fbx中的动画文件
    /// </summary>
    [MenuItem("Assets/Animation Tools/Dulicate Clips")]
    public static void DulicateClips()
    {
        DulicateClips(Selection.objects);
    }
    public static void DulicateClips(Object[] objects)
    {
        foreach (Object obj in objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                continue;

            Object[] importObjects = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath);
            foreach (Object asset in importObjects)
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip != null && clip.name.IndexOf("__preview__") == -1)
                {
                    AnimationClip newClip = DuplicateClip(clip);
                }
            }
        }
    }

    public static AnimationClip DuplicateClip(AnimationClip sourceClip)
    {
        if (sourceClip != null)
        {
            string path = AssetDatabase.GetAssetPath(sourceClip);
            path = Path.Combine(Path.GetDirectoryName(path), sourceClip.name) + ".anim";
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
            AnimationClip newClip = new AnimationClip();
            EditorUtility.CopySerialized(sourceClip, newClip);
            AssetDatabase.CreateAsset(newClip, newPath);
            return newClip;
        }
        return null;
    }


    /// <summary>
    /// 复制选中的动画的事件
    /// </summary>
    [MenuItem("Assets/Animation Tools/Copy Events")]
    public static void CopyAnimationEvents()
    {
        CopyAnimationEvents(Selection.objects);
    }

    public static void CopyAnimationEvents(Object[] objects)
    {
        EventsClipboard.Clear();
        foreach (Object selectedObj in objects)
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
    public static void PastAnimationEvents()
    {
        PastAnimationEvents(Selection.objects);
    }

    public static void PastAnimationEvents(Object[] objects)
    {
        foreach (Object selectedObj in objects)
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

}