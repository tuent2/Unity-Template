using System.Collections.Generic;
using UnityEngine;

namespace Tuent.Core
{
    [CreateAssetMenu(menuName = "Tuent/Audio/Audio Database", fileName = "AudioDatabase")]
    public class AudioDatabase : ScriptableObject
    {
        public List<AudioIdentityReference> identityReferences = new();
    }
}

