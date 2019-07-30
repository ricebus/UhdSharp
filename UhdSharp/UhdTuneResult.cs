using System;
using System.Collections.Generic;
using System.Text;

namespace UhdSharp
{
    //! Stores RF and DSP tuned frequencies.
    /*!
     * See uhd::tune_result_t for more details.
     */
    struct UhdTuneResult
    {
        //! Target RF frequency, clipped to be within system range
        public double clipped_rf_freq;
        //! Target RF frequency, including RF FE offset
        public double target_rf_freq;
        //! Frequency to which RF LO is actually tuned
        public double actual_rf_freq;
        //! Frequency the CORDIC must adjust the RF
        public double target_dsp_freq;
        //! Frequency to which the CORDIC in the DSP actually tuned
        public double actual_dsp_freq;
    }
}
