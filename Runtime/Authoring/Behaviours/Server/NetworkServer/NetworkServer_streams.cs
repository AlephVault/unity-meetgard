using AlephVault.Unity.Binary;
using AlephVault.Unity.Meetgard.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AlephVault.Unity.Meetgard
{
    namespace Authoring
    {
        namespace Behaviours
        {
            namespace Server
            {
                public partial class NetworkServer : MonoBehaviour
                {
                    // The server-side certificate as instantiated immediately
                    // after the server listening lifecycle loop.
                    private X509Certificate2 certificate = null;

                    // Prepares the certificate depending on the certificate
                    // path setting and the passphrase environment variable.
                    private void PrepareCertificate()
                    {
                        if (!secure)
                        {
                            return;
                        }
                        
                        privateKeyPassphraseEnvVar = privateKeyPassphraseEnvVar.Trim();
                        if (privateKeyPassphraseEnvVar == "")
                        {
                            certificate = new X509Certificate2(certificatePath);
                        }
                        else
                        {
                            string passphrase = privateKeyPassphraseEnvVar; // Environment.GetEnvironmentVariable(privateKeyPassphraseEnvVar);
                            if (passphrase == null)
                            {
                                throw new InvalidOperationException("The passphrase content is empty or " +
                                                                    "undefined at the given environment variable");
                            }

                            certificate = new X509Certificate2(certificatePath, passphrase);
                        }
                    }
                    
                    // Prepares a stream, using the current certificate if
                    // the connection is specified as secure.
                    private Stream PrepareStream(NetworkStream stream)
                    {
                        if (!secure)
                        {
                            return stream;
                        }

                        SslStream newStream = new SslStream(stream, true);
                        newStream.AuthenticateAsServer(certificate, false, sslProtocols, false);
                        return newStream;
                    }
                }
            }
        }
    }
}
