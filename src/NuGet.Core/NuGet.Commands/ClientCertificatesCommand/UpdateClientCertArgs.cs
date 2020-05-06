// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Commands
{
    public partial class UpdateClientCertArgs : IClientCertArgsWithPackageSource,
                                                IClientCertArgsWithConfigFile,
                                                IClientCertArgsWithFileData,
                                                IClientCertArgsWithStoreData,
                                                IClientCertArgsWithForce
    {
    }
}
