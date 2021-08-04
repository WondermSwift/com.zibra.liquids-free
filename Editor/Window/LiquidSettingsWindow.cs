#if UNITY_2019_4_OR_NEWER
using UnityEngine;
using UnityEngine.UIElements;

namespace com.zibra.liquid.Editor
{
    class LiquidSettingsWindow : PackageSettingsWindow<LiquidSettingsWindow>
    {
	    internal override IPackageInfo GetPackageInfo()
#if ZIBRA_PLUGIN
		    => new PluginInfo();
#else
            => new ZibraAiPackageInfo(ZibraAIPackage.PackageName);
#endif

        protected override void OnWindowEnable(VisualElement root)
        {
            //AddTab("Settings", new SettingsTab());
            AddTab("Info", new AboutTab());
        }

        public static GUIContent WindowTitle => new GUIContent(ZibraAIPackage.DisplayName);
    }
}
#endif