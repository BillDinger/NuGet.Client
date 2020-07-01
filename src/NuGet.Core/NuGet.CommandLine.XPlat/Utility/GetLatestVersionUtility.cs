// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NuGet.CommandLine.XPlat.Utility
{
    public static class GetLatestVersionUtility
    {
        public static async Task<NuGetVersion> GetLatestVersionFromSources(IList<PackageSource> sources, ILogger logger, string packageId, bool prerelease)
        {
            var maxTasks = Environment.ProcessorCount;
            var tasks = new List<Task<NuGetVersion>>();
            var latestReleaseList = new List<NuGetVersion>();

            foreach (var source in sources)
            {
                tasks.Add(Task.Run(() => GetLatestVersionFromSource(source, logger, packageId, prerelease)));
                if (maxTasks <= tasks.Count)
                {
                    var finishedTask = await Task.WhenAny(tasks);
                    tasks.Remove(finishedTask);
                    latestReleaseList.Add(await finishedTask);
                }
            }

            await Task.WhenAll(tasks);

            foreach (var t in tasks)
            {
                latestReleaseList.Add(await t);
            }

            ValidateVersion(latestReleaseList);
            return latestReleaseList.Max();
        }

        public static async Task<NuGetVersion> GetLatestVersionFromSource(PackageSource source, ILogger logger, string packageId, bool prerelease)
        {
            SourceRepository repository = Repository.Factory.GetCoreV3(source);
            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            using (var cache = new SourceCacheContext())
            {
                IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                    packageId,
                    includePrerelease: prerelease,
                    includeUnlisted: false,
                    cache,
                    logger,
                    CancellationToken.None
                );

                return packages.LastOrDefault()?.Identity.Version;
            }
        }

        public static List<PackageSource> EvaluateSources(IList<PackageSource> requestedSources, IList<string> configFilePaths)
        {
            using (var settingsLoadingContext = new SettingsLoadingContext())
            {
                var settings = Settings.LoadImmutableSettingsGivenConfigPaths(configFilePaths, settingsLoadingContext);
                var packageSources = new List<PackageSource>();

                var packageSourceProvider = new PackageSourceProvider(settings);
                var packageProviderSources = packageSourceProvider.LoadPackageSources();

                for (var i = 0; i < requestedSources.Count; i++)
                {
                    var matchedSource = packageProviderSources.FirstOrDefault(e => e.Source == requestedSources[i].Source);
                    if (matchedSource == null)
                    {
                        packageSources.Add(requestedSources[i]);
                    }
                    else
                    {
                        packageSources.Add(matchedSource);
                    }
                }

                return packageSources;
            }
        }

        private static void ValidateVersion(List<NuGetVersion> versions)
        {
            if (versions.Count == 1 && versions[0] == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.Error_NoPrereleaseVersions));
            }
        }
    }
}
