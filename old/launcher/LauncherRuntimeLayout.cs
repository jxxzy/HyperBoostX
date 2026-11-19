using System.IO;

namespace HyperBoostLauncher
{
    public static class LauncherRuntimeLayout
    {
        public static string ResolveDirectory(string appRoot, string installRoot, params string[] candidateSubdirs)
        {
            foreach (var subdir in candidateSubdirs)
            {
                var preferred = Path.Combine(appRoot, subdir);
                if (Directory.Exists(preferred))
                    return preferred;

                var sibling = Path.Combine(installRoot, subdir);
                if (Directory.Exists(sibling))
                    return sibling;
            }

            return appRoot;
        }

        public static string ResolveFile(string appRoot, string installRoot, string fileName, params string[] candidateSubdirs)
        {
            foreach (var subdir in candidateSubdirs)
            {
                var preferred = Path.Combine(appRoot, subdir, fileName);
                if (File.Exists(preferred))
                    return preferred;

                var sibling = Path.Combine(installRoot, subdir, fileName);
                if (File.Exists(sibling))
                    return sibling;
            }

            var root = Path.Combine(appRoot, fileName);
            if (File.Exists(root))
                return root;

            return Path.Combine(appRoot, candidateSubdirs.Length > 0 ? candidateSubdirs[0] : string.Empty, fileName);
        }

        public static string ResolveFileFromCandidates(string appRoot, string installRoot, string[] fileNames, params string[] candidateSubdirs)
        {
            foreach (var fileName in fileNames)
            {
                var resolved = ResolveFile(appRoot, installRoot, fileName, candidateSubdirs);
                if (File.Exists(resolved))
                    return resolved;
            }

            var firstFile = fileNames.Length > 0 ? fileNames[0] : string.Empty;
            return Path.Combine(appRoot, candidateSubdirs.Length > 0 ? candidateSubdirs[0] : string.Empty, firstFile);
        }
    }
}
