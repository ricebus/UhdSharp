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
    //! A struct of parameters to construct a stream.
    /*!
     * See uhd::stream_args_t for more details.
     */
    struct UhdStreamArgs
    {
        //! Format of host memory
        public string cpu_format;
        //! Over-the-wire format
        public string otw_format;
        //! Other stream args
        public string args;
        //! Array that lists channels
        public size_t[] channel_list;
        //! Number of channels
        public int n_channels;
    }
}
