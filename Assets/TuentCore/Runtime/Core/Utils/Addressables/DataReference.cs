using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tuent.Core
{
    [Serializable]
    public class DataReference : AssetReferenceT<ScriptableObject>
    {
        public DataReference(string guid) : base(guid)
        {
        }
    }
}