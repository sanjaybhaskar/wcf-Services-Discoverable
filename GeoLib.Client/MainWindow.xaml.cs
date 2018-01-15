using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Windows;
using GeoLib.Contracts;
using GeoLib.Proxies;

namespace GeoLib.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            CheckForServices();

            AnnouncementService announcementService = new AnnouncementService();
            
            announcementService.OnlineAnnouncementReceived += (s, e) =>
            {
                if (e.EndpointDiscoveryMetadata.ContractTypeNames.FirstOrDefault(item => item.Name == "IGeoService") != null)
                {
                    _DiscoveredAddress = e.EndpointDiscoveryMetadata.Address;

                    btnGetInfo.IsEnabled = true;
                    btnGetZipCodes.IsEnabled = true;
                }
            };

            announcementService.OfflineAnnouncementReceived += (s, e) =>
            {
                if (e.EndpointDiscoveryMetadata.ContractTypeNames.FirstOrDefault(item => item.Name == "IGeoService") != null)
                {
                    _DiscoveredAddress = null;

                    btnGetInfo.IsEnabled = false;
                    btnGetZipCodes.IsEnabled = false;
                }
            };

            _AnnouncementService = new ServiceHost(announcementService);
            _AnnouncementService.Open();
        }

        ServiceHost _AnnouncementService = null;

        private void btnGetInfo_Click(object sender, RoutedEventArgs e)
        {
            if (txtZipCode.Text != "")
            {
                //GeoClient proxy = new GeoClient();
                //IGeoService proxy = CreateProxy();
                GeoClient proxy = new GeoClient("dynamicGeoService");

                ZipCodeData data = proxy.GetZipInfo(txtZipCode.Text);
                if (data != null)
                {
                    lblCity.Content = data.City;
                    lblState.Content = data.State;
                }

                ((IDisposable)proxy).Dispose();
                //proxy.Close();
            }
        }

        private void btnGetZipCodes_Click(object sender, RoutedEventArgs e)
        {
            if (txtState.Text != null)
            {
                //GeoClient proxy = new GeoClient();
                //IGeoService proxy = CreateProxy();
                GeoClient proxy = new GeoClient("dynamicGeoService");

                IEnumerable<ZipCodeData> data = proxy.GetZips(txtState.Text);
                if (data != null)
                    lstZips.ItemsSource = data;

                ((IDisposable)proxy).Dispose();
                //proxy.Close();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            CheckForServices();
        }

        EndpointAddress _DiscoveredAddress = null;

        void CheckForServices()
        {
            btnRefresh.IsEnabled = false;

            DiscoveryClient discoveryProxy = new DiscoveryClient(new UdpDiscoveryEndpoint());
            FindCriteria findCriteria = new FindCriteria(typeof(IGeoService))
            {
                MaxResults = 1,
                Duration = new TimeSpan(0, 0, 5)
            };

            findCriteria.Scopes.Add(new Uri("http://www.pluralsight.com/miguel-castro/discoverability"));

            FindResponse discoveryResponse = discoveryProxy.Find(findCriteria);

            if (discoveryResponse.Endpoints.Count == 0)
            {
                _DiscoveredAddress = null;

                btnGetInfo.IsEnabled = false;
                btnGetZipCodes.IsEnabled = false;
            }
            else
            {
                EndpointDiscoveryMetadata discoveredEndpoint = discoveryResponse.Endpoints[0];
                _DiscoveredAddress = discoveredEndpoint.Address;

                btnGetInfo.IsEnabled = true;
                btnGetZipCodes.IsEnabled = true;
            }

            btnRefresh.IsEnabled = true;
        }

        IGeoService CreateProxy()
        {
            NetTcpBinding binding = new NetTcpBinding();

            ChannelFactory<IGeoService> factory = new ChannelFactory<IGeoService>(binding, _DiscoveredAddress);
            IGeoService proxy = factory.CreateChannel();

            return proxy;
        }
    }
}
