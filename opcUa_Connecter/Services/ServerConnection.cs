// ServerConnection.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;

namespace opcUa_Connecter.Services
{
    public class ServerConnection
    {
        private Session _sessionRef;
        private volatile bool _online;

        public Session Session => Volatile.Read(ref _sessionRef);
        public bool IsConnected => _online;
        

        public event Action<bool> ConnectionChanged;
        private readonly SemaphoreSlim _connectGate = new(1, 1);

        //Connect without CancellationToken.
        public async Task ConnectAsync(string endpointURL) =>
            await ConnectAsync(endpointURL, CancellationToken.None).ConfigureAwait(false);

        //Connect with CancellationToken
        public async Task ConnectAsync(string endpointURL, CancellationToken ct)
        {
            await _connectGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await ConnectInternalAsync(endpointURL, ct).ConfigureAwait(false);
            }
            finally
            {
                _connectGate.Release();
            }
        }

        //Disconnect current session and emit ConnectionChanged
        public async Task DisconnectAsync()
        {
            await _connectGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var old = Interlocked.Exchange(ref _sessionRef, null);
                if (old != null)
                {
                    try { await old.CloseAsync().ConfigureAwait(false); } catch { }
                    old.Dispose();
                }
                _online = false; // mark offline
                ConnectionChanged?.Invoke(false);
            }
            finally { _connectGate.Release(); }
        }

        /// <summary>
        /// Simple retry loop for reconnect. Returns true if connected before cancellation.
        /// </summary>
        public async Task<bool> TryReconnectLoopAsync(string endpointURL, TimeSpan delay, CancellationToken token)
        {
            while (!token.IsCancellationRequested && !IsConnected)
            {
                try
                {
                    await ConnectAsync(endpointURL, token).ConfigureAwait(false);
                }
                catch
                {
                    // ignore a single failure, we'll delay and try again
                }

                if (IsConnected)
                    return true;

                try { await Task.Delay(delay, token).ConfigureAwait(false); }
                catch (TaskCanceledException) { /* cancelled */ }
            }
            return IsConnected;
        }


        private async Task ConnectInternalAsync(string endpointURL, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            //  Build application configuration 
            var config = new ApplicationConfiguration
            {
                ApplicationName = "MyOpcUaClient",
                ApplicationUri = $"urn:{Utils.GetHostName()}:MyOpcUaClient",
                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "CN=MyOpcUaClient, O=MyOrg"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Applications"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = true,
                    SuppressNonceValidationErrors = true
                },

                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000,
                    MaxStringLength = 1_048_576,
                    MaxByteStringLength = 1_048_576,
                    MaxArrayLength = 65_536,
                    MaxMessageSize = 4_194_304,
                    MaxBufferSize = 65_536,
                    ChannelLifetime = 300_000,
                    SecurityTokenLifetime = 3_600_000
                },

                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60_000,
                    MinSubscriptionLifetime = 10_000
                },

                CertificateValidator = new CertificateValidator()
            };

            await config.Validate(ApplicationType.Client).ConfigureAwait(false);

            // Accept all server certs 
            config.CertificateValidator.Update(config);
            config.CertificateValidator.CertificateValidation += (s, e) =>
            {
                e.Accept = true;
                // Console.WriteLine($"Accepted certificate: {e.Certificate?.Subject}");
            };

            // Select endpoint 
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, useSecurity: false, 15000);
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            //  Create a NEW session first
            var newSession = await Session.Create(
                configuration: config,
                endpoint: endpoint,
                updateBeforeConnect: true,
                sessionName: "MySession",
                sessionTimeout: 60000,
                identity: new UserIdentity(),
                preferredLocales: null
            ).ConfigureAwait(false);

            // Wire events on the new session BEFORE swapping it in
            WireKeepAlive(newSession);

            // automaticaly close old session starta new one
            var old = Interlocked.Exchange(ref _sessionRef, newSession);
            if (old != null)
            {
                try { await old.CloseAsync().ConfigureAwait(false); } catch { /* ignore */ }
                old.Dispose();
            }
            _online = true;
            ConnectionChanged?.Invoke(true);
        }

        /// <summary>
        /// KeepAlive and Closing hooks to flip UI to offline immediately on drop.
        /// </summary>
        private void WireKeepAlive(Session session)
        {
            session.KeepAlive += async (s, e) =>
            {
                var code = e?.Status?.StatusCode ?? StatusCodes.Good;
                if (StatusCode.IsBad(code))
                {
                    _online = false;      //turn               
                    ConnectionChanged?.Invoke(false);

                    //close this broken session , SDK also flips to !Connected.
                    try { await session.CloseAsync().ConfigureAwait(false); } catch { }
                    try { session.Dispose(); } catch { }
                }
            };

            session.SessionClosing += (s, e2) =>
            {
                _online = false;              //turn        
                ConnectionChanged?.Invoke(false);
            };
        }
    }
}
