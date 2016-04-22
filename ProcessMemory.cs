using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace ReplayDll
{
    public class ProcessMemory
    {
        [System.Flags]
        public enum MemoryState : uint
        {
            MEM_COMMIT = 4096u,
            MEM_FREE = 65536u,
            MEM_RESERVE = 8192u
        }
        [System.Flags]
        public enum MemoryType : uint
        {
            MEM_IMAGE = 16777216u,
            MEM_MAPPED = 262144u,
            MEM_PRIVATE = 131072u
        }
        [System.Flags]
        public enum AllocationProtect : uint
        {
            PAGE_EXECUTE = 16u,
            PAGE_EXECUTE_READ = 32u,
            PAGE_EXECUTE_READWRITE = 64u,
            PAGE_EXECUTE_WRITECOPY = 128u,
            PAGE_NOACCESS = 1u,
            PAGE_READONLY = 2u,
            PAGE_READWRITE = 4u,
            PAGE_WRITECOPY = 8u,
            PAGE_GUARD = 256u,
            PAGE_NOCACHE = 512u,
            PAGE_WRITECOMBINE = 1024u
        }
        public struct MEMORY_BASIC_INFORMATION
        {
            public uint BaseAddress;
            public uint AllocationBase;
            public ProcessMemory.AllocationProtect AllocationProtect;
            public uint RegionSize;
            public ProcessMemory.MemoryState State;
            public uint Protect;
            public ProcessMemory.MemoryType Type;
        }
        private uint processHandle;
        private System.Collections.Generic.List<ProcessMemory.MEMORY_BASIC_INFORMATION> memorySnapshot;
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern uint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool CloseHandle(uint hObject);
        [System.Runtime.InteropServices.DllImport("kernel32.dll ")]
        private static extern bool ReadProcessMemory(uint hProcess, uint lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(uint hProcess, uint lpAddress, ref ProcessMemory.MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
        public bool openProcess(string pName)
        {
            Process[] processesByName = Process.GetProcessesByName(pName);
            if (processesByName.Length == 0)
            {
                return false;
            }
            Process process = processesByName[0];
            this.processHandle = ProcessMemory.OpenProcess(2035711u, false, process.Id);
            return true;
        }
        public bool closeProcess()
        {
            bool result = ProcessMemory.CloseHandle(this.processHandle);
            this.processHandle = 0u;
            return result;
        }
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName([System.Runtime.InteropServices.In] System.IntPtr hProcess, [System.Runtime.InteropServices.In] int dwFlags, [System.Runtime.InteropServices.Out] System.Text.StringBuilder lpExeName, ref int lpdwSize);
        public string GetProcessPath()
        {
            int num = 1024;
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(num);
            ProcessMemory.QueryFullProcessImageName((System.IntPtr)((long)((ulong)this.processHandle)), 0, stringBuilder, ref num);
            try
            {
                stringBuilder.ToString(0, num);
            }
            catch
            {
                return null;
            }
            return stringBuilder.ToString(0, num);
        }
        public byte[] readMemory(uint address, uint size)
        {
            byte[] array = new byte[size];
            int num;
            ProcessMemory.ReadProcessMemory(this.processHandle, address, array, size, out num);
            return array;
        }
        public void recordMemorysInfo()
        {
            this.memorySnapshot = new System.Collections.Generic.List<ProcessMemory.MEMORY_BASIC_INFORMATION>();
            uint num = 1u;
            while (num != 0u)
            {
                ProcessMemory.MEMORY_BASIC_INFORMATION item = default(ProcessMemory.MEMORY_BASIC_INFORMATION);
                ProcessMemory.VirtualQueryEx(this.processHandle, num, ref item, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(ProcessMemory.MEMORY_BASIC_INFORMATION)));
                uint num2 = num;
                num = item.BaseAddress + item.RegionSize;
                if (num < num2)
                {
                    return;
                }
                if (item.AllocationProtect == ProcessMemory.AllocationProtect.PAGE_READWRITE && item.Type == ProcessMemory.MemoryType.MEM_PRIVATE && item.State == ProcessMemory.MemoryState.MEM_COMMIT)
                {
                    this.memorySnapshot.Add(item);
                }
            }
        }
        public uint[] findInteger(int n)
        {
            System.Collections.Generic.List<uint> list = new System.Collections.Generic.List<uint>();
            foreach (ProcessMemory.MEMORY_BASIC_INFORMATION current in this.memorySnapshot)
            {
                byte[] array = this.readMemory(current.BaseAddress, current.RegionSize);
                if (array != null)
                {
                    int num = array.Length - 4;
                    for (int i = 0; i < num; i++)
                    {
                        int num2 = System.BitConverter.ToInt32(array, i);
                        if (num2 == n)
                        {
                            list.Add(current.BaseAddress + (uint)i);
                        }
                    }
                }
            }
            return list.ToArray();
        }
        public uint[] findString(string str, System.Text.Encoding encoding, int max = 0)
        {
            byte[] bytes = encoding.GetBytes(str);
            System.Collections.Generic.List<uint> list = new System.Collections.Generic.List<uint>();
            foreach (ProcessMemory.MEMORY_BASIC_INFORMATION current in this.memorySnapshot)
            {
                byte[] array = this.readMemory(current.BaseAddress, current.RegionSize);
                if (array != null)
                {
                    int num = array.Length - bytes.Length;
                    for (int i = 0; i < num; i++)
                    {
                        int num2 = 0;
                        while (num2 < bytes.Length && bytes[num2] == array[i + num2])
                        {
                            if (num2 == bytes.Length - 1)
                            {
                                list.Add(current.BaseAddress + (uint)i);
                                if (max != 0 && max >= list.Count)
                                {
                                    return list.ToArray();
                                }
                            }
                            num2++;
                        }
                    }
                }
            }
            return list.ToArray();
        }
    }
}
