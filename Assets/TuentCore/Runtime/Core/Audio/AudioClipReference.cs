using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tuent.Core
{
    [Serializable]
    public class AudioClipReference : AssetReferenceT<AudioClip>
    {
        public AudioClipReference(string guid) : base(guid)
        {
        }
    }
}