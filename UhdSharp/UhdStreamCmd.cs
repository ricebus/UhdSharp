using System;
using System.Collections.Generic;
using System.Text;

#if _WIN64
    using size_t = System.Int64;
#else
    using size_t = System.Int32;
#endif

namespace UhdSharp
{
    //! Define how device streams to host
    /*!
     * See uhd::stream_cmd_t for more details.
     */
    struct UhdStreamCmd
    {
        //! How streaming is issued to the device
        public UhdStreamMode stream_mode;
        //! Number of samples
        public size_t num_samps;
        //! Stream now?
        public bool stream_now;
        //! If not now, then full seconds into future to stream
        public long time_spec_full_secs;
        //! If not now, then fractional seconds into future to stream
        public double time_spec_frac_secs;
    }
}
