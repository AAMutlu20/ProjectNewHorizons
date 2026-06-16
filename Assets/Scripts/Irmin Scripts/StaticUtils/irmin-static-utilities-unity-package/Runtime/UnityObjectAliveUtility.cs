using UnityEngine;

namespace IrminStaticUtilities.Tools
{
    public static class UnityObjectAliveUtility
    {
        public static bool IsInterfaceObjectDestroyed<T>(T pInterfaceToTest)
        {
            if (pInterfaceToTest is UnityEngine.Object foundObject)
                return foundObject == null;
            return pInterfaceToTest == null;
        }
    }
}