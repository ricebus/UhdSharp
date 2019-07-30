using System;
using System.Collections.Generic;
using System.Text;

namespace UhdSharp
{
    //! How streaming is issued to the device
    /*!
     * See uhd::stream_cmd_t for more details.
     */
    public enum UhdStreamMode
    {
        //! Stream samples indefinitely
        UHD_STREAM_MODE_START_CONTINUOUS = 97,
        //! End continuous streaming
        UHD_STREAM_MODE_STOP_CONTINUOUS = 111,
        //! Stream some number of samples and finish
        UHD_STREAM_MODE_NUM_SAMPS_AND_DONE = 100,
        //! Stream some number of samples but expect more
        UHD_STREAM_MODE_NUM_SAMPS_AND_MORE = 109
    }
}
