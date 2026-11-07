using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
namespace PHEOC.OMS.Web.ViewSupport;
public class StringMetadata : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if(context.Key.ModelType==typeof(string))context.DisplayMetadata.ConvertEmptyStringToNull=false;
    }
}
