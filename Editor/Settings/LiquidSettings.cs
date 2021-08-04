using com.zibra.liquid.Plugins;

namespace com.zibra.liquid.Editor
{
    /// <summary>
    /// Scene Management Settings scriptable object.
    /// You can modify this settings using C# or Scene Management Editor Window.
    /// </summary>
    class LiquidSettings : PackageScriptableSettingsSingleton<LiquidSettings>
    {
        protected override bool IsEditorOnly => true;
        public override string PackageName => ZibraAIPackage.PackageName;
    }
}
