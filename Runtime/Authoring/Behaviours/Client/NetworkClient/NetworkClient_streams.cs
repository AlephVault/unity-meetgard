using AlephVault.Unity.Support.Utils;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Client
            {
                public partial class NetworkClient : MonoBehaviour
                {
                    // Performs the client-side validation of the server certificate.
                    private bool ValidateServerCertificate(
                        object sender, X509Certificate certificate, X509Chain chain,
                        SslPolicyErrors sslPolicyErrors
                    )
                    {
                        // Please don't set trustCertificate=true in production!
                        if (trustServerCertificate)
                        {
                            Debug.LogWarning("You are using trustServerCertificate==true. Remember to " +
                                             "disable it when making a Production build!");
                            return true;
                        }
                        if (sslPolicyErrors == SslPolicyErrors.None) return true;
                        Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
                        // Do not allow this client to communicate with unauthenticated servers.
                        return false;
                    }
                    
                    // Creates the appropriate stream to be used in the
                    // connection lifecycle against a server.
                    private Stream PrepareStream(NetworkStream stream)
                    {
                        if (!secure)
                        {
                            return stream;
                        }

                        SslStream newStream = new SslStream(
                            stream, true,
                            new RemoteCertificateValidationCallback(ValidateServerCertificate), 
                            null
                        );
                        newStream.AuthenticateAsClient(
                            targetHostName, null, sslProtocols,
                            checkCertificateRevocation
                        );
                        return newStream;
                    }
                }
            }
        }
    }
}
