using DeptMTB.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DeptMTB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchMovieDataController : ControllerBase
    {
        private readonly ILogger<SearchMovieDataController> _logger;
        private readonly IConfiguration Configuration;
        IMemoryCache _memoryCache;

        public SearchMovieDataController(ILogger<SearchMovieDataController> logger, IConfiguration configuration,
        IMemoryCache memoryCache)
        {
            _logger = logger;
            Configuration = configuration;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        [Route("~/api/getMovieDetails")]
        public AggregatedMovieData GetMovieDetails (string searchTerm)
        {
            if (!_memoryCache.TryGetValue(searchTerm, out AggregatedMovieData movieData))
            {
                SearchYouTubeController searchYouTube = new SearchYouTubeController(_logger, Configuration);
                SearchIMDbController searchIMDb = new SearchIMDbController(_logger, Configuration);
                try
                {
                    movieData = new AggregatedMovieData();
                    movieData.Trailers = searchYouTube.GetMovieTrailers(searchTerm);
                    movieData.IMDbData = searchIMDb.GetMovieDetails(searchTerm);
                }
                catch(Exception e)
                {
                    _logger.LogError(e.Message);
                }
                var cacheExpiryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddSeconds(600),
                    Priority = CacheItemPriority.High,
                    SlidingExpiration = TimeSpan.FromSeconds(600)
                };
                _memoryCache.Set(searchTerm, movieData, cacheExpiryOptions);
            }
            return movieData;
        }
    }
}
