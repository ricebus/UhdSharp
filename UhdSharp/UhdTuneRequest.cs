using System;
using System.Collections.Generic;
using System.Text;

namespace UhdSharp
{
    //! Instructs implementation how to tune the RF chain
    /*!
     * See uhd::tune_request_t for more details.
     */
    struct UhdTuneRequest
    {
        //! Target frequency for RF chain in Hz
        public double target_freq;
        //! RF frequency policy
        public UhdTuneRequestPolicy rf_freq_policy;
        //! RF frequency in Hz
        public double rf_freq;
        //! DSP frequency policy
        public UhdTuneRequestPolicy dsp_freq_policy;
        //! DSP frequency in Hz
        public double dsp_freq;
        //! Key-value pairs delimited by commas
        public string args;
    }
}
