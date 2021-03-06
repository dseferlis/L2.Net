﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using L2.Net.Network;
using L2.Net.CacheService.Properties;

namespace L2.Net.CacheService.Network
{
    /// <summary>
    /// Provides class, that handles incoming connections.
    /// </summary>
    internal static class NetworkListener
    {
        private const int m_BackLog = 10;
        private static Listener m_ListenerService;
        private static Thread m_ListenerThread;
        private static volatile bool m_Active;

        /// <summary>
        /// Initializes listener service.
        /// </summary>
        /// <param name="localEndPoint">Local endpoint.</param>
        /// <param name="enableFirewall">True, to force service use firewall, otherwise false.</param>
        internal static void Initialize( IPEndPoint localEndPoint, bool enableFirewall )
        {
            Disable();

            InitializeListener(localEndPoint);
            StartListenerThread(enableFirewall);
        }

        /// <summary>
        /// Disables network listener and stops listener thread.
        /// </summary>
        internal static void Disable()
        {
            if ( m_Active )
            {
                StopListener();
                StopListenerThread();
            }
        }

        /// <summary>
        /// Initializes listener.
        /// </summary>
        /// <param name="localEndPoint">Local endpoint.</param>
        private static void InitializeListener( IPEndPoint localEndPoint )
        {
            m_ListenerService = new Listener(localEndPoint, m_BackLog);
            m_ListenerService.OnStarted += new OnListenerStartedEventHandler(ListenerService_OnStarted);
            m_ListenerService.OnStopped += new OnListenerStoppedEventHandler(ListenerService_OnStopped);
            m_ListenerService.OnTerminated += new OnListenerTerminatedEventHandler(ListenerService_OnTerminated);
            m_ListenerService.OnConnectionAccepted += new OnConnectionAcceptedEventHandler(ListenerService_OnConnectionAccepted);
        }

        /// <summary>
        /// Tries to start listener thread.
        /// </summary>
        /// <param name="enableFirewall">True, if listener must use firewall, otherwise false.</param>
        private static void StartListenerThread( bool enableFirewall )
        {
            try
            {
                m_ListenerThread = new Thread(new ParameterizedThreadStart(m_ListenerService.Start));
                m_ListenerThread.Name = "ListenerThread";
                m_ListenerThread.Start(enableFirewall);
                m_Active = true;
            }
            catch ( Exception e )
            {
                Logger.Exception(e, "Failed to start listener thread");
            }
        }

        /// <summary>
        /// Executes after listener accepted new client.
        /// </summary>
        /// <param name="socket">New client socket.</param>
        private static void ListenerService_OnConnectionAccepted( Socket socket )
        {
            ConnectionsManager.AcceptConnection(socket);
        }

        /// <summary>
        /// Executes after listener was terminated.
        /// </summary>
        private static void ListenerService_OnTerminated()
        {
            Service.NetworkListenerIsActive = true;
            Logger.WriteLine(Source.Listener, "Network listener terminated.");
        }

        /// <summary>
        /// Executes after listener was stopped.
        /// </summary>
        private static void ListenerService_OnStopped()
        {
            Service.NetworkListenerIsActive = true;
            Logger.WriteLine(Source.Listener, "Network listener stopped.");
        }

        /// <summary>
        /// Executes after listener was started.
        /// </summary>
        private static void ListenerService_OnStarted()
        {
            Service.NetworkListenerIsActive = true;

            // audit record
            DataProvider.DataBase.ServiceAudit
                (
                    Settings.Default.ServiceUniqueID,
                    ( byte )ServiceType.CacheService,
                    ServiceAuditType.NetworkListenerStarted,
                    LocalEndPoint.ToString()
                );

            Logger.WriteLine(Source.Listener, "Network listener started on {0}", m_ListenerService.LocalEndPoint);
        }

        /// <summary>
        /// Stops listener.
        /// </summary>
        private static void StopListener()
        {
            if ( m_ListenerService != null && m_ListenerService.Active )
                m_ListenerService.Stop();

            m_Active = false;
        }

        /// <summary>
        /// Stops listener thread.
        /// </summary>
        private static void StopListenerThread()
        {
            if ( m_ListenerThread != null && m_ListenerThread.IsAlive )
                m_ListenerThread.Abort();
        }

        /// <summary>
        /// Gets local <see cref="IPEndPoint"/> current that <see cref="NetworkListener"/> listens on.
        /// </summary>
        internal static IPEndPoint LocalEndPoint
        {
            get
            {
                return m_ListenerService.LocalEndPoint;
            }
        }
    }
}