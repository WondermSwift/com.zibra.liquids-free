namespace com.zibra.liquid.Editor
{
	static class ZibraAIPackage
	{
		public const string ZibraAiSupportEmail = "support@zibraai.com";
		public const string ZibraAiCeoEMail = "ceo@zibraai.com";
		public const string ZibraAiWebsiteRootUrl = "https://zibra.ai/";
		public const string ZibraAiLinkedinUrl = "https://www.linkedin.com/company/zibra-ai/";
		public const string ZibraAiFBUrl = "https://www.facebook.com/zibraAI/";
		public const string ZibraAiYoutubeUrl = "https://www.youtube.com/channel/UC2AkLxzSIFTB1E2lvNs73xg";
		public const string ZibraAiDiscordUrl = "https://discord.gg/G4FNajB3D9";

		public const string PackageName = "com.zibra.liquids-free";
		public const string DisplayName = "Zibra AI - Liquids";
		public const string RootMenu = "Zibra AI/" + DisplayName + "/";

#if ZIBRA_PLUGIN
        public static readonly string RootPath = "Assets/Plugins/Zibra/Liquids";
#else
		public static readonly string RootPath = PackageManagerUtility.GetPackageRootPath(PackageName);
#endif

		internal static readonly string WindowTabsPath = $"{RootPath}/Editor/Window/Tabs";

		internal static readonly string UIToolkitPath = $"{RootPath}/Editor/UIToolkit";
		internal static readonly string UIToolkitControlsPath = $"{UIToolkitPath}/Controls";

		public static readonly string EditorArtAssetsPath = $"{RootPath}/Editor/Art";
		public static readonly string EditorIconAssetsPath = $"{EditorArtAssetsPath}/Icons";
		public static readonly string EditorFontAssetsPath = $"{EditorArtAssetsPath}/Fonts";
	}
}
