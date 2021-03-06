﻿/*
Silver Spring Networks, Inc. ("Silver Spring") licenses this software to you and the company (“You” or “Your”) 
a license for this software (“Software”).  
Using the Software constitutes Your acceptance of these terms (“License”).  

License Grant. Subject to Your acceptance and compliance with this License, Silver Spring grants to You, solely for Your own internal business purpose, 
               a non-exclusive, non-transferable license to access and use the Software and the associated user documentation (“Documentation”) 
               for the term and number agreed to be Silver Spring. 

Restrictions. No intellectual property rights are granted under this License and Silver Spring reserves all rights not expressly granted. 
You may not:  
(a) modify or create derivative works of the Software or Documentation; 
(b) assign, transfer, lease or sublicense the Software or Documentation to any third party 
    (other than Your consultants who are bound to written obligations of confidentiality at least as restrictive as those contained in this License); 
and (c) reverse engineer, disassemble, decrypt, extract or otherwise reduce the Software to a human perceivable 
    form or otherwise attempt to determine the source code or algorithms of the Software 
    (unless the foregoing restriction is expressly prohibited by applicable law).
You may not remove or destroy any proprietary, trademark or copyright markings or notices placed on or contained in the Software or Documentation.  
Silver Spring PROVIDES THE SOFTWARE “AS IS” AND MAKES NO WARRANTIES, WHETHER EXPRESS, IMPLIED OR STATUTORY REGARDING OR RELATING TO THE SOFTWARE.  
Silver Spring HEREBY DISCLAIMS ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE, 
WHETHER RIGHTS ARISES BY LAW, BY USE AND CUSTOM OR USAGE IN THE TRADE, OR BY COURSE OF DEALING. 
Silver Spring DOES NOT WARRANT THAT THE SOFTWARE OR ANY PORTION THEREOF WILL BE ERROR-FREE.

Termination. This License is effective until terminated. Your rights under this License will terminate automatically without notice if you fail to comply with any of its terms. 
Upon the termination of this License, You must cease all use of the Software and remove it from Your computer.
*/
using System;
using System.Collections;
using EXILANT.Labs.CoAP.Channels;
using EXILANT.Labs.CoAP.Message;
using EXILANT.Labs.CoAP.Helpers;

namespace HdkClient
{
    /// <summary>
    /// This is the Simulator implementation of CoApGet.
    /// </summary>
    public class CoApGetSimulator : CoApGet
    {
        /// <summary>
        /// Send the CoAP request to the server
        /// </summary>
        public override void Send()
        {

            string device = __IpAddress;
            string serverIP = "localhost";
            __coapClient.Initialize(serverIP, __ServerPort);

            __coapClient.Initialize(serverIP, __ServerPort);
            __coapClient.CoAPResponseReceived += new CoAPResponseReceivedHandler(OnCoAPResponseReceived);

            __coapClient.CoAPError += new CoAPErrorHandler(OnCoAPError);

            coapReq = new CoAPRequest(this.ConfirmableMessageType,//CoAPMessageType.NON,
                                                CoAPMessageCode.GET,
                                                HdkUtils.MessageId());
            string uriToCall = "coap://" + serverIP + ":" + __ServerPort + "/" + __IpAddress + __URI;

            coapReq.SetURL(uriToCall);
            SetToken();
            // Send out the request.
            coapReq.Options.AddOption(CoAPHeaderOption.PROXY_SCHEME, "coap");
            FileLogger.Write("About to send CoAP request");
            FileLogger.Write(coapReq.ToString());
            __coapClient.Send(coapReq);

            __Done.WaitOne(GatewaySettings.Instance.RequestTimeout);
            __Done.Reset();
            CoApGatewaySessionManager.Instance.Client.CoAPResponseReceived -= new CoAPResponseReceivedHandler(OnCoAPResponseReceived);
            CoApGatewaySessionManager.Instance.Client.CoAPError -= new CoAPErrorHandler(OnCoAPError);
        }

    /// <summary>
    /// Called when error occurs
    /// </summary>
    /// <param name="e">The exception that occurred</param>
    /// <param name="associatedMsg">The associated message (if any)</param>    
    private void OnCoAPError(Exception e, AbstractCoAPMessage associatedMsg)
        {
            Console.WriteLine(e.Message);
            //Write your error logic here
            __Done.Set();
        }

        /// <summary>
        /// Called when a response is received against a sent request
        /// </summary>
        /// <param name="coapResp">The CoAPResponse object</param>
        private void OnCoAPResponseReceived(CoAPResponse coapResp)
        {
            string tokenRx = (coapResp.Token != null && coapResp.Token.Value != null) ? AbstractByteUtils.ByteToStringUTF8(coapResp.Token.Value) : "";
            if (tokenRx == __Token)
            {
                if (coapResp.Code.Value != CoAPMessageCode.VALID)
                {
                    FileLogger.Write("Error on GET:");
                    __Response = coapResp;
                    __GetResult = coapResp.ToString();
                    FileLogger.Write(coapResp.ToString());
                    __Done.Set();
                    return;
                }

                if (coapResp.Code.Value == CoAPMessageCode.CONTENT)
                {

                    ArrayList options = coapResp.Options.GetOptions((ushort)CoAPHeaderOption.CONTENT_FORMAT);
                    if (options.Count > 0)
                    {
                        CoAPContentFormatOption ccformat = new CoAPContentFormatOption();
                        bool proceed = false;
                        foreach (CoAPHeaderOption o in options)
                        {
                            ccformat = new CoAPContentFormatOption(AbstractByteUtils.ToUInt16(o.Value));
                            if (ccformat.IsValid())
                            {
                                proceed = true;
                                break;
                            }
                        }
                        if (proceed)
                        {
                            if (ccformat.Value == CoAPContentFormatOption.TEXT_PLAIN)
                            {
                                string result = AbstractByteUtils.ByteToStringUTF8(coapResp.Payload.Value);
                                Console.WriteLine("Get on Olimex " + __coapClient.EndPoint.ToString() + " = " + result);
                                __GetResult = result;
                            }
                            if (ccformat.Value == CoAPContentFormatOption.APPLICATION_OCTET_STREAM)
                            {
                                string result = HdkUtils.BytesToHexView(coapResp.Payload.Value);
                                Console.WriteLine("Get on Olimex " + __coapClient.EndPoint.ToString() + " = " + result);
                                __GetResult = result;
                            }
                            if (ccformat.Value == CoAPContentFormatOption.APPLICATION_JSON)
                            {
                                string result = HdkUtils.BytesToHexView(coapResp.Payload.Value);
                                Console.WriteLine("Get on Olimex " + __coapClient.EndPoint.ToString() + " = " + result);
                                __GetResult = result;
                            }
                            FileLogger.Write("Received valid response from server:");
                            FileLogger.Write(__GetResult);
                            __Response = coapResp;
                        }
                    }
                }
                else
                {
                    //Will come here if an error occurred..
                }
            }
            __Done.Set();

        }
    }
}
