using AasCore.Aas3_0;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AasxPluginVws4ls.BasicAasUtils;

namespace AasxPluginVws4ls;

public static class VersionUtils
{
    public const string SEM_ID_PREVIOUS_VERSION = "https://arena2036.de/vws4ls/previous/1/0";
    public const string NAME_PREVIOUS_VERSION = "PreviousVersion";

    public static void AddReferenceToPreviousVersion(IIdentifiable currentVersion, IIdentifiable previousVersion)
    {
        var previousVersionExtension = CreatePreviousVersionExtension(previousVersion);
        
        if (currentVersion.Extensions == null)
        {
            currentVersion.Extensions = new List<IExtension>();
        }

        currentVersion.Extensions.Add(previousVersionExtension);
    }

    public static IExtension CreatePreviousVersionExtension(IIdentifiable previousVersion)
    {
        var referenceToPrevious = previousVersion.GetReference();
        return new Extension(
            NAME_PREVIOUS_VERSION, 
            CreateSemanticId(KeyTypes.GlobalReference, SEM_ID_PREVIOUS_VERSION), 
            refersTo: new List<IReference>() { referenceToPrevious }
        );
    }

    public static IExtension GetPreviousVersionExtension(IIdentifiable currentVersion)
    {
        return currentVersion?.Extensions?.FirstOrDefault(e => e.HasSemanticId(KeyTypes.GlobalReference, SEM_ID_PREVIOUS_VERSION));
    }
}
