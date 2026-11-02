#if ADSIAPKIT_UGUI
using UnityEngine.UI;

namespace AdsIAPKit
{
    /// <summary>Sets text on a legacy Text or a TextMeshPro component, so shop cards work with either.</summary>
    internal static class ShopText
    {
        public static void Set(Graphic graphic, string text)
        {
            if (graphic == null) return;
            if (graphic is Text legacy) { legacy.text = text; return; }
#if ADSIAPKIT_TMP
            if (graphic is TMPro.TMP_Text tmp) { tmp.text = text; return; }
#endif
        }
    }
}
#endif
