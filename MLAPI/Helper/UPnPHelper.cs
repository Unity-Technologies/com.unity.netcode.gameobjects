using Open.Nat;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MLAPI.Helper
{
    public class UPnPHelper
    {
        internal static void AttemptPortMap(int port, Action<bool, IPAddress> callback)
        {
            bool invoked = false;
            NatDiscoverer nat = new NatDiscoverer();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(10000);

            NatDevice device = null;
            StringBuilder sb = new StringBuilder();
            Task<NatDevice> natTask = nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            Mapping tcpMapping = new Mapping(Protocol.Tcp, port, port, 0, Application.productName + " (TCP)");
            Mapping udpMapping = new Mapping(Protocol.Udp, port, port, 0, Application.productName + " (UDP)");
            IPAddress publicIPAddress = null;
            natTask.ContinueWith(tt =>
            {
                device = tt.Result;
                device.GetExternalIPAsync()
                    .ContinueWith(task =>
                    {
                        publicIPAddress = task.Result;
                        return device.CreatePortMapAsync(udpMapping);
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.CreatePortMapAsync(udpMapping);
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.GetAllMappingsAsync();
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        Mapping[] mappings = task.Result.ToArray();
                        if(mappings.Length == 0)
                        {
                            if (!invoked)
                                callback(false, publicIPAddress);
                            invoked = true;
                        }
                        else
                        {
                            for (int i = 0; i < mappings.Length; i++)
                            {
                                if(mappings[i].PrivatePort == port)
                                {
                                    if (!invoked)
                                        callback(true, publicIPAddress);
                                    invoked = true;
                                }
                            }
                        }
                    });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            try
            {
                natTask.Wait();
            }
            catch (AggregateException e)
            {
                if (e.InnerException is NatDeviceNotFoundException)
                {
                    if (!invoked)
                        callback(false, publicIPAddress);
                    invoked = true;
                }
            }
        }
    }
}
