using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using UhdUsrpHandle = System.IntPtr;
using UhdRxStreamerHandle = System.IntPtr;
using UhdRxMetadataHandle = System.IntPtr;

#if _WIN64
    using size_t = System.Int64;
#else
    using size_t = System.Int32;
#endif

namespace UhdSharp
{
    public class UHD : IDisposable
    {
        public delegate void IQRecvPtr(short[] iq);

        UhdUsrpHandle _usrpHandle;
        UhdRxStreamerHandle _rxStreamerHandle;
        UhdRxMetadataHandle _rxMetadataHandle;

        public UHD()
        {

        }
        private size_t _channel = 0;

        #region Properties
        private double _sampleRate;
        public double SampleRate
        {
            get
            {
                return _sampleRate;
            }
            set
            {
                _sampleRate = value;
                uhd_usrp_set_rx_rate(_usrpHandle, _sampleRate, _channel);
                uhd_usrp_get_rx_rate(_usrpHandle, _channel, out _sampleRate);
            }
        }

        private double _gain;
        public double Gain
        {
            get
            {
                return _gain;
            }
            set
            {
                _gain = value;
                uhd_usrp_set_rx_gain(_usrpHandle, _gain, _channel, "");
                uhd_usrp_get_rx_gain(_usrpHandle, _channel, "", out _gain);
            }
        }

        private double _freq;
        public double Frequency
        {
            get
            {
                return _freq;
            }
            set
            {
                _freq = value;
                UhdTuneRequest tuneRequest = new UhdTuneRequest()
                {
                    target_freq = _freq,
                    rf_freq_policy = UhdTuneRequestPolicy.UHD_TUNE_REQUEST_POLICY_AUTO,
                    dsp_freq_policy = UhdTuneRequestPolicy.UHD_TUNE_REQUEST_POLICY_AUTO
                };

                UhdTuneResult tuneResult = new UhdTuneResult();
                UHD.uhd_usrp_set_rx_freq(_usrpHandle, ref tuneRequest, _channel, ref tuneResult);
                UHD.uhd_usrp_get_rx_freq(_usrpHandle, _channel, out _freq);
            }
        }
        #endregion

        #region Methods
        public short[] GetIQ(double sampleRate, double frequency, double captureLength, double gain)
        {
            if(SampleRate != sampleRate)
                SampleRate = sampleRate;
            if(Frequency != frequency)
                Frequency = frequency;
            if(Gain != gain)
                Gain = gain;

            size_t n_samples = (size_t) Math.Ceiling(SampleRate * captureLength);
            Console.WriteLine($"Requesting {n_samples} samples");

            UhdStreamArgs streamArgs = new UhdStreamArgs()
            {
                cpu_format = "sc16",
                otw_format = "sc16",
                args = "",
                channel_list = new size_t[] { _channel },
                n_channels = 0
            };

            UhdStreamCmd streamCmd = new UhdStreamCmd()
            {
                stream_mode = UhdStreamMode.UHD_STREAM_MODE_NUM_SAMPS_AND_DONE,
                num_samps = n_samples,
                stream_now = false,
                time_spec_frac_secs = 0,
                time_spec_full_secs = 1
            };

            size_t num_acc_samps = 0, samps_per_buff = n_samples;

            UhdError error = uhd_usrp_get_rx_stream(_usrpHandle, ref streamArgs, _rxStreamerHandle);
            //uhd_rx_streamer_max_num_samps(_rxStreamerHandle, out samps_per_buff);  
            short[] managed_buf = new short[samps_per_buff * 2];
            unsafe
            {                
                fixed (short* buf = managed_buf)
                {
                    uhd_rx_streamer_issue_stream_cmd(_rxStreamerHandle, streamCmd);
                    while (num_acc_samps < n_samples)
                    {
                        size_t num_rx_samps = 0;
                        uhd_rx_streamer_recv(_rxStreamerHandle, &buf, samps_per_buff, ref _rxMetadataHandle, 3, false, out num_rx_samps);

                        UhdRxMetadataErrorCode errorCode = new UhdRxMetadataErrorCode();
                        uhd_rx_metadata_error_code(_rxMetadataHandle, out errorCode);
                        if (errorCode != UhdRxMetadataErrorCode.UHD_RX_METADATA_ERROR_CODE_NONE)
                        {
                            Console.WriteLine("Error code was returned during streaming. Aborting.\n");
                            break;
                        }

                        num_acc_samps += num_rx_samps;
                        Console.WriteLine($"Received packet: {num_rx_samps} samples, Total: {num_acc_samps} samples");
                        //UHD.uhd_rx_metadata_time_spec(rxMetadataHandle, out long full_secs, out double frac_secs);
                    }
                }            
            }

            return managed_buf;
        }

        public short[] GetIQEvent(IQRecvPtr callback, double sampleRate, double frequency, double captureLength, double gain)
        {
            if (SampleRate != sampleRate)
                SampleRate = sampleRate;
            if (Frequency != frequency)
                Frequency = frequency;
            if (Gain != gain)
                Gain = gain;

            size_t n_samples = (size_t)Math.Ceiling(SampleRate * captureLength);
            Console.WriteLine($"Requesting {n_samples} samples");

            UhdStreamArgs streamArgs = new UhdStreamArgs()
            {
                cpu_format = "sc16",
                otw_format = "sc16",
                args = "",
                channel_list = new size_t[] { _channel },
                n_channels = 0
            };

            UhdStreamCmd streamCmd = new UhdStreamCmd()
            {
                stream_mode = UhdStreamMode.UHD_STREAM_MODE_START_CONTINUOUS,
                num_samps = n_samples,
                stream_now = false,
                time_spec_frac_secs = 0,
                time_spec_full_secs = 2
            };

            size_t num_acc_samps = 0, samps_per_buff = n_samples;

            UhdError error = uhd_usrp_get_rx_stream(_usrpHandle, ref streamArgs, _rxStreamerHandle);
            //uhd_rx_streamer_max_num_samps(_rxStreamerHandle, out samps_per_buff);
            uhd_rx_streamer_issue_stream_cmd(_rxStreamerHandle, streamCmd);
            while (true)
            {
                short[] managed_buf = new short[samps_per_buff * 2];
                unsafe
                {
                    fixed (short* buf = managed_buf)
                    {                        
                        while (num_acc_samps < n_samples)
                        {
                            size_t num_rx_samps = 0;
                            uhd_rx_streamer_recv(_rxStreamerHandle, &buf, samps_per_buff, ref _rxMetadataHandle, 3, false, out num_rx_samps);

                            UhdRxMetadataErrorCode errorCode = new UhdRxMetadataErrorCode();
                            uhd_rx_metadata_error_code(_rxMetadataHandle, out errorCode);
                            if (errorCode != UhdRxMetadataErrorCode.UHD_RX_METADATA_ERROR_CODE_NONE)
                            {
                                Console.WriteLine("Error code was returned during streaming. Aborting.\n");
                                break;
                            }

                            num_acc_samps += num_rx_samps;
                            Console.WriteLine($"Received packet: {num_rx_samps} samples, Total: {num_acc_samps} samples");
                            //UHD.uhd_rx_metadata_time_spec(rxMetadataHandle, out long full_secs, out double frac_secs);
                        }
                        callback(managed_buf);
                    }
                }
            }

            //return managed_buf;
        }

        public bool StopStream()
        {
            UhdStreamArgs streamArgs = new UhdStreamArgs()
            {
                cpu_format = "fc32",
                otw_format = "sc16",
                args = "",
                channel_list = new size_t[] { _channel },
                n_channels = 0
            };

            UhdStreamCmd streamCmd = new UhdStreamCmd()
            {
                stream_mode = UhdStreamMode.UHD_STREAM_MODE_STOP_CONTINUOUS,
                num_samps = 0,
                stream_now = false,
                time_spec_frac_secs = 0,
                time_spec_full_secs = 1
            };


            UhdError error = uhd_usrp_get_rx_stream(_usrpHandle, ref streamArgs, _rxStreamerHandle);

            return error == UhdError.UHD_ERROR_NONE;
        }

        //public unsafe void FullExample()
        //{
        //    double freq = 100e6;
        //    size_t channel = 0;
        //    size_t n_samples = 1000000;
        //    int option = 0;
        //    double rate = 2e6;
        //    double gain = 5.0;
        //    string device_args = "";
        //    bool verbose = true;

        //    Console.WriteLine($"Creating USRP with args \"{device_args}\"...\n");
        //    UhdUsrpHandle usrpHandle = new UhdUsrpHandle();
        //    UHD.uhd_usrp_make(ref usrpHandle, device_args);

        //    UhdRxStreamerHandle rxStreamerHandle = new UhdRxStreamerHandle();
        //    UHD.uhd_rx_streamer_make(ref rxStreamerHandle);

        //    UhdRxMetadataHandle rxMetadataHandle = new UhdRxMetadataHandle();
        //    UHD.uhd_rx_metadata_make(ref rxMetadataHandle);

        //    // Create other necessary structs
        //    UhdTuneRequest tuneRequest = new UhdTuneRequest()
        //    {
        //        target_freq = freq,
        //        rf_freq_policy = UhdTuneRequestPolicy.UHD_TUNE_REQUEST_POLICY_AUTO,
        //        dsp_freq_policy = UhdTuneRequestPolicy.UHD_TUNE_REQUEST_POLICY_AUTO
        //    };

        //    UhdTuneResult tuneResult = new UhdTuneResult();

        //    UhdStreamArgs streamArgs = new UhdStreamArgs()
        //    {
        //        cpu_format = "fc32",
        //        otw_format = "sc16",
        //        args = "",
        //        channel_list = new size_t[] { channel },
        //        n_channels = 0
        //    };

        //    UhdStreamCmd streamCmd = new UhdStreamCmd()
        //    {
        //        stream_mode = UhdStreamMode.UHD_STREAM_MODE_NUM_SAMPS_AND_DONE,
        //        num_samps = n_samples,
        //        stream_now = false,
        //        time_spec_frac_secs = 0,
        //        time_spec_full_secs = 1
        //    };

        //    Console.WriteLine($"Setting RX Rate: {rate}...");
        //    uhd_usrp_set_rx_rate(usrpHandle, rate, channel);
        //    uhd_usrp_get_rx_rate(usrpHandle, channel, out rate);
        //    Console.WriteLine($"Actual RX Rate: {rate}...");

        //    Console.WriteLine($"Setting RX Gain: {gain} dB...");
        //    uhd_usrp_set_rx_gain(usrpHandle, gain, channel, "");
        //    uhd_usrp_get_rx_gain(usrpHandle, channel, "", out gain);
        //    Console.WriteLine($"Actual RX Gain: {gain} dB...");

        //    Console.WriteLine($"Setting RX frequency: {freq / 1e6} MHz...");
        //    uhd_usrp_set_rx_freq(usrpHandle, ref tuneRequest, channel, ref tuneResult);
        //    uhd_usrp_get_rx_freq(usrpHandle, channel, out freq);
        //    Console.WriteLine($"Actual RX frequency: {freq / 1e6} MHz...");

        //    //streamArgs.channel_list = new size_t[] { channel };
        //    size_t samps_per_buff = new size_t();

        //    UhdError error = UHD.uhd_usrp_get_rx_stream(usrpHandle, ref streamArgs, rxStreamerHandle);
        //    uhd_rx_streamer_max_num_samps(rxStreamerHandle, out samps_per_buff);
        //    Console.WriteLine($"Buffer size in samples: {samps_per_buff}");

        //    Console.WriteLine("Issuing stream command.");
        //    uhd_rx_streamer_issue_stream_cmd(rxStreamerHandle, streamCmd);

        //    size_t num_acc_samps = 0;

        //    float[] managed_buf = new float[samps_per_buff * 2];
        //    while (num_acc_samps < n_samples)
        //    {
        //        size_t num_rx_samps = 0;
        //        fixed (float* buf = managed_buf)
        //        {
        //            uhd_rx_streamer_recv(rxStreamerHandle, &buf, samps_per_buff, ref rxMetadataHandle, 3, false, out num_rx_samps);
        //        }
        //        UhdRxMetadataErrorCode errorCode = new UhdRxMetadataErrorCode();
        //        uhd_rx_metadata_error_code(rxMetadataHandle, out errorCode);
        //        if (errorCode != UhdRxMetadataErrorCode.UHD_RX_METADATA_ERROR_CODE_NONE)
        //            Console.WriteLine("Error code was returned during streaming. Aborting.\n");

        //        // Handle data
        //        //fwrite(buff, sizeof(float) * 2, num_rx_samps, fp);
        //        Console.WriteLine($"Received packet: {num_rx_samps} samples");
        //        //if (verbose)
        //        //{
        //        //    //UHD.uhd_rx_metadata_time_spec(rxMetadataHandle, out long full_secs, out double frac_secs);
        //        //    Console.WriteLine($"Received packet: {num_rx_samps} samples");
        //        //}

        //        num_acc_samps += num_rx_samps;
        //    }
        //}

        private bool _disposed = true;

        public void Init()
        {
            string device_args = "";
            if (_disposed)
            {
                _usrpHandle = new UhdUsrpHandle();
                uhd_usrp_make(ref _usrpHandle, device_args);

                _rxStreamerHandle = new UhdRxStreamerHandle();
                uhd_rx_streamer_make(ref _rxStreamerHandle);

                _rxMetadataHandle = new UhdRxMetadataHandle();
                uhd_rx_metadata_make(ref _rxMetadataHandle);

                _disposed = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                uhd_rx_streamer_free(ref _rxStreamerHandle);
                uhd_rx_metadata_free(ref _rxMetadataHandle);
                uhd_usrp_free(ref _usrpHandle);
                _disposed = true;
            }
        }
        #endregion

        #region UHD C API
        //! Create a USRP handle.
        /*!
         * \param h the handle
         * \param args device args (e.g. "type=x300")
         */
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_make(ref UhdUsrpHandle h, string args);

        //! Create an RX streamer handle.
        /*!
         * NOTE: Using this streamer before passing it into uhd_usrp_get_rx_stream()
         * will result in undefined behavior.
         */
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_streamer_make(ref UhdRxStreamerHandle h);

        //! Create a new RX metadata handle
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_metadata_make(ref UhdRxMetadataHandle handle);

        //! Set the given RX channel's sample rate (in Sps)
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_set_rx_rate(UhdUsrpHandle h, double rate, size_t chan);

        //! Get the given RX channel's sample rate (in Sps)
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_get_rx_rate(UhdUsrpHandle h, size_t chan, out double rate_out);

        //! Set the RX gain for the given channel and name
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_set_rx_gain(UhdUsrpHandle h, double gain, size_t chan, string gain_name);

        //! Get the given channel's RX gain
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_get_rx_gain(UhdUsrpHandle h, size_t chan, string gain_name, out double gain_out);

        //! Set the given channel's center RX frequency
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_set_rx_freq(UhdUsrpHandle h, ref UhdTuneRequest tune_request, size_t chan, ref UhdTuneResult tune_result);

        //! Get the given channel's center RX frequency
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_get_rx_freq(UhdUsrpHandle h, size_t chan, out double freq_out);

        //! Create RX streamer from a USRP handle and given stream args
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_get_rx_stream(UhdUsrpHandle h, ref UhdStreamArgs stream_args, UhdRxStreamerHandle h_out);

        //! Get the max number of samples per buffer per packet
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_streamer_max_num_samps(UhdRxStreamerHandle h, out size_t max_num_samps_out);

        //! Get the number of channels associated with this streamer
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_streamer_num_channels(UhdRxStreamerHandle h, out size_t num_channels_out);


        //! Issue the given stream command
        /*!
         * See uhd::rx_streamer::issue_stream_cmd() for more details.
         */
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_streamer_issue_stream_cmd(UhdRxStreamerHandle h, UhdStreamCmd stream_cmd);

        //! Receive buffers containing samples into the given RX streamer
        /*!
         * See uhd::rx_streamer::recv() for more details.
         *
         * \param h RX streamer handle
         * \param buffs pointer to buffers in which to receive samples
         * \param samps_per_buff max number of samples per buffer
         * \param md handle to RX metadata in which to receive results
         * \param timeout timeout in seconds to wait for a packet
         * \param one_packet send a single packet
         * \param items_recvd pointer to output variable for number of samples received
         */
        [DllImport("uhd.dll")]
        unsafe static extern UhdError uhd_rx_streamer_recv(UhdRxStreamerHandle h, short** buffs, // void**
            size_t samps_per_buff, ref UhdRxMetadataHandle md, double timeout, bool one_packet, out size_t items_recvd);

        //! Get the last error state of the RX metadata object.
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_metadata_error_code(UhdRxMetadataHandle h, out UhdRxMetadataErrorCode error_code_out);

        //! Time of first sample
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_metadata_time_spec(UhdRxMetadataHandle h, out Int64 full_secs_out, out double frac_secs_out);

        //! Free an RX streamer handle.
        /*!
         * NOTE: Using a streamer after passing it into this function will result
         * in a segmentation fault.
         */
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_streamer_free(ref UhdRxStreamerHandle h);

        //! Free an RX metadata handle
        /*!
         * Using a handle after freeing it here will result in a segmentation fault.
         */
        [DllImport("uhd.dll")]
        static extern UhdError uhd_rx_metadata_free(ref UhdRxMetadataHandle handle);

        //! Get the last error reported by the USRP handle
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_last_error(UhdUsrpHandle h, out string error_out, size_t strbuffer_len);

        //! Safely destroy the USRP object underlying the handle.
        /*!
         * NOTE: Attempting to use a USRP handle after passing it into this function
         * will result in a segmentation fault.
         */
        [DllImport("uhd.dll")]
        static extern UhdError uhd_usrp_free(ref UhdUsrpHandle h);
        #endregion
    }
}
