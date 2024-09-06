using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PJLinkProjectorEpi
{
    public class DeviceFactory : EssentialsPluginDeviceFactory<PJLinkProjector>
    {
        public DeviceFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.8.1";
            TypeNames = new List<string>() { "PJLinkProjector", "pjlink" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var props = PropsConfig.FromDeviceConfig(dc);
            var coms = CommFactory.CreateCommForDevice(dc);
            var device = new PJLinkProjector(dc.Key, dc.Name, props, coms);

            return device;
        }
    }
}