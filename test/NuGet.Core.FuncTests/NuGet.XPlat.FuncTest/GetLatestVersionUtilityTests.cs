// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using NuGet.CommandLine.XPlat.Utility;
using NuGet.Configuration;
using NuGet.Configuration.Test;
using NuGet.Test.Utility;
using Xunit;

namespace NuGet.XPlat.FuncTest
{
    public class GetLatestVersionUtilityTests
    {
        [Fact]
        private void EvaluateSources_Success()
        {
            using (var mockBaseDirectory = TestDirectory.Create())
            {
                List<PackageSource> source = new List<PackageSource>() { new PackageSource("https://contoso.org/v3/index.json"), new PackageSource("b") };

                var nugetConfigFileName = "NuGet.Config";
                var config = @"<?xml version=""1.0"" encoding=""utf-8""?>
                            <configuration>
                                <packageSources>
                                    <add key=""Contoso"" value=""https://contoso.org/v3/index.json"" />
                                    <add key=""b"" value =""b"" />
                                </packageSources>
                                <packageSourceCredentials>
                                    <Contoso>
                                        <add key=""Username"" value=""user @contoso.com"" />
                                        <add key=""Password"" value=""..."" />
                                    </Contoso>
                                </packageSourceCredentials>
                            </configuration>";
                
                var configPath = Path.Combine(mockBaseDirectory, nugetConfigFileName);
                SettingsTestUtils.CreateConfigurationFile(nugetConfigFileName, mockBaseDirectory, config);
                var settingsLoadContext = new SettingsLoadingContext();

                var settings = Settings.LoadImmutableSettingsGivenConfigPaths(new string[] { configPath }, settingsLoadContext);
                var result = GetLatestVersionUtility.EvaluateSources(source, settings.GetConfigFilePaths());

                // Asert
                Assert.Equal(2, result.Count);
                Assert.NotEqual(null, result[0].Credentials);
            }
        }
    }
}
