using System;
using Foundation;
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace FFImageLoading
{
    public class PlatformPerformance : IPlatformPerformance
    {
        public PlatformPerformance()
        {
            var handle = Dlfcn.dlopen("/usr/lib/libSystem.dylib", 0);
            self = Dlfcn.GetIntPtr(handle, "mach_task_self_");
            Dlfcn.dlclose(handle);
        }

        public int GetCurrentManagedThreadId()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public int GetCurrentSystemThreadId()
        {
            var threadId = (NSNumber)(NSThread.Current.ValueForKeyPath(new NSString("private.seqNum")));

            return threadId.Int32Value;
        }

        struct mach_task_basic_info
        {
            public int /* mach_vm_size_t */ virtual_size;       /* virtual memory size (bytes) */
            public int /* mach_vm_size_t */ resident_size;      /* resident memory size (bytes) */
            public int /* mach_vm_size_t */ resident_size_max;  /* maximum resident memory size (bytes) */
            public long /* time_value_t */ user_time;          /* total user run time for terminated threads */
            public long /* time_value_t */ system_time;        /* total system run time for terminated threads */
            public int /* policy_t */ policy;             /* default policy for new threads */
            public int /* integer_t */ suspend_count;      /* suspend count for task */
        };

        [DllImport("/usr/lib/libSystem.dylib")]
        extern static /* kern_return_t */ int task_info(
                        /* task_name_t -> mach_port_t */ IntPtr target_task,
                        /* task_flavor_t -> natural_t */ int flavor,
                        /* task_info_t -> integer_t* */ ref mach_task_basic_info task_info_out,
                        /* mach_msg_type_number_t* -> natural_t* */ ref int task_info_outCnt);

        const int KERN_SUCCESS = 0;
        const int MACH_TASK_BASIC_INFO = 20;

        IntPtr self;
        mach_task_basic_info tbi = new mach_task_basic_info();
        int size = Marshal.SizeOf(typeof(mach_task_basic_info));

        mach_task_basic_info? GetResidentSize()
        {
            var err = task_info(self, MACH_TASK_BASIC_INFO, ref tbi, ref size);
            return (err == KERN_SUCCESS) ? tbi : default(mach_task_basic_info?);
        }

        public string GetMemoryInfo()
        {
            var memoryInfo = GetResidentSize();

            double virtualSize = 0d;
            double residentSize = 0d;
            double residentSizeMax = 0d;

            if (memoryInfo.HasValue)
            {
                virtualSize = (double)memoryInfo.Value.virtual_size / 1048576d;
                residentSize = (double)memoryInfo.Value.resident_size / 1048576d;
                residentSizeMax = (double)memoryInfo.Value.resident_size_max / 1048576d;
            }

            return string.Format("[PERFORMANCE] Memory - resident_size: {0:0}MB, resident_size_max: {1:0}MB, virtual_size: {2:0}MB",
                                 residentSize, residentSizeMax, virtualSize);
        }
    }
}

