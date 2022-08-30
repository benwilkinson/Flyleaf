﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Vortice.DXGI;

using FlyleafLib.MediaFramework.MediaRenderer;
using System.Linq;

namespace FlyleafLib
{
    public class VideoEngine
    {
        /// <summary>
        /// List of Video Capture Devices
        /// </summary>
        public ObservableCollection<string> CapDevices { get; private set; } = new ObservableCollection<string>();

        /// <summary>
        /// List of GPU Adpaters <see cref="Config.VideoConfig.GPUAdapter"/>
        /// </summary>
        public Dictionary<long, GPUAdapter>  GPUAdapters { get; internal set; }

        internal IDXGIFactory2 Factory;

        internal VideoEngine()
        {
            if (DXGI.CreateDXGIFactory1(out Factory).Failure)
                throw new InvalidOperationException("Cannot create IDXGIFactory1");

            GPUAdapters = GetAdapters();
        }

        private Dictionary<long, GPUAdapter> GetAdapters()
        {
            Dictionary<long, GPUAdapter> adapters = new Dictionary<long, GPUAdapter>();
            
            string dump = "";

            var adapters2 = Factory.EnumAdapters1().ToList();
            for (int i=0; i<adapters2.Count; i++)
            {
                IDXGIAdapter1 adapter = adapters2[i];
                dump += $"[#{i+1}] {RendererInfo.VendorIdStr(adapter.Description1.VendorId)} {adapter.Description1.Description} (Id: {adapter.Description1.DeviceId} | Luid: {adapter.Description1.Luid}) | DVM: {RendererInfo.GetBytesReadable(adapter.Description1.DedicatedVideoMemory)}\r\n";

                if ((adapter.Description1.Flags & AdapterFlags.Software) != AdapterFlags.None)
                {
                    adapter.Dispose();
                    continue;
                }

                int idx = 0;
                bool hasOutput = false;

                List<GPUOutput> outputs = new List<GPUOutput>();

                var outputs2 = adapter.EnumOutputs().ToList();
                for (int l=0; l<outputs2.Count; l++)
                {
                    IDXGIOutput output = outputs2[l];
                    GPUOutput gpout = new GPUOutput();

                    gpout.DeviceName= output.Description.DeviceName;
                    gpout.Left      = output.Description.DesktopCoordinates.Left;
                    gpout.Top       = output.Description.DesktopCoordinates.Top;
                    gpout.Right     = output.Description.DesktopCoordinates.Right;
                    gpout.Bottom    = output.Description.DesktopCoordinates.Bottom;
                    gpout.IsAttached= output.Description.AttachedToDesktop;
                    gpout.Rotation  = (int)output.Description.Rotation;

                    outputs.Add(gpout);

                    if (gpout.IsAttached)
                        hasOutput = true;

                    output.Dispose();

                    idx++;
                }

                adapters[adapter.Description1.Luid] = new GPUAdapter() { Description = adapter.Description1.Description, Luid = adapter.Description1.Luid, HasOutput = hasOutput, Outputs = outputs };

                adapter.Dispose();
            }

            Engine.Log.Info($"GPU Adapters\r\n{dump}");

            return adapters;
        }
    }
}
