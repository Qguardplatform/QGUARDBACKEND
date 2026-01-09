using qguardbackend.Core.Interfaces;
using qguardbackend.Data.DTOs;
using System.Text;

namespace examportal.ServiceExtensions
{
    public class CustomHttpHandler : DelegatingHandler
    {
        private readonly IProctorMeTrackerService _trackerService;

        public CustomHttpHandler(IProctorMeTrackerService trackerService)
        {
            _trackerService = trackerService;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var trackerRecord = new ProctorMeTrackerModel();
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                // for every Http call, you must supply the transaction Id in the header of the request. So i am checking them here.
                if (!request.Headers.TryGetValues("x-transaction-id", out IEnumerable<string> transactionID))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Content = new StringContent("Missing transaction Id", Encoding.UTF8),
                        ReasonPhrase = "Bad Request"
                    };
                }
                // get the status of the transaction and also update it
                var transactionGUID = transactionID.FirstOrDefault();
                if (transactionGUID == "None")
                {
                    trackerRecord.ActivityDescription = request.Content != null ? await request.Content.ReadAsStringAsync() : request.RequestUri.ToString();
                    trackerRecord.ActivityRequest = $"{request.Method} -- {request.RequestUri}";
                    trackerRecord.CreatedAt = DateTime.Now;

                    // TODO: shape data to log into loan tracker table 
                    request.Headers.Remove("X-TransactionID");
                    response = await base.SendAsync(request, token);

                    trackerRecord.ActivityResponse = $"{response.StatusCode}";
                    trackerRecord.ActivityResponseDesc = await response.Content.ReadAsStringAsync() ?? response.ReasonPhrase;
                    trackerRecord.ApplicationState = response.ReasonPhrase == "OK" ? "INITIATED" : "FAILED WHEN INITIATED";
                    await _trackerService.Create(trackerRecord);
                }
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.Content = new StringContent(ex.Message, Encoding.UTF8);

                trackerRecord.ActivityResponse = ex.Message;
                trackerRecord.ActivityResponseDesc = ex.Message;
                trackerRecord.ApplicationState = "FAILED";
                await _trackerService.Create(trackerRecord);
                return response;
            }
        }
    }
}