#if !ZIBRA_PLUGIN
using UnityEditor.PackageManager;

namespace com.zibra.liquid.Editor
{
	class ZibraAiPackageInfo : IPackageInfo
	{
		public string DisplayName => m_PackageInfo.displayName;
		public string Description => m_PackageInfo.description;
		public string Version => m_PackageInfo.version;

		PackageInfo m_PackageInfo;
		public ZibraAiPackageInfo(string packageName)
		{
			m_PackageInfo = PackageManagerUtility.GetPackageInfo(packageName);
		}
	}
}
#endif
