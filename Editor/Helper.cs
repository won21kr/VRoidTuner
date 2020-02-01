using UnityEngine;
using UnityEditor;
using VRM;
using System.Linq;

namespace VRoidTuner
{

    internal class Helper
    {

        private static Color[] GizmoColors = new Color[]{
            new Color(0.0f, 0.0f, 0.0f, 1.0f),
            new Color(1.0f, 0.0f, 0.0f, 1.0f),
            new Color(1.0f, 0.5f, 0.0f, 1.0f),
            new Color(1.0f, 1.0f, 0.0f, 1.0f),
            new Color(0.5f, 1.0f, 0.0f, 1.0f),
            new Color(0.0f, 1.0f, 0.0f, 1.0f),
            new Color(0.0f, 1.0f, 0.5f, 1.0f),
            new Color(0.0f, 1.0f, 1.0f, 1.0f),
            new Color(0.0f, 0.5f, 1.0f, 1.0f),
            new Color(0.0f, 0.0f, 1.0f, 1.0f),
            new Color(0.5f, 0.0f, 1.0f, 1.0f),
            new Color(1.0f, 0.0f, 1.0f, 1.0f),
            new Color(1.0f, 0.0f, 0.5f, 1.0f),
            new Color(0.5f, 0.5f, 0.5f, 1.0f),
            new Color(1.0f, 1.0f, 1.0f, 1.0f),
        };

        internal static Color GizmoColor(int i = -1)
        {
            if (i < 0) i = UnityEngine.Random.Range(0, GizmoColors.Length);
            return GizmoColors[i % GizmoColors.Length];
        }

        /// <summary>
        /// 指定オブジェクト群にアタッチされているコンポーネントをすべて取得します。
        /// </summary>
        /// <typeparam name="T">検索対象コンポーネントの型</typeparam>
        private static T[] GetAllComponents<T>(Object[] objs)
        {
            var result = Enumerable.Empty<T>();
            foreach (GameObject obj in objs)
            {
                result = result.Union(obj.GetComponents<T>());
            }
            return result.ToArray();
        }

        /// <summary>
        /// ヒエラルキーに含まれる全オブジェクトを対象に、アタッチされているコンポーネントをすべて検索します。
        /// </summary>
        /// <typeparam name="T">検索対象コンポーネントの型</typeparam>
        internal static T[] FindAllComponents<T>()
        {
            var objs = Resources.FindObjectsOfTypeAll<GameObject>();
            return GetAllComponents<T>(objs);
        }

        /// <summary>
        /// 選択中のオブジェクトをルートとするツリーを対象に、アタッチされているコンポーネントをすべて検索します。
        /// </summary>
        /// <typeparam name="T">検索対象コンポーネントの型</typeparam>
        internal static T[] FindAllComponentsInSelected<T>()
        {
            var objs = Selection.GetFiltered<GameObject>(SelectionMode.Deep);
            return GetAllComponents<T>(objs);
        }

    }

}
