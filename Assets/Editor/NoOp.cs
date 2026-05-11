#if UNITY_EDITOR
using UnityEngine;

namespace CrowdDefense.Editor
{
    public static class NoOp
    {
        public static void Run()
        {
            Debug.Log("[NoOp] Compile check OK");
        }
    }
}
#endif
