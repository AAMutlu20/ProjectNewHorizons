using UnityEngine;

namespace IrminStaticUtilities.Tools
{
    public static class LayerUtility
    {
        public static bool IsInLayerMask(LayerMask pLayerMask, GameObject pGameObject)
        {
            return IsInLayerMask(pLayerMask, pGameObject.layer);
        }
        public static bool IsInLayerMask(LayerMask pLayerMask, int pLayer)
        {
            // Learned from Chat GPT this is how to check if a layer is in a layermask, has worked so far.
            bool isInMask = (pLayerMask & (1 << pLayer)) != 0;
            return isInMask;
        }
    }
}