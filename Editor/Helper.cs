using UnityEngine;
using UnityEditor;
using VRM;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VRMHelper
{

    internal class Helper
    {

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
            var objs = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            return GetAllComponents<T>(objs);
        }

        /// <summary>
        /// 選択中のオブジェクトをルートとするツリーを対象に、アタッチされているコンポーネントをすべて検索します。
        /// </summary>
        /// <typeparam name="T">検索対象コンポーネントの型</typeparam>
        internal static T[] FindAllComponentsInSelected<T>()
        {
            var objs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Deep);
            return GetAllComponents<T>(objs);
        }

        /// <summary>
        /// ヒエラルキー中およびプロジェクトアセット中で選択中のオブジェクトがVRMモデルであるかを調べます。
        /// </summary>
        internal static (bool inHierarchy, bool inProject) IsVRMSelected()
        {
            var inHierarchy = false;
            var inProject = false;
            foreach (GameObject obj in Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel))
            {
                if (obj.GetComponents<VRMMeta>().Length == 0) continue;
                if (AssetDatabase.Contains(obj))
                    inProject = true;
                else
                    inHierarchy = true;
            }
            return (inHierarchy, inProject);
        }

    }

}
