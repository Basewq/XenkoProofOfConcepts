using Stride.Core.Serialization;

namespace MultiplayerExample.Utilities
{
    public static class UrlReferenceExt
    {
        public static string GetContentName(this UrlReferenceBase urlReference)
        {
            if (urlReference.IsEmpty)
            {
                return "";
            }
            string urlPath = urlReference.Url;
            string contentName = urlPath;
            int slashIndex = urlPath.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                contentName = urlPath.Substring(slashIndex + 1);
            }
            return contentName;
        }
    }
}
