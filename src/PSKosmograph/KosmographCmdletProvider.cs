﻿using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using Kosmograph.LiteDb;
using Kosmograph.Messaging;
using Kosmograph.Model;
using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace PSKosmograph
{
    [CmdletProvider(KosmographCmdletProvider.Id, ProviderCapabilities.None)]
    public class KosmographCmdletProvider : Provider
    {
        static KosmographCmdletProvider()
        {
            NewKosmographService = _ => new KosmographLiteDbPersistence(new KosmographMessageBus());
        }

        public KosmographCmdletProvider()
        {
        }

        public const string Id = "Kosmograph";

        public static Func<string, IKosmographPersistence> NewKosmographService { get; set; }
            = _ => throw new InvalidOperationException($"{NewKosmographService} is uninitalized");

        private KosmographDriveInfo KosmographDriveInfo => (KosmographDriveInfo)this.PSDriveInfo;

        public KosmographDriveInfo GetKosmographDriveInfo(string path)
        {
            if (this.PSDriveInfo is { })
                return (KosmographDriveInfo)this.PSDriveInfo;

            var driveName = Drive.GetDriveName(path);
            return this.ProviderInfo
                .Drives
                .OfType<KosmographDriveInfo>()
                .Where(d => d.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        protected override IPathResolver PathResolver => new KosmographPathResolver();

        protected override IProviderContext CreateContext(string path, bool recurse, bool resolveFinalNodeFilterItems)
        {
            return new KosmographProviderContext(this, path, this.GetKosmographDriveInfo(path), new KosmographPathResolver(), this.DynamicParameters, recurse)
            {
                ResolveFinalNodeFilterItems = resolveFinalNodeFilterItems
            };
        }

        #region DriveCmdletProvider

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            //return new KosmographDriveInfo(drive, NewKosmographService(drive.Root));

            var persistence = NewKosmographService?.Invoke(drive.Root);
            if (persistence is null)
                throw new ArgumentNullException(nameof(NewKosmographService));

            return new KosmographDriveInfo(new PSDriveInfo(drive.Name, drive.Provider, $@"{drive.Name}:\", drive.Description, drive.Credential), persistence);
        }

        public ProviderInfo GetProviderInfo() => this.ProviderInfo;

        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            return base.RemoveDrive(drive);
        }

        #endregion DriveCmdletProvider

        #region ContainerCmdletProvider

        //protected override void GetChildItems(string path, bool recurse)
        //{
        //    base.GetChildItems(path, recurse);
        //}

        //protected override void GetChildItems(string path, bool recurse, uint depth)
        //{
        //    if (string.IsNullOrEmpty(path))
        //        this.KosmographProviderService.GetContainers().ForEach(c => this.WriteItemObject(c, this.MakeItemPath(c), c.IsContainer));
        //    else
        //        this.KosmographProviderService.GetItemsByPath(path).ForEach(i => this.WriteItemObject(i, this.MakeItemPath(path, i), i.IsContainer));
        //}

        //protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        //{
        //    return base.GetChildItemsDynamicParameters(path, recurse);
        //}

        //protected override object GetChildNamesDynamicParameters(string path)
        //{
        //    return base.GetChildNamesDynamicParameters(path);
        //}

        //protected override void GetChildNames(string path, ReturnContainers returnContainers)
        //{
        //    base.GetChildNames(path, returnContainers);
        //}

        //protected override string[] ExpandPath(string path)
        //{
        //    return base.ExpandPath(path);
        //}

        //protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
        //{
        //    return base.ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
        //}

        //protected override bool HasChildItems(string path)
        //{
        //    return base.HasChildItems(path);
        //}

        //private string MakeItemPath(KosmographContainer container) => $@"{this.PSDriveInfo.Name}:\{container.Name}";

        //private string MakeItemPath(string parentPath, KosmographItem item) => $@"{this.PSDriveInfo.Name}:\{parentPath}\{item.Name}";

        #endregion ContainerCmdletProvider

        #region NavigationCmdletProvider

        //protected override string GetChildName(string path)
        //{
        //    return base.GetChildName(this.EnsureOa path);
        //}

        //protected override string GetParentPath(string path, string root)
        //{
        //    return base.GetParentPath(path, root);
        //}

        //protected override bool IsItemContainer(string path)
        //{
        //    var (_, parsedPath) = KosmographPath.TryParse(path);
        //    if (parsedPath.IsRoot)
        //        return true;
        //    if (parsedPath.Items.Count() == 1)
        //        return this.KosmographProviderService.GetContainerByPath(parsedPath) != null;
        //    else return false;
        //}

        //protected override string NormalizeRelativePath(string path, string basePath)
        //{
        //    return base.NormalizeRelativePath(path, basePath);
        //}

        #endregion NavigationCmdletProvider
    }
}