using System;
using System.Net;

namespace PersonListener.Infrastructure.Exceptions
{
    public class GetTenureException : Exception
    {
        public Guid PersonId { get; }
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public GetTenureException(Guid id, HttpStatusCode statusCode, string responseBody)
            : base($"Failed to get tenure details for id {id}. Status code: {statusCode}; Message: {responseBody}")
        {
            PersonId = id;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
