using System;
using System.Collections.Generic;
using System.Text;

namespace UhdSharp
{
    //! Error condition on a receive call
    /*!
     * See uhd::rx_metadata_t::error_code_t for more details.
     */
    enum UhdRxMetadataErrorCode
    {
        //! No error code associated with this metadata
        UHD_RX_METADATA_ERROR_CODE_NONE = 0x0,
        //! No packet received, implementation timed out
        UHD_RX_METADATA_ERROR_CODE_TIMEOUT = 0x1,
        //! A stream command was issued in the past
        UHD_RX_METADATA_ERROR_CODE_LATE_COMMAND = 0x2,
        //! Expected another stream command
        UHD_RX_METADATA_ERROR_CODE_BROKEN_CHAIN = 0x4,
        //! Overflow or sequence error
        UHD_RX_METADATA_ERROR_CODE_OVERFLOW = 0x8,
        //! Multi-channel alignment failed
        UHD_RX_METADATA_ERROR_CODE_ALIGNMENT = 0xC,
        //! The packet could not be parsed
        UHD_RX_METADATA_ERROR_CODE_BAD_PACKET = 0xF
    }
}
