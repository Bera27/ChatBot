using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Api.Filters
{
    public class ApiKeyAttribute : TypeFilterAttribute
    {
        public ApiKeyAttribute() : base(typeof(ApiKeyFilter)) { }
    }
}