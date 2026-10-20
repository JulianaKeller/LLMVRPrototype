using UnityEngine;
using System.Threading.Tasks;

namespace LLMVRPrototype
{
    // Does not depends on UnityWebRequest so future implementations, e.g. unit tests, can use it flexibly
    public interface ILLMTransport
    {
        Task<HttpResponseData> SendAsync(HttpRequestData request);
    }
}
