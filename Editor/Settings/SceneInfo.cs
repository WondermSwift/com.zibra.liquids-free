using System;
using UnityEngine.SceneManagement;

namespace com.zibra.liquid.Editor
{
    [Serializable]
    class SceneStateInfo
    {
        public string Path;
        public bool WasLoaded;

        public SceneStateInfo(Scene scene)
        {
            Path = scene.path;
            WasLoaded = scene.isLoaded;
        }
    }
}
