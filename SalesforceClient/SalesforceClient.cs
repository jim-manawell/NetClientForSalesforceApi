using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using TmsStudentImport.Models.Salesforce;
using System.Linq;

namespace MyOrg.Client.Salesforce
{
    public class SalesforceClient
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _username;
        private readonly string _password;
        private readonly string _token;
        private readonly string _host;
        private readonly string _loginEndpoint;
        private readonly string _apiEndpoint;
        private readonly string _bulkEndpoint;
        private readonly string _logFilename;
        private string _logFilePath;
        private bool _enableLogging;

        private int _batchStatusWaitInterval; // milliseconds to wait until the next batch status request is sent
        private string _sessionId;
        private readonly HttpClient _httpClient;
        private const int _maxCharsForBulkApiQuery = 20000;
        private int _maxBatchStatusRequestAttempts;
        private int _maxRetryAttempts;
        private int _httpClientRequestTimeoutMinutes;
        private SfRecordType _recordtypes;
        public SalesforceClient(string clientId, string clientSecret, string username, string password, string token, string host, string loginEndpoint, string apiEndpoint, string bulkEndpoint, string logFilePath, bool enableLogging)
        {
            if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException("Parameter cannot be null or empty", "clientId");
            if (string.IsNullOrEmpty(clientSecret)) throw new ArgumentNullException("Parameter cannot be null or empty", "clientSecret");
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("Parameter cannot be null or empty", "username");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("Parameter cannot be null or empty", "password");
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException("Parameter cannot be null or empty", "token");
            if (string.IsNullOrEmpty(host)) throw new ArgumentNullException("Parameter cannot be null or empty", "host");
            if (string.IsNullOrEmpty(loginEndpoint)) throw new ArgumentNullException("Parameter cannot be null or empty", "loginEndpoint");
            if (string.IsNullOrEmpty(apiEndpoint)) throw new ArgumentNullException("Parameter cannot be null or empty", "apiEndpoint");
            if (string.IsNullOrEmpty(bulkEndpoint)) throw new ArgumentNullException("Parameter cannot be null or empty", "bulkEndpoint");
            if (enableLogging && string.IsNullOrEmpty(logFilePath)) throw new ArgumentNullException("Parameter cannot be null or empty", "logFilePath");

            _clientId = clientId;
            _clientSecret = clientSecret;
            _username = username;
            _password = password;
            _token = token;
            _host = host;
            _loginEndpoint = loginEndpoint;
            _apiEndpoint = apiEndpoint;
            _bulkEndpoint = bulkEndpoint;
            _batchStatusWaitInterval = 7000;
            _maxBatchStatusRequestAttempts = 1000;
            _maxRetryAttempts = 3;
            _httpClientRequestTimeoutMinutes = 2;
            // Logging
            _enableLogging = enableLogging;
            _logFilePath = logFilePath;
            _logFilename = _logFilePath + "BulkApiResponses_" + DateTime.Now.ToString("yyyy-MM-dd_H-mm-ss") + ".txt";
            if (enableLogging)
            {
                Directory.CreateDirectory(_logFilePath); // Create logging directory for debugging
            }

            //Http Client
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(_httpClientRequestTimeoutMinutes);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public SalesforceClient(string clientId, string clientSecret, string username, string password, string token, string host, string loginEndpoint, string apiEndpoint, string bulkEndpoint)
        {
            if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException("Parameter cannot be null or empty", "clientId");
            if (string.IsNullOrEmpty(clientSecret)) throw new ArgumentNullException("Parameter cannot be null or empty", "clientSecret");
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("Parameter cannot be null or empty", "username");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("Parameter cannot be null or empty", "password");
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException("Parameter cannot be null or empty", "token");
            if (string.IsNullOrEmpty(host)) throw new ArgumentNullException("Parameter cannot be null or empty", "host");
            if (string.IsNullOrEmpty(loginEndpoint)) throw new ArgumentNullException("Parameter cannot be null or empty", "loginEndpoint");
            if (string.IsNullOrEmpty(apiEndpoint)) throw new ArgumentNullException("Parameter cannot be null or empty", "apiEndpoint");
            if (string.IsNullOrEmpty(bulkEndpoint)) throw new ArgumentNullException("Parameter cannot be null or empty", "bulkEndpoint");

            _clientId = clientId;
            _clientSecret = clientSecret;
            _username = username;
            _password = password;
            _token = token;
            _host = host;
            _loginEndpoint = loginEndpoint;
            _apiEndpoint = apiEndpoint;
            _bulkEndpoint = bulkEndpoint;
            _batchStatusWaitInterval = 7000;
            _maxBatchStatusRequestAttempts = 1000;
            _maxRetryAttempts = 3;
            // Logging
            _enableLogging = false;
            _logFilePath = null;
            _logFilename = null;

            //Http Client
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30); // Prevents time out (TaskCanceledException)
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private SfRecordType GetRecordTypeIds()
        {
            string jsonRecordTypes = Query("select ID,Name from RecordType where SObjectType='Account' limit 100");
            SfRecordType sfRecordTypes = new SfRecordType(jsonRecordTypes);
            return sfRecordTypes;

        }

        /// <summary>
        /// Session Id/token assigned when logging
        /// </summary>
        public string SessionId
        {
            get
            {
                return _sessionId;
            }
        }



        /// <summary>
        /// Maximum characters allowed in a Bulk Api query
        /// </summary>
        public int MaxCharsForBulkApiQuery
        {
            get
            {
                return _maxCharsForBulkApiQuery;
            }
        }

        /// <summary>
        /// Milliseconds to wait until the next batch status request to the Salesforce Bulk Api
        /// </summary>
        public int BatchStatusWaitInterval
        {
            get
            {
                return _batchStatusWaitInterval;
            }
            set
            {
                _batchStatusWaitInterval = value;
            }
        }

        /// <summary>
        /// Enable logging if queries and results to files. LogFilePath must not be empty/null if true.
        /// </summary>
        public bool EnableLogging
        {
            get
            {
                return _enableLogging;
            }
            set
            {
                if (value == true && string.IsNullOrEmpty(_logFilePath)) throw new ArgumentNullException("Parameter cannot be null or empty", "LogFilePath");
                _enableLogging = value;
            }
        }

        /// <summary>
        /// The directory where log files will be written to.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                return _logFilePath;
            }
            set
            {
                _logFilePath = value;
            }
        }

        /// <summary>
        /// Maximum number of allowed attempts to request the batch status before exception/error thrown.
        /// </summary>
        public int MaxBatchStatusRequestAttempts
        {
            get
            {
                return _maxBatchStatusRequestAttempts;
            }
            set
            {
                _maxBatchStatusRequestAttempts = value;
            }
        }

        /// <summary>
        /// Max attempts to retry an request. Sometimes salesforce connections just fail and need to be retried (ie. TaskCanceledException)
        /// </summary>
        public int MaxRetryAttempts
        {
            get
            {
                return _maxRetryAttempts;
            }
            set
            {
                _maxRetryAttempts = value;
            }
        }

        /// <summary>
        /// The maximum number of minutes until a http request timeout. Typically a TaskCanceledException when a timeout error occurs.
        /// </summary>
        public int HttpClientRequestTimeoutMinutes
        {
            get
            {
                return _httpClientRequestTimeoutMinutes;
            }
            set
            {
                _httpClientRequestTimeoutMinutes = value;
                _httpClient.Timeout = TimeSpan.FromMinutes(_httpClientRequestTimeoutMinutes);
            }
        }


        public SfRecordType RecordTypes
        {
            get { return _recordtypes; }

        }



        /// <summary>
        /// Authenticates client app. Sets session ID properties which is used for further rest API requests.
        /// </summary>
        public void Login()
        {
            var content = new Dictionary<string, string> {
              { "grant_type", "password" },
              { "client_id", _clientId },
              { "client_secret", _clientSecret },
              { "username", _username },
              { "password", _password + _token }
            };



            var request = new FormUrlEncodedContent(content);
            request.Headers.Add("X-PrettyPrint", "1");
            var response = _httpClient.PostAsync(_host + _loginEndpoint, request).Result;
            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonResponse);
            _sessionId = values["access_token"];
            _recordtypes = GetRecordTypeIds();

        }

        /// <summary>
        /// Create a job to handle bulk api requests. Returns the job id.
        /// </summary>
        public string BulkApiCreateJob(string sfObjectName)
        {
            if (string.IsNullOrEmpty(sfObjectName)) throw new ArgumentNullException("Parameter cannot be null or empty", "sfObjectName");

            // Build request content
            var content = new Dictionary<string, string> {
        { "operation", "query" },
        { "object", sfObjectName },
        { "concurrencyMode", "Parallel" },
        { "contentType", "JSON" }
      };
            var httpContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            // Build POST request
            var request = new HttpRequestMessage(HttpMethod.Post, _host + _bulkEndpoint);
            request.Headers.Add("X-SFDC-Session", _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-PrettyPrint", "1");
            request.Content = httpContent;

            // Process response
            var response = _httpClient.SendAsync(request).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            BulkApiLogResponse("CreateJob", responseContent.PrettifyJson());

            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            return values["id"];
        }

        /// <summary>
        /// Create batch to handle bulk api requests, and link it the provided job. Returns the batch Id.
        /// </summary>
        public string BulkApiCreateBatch(string jobId, string soqlQuery)
        {
            if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException("Parameter cannot be null or empty", "jobId");
            if (string.IsNullOrEmpty(soqlQuery)) throw new ArgumentNullException("Parameter cannot be null or empty", "soqlQuery");

            // Build POST request
            var request = new HttpRequestMessage(HttpMethod.Post, _host + _bulkEndpoint + jobId + "/batch");
            request.Headers.Add("X-SFDC-Session", _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(soqlQuery, Encoding.UTF8, "application/json");

            // Process response
            var response = _httpClient.SendAsync(request).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            BulkApiLogResponse("CreateBatch", responseContent.PrettifyJson());
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            return values["id"];
        }

        /// <summary>
        /// Get the result id of the batch job
        /// </summary>
        public string BulkApiGetResultIds(string jobId, string batchId)
        {
            if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException("Parameter cannot be null or empty", "jobId");
            if (string.IsNullOrEmpty(batchId)) throw new ArgumentNullException("Parameter cannot be null or empty", "batchId");

            // Build GET request
            var request = new HttpRequestMessage(HttpMethod.Get, _host + _bulkEndpoint + jobId + "/batch/" + batchId + "/result");
            request.Headers.Add("X-SFDC-Session", _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Process response
            var response = _httpClient.SendAsync(request).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            BulkApiLogResponse("GetResultsIds", responseContent);

            return responseContent.ExtractResultId(); // response is not JSON: (["resultidhere"], so it needs it's own parser
        }

        /// <summary>
        /// Friendly bulk api query which handles jobs/batches/results and executes the provided soql query.
        /// </summary>
        /// <param name="sfObjectName">Primary salesforce object being queried, required by the bulk api</param>
        /// <param name="soqlQuery"></param>
        /// <returns></returns>
        public string BulkApiQuery(string sfObjectName, string soqlQuery)
        {
            if (string.IsNullOrEmpty(sfObjectName)) throw new ArgumentNullException("Parameter cannot be null or empty", "sfObjectName");
            if (string.IsNullOrEmpty(soqlQuery)) throw new ArgumentNullException("Parameter cannot be null or empty", "soqlQuery");

            string jobId = BulkApiCreateJob(sfObjectName);
            string batchId = BulkApiCreateBatch(jobId, soqlQuery);

            string state = null;
            int statusRequests = 0;
            while ((state == null || state == "Queued" || state == "InProgress") && statusRequests < _maxBatchStatusRequestAttempts)
            {
                state = BulkApiGetBatchStatus(jobId, batchId);
                System.Threading.Thread.Sleep(_batchStatusWaitInterval); // Wait to check the next batch status (in milliseconds)
                statusRequests++;
            }

            if (statusRequests == _maxBatchStatusRequestAttempts)
            {
                throw new ExternalException("Max batch status request attempts reached. Current max attempts: " + _maxBatchStatusRequestAttempts.ToString());
            }

            string resultId = BulkApiGetResultIds(jobId, batchId);
            string results = BulkApiGetResults(jobId, batchId, resultId);
            BulkApiCloseJob(jobId);
            return results;
        }

        public string InsertObject<T>(string sfObjectName, T sfObject)
        {
            // Build post request
            var url = _host + _apiEndpoint + "sobjects/" + sfObjectName;
            var responseContent = SendHttpPostRequest<T>(url, sfObject);

            // Process response
            try
            {
                var value = JsonConvert.DeserializeObject<ResponseInsert>(responseContent);
                return value.Id;
            }
            catch
            {
                throw new SalesforceInsertException(sfObjectName, (JsonConvert.SerializeObject(sfObject)).PrettifyJson(), responseContent);
            }
        }

        public void UpdateObject<T>(string sfObjectName, T sfObject, string sfId)
        {
            // Send post request
            var url = _host + _apiEndpoint + "sobjects/" + sfObjectName + "/" + sfId + "?_HttpMethod=PATCH";
            var responseContent = SendHttpPostRequest<T>(url, sfObject);

            // Process response
            //var value = JsonConvert.DeserializeObject<ResponseInsert>(responseContent);
        }

        public void DeleteObject<T>(string sfObjectName, T sfObject, string sfId)
        {
            // Send post request
            var url = _host + _apiEndpoint + "sobjects/" + sfObjectName + "/" + sfId + "?_HttpMethod=DELETE";
            var responseContent = SendHttpPostRequest<T>(url, sfObject);

            // Process response
            //var value = JsonConvert.DeserializeObject<ResponseInsert>(responseContent);
        }


        public void DeleteMultipleRecords(string records)
        {
            // Send post request
            var url = _host + _apiEndpoint + $"composite/sobjects?ids={records}";
            var response = SendHttpDeleteRequest(url);




            // Process response
            //var value = JsonConvert.DeserializeObject<ResponseInsert>(responseContent);
        }


        /// <summary>
        /// Get the bulk api query results
        /// </summary>
        /// <returns></returns>
        public string BulkApiGetResults(string jobId, string batchId, string resultId)
        {
            if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException("Parameter cannot be null or empty", "jobId");
            if (string.IsNullOrEmpty(batchId)) throw new ArgumentNullException("Parameter cannot be null or empty", "batchId");
            if (string.IsNullOrEmpty(resultId)) throw new ArgumentNullException("Parameter cannot be null or empty", "resultId");

            // Build GET request
            var request = new HttpRequestMessage(HttpMethod.Get, _host + _bulkEndpoint + jobId + "/batch/" + batchId + "/result/" + resultId);
            request.Headers.Add("X-SFDC-Session", _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Process response
            var response = _httpClient.SendAsync(request).Result;
            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Get status of batch job
        /// </summary>
        public string BulkApiGetBatchStatus(string jobId, string batchId)
        {
            if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException("Parameter cannot be null or empty", "jobId");
            if (string.IsNullOrEmpty(batchId)) throw new ArgumentNullException("Parameter cannot be null or empty", "batchId");

            // Build GET request
            var request = new HttpRequestMessage(HttpMethod.Get, _host + _bulkEndpoint + jobId + "/batch/" + batchId);
            request.Headers.Add("X-SFDC-Session", _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Process Response
            var response = _httpClient.SendAsync(request).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            BulkApiLogResponse("GetBatchStatus", responseContent.PrettifyJson());
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

            var state = values["state"];
            if (state == "Failed" || state == "Not Processed")
            {
                var stateMessage = values["stateMessage"];
                throw new ExternalException("Batch status/state returned: " + state + ". Message: " + stateMessage);
            }
            else return state;
        }

        /// <summary>
        /// Close job
        /// </summary>
        public string BulkApiCloseJob(string jobId)
        {
            if (string.IsNullOrEmpty(jobId)) throw new ArgumentNullException("Parameter cannot be null or empty", "jobId");

            // Build request content
            //var content = new Dictionary<string, string>();
            //content.Add("state", "Closed");

            // Build request content
            var content = new Dictionary<string, string> {
        { "state", "Closed" }
      };
            var httpContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            // Build POST request
            var request = new HttpRequestMessage(HttpMethod.Post, _host + _bulkEndpoint + jobId);
            request.Headers.Add("X-SFDC-Session", _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-PrettyPrint", "1");
            request.Content = httpContent;

            // Process response
            var response = _httpClient.SendAsync(request).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;

            // Process response
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
            return values["state"];
        }

        private void BulkApiLogResponse(string header, string response)
        {
            if (_enableLogging)
            {
                using (var sw = new StreamWriter(_logFilename, true))
                {
                    sw.WriteLine("########## " + header + " ##########");
                    sw.WriteLine(response);
                    sw.WriteLine();
                    sw.WriteLine();
                };
            }
        }

        /// <summary>
        /// Standard query which is part of the uri (limited to 2083 characters)
        /// </summary>
        public string Query(string soqlQuery)
        {
            if (string.IsNullOrEmpty(soqlQuery)) throw new ArgumentNullException("Parameter cannot be null or empty", "soqlQuery");

            string url = _host + _apiEndpoint + "query/?q=" + WebUtility.UrlEncode(soqlQuery); // Encode url to handle special characters (%, &...)
            return SendHttpGetRequest(url);
        }

        /// <summary>
        /// Gets the next records when standard query results do not fit in the response message 
        /// </summary>
        public string GetNextRecords(string nextRecordsUrl)
        {
            if (string.IsNullOrEmpty(nextRecordsUrl)) throw new ArgumentNullException("Parameter cannot be null or empty", "nextRecordsUrl");

            string url = _host + nextRecordsUrl;
            return SendHttpGetRequest(url);
        }

        private string SendHttpGetRequest(string url)
        {
            // Build GET request
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", "Bearer " + _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            //request.Headers.Add("X-PrettyPrint", "1");

            // Process response
            HttpResponseMessage response = null;
            try
            {
                response = _httpClient.SendAsync(request).Result;
            }
            catch (Exception e)
            {
                if (e.InnerException != null) throw new SalesforceHttpGetException(e.InnerException, url);
                else throw new SalesforceHttpGetException(e, url);
            }
            return response.Content.ReadAsStringAsync().Result;
        }

        private string SendHttpPostRequest<T>(string url, T sfObject)
        {

            string JsonStringObject = JsonConvert.SerializeObject(sfObject);
            // Build request content
            var httpContent = new StringContent(JsonStringObject, Encoding.UTF8, "application/json");

            // Build POST request
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", "Bearer " + _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-PrettyPrint", "1");
            request.Content = httpContent;

            // Process response
            HttpResponseMessage response = null;
            try
            {
                response = _httpClient.SendAsync(request).Result;
            }
            catch (Exception e)
            {
                if (e.InnerException != null) throw new SalesforceHttpPostException(e.InnerException, url, typeof(T).Name, (JsonConvert.SerializeObject(sfObject)).PrettifyJson());
                else throw new SalesforceHttpPostException(e, url, typeof(T).Name, (JsonConvert.SerializeObject(sfObject)).PrettifyJson());
            }
            return response.Content.ReadAsStringAsync().Result;
        }

        private string SendHttpDeleteRequest(string url)
        {


            // Build POST request
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", "Bearer " + _sessionId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-PrettyPrint", "1");

            // Process response
            HttpResponseMessage response = null;
            try
            {
                response = _httpClient.SendAsync(request).Result;
            }
            catch (Exception e)
            {

            }
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}