using System;
using System.Collections.Generic;
using System.Text;

namespace UhdSharp
{
    //! Policy options for tunable elements in the RF chain.
    enum UhdTuneRequestPolicy
    {
        //! Do not set this argument, use current setting.
        UHD_TUNE_REQUEST_POLICY_NONE = 78,
        //! Automatically determine the argument's value.
        UHD_TUNE_REQUEST_POLICY_AUTO = 65,
        //! Use the argument's value for the setting.
        UHD_TUNE_REQUEST_POLICY_MANUAL = 77
    }
}
