using UnityEngine;

namespace LLMVRPrototype.Assets
{
    // used at runtime, when processing a scene-editing request
    public static class OperationCompatibility
    {
        public static bool SupportsOperation(AssetDefinition asset, string operationId)
        {
            if (asset == null || string.IsNullOrWhiteSpace(operationId))
            {
                return false;
            }

            foreach (string supportedId in asset.SupportedOperationIds)
            {
                if (string.Equals(
                    supportedId,
                    operationId,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetCompatibleOperation(
            AssetDefinition asset,
            string operationId,
            OperationCatalog catalog,
            out OperationDefinition operation)
        {
            operation = null;

            if (asset == null || catalog == null)
                return false;

            if (!SupportsOperation(asset, operationId))
                return false;

            if (!catalog.TryGetById(operationId, out operation))
                return false;

            return operation != null && operation.IsEnabled;
        }
    }
}
