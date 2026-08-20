using UnityEngine;

namespace Core
{
    public class ChildrenLayerSetter : MonoBehaviour
    {
        public void ApplyLayerToChildren()
        {
            var targetLayer = transform.gameObject.layer;
            ApplyLayerToChildren(targetLayer);
        }

        public void ApplyLayerToChildren(string layerName)
        {
            int resolvedLayer = LayerMask.NameToLayer(layerName);
            if (resolvedLayer < 0)
                return;

            ApplyLayerToChildren(resolvedLayer);
        }

        public void ApplyLayerToChildren(int layer)
        {
            SetLayerRecursively(transform, layer);
        }

        private void SetLayerRecursively(Transform currentTransform, int layer)
        {
            currentTransform.gameObject.layer = layer;

            foreach (Transform child in currentTransform)
                SetLayerRecursively(child, layer);
        }
    }
}
