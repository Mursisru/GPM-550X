using System.IO;
using UnityEditor;

public static class BatchBuild
{
    public static void Build()
    {
        Gpm.UnityBake.NobpBundleBuilder.Build();
    }
}
