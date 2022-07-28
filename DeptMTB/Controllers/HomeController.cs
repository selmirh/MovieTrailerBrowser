using DeptMTB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace DeptMTB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration Configuration;
        IMemoryCache _memoryCache;

        public HomeController(ILogger<HomeController> logger, IConfiguration config, IMemoryCache memoryCache)
        {
            _logger = logger;
            Configuration = config;
            _memoryCache = memoryCache;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public ActionResult ImdbData(string searchTerm)
        {
            var movieData = GetCachedData(searchTerm);
            return movieData != null ? PartialView("Partials/_ImdbData", movieData.IMDbData) : Error();
        }

        public ActionResult YouTubeVideo(string searchTerm, int videosShown = 1, bool showMore = false)
        {
            var trailers = GetCachedData(searchTerm).Trailers;
            if(showMore)
            {
                videosShown += 3;
            }
            ViewBag.SearchTerm = searchTerm;
            var t = new List<string>(trailers.Take(videosShown));
            return trailers != null ? PartialView("Partials/_YouTubeVideos", t) : Error();
        }

        public async Task<IActionResult> GetMovieData(string searchTerm)
        {
            if (!_memoryCache.TryGetValue(searchTerm, out AggregatedMovieData movieData))
            {
                try
                {
                    string apiUrl = Configuration["ApiUrl"] + "getMovieDetails?searchTerm=" + searchTerm;
                    movieData = new AggregatedMovieData();
                    HttpClient client = new HttpClient();
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        movieData = await response.Content.ReadFromJsonAsync<AggregatedMovieData>();
                    }

                    var cacheExpiryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTime.Now.AddSeconds(600),
                        Priority = CacheItemPriority.High,
                        SlidingExpiration = TimeSpan.FromSeconds(600)
                    };
                    _memoryCache.Set(searchTerm, movieData, cacheExpiryOptions);

                    return Ok(movieData);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    return Error();
                }
            }
            else
            {
                return Ok(movieData);
            }
        }

        private AggregatedMovieData GetCachedData (string searchTerm)
        {
            _memoryCache.TryGetValue(searchTerm, out AggregatedMovieData cachedData);
            return cachedData;
        }
    }
}