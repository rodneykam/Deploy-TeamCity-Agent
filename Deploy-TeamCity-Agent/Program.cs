using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;

namespace Deploy_TeamCity_Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            const string subscriptionId = "a646d321-18de-4209-a895-5c7dec3a9ca0";
            const string clientId = "b953e870-be7d-44b5-a4ed-17a3ba091aa3";
            const string clientSecret = "R3l@yH3@lth";
            const string tenantId = "b37600f0-1e5e-48fb-b34d-d9bdb51cdbc5";

            #region Create Credentials
            Console.WriteLine("Creating Credentials...");
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION"));
            //var credentials = SdkContext.AzureCredentialsFactory
            //    .FromServicePrincipal(clientId,
            //    clientSecret,
            //    tenantId,
            //    AzureEnvironment.AzureGlobalCloud);
            #endregion

            #region Create Management Client
            Console.WriteLine("Creating Management Client...");
            var azure = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);
            #endregion

            #region Create Resources
            const string groupName = "deploy-teamcity-agent-rg";
            const string vmName = "hsagent-vm";
            var location = Region.USWest;

            Console.WriteLine("Creating Resources...");
            var resourceGroup = azure.ResourceGroups.Define(groupName)
                .WithRegion(location)
    .           Create();

            Console.WriteLine("Creating public IP address...");
            var publicIPAddress = azure.PublicIPAddresses.Define("deploy-teamcity-agent-publicip")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithStaticIP()
                .Create();

            Console.WriteLine("Creating virtual network...");
            var network = azure.Networks.Define("deploy-teamcity-agent-vnet")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet("deploy-teamcity-agent-subnet", "10.0.0.0/24")
                .Create();

            Console.WriteLine("Creating Load Balancer...");
            var loadBalancer = azure.LoadBalancers.Define("deploy-teamcity-agent-lb")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .DefineInboundNatRule("deploy-teamcity-agent-NATRule1")
                    .WithProtocol(TransportProtocol.Tcp)
                    .FromFrontend("deploy-teamcity-agent-frontend")
                    .FromFrontendPort(443)
                    .ToBackendPort(3389)
                    .WithIdleTimeoutInMinutes(5)
                    .Attach()
                .DefinePublicFrontend("deploy-teamcity-agent-frontend")
                    .WithExistingPublicIPAddress(publicIPAddress)
                    .Attach()
                .Create();

            Console.WriteLine("Creating network interface...");
            var networkInterface = azure.NetworkInterfaces.Define("deploy-teamcity-agent-NIC")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet("deploy-teamcity-agent-subnet")
                .WithPrimaryPrivateIPAddressDynamic()
                .Create();

            var t1 = new DateTime();
            Console.WriteLine("Creating virtual machine...");
            azure.VirtualMachines.Define(vmName)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetworkInterface(networkInterface)
                .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                .WithAdminUsername("azureuser")
                .WithAdminPassword("Azure12345678")
                .WithComputerName(vmName)
                .WithSize(VirtualMachineSizeTypes.StandardDS1)
                .Create();

            var t2 = new DateTime();

            Console.WriteLine("Elapse Time for Creating VM: " + (t2 - t1).TotalMinutes + " minutes.");

            #endregion

            var vm = azure.VirtualMachines.GetByResourceGroup(groupName, vmName);

            Console.WriteLine("Getting information about the virtual machine...");
            Console.WriteLine("hardwareProfile");
            Console.WriteLine("   vmSize: " + vm.Size);
            Console.WriteLine("storageProfile");
            Console.WriteLine("  imageReference");
            Console.WriteLine("    publisher: " + vm.StorageProfile.ImageReference.Publisher);
            Console.WriteLine("    offer: " + vm.StorageProfile.ImageReference.Offer);
            Console.WriteLine("    sku: " + vm.StorageProfile.ImageReference.Sku);
            Console.WriteLine("    version: " + vm.StorageProfile.ImageReference.Version);
            Console.WriteLine("  osDisk");
            Console.WriteLine("    osType: " + vm.StorageProfile.OsDisk.OsType);
            Console.WriteLine("    name: " + vm.StorageProfile.OsDisk.Name);
            Console.WriteLine("    createOption: " + vm.StorageProfile.OsDisk.CreateOption);
            Console.WriteLine("    caching: " + vm.StorageProfile.OsDisk.Caching);
            Console.WriteLine("osProfile");
            Console.WriteLine("  computerName: " + vm.OSProfile.ComputerName);
            Console.WriteLine("  adminUsername: " + vm.OSProfile.AdminUsername);
            Console.WriteLine("  provisionVMAgent: " + vm.OSProfile.WindowsConfiguration.ProvisionVMAgent.Value);
            Console.WriteLine("  enableAutomaticUpdates: " + vm.OSProfile.WindowsConfiguration.EnableAutomaticUpdates.Value);
            Console.WriteLine("networkProfile");
            foreach (string nicId in vm.NetworkInterfaceIds)
            {
                Console.WriteLine("  networkInterface id: " + nicId);
            }
            Console.WriteLine("vmAgent");
            Console.WriteLine("  vmAgentVersion" + vm.InstanceView.VmAgent.VmAgentVersion);
            Console.WriteLine("    statuses");
            foreach (InstanceViewStatus stat in vm.InstanceView.VmAgent.Statuses)
            {
                Console.WriteLine("    code: " + stat.Code);
                Console.WriteLine("    level: " + stat.Level);
                Console.WriteLine("    displayStatus: " + stat.DisplayStatus);
                Console.WriteLine("    message: " + stat.Message);
                Console.WriteLine("    time: " + stat.Time);
            }
            Console.WriteLine("disks");
            foreach (DiskInstanceView disk in vm.InstanceView.Disks)
            {
                Console.WriteLine("  name: " + disk.Name);
                Console.WriteLine("  statuses");
                foreach (InstanceViewStatus stat in disk.Statuses)
                {
                    Console.WriteLine("    code: " + stat.Code);
                    Console.WriteLine("    level: " + stat.Level);
                    Console.WriteLine("    displayStatus: " + stat.DisplayStatus);
                    Console.WriteLine("    time: " + stat.Time);
                }
            }
            Console.WriteLine("VM general status");
            Console.WriteLine("  provisioningStatus: " + vm.ProvisioningState);
            Console.WriteLine("  id: " + vm.Id);
            Console.WriteLine("  name: " + vm.Name);
            Console.WriteLine("  type: " + vm.Type);
            Console.WriteLine("  location: " + vm.Region);
            Console.WriteLine("VM instance status");
            foreach (InstanceViewStatus stat in vm.InstanceView.Statuses)
            {
                Console.WriteLine("  code: " + stat.Code);
                Console.WriteLine("  level: " + stat.Level);
                Console.WriteLine("  displayStatus: " + stat.DisplayStatus);
            }
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }
    }
}
