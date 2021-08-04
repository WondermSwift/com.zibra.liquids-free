using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_2019_4_OR_NEWER
namespace com.zibra.liquid.Editor
{
    class AboutTab : BaseTab
    {
        public AboutTab()
            : base($"{ZibraAIPackage.UIToolkitPath}/AboutTab/AboutTab")

        {
            var supportMailHyperlink = this.Q<Hyperlink>("supportEmail");
            supportMailHyperlink.Link = "mailto:" + ZibraAIPackage.ZibraAiSupportEmail;
            
            var supportMailLabel = this.Q<Label>("supportEmailText");
            supportMailLabel.text = ZibraAIPackage.ZibraAiSupportEmail;
            
            var ceoMailHyperlink = this.Q<Hyperlink>("ceoEmail");
            ceoMailHyperlink.Link = "mailto:" + ZibraAIPackage.ZibraAiCeoEMail;
            
            var ceoMailLabel = this.Q<Label>("ceoEmailText");
            ceoMailLabel.text = ZibraAIPackage.ZibraAiCeoEMail;
            
            var linkedinHyperlink = this.Q<Hyperlink>("LinkedinElement");
            linkedinHyperlink.Link = ZibraAIPackage.ZibraAiLinkedinUrl;
            
            var fbHyperlink = this.Q<Hyperlink>("FacebookElement");
            fbHyperlink.Link = ZibraAIPackage.ZibraAiFBUrl;
            
            var youtubeHyperlink = this.Q<Hyperlink>("YoutubeElement");
            youtubeHyperlink.Link = ZibraAIPackage.ZibraAiYoutubeUrl;
            
            var discordHyperlink = this.Q<Hyperlink>("DicordElement");
            discordHyperlink.Link = ZibraAIPackage.ZibraAiDiscordUrl;
            
            var logoHyperlink = this.Q<Hyperlink>("LogoElement");
            logoHyperlink.Link = ZibraAIPackage.ZibraAiWebsiteRootUrl;
        }
    }
}
#endif