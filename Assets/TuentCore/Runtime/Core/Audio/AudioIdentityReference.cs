using System;
using UnityEngine.AddressableAssets;

namespace Tuent.Core
{
    [Serializable]
    public class AudioIdentityReference : AssetReferenceT<AudioIdentity>
    {
        public AudioIdentityReference(string guid) : base(guid)
        {
        }
    }
}

