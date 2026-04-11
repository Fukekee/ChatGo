using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ChatGo.EditorTools
{
    public static class PlayerVisualAnimatorSetup
    {
        private const string RootFolder = "Assets/Animations/Player";
        private const string ControllerPath = RootFolder + "/PlayerVisual.controller";
        private const string ClipIdleFront = RootFolder + "/IdleFront.anim";
        private const string ClipIdleSide = RootFolder + "/IdleSide.anim";
        private const string ClipWalk = RootFolder + "/Walk.anim";

        [MenuItem("ChatGo/Player/Create Player Visual Animator Assets")]
        public static void CreateAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.CreateFolder("Assets/Animations", "Player");
            }

            CreateClipIfMissing(ClipIdleFront, "IdleFront");
            CreateClipIfMissing(ClipIdleSide, "IdleSide");
            CreateClipIfMissing(ClipWalk, "Walk");

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Player Visual",
                        "已存在 " + ControllerPath + "，是否覆盖？",
                        "覆盖",
                        "取消"))
                {
                    return;
                }

                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var idleFront = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipIdleFront);
            var idleSide = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipIdleSide);
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipWalk);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("FacingRight", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HasMoved", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorState stIdleFront = root.AddState("IdleFront");
            stIdleFront.motion = idleFront;
            AnimatorState stWalk = root.AddState("Walk");
            stWalk.motion = walk;
            AnimatorState stIdleSide = root.AddState("IdleSide");
            stIdleSide.motion = idleSide;

            root.defaultState = stIdleFront;

            AddInstantTransition(stIdleFront, stWalk, AnimatorConditionMode.Greater, 0.01f, "Speed");

            AnimatorStateTransition tWalkToSide = stWalk.AddTransition(stIdleSide);
            tWalkToSide.hasExitTime = false;
            tWalkToSide.duration = 0f;
            tWalkToSide.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
            tWalkToSide.AddCondition(AnimatorConditionMode.If, 0f, "HasMoved");

            AnimatorStateTransition tWalkToFront = stWalk.AddTransition(stIdleFront);
            tWalkToFront.hasExitTime = false;
            tWalkToFront.duration = 0f;
            tWalkToFront.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
            tWalkToFront.AddCondition(AnimatorConditionMode.IfNot, 0f, "HasMoved");

            AddInstantTransition(stIdleSide, stWalk, AnimatorConditionMode.Greater, 0.01f, "Speed");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(controller);
            Debug.Log($"ChatGo: 已生成 {ControllerPath}。请在 IdleFront / IdleSide / Walk 三个 Animation Clip 里为 SpriteRenderer 绑定精灵（走路为 8 帧）。");
        }

        private static void CreateClipIfMissing(string path, string clipName)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            {
                return;
            }

            var clip = new AnimationClip { name = clipName };
            clip.wrapMode = clipName == "Walk" ? WrapMode.Loop : WrapMode.ClampForever;
            AssetDatabase.CreateAsset(clip, path);
        }

        private static void AddInstantTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string param)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.AddCondition(mode, threshold, param);
        }
    }
}
