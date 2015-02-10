using System;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Serializers;

namespace CustomerIOSharp
{
    public class CustomerIo
    {
        private readonly string _siteId;
        private readonly string _apiKey;
        private readonly ICustomerFactory _customerFactory;

        private const string Endpoint = "https://track.customer.io/api/v1/";

        private const string MethodCustomer = "customers/{customer_id}";
        private const string MethodTrack = "customers/{customer_id}/events";

        private readonly RestClient _client;

        public CustomerIo(string siteId, string apiKey)
            : this(siteId, apiKey, null)
        {
        }

        public CustomerIo(string siteId, string apiKey, ICustomerFactory customerFactory)
        {
            _siteId = siteId;
            _apiKey = apiKey;
            _customerFactory = customerFactory;

            _client = new RestClient(Endpoint)
                {
                    Authenticator = new HttpBasicAuthenticator(_siteId, _apiKey)
                };
        }

        private void CheckCustomerFactory()
        {
            if (_customerFactory == null)
            {
                throw new ArgumentNullException("customerFactory", "set customerFactory param in the ctor");
            }
        }

        private async Task CallMethodAsync(string method, string customerId, Method httpMethod, object data)
        {
            // do not transmit events if we do not have a customer id
            if (customerId == null) return;

            var request = new RestRequest(method)
            {
                Method = httpMethod,
                RequestFormat = DataFormat.Json,
                JsonSerializer = new JsonSerializer()
            };
            request.AddUrlSegment(@"customer_id", customerId);
            request.AddBody(data);

            var response = await _client.ExecuteAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new CustomerIoApiException(response.StatusCode);
            }
        }

        /// <summary>
        /// Update the customer. Can only be used if customerFactory was defined
        /// </summary>
        /// <returns></returns>
        public async Task IdentifyAsync()
        {
            CheckCustomerFactory();
            var id = _customerFactory.GetCustomerId();
            var customerDetails = _customerFactory.GetCustomerDetails();
            await IdentifyAsync(id, customerDetails);
        }

        /// <summary>
        /// Update the customer
        /// </summary>
        /// <returns></returns>
        public async Task IdentifyAsync(string customerId, object customerDetails)
        {
            await CallMethodAsync(MethodCustomer, customerId, Method.PUT, customerDetails);
        }

        /// <summary>
        /// Delete the customer. Can only be used if customerFactory was defined
        /// </summary>
        /// <returns></returns>
        public async Task DeleteCustomerAsync()
        {
            CheckCustomerFactory();
            var id = _customerFactory.GetCustomerId();
            await DeleteCustomerAsync(id);
        }

        /// <summary>
        /// Delete the customer.
        /// </summary>
        /// <returns></returns>
        public async Task DeleteCustomerAsync(string customerId)
        {
            await CallMethodAsync(MethodCustomer, customerId, Method.DELETE, null);
        }

        /// <summary>
        /// Track a custom event. Can only be used if customerFactory was defined
        /// </summary>
        /// <param name="eventName">The name of the event you want to track</param>
        /// <param name="data">Any related information you’d like to attach to this event. These attributes can be used in your triggers to control who should receive the triggered email. You can set any number of data key and values.</param>
        /// <param name="timestamp">Allows you to back-date the event, pass null to use current time</param>
        /// <returns>Nothing if successful, throws if failed</returns>
        /// <exception cref="CustomerIoApiException">If any code besides 200 OK is returned from the server.</exception>
        public async Task TrackEventAsync(string eventName, object data = null, DateTime? timestamp = null)
        {
            CheckCustomerFactory();
            var id = _customerFactory.GetCustomerId();
            await TrackEventAsync(id, eventName, data, timestamp);
        }

        /// <summary>
        /// Track a custom event.
        /// </summary>
        /// <param name="customerId">The id of the customer you want to track</param>
        /// <param name="eventName">The name of the event you want to track</param>
        /// <param name="data">Any related information you’d like to attach to this event. These attributes can be used in your triggers to control who should receive the triggered email. You can set any number of data key and values.</param>
        /// <param name="timestamp">Allows you to back-date the event, pass null to use current time</param>
        /// <returns>Nothing if successful, throws if failed</returns>
        /// <exception cref="CustomerIoApiException">If any code besides 200 OK is returned from the server.</exception>
        public async Task TrackEventAsync(string customerId, string eventName, object data = null, DateTime? timestamp = null)
        {
            var wrappedData = new TrackedEvent
                {
                    Name = eventName,
                    Data = data,
                    Timestamp = timestamp
                };

            await CallMethodAsync(MethodTrack, customerId, Method.POST, wrappedData);
        }
    }

    [SerializeAs(NameStyle = NameStyle.CamelCase)]
    class TrackedEvent
    {
        public string Name { get; set; }
        public object Data { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
