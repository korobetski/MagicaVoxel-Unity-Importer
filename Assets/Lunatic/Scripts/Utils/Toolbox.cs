using UnityEngine;

namespace Lunatic.Utils
{
    public class Toolbox
    {
        public static void DestroyChildren(GameObject gameObject, bool coroutine = false)
        {
            int cc = gameObject.transform.childCount;
            if (cc > 0)
            {
                foreach (Transform child in gameObject.transform)
                {
#if UNITY_EDITOR
                    if (coroutine)
                    {
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            GameObject.DestroyImmediate(child.gameObject);
                        };
                    }
                    else
                    {
                        GameObject.DestroyImmediate(child.gameObject);
                    }
#else
            GameObject.Destroy(child.gameObject);
#endif
                }
            }
        }

        public static string NameFromPath(string path)
        {
            string[] subs = path.Split('/');
            return subs[subs.Length - 1];
        }
        public static bool IsWithin(int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }
    }
}